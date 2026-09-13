using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;

namespace NOSDA
{
    // Banner size, spacing, position, and color — adjustable live via BepInEx's F1 Configuration
    // Manager menu.
    internal static class BannerConfig
    {
        private static ConfigEntry<int>? _nameFontSize;
        private static ConfigEntry<int>? _verbFontSize;
        private static ConfigEntry<int>? _killerFontSize;
        private static ConfigEntry<float>? _lineSpacing;
        private static ConfigEntry<float>? _horizontalPosition;
        private static ConfigEntry<float>? _verticalPosition;
        private static ConfigEntry<bool>? _livePreviewEnemy;
        private static ConfigEntry<bool>? _livePreviewFriendly;

        private static readonly ColorSet EnemyColor = new ColorSet("NOSDA_EnemyColorHex");
        private static readonly ColorSet FriendlyColor = new ColorSet("NOSDA_FriendlyColorHex");

        public static int NameFontSize => _nameFontSize?.Value ?? 20;
        public static int VerbFontSize => _verbFontSize?.Value ?? 20;
        public static int KillerFontSize => _killerFontSize?.Value ?? 10;

        // Gap between adjacent lines, in canvas units, on top of each line's own text height.
        public static float LineSpacing => _lineSpacing?.Value ?? 0f;

        // isFriendly = the killed player shares the local player's own faction.
        public static Color GetTextColor(bool isFriendly) => (isFriendly ? FriendlyColor : EnemyColor).Value;

        // 0 = banner's near edge flush with the screen's near edge, 1 = flush with the far edge.
        public static float PositionHorizontal => _horizontalPosition?.Value ?? 0.5f;
        public static float PositionVertical => _verticalPosition?.Value ?? 1f;

        // A persistent sample banner Banner.Update() keeps showing while true, so slider edits
        // are visible immediately. Mutually exclusive with each other (see Bind).
        public static bool LivePreviewEnemy => _livePreviewEnemy?.Value ?? false;
        public static bool LivePreviewFriendly => _livePreviewFriendly?.Value ?? false;

