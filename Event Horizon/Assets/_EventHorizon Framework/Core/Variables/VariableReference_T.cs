using System;
using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A flexible reference that can be either a constant value or a VariableSO asset.
    /// Designers toggle between the two modes in the Inspector per-field.
    /// Eliminates the need for trivial SO assets that just hold a single fixed value.
    /// </summary>
    [Serializable]
    public class VariableReference<T>
    {
        [SerializeField] private bool _useConstant = true;
        [SerializeField] private T _constantValue;
        [SerializeField] private VariableSO<T> _variable;

        /// <summary>
        /// Whether this reference is using a constant value (true) or a VariableSO (false).
        /// </summary>
        public bool UseConstant
        {
            get => _useConstant;
            set => _useConstant = value;
        }

        /// <summary>
        /// The constant value used when UseConstant is true.
        /// </summary>
        public T ConstantValue
        {
            get => _constantValue;
            set => _constantValue = value;
        }

        /// <summary>
        /// The VariableSO asset used when UseConstant is false.
        /// </summary>
        public VariableSO<T> Variable
        {
            get => _variable;
            set => _variable = value;
        }

        /// <summary>
        /// Returns the current value — either the constant or the variable's RuntimeValue.
        /// </summary>
        public T Value
        {
            get => _useConstant ? _constantValue : (_variable != null ? _variable.RuntimeValue : default);
            set
            {
                if (_useConstant)
                {
                    _constantValue = value;
                }
                else if (_variable != null)
                {
                    _variable.SetValue(value);
                }
            }
        }

        /// <summary>
        /// Creates a reference defaulting to constant mode.
        /// </summary>
        public VariableReference() { }

        /// <summary>
        /// Creates a reference with an initial constant value.
        /// </summary>
        public VariableReference(T constantValue)
        {
            _useConstant = true;
            _constantValue = constantValue;
        }

        /// <summary>
        /// Creates a reference pointing to a VariableSO asset.
        /// </summary>
        public VariableReference(VariableSO<T> variable)
        {
            _useConstant = false;
            _variable = variable;
        }

        /// <summary>
        /// Implicit conversion to the value type for ergonomic usage.
        /// </summary>
        public static implicit operator T(VariableReference<T> reference)
        {
            return reference.Value;
        }
    }
}
