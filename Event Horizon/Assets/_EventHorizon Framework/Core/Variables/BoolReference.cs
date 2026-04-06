using System;

namespace EventHorizon.Core
{
    /// <summary>
    /// A reference that holds either a constant bool or a BoolVariableSO.
    /// </summary>
    [Serializable]
    public class BoolReference : VariableReference<bool>
    {
        /// <summary>Creates a reference defaulting to constant mode.</summary>
        public BoolReference() { }

        /// <summary>Creates a reference with an initial constant value.</summary>
        public BoolReference(bool constantValue) : base(constantValue) { }

        /// <summary>Creates a reference pointing to a BoolVariableSO.</summary>
        public BoolReference(BoolVariableSO variable) : base(variable) { }
    }
}
