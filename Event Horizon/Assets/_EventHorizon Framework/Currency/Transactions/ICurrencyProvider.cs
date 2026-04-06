namespace EventHorizon.Currency
{
    /// <summary>
    /// Contract for systems that manage currency balances and transactions.
    /// </summary>
    public interface ICurrencyProvider
    {
        /// <summary>
        /// Returns the current balance for the specified currency.
        /// </summary>
        int GetBalance(CurrencyDefinitionSO currency);

        /// <summary>
        /// Adds the specified amount to the currency balance.
        /// </summary>
        void AddCurrency(CurrencyDefinitionSO currency, int amount);

        /// <summary>
        /// Attempts to spend the specified amount. Returns true if successful.
        /// </summary>
        bool SpendCurrency(CurrencyDefinitionSO currency, int amount);

        /// <summary>
        /// Returns true if the balance is sufficient for the specified amount.
        /// </summary>
        bool HasSufficient(CurrencyDefinitionSO currency, int amount);
    }
}
