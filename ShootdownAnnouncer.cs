using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace NOSDA
{
    // Owns the audio clip and delegates the on-screen banner to Banner. Lives on a persistent
    // worker GameObject (see Plugin.OnSceneLoaded) rather than the BaseUnityPlugin itself so it
    // survives the boot -> MainMenu scene transition.
    internal class ShootdownAnnouncer : MonoBehaviour
    {
        private const string SoundFileName = "sound.wav";

        private AudioSource _audioSource = null!;
        private AudioClip? _clip;
        private Banner _banner = null!;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 2D cue, not positional in the 3D scene
            _banner = gameObject.AddComponent<Banner>();
            _banner.Build(transform);
            StartCoroutine(LoadClip());
        }

        // sound.wav ships as a loose file next to the DLL (not an embedded resource) specifically
        // so a player can drop in their own replacement under the same name.
        private IEnumerator LoadClip()
        {
            string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string path = Path.Combine(dllDir, SoundFileName);
            if (!File.Exists(path))
            {
                Plugin.Log?.LogWarning($"[NOSDA] {SoundFileName} not found next to the plugin DLL ({path}) — shootdown sound disabled.");
                yield break;
            }

            string url = "file:///" + path.Replace('\\', '/');
            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Plugin.Log?.LogWarning($"[NOSDA] Failed to load {SoundFileName}: {request.error}");
                yield break;
            }
            _clip = DownloadHandlerAudioClip.GetContent(request);
        }

        // killerName is null for a crash (no shooter). isFriendly is whether the killed player
        // shares the local player's own faction — it picks which banner color to use.
        internal void Announce(string playerName, string? killerName, bool isFriendly)
        {
            if (_clip != null) _audioSource.PlayOneShot(_clip);
            _banner.Show(playerName, killerName, isFriendly);
        }
    }
}
