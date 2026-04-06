using System;

namespace EventHorizon.Core
{
    /// <summary>
    /// A reference that holds either a constant int or an IntVariableSO.
    /// </summary>
    [Serializable]
    public class IntReference : VariableReference<int>
    {
        /// <summary>Creates a reference defaulting to constant mode.</summary>
        public IntReference() { }

        /// <summary>Creates a reference with an initial constant value.</summary>
        public IntReference(int constantValue) : base(constantValue) { }

        /// <summary>Creates a reference pointing to an IntVariableSO.</summary>
        public IntReference(IntVariableSO variable) : base(variable) { }
    }
}
