using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A typed event channel that broadcasts a bool value.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Events/Bool Event", order = 3)]
    public class BoolEventSO : GameEventSO<bool>
    {
    }
}
