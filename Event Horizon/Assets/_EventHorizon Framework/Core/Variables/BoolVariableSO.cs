using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A ScriptableObject variable that holds a bool value at runtime.
    /// Implements ISaveable for persistence through the Save module.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Variables/Bool Variable", order = 2)]
    public class BoolVariableSO : VariableSO<bool>, ISaveable
    {
        /// <summary>Unique key for save/load operations.</summary>
        public string SaveKey => name;

        /// <summary>Captures the runtime value as JSON.</summary>
        public string CaptureState() => JsonUtility.ToJson(new SaveWrapper { Value = RuntimeValue });

        /// <summary>Restores the runtime value from JSON.</summary>
        public void RestoreState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            SetValue(JsonUtility.FromJson<SaveWrapper>(state).Value);
        }

        [System.Serializable]
        private struct SaveWrapper { public bool Value; }
    }
}
