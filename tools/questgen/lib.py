"""Shared builder for the PPES event quests. Specs live in the events_*.py modules; build.py runs them all."""
import difflib
import hashlib
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
DB = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else r"C:\SPT\SPT_Runtime\SPT_Data\database")

items = json.loads((DB / "templates/items.json").read_text(encoding="utf-8"))
presets = json.loads((DB / "globals.json").read_text(encoding="utf-8"))["ItemPresets"]
stock = json.loads((DB / "templates/quests.json").read_text(encoding="utf-8"))
loc = json.loads((DB / "locales/global/en.json").read_text(encoding="utf-8"))
wiki = json.loads((ROOT / "research/wiki_all.json").read_text(encoding="utf-8"))

TRADERS = {
    "Prapor": "54cb50c76803fa8b248b4571", "Therapist": "54cb57776803fa99248b456e", "Fence": "579dc571d53a0658a154fbec",
    "Skier": "58330581ace78e27b8b10cee", "Peacekeeper": "5935c25fb3acc3127c3d8cd9", "Mechanic": "5a7c2eca46aef81a7ca2145d",
    "Ragman": "5ac3b934156ae10c4430e83c", "Jaeger": "5c0647fdd443bc2504c2d371", "Lightkeeper": "638f541a29ffd1183d187f57",
    "Ref": "6617beeaa9cfa777ca915b7c",
}
RUB, USD, EUR, GP = "5449016a4bdc2d6f028b456f", "5696686a4bdc2da3298b456a", "569668774bdc2da2298b4568", "5d235b4d86f7742e017bc88a"
MAPS = {"customs": "bigmap", "woods": "Woods", "shoreline": "Shoreline", "interchange": "Interchange", "reserve": "RezervBase",
        "lighthouse": "Lighthouse", "streets": "TarkovStreets", "factory": ["factory4_day", "factory4_night"], "lab": "laboratory",
        "gz": ["Sandbox", "Sandbox_high"], "gz21": "Sandbox_high"}


def oid(*seed):
    # stable ids so regenerating never breaks profiles that hold these quests
    return hashlib.md5(("ppes-quests:" + ":".join(seed)).encode()).hexdigest()[:24]


# ---------- names, ids and leftover texts ----------
def norm(s):
    t = s.lower()
    for a, b in zip("асеорхукмтвн", "aceopxykmtbh"):
        t = t.replace(a, b)
    t = re.sub(r" \((quest|\d{4}|finale|consequences)\)$", "", t)
    return re.sub(r"[^a-z0-9]", "", t)


_names = {}
for k, v in loc.items():
    m = re.fullmatch(r"([0-9a-f]{24}) Name", k)
    if m and m.group(1) in items:
        _names.setdefault(v.strip().lower(), []).append(m.group(1))


# wiki spellings that differ from the game's item names
ALIASES = {"altyn assault helmet (olive drab)": "altyn bulletproof helmet (olive drab)",
           "rys-t assault helmet (black)": "rys-t bulletproof helmet (black)",
           "equipment crate (rare)": "unlocked equipment crate (rare)", "weapon crate (rare)": "unlocked weapon crate (rare)",
           "valuables crate (rare)": "unlocked valuables crate (rare)"}


def tpl(name):
    """Item id by its English name; raises so typos surface at build time."""
    key = name.strip().lower()
    hit = _names.get(ALIASES.get(key, key))
    if not hit:
        close = difflib.get_close_matches(name.lower(), list(_names), 3, 0.6)
        raise KeyError(f"item not found: {name!r}; close: {close}")
    return hit[0]


def tpls(*names):
    return [tpl(n) for n in names]


_quest_ids = {}
for k, v in loc.items():
    m = re.fullmatch(r"([0-9a-f]{24}) name", k)
    if m and m.group(1) not in stock:
        _quest_ids.setdefault(norm(v), []).append(m.group(1))


def leftover_ids(name):
    return list(_quest_ids.get(norm(name), []))


_used_cond = set()
for x in stock.values():
    for c in x["conditions"]["AvailableForFinish"]:
        _used_cond.add(c["id"])
        for cc in (c.get("counter") or {}).get("conditions", []):
            _used_cond.add(cc["id"])
