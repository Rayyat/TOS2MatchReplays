using System;
using System.Collections.Generic;
using HarmonyLib;
using Home.Common.Settings;
using Home.Services;
using Services;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Gives the mod's custom hotkeys (see <see cref="HotKeyTypePlus"/>) a proper label
    /// in the hotkey rebinding list, instead of falling back to the game's default
    /// (and incorrect) handling for unknown hotkey types.
    /// </summary>
    [HarmonyPatch(typeof(HotKeyListItem), "SetData")]
    public static class HotKeyNamePatch
    {
        public static bool Prefix(HotKeyListItem __instance, KeyValuePair<InputService.HotKey.HotKeyType, InputService.HotKey> hotKeyType)
        {
            __instance.type = hotKeyType.Key;

            if (!HotKeyTypePlus.HotKeyTypePlusLookup.TryGetValue((int)__instance.type, out string label))
            {
                // Not one of our custom hotkeys - let the original method run.
                return true;
            }

            __instance.hotKeyLabel.SetText(label);
            __instance.hotKeyButtonText.SetText(Service.Home.InputService.GetKeyLabel(__instance.type, false));
            __instance.hotKeyButton.gameObject.SetActive(true);
            __instance.nonConfigHotKeyLabel.gameObject.SetActive(false);
            __instance.ImageLock.SetActive(false);

            // We've handled it ourselves - skip the original method.
            return false;
        }
    }
}