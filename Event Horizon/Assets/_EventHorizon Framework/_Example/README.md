# EventHorizon.Example

```text
   .   *      .      /\       .
      .    *        /==\   .
   .       .       |::::|     *
      ORBITAL OBSERVATION DECK
```

| Document Field | Value |
|---|---|
| Layer | Reference / Example Layer |
| Assembly | `EventHorizon.Example` |
| Primary Entry Points | example `MonoBehaviour` and `UIViewBase` consumers |
| Depends On | `EventHorizon.Core`, `EventHorizon.UI`, `EventHorizon.Currency`, `EventHorizon.Sound`, `Unity.TextMeshPro` |
| Technical Role | demonstrate intended integration patterns without production coupling |

`_Example` is the reference layer for the framework. It is not a production system. It exists to show how the modules are intended to be consumed from normal game code.

Assembly dependencies:

- `EventHorizon.Core`
- `EventHorizon.UI`
- `EventHorizon.Currency`
- `EventHorizon.Sound`
- `Unity.TextMeshPro`

## What the example folder demonstrates

### Currency examples

- `CurrencyBalanceView`
  - shows a minimal reactive currency readout using `IntVariableSO`
- `CurrencyView`
  - similar reactive display for coins and gems

What these examples teach:

- UI should bind to variables, not the currency module directly
- a view can update instantly when a variable changes without polling

### Sound examples

- `SoundTestTrigger`
  - shows how a normal `MonoBehaviour` can trigger play and stop through `SoundEventChannelSO`

What this teaches:

- gameplay code does not need a `SoundModuleSO` reference
- button-driven sound playback can remain fully decoupled

### Navigation examples

- `MockController`
  - raises a show event on `Start()`
- `SettingsPanelView`
  - raises a close request event instead of talking to the UI module directly

What this teaches:

- view display is event-driven
- closing a view is usually a navigation request, not a direct stack mutation from the view itself

### General UI examples

- `SettingsView`
  - binds booleans and strings into toggles and a dropdown
  - writes changes back into variables
- `HealthBarView`
  - intended to show a health bar binding pattern
  - currently contains commented-out binding code, so treat it as a structural sketch rather than a complete ready-to-drop example

## How to use the examples

These scripts are best read as integration templates.

Recommended workflow:

1. Create the required framework assets first.
2. Build a basic scene with `ModuleBootstrapper`, `ModuleRegistry`, `Canvas`, and `UIRoot`.
3. Add the modules you want to test.
4. Create the variables, events, and definitions the example script expects.
5. Drop the example script on a suitable GameObject or prefab.
6. Wire every serialized field explicitly in the Inspector.

## Example setup tutorial

### 1. Bootstrap the framework

At minimum, create:

- `ModuleRegistry`
- `ModuleBootstrapper`
- `UIModuleSO` if you want UI examples
- `SoundModuleSO` if you want sound examples
- `CurrencyModuleSO` if you want currency examples

### 2. Create data assets the examples depend on

Typical assets:

- `IntVariableSO` for coins and gems
- `BoolVariableSO` for settings toggles
- `StringVariableSO` for theme selection
- `GameEventSO` to show a sample panel
- `SoundCueSO` and `SoundEventChannelSO` for sound

### 3. Build a sample canvas

For UI examples, create a canvas with:

- `TMP_Text` labels
- `Toggle` components
- `Button` components
- `TMP_Dropdown` where needed
- optional `Slider` for the health bar pattern

### 4. Wire the example components

For instance:

- `SettingsView`
  - assign the four variable assets
  - assign three toggles and one dropdown
- `SoundTestTrigger`
  - assign the event channel
  - assign the cue
  - assign play and stop buttons
- `MockController`
  - assign the `GameEventSO` that is also used by a `UIViewDefinitionSO` as its show event

### 5. Enter Play Mode and validate the intended flow

You should see:

- UI values update from variable state
- changing toggles writes back into variable assets
- sound requests fire through the event channel
- navigation requests show and hide views without direct module references

## What these examples are trying to teach

The examples consistently reinforce these framework rules:

- data in through variables
- actions out through events
- modules own systems
- views do not hold domain authority
- scene scripts are usually just relays

## Important note about example quality

These examples are intentionally small and focused. They are not a complete sample game.

In particular:

- `HealthBarView` is currently partial and commented out
- some examples assume you will create matching variables and events yourself
- they show framework usage patterns more than production polish

Use them as templates, not as drop-in finished features.

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
