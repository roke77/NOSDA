using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NOSDA
{
    // On-screen shootdown/crash banner: a bold main line, plus a smaller "by <killer>" sub-line
    // shown only when there's a shooter. Size, color, and position come from BannerConfig
    // (adjustable live via BepInEx's F1 Configuration Manager menu) — this file only lays out
    // and re-styles the two lines.
    internal class Banner : MonoBehaviour
    {
        private static readonly Vector2 MainSize = new Vector2(1600, 150);
        private static readonly Vector2 KillerSize = new Vector2(1600, 60);
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

            _mainText = MakeText("NOSDA_BannerText", FontStyle.Bold, MainSize);
            _killerText = MakeText("NOSDA_KillerText", FontStyle.Normal, KillerSize);

            ApplyStyle();
            _root.SetActive(false);
        }

        private Text MakeText(string name, FontStyle style, Vector2 size)
        {
            var textObj = new GameObject(name);
            textObj.transform.SetParent(_root.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            // Config lets font size grow well past what a fixed box would fit — never clip it.
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.rectTransform.sizeDelta = size;
            return text;
        }

        // Re-reads BannerConfig so an F1-menu edit takes effect on the next announcement.
        private void ApplyStyle()
        {
            Vector2 anchor = BannerConfig.AnchorPoint;
            Color color = BannerConfig.TextColor;

            Restyle(_mainText, BannerConfig.MainFontSize, anchor, Vector2.zero, color);
            Restyle(_killerText, BannerConfig.KillerFontSize, anchor, BannerConfig.KillerOffset, color);
        }

        private static void Restyle(Text text, int fontSize, Vector2 anchor, Vector2 anchoredPosition, Color color)
        {
            text.fontSize = fontSize;
            text.color = color;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = anchoredPosition;
        }

        // killerName is null for a crash (no shooter) — the sub-line is omitted rather than left blank.
        internal void Show(string playerName, string? killerName)
        {
            ApplyStyle();

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
