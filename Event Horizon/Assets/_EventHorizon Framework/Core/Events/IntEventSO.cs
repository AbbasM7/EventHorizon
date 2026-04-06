using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A typed event channel that broadcasts an int value.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Events/Int Event", order = 2)]
    public class IntEventSO : GameEventSO<int>
    {
    }
}
