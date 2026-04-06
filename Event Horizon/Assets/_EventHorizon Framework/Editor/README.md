# EventHorizon.Editor

```text
   .   *      .      /\       .
      .    *        /==\   .
   .       .       |::::|     *
      ORBITAL OBSERVATION DECK
```

| Document Field | Value |
|---|---|
| Layer | Editor Tooling |
| Assembly | `EventHorizon.Editor` |
| Primary Entry Points | `LaunchSequence`, `ObservationDeck`, `DataOrbit`, `StarView` |
| Depends On | `EventHorizon.Core`, `EventHorizon.Core.Editor`, `EventHorizon.UI` |
| Technical Role | setup tooling, diagnostics, save inspection, workspace utilities |

`EventHorizon.Editor` is the tooling layer for the framework. It contains editor-only utilities that make the runtime modules easier to inspect, bootstrap, and debug inside Unity.

This module is editor-only by assembly definition and depends on:

- `EventHorizon.Core`
- `EventHorizon.Core.Editor`
- `EventHorizon.UI`

## What is in this module

### Launch and bootstrap tools

- `LaunchSequence`
  - mission-control style setup window
  - scans the current scene for required launch hardware
  - can create:
    - a `ModuleBootstrapper` scene object
    - a `Canvas` with `UIRoot`
  - available from `EventHorizon/Launch Sequence`

### Save inspection and editing

- `DataOrbit`
  - advanced PlayerPrefs and JSON save inspector
  - reads:
    - normal PlayerPrefs keys
    - JSON save containers stored in PlayerPrefs
    - external JSON save files
  - supports editing supported values from the Unity Editor
  - available from `EventHorizon/DataOrbit`

### Logging dashboard

- `ObservationDeck`
  - styled console viewer for EventHorizon logs
  - reads from `ObservationDeckLogStore`
  - supports:
    - category filtering
    - searching
    - duplicate collapsing
    - source trace inspection
    - opening source files from stack frames
  - available from `EventHorizon/Observation Deck`
- `ObservationDeckLogStore`
  - hooks into `Application.logMessageReceived`
  - captures logs, strips rich text for indexing, and extracts category tags from messages

### Visual shell and utility windows

- `StarView`
  - lightweight branded editor dashboard window
- `EventHorizonEditorFont`
  - resolves the bundled Cascadia Mono font for all editor windows

## How the editor tooling fits into the framework

The runtime modules are intentionally asset-heavy and event-driven. That architecture is flexible, but it also means debugging can get abstract if you only use Unity's default inspector and console.

The editor package addresses that by providing three practical tools:

- bootstrap help with `LaunchSequence`
- save and preference inspection with `DataOrbit`
- categorized log monitoring with `ObservationDeck`

## Setup tutorial

### 1. Ensure the editor assembly compiles

This module is already in an editor-only assembly:

- `Assets/_EventHorizon/Editor/EventHorizon.Editor.asmdef`

No extra code setup is required as long as the project imports `_EventHorizon` intact.

### 2. Open the setup window

From the Unity menu:

- `EventHorizon > Launch Sequence`

Use it to verify whether the current scene already contains:

- a `ModuleBootstrapper`
- a `Canvas`
- a `UIRoot`

If missing, the window can generate the basic scene scaffolding for you.

### 3. Configure logging

To get the best results from `ObservationDeck`:

1. create a `SingularityConsoleModuleSO`
2. add it early in your `ModuleRegistry`
3. play the scene
4. open `EventHorizon > Observation Deck`

Because `SingularityConsole` formats logs with category tags like `[CORE]` and `[UI]`, the observation tool can group them correctly.

### 4. Configure save inspection

If you use `EventHorizon.Save`, open:

- `EventHorizon > DataOrbit`

Use it to inspect:

- PlayerPrefs values
- known JSON container keys such as `SaveSlot_0`
- external file saves under `Application.persistentDataPath`

This is especially useful when validating:

- save slot switching
- variable persistence
- wallet balance restores

## Tool-by-tool usage

### LaunchSequence

Use when:

- starting a new scene
- confirming scene wiring
- onboarding someone to the framework

What it checks:

- `ModuleBootstrapper` existence
- `UIRoot` existence

What it creates:

- `Control Center` game object with `ModuleBootstrapper`
- `UICanvas` with:
  - `Canvas`
  - `CanvasScaler`
  - `GraphicRaycaster`
  - `ViewContainer`
  - `UIRoot`

After generation you still need to assign:

- the `ModuleRegistry` on `ModuleBootstrapper`
- the `UIModuleSO` on `UIRoot`

### DataOrbit

Use when:

- debugging PlayerPrefs-backed settings
- inspecting save JSON without leaving the editor
- editing test values quickly
- checking file storage output

Important behavior:

- it understands EventHorizon-style save containers with `Entries` and `Timestamp`
- it can flatten nested entries so individual saveables become editable rows
- it distinguishes between direct PlayerPrefs entries and values extracted from containers

Recommended use cases:

- validate `SaveModuleSO.SaveAll()`
- inspect wallet balances in saved JSON
- change a test slot value without writing custom debug menus

### ObservationDeck

Use when:

- tracking startup order
- checking module lifecycle logs
- debugging ads, UI navigation, save flow, or scene loading

Useful detail:

- it parses the category tag from the formatted log string
- it can open code files from stack traces
- it stores a capped rolling buffer of recent logs

### StarView

Use when:

- you want the custom workspace window
- you want a quick branded entry point in the editor

It is not required for runtime operation.

## Notes and limitations

- All of these tools are editor-only. None of them are required in a build.
- `DataOrbit` is most useful when your project follows the save conventions already used in this framework.
- `ObservationDeck` only sees logs that reach Unity's logging pipeline.
- `LaunchSequence` creates scaffold objects, but it does not infer your project-specific assets automatically.

## Recommended workflow

1. Open `LaunchSequence` when creating or validating a bootstrap scene.
2. Add `SingularityConsoleModuleSO` early in the registry.
3. Run the game and monitor `ObservationDeck` during initialization.
4. Use `DataOrbit` to verify saved state after testing save and load flows.

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
