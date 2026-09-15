# NOSDA — Nuclear Option Shut-Down Announcer

A [BepInEx](https://github.com/BepInEx/BepInEx) client-side mod for [Nuclear Option](https://store.steampowered.com/app/2168680/Nuclear_Option/) that plays a sound and shows a banner whenever a player is shot down or crashes.

## What it does

Whenever the game announces a player has been shot down or crashed, NOSDA plays a sound and shows an on-screen banner:

- **Shot down**: `PLAYER SHOT DOWN`, with the shooter's name shown underneath. Plays `enemy.wav` if the shot-down player is on a different faction than you, `friendly.wav` if they share yours.
- **Crash** (no shooter — terrain, fuel, structural failure): `PLAYER CRASHED`, no shooter line. Same enemy/friendly sound choice.
- If that pilot has already died more than once this mission, their name is followed by a count in parentheses, e.g. `PLAYER (3) SHOT DOWN`. The count resets whenever a mission starts (leaving to the main menu, restarting, or starting a new one), and only counts deaths NOSDA has personally observed — it doesn't know about deaths from before you joined the server, or on any other server.

If [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) is installed, press F1 in-game and open NOSDA's "Banner" and "Sound" sections to adjust each line's font size, color, and screen position, the display duration, and the sound volume — all live, with buttons to preview a sample banner without waiting for a real kill.

Runs entirely client-side: it only reacts to a notification the client already receives, so it's safe to use on public servers.

## Install

1. Download `NOSDA_X.Y.Z.zip` from the [latest release](https://github.com/roke77/NOSDA/releases/latest).
2. Extract `NOSDA.dll`, `enemy.wav`, and `friendly.wav` directly into `BepInEx/plugins/` in your Nuclear Option install.

## Sounds

Three levels, each optional on top of the last — use whichever fits.

**1. Replace the default sounds.** Swap `enemy.wav`/`friendly.wav` for your own — same filenames, any WAV:

```
BepInEx/plugins/NOSDA/
  enemy.wav       ← plays when an enemy player is shot down/crashes
  friendly.wav    ← plays when a friendly player is shot down/crashes
```

**2. Play a random sound from a pool.** Add a `sounds/enemy/` and/or `sounds/friendly/` folder with as many WAVs as you like — filenames don't matter, only the `.wav` extension:

```
BepInEx/plugins/NOSDA/
  sounds/
    enemy/
      explosion1.wav
      explosion2.wav
    friendly/
      oof1.wav
      oof2.wav
```

A side with an empty or missing folder just falls back to its plain `enemy.wav`/`friendly.wav` — you can mix, e.g. a randomized enemy pool with a single friendly.wav.

**3. Give one pilot their own sound(s).** Add a folder under `sounds/players/` named after that player's **SteamID64**:

```
BepInEx/plugins/NOSDA/
  sounds/
    players/
      76561198012345678/
        haha.wav
        haha2.wav    ← optional extras also randomize
```

Whenever that pilot dies — enemy or friendly — this pool wins over the enemy/friendly one. NOSDA logs each kill's SteamID64 to the BepInEx console/log (`[NOSDA] PlayerName down (SteamID=...)`), so you can grab an ID from there after playing alongside someone.

**Precedence:** that pilot's personal pool (if they have one) → otherwise the enemy/friendly pool (the `sounds/` folder if it has files, else the plain `enemy.wav`/`friendly.wav`).

## Build

Requires a local Nuclear Option install (for `Assembly-CSharp.dll` and friends) — see `GameDir.props` setup in `NOSDA.csproj`.

```bash
dotnet build
```
