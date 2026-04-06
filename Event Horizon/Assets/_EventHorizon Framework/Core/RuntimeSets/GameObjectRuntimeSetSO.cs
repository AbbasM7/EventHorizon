using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A runtime set that tracks active GameObjects.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Runtime Sets/GameObject Set", order = 0)]
    public class GameObjectRuntimeSetSO : RuntimeSetSO<GameObject>
    {
    }
}
