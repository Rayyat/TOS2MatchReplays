using System;
using Game.Interface;
using HarmonyLib;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Prevents the player from chatting while a replay is active, since chat input
    /// during a replay would have no meaningful effect (and could confuse other systems
    /// that assume a live match).
    /// </summary>
    [HarmonyPatch(typeof(ChatInputController), "CanChatNow")]
    public static class DisableChat
    {
        public static bool Prefix(ref bool __result)
        {
            __result = !ActiveReplayInfo.ReplayActive;
            return __result;
        }
    }
}