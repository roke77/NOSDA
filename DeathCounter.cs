using System.Collections.Generic;

namespace NOSDA
{
    // How many times each pilot (by Steam ID) has been shot down or crashed this session. Resets
    // whenever the game restarts — it's purely a local tally of what this client has observed, not
    // synced with other players, so a pilot who already died on a server before you joined starts
    // back at zero from your point of view.
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
