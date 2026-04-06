using UnityEngine;
using UnityEngine.Events;

namespace EventHorizon.Core
{
    /// <summary>
    /// MonoBehaviour that subscribes to a GameEventSO and invokes a UnityEvent response.
    /// Attach to any GameObject to react to ScriptableObject events in the scene.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [Tooltip("The GameEventSO to listen to.")]
        [SerializeField] private GameEventSO _event;

        [Tooltip("The response to invoke when the event is raised.")]
        [SerializeField] private UnityEvent _response;

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
        private void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