orphans = {k: v for k, v in loc.items() if re.fullmatch(r"[0-9a-f]{24}", k) and k not in _used_cond and k not in stock}
_taken = set()


def orphan_for(text, prefer=None):
    """An objective text left over from a removed quest, so every language already has it."""
    if prefer and prefer in orphans and prefer not in _taken:
        _taken.add(prefer)
        return prefer
    t = text.lower()
    best, score = None, 0
    for k, v in orphans.items():
        if k in _taken:
            continue
        r = difflib.SequenceMatcher(None, t, v.lower())
        if r.real_quick_ratio() < 0.85 or r.quick_ratio() < 0.85:
            continue
        s = r.ratio()
        if s > score:
            best, score = k, s
    if best and score >= 0.9:
        _taken.add(best)
        return best
    return None


# ---------- wiki facts ----------
def _clean(s):
    s = re.sub(r"\[\[(?:[^|\]]*\|)?([^\]]*)\]\]", r"\1", s or "")
    return re.sub(r"<[^>]+>|'''|''|\{\{[^}]*\}\}", "", s).strip()


def wiki_entry(name):
    for k, v in wiki.items():
        if norm(k) == norm(name):
            return v
    raise KeyError(f"no wiki data for {name}")


def wiki_trader(name):
    g = _clean(wiki_entry(name)["info"].get("given by", ""))
    return TRADERS.get(g.split()[0]) if g else None


def wiki_rewards(name):
    """(exp, money, standing list, items) parsed from the wiki rewards list; unknown items are reported and skipped."""
    exp, money, standing, things, unknown = 0, None, [], [], []
    for line in wiki_entry(name)["rewards"].splitlines():
        if not line.startswith("*") or line.startswith("**"):
            continue
        l = _clean(line.lstrip("* "))
        if m := re.match(r"\+?([\d,]+) EXP", l):
            exp = int(m.group(1).replace(",", ""))
        elif m := re.match(r"(\w+) Rep \+?(-?[\d.]+)", l):
            if m.group(1) in TRADERS:
                standing.append((TRADERS[m.group(1)], float(m.group(2))))
        elif m := re.match(r"([\d,]+) (Roubles|Dollars|Euros|GP coins)$", l):
            cur = {"Roubles": RUB, "Dollars": USD, "Euros": EUR, "GP coins": GP}[m.group(2)]
            n = int(m.group(1).replace(",", ""))
            if money is None and cur != GP:
                money = (cur, n)
            else:
                things.append((cur, n))
        elif m := re.match(r"(\d+)\s*[×x�]\s*(.+)$", l):
            try:
                things.append((tpl(m.group(2)), int(m.group(1))))
            except KeyError:
                unknown.append(m.group(2))
    if unknown:
        print(f"  [{name}] reward items skipped: {unknown}")
    return exp, money, standing, things


# ---------- item trees for rewards ----------
def tree(t, key):
    root = oid(key, "root")
    preset = next((p for p in presets.values() if p.get("_encyclopedia") == t), None)
    if preset:
        ids = {it["_id"]: (root if it["_id"] == preset["_parent"] else oid(key, str(n))) for n, it in enumerate(preset["_items"])}
        out = []
        for it in preset["_items"]:
            new = {"_id": ids[it["_id"]], "_tpl": it["_tpl"]}
            if "parentId" in it and it["parentId"] in ids:
                new["parentId"], new["slotId"] = ids[it["parentId"]], it["slotId"]
            out.append(new)
        out[0]["upd"] = {"StackObjectsCount": 1}
        return out
    props = items[t]["_props"]
    out = [{"_id": root, "_tpl": t, "upd": {"StackObjectsCount": 1}}]
    for slot in props.get("Slots") or []:
        plate = (slot["_props"].get("filters") or [{}])[0].get("Plate")
        if plate:
            out.append({"_id": oid(key, slot["_name"]), "_tpl": plate, "parentId": root, "slotId": slot["_name"]})
    for st in props.get("StackSlots") or []:
        out.append({"_id": oid(key, "ammo"), "_tpl": st["_props"]["filters"][0]["Filter"][0], "parentId": root,
                    "slotId": "cartridges", "upd": {"StackObjectsCount": st["_max_count"]}})
    return out


