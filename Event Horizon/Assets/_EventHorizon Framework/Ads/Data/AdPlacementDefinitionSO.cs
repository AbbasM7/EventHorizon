using UnityEngine;

namespace EventHorizon.Ads
{
    [CreateAssetMenu(menuName = "EventHorizon/Ads/Placement", order = 0)]
    public class AdPlacementDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _placementId = "placement_id";
        [SerializeField] private AdFormat _format = AdFormat.Interstitial;
        [SerializeField] private AdViewPosition _viewPosition = AdViewPosition.Bottom;
        [SerializeField] private bool _preloadOnInitialize = true;
        [SerializeField] private float _loadTimeoutSeconds = 5f;
        [SerializeField] private float _showTimeoutSeconds = 30f;

        public string PlacementId => _placementId;
        public AdFormat Format => _format;
        public AdViewPosition ViewPosition => _viewPosition;
        public bool PreloadOnInitialize => _preloadOnInitialize;
        public float LoadTimeoutSeconds => _loadTimeoutSeconds;
        public float ShowTimeoutSeconds => _showTimeoutSeconds;
    }
}
