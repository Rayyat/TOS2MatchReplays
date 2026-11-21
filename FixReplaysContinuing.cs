using System;
using HarmonyLib;
using Home.Common.Dialog;
using Home.Common.Settings;
using UnityEngine;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Ensures an active replay is torn down when the player backs out of the settings
    /// menu via "Return to Home", instead of continuing to run in the background.
    /// </summary>
    [HarmonyPatch(typeof(SettingsController), "OnReturnToHomeClicked")]
    public static class FixReplaysContinuing
    {
        public static void Postfix(SettingsController __instance)
        {
            DialogController dialogController = __instance._dialogController;
            dialogController.OnButtonActivated = (Action<int>)Delegate.Combine(
                dialogController.OnButtonActivated,
                new Action<int>(OnDialogButtonActivated));
        }

        private const int ReturnToHomeButtonIndex = 3;

        private static void OnDialogButtonActivated(int button)
        {
            if (ActiveReplayInfo.MatchReplayGO != null && button == ReturnToHomeButtonIndex)
            {
                UnityEngine.Object.Destroy(ActiveReplayInfo.MatchReplayGO.gameObject);
            }
        }
    }
}