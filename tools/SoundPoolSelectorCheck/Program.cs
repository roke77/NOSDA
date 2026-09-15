using NOSDA;

var withPlayerPool = new HashSet<ulong> { 123 };

string key = SoundPoolSelector.ResolveKey(123, isFriendly: false, withPlayerPool);
if (key != "player:123") throw new Exception($"a player with a pool should resolve to their own key, got {key}");

key = SoundPoolSelector.ResolveKey(456, isFriendly: false, withPlayerPool);
if (key != SoundPoolSelector.EnemyKey) throw new Exception($"a player with no pool should fall back to enemy, got {key}");

key = SoundPoolSelector.ResolveKey(456, isFriendly: true, withPlayerPool);
if (key != SoundPoolSelector.FriendlyKey) throw new Exception($"a player with no pool should fall back to friendly, got {key}");

var zeroHasPool = new HashSet<ulong> { 0 };
key = SoundPoolSelector.ResolveKey(0, isFriendly: false, zeroHasPool);
if (key != SoundPoolSelector.EnemyKey)
    throw new Exception($"steamId 0 (non-Steam/LAN, a shared bucket by design) must never resolve to a personal pool, got {key}");

Console.WriteLine("SoundPoolSelector: OK");
