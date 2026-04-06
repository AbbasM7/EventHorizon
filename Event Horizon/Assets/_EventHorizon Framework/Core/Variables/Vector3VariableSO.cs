using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A ScriptableObject variable that holds a Vector3 value at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Variables/Vector3 Variable", order = 4)]
    public class Vector3VariableSO : VariableSO<Vector3>
    {
    }
}
