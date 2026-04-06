using UnityEngine;

namespace EventHorizon.Sound
{
    /// <summary>
    /// Wraps a single AudioSource for playing SoundCueSO data.
    /// Managed by SoundModuleSO via UnityEngine.Pool â€” never instantiated ad hoc.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        private AudioSource _audioSource;

        /// <summary>
        /// Whether this emitter is currently playing audio.
        /// </summary>
        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

        /// <summary>
        /// The cue currently assigned to this emitter, if any.
        /// </summary>
        public SoundCueSO CurrentCue { get; private set; }

        /// <summary>
        /// Plays the specified cue, applying all settings from the SoundCueSO asset.
        /// </summary>
        public void PlayCue(SoundCueSO cue)
        {
            if (cue == null) return;

            EnsureAudioSource();

            CurrentCue = cue;

            AudioClip clip = cue.GetClip();
            if (clip == null) return;

            _audioSource.clip = clip;
            _audioSource.volume = cue.Volume;
            _audioSource.pitch = cue.GetRandomizedPitch();
            _audioSource.loop = cue.Loop;
            _audioSource.Play();
        }

        /// <summary>
        /// Stops the currently playing audio.
        /// </summary>
        public void Stop()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            CurrentCue = null;
        }

        /// <summary>
        /// Sets the volume on the underlying AudioSource.
        /// </summary>
        public void SetVolume(float volume)
        {
            EnsureAudioSource();
            _audioSource.volume = volume;
        }

        /// <summary>
        /// Initializes references when the component awakens.
        /// </summary>
        private void Awake()
        {
            EnsureAudioSource();
        }

        /// <summary>
        /// Ensures audio source is ready before continuing.
        /// </summary>
        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }
    }
}
