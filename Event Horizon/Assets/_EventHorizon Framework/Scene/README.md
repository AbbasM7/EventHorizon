# EventHorizon.Scene

```text
   .   *      .      /\       .
      .    *        /==\   .
   .       .       |::::|     *
      ORBITAL OBSERVATION DECK
```

| Document Field | Value |
|---|---|
| Layer | Runtime Feature Module |
| Assembly | `Assembly-CSharp` (folder-level framework module) |
| Primary Entry Point | `BaseControllerModuleSO` |
| Depends On | `EventHorizon.Core` |
| Technical Role | additive scene loading, registry-based key resolution, scene request handling |

`EventHorizon.Scene` is the additive scene loading layer for the framework. It keeps scene transitions asset-driven and decoupled from scene names by routing everything through:

- scene key strings
- a `SceneRegistrySO`
- a `SceneEventChannelSO`
- a `BaseControllerModuleSO`

This means gameplay scripts can request scene transitions without hardcoding actual Unity scene names.

## What is in this module

### Runtime module

- `BaseControllerModuleSO`
  - listens for scene load and unload requests
  - resolves keys through `SceneRegistrySO`
  - loads scenes additively
  - tracks only scenes it loaded itself
  - can also subscribe to direct trigger events configured per scene entry

### Scene metadata

- `SceneRegistrySO`
  - stores all `SceneEntryData` records
  - resolves a key to a Unity scene name
- `SceneEntryData`
  - stores:
    - a unique key
    - the exact scene name
    - optional direct load trigger
    - optional direct unload trigger

### Events

- `SceneEventChannelSO`
  - `OnLoadSceneRequested`
  - `OnUnloadSceneRequested`
  - `OnUnloadAllRequested`

## How the module works

### Key-based requests

When another system raises `OnLoadSceneRequested` with a key:

1. `BaseControllerModuleSO` receives the key
2. it asks `SceneRegistrySO` for the matching scene name
3. if found, it calls `SceneManager.LoadSceneAsync(..., Additive)`
4. after completion, the loaded scene name is tracked internally

Unload works the same way in reverse.

### Direct trigger requests

`SceneEntryData` can also store `GameEventSO` references for load and unload.

At initialization:

1. the controller iterates the registry
2. it subscribes handlers to each configured trigger
3. raising the trigger later loads or unloads that entry's scene directly

This is useful when you want asset-only scene wiring without string payloads at call sites.

### Unload-all behavior

The module only unloads scenes it previously tracked in `_loadedSceneNames`.

That means:

- it will not blindly unload every loaded scene in the game
- only additive scenes loaded through this controller are considered

## Setup tutorial

### 1. Create the scene event assets

Create:

- a `StringEventSO` for load requests
- a `StringEventSO` for unload requests
- a `GameEventSO` for unload-all requests

Then create:

- `Create > EventHorizon > Scene > Scene Event Channel`

Assign those assets.

### 2. Create the scene registry

1. Right-click.
2. Choose `Create > EventHorizon > Scene > Scene Registry`.
3. Add entries for every additive scene you want the module to manage.

For each entry set:

- `Key`
  - logical identifier like `GameplayHUD`, `Shop`, or `Results`
- `Scene Name`
  - exact Unity scene name as it appears in Build Settings
- optional `Load Trigger`
- optional `Unload Trigger`

### 3. Create the controller module

1. Right-click.
2. Choose `Create > EventHorizon > Scene > Base Controller Module`.
3. Assign:
   - `Scene Registry`
   - `Scene Event Channel`

Then add the module to your `ModuleRegistry`.

### 4. Add scenes to Build Settings

This step is required by Unity, not just the framework.

Make sure every scene referenced by `SceneRegistrySO` is included in Build Settings. If the scene name is valid in the registry but absent from the build list, loading will fail at runtime.

### 5. Raise scene requests

Example with keys:

```csharp
[SerializeField] private SceneEventChannelSO _sceneChannel;

public void OpenShop()
{
    _sceneChannel.OnLoadSceneRequested.Raise("Shop");
}

public void CloseShop()
{
    _sceneChannel.OnUnloadSceneRequested.Raise("Shop");
}
```

To unload everything the controller loaded:

```csharp
_sceneChannel.OnUnloadAllRequested.Raise();
```

## Recommended usage patterns

- Use short logical keys, not scene file names, in gameplay code.
- Reserve direct trigger events for places where string payloads are unnecessary.
- Keep this module focused on additive overlays, controllers, or content chunks.
- Use one registry as the source of truth for managed additive scenes.

## What this module does not do

It does not currently:

- set an active scene after loading
- manage loading screens
- coordinate async progress UI
- load by addressables
- prevent conflicting load requests beyond duplicate loaded-scene checks

If you need those features, this module is a good foundation, but you will need to extend it.

## Common pitfalls

- Scene key mismatch between caller and registry entry.
- Scene name mismatch between registry entry and Build Settings.
- Forgetting to add the controller module to the `ModuleRegistry`.
- Expecting it to manage non-additive scene replacement automatically.
- Expecting `UnloadAll` to affect scenes not loaded through this module.

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