def reward_items(qid, t, count):
    key = f"{qid}:{t}"
    p = items[t]["_props"]
    if t in (RUB, USD, EUR, GP) or (p.get("StackMaxSize", 1) > 1 and not p.get("StackSlots")):
        root = oid(key)
        return {"items": [{"_id": root, "_tpl": t, "upd": {"StackObjectsCount": count}}], "target": root, "value": count}
    out = []
    for n in range(count):
        out += tree(t, f"{key}:{n}")
    return {"items": out, "target": out[0]["_id"], "value": count}


def build_rewards(qid, exp, money, standing, things, crafts=()):
    out = []

    def base(kind):
        return {"availableInGameEditions": [], "gameMode": ["regular", "pve"], "id": oid(qid, "reward", str(len(out))),
                "isHidden": False, "type": kind, "unknown": False}
    if exp:
        r = base("Experience"); r["value"] = exp; out.append(r)
    for trader, value in standing:
        r = base("TraderStanding"); r["target"], r["value"] = trader, value; out.append(r)
    for t, n in ([money] if money else []) + list(things):
        r = base("Item"); r.update({"findInRaid": t not in (RUB, USD, EUR, GP), "isEncoded": False}); r.update(reward_items(qid, t, n)); out.append(r)
    for product in crafts:
        r = base("ProductionScheme"); root = oid(qid, "craft", product)
        r.update({"items": [{"_id": root, "_tpl": product, "upd": {"StackObjectsCount": 1}}], "target": root, "loyaltyLevel": 1, "traderId": 10})
        out.append(r)
    return out


# ---------- objectives ----------
# Each helper returns an objective: a condition factory plus its EN/PT text. The text picks a leftover id when one matches.
class Obj:
    def __init__(self, en, pt, make, cid=None):
        self.en, self.pt, self.make, self.cid = en, pt, make, cid


def _common(cid, n, kind, **extra):
    c = {"conditionType": kind, "dynamicLocale": False, "globalQuestCounterId": "", "id": cid, "index": n, "parentId": "", "visibilityConditions": []}
    c.update(extra)
    return c


def _counter(cid, n, ctype, conds, value=1, one_session=False):
    counter = {"id": oid(cid, "counter"), "conditions": [dict(c, id=oid(cid, "c", str(i)), dynamicLocale=False) for i, c in enumerate(conds)]}
    return _common(cid, n, "CounterCreator", completeInSeconds=0, counter=counter, doNotResetIfCounterCompleted=False,
                   isNecessary=False, isResetOnConditionFailed=False, oneSessionOnly=one_session, type=ctype, value=value)


def _maps(m):
    if m is None:
        return []
    out = []
    for x in ([m] if isinstance(m, str) else m):
        v = MAPS.get(x, x)
        out += v if isinstance(v, list) else [v]
    return out


def kill(en, pt, count=1, target="Any", roles=(), map=None, zones=(), weapons=(), wear=(), daytime=None, one_session=False, cid=None, extra=()):
    def make(c, n):
        k = {"bodyPart": [], "compareMethod": ">=", "conditionType": "Kills", "daytime": {"from": daytime[0], "to": daytime[1]} if daytime else {"from": 0, "to": 0},
             "distance": {"compareMethod": ">=", "value": 0}, "enemyEquipmentExclusive": [], "enemyEquipmentInclusive": [],
             "enemyHealthEffects": [], "resetOnSessionEnd": False, "savageRole": list(roles), "target": target, "value": 1,
             "weapon": list(weapons), "weaponCaliber": [], "weaponModsExclusive": [], "weaponModsInclusive": []}
        conds = [k]
        if map:
            conds.append({"conditionType": "Location", "target": _maps(map)})
        if zones:
            conds.append({"conditionType": "InZone", "zoneIds": list(zones)})
        if wear:
            conds.append({"IncludeNotEquippedItems": False, "conditionType": "Equipment", "equipmentExclusive": [], "equipmentInclusive": [list(w) if isinstance(w, (list, tuple)) else [w] for w in wear]})
        conds += list(extra)
        return _counter(c, n, "Elimination", conds, count, one_session)
    return Obj(en, pt, make, cid)


