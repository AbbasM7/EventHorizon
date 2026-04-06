using UnityEngine;

namespace EventHorizon.Save
{
    /// <summary>
    /// ISaveStorage implementation backed by Unity's PlayerPrefs.
    /// Simple and cross-platform. Best for small save data (settings, progress flags).
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Save/PlayerPrefs Storage", order = 1)]
    public class PlayerPrefsSaveStorage : ScriptableObject, ISaveStorage
    {
        [Tooltip("Optional prefix prepended to all keys to avoid collisions.")]
        [SerializeField] private string _keyPrefix = "GDF_";

        /// <summary>
        /// Saves data to PlayerPrefs.
        /// </summary>
        public void Save(string key, string data)
        {
            PlayerPrefs.SetString(GetPrefixedKey(key), data);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads data from PlayerPrefs. Returns null if the key does not exist.
        /// </summary>
        public string Load(string key)
        {
            string prefixed = GetPrefixedKey(key);
            return PlayerPrefs.HasKey(prefixed) ? PlayerPrefs.GetString(prefixed) : null;
        }

        /// <summary>
        /// Returns true if the key exists in PlayerPrefs.
        /// </summary>
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(GetPrefixedKey(key));
        }

        /// <summary>
        /// Deletes a key from PlayerPrefs.
        /// </summary>
        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(GetPrefixedKey(key));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Deletes all PlayerPrefs data. Use with caution.
        /// </summary>
        public void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Gets prefixed key.
        /// </summary>
        private string GetPrefixedKey(string key)
        {
            return string.IsNullOrEmpty(_keyPrefix) ? key : _keyPrefix + key;
        }
    }
}
