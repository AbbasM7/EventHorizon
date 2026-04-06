using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A runtime set that tracks active Transforms.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Runtime Sets/Transform Set", order = 1)]
    public class TransformRuntimeSetSO : RuntimeSetSO<Transform>
    {
    }
}
