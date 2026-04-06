using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Ads
{
    [CreateAssetMenu(menuName = "EventHorizon/Ads/Events/Telemetry Event", order = 5)]
    public class AdTelemetryEventSO : GameEventSO<AdTelemetryEvent>
    {
    }
}
