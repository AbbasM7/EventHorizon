using System;

namespace EventHorizon.Ads
{
    [Serializable]
    public struct AdLoadResult
    {
        public AdRequestContext Request;
        public AdPlacementDefinitionSO Placement;
        public string ProviderId;
        public bool IsSuccess;
        public string FailureReason;

        /// <summary>
        /// Executes success.
        /// </summary>
        public static AdLoadResult Success(AdRequestContext request, string providerId)
        {
            return new AdLoadResult
            {
                Request = request,
                Placement = request.Placement,
                ProviderId = providerId,
                IsSuccess = true,
                FailureReason = string.Empty
            };
        }

        /// <summary>
        /// Executes failed.
        /// </summary>
        public static AdLoadResult Failed(AdRequestContext request, string providerId, string failureReason)
        {
            return new AdLoadResult
            {
                Request = request,
                Placement = request.Placement,
                ProviderId = providerId,
                IsSuccess = false,
                FailureReason = failureReason
            };
        }
    }
}
