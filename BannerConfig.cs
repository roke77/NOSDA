using BepInEx.Configuration;
using UnityEngine;

namespace NOSDA
{
    // Banner size, position, and color — adjustable live via BepInEx's F1 Configuration Manager
    // menu. Color is exposed as separate R/G/B sliders rather than a single Color entry: it's what
    // ConfigurationManager can actually render as sliders, since it has no color-picker widget.
    internal static class BannerConfig
    {
        private static ConfigEntry<int>? _mainFontSize;
        private static ConfigEntry<int>? _killerFontSize;
        private static ConfigEntry<float>? _colorR;
        private static ConfigEntry<float>? _colorG;
        private static ConfigEntry<float>? _colorB;
        private static ConfigEntry<float>? _horizontalPosition;
        private static ConfigEntry<float>? _verticalPosition;
        private static ConfigEntry<float>? _killerLineOffset;

        public static int MainFontSize => _mainFontSize?.Value ?? 72;
        public static int KillerFontSize => _killerFontSize?.Value ?? 28;
        public static Color TextColor => new Color(_colorR?.Value ?? 1f, _colorG?.Value ?? 0f, _colorB?.Value ?? 0f);

        // Screen anchor: (0,0) bottom-left, (1,1) top-right.
        public static Vector2 AnchorPoint => new Vector2(_horizontalPosition?.Value ?? 0.5f, _verticalPosition?.Value ?? 0.75f);

        // Killer sub-line's offset from the main line, in canvas units — negative moves it down.
        public static Vector2 KillerOffset => new Vector2(0f, _killerLineOffset?.Value ?? -90f);

        public static void Bind(ConfigFile config)
        {
            const string section = "Banner";

            _mainFontSize = config.Bind(section, "MainFontSize", 72,
                new ConfigDescription("Font size of the main SHOT DOWN / CRASHED line.", new AcceptableValueRange<int>(20, 150)));
            _killerFontSize = config.Bind(section, "KillerFontSize", 28,
                new ConfigDescription("Font size of the smaller 'by <killer>' line.", new AcceptableValueRange<int>(10, 80)));

            _colorR = config.Bind(section, "ColorRed", 1f,
                new ConfigDescription("Text color, red channel.", new AcceptableValueRange<float>(0f, 1f)));
            _colorG = config.Bind(section, "ColorGreen", 0f,
                new ConfigDescription("Text color, green channel.", new AcceptableValueRange<float>(0f, 1f)));
            _colorB = config.Bind(section, "ColorBlue", 0f,
                new ConfigDescription("Text color, blue channel.", new AcceptableValueRange<float>(0f, 1f)));

            _horizontalPosition = config.Bind(section, "HorizontalPosition", 0.5f,
                new ConfigDescription("Horizontal position on screen: 0 = left edge, 1 = right edge.", new AcceptableValueRange<float>(0f, 1f)));
            _verticalPosition = config.Bind(section, "VerticalPosition", 0.75f,
                new ConfigDescription("Vertical position on screen: 0 = bottom edge, 1 = top edge.", new AcceptableValueRange<float>(0f, 1f)));
            _killerLineOffset = config.Bind(section, "KillerLineOffset", -90f,
                new ConfigDescription("Vertical offset of the 'by <killer>' line from the main line, in canvas units. Negative moves it down.", new AcceptableValueRange<float>(-400f, 0f)));
        }
    }
}
