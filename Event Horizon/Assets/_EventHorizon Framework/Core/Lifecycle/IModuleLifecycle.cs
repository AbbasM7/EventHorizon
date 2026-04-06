namespace EventHorizon.Core
{
    /// <summary>
    /// Defines the lifecycle contract for all framework modules.
    /// </summary>
    public interface IModuleLifecycle
    {
        /// <summary>
        /// Initializes the module, preparing internal state and resources.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Activates the module, enabling runtime behavior.
        /// </summary>
        void Activate();

        /// <summary>
        /// Deactivates the module, pausing runtime behavior without releasing resources.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Disposes the module, releasing all resources and cleaning up state.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Whether the module is currently active.
        /// </summary>
        bool IsActive { get; }
    }
}
