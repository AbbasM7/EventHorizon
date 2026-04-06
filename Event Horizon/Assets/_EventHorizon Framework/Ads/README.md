# EventHorizon.Ads

```text
   .   *      .      /\       .
      .    *        /==\   .
   .       .       |::::|     *
      ORBITAL OBSERVATION DECK
```

| Document Field | Value |
|---|---|
| Layer | Runtime Feature Module |
| Assembly | `EventHorizon.Ads` |
| Primary Entry Point | `AdsManagerModuleSO` |
| Depends On | `EventHorizon.Core` |
| Technical Role | ad request routing, provider fallback, placement metadata, telemetry |

`EventHorizon.Ads` is a provider-agnostic ad orchestration layer. It does not hard-link the assembly to AdMob or AppLovin at compile time. Instead, it uses:

- `AdsManagerModuleSO` as the central coordinator
- provider assets derived from `AdsProviderManagerBaseSO`
- `AdPlacementDefinitionSO` assets to describe placements
- `AdsEventChannelSO` for request and telemetry flow
- reflection helpers so SDKs can be present or absent without breaking the framework assembly

This makes the ad system optional, asset-driven, and easier to swap or fall back between providers.

## Assembly dependency

- `EventHorizon.Ads` depends on `EventHorizon.Core`

The provider code is still able to interact with AdMob and AppLovin because it resolves SDK types dynamically through `AdsSdkReflection`.

## Module contents

### Central module

- `AdsManagerModuleSO`
  - Main runtime entry point.
  - Sorts providers by priority.
  - Initializes providers asynchronously.
  - Handles preload, availability checks, show, hide, and fallback.
  - Emits telemetry and result events.

### Provider abstraction

- `IAdsManager`
  - Common consumer-facing contract.
- `AdsProviderManagerBaseSO`
  - Base class for provider-specific implementations.
  - Stores provider priority, default timeouts, and placement-to-ad-unit bindings.
- `AdMobAdsManagerSO`
  - AdMob integration using reflection against Google Mobile Ads SDK.
- `AppLovinAdsManagerSO`
  - AppLovin MAX integration using reflection against MAX SDK.

### Data assets and payloads

- `AdPlacementDefinitionSO`
  - placement ID
  - format
  - view position
  - preload-on-start flag
  - load timeout
  - show timeout
- `AdRequestContext`
  - placement
  - source ID
  - flow ID
  - reason
- `AdProviderAdUnitBinding`
  - maps a placement asset to a provider-specific ad unit ID
- result and state types
  - `AdAvailabilityState`
  - `AdLoadResult`
  - `AdShowResult`
  - `AdShowStatus`
  - `AdAvailabilityChanged`
  - `AdTelemetryEvent`

### Events

- `AdsEventChannelSO`
  - preload requested
  - show requested
  - hide requested
  - show started
  - availability changed
  - load completed
  - show completed
  - telemetry reported

## How the module works

### Startup

When `AdsManagerModuleSO` initializes:

1. providers are sorted by ascending priority
2. request listeners are attached to `AdsEventChannelSO`
3. provider initialization begins asynchronously
4. any placements marked for preload are loaded after provider startup completes

### Preload flow

When a preload request arrives:

1. the module validates the placement
2. it waits until provider initialization is finished
3. it walks providers in priority order
4. for each provider that supports the placement:
   - preload is attempted
   - load result is raised
   - availability update is raised
   - telemetry is raised
5. the operation stops after the first successful preload

### Show flow

When a show request arrives:

1. the module validates the placement
2. it waits for initialization
3. it checks providers in order
4. if a provider is not ready, it tries to preload first
5. if the provider reports it can show, the module raises `OnShowStarted`
6. the provider attempts the show
7. availability and telemetry are updated
8. if the show fails, the manager falls back to the next provider
9. the final `AdShowResult` is raised on `OnShowCompleted`

This is the key architectural value of the module: caller code does not care which provider actually displayed the ad.

### Persistent ads vs fullscreen ads

The code treats formats differently:

- persistent formats
  - `Banner`
  - `MRec`
  - can remain in `Showing` state
  - `HideAsync()` matters
- fullscreen formats
  - `Interstitial`
  - `Rewarded`
  - show flow waits for completion, failure, or timeout

## Setup tutorial

### 1. Import the SDKs you want to use

The framework includes packaged SDK archives in `Assets/_EventHorizon/SDKs`:

- AdMob package in `Assets/_EventHorizon/SDKs/AdMob`
- AppLovin MAX package in `Assets/_EventHorizon/SDKs/AppLovin`

Import at least one before expecting live ads. If you skip this step, the provider will log that the SDK is unavailable and remain inert.

### 2. Create event assets for the ad channel

