using System;
using System.Collections;
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

        private AudioSource _audioSource = null!;
        private AudioClip? _enemyClip;
        private AudioClip? _friendlyClip;
        private Banner _banner = null!;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 2D cue, not positional in the 3D scene
            _banner = gameObject.AddComponent<Banner>();
            _banner.Build();
            StartCoroutine(LoadClip(EnemySoundFileName, clip => _enemyClip = clip));
            StartCoroutine(LoadClip(FriendlySoundFileName, clip => _friendlyClip = clip));
        }

        // enemy.wav/friendly.wav ship as loose files next to the DLL (not embedded resources)
        // specifically so a player can drop in their own replacement under the same name.
        private IEnumerator LoadClip(string fileName, Action<AudioClip> onLoaded)
        {
            string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string path = Path.Combine(dllDir, fileName);
            if (!File.Exists(path))
            {
                Plugin.Log?.LogWarning($"[NOSDA] {fileName} not found next to the plugin DLL ({path}) — that sound is disabled.");
                yield break;
            }

            string url = "file:///" + path.Replace('\\', '/');
            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Plugin.Log?.LogWarning($"[NOSDA] Failed to load {fileName}: {request.error}");
                yield break;
            }
            onLoaded(DownloadHandlerAudioClip.GetContent(request));
        }

        // killerName is null for a crash (no shooter). isFriendly is whether the killed player
        // shares the local player's own faction — it picks the sound and banner color. deathCount
        // is that pilot's cumulative death count this session (DeathCounter.RecordDeath).
        internal void Announce(string playerName, int deathCount, string? killerName, bool isFriendly)
        {
            AudioClip? clip = isFriendly ? _friendlyClip : _enemyClip;
            if (clip != null) _audioSource.PlayOneShot(clip, SoundConfig.Volume);
            _banner.Show(playerName, deathCount, killerName, isFriendly);
        }
    }
}
