using UnityEngine;

namespace EventHorizon.Currency
{
    /// <summary>
    /// Data asset defining a single currency type (e.g., Coins, Gems, Stars).
    /// Each currency is a unique asset — no hardcoded enums or string constants.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Currency/Currency Definition", order = 0)]
    public class CurrencyDefinitionSO : ScriptableObject
    {
        [Tooltip("Unique identifier for this currency (e.g., 'Coins', 'Gems').")]
        [SerializeField] private string _currencyID;

        [Tooltip("Localized display name for UI.")]
        [SerializeField] private string _displayName;

        [Tooltip("Icon sprite for UI display.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Starting balance when the wallet is initialized.")]
        [SerializeField] private int _initialBalance;

        /// <summary>
        /// Unique identifier for this currency.
        /// </summary>
        public string CurrencyID => _currencyID;

        /// <summary>
        /// Display name shown in UI.
        /// </summary>
        public string DisplayName => _displayName;

        /// <summary>
        /// Icon sprite for this currency.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Starting balance when the wallet is first initialized.
        /// </summary>
        public int InitialBalance => _initialBalance;
    }
}
