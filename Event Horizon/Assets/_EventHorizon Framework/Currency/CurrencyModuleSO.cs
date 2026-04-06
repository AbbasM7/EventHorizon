using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Currency
{
    /// <summary>
    /// Currency management module. Handles balance tracking, transaction validation,
    /// and event broadcasting. All writes go through this module â€” no external system
    /// mutates balances directly.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Currency/Currency Module", order = 1)]
    public class CurrencyModuleSO : ModuleBase, ICurrencyProvider
    {
        [Tooltip("The wallet asset holding all runtime balances.")]
        [SerializeField] private CurrencyWalletSO _wallet;

        [Tooltip("Event channels for broadcasting currency changes.")]
        [SerializeField] private CurrencyEventChannelSO _eventChannel;

        /// <summary>
        /// Returns the current balance for the specified currency.
        /// </summary>
        public int GetBalance(CurrencyDefinitionSO currency)
        {
            return _wallet != null ? _wallet.GetBalance(currency) : 0;
        }

        /// <summary>
        /// Adds the specified amount to the currency balance.
        /// Amount must be positive.
        /// </summary>
        public void AddCurrency(CurrencyDefinitionSO currency, int amount)
        {
            if (currency == null || amount <= 0 || _wallet == null) return;

            int oldBalance = _wallet.GetBalance(currency);
            int newBalance = oldBalance + amount;
            _wallet.SetBalance(currency, newBalance);

            RaiseTransactionEvent(currency, oldBalance, newBalance, amount);
        }

        /// <summary>
        /// Attempts to spend the specified amount. Returns false and raises
        /// OnInsufficientFunds if the balance is not sufficient.
        /// </summary>
        public bool SpendCurrency(CurrencyDefinitionSO currency, int amount)
        {
            if (currency == null || amount <= 0 || _wallet == null) return false;

            if (!HasSufficient(currency, amount))
            {
                if (_eventChannel != null && _eventChannel.OnInsufficientFunds != null)
                {
                    _eventChannel.OnInsufficientFunds.Raise();
                }

                return false;
            }

            int oldBalance = _wallet.GetBalance(currency);
            int newBalance = oldBalance - amount;
            _wallet.SetBalance(currency, newBalance);

            RaiseTransactionEvent(currency, oldBalance, newBalance, -amount);

            return true;
        }

        /// <summary>
        /// Returns true if the current balance meets or exceeds the specified amount.
        /// Pure read â€” no side effects.
        /// </summary>
        public bool HasSufficient(CurrencyDefinitionSO currency, int amount)
        {
            return _wallet != null && _wallet.GetBalance(currency) >= amount;
        }

        /// <summary>
        /// Raises the transaction event event.
        /// </summary>
        private void RaiseTransactionEvent(CurrencyDefinitionSO currency, int oldBalance, int newBalance, int delta)
        {
            if (_eventChannel == null || _eventChannel.OnCurrencyChanged == null) return;

            _eventChannel.OnCurrencyChanged.Raise(new CurrencyTransactionEvent
            {
                Currency = currency,
                OldBalance = oldBalance,
                NewBalance = newBalance,
                Delta = delta
            });
        }
    }
}
