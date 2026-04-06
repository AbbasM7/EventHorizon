using System;
using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Scene
{
    /// <summary>
    /// Maps a string key and optional trigger events to a Unity scene name.
    /// Trigger events allow any caller to fire a parameterless GameEventSO and
    /// have BaseControllerModuleSO resolve the correct scene automatically —
    /// no key string required at the call site.
    /// </summary>
    [Serializable]
    public class SceneEntryData
    {
        [Tooltip("Unique string key used to identify this scene in load/unload events.")]
        [SerializeField] private string _key;

        [Tooltip("The exact name of the Unity scene asset as it appears in Build Settings.")]
        [SerializeField] private string _sceneName;

        [Header("Direct Triggers (optional)")]
        [Tooltip("Raise this event anywhere to load this scene — no key needed.")]
        [SerializeField] private GameEventSO _loadTrigger;

        [Tooltip("Raise this event anywhere to unload this scene — no key needed.")]
        [SerializeField] private GameEventSO _unloadTrigger;

        /// <summary>The key used to reference this entry from string event payloads.</summary>
        public string Key => _key;

        /// <summary>The Unity scene name passed to SceneManager.</summary>
        public string SceneName => _sceneName;

        /// <summary>Optional parameterless event that triggers an additive load of this scene.</summary>
        public GameEventSO LoadTrigger => _loadTrigger;

        /// <summary>Optional parameterless event that triggers an unload of this scene.</summary>
        public GameEventSO UnloadTrigger => _unloadTrigger;
    }
}
