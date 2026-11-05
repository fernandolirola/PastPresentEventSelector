using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace PPES.Events;

/// <summary>Adds items to loot pools as a share of the picks, so the rest of the pool keeps its balance.</summary>
internal static class LootShares
{
    private static readonly string[] Maps = ["bigmap", "woods", "shoreline", "interchange", "lighthouse", "rezervbase", "tarkovstreets",
        "factory4_day", "factory4_night", "laboratory", "sandbox", "sandbox_high"];

    // drawers, jackets and PC blocks
    private static readonly HashSet<string> SmallContainers = ["5675838d4bdc2d95058b456e", "578f87b7245977356274f2cd", "578f8778245977358849a9b5",
        "5914944186f774189e5e76c2", "5937ef2b86f77408a47244b3", "59139c2186f77411564f8e42"];

    /// <summary>Puts an item in the small loot containers of every map at about <paramref name="share"/> of each container's picks.</summary>
    public static void AddToSmallContainers(LocationTable locations, string tpl, double share)
    {
        foreach (var map in Maps)
        {
            locations.GetLocation(map)?.StaticLoot?.AddTransformer(data =>
            {
                if (data is null) return data;
                foreach (var (container, details) in data)
                {
                    if (!SmallContainers.Contains(container.ToString()) || details.ItemDistribution is not { } dist || dist.Any(d => d.Tpl == tpl)) continue;
                    var sum = dist.Sum(d => d.RelativeProbability ?? 0);
                    details.ItemDistribution = dist.Append(new ItemDistribution { Tpl = tpl, RelativeProbability = (float)Math.Max(1, Math.Round(sum * share / (1 - share))) }).ToList();
                }
                return data;
            });
        }
    }

    /// <summary>Sets an item's weight so it is about <paramref name="share"/> of a weighted pool.</summary>
    public static void AddShare(Dictionary<MongoId, double> pool, string tpl, double share)
    {
        var others = pool.Where(p => p.Key != tpl).Sum(p => p.Value);
        pool[tpl] = Math.Max(1, Math.Round(others * share / (1 - share)));
    }
}
