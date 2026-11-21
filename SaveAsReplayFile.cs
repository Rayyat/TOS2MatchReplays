using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Game.Interface;
using HarmonyLib;
using Server.Shared.Collections;
using Server.Shared.Extensions;
using Server.Shared.Messages;
using Server.Shared.State;
using Services;
using SML;
using UnityEngine;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Once a match's "previous game" info panel updates (i.e. the match just ended),
    /// writes everything recorded by <see cref="MessageHandler"/> out to a .replay file.
    /// </summary>
    [HarmonyPatch(typeof(HudLobbyPreviousGamePanel), "OnPreviousGameInfoChanged")]
    public static class SaveAsReplayFile
    {
        public static void Postfix()
        {
            if (!SetFlag.GameActive)
            {
                return;
            }

            SetFlag.GameActive = false;

            if (ActiveReplayInfo.ReplayActive)
            {
                // Don't save a "replay of a replay".
                return;
            }

            using (MemoryStream memoryStream = new MemoryStream())
            using (WriteBuffer writeBuffer = new WriteBuffer(memoryStream))
            {
                ReplayInfo replayInfo = GetReplayInfo(Service.Game.Sim.info.previousGameInfo.Data,Service.Game.Sim.simulation.roleDeckBuilder.Data);
                byte[] replayInfoBytes = GetReplayInfoBytes(replayInfo);

                writeBuffer.Write(replayInfoBytes.Length);
                writeBuffer.Write(replayInfoBytes);
                writeBuffer.Write(MessageHandler.GameMessages.Count);

                try
                {
                    MessageHandler.GameMessages.ForEach(gameMessage => writeBuffer.Write(gameMessage.Write()));
                }
                catch (Exception ex)
                {
                    ModLogger.Log(ex.Message);
                }

                byte[] bytes = memoryStream.ToArray();
                DateTime now = DateTime.Now;

                string description = Service.Game.Sim.info.lobby.Data.description;
                string fileName = !string.IsNullOrWhiteSpace(description)
                    ? string.Format("{0}#{1:yyyy-MM-dd-HH-mm-ss}.replay", description, now)
                    : string.Format("{0}_{1:yyyy-MM-dd-HH-mm-ss}.replay", Pepper.GetCurrentGameMode().gameType, now);

                string path = Path.Combine(Main.FolderPath, fileName);
                File.WriteAllBytes(path, bytes);

                MessageHandler.GameMessages.Clear();
                MessageHandler.MyFaction = FactionType.NONE;
            }
        }

        public static byte[] GetReplayInfoBytes(ReplayInfo replayInfo)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, replayInfo);
                return memoryStream.ToArray();
            }
        }

        // NOTE: the parameter here is the game's own Server.Shared.State.PreviousGameData
        // type (the raw "previous game" panel data), not this mod's MatchReplaysPlus.PreviousGameData
        // (which only stores the subset of that data - role/modifier/faction - needed to
        // support "Reveal Roles" during playback). Both are named PreviousGameData, so the
        // game's type must be fully qualified here or C# would silently resolve the bare
        // name to this mod's own type instead (which doesn't have winningFaction/entries).
        public static ReplayInfo GetReplayInfo(Server.Shared.State.PreviousGameData data,RoleDeckBuilder roleDeckData)
        {
            ReplayInfo replayInfo = new ReplayInfo
            {
                FactionWin = data.winningFaction,
                GameType = Pepper.GetCurrentGameType(),
                PlayerRole = data.entries[Pepper.GetMyPosition()].originalRole,
                PlayerFaction = MessageHandler.MyFaction,
                PrevGame = new Dictionary<int, PreviousGameData>(),
                BuildVersion = SharedConstants.BUILD_VERSION,
                Hash = HashCodeHelper.GetHashCodeList(MessageHandler.GameMessages),
                RoleList = roleDeckData.roleDeckSlots,
                Bans = roleDeckData.bannedRoleDeckSlots,
                Modifiers = roleDeckData.modifierDeckSlots,
                BetterTOS2 = ModStates.IsLoaded("curtis.tuba.better.tos2") && Main.IsBtos()

            };

            data.entries?.ForEach(entry =>
            {
                replayInfo.PrevGame.TryAdd(entry.playerPosition, new PreviousGameData
                {
                    Role = entry.role == Role.VAMPIRE ? entry.originalRole : entry.role,
                    Modifier = entry.modifier,
					Faction = entry.factionType
                });
            });

            return replayInfo;
        }
        //uh
        public static void PrintByteArray(byte[] bytes)
        {
            StringBuilder stringBuilder = new StringBuilder($"new byte[{bytes.Length}] {{ ");
            bytes.ForEach(b => stringBuilder.Append(b.ToString() + ", "));
            stringBuilder.Append("}");
            Debug.Log(stringBuilder.ToString());
        }
    }
}