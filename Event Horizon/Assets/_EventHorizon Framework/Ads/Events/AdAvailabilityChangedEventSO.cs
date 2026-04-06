using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Ads
{
    [CreateAssetMenu(menuName = "EventHorizon/Ads/Events/Availability Changed Event", order = 2)]
    public class AdAvailabilityChangedEventSO : GameEventSO<AdAvailabilityChanged>
    {
    }
}
