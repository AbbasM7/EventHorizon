using System;

namespace EventHorizon.Currency
{
    /// <summary>
    /// Immutable data payload describing a currency balance change.
    /// Positive delta = earned, negative delta = spent.
    /// </summary>
    [Serializable]
    public struct CurrencyTransactionEvent
    {
        /// <summary>
        /// The currency that changed.
        /// </summary>
        public CurrencyDefinitionSO Currency;

        /// <summary>
        /// The balance before the transaction.
        /// </summary>
        public int OldBalance;

        /// <summary>
        /// The balance after the transaction.
        /// </summary>
        public int NewBalance;

        /// <summary>
        /// The change in balance (positive = earned, negative = spent).
        /// </summary>
        public int Delta;
    }
}
