using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBelow.Narrative
{
    internal sealed class StoryFlags
    {
        private readonly HashSet<string> _enabled = new(StringComparer.Ordinal);

        internal static bool IsValidId(string id) => !string.IsNullOrWhiteSpace(id) && id == id.Trim();

        internal static bool AreValid(string[] flags)
        {
            if (flags == null)
                return true;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var flag in flags)
                if (!IsValidId(flag) || !seen.Add(flag))
                    return false;
            return true;
        }

        internal bool Has(string id) => _enabled.Contains(id);
        internal void Set(string id) => _enabled.Add(id);
        internal string[] Capture() => _enabled.OrderBy(id => id, StringComparer.Ordinal).ToArray();

        internal void Restore(string[] flags)
        {
            _enabled.Clear();
            if (flags != null)
                _enabled.UnionWith(flags);
        }
    }
}
