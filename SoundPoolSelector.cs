using System.Collections.Generic;

namespace NOSDA
{
    // Which sound pool plays for a kill. steamId 0 (non-Steam/LAN players, see DeathCounter's own
    // comment on this) never resolves to a personal pool — every such player shares that key, so a
    // "personal" sound would leak across all of them.
    internal static class SoundPoolSelector
    {
        internal const string EnemyKey = "enemy";
        internal const string FriendlyKey = "friendly";

        internal static string ResolveKey(ulong steamId, bool isFriendly, ISet<ulong> playerPoolsAvailable)
        {
            if (steamId != 0 && playerPoolsAvailable.Contains(steamId)) return "player:" + steamId;
            return isFriendly ? FriendlyKey : EnemyKey;
        }
    }
}
