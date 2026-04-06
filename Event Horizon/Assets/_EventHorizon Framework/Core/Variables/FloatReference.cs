using System;

namespace EventHorizon.Core
{
    /// <summary>
    /// A reference that holds either a constant float or a FloatVariableSO.
    /// </summary>
    [Serializable]
    public class FloatReference : VariableReference<float>
    {
        /// <summary>Creates a reference defaulting to constant mode.</summary>
        public FloatReference() { }

        /// <summary>Creates a reference with an initial constant value.</summary>
        public FloatReference(float constantValue) : base(constantValue) { }

        /// <summary>Creates a reference pointing to a FloatVariableSO.</summary>
        public FloatReference(FloatVariableSO variable) : base(variable) { }
    }
}
