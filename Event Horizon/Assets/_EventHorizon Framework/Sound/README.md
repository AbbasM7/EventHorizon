# EventHorizon.Sound

```text
   .   *      .      /\       .
      .    *        /==\   .
   .       .       |::::|     *
      ORBITAL OBSERVATION DECK
```

| Document Field | Value |
|---|---|
| Layer | Runtime Feature Module |
| Assembly | `EventHorizon.Sound` |
| Primary Entry Point | `SoundModuleSO` |
| Depends On | `EventHorizon.Core` |
| Technical Role | event-driven playback, emitter pooling, channel volume propagation |

`EventHorizon.Sound` is an event-driven audio playback module. The design goal is simple:

- gameplay code should never need to create or manage `AudioSource` instances directly

Instead, audio content lives in `SoundCueSO` assets, requests flow through `SoundEventChannelSO`, and playback is centralized in `SoundModuleSO`.

## Assembly dependency

- `EventHorizon.Sound` depends on `EventHorizon.Core`

## Module contents

### Runtime module

- `SoundModuleSO`
  - central playback coordinator
  - pools SFX emitters
  - owns a dedicated music emitter
  - listens to the sound event channel
  - reacts to volume variable changes

### Data

- `SoundCueSO`
  - one or more clips
  - sound type
  - volume
  - pitch
  - pitch variance
  - loop flag
  - random clip selection flag
- `SoundType`
  - `SFX`
  - `Music`
  - `UI`

### Playback

- `SoundEmitter`
  - wrapper around one `AudioSource`
  - plays a cue and applies its clip, volume, pitch, and loop settings
- `ISoundPlayer`
  - consumer-facing interface

### Events

- `SoundEventChannelSO`
  - `OnPlayRequested`
  - `OnStopRequested`
  - `OnStopAllRequested`
- `SoundCueEventSO`
  - typed event for `SoundCueSO` payloads

## How the module works

### Startup

When `SoundModuleSO` initializes:

1. it creates a persistent parent object named `EventHorizon_Sound`
2. it builds an `ObjectPool<SoundEmitter>` for SFX playback
3. it instantiates one dedicated music emitter if a prefab is assigned
4. it subscribes to the sound event channel
5. it subscribes to master, music, and SFX volume variable events

### Playback routing

When a cue is played:

- `Music` cues go to the dedicated music emitter
- `SFX` and `UI` cues go through the pooled SFX emitters

The module distinguishes between channels through `SoundType` and current volume variables.

### SFX pooling

For non-music cues:

1. an emitter is requested from the pool
2. the cue is applied to its `AudioSource`
3. the emitter is added to the active list
4. if the cue is not looping, the emitter is returned after the clip duration

This avoids creating and destroying audio objects for every click, hit, or notification.

### Music behavior

Music uses one dedicated emitter:

- if music is already playing, it is stopped before the new cue starts
- this guarantees music does not stack accidentally

### Volume behavior

Volume is driven by `FloatVariableSO` assets:

- master volume affects everything
- music volume affects only the music channel
- SFX volume affects all active SFX/UI emitters

The module listens to variable change events and updates live emitters immediately.

## Setup tutorial

### 1. Create sound cue assets

For each sound:

1. Right-click.
2. Choose `Create > EventHorizon > Sound > Sound Cue`.
3. Configure:
   - clips
   - type
   - base volume
   - base pitch
   - pitch variance
   - loop
   - randomize clip

Suggested categories:

- one-shot button and UI sounds
- gameplay SFX
- ambient loops
- background music

### 2. Create the sound event assets

Create:

- one `SoundCueEventSO` for play requests
- one `SoundCueEventSO` for stop requests
- one `GameEventSO` for stop-all requests

Then create:

- `Create > EventHorizon > Sound > Sound Event Channel`

Assign those event assets.

### 3. Create volume variables

Create three `FloatVariableSO` assets:

- `MasterVolume`
- `MusicVolume`
- `SFXVolume`

Recommended initial values:

- `1.0`
- `1.0`
- `1.0`

If you want reactive updates, assign each variable's `OnValueChanged` event field.

### 4. Create emitter prefabs

Create at least one prefab with:

- `AudioSource`
- `SoundEmitter`

You can use the same prefab for SFX and music, or separate prefabs if you want different `AudioSource` defaults.

### 5. Create the module

1. Right-click.
2. Choose `Create > EventHorizon > Sound > Sound Module`.
3. Assign:
   - event channel
   - SFX emitter prefab
   - music emitter prefab
   - pool size
   - master volume variable
   - music volume variable
   - SFX volume variable

Then add the module to your `ModuleRegistry`.

### 6. Trigger playback

Event-driven usage:

```csharp
[SerializeField] private SoundEventChannelSO _soundChannel;
[SerializeField] private SoundCueSO _buttonClickCue;

public void OnButtonClicked()
{
    _soundChannel.OnPlayRequested.Raise(_buttonClickCue);
}
```

To stop one cue:

```csharp
_soundChannel.OnStopRequested.Raise(_buttonClickCue);
```

To stop all active sound:

```csharp
_soundChannel.OnStopAllRequested.Raise();
```

## Settings UI integration

Volume sliders should usually write into the variable assets, not call the sound module directly.

Example:

```csharp
[SerializeField] private FloatVariableSO _masterVolume;

public void OnMasterVolumeChanged(float value)
{
    _masterVolume.SetValue(value);
}
```

Because the sound module listens to those variable events, the audio updates automatically.

## Recommended conventions

- Use `Music` for long-running background tracks.
- Use `UI` and `SFX` as separate cue categories if you want to evolve the module later.
- Keep clip randomness inside `SoundCueSO` instead of writing variation logic in gameplay scripts.
- Use one shared event channel for project-wide decoupled sound triggers.

## Common pitfalls

- Forgetting to assign emitter prefabs.
- Expecting sound playback without adding `SoundModuleSO` to the registry.
- Changing volume variable values without assigning or listening to their `OnValueChanged` events where reactive updates matter.
- Using looping SFX cues without a stop path.
- Using non-looping clips for music and expecting them to persist naturally.

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
