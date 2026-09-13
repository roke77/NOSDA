# NOSDA — Nuclear Option Shut-Down Announcer

A [BepInEx](https://github.com/BepInEx/BepInEx) client-side mod for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/) that plays a sound and shows a banner whenever a player is shot down or crashes.

## What it does

Whenever the game announces a player has been shot down or crashed, NOSDA plays `sound.wav` from its own plugin folder and shows an on-screen banner:

- **Shot down**: `PLAYER SHOT DOWN`, with the shooter's name shown underneath.
- **Crash** (no shooter — terrain, fuel, structural failure): `PLAYER CRASHED`, no shooter line.

Swap `sound.wav` out for your own — same filename, any WAV — to change the sound.

Runs entirely client-side: it only reacts to a notification the client already receives, so it's safe to use on public servers.

## Install

1. Download `NOSDA_X.Y.Z.zip` from the [latest release](https://github.com/roke77/NOSDA/releases/latest).
2. Extract `NOSDA.dll` and `sound.wav` directly into `BepInEx/plugins/` in your Nuclear Option install.

## Build

Requires a local Nuclear Option install (for `Assembly-CSharp.dll` and friends) — see `GameDir.props` setup in `NOSDA.csproj`.

```bash
dotnet build
```
