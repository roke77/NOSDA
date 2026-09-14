using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NuclearOption.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NOSDA
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "com.roque.NOSDA";
        internal const string PluginName = "NOSDA";
        internal const string PluginVersion = "0.3.0";

        internal static ManualLogSource? Log;
        internal static ShootdownAnnouncer? Announcer;

        private bool _spawnedWorker;

        private void Awake()
        {
            Log = Logger;
            BannerConfig.Bind(Config);
            SoundConfig.Bind(Config);
            SceneManager.sceneLoaded += OnSceneLoaded;

            var harmony = new Harmony(PluginGuid);
            try { harmony.PatchAll(typeof(Plugin).Assembly); }
            catch (Exception e) { Log.LogWarning($"[NOSDA] Harmony patch failed to apply: {e.Message}"); }
        }

        // BepInEx_Manager (and anything parented under it during the boot scene) is torn down on
        // the boot -> MainMenu transition, so the announcer's AudioSource/Canvas have to live on a
        // GameObject spawned from a real scene load instead of directly on this plugin.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_spawnedWorker) return;
            _spawnedWorker = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            var worker = new GameObject("NOSDA_Worker");
            DontDestroyOnLoad(worker);
            Announcer = worker.AddComponent<ShootdownAnnouncer>();
        }

        // MessageManager.RpcKillMessage is the ClientRpc that drives the game's own kill-feed
        // ticker, but the public method only runs on the sending/host side — a remote client
        // receiving the RPC never calls it, going straight from the network Skeleton_... reader
        // into the generated UserCode_RpcKillMessage_... method instead. THAT is the method every
        // observer's kill actually reaches, so it's the patch target — resolved by name prefix
        // rather than the full mangled name, since the numeric suffix is a Mirage-weaver hash tied
        // to the RPC's signature and isn't guaranteed stable across a game update.
        [HarmonyPatch]
        private static class MessageManager_RpcKillMessage_Patch
        {
            private static MethodBase? TargetMethod()
            {
                foreach (MethodInfo m in typeof(MessageManager).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.Name.StartsWith("UserCode_RpcKillMessage")) return m;
                }
                return null;
            }

            private static void Postfix(PersistentID killerID, PersistentID killedID, KillType killedType)
            {
                // KillType.Aircraft covers both a shootdown (killerID resolves to some unit —
                // player or NPC/AI) and a crash (killerID doesn't resolve at all: terrain, fuel,
                // structural failure).
                if (killedType != KillType.Aircraft) return;
                if (!UnitRegistry.TryGetPersistentUnit(killedID, out PersistentUnit killed)) return;
                if (killed.unit is not Aircraft killedAircraft || killedAircraft.Player == null) return;

                string playerName = killedAircraft.Player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard);
                // No local HQ (e.g. not currently in a mission) reads as an enemy kill — the
                // pre-existing, already-tested default color rather than a guessed-at third state.
                bool isFriendly = GameManager.GetLocalHQ(out FactionHQ localHq) && localHq != null && killed.GetHQ() == localHq;
                int deathCount = DeathCounter.RecordDeath(killedAircraft.Player.SteamID);

                Announcer?.Announce(playerName, deathCount, GetKillerName(killerID), isFriendly);
            }

            // Null only when killerID doesn't resolve to any unit (a crash, no shooter). Prefers
            // the player's display name; falls back to the unit's own name for a non-player killer
            // (a frigate's guns, a SAM site, an AI aircraft) so that kill still reads as a shootdown.
            private static string? GetKillerName(PersistentID id)
            {
                if (!UnitRegistry.TryGetPersistentUnit(id, out PersistentUnit unit)) return null;
                if (unit.unit is Aircraft aircraft && aircraft.Player != null)
                    return aircraft.Player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard);
                return unit.unitName;
            }
        }
    }
}
