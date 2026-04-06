using System;

namespace EventHorizon.Ads
{
    [Serializable]
    public struct AdShowResult
    {
        public AdRequestContext Request;
        public AdPlacementDefinitionSO Placement;
        public string ProviderId;
        public AdFormat Format;
        public bool WasShown;
        public bool WasCompleted;
        public bool RewardEarned;
        public AdShowStatus Status;
        public string FailureReason;
        public int RewardAmount;
        public string RewardLabel;

        /// <summary>
        /// Executes failed.
        /// </summary>
        public static AdShowResult Failed(AdRequestContext request, string providerId, string failureReason)
        {
            return new AdShowResult
            {
                Request = request,
                Placement = request.Placement,
                ProviderId = providerId,
                Format = request.Placement != null ? request.Placement.Format : AdFormat.Interstitial,
                WasShown = false,
                WasCompleted = false,
                RewardEarned = false,
                Status = AdShowStatus.FailedToStart,
                FailureReason = failureReason,
                RewardAmount = 0,
                RewardLabel = string.Empty
            };
        }
    }
}
