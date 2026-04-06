using UnityEngine;

namespace EventHorizon.Sound
{
    /// <summary>
    /// Data container for a single audio cue. Configured entirely in the Inspector.
    /// Supports multiple clips, random selection, pitch variance, and looping.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Sound/Sound Cue", order = 0)]
    public class SoundCueSO : ScriptableObject
    {
        [Tooltip("One or more audio clips for this cue.")]
        [SerializeField] private AudioClip[] _clips;

        [Tooltip("Category for volume channel routing.")]
        [SerializeField] private SoundType _type = SoundType.SFX;

        [Tooltip("Base volume (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 1f;

        [Tooltip("Base pitch multiplier.")]
        [SerializeField] private float _pitch = 1f;

        [Tooltip("Random pitch offset applied per play (e.g., 0.1 means ±0.1).")]
        [SerializeField] private float _pitchVariance;

        [Tooltip("Whether this cue loops.")]
        [SerializeField] private bool _loop;

        [Tooltip("If true and multiple clips are assigned, a random clip is chosen each play.")]
        [SerializeField] private bool _randomiseClip;

        /// <summary>Audio clips assigned to this cue.</summary>
        public AudioClip[] Clips => _clips;

        /// <summary>Sound category for volume routing.</summary>
        public SoundType Type => _type;

        /// <summary>Base volume (0-1).</summary>
        public float Volume => _volume;

        /// <summary>Base pitch multiplier.</summary>
        public float Pitch => _pitch;

        /// <summary>Random pitch offset range applied per play.</summary>
        public float PitchVariance => _pitchVariance;

        /// <summary>Whether this cue loops playback.</summary>
        public bool Loop => _loop;

        /// <summary>Whether to randomly select from available clips.</summary>
        public bool RandomiseClip => _randomiseClip;

        /// <summary>
        /// Returns a clip to play — random if RandomiseClip is true, otherwise sequential index 0.
        /// </summary>
        public AudioClip GetClip()
        {
            if (_clips == null || _clips.Length == 0) return null;

            if (_randomiseClip)
            {
                return _clips[Random.Range(0, _clips.Length)];
            }

            return _clips[0];
        }

        /// <summary>
        /// Returns the pitch with random variance applied.
        /// </summary>
        public float GetRandomizedPitch()
        {
            return _pitch + Random.Range(-_pitchVariance, _pitchVariance);
        }
    }
}