        public static void Bind(ConfigFile config)
        {
            const string section = "Banner";

            // Dummy bool entries — CustomDrawer replaces their checkbox with a button, so the
            // value itself is never read. Bound first so the buttons sit above the settings
            // they're meant to preview.
            config.Bind(section, "Test Enemy Shot Down", false, new ConfigDescription(
                "Preview an enemy shootdown, using the current settings below.", null,
                new ConfigurationManagerAttributes { CustomDrawer = e => DrawTestButton(e, "Preview: Enemy Shot Down", "TestPilot", "TestKiller", false) }));
            config.Bind(section, "Test Enemy Crash", false, new ConfigDescription(
                "Preview an enemy crash (no killer line), using the current settings below.", null,
                new ConfigurationManagerAttributes { CustomDrawer = e => DrawTestButton(e, "Preview: Enemy Crash", "TestPilot", null, false) }));
            config.Bind(section, "Test Friendly Shot Down", false, new ConfigDescription(
                "Preview a friendly shootdown, using the current settings below.", null,
                new ConfigurationManagerAttributes { CustomDrawer = e => DrawTestButton(e, "Preview: Friendly Shot Down", "TestPilot", "TestKiller", true) }));
            config.Bind(section, "Test Friendly Crash", false, new ConfigDescription(
                "Preview a friendly crash (no killer line), using the current settings below.", null,
                new ConfigurationManagerAttributes { CustomDrawer = e => DrawTestButton(e, "Preview: Friendly Crash", "TestPilot", null, true) }));

            // Unlike the Test buttons above (fire once, auto-hide after 2.5s), these keep a sample
            // banner on screen continuously so position/size/color edits are visible immediately
            // while dragging a slider. The two are mutually exclusive — enabling one disables the
            // other, since both would render on top of each other at the same position.
            _livePreviewEnemy = config.Bind(section, "Live Preview: Enemy", false,
                new ConfigDescription("Keep an enemy-colored sample banner on screen continuously, to preview edits live."));
            _livePreviewFriendly = config.Bind(section, "Live Preview: Friendly", false,
                new ConfigDescription("Keep a friendly-colored sample banner on screen continuously, to preview edits live."));
            _livePreviewEnemy.SettingChanged += (_, _) => { if (_livePreviewEnemy.Value) _livePreviewFriendly.Value = false; };
            _livePreviewFriendly.SettingChanged += (_, _) => { if (_livePreviewFriendly.Value) _livePreviewEnemy.Value = false; };

            // "TextSize*" keys keep the 3 font sizes adjacent in the F1 menu (sorted alphabetically
            // by key), with "TextSpacing" sorting right after them.
            _nameFontSize = config.Bind(section, "TextSizeName", 20,
                new ConfigDescription("Font size of the player-name line.", new AcceptableValueRange<int>(8, 150)));
            _verbFontSize = config.Bind(section, "TextSizeVerb", 20,
                new ConfigDescription("Font size of the SHOT DOWN / CRASHED line.", new AcceptableValueRange<int>(8, 150)));
            _killerFontSize = config.Bind(section, "TextSizeKiller", 10,
                new ConfigDescription("Font size of the smaller 'by <killer>' line.", new AcceptableValueRange<int>(8, 80)));
            _lineSpacing = config.Bind(section, "TextSpacing", 0f,
                new ConfigDescription("Gap between lines, in canvas units, on top of each line's own text height.", new AcceptableValueRange<float>(0f, 100f)));

            EnemyColor.Bind(config, section, "EnemyColor", new Color(1f, 0f, 0f));
            FriendlyColor.Bind(config, section, "FriendlyColor", new Color(0f, 0f, 1f));

            // Named to sort adjacently in the F1 menu, which lists entries alphabetically by key —
            // "HorizontalPosition"/"VerticalPosition" landed far apart under that sort.
            _horizontalPosition = config.Bind(section, "PositionHorizontal", 0.5f,
                new ConfigDescription("Horizontal position: 0 = flush against the screen's left edge, 1 = flush against the right edge. The banner is always fully on screen.", new AcceptableValueRange<float>(0f, 1f)));
            _verticalPosition = config.Bind(section, "PositionVertical", 1f,
                new ConfigDescription("Vertical position: 0 = flush against the screen's bottom edge, 1 = flush against the top edge. The banner is always fully on screen.", new AcceptableValueRange<float>(0f, 1f)));
        }

        private static void DrawTestButton(ConfigEntryBase _, string label, string playerName, string? killerName, bool isFriendly)
        {
            if (GUILayout.Button(label)) Plugin.Announcer?.Announce(playerName, killerName, isFriendly);
        }

        // Four float channels plus the combined swatch/hex/RGBA CustomDrawer widget that edits
        // them together — bundled per color so Enemy and Friendly can each have their own
        // independent hex-field text-entry state without colliding (GUI.SetNextControlName needs
        // a name unique per on-screen control).
        private sealed class ColorSet
        {
            private readonly string _hexControlName;
            private string _hexInput = "";
            private ConfigEntry<float>? _r, _g, _b, _a;

            public ColorSet(string hexControlName) => _hexControlName = hexControlName;

            public Color Value => new Color(_r?.Value ?? 1f, _g?.Value ?? 0f, _b?.Value ?? 0f, _a?.Value ?? 1f);

