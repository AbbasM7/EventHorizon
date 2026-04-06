using System;
using System.Collections.Generic;
using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// Generic base for typed ScriptableObject event channels.
    /// Concrete subclasses provide the serializable type (e.g., FloatEventSO).
    /// </summary>
    public abstract class GameEventSO<T> : ScriptableObject
    {
        private readonly List<Action<T>> _listeners = new List<Action<T>>();

        /// <summary>
        /// Raises the event with a payload, notifying all registered listeners.
        /// Iterates in reverse for safe mid-invocation unsubscribes.
        /// </summary>
        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke(value);
            }
        }

        /// <summary>
        /// Registers a typed listener to be notified when this event is raised.
        /// </summary>
        public void RegisterListener(Action<T> listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unregisters a typed listener so it will no longer be notified.
        /// </summary>
        public void UnregisterListener(Action<T> listener)
        {
            _listeners.Remove(listener);
        }
    }
}
