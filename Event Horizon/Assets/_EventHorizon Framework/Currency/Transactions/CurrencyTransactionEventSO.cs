using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Currency
{
    /// <summary>
    /// Typed event channel that broadcasts CurrencyTransactionEvent payloads.
    /// Used by CurrencyEventChannelSO to notify listeners of balance changes.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Currency/Currency Transaction Event", order = 4)]
    public class CurrencyTransactionEventSO : GameEventSO<CurrencyTransactionEvent>
    {
    }
}
