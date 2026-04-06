using UnityEngine;

namespace EventHorizon.Ads
{
    public interface IAdsManager
    {
        Awaitable<bool> CanShowAsync(AdRequestContext request);
        Awaitable PreloadAsync(AdRequestContext request);
        Awaitable<AdShowResult> ShowAsync(AdRequestContext request);
        Awaitable HideAsync(AdRequestContext request);
        Awaitable<AdAvailabilityState> GetAvailabilityAsync(AdRequestContext request);
    }
}