def visit(en, pt, zone, map=None, cid=None):
    def make(c, n):
        conds = [{"conditionType": "VisitPlace", "target": zone, "value": 1}]
        return _counter(c, n, "Exploration", conds)
    return Obj(en, pt, make, cid)


def extract(en, pt, map=None, status=("Survived", "Runner"), exit_name=None, count=1, cid=None, wear=()):
    def make(c, n):
        conds = []
        if wear:
            conds.append({"IncludeNotEquippedItems": False, "conditionType": "Equipment", "equipmentExclusive": [], "equipmentInclusive": [list(w) if isinstance(w, (list, tuple)) else [w] for w in wear]})
        if map:
            conds.append({"conditionType": "Location", "target": _maps(map)})
        conds.append({"conditionType": "ExitStatus", "status": list(status)})
        if exit_name:
            conds.append({"conditionType": "ExitName", "exitName": exit_name})
        return _counter(c, n, "Completion", conds, count)
    return Obj(en, pt, make, cid)


def flare(en, pt, zone, cid=None):
    return Obj(en, pt, lambda c, n: _counter(c, n, "Completion", [{"conditionType": "LaunchFlare", "target": zone}]), cid)


def handover(en, pt, targets, count=1, fir=False, cid=None):
    targets = [targets] if isinstance(targets, str) else list(targets)
    return Obj(en, pt, lambda c, n: _common(c, n, "HandoverItem", dogtagLevel=0, isEncoded=False, maxDurability=100, minDurability=0,
                                            onlyFoundInRaid=fir, target=targets, value=count), cid)


def find(en, pt, targets, count=1, fir=True, cid=None):
    targets = [targets] if isinstance(targets, str) else list(targets)
    return Obj(en, pt, lambda c, n: _common(c, n, "FindItem", countInRaid=False, dogtagLevel=0, isEncoded=False, maxDurability=100,
                                            minDurability=0, onlyFoundInRaid=fir, target=targets, value=count), cid)


def stash(en, pt, targets, zone, count=1, cid=None):
    targets = [targets] if isinstance(targets, str) else list(targets)
    return Obj(en, pt, lambda c, n: _common(c, n, "LeaveItemAtLocation", dogtagLevel=0, isEncoded=False, maxDurability=100, minDurability=0,
                                            onlyFoundInRaid=False, plantTime=10, target=targets, value=count, zoneId=zone), cid)


def mark(en, pt, zone, item="MS2000 Marker", cid=None):
    t = tpl(item)
    return Obj(en, pt, lambda c, n: _common(c, n, "PlaceBeacon", plantTime=30, target=[t], value=1, zoneId=zone), cid)


def sell(en, pt, trader, targets, count, cid=None):
    return Obj(en, pt, lambda c, n: _common(c, n, "SellItemToTrader", dogtagLevel=0, isEncoded=False, maxDurability=100, minDurability=0,
                                            onlyFoundInRaid=False, target=list(targets), value=count, **({"traderId": trader} if trader else {})), cid)


def category(*parents):
    """All item ids under these parent categories."""
    want, out = set(parents), []
    for i, x in items.items():
        p = x["_parent"]
        while p:
            if p in want:
                out.append(i)
                break
            p = items.get(p, {}).get("_parent")
    return sorted(out)


