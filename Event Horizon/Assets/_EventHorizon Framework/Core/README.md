# EventHorizon.Core

```text
   .   *      .      /\       .
      .    *        /==\   .
   .       .       |::::|     *
      ORBITAL OBSERVATION DECK
```

| Document Field | Value |
|---|---|
| Layer | Runtime Foundation |
| Assembly | `EventHorizon.Core` |
| Primary Entry Points | `ModuleRegistry`, `ModuleBootstrapper`, `GameEventSO`, `VariableSO<T>` |
| Depends On | none |
| Technical Role | lifecycle, shared messaging, shared state, logging, runtime sets |

`EventHorizon.Core` is the foundation of the framework. Every runtime module hangs off this assembly, and most of the architectural rules used elsewhere start here:

- modules are `ScriptableObject` assets, not scene singletons
- scene objects act as relays, not owners of business logic
- communication happens through `GameEventSO` assets and typed variants
- state is exposed through `VariableSO<T>` assets and optional event notifications
- boot order is controlled by a single `ModuleRegistry`

If you understand this folder, the rest of `_EventHorizon` reads much more easily.

## What is in this module

### Lifecycle

- `ModuleBase`
  - Base class for all framework modules.
  - Implements the public lifecycle contract and exposes protected `OnInitialize`, `OnActivate`, `OnDeactivate`, and `OnDispose` hooks.
  - Keeps the lifecycle sequencing consistent by sealing the public flow.
- `IModuleLifecycle`
  - Common interface implemented by framework modules.
- `ModuleRegistry`
  - A `ScriptableObject` asset that stores an ordered list of modules.
  - Initializes and activates modules from top to bottom.
  - Deactivates and disposes modules from bottom to top.
- `ModuleBootstrapper`
  - The only required scene-side bootstrap component for the framework.
  - Calls `InitializeAll()` in `Awake()` and `DisposeAll()` in `OnDestroy()`.

### Events

- `GameEventSO`
  - Parameterless event channel.
- `GameEventSO<T>`
  - Typed event base for payload-based messaging.
- typed event assets
  - `BoolEventSO`
  - `FloatEventSO`
  - `IntEventSO`
  - `StringEventSO`
- listener components
  - `GameEventListener`
  - `BoolEventListener`
  - `FloatEventListener`
  - `IntEventListener`
  - `StringEventListener`

### Variables

- `VariableSO<T>`
  - Stores an initial design-time value and a runtime working value.
  - Resets runtime state in `OnEnable()`.
  - Can raise a typed event whenever the value changes.
- concrete variable assets
  - `BoolVariableSO`
  - `FloatVariableSO`
  - `IntVariableSO`
  - `StringVariableSO`
  - `Vector3VariableSO`
- references
  - `VariableReference<T>` switches between a constant and a `VariableSO<T>`.
  - concrete wrappers: `BoolReference`, `FloatReference`, `IntReference`, `StringReference`, `Vector3Reference`.

### Runtime sets

- `RuntimeSetSO<T>`
  - Shared runtime registries for scene objects or other tracked instances.
- concrete sets
  - `GameObjectRuntimeSetSO`
  - `TransformRuntimeSetSO`
- entries
  - `GameObjectRuntimeSetEntry`
  - `TransformRuntimeSetEntry`

### Logging

- `SingularityConsole`
  - Central log formatter and category resolver.
  - Routes messages by namespace and type name into framework categories.
- `SingularityConsoleModuleSO`
  - Optional module asset that binds custom channel settings to the logger.
  - Lets you enable, disable, and recolor log categories from the Inspector.

### Editor support

- `VariableReferenceDrawer`
  - Inspector drawer for the reference wrappers.
  - Lets a field switch between `Constant` and `Variable` modes without custom editor code in every consumer.

## How the Core layer works

The framework is built around a deliberate split:

1. Scene objects provide entry points.
2. ScriptableObject modules own behavior and long-lived state.
3. Events connect systems without direct references.
4. Variables expose state to UI and gameplay in a reactive way.

That means the scene usually stays very small. A typical setup contains:

- one `ModuleBootstrapper`
- one `ModuleRegistry` asset
- optional helper relays such as `UIRoot`
- game-specific controllers that only raise events or set variables

