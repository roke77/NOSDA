using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace NOSDA
{
    // Owns the audio clip and the on-screen banner. Lives on a persistent worker GameObject
    // (see Plugin.OnSceneLoaded) rather than the BaseUnityPlugin itself so it survives the
    // boot -> MainMenu scene transition.
    internal class ShootdownAnnouncer : MonoBehaviour
    {
        private const string SoundFileName = "sound.wav";
        private const float BannerSeconds = 2.5f;

        private AudioSource _audioSource = null!;
        private AudioClip? _clip;
        private GameObject _bannerRoot = null!;
        private Text _bannerText = null!;
        private Text _killerText = null!;
        private Coroutine? _hideBanner;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; // 2D cue, not positional in the 3D scene
            BuildBanner();
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

        private void BuildBanner()
        {
            var canvasObj = new GameObject("NOSDA_Canvas");
            canvasObj.transform.SetParent(transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            _bannerRoot = new GameObject("NOSDA_BannerRoot");
            _bannerRoot.transform.SetParent(canvasObj.transform, false);

            _bannerText = MakeBannerText("NOSDA_BannerText", 72, FontStyle.Bold, Vector2.zero, new Vector2(1600, 150));
            // Sits just below the main line — its own anchored position, not a separate anchor point.
            _killerText = MakeBannerText("NOSDA_KillerText", 28, FontStyle.Normal, new Vector2(0, -90), new Vector2(1600, 60));

            _bannerRoot.SetActive(false);
        }

        private Text MakeBannerText(string name, int fontSize, FontStyle style, Vector2 anchoredPosition, Vector2 size)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(_bannerRoot.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.red;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.75f);
            rect.anchorMax = new Vector2(0.5f, 0.75f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        // killerName is null for a crash (no shooter) — the sub-line is omitted rather than left blank.
        internal void Announce(string playerName, string? killerName)
        {
            if (_clip != null) _audioSource.PlayOneShot(_clip);

            _bannerText.text = killerName != null ? $"{playerName} SHOT DOWN" : $"{playerName} CRASHED";
            _killerText.gameObject.SetActive(killerName != null);
            if (killerName != null) _killerText.text = $"by {killerName}";

            _bannerRoot.SetActive(true);
            if (_hideBanner != null) StopCoroutine(_hideBanner);
            _hideBanner = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(BannerSeconds);
            _bannerRoot.SetActive(false);
        }
    }
}
