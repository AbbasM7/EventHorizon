namespace EventHorizon.Save
{
    /// <summary>
    /// Contract for pluggable storage backends (PlayerPrefs, file system, cloud, etc.).
    /// The SaveModuleSO delegates all persistence to this interface.
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>
        /// Saves a key-value pair to persistent storage.
        /// </summary>
        void Save(string key, string data);

        /// <summary>
        /// Loads a value from persistent storage. Returns null if the key does not exist.
        /// </summary>
        string Load(string key);

        /// <summary>
        /// Returns true if the key exists in persistent storage.
        /// </summary>
        bool HasKey(string key);

        /// <summary>
        /// Deletes a key from persistent storage.
        /// </summary>
        void Delete(string key);

        /// <summary>
        /// Deletes all data from this storage backend.
        /// </summary>
        void DeleteAll();
    }
}
