using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// MonoBehaviour that auto-registers and unregisters a component with a RuntimeSetSO.
    /// Attach to any GameObject alongside the target component.
    /// </summary>
    public abstract class RuntimeSetEntry<T> : MonoBehaviour where T : class
    {
        [Tooltip("The runtime set this object registers with.")]
        [SerializeField] private RuntimeSetSO<T> _runtimeSet;

        /// <summary>
        /// Override to provide the component instance to register.
        /// Defaults to GetComponent&lt;T&gt;().
        /// </summary>
        protected virtual T GetEntry()
        {
            return GetComponent<T>();
        }

        /// <summary>
        /// Binds state when the component is enabled.
        /// </summary>
        private void OnEnable()
        {
            if (_runtimeSet != null)
            {
                T entry = GetEntry();
                if (entry != null)
                {
                    _runtimeSet.Add(entry);
                }
            }
        }

        /// <summary>
        /// Releases bindings when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (_runtimeSet != null)
            {
                T entry = GetEntry();
                if (entry != null)
                {
                    _runtimeSet.Remove(entry);
                }
            }
        }
    }
}
