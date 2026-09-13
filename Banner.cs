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

        private Canvas _canvas = null!;
        private RectTransform _rootRect = null!;
        private GameObject _root = null!;
        private Text _nameText = null!;
        private Text _verbText = null!;
        private Text _killerText = null!;
        private Coroutine? _hideCoroutine;
        private bool _isFriendly;
        private bool _livePreviewShowing;

        internal void Build(Transform parent)
        {
            // typeof(RectTransform) is required here — a plain GameObject only gets a Transform,
            // which breaks anchor-based positioning for every UI child parented under it (anchors
            // interpolate against the immediate parent's RectTransform; with none, they collapse
            // to a single point regardless of the anchor value).
            var canvasObj = new GameObject("NOSDA_Canvas", typeof(RectTransform));
            canvasObj.transform.SetParent(parent, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // _root is sized to exactly fit its current content (in ApplyStyle) and anchored to
            // the canvas's bottom-left corner via its own pivot, so PositionHorizontal/
            // PositionVertical slide _root's own near edge from 0 (flush against the screen's
            // near edge) to 1 (flush against the far edge) — the banner is always fully on screen
            // at every slider value, rather than being anchored by a single point that pushes half
            // of it off-screen at the extremes (the previous approach's actual bug).
            _root = new GameObject("NOSDA_BannerRoot", typeof(RectTransform));
            _root.transform.SetParent(canvasObj.transform, false);
            _rootRect = (RectTransform)_root.transform;
            _rootRect.anchorMin = Vector2.zero;
            _rootRect.anchorMax = Vector2.zero;
            _rootRect.pivot = Vector2.zero;

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
            // Anchored to _root's own top-left corner, top-center pivot: ApplyStyle positions each
            // line by its cumulative height from the top of the stack, centered across the group's
            // own width — independent of _root's pivot/anchor, which only affects _root itself.
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(0f, 1f);
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            text.rectTransform.sizeDelta = LineSize;
            return text;
        }

        // Re-reads BannerConfig, sizes _root to fit whichever lines are visible, and positions it
        // so PositionHorizontal/PositionVertical slide it from the screen's near edge (0) to its
        // far edge (1) — the banner always stays fully on screen. Lines stack inside _root top to
        // bottom, each spaced below the one above by its own text height plus LineSpacing; a
        // hidden killer line leaves no gap behind it.
        private void ApplyStyle()
        {
            Color color = BannerConfig.GetTextColor(_isFriendly);
            float spacing = BannerConfig.LineSpacing;
            bool showKiller = _killerText.gameObject.activeSelf;

            _nameText.fontSize = BannerConfig.NameFontSize;
            _verbText.fontSize = BannerConfig.VerbFontSize;
            _killerText.fontSize = BannerConfig.KillerFontSize;
            _nameText.color = color;
            _verbText.color = color;
            _killerText.color = color;

            float groupWidth = Mathf.Max(_nameText.preferredWidth, _verbText.preferredWidth,
                showKiller ? _killerText.preferredWidth : 0f);
            float groupHeight = _nameText.preferredHeight + spacing + _verbText.preferredHeight;
            if (showKiller) groupHeight += spacing + _killerText.preferredHeight;

            float y = 0f;
            SetLinePosition(_nameText, groupWidth, y);
            y -= _nameText.preferredHeight + spacing;
            SetLinePosition(_verbText, groupWidth, y);
            if (showKiller)
            {
                y -= _verbText.preferredHeight + spacing;
                SetLinePosition(_killerText, groupWidth, y);
            }

            // Screen.width/height (raw pixels) divided by the canvas's scaleFactor gives the same
            // "content unit" system font sizes and preferredWidth/Height already use — the Canvas's
            // own RectTransform.rect isn't a reliable source for this: it renders full-screen via
            // Canvas's internal overlay handling regardless of its own declared anchors/size, but
            // that special-cased rendering doesn't mean .rect itself reports true screen dimensions.
            float canvasWidth = Screen.width / _canvas.scaleFactor;
            float canvasHeight = Screen.height / _canvas.scaleFactor;

            _rootRect.sizeDelta = new Vector2(groupWidth, groupHeight);
            float availableX = Mathf.Max(0f, canvasWidth - groupWidth);
            float availableY = Mathf.Max(0f, canvasHeight - groupHeight);
            _rootRect.anchoredPosition = new Vector2(
                BannerConfig.PositionHorizontal * availableX,
                BannerConfig.PositionVertical * availableY);
        }

        private static void SetLinePosition(Text text, float groupWidth, float y) =>
            text.rectTransform.anchoredPosition = new Vector2(groupWidth / 2f, y);

        // killerName is null for a crash (no shooter) — that line is hidden rather than left blank.
        // isFriendly picks which of BannerConfig's two color sets to use.
        internal void Show(string playerName, string? killerName, bool isFriendly)
        {
            _nameText.text = playerName;
            _verbText.text = killerName != null ? "SHOT DOWN" : "CRASHED";
            _killerText.gameObject.SetActive(killerName != null);
            if (killerName != null) _killerText.text = $"by {killerName}";
            _isFriendly = isFriendly;

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

        // Live Preview (F1 menu) keeps a fixed sample banner on screen and re-applies BannerConfig
        // every frame, so dragging a position/size/color slider shows the result immediately
        // instead of needing a fresh Test button click each time. While either toggle is on, it
        // overrides any real announcement's content and cancels its auto-hide — an accepted
        // trade-off for a tuning aid the player turns off when done.
        private void Update()
        {
            bool enemy = BannerConfig.LivePreviewEnemy;
            bool friendly = BannerConfig.LivePreviewFriendly;
            if (!enemy && !friendly)
            {
                if (_livePreviewShowing)
                {
                    _livePreviewShowing = false;
                    _root.SetActive(false);
                }
                return;
            }

            if (_hideCoroutine != null) { StopCoroutine(_hideCoroutine); _hideCoroutine = null; }
            _livePreviewShowing = true;

            _nameText.text = "PreviewPilot";
            _verbText.text = "SHOT DOWN";
            _killerText.gameObject.SetActive(true);
            _killerText.text = "by PreviewKiller";
            _isFriendly = friendly;

            ApplyStyle();
            _root.SetActive(true);
        }
    }
}