            public void Bind(ConfigFile config, string section, string keyPrefix, Color defaultColor)
            {
                // keyPrefix itself (no channel suffix) carries the CustomDrawer — its row label is
                // what the F1 menu shows, and "EnemyColorRed" read as if the widget only covered
                // the red channel. Green/Blue/Alpha are separate entries, hidden from their own
                // rows since that widget edits all four together.
                _r = config.Bind(section, keyPrefix, defaultColor.r,
                    new ConfigDescription($"{keyPrefix.Replace("Color", "")} kill text color.", new AcceptableValueRange<float>(0f, 1f),
                        new ConfigurationManagerAttributes { CustomDrawer = _ => Draw() }));
                _g = config.Bind(section, $"{keyPrefix}Green", defaultColor.g,
                    new ConfigDescription("Green channel.", new AcceptableValueRange<float>(0f, 1f),
                        new ConfigurationManagerAttributes { Browsable = false }));
                _b = config.Bind(section, $"{keyPrefix}Blue", defaultColor.b,
                    new ConfigDescription("Blue channel.", new AcceptableValueRange<float>(0f, 1f),
                        new ConfigurationManagerAttributes { Browsable = false }));
                _a = config.Bind(section, $"{keyPrefix}Alpha", defaultColor.a,
                    new ConfigDescription("Alpha (opacity) channel.", new AcceptableValueRange<float>(0f, 1f),
                        new ConfigurationManagerAttributes { Browsable = false }));
            }

            private void Set(Color c)
            {
                if (_r != null) _r.Value = c.r;
                if (_g != null) _g.Value = c.g;
                if (_b != null) _b.Value = c.b;
                if (_a != null) _a.Value = c.a;
            }

            private void Draw()
            {
                Color color = Value;
                _swatch ??= new Texture2D(1, 1);
                _swatch.SetPixel(0, 0, color);
                _swatch.Apply();

                // ConfigurationManager calls CustomDrawer from inside its own row's
                // BeginHorizontal — without this wrapper, each row below is a horizontal sibling
                // of that row instead of stacking underneath it, and everything spills out
                // sideways instead of downward.
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label(_swatch, GUILayout.Width(32), GUILayout.Height(20));

                GUI.SetNextControlName(_hexControlName);
                bool hexFocused = GUI.GetNameOfFocusedControl() == _hexControlName;
                if (!hexFocused) _hexInput = ColorToHex(color);
                string typed = GUILayout.TextField(_hexInput, GUILayout.Width(90));
                if (typed != _hexInput)
                {
                    _hexInput = typed;
                    if (TryParseHex(typed, out Color parsed)) Set(parsed);
                }
                GUILayout.EndHorizontal();

                DrawChannelSlider("R", _r);
                DrawChannelSlider("G", _g);
                DrawChannelSlider("B", _b);
                DrawChannelSlider("A", _a);

                GUILayout.EndVertical();
            }
        }

        // Shared 1x1 swatch texture — safe to share between both ColorSets since each Draw() call
        // sets its pixel and reads it back synchronously, never deferred across OnGUI passes.
        private static Texture2D? _swatch;

        private static void DrawChannelSlider(string label, ConfigEntry<float>? entry)
        {
            if (entry == null) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(14));
            entry.Value = GUILayout.HorizontalSlider(entry.Value, 0f, 1f, GUILayout.Width(120));
            GUILayout.Label(entry.Value.ToString("0.00", CultureInfo.InvariantCulture), GUILayout.Width(36));
            GUILayout.EndHorizontal();
        }

        private static string ColorToHex(Color c) =>
            $"{ToByte(c.r):X2}{ToByte(c.g):X2}{ToByte(c.b):X2}{ToByte(c.a):X2}";

        private static byte ToByte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

        private static bool TryParseHex(string hex, out Color color)
        {
            color = default;
            hex = hex.TrimStart('#');
            if (hex.Length != 6 && hex.Length != 8) return false;
            if (!TryParseByte(hex, 0, out byte r)) return false;
            if (!TryParseByte(hex, 2, out byte g)) return false;
            if (!TryParseByte(hex, 4, out byte b)) return false;
            byte a = 255;
            if (hex.Length == 8 && !TryParseByte(hex, 6, out a)) return false;
            color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            return true;
        }

        private static bool TryParseByte(string hex, int offset, out byte value) =>
            byte.TryParse(hex.Substring(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }
}
