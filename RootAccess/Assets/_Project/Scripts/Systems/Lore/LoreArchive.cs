using System;
using System.Collections.Generic;

namespace RootAccess.Systems
{
    /// <summary>Quest target ids reported when data logs are found.</summary>
    public static class LoreTargets
    {
        /// <summary>Reported (Collect) for every log found, whichever it is: the archivist contract counts these.</summary>
        public const string AnyLog = "lore_any";
    }

    /// <summary>
    /// Data logs the player has found, in the order they were found. Plain C# like the other session models, so it is
    /// unit-testable and saved as a list of ids.
    /// </summary>
    public sealed class LoreArchive
    {
        private readonly HashSet<string> _found = new();
        private readonly List<string> _order = new();

        /// <summary>Raised with the id of a log found for the first time.</summary>
        public event Action<string> Collected;

        public int Count => _found.Count;
        public IReadOnlyList<string> FoundIds => _order;

        public bool Has(string loreId) => !string.IsNullOrEmpty(loreId) && _found.Contains(loreId);

        /// <summary>Marks a log as found; false when the id is empty or it was already found.</summary>
        public bool Collect(string loreId)
        {
            if (string.IsNullOrEmpty(loreId) || !_found.Add(loreId)) return false;
            _order.Add(loreId);
            Collected?.Invoke(loreId);
            return true;
        }

        /// <summary>How many of the logs hidden in <paramref name="levelId"/> have been found.</summary>
        public int CountInLevel(IEnumerable<LoreDefinition> logs, string levelId)
        {
            int count = 0;
            if (logs == null) return count;
            foreach (LoreDefinition log in logs)
                if (log != null && log.LevelId == levelId && Has(log.LoreId)) count++;
            return count;
        }

        public List<string> ToData() => new(_order);

        public void LoadFrom(IEnumerable<string> ids)
        {
            _found.Clear();
            _order.Clear();
            if (ids == null) return;
            foreach (string id in ids)
                if (!string.IsNullOrEmpty(id) && _found.Add(id))
                    _order.Add(id);
        }
    }
}