# ---------- quests ----------
class Q:
    def __init__(self, key, event, objectives, name=None, trader=None, after=None, needs=(), level=None, rewards=None,
                 desc=None, success=None, exclusive=None, fail=(), requires=None, ids=None, kind=None, crafts=(), extra_start=(), bonus=(), rivals=()):
        self.key, self.event, self.objectives = key, event, objectives
        self.name = name or key
        self.trader = trader or wiki_trader(self.name)
        self.after = after if isinstance(after, list) else ([after] if after else [])
        self.needs, self.level, self.rewards, self.desc, self.success = needs, level, rewards, desc, success
        self.exclusive, self.fail, self.requires, self.kind, self.crafts, self.extra_start = exclusive, fail, requires, kind, crafts, extra_start
        self.bonus, self.rivals = list(bonus), list(rivals)
        self.ids = ids if ids is not None else leftover_ids(self.name)
        if not self.trader:
            raise ValueError(f"{key}: no trader")

    def trader_for(self, path):
        # a callable trader picks the giver per route copy (Atonement is Skier's or Peacekeeper's)
        return self.trader(path) if callable(self.trader) else self.trader


ALL = []  # every Q in build order
_by_key = {}
_instances = {}   # key -> list of (qid, predecessor qid or None, path)
quests, texts, meta = {}, {"en": {}, "po": {}}, {"aliases": {}, "events": {}, "requires": {}}
EXISTING = {}     # stock quest ids usable as predecessors, by key
SWAPS = []        # (quest key, stock reward tpl, tpl from another mod) swapped in at load when that mod is installed


def add(*qs):
    for q in qs:
        ALL.append(q)
        _by_key[q.key] = q


def _icon(trader):
    return next((x["image"] for x in stock.values() if x["traderId"] == trader), "")


def _kind(q):
    if q.kind:
        return q.kind
    first = q.objectives[0].make("x" * 24, 0)
    t = first.get("type") or {"HandoverItem": "PickUp", "FindItem": "PickUp", "LeaveItemAtLocation": "PickUp", "PlaceBeacon": "PickUp", "SellItemToTrader": "Merchant"}.get(first["conditionType"], "Completion")
    return t


def build_all():
    for q in ALL:
        preds = []
        for a in q.after:
            if isinstance(a, tuple):            # (key, statuses)
                key, status = a
            else:
                key, status = a, (4,)
            if key in EXISTING:
                preds.append((EXISTING[key], status, key))
            else:
                preds += [(qid, status, path) for qid, _, path in _instances[key]]
        if not preds:
            preds = [(None, None, "")]
        insts = []
        for n, (pred, status, path) in enumerate(preds):
            ipath = f"{path}>{q.key}" if path else q.key
            qid = q.ids[n] if n < len(q.ids) else oid(ipath)
            insts.append((qid, pred, ipath, status))
        _instances[q.key] = [(qid, pred, ipath) for qid, pred, ipath, _ in insts]
        src = q.ids[0] if q.ids else insts[0][0]
        for n, (qid, pred, ipath, status) in enumerate(insts):
            _emit(q, qid, pred, status, src if n >= len(q.ids) and n > 0 else None, n, ipath)
    # exclusive groups: instances sharing a predecessor fail each other once one is started
    groups = {}
    for q in ALL:
        if q.exclusive:
            for qid, pred, _ in _instances[q.key]:
                groups.setdefault((q.exclusive, pred), []).append(qid)
    # rivals: quests in other chains that rule this one out once started
    for q in ALL:
        for rival in q.rivals:
            # a (key, statuses) rival rules this one out only at those statuses, e.g. (key, [4]) when it is completed
            rival, status = rival if isinstance(rival, tuple) else (rival, [2, 3, 4])
            for qid, _, _ in _instances[q.key]:
                for other, _, _ in _instances[rival]:
                    fail = quests[qid]["conditions"]["Fail"]
                    fail.append(_common(oid(qid, "rival", other), len(fail), "Quest", availableAfter=0, dispersion=0, status=list(status), target=other))
    for members in groups.values():
        for qid in members:
            for other in members:
                if other != qid:
                    fail = quests[qid]["conditions"]["Fail"]
                    fail.append(_common(oid(qid, "excl", other), len(fail), "Quest", availableAfter=0, dispersion=0, status=[2, 3, 4], target=other))


