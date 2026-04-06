using System;
using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// Abstract base for all framework modules. Implements the lifecycle contract
    /// with sealed public methods to guarantee registry-safe sequencing.
    /// Subclasses override the protected virtual On* hooks to add behavior.
    /// </summary>
    public abstract class ModuleBase : ScriptableObject, IModuleLifecycle
    {
        /// <summary>
        /// Fired after the module has been initialized.
        /// </summary>
        public event Action OnInitialized;

        /// <summary>
        /// Fired after the module has been activated.
        /// </summary>
        public event Action OnActivated;

        /// <summary>
        /// Fired after the module has been deactivated.
        /// </summary>
        public event Action OnDeactivated;

        /// <summary>
        /// Fired after the module has been disposed.
        /// </summary>
        public event Action OnDisposed;

        /// <summary>
        /// Whether this module is currently active.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Initializes the module. Sealed to protect the lifecycle contract.
        /// </summary>
        public void Initialize()
        {
            OnInitialize();
            LogInfo($"{name} initialized.");
            OnInitialized?.Invoke();
        }

        /// <summary>
        /// Activates the module. Sealed to protect the lifecycle contract.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            OnActivate();
            LogInfo($"{name} activated.");
            OnActivated?.Invoke();
        }

        /// <summary>
        /// Deactivates the module. Sealed to protect the lifecycle contract.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            OnDeactivate();
            LogInfo($"{name} deactivated.");
            OnDeactivated?.Invoke();
        }

        /// <summary>
        /// Disposes the module. Sealed to protect the lifecycle contract.
        /// </summary>
        public void Dispose()
        {
            OnDispose();
            LogInfo($"{name} disposed.");
            OnDisposed?.Invoke();
        }

        /// <summary>
        /// Executes log info.
        /// </summary>
        protected void LogInfo(string message)
        {
            SingularityConsole.Log(this, message);
        }

        /// <summary>
        /// Executes log warning.
        /// </summary>
        protected void LogWarning(string message)
        {
            SingularityConsole.LogWarning(this, message);
        }

        /// <summary>
        /// Executes log error.
        /// </summary>
        protected void LogError(string message)
        {
            SingularityConsole.LogError(this, message);
        }

        /// <summary>
        /// Override to perform custom initialization logic.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Override to perform custom activation logic.
        /// </summary>
        protected virtual void OnActivate() { }

        /// <summary>
        /// Override to perform custom deactivation logic.
        /// </summary>
        protected virtual void OnDeactivate() { }

        /// <summary>
        /// Override to perform custom disposal and cleanup logic.
        /// </summary>
        protected virtual void OnDispose() { }
    }
}