The modules themselves stay outside the scene and can be reused across scenes.

## Lifecycle sequence

The runtime sequence is:

1. Unity loads the scene.
2. `ModuleBootstrapper.Awake()` runs.
3. The assigned `ModuleRegistry` initializes each module in list order.
4. Each module is immediately activated after initialization.
5. Systems run during play.
6. When the bootstrap object is destroyed, the registry deactivates and disposes modules in reverse order.

Practical implication:

- place foundational modules first in the registry
- place dependent modules after the systems they rely on
- put logging early if you want startup logs from later modules

Recommended registry order:

1. `SingularityConsoleModuleSO`
2. `SaveModuleSO`
3. `CurrencyModuleSO`
4. `SoundModuleSO`
5. `UIModuleSO`
6. `BaseControllerModuleSO`
7. `AdsManagerModuleSO`

Adjust the order if your project has stricter dependencies.

## Setup tutorial

### 1. Create the registry

In the Project window:

1. Right-click.
2. Choose `Create > EventHorizon > Core > Module Registry`.
3. Name the asset something like `MainModuleRegistry`.

### 2. Create the bootstrap object

In your startup scene:

1. Add an empty `GameObject`.
2. Name it `Control Center` or `ModuleBootstrapper`.
3. Add the `ModuleBootstrapper` component.
4. Assign the `ModuleRegistry` asset.

### 3. Create shared event assets

You will use these everywhere. Start with a few:

- `GameEventSO` for button clicks, panel open/close requests, and save requests
- `StringEventSO` for scene keys
- `FloatEventSO` for audio sliders
- `IntEventSO` for score or counters if needed

Create them from:

- `Create > EventHorizon > Events > Game Event`
- `Create > EventHorizon > Events > Float Event`
- `Create > EventHorizon > Events > Int Event`
- `Create > EventHorizon > Events > String Event`

### 4. Create shared variable assets

Typical first-pass variables:

- `MasterVolume`
- `MusicVolume`
- `SFXVolume`
- `Coins`
- `Gems`
- `MusicEnabled`
- `Theme`

Create them from:

- `Create > EventHorizon > Variables > Float Variable`
- `Create > EventHorizon > Variables > Int Variable`
- `Create > EventHorizon > Variables > Bool Variable`
- `Create > EventHorizon > Variables > String Variable`

### 5. Connect variable change events when needed

Each variable can optionally raise a typed event on changes. Assign one when:

- UI should update reactively
- another module needs notification without polling
- you want inspector-driven listeners

Leave it empty if direct reads are enough.

## Patterns used in the code

### ScriptableObject modules

Why:

- easy to reuse across scenes
- serializable in the Inspector
- keeps framework state centralized
- avoids many scene wiring problems

### Event channels

Why:

- caller does not need a direct reference to the receiver
- many listeners can react to the same action
- works well for designer-driven setups

Tradeoff:

- debugging requires disciplined naming and documentation
- overusing events for everything can make flow harder to trace

### Variable assets

Why:

- UI can bind to shared data without hunting scene references
- values can be inspected during play
- settings and lightweight runtime state become asset-driven

Tradeoff:

- `VariableSO<T>.OnEnable()` resets runtime value to initial value, so persistence must be reapplied after startup

### Scene relays

Why:

- the scene remains thin
- scene-specific objects only bridge engine concerns into module assets
- modules stay testable and reusable

## Setup notes and pitfalls

- Only one active `ModuleBootstrapper` should drive a given registry at a time.
- Put all required module assets into the registry. Creating a module asset alone does nothing.
- `ModuleBootstrapper` only logs an error if no registry is assigned. It does not recover automatically.
- If you rely on saved variables or wallets, make sure the save system runs before you expect restored state.
- Variable assets reset on enable. That is expected behavior, not data loss.
- The generic `GetModule<T>()` lookup on `ModuleRegistry` exists as a last resort. Prefer event channels and assets instead.

## When to extend Core

Add to `Core` only if the feature is genuinely cross-cutting:

- generic event types
- generic variable types
- shared lifecycle contracts
- shared logging helpers
- editor tooling that supports the core data model

Do not add domain systems like inventory, combat, or progression here unless they are truly universal to the framework.

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
