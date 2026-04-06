using UnityEngine;
using UnityEngine.Events;

namespace EventHorizon.Core
{
    /// <summary>
    /// Generic base for typed MonoBehaviour event listeners.
    /// Subscribes to a GameEventSO&lt;T&gt; and invokes a UnityEvent&lt;T&gt; response.
    /// Concrete subclasses provide the serializable type.
    /// </summary>
    public abstract class GameEventListener<TEvent, T> : MonoBehaviour
        where TEvent : GameEventSO<T>
    {
        [Tooltip("The typed event to listen to.")]
        [SerializeField] private TEvent _event;

        [Tooltip("The response invoked with the event payload.")]
        [SerializeField] private UnityEvent<T> _response;

        /// <summary>
        /// Binds state when the component is enabled.
        /// </summary>
        private void OnEnable()
        {
            if (_event != null)
            {
                _event.RegisterListener(OnEventRaised);
            }
        }

        /// <summary>
        /// Releases bindings when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (_event != null)
            {
                _event.UnregisterListener(OnEventRaised);
            }
        }

        /// <summary>
        /// Handles event raised.
        /// </summary>
        private void OnEventRaised(T value)
        {
            _response?.Invoke(value);
        }
    }
}
