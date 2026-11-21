using System;
using System.Collections.Generic;

namespace MatchReplaysPlus
{
    /// <summary>
    /// Produces a simple order-sensitive hash for a list of items, used to detect
    /// whether a replay file's recorded messages have been tampered with or are stale.
    /// </summary>
    public static class HashCodeHelper
    {
        public static int GetHashCodeList<T>(List<T> list)
        {
            int hash = 19;
            list.ForEach(item => hash *= 31 + item.GetHashCode());
            return hash;
        }
    }
}