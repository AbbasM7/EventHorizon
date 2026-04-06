using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Sound
{
    /// <summary>
    /// Event channels for requesting sound playback and control.
    /// Game systems raise these events — only SoundModuleSO listens.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Sound/Sound Event Channel", order = 1)]
    public class SoundEventChannelSO : ScriptableObject
    {
        [Tooltip("Raise to request playback of a SoundCueSO.")]
        [SerializeField] private SoundCueEventSO _onPlayRequested;

        [Tooltip("Raise to request stopping a specific SoundCueSO.")]
        [SerializeField] private SoundCueEventSO _onStopRequested;

        [Tooltip("Raise to stop all currently playing sounds.")]
        [SerializeField] private GameEventSO _onStopAllRequested;

        /// <summary>Event to request playing a sound cue.</summary>
        public SoundCueEventSO OnPlayRequested => _onPlayRequested;

        /// <summary>Event to request stopping a specific sound cue.</summary>
        public SoundCueEventSO OnStopRequested => _onStopRequested;

        /// <summary>Event to request stopping all sounds.</summary>
        public GameEventSO OnStopAllRequested => _onStopAllRequested;
    }
}
