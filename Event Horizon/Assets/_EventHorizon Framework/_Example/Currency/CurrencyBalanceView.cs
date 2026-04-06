using EventHorizon.Core;
using EventHorizon.UI;
using TMPro;
using UnityEngine;

namespace EventHorizon.Example
{
    /// <summary>
    /// Example view that displays currency balances driven by IntVariableSO assets.
    /// Reacts instantly to any currency updates through VariableSO — no direct
    /// reference to CurrencyModuleSO or Wallet.
    /// </summary>
    public class CurrencyBalanceView : UIViewBase
    {
        [Header("Data")]
        [SerializeField] private IntVariableSO _coins;
        [SerializeField] private IntVariableSO _gems;

        [Header("UI")]
        [SerializeField] private TMP_Text _coinsLabel;
        [SerializeField] private TMP_Text _gemsLabel;

        public override void Bind()
        {
            base.Bind();
            BindVariable(_coins, OnCoinsChanged);
            BindVariable(_gems, OnGemsChanged);
        }

        private void OnCoinsChanged(int value)
        {
            if (_coinsLabel != null)
            {
                _coinsLabel.text = value.ToString();
            }
        }

        private void OnGemsChanged(int value)
        {
            if (_gemsLabel != null)
            {
                _gemsLabel.text = value.ToString();
            }
        }
    }
}
