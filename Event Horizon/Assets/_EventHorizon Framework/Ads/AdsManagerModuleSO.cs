using System.Collections.Generic;
using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Ads
{
    [CreateAssetMenu(menuName = "EventHorizon/Ads/Ads Manager Module", order = 0)]
    public class AdsManagerModuleSO : ModuleBase, IAdsManager
    {
        [Header("Providers")]
        [SerializeField] private List<AdsProviderManagerBaseSO> _providers = new List<AdsProviderManagerBaseSO>();

        [Header("Events")]
        [SerializeField] private AdsEventChannelSO _eventChannel;

        [Header("Bootstrap")]
        [SerializeField] private List<AdPlacementDefinitionSO> _preloadOnInitialize = new List<AdPlacementDefinitionSO>();

        private bool _initializationStarted;
        private bool _initializationCompleted;
        private bool _initializationFailed;

        /// <summary>
        /// Performs module-specific initialization logic.
        /// </summary>
        protected override void OnInitialize()
        {
            SortProvidersByPriority();
            SubscribeToEvents();
            SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] Initializing AdsManagerModuleSO with {_providers.Count} configured providers.", this);
            BeginInitializationAsync();
        }

        /// <summary>
        /// Performs module-specific cleanup logic.
        /// </summary>
        protected override void OnDispose()
        {
            UnsubscribeFromEvents();

            for (int i = 0; i < _providers.Count; i++)
            {
                SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] Disposing provider at index {i}: {_providers[i]?.ProviderId ?? "null"}.", this);
                _providers[i]?.DisposeProvider();
            }

            _initializationStarted = false;
            _initializationCompleted = false;
            _initializationFailed = false;
        }

        /// <summary>
        /// Determines whether it can show async.
        /// </summary>
        public Awaitable<bool> CanShowAsync(AdPlacementDefinitionSO placement) => CanShowAsync(AdRequestContext.ForPlacement(placement));

        /// <summary>
        /// Determines whether it can show async.
        /// </summary>
        public async Awaitable<bool> CanShowAsync(AdRequestContext request)
        {
            AdPlacementDefinitionSO placement = request.Placement;
            if (placement == null)
            {
                SingularityConsole.LogWarning(EventHorizonLogModule.Ads, "[Central] CanShowAsync called with a null placement.", this);
                return false;
            }

            SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] CanShowAsync requested for placement '{placement.PlacementId}' ({placement.Format}), source='{request.SourceId}', flow='{request.FlowId}', reason='{request.Reason}'.", this);
            await EnsureInitializedAsync();

            for (int i = 0; i < _providers.Count; i++)
            {
                AdsProviderManagerBaseSO provider = _providers[i];
                if (provider == null || !provider.Supports(placement))
                {
                    continue;
                }

                if (CanProviderShow(placement, provider.GetAvailability(placement)))
                {
                    RaiseAvailabilityChanged(placement, provider);
                    SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] Provider '{provider.ProviderId}' is already ready for placement '{placement.PlacementId}'.", this);
                    RaiseTelemetry(AdTelemetryEvent.From(provider.ProviderId, request, true, true, false, false, false, string.Empty));
                    return true;
                }
            }

            for (int i = 0; i < _providers.Count; i++)
            {
                AdsProviderManagerBaseSO provider = _providers[i];
                if (provider == null || !provider.Supports(placement))
                {
                    continue;
                }

                AdLoadResult loadResult = await provider.PreloadAsync(request);
                RaiseLoadCompleted(loadResult);
                RaiseAvailabilityChanged(placement, provider);
                SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] Preload attempt from provider '{provider.ProviderId}' for placement '{placement.PlacementId}' completed with success={loadResult.IsSuccess}.", this);
                RaiseTelemetry(AdTelemetryEvent.From(provider.ProviderId, request, loadResult.IsSuccess, loadResult.IsSuccess, false, false, false, loadResult.FailureReason));

                if (loadResult.IsSuccess && CanProviderShow(placement, provider.GetAvailability(placement)))
                {
                    return true;
                }
            }

            RaiseTelemetry(AdTelemetryEvent.From(string.Empty, request, false, false, false, false, false, "No provider available."));
            return false;
        }

        /// <summary>
        /// Executes preload async asynchronously.
        /// </summary>
        public Awaitable PreloadAsync(AdPlacementDefinitionSO placement) => PreloadAsync(AdRequestContext.ForPlacement(placement));

        /// <summary>
        /// Executes preload async asynchronously.
        /// </summary>
        public async Awaitable PreloadAsync(AdRequestContext request)
        {
            AdPlacementDefinitionSO placement = request.Placement;
            if (placement == null)
            {
                SingularityConsole.LogWarning(EventHorizonLogModule.Ads, "[Central] PreloadAsync called with a null placement.", this);
                return;
            }

            SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] PreloadAsync requested for placement '{placement.PlacementId}' ({placement.Format}).", this);
            await EnsureInitializedAsync();

            for (int i = 0; i < _providers.Count; i++)
            {
                AdsProviderManagerBaseSO provider = _providers[i];
                if (provider == null || !provider.Supports(placement))
                {
                    continue;
                }

                AdLoadResult loadResult = await provider.PreloadAsync(request);
                RaiseLoadCompleted(loadResult);
                RaiseAvailabilityChanged(placement, provider);
                SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] Provider '{provider.ProviderId}' preload result for placement '{placement.PlacementId}': success={loadResult.IsSuccess}, reason='{loadResult.FailureReason}'.", this);
                RaiseTelemetry(AdTelemetryEvent.From(provider.ProviderId, request, loadResult.IsSuccess, loadResult.IsSuccess, false, false, false, loadResult.FailureReason));

                if (loadResult.IsSuccess)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Shows async.
        /// </summary>
        public Awaitable<AdShowResult> ShowAsync(AdPlacementDefinitionSO placement) => ShowAsync(AdRequestContext.ForPlacement(placement));

        /// <summary>
        /// Shows async.
        /// </summary>
        public async Awaitable<AdShowResult> ShowAsync(AdRequestContext request)
        {
            AdPlacementDefinitionSO placement = request.Placement;
            if (placement == null)
            {
                SingularityConsole.LogError(EventHorizonLogModule.Ads, "[Central] ShowAsync called with a null placement.", this);
                return AdShowResult.Failed(request, string.Empty, "No placement provided.");
            }

            SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] ShowAsync requested for placement '{placement.PlacementId}' ({placement.Format}), source='{request.SourceId}', flow='{request.FlowId}', reason='{request.Reason}'.", this);
            await EnsureInitializedAsync();
            AdShowResult lastFailure = AdShowResult.Failed(request, string.Empty, $"No provider could show placement '{placement.PlacementId}'.");

            for (int i = 0; i < _providers.Count; i++)
            {
                AdsProviderManagerBaseSO provider = _providers[i];
                if (provider == null || !provider.Supports(placement))
                {
                    continue;
                }

                AdAvailabilityState availability = provider.GetAvailability(placement);
                if (!CanProviderShow(placement, availability))
                {
                    AdLoadResult loadResult = await provider.PreloadAsync(request);
                    RaiseLoadCompleted(loadResult);
                    RaiseAvailabilityChanged(placement, provider);
                    RaiseTelemetry(AdTelemetryEvent.From(provider.ProviderId, request, loadResult.IsSuccess, loadResult.IsSuccess, false, false, false, loadResult.FailureReason));

                    if (!loadResult.IsSuccess)
                    {
                        SingularityConsole.LogWarning(EventHorizonLogModule.Ads, $"[Central] Provider '{provider.ProviderId}' failed preload before show for placement '{placement.PlacementId}': {loadResult.FailureReason}", this);
                        continue;
                    }
                }

                if (!await provider.CanShowAsync(request))
                {
                    RaiseAvailabilityChanged(placement, provider);
                    RaiseTelemetry(AdTelemetryEvent.From(provider.ProviderId, request, false, true, false, false, false, "Provider reported unavailable before show."));
                    SingularityConsole.LogWarning(EventHorizonLogModule.Ads, $"[Central] Provider '{provider.ProviderId}' reported unavailable immediately before show for placement '{placement.PlacementId}'.", this);
                    continue;
                }

                _eventChannel?.OnShowStarted?.Raise(request);
                SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] Starting show with provider '{provider.ProviderId}' for placement '{placement.PlacementId}'.", this);
                AdShowResult showResult = await provider.ShowAsync(request);
                RaiseAvailabilityChanged(placement, provider);
                RaiseTelemetry(AdTelemetryEvent.From(showResult.ProviderId, request, true, true, showResult.WasShown, showResult.WasCompleted, showResult.RewardEarned, showResult.FailureReason));

                if (showResult.WasShown)
                {
                    SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] Provider '{provider.ProviderId}' completed show for placement '{placement.PlacementId}'. completed={showResult.WasCompleted}, rewardEarned={showResult.RewardEarned}.", this);
                    RaiseShowCompleted(showResult);
                    return showResult;
                }

                SingularityConsole.LogWarning(EventHorizonLogModule.Ads, $"[Central] Provider '{provider.ProviderId}' failed to show placement '{placement.PlacementId}'. reason='{showResult.FailureReason}'. Falling back if possible.", this);
                lastFailure = showResult;
            }

            SingularityConsole.LogWarning(EventHorizonLogModule.Ads, $"[Central] All providers failed for placement '{placement.PlacementId}'. Final reason='{lastFailure.FailureReason}'.", this);
            RaiseTelemetry(AdTelemetryEvent.From(lastFailure.ProviderId, request, false, false, lastFailure.WasShown, lastFailure.WasCompleted, lastFailure.RewardEarned, lastFailure.FailureReason));
            RaiseShowCompleted(lastFailure);
            return lastFailure;
        }

        /// <summary>
        /// Executes hide async asynchronously.
        /// </summary>
        public Awaitable HideAsync(AdPlacementDefinitionSO placement) => HideAsync(AdRequestContext.ForPlacement(placement));

        /// <summary>
        /// Executes hide async asynchronously.
        /// </summary>
        public async Awaitable HideAsync(AdRequestContext request)
        {
            AdPlacementDefinitionSO placement = request.Placement;
            if (placement == null)
            {
                SingularityConsole.LogWarning(EventHorizonLogModule.Ads, "[Central] HideAsync called with a null placement.", this);
                return;
            }

            await EnsureInitializedAsync();

            for (int i = 0; i < _providers.Count; i++)
            {
                AdsProviderManagerBaseSO provider = _providers[i];
                if (provider == null || !provider.Supports(placement))
                {
                    continue;
                }

                await provider.HideAsync(request);
                RaiseAvailabilityChanged(placement, provider);
            }
        }

        /// <summary>
        /// Gets availability async.
        /// </summary>
        public Awaitable<AdAvailabilityState> GetAvailabilityAsync(AdPlacementDefinitionSO placement) => GetAvailabilityAsync(AdRequestContext.ForPlacement(placement));

        /// <summary>
        /// Gets availability async.
        /// </summary>
        public async Awaitable<AdAvailabilityState> GetAvailabilityAsync(AdRequestContext request)
        {
            AdPlacementDefinitionSO placement = request.Placement;
            if (placement == null)
            {
                SingularityConsole.LogWarning(EventHorizonLogModule.Ads, "[Central] GetAvailabilityAsync called with a null placement.", this);
                return AdAvailabilityState.Unavailable;
            }

            await EnsureInitializedAsync();

            AdAvailabilityState bestState = AdAvailabilityState.Unavailable;

            for (int i = 0; i < _providers.Count; i++)
            {
                AdsProviderManagerBaseSO provider = _providers[i];
                if (provider == null || !provider.Supports(placement))
                {
                    continue;
                }

                AdAvailabilityState state = provider.GetAvailability(placement);
                if (state > bestState)
                {
                    bestState = state;
                }
            }

            return bestState;
        }

        /// <summary>
        /// Begins initialization async.
        /// </summary>
        private async void BeginInitializationAsync()
        {
            if (_initializationStarted)
            {
                return;
            }

            _initializationStarted = true;
            _initializationCompleted = false;
            _initializationFailed = false;
            SingularityConsole.Log(EventHorizonLogModule.Ads, "[Central] Beginning provider initialization sequence.", this);

            for (int i = 0; i < _providers.Count; i++)
            {
                if (_providers[i] == null)
                {
                    SingularityConsole.LogWarning(EventHorizonLogModule.Ads, $"[Central] Null provider entry at index {i}.", this);
                    continue;
                }

                SingularityConsole.Log(EventHorizonLogModule.Ads, $"[Central] Initializing provider '{_providers[i].ProviderId}'.", this);
                await _providers[i].InitializeAsync();
            }

            _initializationCompleted = true;
            SingularityConsole.Log(EventHorizonLogModule.Ads, "[Central] Provider initialization sequence completed.", this);

            for (int i = 0; i < _preloadOnInitialize.Count; i++)
            {
                if (_preloadOnInitialize[i] == null || !_preloadOnInitialize[i].PreloadOnInitialize)
                {
                    continue;
                }

                await PreloadAsync(AdRequestContext.ForPlacement(_preloadOnInitialize[i]));
            }
        }

        /// <summary>
        /// Ensures initialized async is ready before continuing.
        /// </summary>
        private async Awaitable EnsureInitializedAsync()
        {
            if (!_initializationStarted)
            {
                BeginInitializationAsync();
            }

            while (!_initializationCompleted && !_initializationFailed)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        /// <summary>
        /// Subscribes to events.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.OnPreloadRequested?.RegisterListener(OnPreloadRequested);
            _eventChannel.OnShowRequested?.RegisterListener(OnShowRequested);
            _eventChannel.OnHideRequested?.RegisterListener(OnHideRequested);
        }

        /// <summary>
        /// Unsubscribes from events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.OnPreloadRequested?.UnregisterListener(OnPreloadRequested);
            _eventChannel.OnShowRequested?.UnregisterListener(OnShowRequested);
            _eventChannel.OnHideRequested?.UnregisterListener(OnHideRequested);
        }

        /// <summary>
        /// Handles the preload request.
        /// </summary>
        private async void OnPreloadRequested(AdRequestContext request)
        {
            await PreloadAsync(request);
        }

        /// <summary>
        /// Handles the show request.
        /// </summary>
        private async void OnShowRequested(AdRequestContext request)
        {
            await ShowAsync(request);
        }

        /// <summary>
        /// Handles the hide request.
        /// </summary>
        private async void OnHideRequested(AdRequestContext request)
        {
            await HideAsync(request);
        }

        /// <summary>
        /// Raises the availability changed event.
        /// </summary>
        private void RaiseAvailabilityChanged(AdPlacementDefinitionSO placement, AdsProviderManagerBaseSO provider)
        {
            if (_eventChannel?.OnAvailabilityChanged == null || placement == null || provider == null)
            {
                return;
            }

            AdAvailabilityState state = provider.GetAvailability(placement);

            _eventChannel.OnAvailabilityChanged.Raise(new AdAvailabilityChanged
            {
                Placement = placement,
                ProviderId = provider.ProviderId,
                State = state,
                CanShow = state == AdAvailabilityState.Ready
            });
        }

        /// <summary>
        /// Raises the load completed event.
        /// </summary>
        private void RaiseLoadCompleted(AdLoadResult result)
        {
            _eventChannel?.OnLoadCompleted?.Raise(result);
        }

        /// <summary>
        /// Raises the show completed event.
        /// </summary>
        private void RaiseShowCompleted(AdShowResult result)
        {
            _eventChannel?.OnShowCompleted?.Raise(result);
        }

        /// <summary>
        /// Raises the telemetry event.
        /// </summary>
        private void RaiseTelemetry(AdTelemetryEvent telemetry)
        {
            _eventChannel?.OnTelemetryReported?.Raise(telemetry);
        }

        /// <summary>
        /// Sorts providers by priority.
        /// </summary>
        private void SortProvidersByPriority()
        {
            _providers.Sort((left, right) =>
            {
                if (left == null && right == null) return 0;
                if (left == null) return 1;
                if (right == null) return -1;
                return left.Priority.CompareTo(right.Priority);
            });
        }

        /// <summary>
        /// Determines whether the provider can show the placement without another preload.
        /// </summary>
        private static bool CanProviderShow(AdPlacementDefinitionSO placement, AdAvailabilityState availability)
        {
            if (placement == null)
            {
                return false;
            }

            if (availability == AdAvailabilityState.Ready)
            {
                return true;
            }

            return IsPersistentAdFormat(placement.Format) && availability == AdAvailabilityState.Showing;
        }

        /// <summary>
        /// Determines whether the format represents a persistent ad view.
        /// </summary>
        private static bool IsPersistentAdFormat(AdFormat format)
        {
            return format == AdFormat.Banner || format == AdFormat.MRec;
        }
    }
}
