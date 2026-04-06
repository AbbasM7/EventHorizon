using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Sound
{
    /// <summary>
    /// Typed event channel that broadcasts a SoundCueSO reference.
    /// Used by SoundEventChannelSO for play/stop requests.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Sound/Sound Cue Event", order = 2)]
    public class SoundCueEventSO : GameEventSO<SoundCueSO>
    {
    }
}
