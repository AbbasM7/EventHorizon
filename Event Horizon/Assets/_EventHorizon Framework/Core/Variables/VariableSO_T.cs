using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// Generic base for typed ScriptableObject variables.
    /// Stores an initial value and a runtime working copy that resets on play-mode entry.
    /// Optionally raises a typed event when the runtime value changes.
    /// </summary>
    public abstract class VariableSO<T> : ScriptableObject
    {
        [Tooltip("The design-time value. Never mutated at runtime.")] [SerializeField]
        private T _initialValue;

        [Tooltip("Optional typed event raised whenever RuntimeValue changes.")] [SerializeField]
        private GameEventSO<T> _onValueChanged;

        [Tooltip("The runtime value. This is the value that changes at runtime.")] [SerializeField]
        private T _runtimeValue;

        /// <summary>
        /// The design-time initial value configured in the Inspector.
        /// </summary>
        public T InitialValue => _initialValue;

        /// <summary>
        /// The working runtime copy. Reset to InitialValue on OnEnable.
        /// </summary>
        public T RuntimeValue
        {
            get => _runtimeValue;
            private set => _runtimeValue = value;
        }

        /// <summary>
        /// Optional event raised when RuntimeValue changes via SetValue.
        /// </summary>
        public GameEventSO<T> OnValueChanged => _onValueChanged;

        /// <summary>
        /// Sets the runtime value and raises the OnValueChanged event if assigned.
        /// </summary>
        public void SetValue(T value)
        {
            RuntimeValue = value;

            if (_onValueChanged != null)
            {
                _onValueChanged.Raise(value);
            }
        }

        /// <summary>
        /// Binds state when the component is enabled.
        /// </summary>
        private void OnEnable()
        {
            RuntimeValue = _initialValue;
        }
    }
}
