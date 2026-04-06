using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// Scene-side entry point that drives module lifecycle.
    /// Place on a single GameObject in your bootstrap scene.
    /// Contains zero game logic â€” pure lifecycle relay only.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ModuleBootstrapper : MonoBehaviour
    {
        [Tooltip("The Module Registry asset containing all framework modules.")]
        [SerializeField] private ModuleRegistry _registry;

        /// <summary>
        /// Public accessor for the registry.
        /// </summary>
        public ModuleRegistry Registry => _registry;

        /// <summary>
        /// Initializes references when the component awakens.
        /// </summary>
        private void Awake()
        {
            if (_registry != null)
            {
                _registry.InitializeAll();
            }
            else
            {
                SingularityConsole.LogError<ModuleBootstrapper>("No ModuleRegistry assigned.");
            }
        }

        /// <summary>
        /// Handles destroy.
        /// </summary>
        private void OnDestroy()
        {
            if (_registry != null)
            {
                _registry.DisposeAll();
            }
        }
    }
}