def _emit(q, qid, pred, status, alias_of, n, ipath=""):
    trader = q.trader_for(ipath)
    start = []
    if q.level:
        start.append(_common(oid(qid, "lvl"), len(start), "Level", compareMethod=">=", value=q.level))
    if pred:
        start.append(_common(oid(qid, "pred"), len(start), "Quest", availableAfter=0, dispersion=0, status=list(status), target=pred))
    for key, st in q.needs:
        target = EXISTING.get(key) or _instances[key][0][0]
        start.append(_common(oid(qid, "need", key), len(start), "Quest", availableAfter=0, dispersion=0, status=list(st), target=target))
    for c in q.extra_start:
        start.append(dict(c, id=oid(qid, "xs", str(len(start))), index=len(start)))
    if not start:
        start.append(_common(oid(qid, "lvl"), 0, "Level", compareMethod=">=", value=1))
    finish = []
    for i, o in enumerate(q.objectives):
        # copies of a quest reuse the same text; a leftover id is used when one with that text is still free
        cid = (o.cid if n == 0 else None) or orphan_for(o.en) or oid(qid, "obj", str(i))
        finish.append(o.make(cid, i))
        if cid not in orphans:
            texts["en"][cid], texts["po"][cid] = o.en, o.pt
    fail = []
    for i, f in enumerate(q.fail):
        fid = oid(qid, "fail", str(i))
        if isinstance(f, Obj):
            fail.append(f.make(fid, i))
            texts["en"][fid], texts["po"][fid] = f.en, f.pt
        else:
            fail.append(dict(f, id=fid, index=i))
    if q.rewards is not None:
        exp, money, standing, things = q.rewards
    else:
        exp, money, standing, things = wiki_rewards(q.name)
    things = list(things) + q.bonus
    keys = ["acceptPlayerMessage", "changeQuestMessageText", "completePlayerMessage", "declinePlayerMessage", "description",
            "failMessageText", "name", "note", "startedMessageText", "successMessageText"]
    quest = {k: f"{qid} {k}" for k in keys}
    quest.update({
        "QuestName": q.name, "_id": qid, "acceptanceAndFinishingSource": "eft", "arenaLocations": [], "canShowNotificationsInGame": True,
        "conditions": {"AvailableForFinish": finish, "AvailableForStart": start, "Fail": fail}, "gameModes": [], "image": _icon(trader),
        "instantComplete": False, "isKey": False, "location": "any", "progressSource": "eft", "rankingModes": [], "restartable": False,
        "rewards": {"Fail": [], "Started": [], "Success": build_rewards(qid, exp, money, standing, things, q.crafts)},
        "secretQuest": False, "side": "Pmc", "status": 0, "traderId": trader, "type": _kind(q),
    })
    quests[qid] = quest
    meta["events"].setdefault(q.event, []).append(qid)
    if q.requires:
        meta["requires"][qid] = q.requires
    has_texts = f"{qid} description" in loc
    if alias_of and not has_texts:
        meta["aliases"][qid] = alias_of
    elif not has_texts:
        if not q.desc:
            raise ValueError(f"{q.key}: needs desc/success texts")
        en_d, pt_d = q.desc
        en_s, pt_s = q.success or ("", "")
        for lang, d, s in (("en", en_d, en_s), ("po", pt_d, pt_s)):
            texts[lang][f"{qid} name"] = q.name
            texts[lang][f"{qid} description"] = d
            texts[lang][f"{qid} successMessageText"] = s
    for lang in ("en", "po"):
        for k in ("note", "changeQuestMessageText", "startedMessageText", "failMessageText", "acceptPlayerMessage", "declinePlayerMessage", "completePlayerMessage"):
            texts[lang].setdefault(f"{qid} {k}", "")


def write():
    meta["swaps"] = [{"quest": qid, "from": a, "to": b} for key, a, b in SWAPS for qid, _, _ in _instances[key]]
    out = ROOT / "Quests"
    out.mkdir(exist_ok=True)
    (out / "eventQuests.json").write_text(json.dumps(quests, ensure_ascii=False, indent=1), encoding="utf-8")
    (out / "eventLocales.json").write_text(json.dumps(texts, ensure_ascii=False, indent=1), encoding="utf-8")
    (out / "eventQuestMeta.json").write_text(json.dumps(meta, ensure_ascii=False, indent=1), encoding="utf-8")
