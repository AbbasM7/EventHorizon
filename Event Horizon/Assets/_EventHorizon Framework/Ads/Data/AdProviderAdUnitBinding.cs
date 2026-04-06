using System;
using UnityEngine;

namespace EventHorizon.Ads
{
    [Serializable]
    public class AdProviderAdUnitBinding
    {
        [SerializeField] private AdPlacementDefinitionSO _placement;
        [SerializeField] private string _adUnitId;

        public AdPlacementDefinitionSO Placement => _placement;
        public string AdUnitId => _adUnitId;
    }
}
