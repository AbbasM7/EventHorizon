using System;
using System.Collections.Generic;
using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Save
{
    /// <summary>
    /// Save/persistence module. Orchestrates snapshotting and restoring state
    /// across all registered ISaveable objects using a pluggable ISaveStorage backend.
    /// Communicates through SaveEventChannelSO â€” no direct references needed from game code.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Save/Save Module", order = 0)]
    public class SaveModuleSO : ModuleBase
    {
        [Header("Storage")]
        [Tooltip("The storage backend (PlayerPrefs, File, etc.). Must implement ISaveStorage.")]
        [SerializeField] private ScriptableObject _storageAsset;

        [Tooltip("The key used to store the combined save data in the storage backend.")]
        [SerializeField] private string _saveSlotKey = "SaveSlot_0";

        [Header("Events")]
        [Tooltip("Event channel for save/load/delete requests and completion signals.")]
        [SerializeField] private SaveEventChannelSO _eventChannel;

        [Header("Saveables")]
        [Tooltip("All ScriptableObject saveables to include in snapshots. Must implement ISaveable.")]
        [SerializeField] private List<ScriptableObject> _registeredSaveables = new List<ScriptableObject>();

        private ISaveStorage _storage;

        /// <summary>
        /// Saves all registered saveables to the storage backend.
        /// </summary>
        public void SaveAll()
        {
            if (_storage == null)
            {
                LogError("No valid storage backend assigned.");
                return;
            }

            SaveData data = new SaveData
            {
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            for (int i = 0; i < _registeredSaveables.Count; i++)
            {
                if (_registeredSaveables[i] is ISaveable saveable)
                {
                    string state = saveable.CaptureState();
                    if (state != null)
                    {
                        data.Set(saveable.SaveKey, state);
                    }
                }
            }

            string json = JsonUtility.ToJson(data, true);
            _storage.Save(_saveSlotKey, json);

            LogInfo($"Saved {data.Entries.Count} entries to '{_saveSlotKey}'.");

            if (_eventChannel != null && _eventChannel.OnSaveCompleted != null)
            {
                _eventChannel.OnSaveCompleted.Raise();
            }
        }

        /// <summary>
        /// Loads save data from storage and restores all registered saveables.
        /// </summary>
        public void LoadAll()
        {
            if (_storage == null)
            {
                LogError("No valid storage backend assigned.");
                return;
            }

            if (!_storage.HasKey(_saveSlotKey))
            {
                LogInfo($"No save data found for '{_saveSlotKey}'.");
                return;
            }

            string json = _storage.Load(_saveSlotKey);
            if (string.IsNullOrEmpty(json)) return;

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return;

            int restoredCount = 0;

            for (int i = 0; i < _registeredSaveables.Count; i++)
            {
                if (_registeredSaveables[i] is ISaveable saveable)
                {
                    string state = data.Get(saveable.SaveKey);
                    if (state != null)
                    {
                        saveable.RestoreState(state);
                        restoredCount++;
                    }
                }
            }

            LogInfo($"Restored {restoredCount} entries from '{_saveSlotKey}'.");

            if (_eventChannel != null && _eventChannel.OnLoadCompleted != null)
            {
                _eventChannel.OnLoadCompleted.Raise();
            }
        }

        /// <summary>
        /// Deletes all save data from the storage backend.
        /// </summary>
        public void DeleteSave()
        {
            if (_storage == null) return;

            _storage.Delete(_saveSlotKey);
            LogInfo($"Deleted save data for '{_saveSlotKey}'.");
        }

        /// <summary>
        /// Returns true if save data exists for the current slot.
        /// </summary>
        public bool HasSaveData()
        {
            return _storage != null && _storage.HasKey(_saveSlotKey);
        }

        /// <summary>
        /// Changes the active save slot key at runtime (for multiple save profiles).
        /// </summary>
        public void SetSaveSlot(string slotKey)
        {
            _saveSlotKey = slotKey;
        }

        /// <summary>
        /// Performs module-specific initialization logic.
        /// </summary>
        protected override void OnInitialize()
        {
            if (_storageAsset is ISaveStorage storage)
            {
                _storage = storage;
            }
            else
            {
                LogError("Storage asset does not implement ISaveStorage.");
            }

            for (int i = 0; i < _registeredSaveables.Count; i++)
            {
                if (_registeredSaveables[i] != null && _registeredSaveables[i] is not ISaveable)
                {
                    LogWarning($"'{_registeredSaveables[i].name}' does not implement ISaveable. Skipping.");
                }
            }

            SubscribeToEvents();
        }

        /// <summary>
        /// Performs module-specific cleanup logic.
        /// </summary>
        protected override void OnDispose()
        {
            UnsubscribeFromEvents();
            _storage = null;
        }

        /// <summary>
        /// Subscribes to to events.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_eventChannel == null) return;

            if (_eventChannel.OnSaveRequested != null)
                _eventChannel.OnSaveRequested.RegisterListener(SaveAll);

            if (_eventChannel.OnLoadRequested != null)
                _eventChannel.OnLoadRequested.RegisterListener(LoadAll);

            if (_eventChannel.OnDeleteRequested != null)
                _eventChannel.OnDeleteRequested.RegisterListener(DeleteSave);
        }

        /// <summary>
        /// Unsubscribes from from events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_eventChannel == null) return;

            if (_eventChannel.OnSaveRequested != null)
                _eventChannel.OnSaveRequested.UnregisterListener(SaveAll);

            if (_eventChannel.OnLoadRequested != null)
                _eventChannel.OnLoadRequested.UnregisterListener(LoadAll);

            if (_eventChannel.OnDeleteRequested != null)
                _eventChannel.OnDeleteRequested.UnregisterListener(DeleteSave);
        }
    }
}
