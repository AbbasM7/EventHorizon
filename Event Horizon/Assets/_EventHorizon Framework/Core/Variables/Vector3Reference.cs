using System;
using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A reference that holds either a constant Vector3 or a Vector3VariableSO.
    /// </summary>
    [Serializable]
    public class Vector3Reference : VariableReference<Vector3>
    {
        /// <summary>Creates a reference defaulting to constant mode.</summary>
        public Vector3Reference() { }

        /// <summary>Creates a reference with an initial constant value.</summary>
        public Vector3Reference(Vector3 constantValue) : base(constantValue) { }

        /// <summary>Creates a reference pointing to a Vector3VariableSO.</summary>
        public Vector3Reference(Vector3VariableSO variable) : base(variable) { }
    }
}
