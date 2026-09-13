# tools/

Standalone dev tools — never built into the plugin (excluded via `NOSDA.csproj`'s
`<Compile Remove="tools\**\*.cs" />`).

## DeathCounterCheck

Self-check for `DeathCounter`, the one piece of plugin logic with no Unity/game-API dependency.

```bash
dotnet run --project tools/DeathCounterCheck
```
