using System;
using BepInEx.Configuration;

namespace NOSDA
{
    // ConfigurationManager (F1 menu) is a soft dependency: it reflects over ConfigDescription tags,
    // matching this class by exact type name and field name. Fields must match what it reads.
    // ponytail: only the fields we use are present (upstream has ~20); add more only as needed.
    internal sealed class ConfigurationManagerAttributes
    {
        // Replaces the default value widget with this draw callback — used for the test buttons
        // and the combined color swatch/hex/RGBA widget.
        public Action<ConfigEntryBase>? CustomDrawer;

        // false = hide the entry's row entirely — used for the G/B/A channel entries, whose
        // values are edited through the R entry's combined CustomDrawer instead.
        public bool? Browsable;
    }
}
