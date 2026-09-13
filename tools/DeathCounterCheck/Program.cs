using NOSDA;

int first = DeathCounter.RecordDeath(123);
if (first != 1) throw new Exception($"first death for a pilot should return 1, got {first}");

int second = DeathCounter.RecordDeath(123);
if (second != 2) throw new Exception($"second death for the same pilot should return 2, got {second}");

int otherPilot = DeathCounter.RecordDeath(456);
if (otherPilot != 1) throw new Exception($"a different pilot's first death should return 1, got {otherPilot}");

int sameSteamIdZero = DeathCounter.RecordDeath(0);
int otherSteamIdZero = DeathCounter.RecordDeath(0);
if (sameSteamIdZero != 1 || otherSteamIdZero != 2)
    throw new Exception($"steamId 0 (non-Steam/LAN players) is a known shared bucket by design, not a bug — got {sameSteamIdZero}, {otherSteamIdZero}");

Console.WriteLine("DeathCounter: OK");
