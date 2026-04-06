using System;

namespace EventHorizon.Ads
{
    [Serializable]
    public struct AdTelemetryEvent
    {
        public string PlacementId;
        public string SourceId;
        public string FlowId;
        public string Reason;
        public string ProviderId;
        public bool CanShow;
        public bool LoadSucceeded;
        public bool WasShown;
        public bool WasCompleted;
        public bool RewardEarned;
        public string FailureReason;

        /// <summary>
        /// Executes from.
        /// </summary>
        public static AdTelemetryEvent From(string providerId, AdRequestContext request, bool canShow, bool loadSucceeded, bool wasShown, bool wasCompleted, bool rewardEarned, string failureReason)
        {
            return new AdTelemetryEvent
            {
                PlacementId = request.Placement != null ? request.Placement.PlacementId : string.Empty,
                SourceId = request.SourceId ?? string.Empty,
                FlowId = request.FlowId ?? string.Empty,
                Reason = request.Reason ?? string.Empty,
                ProviderId = providerId ?? string.Empty,
                CanShow = canShow,
                LoadSucceeded = loadSucceeded,
                WasShown = wasShown,
                WasCompleted = wasCompleted,
                RewardEarned = rewardEarned,
                FailureReason = failureReason ?? string.Empty
            };
        }
    }
}
