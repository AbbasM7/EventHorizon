using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// Auto-registers this GameObject with a GameObjectRuntimeSetSO on enable.
    /// </summary>
    public class GameObjectRuntimeSetEntry : RuntimeSetEntry<GameObject>
    {
        /// <summary>
        /// Gets entry.
        /// </summary>
        protected override GameObject GetEntry()
        {
            return gameObject;
        }
    }
}
