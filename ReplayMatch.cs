using System;
using System.Collections;
using System.Collections.Generic;
using Home.Shared;
using Home.Shared.Enums;
using LiteNetLib;
using Server.Shared.Messages;
using Server.Shared.State;
using Services;
using SML;
using UnityEngine;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Drives playback of a loaded replay: feeds its recorded messages back through the
    /// local client on their original timing, and handles cleanup / returning to the
    /// home scene once playback ends or the replay is closed early.
    /// </summary>
    public class ReplayMatch : MonoBehaviour
    {
        public List<GameMessage> GameMessages = new List<GameMessage>();
        public float TimeScale;

        private Coroutine ReplayCoroutine;

        public void Start()
        {
            ReplayCoroutine = StartCoroutine(Replay());
        }

        public void Update()
        {
            Time.timeScale = TimeScale;
        }

        public void OnDestroy()
        {
            TimeScale = 1f;
            StopCoroutine(ReplayCoroutine);
            StartCoroutine(ReturnHome());
        }

        private IEnumerator Replay()
        {
            double lengthMinutes = Math.Round((GameMessages[GameMessages.Count - 1].Timestamp - GameMessages[0].Timestamp).TotalMinutes, 2);
            ModLogger.Log($"Replay started! Replay is {lengthMinutes} minutes long!");

            int expectedHash = ActiveReplayInfo.ReplayInfo.Hash;
            int actualHash = HashCodeHelper.GetHashCodeList(GameMessages);
            if (actualHash != expectedHash)
            {
                ModLogger.Log($"The replay file's hash ({expectedHash}) does not match the actual hash of the replay ({actualHash}). Replay is either outdated or has been tampered with!");
            }

            TimeScale = GetStartTimeScale();

            for (int i = 0; i < GameMessages.Count; i++)
            {
                LoadReplay.Local.HandleMessage(GameMessages[i], DeliveryMethod.ReliableOrdered);

                if (i + 1 >= GameMessages.Count)
                {
                    break;
                }

                if (!Pepper.IsLobbyPhase() && !Pepper.IsNonePhase())
                {
                    TimeSpan delay = GameMessages[i + 1].Timestamp - GameMessages[i].Timestamp;
                    yield return new WaitForSeconds((float)delay.TotalSeconds);
                }
            }

            ModLogger.Log("Replay Ended!");
            UnityEngine.Object.Destroy(gameObject);
        }

        private static float GetStartTimeScale()
        {
            string startSpeed = ModSettings.GetString("Replay Start Speed", "curtis.match.replays");
            switch (startSpeed)
            {
                case "Fast":
                    return 4f;
                case "Very Fast":
                    return 8f;
                case "Sonic Speed":
                    return 16f;
                default:
                    return 1f;
            }
        }

        private static IEnumerator ReturnHome()
        {
            ApplicationController.ApplicationContext.EnableCutoutTransitionOverlay(CutoutTransitionType.HOME, "");
            yield return new WaitForSeconds(1f);
            ApplicationController.ApplicationContext.EnableTransitionOverlay(true, false, "");

            if (Service.Home.NetworkService.VerifySocketConnection())
            {
                Service.Game.Network.Send(new RemovePlayerFromCellMessage(RemovedFromGameReason.EXIT_TO_MAIN_MENU, false));
                ApplicationController.ApplicationContext.navigationController.ShowScene(SceneType.HOME);
            }
        }
    }
}