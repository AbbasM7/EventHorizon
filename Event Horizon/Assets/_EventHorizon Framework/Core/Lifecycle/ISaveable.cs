namespace EventHorizon.Core
{
    /// <summary>
    /// Contract for any object that can capture and restore its state as a string.
    /// Implemented by ScriptableObjects, MonoBehaviours, or plain classes that
    /// participate in the save system.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique key identifying this saveable's data slot.
        /// Must be stable across sessions (e.g., asset name or a GUID).
        /// </summary>
        string SaveKey { get; }

        /// <summary>
        /// Captures the current state as a serialized string (typically JSON).
        /// </summary>
        string CaptureState();

        /// <summary>
        /// Restores state from a previously captured serialized string.
        /// </summary>
        void RestoreState(string state);
    }
}
