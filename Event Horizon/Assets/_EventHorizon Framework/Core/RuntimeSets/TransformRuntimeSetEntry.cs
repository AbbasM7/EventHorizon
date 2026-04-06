using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// Auto-registers this Transform with a TransformRuntimeSetSO on enable.
    /// </summary>
    public class TransformRuntimeSetEntry : RuntimeSetEntry<Transform>
    {
        /// <summary>
        /// Gets entry.
        /// </summary>
        protected override Transform GetEntry()
        {
            return transform;
        }
    }
}
