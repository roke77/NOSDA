using System.Collections.Generic;

namespace NOSDA
{
    // How many times each pilot (by Steam ID) has been shot down or crashed this session. Resets
    // whenever the game restarts — it's purely a local tally of what this client has observed, not
    // synced with other players, so a pilot who already died on a server before you joined starts
    // back at zero from your point of view.
    //
    // Keyed by Player.SteamID, which is 0 for a non-Steam player (e.g. a LAN game) — every such
    // player's deaths merge into one shared count under that key. No fallback identity is
    // currently substituted for that case.
    internal static class DeathCounter
    {
        private static readonly Dictionary<ulong, int> _counts = new Dictionary<ulong, int>();

        internal static int RecordDeath(ulong steamId)
        {
            _counts.TryGetValue(steamId, out int count);
            count++;
            _counts[steamId] = count;
            return count;
        }
    }
}
