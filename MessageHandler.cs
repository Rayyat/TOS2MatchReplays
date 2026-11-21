using System;
using System.Collections.Generic;
using Game;
using HarmonyLib;
using LiteNetLib;
using Server.Shared.Info;
using Server.Shared.Messages;
using Server.Shared.State;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Records every relevant game message as it's handled during a live match, so the
    /// match can later be saved as a replay (see <see cref="SaveAsReplayFile"/>).
    /// </summary>
    [HarmonyPatch(typeof(LocalClient))]
    public static class MessageHandler
    {
        public static readonly List<GameMessage> GameMessages = new List<GameMessage>();
        public static FactionType MyFaction;

        [HarmonyPatch("HandleMessage")]
        public static void Postfix(ref GameMessage message, ref DeliveryMethod deliveryMethod)
        {
            if (deliveryMethod == DeliveryMethod.Sequenced || IsFiltered(message))
            {
                return;
            }

            GameMessages.Add(message);

            if (MyFaction == FactionType.NONE)
            {
                MyFaction = Pepper.GetMyFaction();
            }
        }

        [HarmonyPatch("OnDestroy")]
        public static void Postfix()
        {
            GameMessages.Clear();
            SetFlag.GameActive = false;
            MyFaction = FactionType.NONE;
        }

        /// <summary>
        /// Decides whether a message should be excluded from the recorded replay.
        /// </summary>
        public static bool IsFiltered(GameMessage message)
        {
            if (Pepper.GetCurrentGamePhase() == GamePhase.LOBBY)
            {
                if (Pepper.GetCurrentGameType() == GameType.Ranked)
                {
                    return false;
                }

                int messageId = (int)message.MessageId;
                if (messageId - 48 <= 1)
                {
                    return false;
                }

                if (message.MessageId == MessageType.OBSERVATION)
                {
                    ObservationMessage observationMessage = message as ObservationMessage;
                    if (observationMessage.observation.type == ObservationType.PREVIOUS_GAME)
                    {
                        return false;
                    }
                }

                return true;
            }

            if (message.MessageId == MessageType.OBSERVATION)
            {
                ObservationMessage observationMessage = message as ObservationMessage;
                if (observationMessage.observation.type == ObservationType.LWDN)
                {
                    return true;
                }
            }

            return message.MessageId == MessageType.GAME_RESULTS;
        }
    }
}