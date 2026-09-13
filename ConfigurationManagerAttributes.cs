using System;
using BepInEx.Configuration;

namespace NOSDA
{
    // ConfigurationManager (F1 menu) is a soft dependency: it reflects over ConfigDescription tags,
    // matching this class by exact type name and field name. Fields must match what it reads.
    // ponytail: only the field we use is present (upstream has ~20); add more only as needed.
    internal sealed class ConfigurationManagerAttributes
    {
        // Replaces the default value widget with this draw callback — used for the test buttons.
        public Action<ConfigEntryBase>? CustomDrawer;
    }
}
