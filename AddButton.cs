using System;
using SML;
using UnityEngine;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Adds the "Match Replays" button to the Salem Mod Loader menu, which opens the
    /// Match Replays UI when clicked.
    /// </summary>
    [Mod.SalemMenuItem]
    public static class AddButton
    {
        public static readonly Mod.SalemMenuButton Button = new Mod.SalemMenuButton
        {
            Label = "Match Replays",
            Icon = FromResources.LoadSprite("MatchReplaysPlus.Resources.Thumbnail.png", 100f, SpriteMeshType.Tight),
            OnClick = OnMenuButtonClicked
        };

        private static void OnMenuButtonClicked()
        {
            GameObject uiInstance = UnityEngine.Object.Instantiate(
                Main.Bundle.LoadAsset<GameObject>("MatchReplaysUI"),
                HomeScene.Controller.SafeArea.transform,
                false);

            uiInstance.transform.localPosition = Vector3.zero;
            uiInstance.transform.localScale = Vector3.one;
            uiInstance.transform.SetAsLastSibling();
            uiInstance.AddComponent<MatchReplayUIController>();
        }
    }
}