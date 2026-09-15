using BepInEx.Configuration;

namespace NOSDA
{
    // Sound playback volume — adjustable live via BepInEx's F1 Configuration Manager menu.
    internal static class SoundConfig
    {
        private static ConfigEntry<float>? _volume;

        public static float Volume => _volume?.Value ?? 1f;

        public static void Bind(ConfigFile config)
        {
            _volume = config.Bind("Sound", "Volume", 1f,
                new ConfigDescription("Playback volume for the shootdown/crash sound.", new AcceptableValueRange<float>(0f, 1f)));
        }
    }
}
