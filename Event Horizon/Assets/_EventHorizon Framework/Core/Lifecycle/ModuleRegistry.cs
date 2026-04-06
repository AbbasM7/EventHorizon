using System.Collections.Generic;
using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// Central registry that holds all framework modules and orchestrates their
    /// lifecycle in the order defined in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Core/Module Registry", order = 0)]
    public class ModuleRegistry : ScriptableObject
    {
        [Tooltip("Ordered list of modules. Initialized top-to-bottom, disposed bottom-to-top.")]
        [SerializeField] private List<ModuleBase> _modules = new List<ModuleBase>();

        /// <summary>
        /// Initializes and then activates every registered module in order.
        /// </summary>
        public void InitializeAll()
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] == null)
                {
                    SingularityConsole.LogWarning<ModuleRegistry>($"Null module entry at index {i}. Skipping.");
                    continue;
                }

                _modules[i].Initialize();
                _modules[i].Activate();
            }
        }

        /// <summary>
        /// Deactivates and then disposes every registered module in reverse order.
        /// </summary>
        public void DisposeAll()
        {
            for (int i = _modules.Count - 1; i >= 0; i--)
            {
                if (_modules[i] == null) continue;

                _modules[i].Deactivate();
                _modules[i].Dispose();
            }
        }

        /// <summary>
        /// Last-resort lookup for a module by type. Prefer SO-based communication instead.
        /// </summary>
        public T GetModule<T>() where T : ModuleBase
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] is T typed)
                {
                    return typed;
                }
            }

            return null;
        }
    }
}
