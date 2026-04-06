using UnityEngine;

namespace EventHorizon.Ads
{
    [CreateAssetMenu(menuName = "EventHorizon/Ads/Event Channel", order = 0)]
    public class AdsEventChannelSO : ScriptableObject
    {
        [SerializeField] private AdRequestContextEventSO _onPreloadRequested;
        [SerializeField] private AdRequestContextEventSO _onShowRequested;
        [SerializeField] private AdRequestContextEventSO _onHideRequested;
        [SerializeField] private AdRequestContextEventSO _onShowStarted;
        [SerializeField] private AdAvailabilityChangedEventSO _onAvailabilityChanged;
        [SerializeField] private AdLoadResultEventSO _onLoadCompleted;
        [SerializeField] private AdShowResultEventSO _onShowCompleted;
        [SerializeField] private AdTelemetryEventSO _onTelemetryReported;

        public AdRequestContextEventSO OnPreloadRequested => _onPreloadRequested;
        public AdRequestContextEventSO OnShowRequested => _onShowRequested;
        public AdRequestContextEventSO OnHideRequested => _onHideRequested;
        public AdRequestContextEventSO OnShowStarted => _onShowStarted;
        public AdAvailabilityChangedEventSO OnAvailabilityChanged => _onAvailabilityChanged;
        public AdLoadResultEventSO OnLoadCompleted => _onLoadCompleted;
        public AdShowResultEventSO OnShowCompleted => _onShowCompleted;
        public AdTelemetryEventSO OnTelemetryReported => _onTelemetryReported;
    }
}
