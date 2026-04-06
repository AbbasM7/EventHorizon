using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Scene
{
    /// <summary>
    /// Event channel for scene load and unload requests.
    /// Callers raise these events with a scene key — only BaseControllerModuleSO listens.
    /// No direct coupling between any two systems.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Scene/Scene Event Channel", order = 1)]
    public class SceneEventChannelSO : ScriptableObject
    {
        [Tooltip("Raise with a scene key to load that scene additively.")]
        [SerializeField] private StringEventSO _onLoadSceneRequested;

        [Tooltip("Raise with a scene key to unload that additive scene.")]
        [SerializeField] private StringEventSO _onUnloadSceneRequested;

        [Tooltip("Raise to unload all scenes that were loaded additively by the controller.")]
        [SerializeField] private GameEventSO _onUnloadAllRequested;

        /// <summary>Raised with a scene key to trigger an additive load.</summary>
        public StringEventSO OnLoadSceneRequested => _onLoadSceneRequested;

        /// <summary>Raised with a scene key to trigger an additive unload.</summary>
        public StringEventSO OnUnloadSceneRequested => _onUnloadSceneRequested;

        /// <summary>Raised to unload all additively loaded scenes at once.</summary>
        public GameEventSO OnUnloadAllRequested => _onUnloadAllRequested;
    }
}
