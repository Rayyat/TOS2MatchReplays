using System;
using Server.Shared.State;

namespace MatchReplaysPlus
{
    /// <summary>
    /// A single player's role/faction, as recorded in a replay's "reveal roles" data.
    /// </summary>
    [Serializable]
    public class PreviousGameData
    {
        public Role Role;
        public Role Modifier;
        public FactionType Faction;
    }
}