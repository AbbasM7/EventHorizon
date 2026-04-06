# EventHorizon.UI

```text
   .   *      .      /\       .
      .    *        /==\   .
   .       .       |::::|     *
      ORBITAL OBSERVATION DECK
```

| Document Field | Value |
|---|---|
| Layer | Runtime Feature Module |
| Assembly | `EventHorizon.UI` |
| Primary Entry Point | `UIModuleSO` |
| Depends On | `EventHorizon.Core` |
| Technical Role | runtime view management, stack navigation, variable-bound presentation |

`EventHorizon.UI` is a stack-driven UI framework built around `ScriptableObject` assets and runtime-instantiated view prefabs.

The main architectural rule is:

- views are passive
- data comes in through variables
- actions go out through events
- the UI module owns navigation and instance lifetime

## Assembly dependency

- `EventHorizon.UI` depends on `EventHorizon.Core`

## Module contents

### Runtime module

- `UIModuleSO`
  - owns view registration
  - subscribes to show and hide events per view definition
  - instantiates prefabs at runtime
  - manages navigation through `UINavigationStack`

### Scene relay

- `UIRoot`
  - scene-side bridge that gives the UI module a container transform where instantiated views should live

### Navigation

- `UINavigationStack`
  - pure C# stack abstraction
  - hides the previous top when pushing a new view
  - shows the previous view when popping
  - removes instances from anywhere in the stack if requested

### Views

- `UIViewBase`
  - common base class for views
  - handles show/hide
  - supports variable binding with automatic cleanup
- `IUIView`
  - navigation contract
- `IDataBindable`
  - data-binding contract
- `UIViewDefinitionSO`
  - binds a prefab to a show event and optional hide event

### Events

- `UIEventChannelSO`
  - pop top
  - pop to root
  - clear stack
  - hide all views

## How the module works

### Registration model

The UI module does not discover views automatically. It only manages views listed in its `Registered Views` list.

Each `UIViewDefinitionSO` tells the module:

- which prefab to instantiate
- which `GameEventSO` should push it
- which `GameEventSO` should hide and remove it

### Show flow

When a view's `ShowEvent` is raised:

1. the UI module receives the event
2. it instantiates the prefab under the assigned container
3. it stores the instance in the active instance map
4. it pushes the instance onto the navigation stack
5. the stack hides the previous top view and shows the new one

### Hide and removal flow

When a definition's `HideEvent` is raised:

1. the UI module looks up the active instance for that definition
2. the navigation stack removes it
3. `OnViewRemoved` fires
4. the module destroys the instantiated GameObject

### Data binding flow

`UIViewBase.BindVariable()`:

1. immediately invokes the callback with the current runtime value
2. registers a listener on the variable's `OnValueChanged`
3. records an unbind action
4. automatically unregisters everything in `Unbind()`

This is what makes the views reactive without manual subscription bookkeeping in every screen.

## Setup tutorial

### 1. Create the scene root

In the scene:

1. create or use a `Canvas`
2. create a child object that will hold runtime views
3. add `UIRoot` to a suitable object
4. assign:
   - the `UIModuleSO`
   - the container `Transform`

`LaunchSequence` from the editor tools can scaffold this for you automatically.

### 2. Create navigation event assets

Create `GameEventSO` assets for:

- pop
- pop to root
- clear stack
- hide all

Then create:

- `Create > EventHorizon > UI > UI Event Channel`

Assign those events.

### 3. Create view prefabs

For each screen, popup, or panel:

1. create a prefab with a component inheriting from `UIViewBase`
2. wire serialized references such as labels, buttons, variables, and events

### 4. Create view definition assets

For each prefab:

1. Right-click.
2. Choose `Create > EventHorizon > UI > View Definition`.
3. Assign:
   - `Prefab`
   - `Show Event`
   - optional `Hide Event`

### 5. Create the UI module

1. Right-click.
2. Choose `Create > EventHorizon > UI > UI Module`.
3. Assign:
   - all registered view definitions
   - the `UIEventChannelSO`

Then add it to the `ModuleRegistry`.

### 6. Trigger views from game code

To show a view:

```csharp
[SerializeField] private GameEventSO _showSettings;

public void OpenSettings()
{
    _showSettings.Raise();
}
```

To pop the current view:

```csharp
[SerializeField] private UIEventChannelSO _uiChannel;

public void CloseTop()
{
    _uiChannel.OnPopViewRequested.Raise();
}
```

## View authoring tutorial

### Minimal view

```csharp
using EventHorizon.UI;

public class MyView : UIViewBase
{
    protected override void OnShow()
    {
    }

    protected override void OnHide()
    {
        base.OnHide();
    }
}
```

### Bind a variable

```csharp
using EventHorizon.Core;
using EventHorizon.UI;
using TMPro;
using UnityEngine;

public class ScoreView : UIViewBase
{
    [SerializeField] private IntVariableSO _score;
    [SerializeField] private TMP_Text _label;

    public override void Bind()
    {
        base.Bind();
        BindVariable(_score, OnScoreChanged);
    }

    private void OnScoreChanged(int value)
    {
        _label.text = value.ToString();
    }
}
```

### Raise an event from a button

```csharp
using EventHorizon.Core;
using EventHorizon.UI;
using UnityEngine;
using UnityEngine.UI;

public class PauseButtonView : UIViewBase
{
    [SerializeField] private GameEventSO _pauseRequested;
    [SerializeField] private Button _button;

    public override void Bind()
    {
        base.Bind();
        _button.onClick.AddListener(OnClicked);
    }

    public override void Unbind()
    {
        _button.onClick.RemoveListener(OnClicked);
        base.Unbind();
    }

    private void OnClicked()
    {
        _pauseRequested.Raise();
    }
}
```

## Recommended conventions

- Use one `UIViewDefinitionSO` per view prefab.
- Use `GameEventSO` assets as public navigation endpoints.
- Keep business logic out of views; views should display data and emit intent.
- Use variable assets for reactive state instead of pulling module references into UI classes.
- Let the module own instantiation and destruction instead of placing every possible panel in the scene.

## Common pitfalls

- Forgetting to assign `UIRoot`, which leaves the UI module with no container.
- Forgetting to register a view definition in `UIModuleSO`.
- Calling `Show()` or `Hide()` on views directly from outside the navigation system.
- Overriding `Unbind()` and forgetting to call `base.Unbind()` when variable bindings are used.
- Assuming a hide event just disables a prefab template; it removes the instantiated runtime instance from the stack.

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
