using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Currency
{
    /// <summary>
    /// Event channels for currency transactions and insufficient funds notifications.
    /// Wired in the Inspector to decouple currency logic from UI and game systems.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Currency/Currency Event Channel", order = 3)]
    public class CurrencyEventChannelSO : ScriptableObject
    {
        [Tooltip("Raised when any currency balance changes, with full transaction details.")]
        [SerializeField] private GameEventSO<CurrencyTransactionEvent> _onCurrencyChanged;

        [Tooltip("Raised when a spend attempt fails due to insufficient funds.")]
        [SerializeField] private GameEventSO _onInsufficientFunds;

        /// <summary>
        /// Event raised when a currency balance changes.
        /// </summary>
        public GameEventSO<CurrencyTransactionEvent> OnCurrencyChanged => _onCurrencyChanged;

        /// <summary>
        /// Event raised when a spend attempt fails due to insufficient funds.
        /// </summary>
        public GameEventSO OnInsufficientFunds => _onInsufficientFunds;
    }
}
