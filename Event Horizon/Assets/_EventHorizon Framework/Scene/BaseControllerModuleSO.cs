using System;
using System.Collections.Generic;
using EventHorizon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EventHorizon.Scene
{
    /// <summary>
    /// Framework module that handles additive scene loading and unloading.
    /// Listens exclusively to SceneEventChannelSO â€” no direct dependency on any other module.
    /// Scene identity is resolved through SceneRegistrySO using string keys, keeping
    /// callers decoupled from scene names and build-index details.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Scene/Base Controller Module", order = 0)]
    public class BaseControllerModuleSO : ModuleBase
    {
        [Header("Registry")]
        [Tooltip("Registry that maps scene keys to Unity scene names.")]
        [SerializeField] private SceneRegistrySO _sceneRegistry;

        [Header("Event Channel")]
        [Tooltip("Event channel the module listens to for load and unload requests.")]
        [SerializeField] private SceneEventChannelSO _eventChannel;

        private readonly List<string> _loadedSceneNames = new List<string>();
        private readonly List<(GameEventSO trigger, Action handler)> _triggerSubscriptions = new List<(GameEventSO, Action)>();

        // â”€â”€â”€ Lifecycle â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Performs module-specific initialization logic.
        /// </summary>
        protected override void OnInitialize()
        {
            SubscribeToEvents();
            SubscribeToTriggers();
        }

        /// <summary>
        /// Performs module-specific cleanup logic.
        /// </summary>
        protected override void OnDispose()
        {
            UnsubscribeFromTriggers();
            UnsubscribeFromEvents();
            _loadedSceneNames.Clear();
        }

        // â”€â”€â”€ Event wiring â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Subscribes to to events.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_eventChannel == null) return;

            if (_eventChannel.OnLoadSceneRequested != null)
                _eventChannel.OnLoadSceneRequested.RegisterListener(OnLoadSceneRequested);

            if (_eventChannel.OnUnloadSceneRequested != null)
                _eventChannel.OnUnloadSceneRequested.RegisterListener(OnUnloadSceneRequested);

            if (_eventChannel.OnUnloadAllRequested != null)
                _eventChannel.OnUnloadAllRequested.RegisterListener(OnUnloadAllRequested);
        }

        /// <summary>
        /// Unsubscribes from from events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_eventChannel == null) return;

            if (_eventChannel.OnLoadSceneRequested != null)
                _eventChannel.OnLoadSceneRequested.UnregisterListener(OnLoadSceneRequested);

            if (_eventChannel.OnUnloadSceneRequested != null)
                _eventChannel.OnUnloadSceneRequested.UnregisterListener(OnUnloadSceneRequested);

            if (_eventChannel.OnUnloadAllRequested != null)
                _eventChannel.OnUnloadAllRequested.UnregisterListener(OnUnloadAllRequested);
        }

        /// <summary>
        /// Subscribes to to triggers.
        /// </summary>
        private void SubscribeToTriggers()
        {
            if (_sceneRegistry == null) return;

            foreach (SceneEntryData entry in _sceneRegistry.Entries)
            {
                if (entry == null) continue;

                if (entry.LoadTrigger != null)
                {
                    string sceneName = entry.SceneName;
                    Action handler = () => LoadSceneAdditive(sceneName);
                    entry.LoadTrigger.RegisterListener(handler);
                    _triggerSubscriptions.Add((entry.LoadTrigger, handler));
                }

                if (entry.UnloadTrigger != null)
                {
                    string sceneName = entry.SceneName;
                    Action handler = () => UnloadScene(sceneName);
                    entry.UnloadTrigger.RegisterListener(handler);
                    _triggerSubscriptions.Add((entry.UnloadTrigger, handler));
                }
            }
        }

        /// <summary>
        /// Unsubscribes from from triggers.
        /// </summary>
        private void UnsubscribeFromTriggers()
        {
            for (int i = 0; i < _triggerSubscriptions.Count; i++)
            {
                _triggerSubscriptions[i].trigger.UnregisterListener(_triggerSubscriptions[i].handler);
            }

            _triggerSubscriptions.Clear();
        }

        // â”€â”€â”€ Handlers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Handles the load scene request.
        /// </summary>
        private void OnLoadSceneRequested(string key)
        {
            if (_sceneRegistry == null)
            {
                LogWarning("No SceneRegistry assigned.");
                return;
            }

            if (!_sceneRegistry.TryGetSceneName(key, out string sceneName))
            {
                LogWarning($"No scene registered for key '{key}'.");
                return;
            }

            LoadSceneAdditive(sceneName);
        }

        /// <summary>
        /// Handles the unload scene request.
        /// </summary>
        private void OnUnloadSceneRequested(string key)
        {
            if (_sceneRegistry == null) return;

            if (!_sceneRegistry.TryGetSceneName(key, out string sceneName))
            {
                LogWarning($"No scene registered for key '{key}'.");
                return;
            }

            UnloadScene(sceneName);
        }

        /// <summary>
        /// Handles the unload all request.
        /// </summary>
        private void OnUnloadAllRequested()
        {
            for (int i = _loadedSceneNames.Count - 1; i >= 0; i--)
            {
                UnloadScene(_loadedSceneNames[i]);
            }
        }

        // â”€â”€â”€ Scene operations â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Loads scene additive.
        /// </summary>
        private async void LoadSceneAdditive(string sceneName)
        {
            if (_loadedSceneNames.Contains(sceneName))
            {
                LogWarning($"Scene '{sceneName}' is already loaded.");
                return;
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null) return;

            while (!op.isDone)
                await Awaitable.NextFrameAsync();

            _loadedSceneNames.Add(sceneName);
            LogInfo($"Scene '{sceneName}' loaded additively.");
        }

        /// <summary>
        /// Unloads scene.
        /// </summary>
        private async void UnloadScene(string sceneName)
        {
            if (!_loadedSceneNames.Contains(sceneName)) return;

            AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
            if (op == null) return;

            while (!op.isDone)
                await Awaitable.NextFrameAsync();

            _loadedSceneNames.Remove(sceneName);
            LogInfo($"Scene '{sceneName}' unloaded.");
        }
    }
}
