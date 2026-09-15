# NOSDA — Nuclear Option Shut-Down Announcer

A [BepInEx](https://github.com/BepInEx/BepInEx) client-side mod for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/) that plays a sound and shows a banner whenever a player is shot down or crashes.

## What it does

Whenever the game announces a player has been shot down or crashed, NOSDA plays a sound and shows an on-screen banner:

- **Shot down**: `PLAYER SHOT DOWN`, with the shooter's name shown underneath. Plays `enemy.wav` if the shot-down player is on a different faction than you, `friendly.wav` if they share yours.
- **Crash** (no shooter — terrain, fuel, structural failure): `PLAYER CRASHED`, no shooter line. Same enemy/friendly sound choice.
- If that pilot has already died more than once this session, their name is followed by a count in parentheses, e.g. `PLAYER (3) SHOT DOWN`. This only counts deaths NOSDA has personally observed since the game was launched — it doesn't know about deaths from before you joined the server, or on any other server.

Swap `enemy.wav`/`friendly.wav` out for your own — same filenames, any WAV — to change the sounds.

For more than one file, or a sound assigned to one specific pilot, create a `sounds/` folder next to the DLL:

```
sounds/
  enemy/*.wav                  optional — randomizes across every .wav here instead of enemy.wav
  friendly/*.wav                optional — same, for friendly.wav
  players/<steamid64>/*.wav     optional — plays only for that pilot's kills, enemy or friendly
```

Each folder can hold one or more WAVs; with more than one, NOSDA picks a random one each time. A player's personal folder always takes priority over enemy/friendly for their kills. NOSDA logs each kill's SteamID64 to the BepInEx console/log so you can find the ID to use for `players/<steamid64>/`.

If [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) is installed, press F1 in-game and open NOSDA's "Banner" and "Sound" sections to adjust each line's font size, color, and screen position, the display duration, and the sound volume — all live, with buttons to preview a sample banner without waiting for a real kill.

Runs entirely client-side: it only reacts to a notification the client already receives, so it's safe to use on public servers.

## Install

1. Download `NOSDA_X.Y.Z.zip` from the [latest release](https://github.com/roke77/NOSDA/releases/latest).
2. Extract `NOSDA.dll`, `enemy.wav`, and `friendly.wav` directly into `BepInEx/plugins/` in your Nuclear Option install.

## Build

Requires a local Nuclear Option install (for `Assembly-CSharp.dll` and friends) — see `GameDir.props` setup in `NOSDA.csproj`.

```bash
dotnet build
```
