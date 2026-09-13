using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NOSDA
{
    // On-screen shootdown/crash banner: a bold main line, plus a smaller "by <killer>" sub-line
    // shown only when there's a shooter. Tweak the constants below for size, color, or position.
    internal class Banner : MonoBehaviour
    {
        private const int MainFontSize = 72;
        private const int KillerFontSize = 28;
        private static readonly Color TextColor = Color.red;
        private static readonly Vector2 AnchorPoint = new Vector2(0.5f, 0.75f); // 50% across, 75% up the screen
        private static readonly Vector2 MainSize = new Vector2(1600, 150);
        private static readonly Vector2 KillerSize = new Vector2(1600, 60);
        private static readonly Vector2 KillerOffset = new Vector2(0, -90); // below the main line
        private const float VisibleSeconds = 2.5f;

        private GameObject _root = null!;
        private Text _mainText = null!;
        private Text _killerText = null!;
        private Coroutine? _hideCoroutine;

        internal void Build(Transform parent)
        {
            var canvasObj = new GameObject("NOSDA_Canvas");
            canvasObj.transform.SetParent(parent, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            _root = new GameObject("NOSDA_BannerRoot");
            _root.transform.SetParent(canvasObj.transform, false);

            _mainText = MakeText("NOSDA_BannerText", MainFontSize, FontStyle.Bold, Vector2.zero, MainSize);
            _killerText = MakeText("NOSDA_KillerText", KillerFontSize, FontStyle.Normal, KillerOffset, KillerSize);

            _root.SetActive(false);
        }

        private Text MakeText(string name, int fontSize, FontStyle style, Vector2 anchoredPosition, Vector2 size)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(_root.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = AnchorPoint;
            rect.anchorMax = AnchorPoint;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        // killerName is null for a crash (no shooter) — the sub-line is omitted rather than left blank.
        internal void Show(string playerName, string? killerName)
        {
            _mainText.text = killerName != null ? $"{playerName} SHOT DOWN" : $"{playerName} CRASHED";
            _killerText.gameObject.SetActive(killerName != null);
            if (killerName != null) _killerText.text = $"by {killerName}";

            _root.SetActive(true);
            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(VisibleSeconds);
            _root.SetActive(false);
        }
    }
}
