using System.IO;
using UnityEngine;

namespace EventHorizon.Save
{
    /// <summary>
    /// ISaveStorage implementation backed by the local file system.
    /// Stores each key as a separate JSON file under Application.persistentDataPath.
    /// Best for larger save data (inventories, world state, full game saves).
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Save/File Storage", order = 2)]
    public class FileSaveStorage : ScriptableObject, ISaveStorage
    {
        [Tooltip("Subfolder under persistentDataPath for save files.")]
        [SerializeField] private string _subfolder = "Saves";

        [Tooltip("File extension for save files.")]
        [SerializeField] private string _extension = ".json";

        /// <summary>
        /// Saves data to a file.
        /// </summary>
        public void Save(string key, string data)
        {
            string path = GetFilePath(key);
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, data);
        }

        /// <summary>
        /// Loads data from a file. Returns null if the file does not exist.
        /// </summary>
        public string Load(string key)
        {
            string path = GetFilePath(key);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        /// <summary>
        /// Returns true if the save file exists.
        /// </summary>
        public bool HasKey(string key)
        {
            return File.Exists(GetFilePath(key));
        }

        /// <summary>
        /// Deletes the save file for the given key.
        /// </summary>
        public void Delete(string key)
        {
            string path = GetFilePath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Deletes all save files in the subfolder.
        /// </summary>
        public void DeleteAll()
        {
            string directory = GetSaveDirectory();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>
        /// Gets save directory.
        /// </summary>
        private string GetSaveDirectory()
        {
            return Path.Combine(Application.persistentDataPath, _subfolder);
        }

        /// <summary>
        /// Gets file path.
        /// </summary>
        private string GetFilePath(string key)
        {
            string sanitized = SanitizeFileName(key);
            return Path.Combine(GetSaveDirectory(), sanitized + _extension);
        }

        /// <summary>
        /// Executes sanitize file name.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }

            return name;
        }
    }
}
