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
        private static ConfigEntry<float>? _colorR;
        private static ConfigEntry<float>? _colorG;
        private static ConfigEntry<float>? _colorB;
        private static ConfigEntry<float>? _colorA;
        private static ConfigEntry<float>? _horizontalPosition;
        private static ConfigEntry<float>? _verticalPosition;

        public static int NameFontSize => _nameFontSize?.Value ?? 56;
        public static int VerbFontSize => _verbFontSize?.Value ?? 72;
        public static int KillerFontSize => _killerFontSize?.Value ?? 28;

        // Gap between adjacent lines, in canvas units, on top of each line's own text height.
        public static float LineSpacing => _lineSpacing?.Value ?? 12f;

        public static Color TextColor => new Color(
            _colorR?.Value ?? 1f, _colorG?.Value ?? 0f, _colorB?.Value ?? 0f, _colorA?.Value ?? 1f);

        // Screen anchor: (0,0) bottom-left, (1,1) top-right.
        public static Vector2 AnchorPoint => new Vector2(_horizontalPosition?.Value ?? 0.5f, _verticalPosition?.Value ?? 0.75f);

        public static void Bind(ConfigFile config)
        {
            const string section = "Banner";

            // Dummy bool entries — CustomDrawer replaces their checkbox with a button, so the
            // value itself is never read. Bound first so the buttons sit above the settings
            // they're meant to preview.
            config.Bind(section, "Test Shot Down", false, new ConfigDescription(
                "Preview the banner as a shootdown, using the current settings below.", null,
                new ConfigurationManagerAttributes { CustomDrawer = e => DrawTestButton(e, "Preview: Shot Down", "TestPilot", "TestKiller") }));
            config.Bind(section, "Test Crash", false, new ConfigDescription(
                "Preview the banner as a crash (no killer line), using the current settings below.", null,
                new ConfigurationManagerAttributes { CustomDrawer = e => DrawTestButton(e, "Preview: Crash", "TestPilot", null) }));

            _nameFontSize = config.Bind(section, "NameFontSize", 56,
                new ConfigDescription("Font size of the player-name line.", new AcceptableValueRange<int>(20, 150)));
            _verbFontSize = config.Bind(section, "VerbFontSize", 72,
                new ConfigDescription("Font size of the SHOT DOWN / CRASHED line.", new AcceptableValueRange<int>(20, 150)));
            _killerFontSize = config.Bind(section, "KillerFontSize", 28,
                new ConfigDescription("Font size of the smaller 'by <killer>' line.", new AcceptableValueRange<int>(10, 80)));
            _lineSpacing = config.Bind(section, "LineSpacing", 12f,
                new ConfigDescription("Gap between lines, in canvas units, on top of each line's own text height.", new AcceptableValueRange<float>(0f, 100f)));

            // Only ColorRed gets a CustomDrawer (the combined swatch/hex/RGBA widget below); the
            // other 3 channels are hidden from their own rows since that widget edits all four.
            _colorR = config.Bind(section, "ColorRed", 1f,
                new ConfigDescription("Text color.", new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { CustomDrawer = DrawColorPicker }));
            _colorG = config.Bind(section, "ColorGreen", 0f,
                new ConfigDescription("Text color, green channel.", new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Browsable = false }));
            _colorB = config.Bind(section, "ColorBlue", 0f,
                new ConfigDescription("Text color, blue channel.", new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Browsable = false }));
            _colorA = config.Bind(section, "ColorAlpha", 1f,
                new ConfigDescription("Text color, alpha (opacity) channel.", new AcceptableValueRange<float>(0f, 1f),
                    new ConfigurationManagerAttributes { Browsable = false }));

            _horizontalPosition = config.Bind(section, "HorizontalPosition", 0.5f,
                new ConfigDescription("Horizontal position on screen: 0 = left edge, 1 = right edge.", new AcceptableValueRange<float>(0f, 1f)));
            _verticalPosition = config.Bind(section, "VerticalPosition", 0.75f,
                new ConfigDescription("Vertical position on screen: 0 = bottom edge, 1 = top edge.", new AcceptableValueRange<float>(0f, 1f)));
        }

        private static void DrawTestButton(ConfigEntryBase _, string label, string playerName, string? killerName)
        {
            if (GUILayout.Button(label)) Plugin.Announcer?.Announce(playerName, killerName);
        }

        // The hex field syncs from the live RGBA values except while it has keyboard focus, so an
        // in-progress edit isn't overwritten mid-keystroke by the next OnGUI pass.
        private const string HexControlName = "NOSDA_ColorHex";
        private static string _hexInput = "";
        private static Texture2D? _swatch;

        private static void DrawColorPicker(ConfigEntryBase _)
        {
            Color color = TextColor;

            _swatch ??= new Texture2D(1, 1);
            _swatch.SetPixel(0, 0, color);
            _swatch.Apply();

            GUILayout.BeginHorizontal();
            GUILayout.Label(_swatch, GUILayout.Width(32), GUILayout.Height(20));

            GUI.SetNextControlName(HexControlName);
            bool hexFocused = GUI.GetNameOfFocusedControl() == HexControlName;
            if (!hexFocused) _hexInput = ColorToHex(color);
            string typed = GUILayout.TextField(_hexInput, GUILayout.Width(90));
            if (typed != _hexInput)
            {
                _hexInput = typed;
                if (TryParseHex(typed, out Color parsed)) SetColor(parsed);
            }
            GUILayout.EndHorizontal();

            DrawChannelSlider("R", _colorR);
            DrawChannelSlider("G", _colorG);
            DrawChannelSlider("B", _colorB);
            DrawChannelSlider("A", _colorA);
        }

        private static void DrawChannelSlider(string label, ConfigEntry<float>? entry)
        {
            if (entry == null) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(14));
            entry.Value = GUILayout.HorizontalSlider(entry.Value, 0f, 1f, GUILayout.Width(150));
            GUILayout.Label(entry.Value.ToString("0.00", CultureInfo.InvariantCulture), GUILayout.Width(36));
            GUILayout.EndHorizontal();
        }

        private static void SetColor(Color c)
        {
            if (_colorR != null) _colorR.Value = c.r;
            if (_colorG != null) _colorG.Value = c.g;
            if (_colorB != null) _colorB.Value = c.b;
            if (_colorA != null) _colorA.Value = c.a;
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
