using System.Collections.Generic;
using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Ads
{
    public abstract class AdsProviderManagerBaseSO : ScriptableObject
    {
        [SerializeField] private int _priority = 0;
        [SerializeField] private float _defaultLoadTimeoutSeconds = 5f;
        [SerializeField] private List<AdProviderAdUnitBinding> _bindings = new List<AdProviderAdUnitBinding>();

        public abstract string ProviderId { get; }
        public int Priority => _priority;
        public bool IsInitialized { get; protected set; }
        protected IReadOnlyList<AdProviderAdUnitBinding> Bindings => _bindings;

        /// <summary>
        /// Executes supports.
        /// </summary>
        public virtual bool Supports(AdPlacementDefinitionSO placement)
        {
            return TryGetBinding(placement, out _);
        }

        public abstract Awaitable InitializeAsync();
        public abstract Awaitable<AdLoadResult> PreloadAsync(AdRequestContext request);
        public abstract Awaitable<bool> CanShowAsync(AdRequestContext request);
        public abstract Awaitable<AdShowResult> ShowAsync(AdRequestContext request);
        public abstract Awaitable HideAsync(AdRequestContext request);
        public abstract AdAvailabilityState GetAvailability(AdPlacementDefinitionSO placement);
        public abstract void DisposeProvider();

        /// <summary>
        /// Executes log info.
        /// </summary>
        protected void LogInfo(string message)
        {
            SingularityConsole.Log(EventHorizonLogModule.Ads, $"[{ProviderId}] {message}", this);
        }

        /// <summary>
        /// Executes log warning.
        /// </summary>
        protected void LogWarning(string message)
        {
            SingularityConsole.LogWarning(EventHorizonLogModule.Ads, $"[{ProviderId}] {message}", this);
        }

        /// <summary>
        /// Executes log error.
        /// </summary>
        protected void LogError(string message)
        {
            SingularityConsole.LogError(EventHorizonLogModule.Ads, $"[{ProviderId}] {message}", this);
        }

        /// <summary>
        /// Tries to get t binding.
        /// </summary>
        protected bool TryGetBinding(AdPlacementDefinitionSO placement, out AdProviderAdUnitBinding binding)
        {
            binding = null;
            if (placement == null)
            {
                return false;
            }

            for (int i = 0; i < _bindings.Count; i++)
            {
                if (_bindings[i] != null && _bindings[i].Placement == placement && !string.IsNullOrWhiteSpace(_bindings[i].AdUnitId))
                {
                    binding = _bindings[i];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets load timeout.
        /// </summary>
        protected float GetLoadTimeout(AdPlacementDefinitionSO placement)
        {
            if (placement != null && placement.LoadTimeoutSeconds > 0f)
            {
                return placement.LoadTimeoutSeconds;
            }

            return _defaultLoadTimeoutSeconds;
        }

        /// <summary>
        /// Executes wait until async asynchronously.
        /// </summary>
        protected static async Awaitable<bool> WaitUntilAsync(System.Func<bool> predicate, float timeoutSeconds)
        {
            float startTime = Time.realtimeSinceStartup;

            while (!predicate())
            {
                if (timeoutSeconds > 0f && Time.realtimeSinceStartup - startTime >= timeoutSeconds)
                {
                    return false;
                }

                await Awaitable.NextFrameAsync();
            }

            return true;
        }
    }
}
