# NOSDA — Nuclear Option Shut-Down Announcer

A [BepInEx](https://github.com/BepInEx/BepInEx) client-side mod for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/) that plays a sound whenever a player is shot down.

## What it does

Whenever the game announces a player has been shot down, NOSDA plays `sound.wav` from its own plugin folder. Swap that file out for your own — same filename, any WAV — to change the sound.

Runs entirely client-side: it only reacts to a notification the client already receives, so it's safe to use on public servers.

## Install

Drop `NOSDA.dll` and `sound.wav` into `BepInEx/plugins/` in your Nuclear Option install.

## Build

Requires a local Nuclear Option install (for `Assembly-CSharp.dll` and friends) — see `GameDir.props` setup in `NOSDA.csproj`.

```bash
dotnet build
```
