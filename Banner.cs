using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NOSDA
{
    // On-screen shootdown/crash banner: player name, then SHOT DOWN/CRASHED, then the killer's
    // name if there is one. Size, color, spacing, and position come from BannerConfig (adjustable
    // via BepInEx's F1 Configuration Manager menu) — this file only lays the lines out.
    internal class Banner : MonoBehaviour
    {
        private const float VisibleSeconds = 2.5f;
        private static readonly Vector2 LineSize = new Vector2(1600, 200);

        private GameObject _root = null!;
        private Text _nameText = null!;
        private Text _verbText = null!;
        private Text _killerText = null!;
        private Coroutine? _hideCoroutine;

        internal void Build(Transform parent)
        {
            // typeof(RectTransform) is required here — a plain GameObject only gets a Transform,
            // which breaks anchor-based positioning for every UI child parented under it (anchors
            // interpolate against the immediate parent's RectTransform; with none, they collapse
            // to a single point regardless of the anchor value — this is why HorizontalPosition/
            // VerticalPosition previously did nothing).
            var canvasObj = new GameObject("NOSDA_Canvas", typeof(RectTransform));
            canvasObj.transform.SetParent(parent, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            _root = new GameObject("NOSDA_BannerRoot", typeof(RectTransform));
            _root.transform.SetParent(canvasObj.transform, false);

            _nameText = MakeText("NOSDA_NameText", FontStyle.Bold);
            _verbText = MakeText("NOSDA_VerbText", FontStyle.Bold);
            _killerText = MakeText("NOSDA_KillerText", FontStyle.Normal);

            ApplyStyle();
            _root.SetActive(false);
        }

        private Text MakeText(string name, FontStyle style)
        {
            var textObj = new GameObject(name, typeof(RectTransform));
            textObj.transform.SetParent(_root.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.rectTransform.sizeDelta = LineSize;
            return text;
        }

        // Re-reads BannerConfig and re-stacks the visible lines around the shared anchor point —
        // top line first, each next line spaced below the one above by its own text height plus
        // LineSpacing. A hidden killer line leaves no gap behind it.
        private void ApplyStyle()
        {
            Vector2 anchor = BannerConfig.AnchorPoint;
            Color color = BannerConfig.TextColor;
            float spacing = BannerConfig.LineSpacing;

            _nameText.fontSize = BannerConfig.NameFontSize;
            _verbText.fontSize = BannerConfig.VerbFontSize;
            _killerText.fontSize = BannerConfig.KillerFontSize;
            _nameText.color = color;
            _verbText.color = color;
            _killerText.color = color;

            float y = 0f;
            SetLinePosition(_nameText, anchor, y);
            y -= _nameText.preferredHeight / 2f + spacing + _verbText.preferredHeight / 2f;
            SetLinePosition(_verbText, anchor, y);
            if (_killerText.gameObject.activeSelf)
            {
                y -= _verbText.preferredHeight / 2f + spacing + _killerText.preferredHeight / 2f;
                SetLinePosition(_killerText, anchor, y);
            }
        }

        private static void SetLinePosition(Text text, Vector2 anchor, float y)
        {
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = new Vector2(0f, y);
        }

        // killerName is null for a crash (no shooter) — that line is hidden rather than left blank.
        internal void Show(string playerName, string? killerName)
        {
            _nameText.text = playerName;
            _verbText.text = killerName != null ? "SHOT DOWN" : "CRASHED";
            _killerText.gameObject.SetActive(killerName != null);
            if (killerName != null) _killerText.text = $"by {killerName}";

            ApplyStyle();

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
