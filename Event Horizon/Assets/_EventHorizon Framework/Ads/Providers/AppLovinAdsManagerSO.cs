using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EventHorizon.Ads
{
    [CreateAssetMenu(menuName = "EventHorizon/Ads/Providers/AppLovin Ads Manager", order = 0)]
    public class AppLovinAdsManagerSO : AdsProviderManagerBaseSO
    {
        private const float InitializationTimeoutSeconds = 10f;

        private readonly Dictionary<string, AdUnitRuntimeState> _states = new Dictionary<string, AdUnitRuntimeState>();
        private Type _maxSdkType;
        private Type _maxSdkCallbacksType;
        private Type _adViewPositionType;
        private Type _adViewConfigurationType;
        private object _interstitialCallbacksSource;
        private object _rewardedCallbacksSource;
        private object _bannerCallbacksSource;
        private object _mrecCallbacksSource;
        private bool _sdkAvailable;
        private bool _callbacksSubscribed;
        private bool _initializationStarted;
        private bool _initializationInProgress;

        public override string ProviderId => "AppLovin";

        /// <summary>
        /// Executes initialize async asynchronously.
        /// </summary>
        public override async Awaitable InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            ResolveSdk();
            if (!_sdkAvailable)
            {
                LogWarning("SDK not found. Provider will remain inactive until AppLovin MAX is installed.");
                return;
            }

            SubscribeCallbacks();

            if (_initializationStarted)
            {
                while (_initializationInProgress)
                {
                    await Awaitable.NextFrameAsync();
                }

                return;
            }

            _initializationStarted = true;
            _initializationInProgress = true;

            List<string> adUnitIds = new List<string>();
            for (int i = 0; i < Bindings.Count; i++)
            {
                if (Bindings[i] != null && !string.IsNullOrWhiteSpace(Bindings[i].AdUnitId))
                {
                    adUnitIds.Add(Bindings[i].AdUnitId);
                }
            }

            LogInfo($"Initializing SDK with {adUnitIds.Count} ad units.");
            AdsSdkReflection.InvokeStatic(_maxSdkType, "InitializeSdk", adUnitIds.Count > 0 ? adUnitIds.ToArray() : null);

            bool initialized = await WaitUntilAsync(() => IsInitialized, InitializationTimeoutSeconds);
            _initializationInProgress = false;

            if (!initialized)
            {
                LogWarning("Initialization timed out.");
            }
        }

        /// <summary>
        /// Executes preload async asynchronously.
        /// </summary>
        public override async Awaitable<AdLoadResult> PreloadAsync(AdRequestContext request)
        {
            AdPlacementDefinitionSO placement = request.Placement;
            if (!ValidatePlacement(placement, out AdProviderAdUnitBinding binding))
            {
                return AdLoadResult.Failed(request, ProviderId, "Placement is not bound for AppLovin.");
            }

            await InitializeAsync();
            if (!_sdkAvailable)
            {
                return AdLoadResult.Failed(request, ProviderId, "SDK unavailable.");
            }

            AdUnitRuntimeState state = GetState(binding.AdUnitId);
            if (IsReadyForFormat(state, binding.AdUnitId, placement.Format))
            {
                state.Availability = state.IsVisible && IsPersistentFormat(placement.Format)
                    ? AdAvailabilityState.Showing
                    : AdAvailabilityState.Ready;
                LogInfo($"Preload skipped for '{placement.PlacementId}'. Ad already ready.");
                return AdLoadResult.Success(request, ProviderId);
            }

            ResetLoadState(state);
            state.Availability = AdAvailabilityState.Loading;

            LogInfo($"Loading {placement.Format} for placement '{placement.PlacementId}' using ad unit '{binding.AdUnitId}'.");
            LoadAd(binding.AdUnitId, placement);

            bool completed = await WaitUntilAsync(() => state.LoadCompleted, GetLoadTimeout(placement));
            if (!completed || !state.LoadSucceeded)
            {
                state.Availability = AdAvailabilityState.Unavailable;
                string reason = completed ? state.FailureReason : "Load timed out.";
                LogWarning($"Load failed for placement '{placement.PlacementId}'. Reason='{reason}'.");
                return AdLoadResult.Failed(request, ProviderId, reason);
            }

            LogInfo($"Load completed for placement '{placement.PlacementId}'.");
            return AdLoadResult.Success(request, ProviderId);
        }

        /// <summary>
        /// Determines whether it can show async.
        /// </summary>
        public override async Awaitable<bool> CanShowAsync(AdRequestContext request)
        {
            AdPlacementDefinitionSO placement = request.Placement;
            if (!ValidatePlacement(placement, out AdProviderAdUnitBinding binding))
            {
                return false;
            }

            await InitializeAsync();
            if (!_sdkAvailable)
            {
                return false;
            }

            AdUnitRuntimeState state = GetState(binding.AdUnitId);
            bool canShow = IsReadyForFormat(state, binding.AdUnitId, placement.Format);
            if (canShow)
            {
                state.Availability = state.IsVisible && IsPersistentFormat(placement.Format)
                    ? AdAvailabilityState.Showing
                    : AdAvailabilityState.Ready;
            }

            LogInfo($"CanShow check for '{placement.PlacementId}' returned {canShow}.");
            return canShow;
        }

        /// <summary>
        /// Shows async.
        /// </summary>
        public override async Awaitable<AdShowResult> ShowAsync(AdRequestContext request)
        {
            AdPlacementDefinitionSO placement = request.Placement;
            if (!ValidatePlacement(placement, out AdProviderAdUnitBinding binding))
            {
                return AdShowResult.Failed(request, ProviderId, "Placement is not bound for AppLovin.");
            }

            if (!await CanShowAsync(request))
            {
                AdLoadResult loadResult = await PreloadAsync(request);
                if (!loadResult.IsSuccess || !await CanShowAsync(request))
                {
                    return AdShowResult.Failed(request, ProviderId, loadResult.FailureReason);
                }
            }

            AdUnitRuntimeState state = GetState(binding.AdUnitId);
            if (IsPersistentFormat(placement.Format))
            {
                ShowPersistentAd(binding.AdUnitId, placement.Format);
                state.IsVisible = true;
                state.Displayed = true;
                state.Hidden = false;
                state.Availability = AdAvailabilityState.Showing;

                return new AdShowResult
                {
                    Request = request,
                    Placement = placement,
                    ProviderId = ProviderId,
                    Format = placement.Format,
                    WasShown = true,
                    WasCompleted = true,
                    RewardEarned = false,
                    Status = AdShowStatus.Completed,
                    FailureReason = string.Empty,
                    RewardAmount = 0,
                    RewardLabel = string.Empty
                };
            }

            ResetShowState(state);
            state.Availability = AdAvailabilityState.Showing;

            LogInfo($"Showing {placement.Format} for placement '{placement.PlacementId}'.");
            ShowAd(binding.AdUnitId, placement);

            bool completed = await WaitUntilAsync(() => state.Hidden || state.DisplayFailed, placement.ShowTimeoutSeconds);
            if (!completed)
            {
                state.Availability = AdAvailabilityState.Unavailable;
                LogWarning($"Show timed out for placement '{placement.PlacementId}'.");
                return AdShowResult.Failed(request, ProviderId, "Show timed out.");
            }

            if (state.DisplayFailed)
            {
                LogWarning($"Show failed for placement '{placement.PlacementId}'. Reason='{state.FailureReason}'.");
                return AdShowResult.Failed(request, ProviderId, state.FailureReason);
            }

            bool wasCompleted = placement.Format == AdFormat.Interstitial
                ? state.Displayed
                : state.Displayed && state.RewardEarned;

            LoadAd(binding.AdUnitId, placement);
            LogInfo($"Show finished for placement '{placement.PlacementId}'. completed={wasCompleted}, rewardEarned={state.RewardEarned}, rewardAmount={state.RewardAmount}, rewardLabel='{state.RewardLabel}'.");

            return new AdShowResult
            {
                Request = request,
                Placement = placement,
                ProviderId = ProviderId,
                Format = placement.Format,
                WasShown = state.Displayed,
                WasCompleted = wasCompleted,
                RewardEarned = state.RewardEarned,
                Status = wasCompleted ? AdShowStatus.Completed : AdShowStatus.ClosedEarly,
                FailureReason = string.Empty,
                RewardAmount = state.RewardAmount,
                RewardLabel = state.RewardLabel
            };
        }

        /// <summary>
        /// Executes hide async asynchronously.
        /// </summary>
        public override async Awaitable HideAsync(AdRequestContext request)
        {
            AdPlacementDefinitionSO placement = request.Placement;
            if (!ValidatePlacement(placement, out AdProviderAdUnitBinding binding) || !IsPersistentFormat(placement.Format))
            {
                return;
            }

            await InitializeAsync();
            if (!_sdkAvailable)
            {
                return;
            }

            HidePersistentAd(binding.AdUnitId, placement.Format);
            AdUnitRuntimeState state = GetState(binding.AdUnitId);
            state.IsVisible = false;
            state.Hidden = true;
            state.Availability = state.LoadSucceeded ? AdAvailabilityState.Ready : AdAvailabilityState.Unavailable;
        }

        /// <summary>
        /// Gets availability.
        /// </summary>
        public override AdAvailabilityState GetAvailability(AdPlacementDefinitionSO placement)
        {
            if (!ValidatePlacement(placement, out AdProviderAdUnitBinding binding))
            {
                return AdAvailabilityState.Unavailable;
            }

            AdUnitRuntimeState state = GetState(binding.AdUnitId);
            if (_sdkAvailable && IsReadyForFormat(state, binding.AdUnitId, placement.Format))
            {
                return state.IsVisible && IsPersistentFormat(placement.Format)
                    ? AdAvailabilityState.Showing
                    : AdAvailabilityState.Ready;
            }

            return state.Availability;
        }

        /// <summary>
        /// Executes dispose provider.
        /// </summary>
        public override void DisposeProvider()
        {
            if (_sdkAvailable)
            {
                foreach (AdProviderAdUnitBinding binding in Bindings)
                {
                    if (binding == null || binding.Placement == null || string.IsNullOrWhiteSpace(binding.AdUnitId))
                    {
                        continue;
                    }

                    DestroyPersistentAd(binding.AdUnitId, binding.Placement.Format);
                }
            }

            UnsubscribeCallbacks();
            _states.Clear();
            _initializationStarted = false;
            _initializationInProgress = false;
            IsInitialized = false;
            LogInfo("Provider disposed.");
        }

        /// <summary>
        /// Validates placement.
        /// </summary>
        private bool ValidatePlacement(AdPlacementDefinitionSO placement, out AdProviderAdUnitBinding binding)
        {
            binding = null;
            if (placement == null)
            {
                LogWarning("Received null placement.");
                return false;
            }

            if (!TryGetBinding(placement, out binding))
            {
                LogWarning($"No binding found for placement '{placement.PlacementId}'.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolves sdk.
        /// </summary>
        private void ResolveSdk()
        {
            if (_maxSdkType != null)
            {
                return;
            }

            _maxSdkType = AdsSdkReflection.FindType("MaxSdk");
            _maxSdkCallbacksType = AdsSdkReflection.FindType("MaxSdkCallbacks");
            _adViewPositionType = _maxSdkType?.GetNestedType("AdViewPosition", BindingFlags.Public);
            _adViewConfigurationType = _maxSdkType?.GetNestedType("AdViewConfiguration", BindingFlags.Public);
            _interstitialCallbacksSource = _maxSdkCallbacksType?.GetNestedType("Interstitial", BindingFlags.Public);
            _rewardedCallbacksSource = _maxSdkCallbacksType?.GetNestedType("Rewarded", BindingFlags.Public);
            _bannerCallbacksSource = _maxSdkCallbacksType?.GetNestedType("Banner", BindingFlags.Public);
            _mrecCallbacksSource = _maxSdkCallbacksType?.GetNestedType("MRec", BindingFlags.Public);
            _sdkAvailable =
                _maxSdkType != null &&
                _maxSdkCallbacksType != null &&
                _adViewPositionType != null &&
                _adViewConfigurationType != null &&
                _interstitialCallbacksSource != null &&
                _rewardedCallbacksSource != null &&
                _bannerCallbacksSource != null &&
                _mrecCallbacksSource != null;
        }

        /// <summary>
        /// Subscribes to callbacks.
        /// </summary>
        private void SubscribeCallbacks()
        {
            if (_callbacksSubscribed || !_sdkAvailable)
            {
                return;
            }

            AdsSdkReflection.AddEventHandler(_maxSdkCallbacksType, "OnSdkInitializedEvent", this, nameof(OnSdkInitialized));
            AdsSdkReflection.AddEventHandler(_interstitialCallbacksSource, "OnAdLoadedEvent", this, nameof(OnInterstitialLoaded));
            AdsSdkReflection.AddEventHandler(_interstitialCallbacksSource, "OnAdLoadFailedEvent", this, nameof(OnInterstitialLoadFailed));
            AdsSdkReflection.AddEventHandler(_interstitialCallbacksSource, "OnAdDisplayedEvent", this, nameof(OnInterstitialDisplayed));
            AdsSdkReflection.AddEventHandler(_interstitialCallbacksSource, "OnAdDisplayFailedEvent", this, nameof(OnInterstitialDisplayFailed));
            AdsSdkReflection.AddEventHandler(_interstitialCallbacksSource, "OnAdHiddenEvent", this, nameof(OnInterstitialHidden));
            AdsSdkReflection.AddEventHandler(_rewardedCallbacksSource, "OnAdLoadedEvent", this, nameof(OnRewardedLoaded));
            AdsSdkReflection.AddEventHandler(_rewardedCallbacksSource, "OnAdLoadFailedEvent", this, nameof(OnRewardedLoadFailed));
            AdsSdkReflection.AddEventHandler(_rewardedCallbacksSource, "OnAdDisplayedEvent", this, nameof(OnRewardedDisplayed));
            AdsSdkReflection.AddEventHandler(_rewardedCallbacksSource, "OnAdDisplayFailedEvent", this, nameof(OnRewardedDisplayFailed));
            AdsSdkReflection.AddEventHandler(_rewardedCallbacksSource, "OnAdReceivedRewardEvent", this, nameof(OnRewardedReceivedReward));
            AdsSdkReflection.AddEventHandler(_rewardedCallbacksSource, "OnAdHiddenEvent", this, nameof(OnRewardedHidden));
            AdsSdkReflection.AddEventHandler(_bannerCallbacksSource, "OnAdLoadedEvent", this, nameof(OnBannerLoaded));
            AdsSdkReflection.AddEventHandler(_bannerCallbacksSource, "OnAdLoadFailedEvent", this, nameof(OnBannerLoadFailed));
            AdsSdkReflection.AddEventHandler(_mrecCallbacksSource, "OnAdLoadedEvent", this, nameof(OnMRecLoaded));
            AdsSdkReflection.AddEventHandler(_mrecCallbacksSource, "OnAdLoadFailedEvent", this, nameof(OnMRecLoadFailed));
            _callbacksSubscribed = true;
            LogInfo("SDK callbacks subscribed.");
        }

        /// <summary>
        /// Unsubscribes from callbacks.
        /// </summary>
        private void UnsubscribeCallbacks()
        {
            if (!_callbacksSubscribed || !_sdkAvailable)
            {
                return;
            }

            AdsSdkReflection.RemoveEventHandler(_maxSdkCallbacksType, "OnSdkInitializedEvent", this, nameof(OnSdkInitialized));
            AdsSdkReflection.RemoveEventHandler(_interstitialCallbacksSource, "OnAdLoadedEvent", this, nameof(OnInterstitialLoaded));
            AdsSdkReflection.RemoveEventHandler(_interstitialCallbacksSource, "OnAdLoadFailedEvent", this, nameof(OnInterstitialLoadFailed));
            AdsSdkReflection.RemoveEventHandler(_interstitialCallbacksSource, "OnAdDisplayedEvent", this, nameof(OnInterstitialDisplayed));
            AdsSdkReflection.RemoveEventHandler(_interstitialCallbacksSource, "OnAdDisplayFailedEvent", this, nameof(OnInterstitialDisplayFailed));
            AdsSdkReflection.RemoveEventHandler(_interstitialCallbacksSource, "OnAdHiddenEvent", this, nameof(OnInterstitialHidden));
            AdsSdkReflection.RemoveEventHandler(_rewardedCallbacksSource, "OnAdLoadedEvent", this, nameof(OnRewardedLoaded));
            AdsSdkReflection.RemoveEventHandler(_rewardedCallbacksSource, "OnAdLoadFailedEvent", this, nameof(OnRewardedLoadFailed));
            AdsSdkReflection.RemoveEventHandler(_rewardedCallbacksSource, "OnAdDisplayedEvent", this, nameof(OnRewardedDisplayed));
            AdsSdkReflection.RemoveEventHandler(_rewardedCallbacksSource, "OnAdDisplayFailedEvent", this, nameof(OnRewardedDisplayFailed));
            AdsSdkReflection.RemoveEventHandler(_rewardedCallbacksSource, "OnAdReceivedRewardEvent", this, nameof(OnRewardedReceivedReward));
            AdsSdkReflection.RemoveEventHandler(_rewardedCallbacksSource, "OnAdHiddenEvent", this, nameof(OnRewardedHidden));
            AdsSdkReflection.RemoveEventHandler(_bannerCallbacksSource, "OnAdLoadedEvent", this, nameof(OnBannerLoaded));
            AdsSdkReflection.RemoveEventHandler(_bannerCallbacksSource, "OnAdLoadFailedEvent", this, nameof(OnBannerLoadFailed));
            AdsSdkReflection.RemoveEventHandler(_mrecCallbacksSource, "OnAdLoadedEvent", this, nameof(OnMRecLoaded));
            AdsSdkReflection.RemoveEventHandler(_mrecCallbacksSource, "OnAdLoadFailedEvent", this, nameof(OnMRecLoadFailed));
            _callbacksSubscribed = false;
        }

        /// <summary>
        /// Loads ad.
        /// </summary>
        private void LoadAd(string adUnitId, AdPlacementDefinitionSO placement)
        {
            if (placement == null)
            {
                return;
            }

            if (placement.Format == AdFormat.Rewarded)
            {
                AdsSdkReflection.InvokeStatic(_maxSdkType, "LoadRewardedAd", adUnitId);
                return;
            }

            if (placement.Format == AdFormat.Interstitial)
            {
                AdsSdkReflection.InvokeStatic(_maxSdkType, "LoadInterstitial", adUnitId);
                return;
            }

            EnsurePersistentAdCreated(adUnitId, placement);
        }

        /// <summary>
        /// Shows ad.
        /// </summary>
        private void ShowAd(string adUnitId, AdPlacementDefinitionSO placement)
        {
            string methodName = placement != null && placement.Format == AdFormat.Rewarded ? "ShowRewardedAd" : "ShowInterstitial";
            AdsSdkReflection.InvokeStatic(_maxSdkType, methodName, adUnitId, placement != null ? placement.PlacementId : null, null);
        }

        /// <summary>
        /// Executes is sdk ready.
        /// </summary>
        private bool IsReadyForFormat(AdUnitRuntimeState state, string adUnitId, AdFormat format)
        {
            if (IsPersistentFormat(format))
            {
                return state != null && state.LoadSucceeded;
            }

            object result = AdsSdkReflection.InvokeStatic(_maxSdkType, format == AdFormat.Rewarded ? "IsRewardedAdReady" : "IsInterstitialReady", adUnitId);
            return result is bool boolResult && boolResult;
        }

        /// <summary>
        /// Shows a persistent ad view.
        /// </summary>
        private void ShowPersistentAd(string adUnitId, AdFormat format)
        {
            AdsSdkReflection.InvokeStatic(_maxSdkType, format == AdFormat.MRec ? "ShowMRec" : "ShowBanner", adUnitId);
        }

        /// <summary>
        /// Hides a persistent ad view.
        /// </summary>
        private void HidePersistentAd(string adUnitId, AdFormat format)
        {
            AdsSdkReflection.InvokeStatic(_maxSdkType, format == AdFormat.MRec ? "HideMRec" : "HideBanner", adUnitId);
        }

        /// <summary>
        /// Destroys a persistent ad view.
        /// </summary>
        private void DestroyPersistentAd(string adUnitId, AdFormat format)
        {
            if (!IsPersistentFormat(format))
            {
                return;
            }

            AdsSdkReflection.InvokeStatic(_maxSdkType, format == AdFormat.MRec ? "DestroyMRec" : "DestroyBanner", adUnitId);
        }

        /// <summary>
        /// Ensures a persistent ad view has been created.
        /// </summary>
        private void EnsurePersistentAdCreated(string adUnitId, AdPlacementDefinitionSO placement)
        {
            AdUnitRuntimeState state = GetState(adUnitId);
            if (state.PersistentAdCreated)
            {
                return;
            }

            object position = Enum.Parse(_adViewPositionType, ToMaxPositionName(placement.ViewPosition));
            object configuration = Activator.CreateInstance(_adViewConfigurationType, position);
            string methodName = placement.Format == AdFormat.MRec ? "CreateMRec" : "CreateBanner";
            AdsSdkReflection.InvokeStatic(_maxSdkType, methodName, adUnitId, configuration);
            state.PersistentAdCreated = true;
            state.IsVisible = false;

            HidePersistentAd(adUnitId, placement.Format);
        }

        /// <summary>
        /// Converts the shared ad view position to a MAX position enum value.
        /// </summary>
        private static string ToMaxPositionName(AdViewPosition position)
        {
            return position switch
            {
                AdViewPosition.Top => "TopCenter",
                AdViewPosition.Bottom => "BottomCenter",
                AdViewPosition.TopLeft => "TopLeft",
                AdViewPosition.TopRight => "TopRight",
                AdViewPosition.BottomLeft => "BottomLeft",
                AdViewPosition.BottomRight => "BottomRight",
                _ => "Centered"
            };
        }

        /// <summary>
        /// Handles sdk initialized.
        /// </summary>
        private void OnSdkInitialized(object configuration)
        {
            IsInitialized = true;
            LogInfo("SDK initialization callback received.");
        }

        /// <summary>
        /// Handles the interstitial loaded callback.
        /// </summary>
        private void OnInterstitialLoaded(string adUnitId, object adInfo) => MarkLoaded(adUnitId, "Interstitial loaded callback received.");
        /// <summary>
        /// Handles the rewarded loaded callback.
        /// </summary>
        private void OnRewardedLoaded(string adUnitId, object adInfo) => MarkLoaded(adUnitId, "Rewarded loaded callback received.");

        /// <summary>
        /// Handles the banner loaded callback.
        /// </summary>
        private void OnBannerLoaded(string adUnitId, object adInfo) => MarkLoaded(adUnitId, "Banner loaded callback received.");

        /// <summary>
        /// Handles the MREC loaded callback.
        /// </summary>
        private void OnMRecLoaded(string adUnitId, object adInfo) => MarkLoaded(adUnitId, "MREC loaded callback received.");
        /// <summary>
        /// Handles interstitial displayed.
        /// </summary>
        private void OnInterstitialDisplayed(string adUnitId, object adInfo) => MarkDisplayed(adUnitId, "Interstitial displayed callback received.");
        /// <summary>
        /// Handles rewarded displayed.
        /// </summary>
        private void OnRewardedDisplayed(string adUnitId, object adInfo) => MarkDisplayed(adUnitId, "Rewarded displayed callback received.");
        /// <summary>
        /// Handles interstitial hidden.
        /// </summary>
        private void OnInterstitialHidden(string adUnitId, object adInfo) => MarkHidden(adUnitId, "Interstitial hidden callback received.");
        /// <summary>
        /// Handles rewarded hidden.
        /// </summary>
        private void OnRewardedHidden(string adUnitId, object adInfo) => MarkHidden(adUnitId, "Rewarded hidden callback received.");
        /// <summary>
        /// Handles the interstitial load failure callback.
        /// </summary>
        private void OnInterstitialLoadFailed(string adUnitId, object errorInfo) => MarkLoadFailed(adUnitId, AdsSdkReflection.ReadString(errorInfo, "Message", "Interstitial load failed."));
        /// <summary>
        /// Handles the rewarded load failure callback.
        /// </summary>
        private void OnRewardedLoadFailed(string adUnitId, object errorInfo) => MarkLoadFailed(adUnitId, AdsSdkReflection.ReadString(errorInfo, "Message", "Rewarded load failed."));

        /// <summary>
        /// Handles the banner load failure callback.
        /// </summary>
        private void OnBannerLoadFailed(string adUnitId, object errorInfo) => MarkLoadFailed(adUnitId, AdsSdkReflection.ReadString(errorInfo, "Message", "Banner load failed."));

        /// <summary>
        /// Handles the MREC load failure callback.
        /// </summary>
        private void OnMRecLoadFailed(string adUnitId, object errorInfo) => MarkLoadFailed(adUnitId, AdsSdkReflection.ReadString(errorInfo, "Message", "MREC load failed."));
        /// <summary>
        /// Handles the interstitial display failure callback.
        /// </summary>
        private void OnInterstitialDisplayFailed(string adUnitId, object errorInfo, object adInfo) => MarkDisplayFailed(adUnitId, AdsSdkReflection.ReadString(errorInfo, "Message", "Interstitial display failed."));
        /// <summary>
        /// Handles the rewarded display failure callback.
        /// </summary>
        private void OnRewardedDisplayFailed(string adUnitId, object errorInfo, object adInfo) => MarkDisplayFailed(adUnitId, AdsSdkReflection.ReadString(errorInfo, "Message", "Rewarded display failed."));

        /// <summary>
        /// Handles rewarded received reward.
        /// </summary>
        private void OnRewardedReceivedReward(string adUnitId, object reward, object adInfo)
        {
            AdUnitRuntimeState state = GetState(adUnitId);
            state.RewardEarned = true;
            state.RewardAmount = AdsSdkReflection.ReadInt(reward, "Amount", 0);
            state.RewardLabel = AdsSdkReflection.ReadString(reward, "Label", string.Empty);
            LogInfo($"Reward callback received for ad unit '{adUnitId}'. amount={state.RewardAmount}, label='{state.RewardLabel}'.");
        }

        /// <summary>
        /// Marks loaded.
        /// </summary>
        private void MarkLoaded(string adUnitId, string debugMessage)
        {
            AdUnitRuntimeState state = GetState(adUnitId);
            state.LoadCompleted = true;
            state.LoadSucceeded = true;
            state.Availability = state.IsVisible ? AdAvailabilityState.Showing : AdAvailabilityState.Ready;
            state.FailureReason = string.Empty;
            LogInfo($"{debugMessage} adUnitId='{adUnitId}'.");
        }

        /// <summary>
        /// Marks load failed.
        /// </summary>
        private void MarkLoadFailed(string adUnitId, string reason)
        {
            AdUnitRuntimeState state = GetState(adUnitId);
            state.LoadCompleted = true;
            state.LoadSucceeded = false;
            state.Availability = AdAvailabilityState.Unavailable;
            state.FailureReason = reason;
            LogWarning($"Load failed for adUnitId='{adUnitId}'. Reason='{reason}'.");
        }

        /// <summary>
        /// Marks displayed.
        /// </summary>
        private void MarkDisplayed(string adUnitId, string debugMessage)
        {
            AdUnitRuntimeState state = GetState(adUnitId);
            state.Displayed = true;
            state.Availability = AdAvailabilityState.Showing;
            LogInfo($"{debugMessage} adUnitId='{adUnitId}'.");
        }

        /// <summary>
        /// Marks display failed.
        /// </summary>
        private void MarkDisplayFailed(string adUnitId, string reason)
        {
            AdUnitRuntimeState state = GetState(adUnitId);
            state.DisplayFailed = true;
            state.Availability = AdAvailabilityState.Unavailable;
            state.FailureReason = reason;
            LogWarning($"Display failed for adUnitId='{adUnitId}'. Reason='{reason}'.");
        }

        /// <summary>
        /// Marks hidden.
        /// </summary>
        private void MarkHidden(string adUnitId, string debugMessage)
        {
            AdUnitRuntimeState state = GetState(adUnitId);
            state.Hidden = true;
            state.IsVisible = false;
            state.Availability = AdAvailabilityState.Unavailable;
            LogInfo($"{debugMessage} adUnitId='{adUnitId}'.");
        }

        /// <summary>
        /// Resets load state.
        /// </summary>
        private static void ResetLoadState(AdUnitRuntimeState state)
        {
            state.LoadCompleted = false;
            state.LoadSucceeded = false;
            state.FailureReason = string.Empty;
        }

        /// <summary>
        /// Resets show state.
        /// </summary>
        private static void ResetShowState(AdUnitRuntimeState state)
        {
            state.Displayed = false;
            state.Hidden = false;
            state.DisplayFailed = false;
            state.RewardEarned = false;
            state.RewardAmount = 0;
            state.RewardLabel = string.Empty;
            state.FailureReason = string.Empty;
        }

        /// <summary>
        /// Gets state.
        /// </summary>
        private AdUnitRuntimeState GetState(string adUnitId)
        {
            if (!_states.TryGetValue(adUnitId, out AdUnitRuntimeState state))
            {
                state = new AdUnitRuntimeState();
                _states.Add(adUnitId, state);
            }

            return state;
        }

        /// <summary>
        /// Determines whether the format is a persistent ad view.
        /// </summary>
        private static bool IsPersistentFormat(AdFormat format)
        {
            return format == AdFormat.Banner || format == AdFormat.MRec;
        }

        private sealed class AdUnitRuntimeState
        {
            public AdAvailabilityState Availability;
            public bool LoadCompleted;
            public bool LoadSucceeded;
            public bool Displayed;
            public bool Hidden;
            public bool DisplayFailed;
            public bool RewardEarned;
            public bool IsVisible;
            public bool PersistentAdCreated;
            public int RewardAmount;
            public string RewardLabel = string.Empty;
            public string FailureReason = string.Empty;
        }
    }
}
