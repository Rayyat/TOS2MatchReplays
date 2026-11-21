using System;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Small logging helper that prefixes all messages with the mod's name.
    /// </summary>
    public static class ModLogger
    {
        public static void Log(object message)
        {
            Console.WriteLine("[MatchReplays] {0}", message);
        }
    }
}