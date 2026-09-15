# tools/

Standalone dev tools — never built into the plugin (excluded via `NOSDA.csproj`'s
`<Compile Remove="tools\**\*.cs" />`).

## DeathCounterCheck

Self-check for `DeathCounter`, the one piece of plugin logic with no Unity/game-API dependency.

```bash
dotnet run --project tools/DeathCounterCheck
```

## SoundPoolSelectorCheck

Self-check for `SoundPoolSelector`, the pure logic behind per-player/randomized kill sounds
(docs/per-player-and-random-sounds.md).

```bash
dotnet run --project tools/SoundPoolSelectorCheck
```
