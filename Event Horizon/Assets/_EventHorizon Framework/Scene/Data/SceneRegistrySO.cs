using System.Collections.Generic;
using UnityEngine;

namespace EventHorizon.Scene
{
    /// <summary>
    /// Central registry that maps string keys to Unity scene names.
    /// BaseControllerModuleSO reads this registry when a load/unload event arrives.
    /// Modules and game systems never reference scene names directly — only keys.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Scene/Scene Registry", order = 0)]
    public class SceneRegistrySO : ScriptableObject
    {
        [Tooltip("All scenes available for additive loading. Keys must be unique.")]
        [SerializeField] private List<SceneEntryData> _entries = new List<SceneEntryData>();

        /// <summary>All registered scene entries. Used by BaseControllerModuleSO to wire trigger events.</summary>
        public IReadOnlyList<SceneEntryData> Entries => _entries;

        /// <summary>
        /// Looks up the Unity scene name for the given key.
        /// Returns false if no entry with that key is registered.
        /// </summary>
        public bool TryGetSceneName(string key, out string sceneName)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i] != null && _entries[i].Key == key)
                {
                    sceneName = _entries[i].SceneName;
                    return true;
                }
            }

            sceneName = null;
            return false;
        }
    }
}
