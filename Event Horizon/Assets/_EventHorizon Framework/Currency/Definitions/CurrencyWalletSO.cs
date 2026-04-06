using System;
using System.Collections.Generic;
using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Currency
{
    /// <summary>
    /// Runtime storage for all currency balances. Single source of truth.
    /// Initializes from registered CurrencyDefinitionSO assets on enable.
    /// Implements ISaveable for persistence through the Save module.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Currency/Currency Wallet", order = 2)]
    public class CurrencyWalletSO : ScriptableObject, ISaveable
    {
        [Tooltip("All currencies tracked by this wallet.")]
        [SerializeField] private List<CurrencyDefinitionSO> _registeredCurrencies = new List<CurrencyDefinitionSO>();

        private readonly Dictionary<string, int> _runtimeBalances = new Dictionary<string, int>();

        /// <summary>
        /// Unique key for save/load operations.
        /// </summary>
        public string SaveKey => name;

        /// <summary>
        /// Returns the current balance for the specified currency.
        /// </summary>
        public int GetBalance(CurrencyDefinitionSO currency)
        {
            if (currency != null && _runtimeBalances.TryGetValue(currency.CurrencyID, out int balance))
            {
                return balance;
            }

            return 0;
        }

        /// <summary>
        /// Sets the balance for the specified currency. Internal use only.
        /// </summary>
        public void SetBalance(CurrencyDefinitionSO currency, int value)
        {
            if (currency != null)
            {
                _runtimeBalances[currency.CurrencyID] = value;
            }
        }

        /// <summary>
        /// Captures all runtime balances as a JSON string.
        /// </summary>
        public string CaptureState()
        {
            WalletSaveData data = new WalletSaveData();

            foreach (var kvp in _runtimeBalances)
            {
                data.Entries.Add(new WalletSaveData.BalanceEntry
                {
                    CurrencyID = kvp.Key,
                    Balance = kvp.Value
                });
            }

            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// Restores runtime balances from a previously captured JSON string.
        /// </summary>
        public void RestoreState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;

            WalletSaveData data = JsonUtility.FromJson<WalletSaveData>(state);
            if (data == null) return;

            for (int i = 0; i < data.Entries.Count; i++)
            {
                _runtimeBalances[data.Entries[i].CurrencyID] = data.Entries[i].Balance;
            }
        }

        /// <summary>
        /// Binds state when the component is enabled.
        /// </summary>
        private void OnEnable()
        {
            _runtimeBalances.Clear();

            for (int i = 0; i < _registeredCurrencies.Count; i++)
            {
                CurrencyDefinitionSO def = _registeredCurrencies[i];
                if (def == null) continue;

                _runtimeBalances[def.CurrencyID] = def.InitialBalance;
            }
        }

        [Serializable]
        private class WalletSaveData
        {
            [Serializable]
            public struct BalanceEntry
            {
                public string CurrencyID;
                public int Balance;
            }

            public List<BalanceEntry> Entries = new List<BalanceEntry>();
        }
    }
}
