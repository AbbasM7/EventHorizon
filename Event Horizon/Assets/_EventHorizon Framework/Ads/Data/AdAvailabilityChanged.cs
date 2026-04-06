using System;

namespace EventHorizon.Ads
{
    [Serializable]
    public struct AdAvailabilityChanged
    {
        public AdPlacementDefinitionSO Placement;
        public string ProviderId;
        public AdAvailabilityState State;
        public bool CanShow;
    }
}
