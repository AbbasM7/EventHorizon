
using UnityEngine;
namespace EventHorizon.Core
{
    /// <summary>
    /// A typed event channel that broadcasts a string value.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Events/String Event", order = 4)]
    public class StringEventSO : GameEventSO<string>
    {
    }
}
