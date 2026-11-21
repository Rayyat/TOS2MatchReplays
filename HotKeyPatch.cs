using System;
using HarmonyLib;
using Home.Services;
using UnityEngine;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Registers the mod's three custom hotkeys (pause, change speed, reveal roles)
    /// as default hotkeys, and makes sure the settings UI knows about them.
    /// </summary>
    [HarmonyPatch(typeof(InputService))]
    public static class HotKeyPatch
    {
        [HarmonyPatch("UseDefaultHotkeys")]
        public static void Postfix(InputService __instance)
        {
            __instance.HotKeys.Add((InputService.HotKey.HotKeyType)100, new InputService.HotKey((InputService.HotKey.HotKeyType)100, KeyCode.Space, true));
            __instance.HotKeys.Add((InputService.HotKey.HotKeyType)101, new InputService.HotKey((InputService.HotKey.HotKeyType)101, KeyCode.Equals, true));
            __instance.HotKeys.Add((InputService.HotKey.HotKeyType)102, new InputService.HotKey((InputService.HotKey.HotKeyType)102, KeyCode.Minus, true));
        }

        [HarmonyPatch("GetCountOfDefaultHotKeys")]
        public static void Postfix(ref int __result)
        {
            __result += 3;
        }
    }
}