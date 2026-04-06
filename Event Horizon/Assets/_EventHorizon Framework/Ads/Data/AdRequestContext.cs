using System;

namespace EventHorizon.Ads
{
    [Serializable]
    public struct AdRequestContext
    {
        public AdPlacementDefinitionSO Placement;
        public string SourceId;
        public string FlowId;
        public string Reason;

        /// <summary>
        /// Executes for placement.
        /// </summary>
        public static AdRequestContext ForPlacement(AdPlacementDefinitionSO placement)
        {
            return new AdRequestContext
            {
                Placement = placement,
                SourceId = string.Empty,
                FlowId = string.Empty,
                Reason = string.Empty
            };
        }
    }
}
