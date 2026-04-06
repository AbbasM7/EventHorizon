using System;
using System.Collections.Generic;
using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A parameterless ScriptableObject-based event channel for decoupled broadcasting.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Events/Game Event", order = 0)]
    public class GameEventSO : ScriptableObject
    {
        private readonly List<Action> _listeners = new List<Action>();

        /// <summary>
        /// Raises the event, notifying all registered listeners.
        /// Uses a defensive copy to handle mid-invocation unsubscribes safely.
        /// </summary>
        [ContextMenu("Raise")]
        /// <summary>
        /// Raises the  event.
        /// </summary>
        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke();
            }
        }

        /// <summary>
        /// Registers a listener to be notified when this event is raised.
        /// </summary>
        public void RegisterListener(Action listener)
        {
            if (listener != null && !_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unregisters a listener so it will no longer be notified.
        /// </summary>
        public void UnregisterListener(Action listener)
        {
            _listeners.Remove(listener);
        }
    }
}
