using System;

namespace EventHorizon.Core
{
    /// <summary>
    /// A reference that holds either a constant string or a StringVariableSO.
    /// </summary>
    [Serializable]
    public class StringReference : VariableReference<string>
    {
        /// <summary>Creates a reference defaulting to constant mode.</summary>
        public StringReference() { }

        /// <summary>Creates a reference with an initial constant value.</summary>
        public StringReference(string constantValue) : base(constantValue) { }

        /// <summary>Creates a reference pointing to a StringVariableSO.</summary>
        public StringReference(StringVariableSO variable) : base(variable) { }
    }
}
