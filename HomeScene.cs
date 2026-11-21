using System;
using HarmonyLib;
using Home.HomeScene;
using Server.Shared.State;
using UnityEngine;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Caches a reference to the <see cref="HomeSceneController"/> and resets all
    /// replay/message state whenever the home scene loads (e.g. after leaving a match).
    /// </summary>
    [HarmonyPatch(typeof(HomeSceneController), "Start")]
    public static class HomeScene
    {
        public static HomeSceneController Controller;

        public static void Prefix(HomeSceneController __instance)
        {
            Controller = __instance;
            Time.timeScale = 1f;
            ActiveReplayInfo.ReplayActive = false;
            MessageHandler.MyFaction = FactionType.NONE;
            MessageHandler.GameMessages.Clear();
        }
    }
}