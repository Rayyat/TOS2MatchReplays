using System;
using HarmonyLib;
using Home.Services;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Registers the localization strings used by the mod's dock buttons
    /// (see <see cref="SetupNewItems"/>).
    /// </summary>
    [HarmonyPatch(typeof(HomeLocalizationService), "RebuildStringTables")]
    public static class StringTable
    {
        public static void Postfix(HomeLocalizationService __instance)
        {
            __instance.stringTable_.Add("REPLAY_PAUSE", "Pause");
            __instance.stringTable_.Add("REPLAY_REVEAL", "Reveal Roles");
            __instance.stringTable_.Add("REPLAY_TIME", "Change Speed");
        }
    }
}