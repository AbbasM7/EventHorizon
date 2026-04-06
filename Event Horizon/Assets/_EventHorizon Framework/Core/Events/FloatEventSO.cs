using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A typed event channel that broadcasts a float value.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Events/Float Event", order = 1)]
    public class FloatEventSO : GameEventSO<float>
    {
    }
}
