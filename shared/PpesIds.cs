namespace PPES
{
    /// <summary>Identifiers the server, the client plugin and the prepatcher must agree on. Compiled into all three.</summary>
    internal static class PpesIds
    {
        public const string Guid = "com.zezinho.pastpresenteventselector";

        public const string CleanupCrew = "ppesCleanupCrew";
        public const string Smuggler = "ppesSmuggler";
        public const string Pillager = "ppesPillager";
        public const string Gus = "ppesGus";
        public const string Basmach = "ppesBasmach";

        /// <summary>Bot roles PPES adds to the game's WildSpawnType enum.</summary>
        public static readonly FactionRole[] Factions =
        {
            new FactionRole(731001, CleanupCrew, "PpesCleanupCrew"),
            new FactionRole(731002, Smuggler, "PpesSmuggler"),
            new FactionRole(731003, Pillager, "PpesPillager"),
            new FactionRole(731004, Gus, "PpesGus"),
            new FactionRole(731005, Basmach, "PpesBasmach"),
        };

        public static bool IsFaction(int roleId)
        {
            foreach (var f in Factions)
                if (f.Id == roleId) return true;
            return false;
        }
    }

    internal sealed class FactionRole
    {
        public FactionRole(int id, string role, string scavRole)
        {
            Id = id;
            Role = role;
            ScavRole = scavRole;
        }

        public int Id { get; }
        /// <summary>Enum member name and bot table key (lowercased).</summary>
        public string Role { get; }
        /// <summary>Locale key suffix of the name shown on kill and in the end-of-raid list.</summary>
        public string ScavRole { get; }
    }
}
