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
        internal const string PluginVersion = "1.0.0";

        internal static ManualLogSource? Log;
        internal static ShootdownAnnouncer? Announcer;

        private bool _spawnedWorker;

        private void Awake()
        {
            Log = Logger;
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
                // "shot down" (KillTypeExtensions.GetVerb) is only the verb for KillType.Aircraft
                // with a killer present — an aircraft killed with no killer "crashed" instead.
                if (killedType != KillType.Aircraft) return;
                if (!UnitRegistry.TryGetPersistentUnit(killerID, out _)) return;
                if (!UnitRegistry.TryGetPersistentUnit(killedID, out PersistentUnit killed)) return;
                if (killed.unit is not Aircraft aircraft || aircraft.Player == null) return;

                Announcer?.Announce(aircraft.Player.GetDisplayName(PlayerNameContext.ChatOrLeaderboard));
            }
        }
    }
}
