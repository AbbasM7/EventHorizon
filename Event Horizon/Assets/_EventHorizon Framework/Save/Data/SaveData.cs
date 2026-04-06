using System;
using System.Collections.Generic;

namespace EventHorizon.Save
{
    /// <summary>
    /// Serializable container for a complete save snapshot.
    /// Holds a dictionary of key-value pairs from all registered saveables.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// Individual entry in the save data.
        /// </summary>
        [Serializable]
        public struct Entry
        {
            /// <summary>The saveable's unique key.</summary>
            public string Key;

            /// <summary>The serialized state data.</summary>
            public string Value;
        }

        /// <summary>
        /// All saved entries.
        /// </summary>
        public List<Entry> Entries = new List<Entry>();

        /// <summary>
        /// Timestamp when this save was created (UTC).
        /// </summary>
        public string Timestamp;

        /// <summary>
        /// Sets or overwrites an entry.
        /// </summary>
        public void Set(string key, string value)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Key == key)
                {
                    Entries[i] = new Entry { Key = key, Value = value };
                    return;
                }
            }

            Entries.Add(new Entry { Key = key, Value = value });
        }

        /// <summary>
        /// Gets an entry by key. Returns null if not found.
        /// </summary>
        public string Get(string key)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Key == key)
                {
                    return Entries[i].Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if an entry with the given key exists.
        /// </summary>
        public bool Has(string key)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Key == key) return true;
            }

            return false;
        }
    }
}
