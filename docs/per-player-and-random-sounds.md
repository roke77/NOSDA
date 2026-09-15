# Per-player and randomized kill sounds

Player suggestion: assign a unique sound to a specific player's kills, and support multiple
sound variants played at random instead of one fixed file per category.

## Current behavior (baseline)

`ShootdownAnnouncer` loads exactly one `AudioClip` each for `enemy.wav` and `friendly.wav` from
next to the DLL (`LoadClip` in [ShootdownAnnouncer.cs](../ShootdownAnnouncer.cs)) and
`Announce()` always plays whichever one matches `isFriendly`.

## Design

Both requests reduce to one mechanism: a **sound pool** — a list of one or more clips, one is
picked at random on each play — resolved per kill in priority order:

1. **Per-player pool** — if the killed player's SteamID has its own non-empty pool, use it.
2. **Faction pool** — otherwise, the existing enemy/friendly pool.

A pool with exactly one clip behaves exactly like today (no randomization to notice), so this is
a strict superset of the current behavior, not a breaking change.

### Folder layout

```
enemy.wav                  (unchanged — the default single-file enemy pool)
friendly.wav                (unchanged — the default single-file friendly pool)
sounds/
  enemy/*.wav                (optional — if present, replaces enemy.wav with a randomized pool)
  friendly/*.wav              (optional — same, for friendly)
  players/<steamid64>/*.wav   (optional — per-player pool, one folder per SteamID)
```

`enemy.wav`/`friendly.wav` stay as the zero-config default so existing installs and the shipped
release zip need no change. `sounds/` is entirely optional and only consulted if present — this
keeps the same "drop a file in next to the DLL" convention players already know, just extended to
a folder when they want more than one file.

SteamID64 is what `DeathCounter` already keys deaths by
([DeathCounter.cs](../DeathCounter.cs)), so no new identifier is introduced. SteamID `0` (a
non-Steam/LAN player — see `DeathCounter`'s own comment on this) never gets a personal pool
lookup, since every such player shares that key and a "personal" sound would leak across all of
them.

**Discoverability problem:** a player has no easy way to find another pilot's SteamID64 today.
Fix: log it at kill time (`Plugin.Log?.LogInfo($"[NOSDA] ... SteamID={killedAircraft.Player.SteamID}")`)
so anyone can grab an ID straight from the BepInEx console/log after playing alongside that pilot,
with no external site needed.

### Resolution logic (the testable piece)

Which pool applies is a small pure decision, independent of Unity/file I/O — the same shape as
`DeathCounter.RecordDeath`, and the same reason it's worth pulling out on its own:

```csharp
internal static class SoundPoolSelector
{
    // playerPoolsAvailable = the set of SteamIDs that have a non-empty players/ folder.
    internal static string ResolveKey(ulong steamId, bool isFriendly, ISet<ulong> playerPoolsAvailable)
        => steamId != 0 && playerPoolsAvailable.Contains(steamId)
            ? $"player:{steamId}"
            : isFriendly ? "friendly" : "enemy";
}
```

`ShootdownAnnouncer` holds a `Dictionary<string, List<AudioClip>>` keyed the same way, built once
at startup by scanning `sounds/players/*/` (folder name = SteamID) plus the enemy/friendly
sources, and `Announce()` becomes: resolve the key, pick `clips[Random.Range(0, clips.Count)]`.

### Loading changes

`LoadClip` (single file → single clip) becomes `LoadClipPool` (a file *or* a directory → a list of
clips): if `sounds/<bucket>/` exists and has `*.wav` files, load each of those; otherwise fall back
to loading the single legacy file. Same `UnityWebRequestMultimedia` + coroutine approach as today,
just looped per file. Same fail-safe pattern as the rest of the project — a missing/unreadable file
logs a warning and is skipped, never a crash (`Plugin.Log?.LogWarning`, matching `LoadClip`'s
existing behavior).

### Config

None needed. Presence of a folder is the toggle, consistent with the existing zero-config
"replace the wav" convention — no new F1 menu entries required for the base feature. An F1
toggle to disable per-player overrides could be added later if anyone actually asks for it, but
isn't part of this plan (YAGNI).

## Feasibility

Fully feasible, no game-API risk — the only Nuclear Option-specific data already flows through
today (`Player.SteamID`, already read in `Plugin.cs`'s kill patch). Everything else is file I/O
and `AudioClip` loading the project already does. Estimated shape of the change:

- `SoundPoolSelector.cs` (new, pure logic) + a `tools/`-style self-check, following the
  `DeathCounter`/`DeathCounterCheck` precedent.
- `ShootdownAnnouncer.cs`: `LoadClip` → `LoadClipPool`, single-clip fields → a pool dictionary,
  `Announce()` resolves through `SoundPoolSelector`.
- `README.md`: document the optional `sounds/` layout and the new SteamID log line.

No changes needed to `Plugin.cs`'s kill patch, `Banner`, `DeathCounter`, or `SoundConfig` — this
is additive and isolated to how a sound is chosen and loaded.

## Open questions for the player base

- Folder-of-wavs vs. a single multi-file naming convention (`enemy1.wav`, `enemy2.wav`, ...) —
  folders were chosen above since they scale to "many players, many variants" without filename
  collisions, but a flat naming scheme is simpler for the enemy/friendly case alone.
- Whether random selection should avoid repeating the same clip twice in a row (a "shuffle bag")
  — not planned initially; add only if players report it feeling too repetitive with 2-3 clips.