Create the typed event assets required by `AdsEventChannelSO`:

- `AdRequestContextEventSO` for preload/show/hide requests
- `AdAvailabilityChangedEventSO`
- `AdLoadResultEventSO`
- `AdShowResultEventSO`
- `AdTelemetryEventSO`

Then create:

1. `Create > EventHorizon > Ads > Event Channel`
2. assign all of the event asset fields

### 3. Create placement assets

For each placement:

1. right-click
2. choose `Create > EventHorizon > Ads > Placement`
3. configure:
   - `Placement Id`
   - `Format`
   - `View Position` for banner-like placements
   - `Preload On Initialize`
   - `Load Timeout Seconds`
   - `Show Timeout Seconds`

Recommended examples:

- `reward_revive`
- `reward_bonus_coins`
- `inter_level_interstitial`
- `main_menu_banner`

### 4. Create provider assets

Create one or more provider managers:

- `Create > EventHorizon > Ads > Providers > AppLovin Ads Manager`
- `Create > EventHorizon > Ads > Providers > AdMob Ads Manager`

For each provider asset:

1. set `Priority`
   - lower numbers are attempted first
2. set default timeout
3. add bindings
   - each binding pairs one `AdPlacementDefinitionSO` with one provider ad unit ID

Important:

- a provider only supports placements that have a valid binding in its list
- if the same placement exists on multiple providers, fallback becomes possible

### 5. Create the central ads module

Create:

- `Create > EventHorizon > Ads > Ads Manager Module`

Assign:

- provider list
- shared `AdsEventChannelSO`
- optional preload placements

Then add the module to your `ModuleRegistry`.

### 6. Request ads from game code

Preferred pattern:

```csharp
[SerializeField] private AdsEventChannelSO _adsChannel;
[SerializeField] private AdPlacementDefinitionSO _rewardPlacement;

public void ShowRewardedAd()
{
    _adsChannel.OnShowRequested.Raise(new AdRequestContext
    {
        Placement = _rewardPlacement,
        SourceId = "shop_popup",
        FlowId = "double_reward",
        Reason = "player_opt_in"
    });
}
```

You can also preload explicitly:

```csharp
_adsChannel.OnPreloadRequested.Raise(AdRequestContext.ForPlacement(_rewardPlacement));
```

Hide persistent ads with:

```csharp
_adsChannel.OnHideRequested.Raise(AdRequestContext.ForPlacement(_bannerPlacement));
```

### 7. Listen to completion and telemetry

Typical listeners:

- reward payout system listens to `OnShowCompleted`
- UI listens to `OnAvailabilityChanged`
- analytics bridge listens to `OnTelemetryReported`

This keeps ad calling code minimal and removes the need for callback plumbing in gameplay scripts.

## Integration notes

### AdMob provider behavior

`AdMobAdsManagerSO`:

- resolves SDK types dynamically
- initializes `MobileAds`
- loads interstitial and rewarded ads through static `Load` methods
- manages banner views per ad unit
- marks readiness from SDK callbacks

If Google Mobile Ads is not imported, the provider logs a warning and remains inactive instead of throwing assembly errors.

### AppLovin provider behavior

`AppLovinAdsManagerSO`:

- resolves `MaxSdk` and `MaxSdkCallbacks` dynamically
- subscribes to MAX callback events
- initializes with known ad unit IDs
- uses readiness checks for fullscreen ads
- creates and hides persistent banners and MRECs until shown

If MAX is not imported, it also stays inactive safely.

## Recommended usage patterns

- Use placement assets as the only ad identifiers in game logic.
- Put monetization metadata in `AdRequestContext` so telemetry stays useful.
- Configure at least two providers for important rewarded placements if you want fallback.
- Separate `banner` and `rewarded` placements into distinct assets even if they are conceptually related.

## Common pitfalls

- Importing no SDK packages and expecting ads to work.
- Forgetting to bind placements inside provider assets.
- Omitting the central `AdsManagerModuleSO` from the `ModuleRegistry`.
- Sending `AdRequestContext` with a null placement.
- Expecting a banner to disappear automatically without sending a hide request.
- Treating provider priority as display frequency; it is actually fallback order.

<table width="100%">
  <tr>
    <td bgcolor="#57A8A8" width="25%"></td>
    <td bgcolor="#30B06E" width="25%"></td>
    <td bgcolor="#D1AD29" width="25%"></td>
    <td bgcolor="#C7262E" width="25%"></td>
  </tr>
</table>

```text
          _|_
---@----(_)--@---
          | |
          ./ \.

    .-----.   .-----.   .-----.
    | Fe  |   | Li  |   | Na  |
    '-----'   '-----'   '-----'
     BLOOD      METH     TEARS
```
