using System;
using System.Collections.Generic;
using Server.Shared.Info;
using Server.Shared.State;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Metadata stored at the start of every .replay file: who won, what the local
    /// player's role/faction was, and the per-player role reveal data used by the
    /// "Reveal Roles" hotkey.
    /// </summary>
    [Serializable]
    public class ReplayInfo
    {
        public FactionType FactionWin;
        public Role PlayerRole;
        public FactionType PlayerFaction;
        public GameType GameType;
        public Dictionary<int, PreviousGameData> PrevGame;
        public string BuildVersion;
        public bool BetterTOS2;
        public int Hash;
        public List<RoleDeckSlot> RoleList;
        public List<RoleDeckSlot> Bans;
        public List<RoleDeckSlot> Modifiers;

    }
}