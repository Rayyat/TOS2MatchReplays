using System;
using System.Collections.Generic;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Display names for the mod's custom hotkey types (registered in <see cref="HotKeyPatch"/>),
    /// keyed by their raw <c>InputService.HotKey.HotKeyType</c> value.
    /// </summary>
    public static class HotKeyTypePlus
    {
        public static Dictionary<int, string> HotKeyTypePlusLookup = new Dictionary<int, string>
        {
            { 100, "Pause Replay" },
            { 101, "Change Replay Time" },
            { 102, "Reveal Replay Roles" }
        };
    }
}