using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EventHorizon.Ads
{
    [CreateAssetMenu(menuName = "EventHorizon/Ads/Providers/AdMob Ads Manager", order = 1)]
    public class AdMobAdsManagerSO : AdsProviderManagerBaseSO
    {
        private const float InitializationTimeoutSeconds = 10f;

        private readonly Dictionary<string, AdUnitRuntimeState> _states = new Dictionary<string, AdUnitRuntimeState>();
        private Type _mobileAdsType;
        private Type _adRequestType;
        private Type _adSizeType;
        private Type _adPositionType;
        private Type _interstitialType;
        private Type _rewardedType;
        private Type _bannerViewType;
        private bool _sdkAvailable;
        private bool _initializationStarted;
        private bool _initializationInProgress;
        private string _pendingLoadAdUnitId;

        public override string ProviderId => "AdMob";

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
                LogWarning("SDK not found. Provider will remain inactive until Google Mobile Ads is installed.");
                return;
            }

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

            PropertyInfo raiseOnMainThread = _mobileAdsType.GetProperty("RaiseAdEventsOnUnityMainThread", BindingFlags.Public | BindingFlags.Static);
            raiseOnMainThread?.SetValue(null, true);

            LogInfo("Initializing SDK.");
            MethodInfo initializeMethod = _mobileAdsType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
            Delegate initDelegate = Delegate.CreateDelegate(initializeMethod.GetParameters()[0].ParameterType, this, nameof(OnInitialized), false);
            initializeMethod?.Invoke(null, new object[] { initDelegate });

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
                return AdLoadResult.Failed(request, ProviderId, "Placement is not bound for AdMob.");
            }

            await InitializeAsync();
            if (!_sdkAvailable)
            {
                return AdLoadResult.Failed(request, ProviderId, "SDK unavailable.");
            }

            AdUnitRuntimeState state = GetState(binding.AdUnitId);
            if (IsLoadedAndReady(state, placement.Format))
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

            if (IsPersistentFormat(placement.Format))
            {
                EnsureBannerView(state, binding.AdUnitId, placement);
                object adRequest = AdsSdkReflection.CreateInstance(_adRequestType);
                AdsSdkReflection.Invoke(state.BannerView, "LoadAd", adRequest);
            }
            else
            {
                object adRequest = AdsSdkReflection.CreateInstance(_adRequestType);
                _pendingLoadAdUnitId = binding.AdUnitId;

                if (placement.Format == AdFormat.Rewarded)
                {
                    MethodInfo loadMethod = _rewardedType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                    Delegate callback = Delegate.CreateDelegate(loadMethod.GetParameters()[2].ParameterType, this, nameof(OnRewardedLoaded), false);
                    loadMethod?.Invoke(null, new[] { binding.AdUnitId, adRequest, callback });
                }
                else
                {
                    MethodInfo loadMethod = _interstitialType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
                    Delegate callback = Delegate.CreateDelegate(loadMethod.GetParameters()[2].ParameterType, this, nameof(OnInterstitialLoaded), false);
                    loadMethod?.Invoke(null, new[] { binding.AdUnitId, adRequest, callback });
                }
            }

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
            bool canShow = IsLoadedAndReady(state, placement.Format);
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
                return AdShowResult.Failed(request, ProviderId, "Placement is not bound for AdMob.");
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
                if (state.BannerView == null)
                {
                    return AdShowResult.Failed(request, ProviderId, $"{placement.Format} view is not loaded.");
                }

                AdsSdkReflection.Invoke(state.BannerView, "Show");
                state.IsVisible = true;
                state.Displayed = true;
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
            state.ActiveShowPlacementId = placement.PlacementId;
            LogInfo($"Showing {placement.Format} for placement '{placement.PlacementId}'.");

            if (placement.Format == AdFormat.Rewarded)
            {
                if (state.RewardedAd == null)
                {
                    return AdShowResult.Failed(request, ProviderId, "Rewarded ad is not loaded.");
                }

                MethodInfo showMethod = state.RewardedAd.GetType().GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);
                Delegate rewardDelegate = Delegate.CreateDelegate(showMethod.GetParameters()[0].ParameterType, this, nameof(OnRewardEarned), false);
                showMethod?.Invoke(state.RewardedAd, new object[] { rewardDelegate });
            }
            else
            {
                if (state.InterstitialAd == null)
                {
                    return AdShowResult.Failed(request, ProviderId, "Interstitial ad is not loaded.");
                }

                AdsSdkReflection.Invoke(state.InterstitialAd, "Show");
            }

            bool completed = await WaitUntilAsync(() => state.Closed || state.DisplayFailed, placement.ShowTimeoutSeconds);
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

            AdUnitRuntimeState state = GetState(binding.AdUnitId);
            if (state.BannerView != null)
            {
                AdsSdkReflection.Invoke(state.BannerView, "Hide");
                state.IsVisible = false;
                state.Availability = state.LoadSucceeded ? AdAvailabilityState.Ready : AdAvailabilityState.Unavailable;
            }
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
            if (_sdkAvailable && IsLoadedAndReady(state, placement.Format))
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
            foreach (KeyValuePair<string, AdUnitRuntimeState> kvp in _states)
            {
                DestroyInterstitial(kvp.Value);
                DestroyRewarded(kvp.Value);
                DestroyBanner(kvp.Value);
            }

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
            if (_mobileAdsType != null)
            {
                return;
            }

            _mobileAdsType = AdsSdkReflection.FindType("GoogleMobileAds.Api.MobileAds");
            _adRequestType = AdsSdkReflection.FindType("GoogleMobileAds.Api.AdRequest");
            _adSizeType = AdsSdkReflection.FindType("GoogleMobileAds.Api.AdSize");
            _adPositionType = AdsSdkReflection.FindType("GoogleMobileAds.Api.AdPosition");
            _interstitialType = AdsSdkReflection.FindType("GoogleMobileAds.Api.InterstitialAd");
            _rewardedType = AdsSdkReflection.FindType("GoogleMobileAds.Api.RewardedAd");
            _bannerViewType = AdsSdkReflection.FindType("GoogleMobileAds.Api.BannerView");
            _sdkAvailable =
                _mobileAdsType != null &&
                _adRequestType != null &&
                _adSizeType != null &&
                _adPositionType != null &&
                _interstitialType != null &&
                _rewardedType != null &&
                _bannerViewType != null;
        }

        /// <summary>
        /// Handles initialized.
        /// </summary>
        private void OnInitialized(object initializationStatus)
        {
            IsInitialized = true;
            _initializationInProgress = false;
            LogInfo("SDK initialization callback received.");
        }

        /// <summary>
        /// Handles the interstitial loaded callback.
        /// </summary>
        private void OnInterstitialLoaded(object ad, object error)
        {
            HandleFullscreenAdLoad(ad, error, false);
        }

        /// <summary>
        /// Handles the rewarded loaded callback.
        /// </summary>
        private void OnRewardedLoaded(object ad, object error)
        {
            HandleFullscreenAdLoad(ad, error, true);
        }

        /// <summary>
        /// Handles the banner ad loaded callback.
        /// </summary>
        private void OnBannerLoaded()
        {
            AdUnitRuntimeState state = FindPendingBannerState();
            if (state == null)
            {
                return;
            }

            state.LoadCompleted = true;
            state.LoadSucceeded = true;
            state.FailureReason = string.Empty;
            state.Availability = state.IsVisible ? AdAvailabilityState.Showing : AdAvailabilityState.Ready;
            LogInfo($"Banner load callback received for adUnitId='{state.AdUnitId}'.");
        }

        /// <summary>
        /// Handles the banner load failure callback.
        /// </summary>
        private void OnBannerLoadFailed(object error)
        {
            AdUnitRuntimeState state = FindPendingBannerState();
            if (state == null)
            {
                return;
            }

            string reason = error != null ? AdsSdkReflection.Invoke(error, "GetMessage") as string : "Load failed.";
            state.LoadCompleted = true;
            state.LoadSucceeded = false;
            state.Availability = AdAvailabilityState.Unavailable;
            state.FailureReason = reason;
            LogWarning($"Banner load failed for adUnitId='{state.AdUnitId}'. Reason='{reason}'.");
        }

        /// <summary>
        /// Handles fullscreen ad loading callbacks.
        /// </summary>
        private void HandleFullscreenAdLoad(object ad, object error, bool rewarded)
        {
            string reason = error != null ? AdsSdkReflection.Invoke(error, "GetMessage") as string : string.Empty;
            string adUnitId = ad != null ? AdsSdkReflection.Invoke(ad, "GetAdUnitID") as string : string.Empty;
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                adUnitId = _pendingLoadAdUnitId;
            }

            if (error != null || ad == null || string.IsNullOrWhiteSpace(adUnitId))
            {
                if (!string.IsNullOrWhiteSpace(adUnitId))
                {
                    MarkLoadFailed(adUnitId, string.IsNullOrWhiteSpace(reason) ? "Load failed." : reason);
                }
                else
                {
                    LogWarning($"Load callback failed before ad unit resolution. Reason='{reason}'.");
                }

                _pendingLoadAdUnitId = null;
                return;
            }

            _pendingLoadAdUnitId = null;

            AdUnitRuntimeState state = GetState(adUnitId);
            if (rewarded)
            {
                DestroyRewarded(state);
                state.RewardedAd = ad;
            }
            else
            {
                DestroyInterstitial(state);
                state.InterstitialAd = ad;
            }

            WireFullscreenCallbacks(state, ad);
            state.LoadCompleted = true;
            state.LoadSucceeded = true;
            state.FailureReason = string.Empty;
            state.Availability = AdAvailabilityState.Ready;
            LogInfo($"{(rewarded ? "Rewarded" : "Interstitial")} load callback received for adUnitId='{adUnitId}'.");
        }

        /// <summary>
        /// Ensures a banner view exists for the requested placement.
        /// </summary>
        private void EnsureBannerView(AdUnitRuntimeState state, string adUnitId, AdPlacementDefinitionSO placement)
        {
            if (state.BannerView != null)
            {
                return;
            }

            object adSize = GetBannerSize(placement.Format);
            object adPosition = GetBannerPosition(placement.ViewPosition);
            object bannerView = Activator.CreateInstance(_bannerViewType, adUnitId, adSize, adPosition);
            state.BannerView = bannerView;
            state.AdUnitId = adUnitId;

            AdsSdkReflection.AddEventHandler(bannerView, "OnBannerAdLoaded", this, nameof(OnBannerLoaded));
            AdsSdkReflection.AddEventHandler(bannerView, "OnBannerAdLoadFailed", this, nameof(OnBannerLoadFailed));
            AdsSdkReflection.AddEventHandler(bannerView, "OnAdFullScreenContentOpened", this, nameof(OnBannerOverlayOpened));
            AdsSdkReflection.AddEventHandler(bannerView, "OnAdFullScreenContentClosed", this, nameof(OnBannerOverlayClosed));
        }

        /// <summary>
        /// Gets the AdMob size object for the placement format.
        /// </summary>
        private object GetBannerSize(AdFormat format)
        {
            string fieldName = format == AdFormat.MRec ? "MediumRectangle" : "Banner";
            PropertyInfo property = _adSizeType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Static);
            if (property != null)
            {
                return property.GetValue(null);
            }

            FieldInfo field = _adSizeType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            return field?.GetValue(null);
        }

        /// <summary>
        /// Gets the AdMob position object for the placement position.
        /// </summary>
        private object GetBannerPosition(AdViewPosition position)
        {
            string enumName = position switch
            {
                AdViewPosition.Top => "Top",
                AdViewPosition.Bottom => "Bottom",
                AdViewPosition.TopLeft => "TopLeft",
                AdViewPosition.TopRight => "TopRight",
                AdViewPosition.BottomLeft => "BottomLeft",
                AdViewPosition.BottomRight => "BottomRight",
                _ => "Center"
            };

            return Enum.Parse(_adPositionType, enumName);
        }

        /// <summary>
        /// Executes wire fullscreen callbacks.
        /// </summary>
        private void WireFullscreenCallbacks(AdUnitRuntimeState state, object ad)
        {
            state.WiredAd = ad;
            AdsSdkReflection.AddEventHandler(ad, "OnAdFullScreenContentOpened", this, nameof(OnAdOpened));
            AdsSdkReflection.AddEventHandler(ad, "OnAdFullScreenContentClosed", this, nameof(OnAdClosed));
            AdsSdkReflection.AddEventHandler(ad, "OnAdFullScreenContentFailed", this, nameof(OnAdFailed));
        }

        /// <summary>
        /// Handles banner overlay openings.
        /// </summary>
        private void OnBannerOverlayOpened()
        {
        }

        /// <summary>
        /// Handles banner overlay closings.
        /// </summary>
        private void OnBannerOverlayClosed()
        {
        }

        /// <summary>
        /// Handles the ad opened callback.
        /// </summary>
        private void OnAdOpened()
        {
            AdUnitRuntimeState state = FindShowingState();
            if (state == null)
            {
                return;
            }

            state.Displayed = true;
            LogInfo($"Fullscreen content opened for adUnitId='{state.AdUnitId}'.");
        }

        /// <summary>
        /// Handles the ad closed callback.
        /// </summary>
        private void OnAdClosed()
        {
            AdUnitRuntimeState state = FindShowingState();
            if (state == null)
            {
                return;
            }

            state.Closed = true;
            state.Availability = AdAvailabilityState.Unavailable;
            LogInfo($"Fullscreen content closed for adUnitId='{state.AdUnitId}'.");
        }

        /// <summary>
        /// Handles the ad failure callback.
        /// </summary>
        private void OnAdFailed(object error)
        {
            AdUnitRuntimeState state = FindShowingState();
            if (state == null)
            {
                return;
            }

            state.DisplayFailed = true;
            state.Availability = AdAvailabilityState.Unavailable;
            state.FailureReason = error != null ? AdsSdkReflection.Invoke(error, "GetMessage") as string : "Display failed.";
            LogWarning($"Fullscreen content failed for adUnitId='{state.AdUnitId}'. Reason='{state.FailureReason}'.");
        }

        /// <summary>
        /// Handles reward earned.
        /// </summary>
        private void OnRewardEarned(object reward)
        {
            AdUnitRuntimeState state = FindShowingState();
            if (state == null)
            {
                return;
            }

            state.RewardEarned = true;
            state.RewardAmount = AdsSdkReflection.ReadInt(reward, "Amount", 0);
            state.RewardLabel = AdsSdkReflection.ReadString(reward, "Type", string.Empty);
            LogInfo($"Reward callback received for adUnitId='{state.AdUnitId}'. amount={state.RewardAmount}, label='{state.RewardLabel}'.");
        }

        /// <summary>
        /// Executes is loaded and ready.
        /// </summary>
        private bool IsLoadedAndReady(AdUnitRuntimeState state, AdFormat format)
        {
            if (state == null)
            {
                return false;
            }

            if (IsPersistentFormat(format))
            {
                return state.BannerView != null && state.LoadSucceeded;
            }

            object adObject = format == AdFormat.Rewarded ? state.RewardedAd : state.InterstitialAd;
            object result = adObject != null ? AdsSdkReflection.Invoke(adObject, "CanShowAd") : null;
            return result is bool canShow && canShow;
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
        /// Finds the active fullscreen show state.
        /// </summary>
        private AdUnitRuntimeState FindShowingState()
        {
            foreach (KeyValuePair<string, AdUnitRuntimeState> kvp in _states)
            {
                if (kvp.Value.Availability == AdAvailabilityState.Showing && !kvp.Value.IsPersistentView)
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the pending banner load state.
        /// </summary>
        private AdUnitRuntimeState FindPendingBannerState()
        {
            foreach (KeyValuePair<string, AdUnitRuntimeState> kvp in _states)
            {
                if (kvp.Value.BannerView != null && kvp.Value.Availability == AdAvailabilityState.Loading)
                {
                    return kvp.Value;
                }
            }

            return null;
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
            state.Closed = false;
            state.DisplayFailed = false;
            state.RewardEarned = false;
            state.RewardAmount = 0;
            state.RewardLabel = string.Empty;
            state.FailureReason = string.Empty;
        }

        /// <summary>
        /// Destroys interstitial.
        /// </summary>
        private static void DestroyInterstitial(AdUnitRuntimeState state)
        {
            if (state?.InterstitialAd != null)
            {
                AdsSdkReflection.Invoke(state.InterstitialAd, "Destroy");
                state.InterstitialAd = null;
            }
        }

        /// <summary>
        /// Destroys rewarded.
        /// </summary>
        private static void DestroyRewarded(AdUnitRuntimeState state)
        {
            if (state?.RewardedAd != null)
            {
                AdsSdkReflection.Invoke(state.RewardedAd, "Destroy");
                state.RewardedAd = null;
            }
        }

        /// <summary>
        /// Destroys banner.
        /// </summary>
        private static void DestroyBanner(AdUnitRuntimeState state)
        {
            if (state?.BannerView != null)
            {
                AdsSdkReflection.Invoke(state.BannerView, "Destroy");
                state.BannerView = null;
                state.IsVisible = false;
            }
        }

        /// <summary>
        /// Gets state.
        /// </summary>
        private AdUnitRuntimeState GetState(string adUnitId)
        {
            if (!_states.TryGetValue(adUnitId, out AdUnitRuntimeState state))
            {
                state = new AdUnitRuntimeState { AdUnitId = adUnitId };
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
            public string AdUnitId;
            public string ActiveShowPlacementId;
            public AdAvailabilityState Availability;
            public bool LoadCompleted;
            public bool LoadSucceeded;
            public bool Displayed;
            public bool Closed;
            public bool DisplayFailed;
            public bool RewardEarned;
            public bool IsVisible;
            public int RewardAmount;
            public string RewardLabel = string.Empty;
            public string FailureReason = string.Empty;
            public object InterstitialAd;
            public object RewardedAd;
            public object BannerView;
            public object WiredAd;
            public bool IsPersistentView => BannerView != null;
        }
    }
}
