# EventHorizon.Save

```text
   .   *      .      /\       .
      .    *        /==\   .
   .       .       |::::|     *
      ORBITAL OBSERVATION DECK
```

| Document Field | Value |
|---|---|
| Layer | Runtime Feature Module |
| Assembly | `EventHorizon.Save` |
| Primary Entry Point | `SaveModuleSO` |
| Depends On | `EventHorizon.Core` |
| Technical Role | persistence orchestration, slot management, backend abstraction |

`EventHorizon.Save` is a pluggable persistence layer. It does not assume a specific save format for your game systems beyond one common contract:

- anything that implements `ISaveable` can participate

The save module gathers state from registered saveables, serializes it into a single `SaveData` container, and passes that string into a storage backend.

## Assembly dependency

- `EventHorizon.Save` depends on `EventHorizon.Core`

## Module contents

### Orchestration

- `SaveModuleSO`
  - runtime coordinator for save, load, delete, and slot switching

### Data model

- `SaveData`
  - serializable container that stores:
    - a list of key/value entries
    - a UTC timestamp

### Event channels

- `SaveEventChannelSO`
  - save requested
  - load requested
  - delete requested
  - save completed
  - load completed

### Storage backends

- `ISaveStorage`
  - backend interface
- `PlayerPrefsSaveStorage`
  - stores one save blob per key in PlayerPrefs
- `FileSaveStorage`
  - stores one save blob per key as a file under `Application.persistentDataPath`

## How the module works

### Save flow

When `SaveAll()` runs:

1. the module verifies the storage backend
2. it creates a new `SaveData`
3. it stamps the current UTC timestamp
4. it loops over every registered saveable
5. for each valid saveable:
   - `CaptureState()` is called
   - the result is stored under that saveable's `SaveKey`
6. the combined container is serialized to JSON
7. the backend writes the JSON under the current slot key
8. `OnSaveCompleted` is raised

### Load flow

When `LoadAll()` runs:

1. the module verifies the storage backend
2. it checks whether the current slot exists
3. it loads the raw JSON
4. it deserializes it into `SaveData`
5. it loops over every registered saveable
6. if a matching entry exists for a saveable's `SaveKey`, `RestoreState()` is called
7. `OnLoadCompleted` is raised

### Delete flow

When `DeleteSave()` runs:

1. the current slot key is deleted from the backend
2. the module logs the deletion

## Built-in saveable types in this framework

From the code in this repository, the following are natural saveable candidates:

- `FloatVariableSO`
- `IntVariableSO`
- `BoolVariableSO`
- `StringVariableSO`
- `CurrencyWalletSO`

Any custom `ScriptableObject` can join by implementing `ISaveable`.

## Setup tutorial

### 1. Choose a storage backend

Create one of the built-in backend assets:

- `Create > EventHorizon > Save > PlayerPrefs Storage`
- `Create > EventHorizon > Save > File Storage`

When to use each:

- `PlayerPrefsSaveStorage`
  - good for settings, unlock flags, small progress data
  - easy cross-platform option
- `FileSaveStorage`
  - better for larger structured saves
  - easier to inspect as real files

### 2. Create the save event assets

Create the parameterless `GameEventSO` assets for:

- save requested
- load requested
- delete requested
- save completed
- load completed

Then create:

- `Create > EventHorizon > Save > Save Event Channel`

Assign those event references.

### 3. Create the save module

1. Right-click.
2. Choose `Create > EventHorizon > Save > Save Module`.
3. Assign:
   - `Storage Asset`
   - `Save Slot Key`
   - `Event Channel`
   - `Registered Saveables`

Then add the `SaveModuleSO` to your `ModuleRegistry`.

### 4. Register saveable assets

Add every asset you want persisted to the save module's `Registered Saveables` list.

Typical examples:

- settings variables
- economy wallet
- progression variables
- any custom inventory or profile assets

### 5. Trigger save and load

Preferred event-driven usage:

```csharp
[SerializeField] private SaveEventChannelSO _saveChannel;

public void SaveGame()
{
    _saveChannel.OnSaveRequested.Raise();
}

public void LoadGame()
{
    _saveChannel.OnLoadRequested.Raise();
}
```

Direct usage is also available if you already own a module reference:

```csharp
saveModule.SaveAll();
saveModule.LoadAll();
saveModule.DeleteSave();
```

## Custom saveables

Implement `ISaveable` on any asset or runtime object you want persisted.

Example:

```csharp
using EventHorizon.Core;
using UnityEngine;

public class InventorySO : ScriptableObject, ISaveable
{
    public string SaveKey => "inventory";

    public string CaptureState()
    {
        return JsonUtility.ToJson(_data);
    }

    public void RestoreState(string state)
    {
        if (!string.IsNullOrEmpty(state))
        {
            _data = JsonUtility.FromJson<InventoryData>(state);
        }
    }

    [SerializeField] private InventoryData _data;
}
```

Then add that asset to `Registered Saveables`.

## Save slot support

The module stores one active slot key as a string. You can switch it at runtime:

```csharp
saveModule.SetSaveSlot("SaveSlot_1");
saveModule.LoadAll();
```

Use `HasSaveData()` before exposing a continue button:

```csharp
bool hasContinue = saveModule.HasSaveData();
```

## Storage backend details

### PlayerPrefsSaveStorage

Behavior:

- prefixes keys if a prefix is configured
- saves the full blob under one PlayerPrefs string
- calls `PlayerPrefs.Save()` after writes and deletes

Best for:

- simple profile settings
- mobile-friendly lightweight data

### FileSaveStorage

Behavior:

- writes one file per slot key
- stores files under `Application.persistentDataPath/<subfolder>`
- sanitizes invalid file-name characters

Best for:

- larger save blobs
- local debugging
- manual inspection of save JSON

## Integration advice

- Put saveables in a stable order only for inspector readability; loading is key-based, not index-based.
- Use stable `SaveKey` values. Renaming a save key after shipping can break restore behavior for existing data.
- Keep each saveable responsible for its own serialization format.
- Use the save event channel from UI and menus to keep scene code decoupled.

## Common pitfalls

- Assigning a `ScriptableObject` backend that does not implement `ISaveStorage`.
- Forgetting to add a saveable asset to `Registered Saveables`.
- Changing `SaveKey` or slot key names after save data already exists.
- Assuming variable assets persist automatically just because they hold runtime values.
- Expecting the save module to find saveables automatically; registration is manual by design.

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
