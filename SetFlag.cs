using System;
using Game.Interface;
using HarmonyLib;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Marks that a real (non-replay) game is now active, so <see cref="SaveAsReplayFile"/>
    /// knows the game that just ended should be saved as a replay.
    /// </summary>
    [HarmonyPatch(typeof(PickNamesPanel), "Start")]
    public static class SetFlag
    {
        public static bool GameActive;

        public static void Postfix()
        {
            GameActive = true;
        }
    }
}