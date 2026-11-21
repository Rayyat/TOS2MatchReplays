using System;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Global state describing the replay that is currently being watched, if any.
    /// </summary>
    public static class ActiveReplayInfo
    {
        public static bool ReplayActive;
        public static bool RolesRevealed;
        public static ReplayMatch MatchReplayGO;
        public static string ReplayFilePath;
        public static ReplayInfo ReplayInfo;
    }
}