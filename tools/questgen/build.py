# Builds Quests/eventQuests.json, eventLocales.json and eventQuestMeta.json.
# Run: python -I tools/questgen/build.py [path to SPT_Data/database]
import collections
import sys

sys.path.insert(0, __file__.rsplit("\\", 1)[0].rsplit("/", 1)[0])
import lib
import events_quick  # noqa: F401  (each module registers its quests)
for name in ("events_mystery", "events_factions", "events_lighthouse"):
    try:
        __import__(name)
    except ModuleNotFoundError as e:
        if e.name != name:
            raise

lib.build_all()
lib.write()
per = collections.Counter(q.event for q in lib.ALL)
inst = collections.Counter()
for q in lib.ALL:
    inst[q.event] += len(lib._instances[q.key])
print(len(lib.quests), "quests from", len(lib.ALL), "specs")
for e in per:
    print(f"  {e}: {per[e]} quests ({inst[e]} with route copies)")
reused = sum(1 for q in lib.quests.values() for c in q["conditions"]["AvailableForFinish"] if c["id"] in lib.orphans)
total = sum(len(q["conditions"]["AvailableForFinish"]) for q in lib.quests.values())
print(f"objectives: {total}, {reused} reuse the game's own translated text")
