using System.Collections.Generic;
using EventHorizon.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace EventHorizon.Sound
{
    /// <summary>
    /// Audio playback orchestration module. Manages SFX pooling via UnityEngine.Pool,
    /// a dedicated music emitter, and volume channels driven by FloatVariableSO assets.
    /// All sound requests arrive through SoundEventChannelSO â€” fully decoupled from callers.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Sound/Sound Module", order = 0)]
    public class SoundModuleSO : ModuleBase, ISoundPlayer
    {
        [Header("Event Channel")]
        [Tooltip("Event channel for play/stop requests.")]
        [SerializeField] private SoundEventChannelSO _eventChannel;

        [Header("Prefabs")]
        [Tooltip("Prefab for SFX/UI emitters (pooled).")]
        [SerializeField] private SoundEmitter _sfxEmitterPrefab;

        [Tooltip("Prefab for the dedicated music emitter.")]
        [SerializeField] private SoundEmitter _musicEmitterPrefab;

        [Header("Pool Settings")]
        [Tooltip("Initial and default pool size for SFX emitters.")]
        [SerializeField] private int _sfxPoolSize = 8;

        [Header("Volume Variables")]
        [Tooltip("Master volume (0-1). Multiplied with channel volumes.")]
        [SerializeField] private FloatVariableSO _masterVolume;

        [Tooltip("Music channel volume (0-1).")]
        [SerializeField] private FloatVariableSO _musicVolume;

        [Tooltip("SFX channel volume (0-1).")]
        [SerializeField] private FloatVariableSO _sfxVolume;

        private GameObject _poolParent;
        private ObjectPool<SoundEmitter> _sfxPool;
        private SoundEmitter _musicEmitter;
        private readonly List<SoundEmitter> _activeSfxEmitters = new List<SoundEmitter>();

        /// <summary>
        /// Plays the specified sound cue, routing through the appropriate channel.
        /// </summary>
        public void PlayCue(SoundCueSO cue)
        {
            if (cue == null) return;

            if (cue.Type == SoundType.Music)
            {
                PlayMusic(cue);
            }
            else
            {
                PlaySFX(cue);
            }
        }

        /// <summary>
        /// Stops the specified cue if currently playing.
        /// </summary>
        public void StopCue(SoundCueSO cue)
        {
            if (cue == null) return;

            if (cue.Type == SoundType.Music)
            {
                if (_musicEmitter != null && _musicEmitter.CurrentCue == cue)
                {
                    _musicEmitter.Stop();
                }
            }
            else
            {
                for (int i = _activeSfxEmitters.Count - 1; i >= 0; i--)
                {
                    if (_activeSfxEmitters[i].CurrentCue == cue)
                    {
                        ReturnEmitterToPool(_activeSfxEmitters[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Stops all currently playing sounds across all channels.
        /// </summary>
        public void StopAll()
        {
            if (_musicEmitter != null)
            {
                _musicEmitter.Stop();
            }

            for (int i = _activeSfxEmitters.Count - 1; i >= 0; i--)
            {
                ReturnEmitterToPool(_activeSfxEmitters[i]);
            }
        }

        /// <summary>
        /// Sets the master volume and updates all active emitters.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            UpdateAllVolumes();
        }

        /// <summary>
        /// Sets the music channel volume and updates the music emitter.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            UpdateMusicVolume();
        }

        /// <summary>
        /// Sets the SFX channel volume and updates all active SFX emitters.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            UpdateSFXVolumes();
        }

        /// <summary>
        /// Performs module-specific initialization logic.
        /// </summary>
        protected override void OnInitialize()
        {
            _poolParent = new GameObject("EventHorizon_Sound");
            Object.DontDestroyOnLoad(_poolParent);

            InitializeSFXPool();
            InitializeMusicEmitter();
            SubscribeToEvents();
            SubscribeToVolumeChanges();
        }

        /// <summary>
        /// Performs module-specific cleanup logic.
        /// </summary>
        protected override void OnDispose()
        {
            StopAll();
            UnsubscribeFromEvents();
            UnsubscribeFromVolumeChanges();

            _sfxPool?.Clear();
            _sfxPool = null;
            _activeSfxEmitters.Clear();

            if (_poolParent != null)
            {
                Object.Destroy(_poolParent);
                _poolParent = null;
            }

            _musicEmitter = null;
        }

        /// <summary>
        /// Executes initialize sfxpool.
        /// </summary>
        private void InitializeSFXPool()
        {
            _sfxPool = new ObjectPool<SoundEmitter>(
                createFunc: () =>
                {
                    SoundEmitter emitter = Object.Instantiate(_sfxEmitterPrefab, _poolParent.transform);
                    emitter.gameObject.SetActive(false);
                    return emitter;
                },
                actionOnGet: emitter =>
                {
                    emitter.gameObject.SetActive(true);
                },
                actionOnRelease: emitter =>
                {
                    emitter.Stop();
                    emitter.gameObject.SetActive(false);
                },
                actionOnDestroy: emitter =>
                {
                    if (emitter != null)
                    {
                        Object.Destroy(emitter.gameObject);
                    }
                },
                defaultCapacity: _sfxPoolSize,
                maxSize: _sfxPoolSize * 2
            );
        }

        /// <summary>
        /// Executes initialize music emitter.
        /// </summary>
        private void InitializeMusicEmitter()
        {
            if (_musicEmitterPrefab != null)
            {
                _musicEmitter = Object.Instantiate(_musicEmitterPrefab, _poolParent.transform);
                _musicEmitter.gameObject.name = "MusicEmitter";
            }
        }

        /// <summary>
        /// Plays music.
        /// </summary>
        private void PlayMusic(SoundCueSO cue)
        {
            if (_musicEmitter == null) return;

            if (_musicEmitter.IsPlaying)
            {
                _musicEmitter.Stop();
            }

            _musicEmitter.PlayCue(cue);
            UpdateMusicVolume();
        }

        /// <summary>
        /// Plays sfx.
        /// </summary>
        private void PlaySFX(SoundCueSO cue)
        {
            if (_sfxPool == null) return;

            SoundEmitter emitter = _sfxPool.Get();
            _activeSfxEmitters.Add(emitter);
            emitter.PlayCue(cue);

            float channelVolume = GetSFXChannelVolume();
            emitter.SetVolume(cue.Volume * channelVolume);

            if (!cue.Loop)
            {
                AudioClip clip = cue.GetClip();
                float duration = clip != null ? clip.length / Mathf.Abs(emitter.IsPlaying ? cue.Pitch : 1f) : 1f;
                ReturnEmitterAfterDelay(emitter, duration);
            }
        }

        /// <summary>
        /// Executes return emitter after delay.
        /// </summary>
        private async void ReturnEmitterAfterDelay(SoundEmitter emitter, float delay)
        {
            await Awaitable.WaitForSecondsAsync(delay);

            if (emitter != null && _activeSfxEmitters.Contains(emitter))
            {
                ReturnEmitterToPool(emitter);
            }
        }

        /// <summary>
        /// Executes return emitter to pool.
        /// </summary>
        private void ReturnEmitterToPool(SoundEmitter emitter)
        {
            _activeSfxEmitters.Remove(emitter);

            if (_sfxPool != null && emitter != null)
            {
                _sfxPool.Release(emitter);
            }
        }

        /// <summary>
        /// Subscribes to to events.
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_eventChannel == null) return;

            if (_eventChannel.OnPlayRequested != null)
                _eventChannel.OnPlayRequested.RegisterListener(PlayCue);

            if (_eventChannel.OnStopRequested != null)
                _eventChannel.OnStopRequested.RegisterListener(StopCue);

            if (_eventChannel.OnStopAllRequested != null)
                _eventChannel.OnStopAllRequested.RegisterListener(StopAll);
        }

        /// <summary>
        /// Unsubscribes from from events.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_eventChannel == null) return;

            if (_eventChannel.OnPlayRequested != null)
                _eventChannel.OnPlayRequested.UnregisterListener(PlayCue);

            if (_eventChannel.OnStopRequested != null)
                _eventChannel.OnStopRequested.UnregisterListener(StopCue);

            if (_eventChannel.OnStopAllRequested != null)
                _eventChannel.OnStopAllRequested.UnregisterListener(StopAll);
        }

        /// <summary>
        /// Subscribes to to volume changes.
        /// </summary>
        private void SubscribeToVolumeChanges()
        {
            if (_masterVolume != null && _masterVolume.OnValueChanged != null)
                _masterVolume.OnValueChanged.RegisterListener(OnMasterVolumeChanged);

            if (_musicVolume != null && _musicVolume.OnValueChanged != null)
                _musicVolume.OnValueChanged.RegisterListener(OnMusicVolumeChanged);

            if (_sfxVolume != null && _sfxVolume.OnValueChanged != null)
                _sfxVolume.OnValueChanged.RegisterListener(OnSFXVolumeChanged);
        }

        /// <summary>
        /// Unsubscribes from from volume changes.
        /// </summary>
        private void UnsubscribeFromVolumeChanges()
        {
            if (_masterVolume != null && _masterVolume.OnValueChanged != null)
                _masterVolume.OnValueChanged.UnregisterListener(OnMasterVolumeChanged);

            if (_musicVolume != null && _musicVolume.OnValueChanged != null)
                _musicVolume.OnValueChanged.UnregisterListener(OnMusicVolumeChanged);

            if (_sfxVolume != null && _sfxVolume.OnValueChanged != null)
                _sfxVolume.OnValueChanged.UnregisterListener(OnSFXVolumeChanged);
        }

        /// <summary>
        /// Handles changes to master volume.
        /// </summary>
        private void OnMasterVolumeChanged(float value) => UpdateAllVolumes();
        /// <summary>
        /// Handles changes to music volume.
        /// </summary>
        private void OnMusicVolumeChanged(float value) => UpdateMusicVolume();
        /// <summary>
        /// Handles changes to sfxvolume.
        /// </summary>
        private void OnSFXVolumeChanged(float value) => UpdateSFXVolumes();

        /// <summary>
        /// Updates all volumes.
        /// </summary>
        private void UpdateAllVolumes()
        {
            UpdateMusicVolume();
            UpdateSFXVolumes();
        }

        /// <summary>
        /// Updates music volume.
        /// </summary>
        private void UpdateMusicVolume()
        {
            if (_musicEmitter == null || _musicEmitter.CurrentCue == null) return;

            float master = _masterVolume != null ? _masterVolume.RuntimeValue : 1f;
            float music = _musicVolume != null ? _musicVolume.RuntimeValue : 1f;
            _musicEmitter.SetVolume(_musicEmitter.CurrentCue.Volume * master * music);
        }

        /// <summary>
        /// Updates sfxvolumes.
        /// </summary>
        private void UpdateSFXVolumes()
        {
            float channelVolume = GetSFXChannelVolume();

            for (int i = 0; i < _activeSfxEmitters.Count; i++)
            {
                SoundEmitter emitter = _activeSfxEmitters[i];
                if (emitter != null && emitter.CurrentCue != null)
                {
                    emitter.SetVolume(emitter.CurrentCue.Volume * channelVolume);
                }
            }
        }

        /// <summary>
        /// Gets sfxchannel volume.
        /// </summary>
        private float GetSFXChannelVolume()
        {
            float master = _masterVolume != null ? _masterVolume.RuntimeValue : 1f;
            float sfx = _sfxVolume != null ? _sfxVolume.RuntimeValue : 1f;
            return master * sfx;
        }
    }
}
