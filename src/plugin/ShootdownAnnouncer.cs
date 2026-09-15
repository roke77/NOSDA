using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace NOSDA
{
    // Owns the audio clips and delegates the on-screen banner to Banner. Lives on a persistent
    // worker GameObject (see Plugin.OnSceneLoaded) rather than the BaseUnityPlugin itself so it
    // survives the boot -> MainMenu scene transition.
    internal class ShootdownAnnouncer : MonoBehaviour
    {
        private const string EnemySoundFileName = "enemy.wav";
        private const string FriendlySoundFileName = "friendly.wav";
        private const string PlayersFolderName = "players";

        private AudioSource _audioSource = null!;
        private Banner _banner = null!;

        // Keyed by SoundPoolSelector's keys ("enemy", "friendly", "player:<steamid>"). A pool with
        // one clip behaves exactly like the old single-clip fields; docs/per-player-and-random-sounds.md.
        private readonly Dictionary<string, List<AudioClip>> _pools = new Dictionary<string, List<AudioClip>>();
        private readonly HashSet<ulong> _playerPoolsAvailable = new HashSet<ulong>();

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 2D cue, not positional in the 3D scene
            _banner = gameObject.AddComponent<Banner>();
            _banner.Build();

            string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

            // sounds/enemy/ and sounds/friendly/ are optional randomized pools; if absent, fall
            // back to the single legacy file next to the DLL so existing installs need no change.
            string soundsDir = Path.Combine(dllDir, "sounds");
            StartCoroutine(LoadFallbackPool(SoundPoolSelector.EnemyKey, Path.Combine(soundsDir, "enemy"), Path.Combine(dllDir, EnemySoundFileName)));
            StartCoroutine(LoadFallbackPool(SoundPoolSelector.FriendlyKey, Path.Combine(soundsDir, "friendly"), Path.Combine(dllDir, FriendlySoundFileName)));

            string playersDir = Path.Combine(soundsDir, PlayersFolderName);
            if (Directory.Exists(playersDir))
            {
                foreach (string playerDir in Directory.GetDirectories(playersDir))
                {
                    string folderName = Path.GetFileName(playerDir);
                    if (!ulong.TryParse(folderName, out ulong steamId))
                    {
                        Plugin.Log?.LogWarning($"[NOSDA] sounds/players/{folderName} isn't a valid SteamID64 folder name — skipped.");
                        continue;
                    }
                    _playerPoolsAvailable.Add(steamId);
                    StartCoroutine(LoadPlayerPool("player:" + steamId, playerDir));
                }
            }
        }

        private static string[] ListWavFiles(string dir) => Directory.Exists(dir) ? Directory.GetFiles(dir, "*.wav") : Array.Empty<string>();

        // Loads every *.wav in poolDir if it has any; otherwise loads the single legacyFile as a
        // one-clip pool, so a fresh install with no sounds/ folder at all behaves exactly like before.
        private IEnumerator LoadFallbackPool(string key, string poolDir, string legacyFile)
        {
            string[] files = ListWavFiles(poolDir);
            return LoadClipsInto(key, files.Length > 0 ? files : new[] { legacyFile });
        }

        private IEnumerator LoadPlayerPool(string key, string poolDir)
        {
            string[] files = ListWavFiles(poolDir);
            if (files.Length == 0)
            {
                Plugin.Log?.LogWarning($"[NOSDA] {poolDir} has no .wav files — that pool is disabled.");
                yield break;
            }
            yield return LoadClipsInto(key, files);
        }

        // sounds ship as loose files (not embedded resources) specifically so a player can drop in
        // their own replacements.
        private IEnumerator LoadClipsInto(string key, IReadOnlyList<string> paths)
        {
            foreach (string path in paths)
            {
                if (!File.Exists(path))
                {
                    Plugin.Log?.LogWarning($"[NOSDA] {path} not found — that sound is disabled.");
                    continue;
                }

                string url = "file:///" + path.Replace('\\', '/');
                using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Log?.LogWarning($"[NOSDA] Failed to load {path}: {request.error}");
                    continue;
                }

                if (!_pools.TryGetValue(key, out List<AudioClip> clips)) _pools[key] = clips = new List<AudioClip>();
                clips.Add(DownloadHandlerAudioClip.GetContent(request));
            }
        }

        // killerName is null for a crash (no shooter). isFriendly is whether the killed player
        // shares the local player's own faction — it picks the fallback sound/banner color.
        // deathCount is that pilot's cumulative death count this session (DeathCounter.RecordDeath).
        internal void Announce(ulong steamId, string playerName, int deathCount, string? killerName, bool isFriendly)
        {
            string key = SoundPoolSelector.ResolveKey(steamId, isFriendly, _playerPoolsAvailable);
            if (_pools.TryGetValue(key, out List<AudioClip> clips) && clips.Count > 0)
            {
                AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Count)];
                _audioSource.PlayOneShot(clip, SoundConfig.Volume);
            }
            _banner.Show(playerName, deathCount, killerName, isFriendly);
        }
    }
}
