using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Ads
{
    [CreateAssetMenu(menuName = "EventHorizon/Ads/Events/Placement Event", order = 1)]
    public class AdPlacementEventSO : GameEventSO<AdPlacementDefinitionSO>
    {
    }
}
