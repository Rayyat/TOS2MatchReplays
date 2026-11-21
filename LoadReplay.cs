using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BetterTOS2;
using Game;
using HarmonyLib;
using Server.Shared.Messages;
using UnityEngine;

namespace MatchReplaysPlus
{
    /// <summary>
    /// When a match starts and a replay has been queued up (via <see cref="MatchReplayUIController.JoinReplay"/>),
    /// reads the replay file from disk, deserializes its header and recorded messages,
    /// and spins up the <see cref="ReplayMatch"/> that will play them back.
    /// </summary>
    [HarmonyPatch(typeof(LocalClient), "Start")]
    public static class LoadReplay
    {
        public static LocalClient Local;

        public static void Postfix(LocalClient __instance)
        {
            Local = __instance;

            if (!ActiveReplayInfo.ReplayActive)
            {
                return;
            }

            byte[] fileBytes = File.ReadAllBytes(ActiveReplayInfo.ReplayFilePath);
            int replayInfoLength = BitConverter.ToInt32(fileBytes, 0);

            ReplayInfo replayInfo = new ReplayInfo();
            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    memoryStream.Write(fileBytes, 4, replayInfoLength);
                    memoryStream.Position = 0L;
                    replayInfo = (ReplayInfo)binaryFormatter.Deserialize(memoryStream);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Match ReplayInfo failed to load. Stack Trace:\n" + ex.Message);
            }

            if (replayInfo.BetterTOS2)
            {
                TryInitBetterTos2Data();
            }

            List<GameMessage> gameMessages = new List<GameMessage>();
            int offset = 8 + replayInfoLength;

            while (offset < fileBytes.Length)
            {
                GameMessage.MessageHeader header = GameMessage.PeekHeader(fileBytes, null, offset);
                if (header.length == 0)
                {
                    break;
                }

                byte[] messageBytes = new byte[header.length];
                Buffer.BlockCopy(fileBytes, offset, messageBytes, 0, header.length);
                offset += header.length;

                GameMessage message = MessageFactory.ReadMessage(messageBytes, null, out bool success, 0);
                if (success)
                {
                    gameMessages.Add(message);
                }
                else
                {
                    ModLogger.Log(message.MessageId);
                }
            }
            LoadReplay.Local.HandleMessage(new SendFullRoleDeckMessage(replayInfo.RoleList,replayInfo.Modifiers,replayInfo.Bans),LiteNetLib.DeliveryMethod.ReliableOrdered);
            ActiveReplayInfo.MatchReplayGO = new GameObject("MatchReplayer").AddComponent<ReplayMatch>();
            ActiveReplayInfo.MatchReplayGO.GameMessages = gameMessages;
            ActiveReplayInfo.ReplayInfo = replayInfo;
        }

        // NOTE: ConfigureModData (from BetterTOS2) is an optional dependency that may
        // not be installed. The call is split into its own method so a missing
        // BetterTOS2.dll only fails to JIT *that* method, not this one - see the
        // matching note on Main.IsBtos() for the full explanation.
        private static void TryInitBetterTos2Data()
        {
            try
            {
                InitBetterTos2Data();
            }
            catch
            {
                // Ignored - BetterTOS2 isn't installed/loaded.
            }
        }

        private static void InitBetterTos2Data()
        {
            ConfigureModData.InitModData();
        }
    }
}