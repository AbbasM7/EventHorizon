using EventHorizon.Sound;
using UnityEngine;
using UnityEngine.UI;

namespace EventHorizon.Example
{
    /// <summary>
    /// Example MonoBehaviour that triggers sound playback via the event channel.
    /// Demonstrates fully decoupled audio — zero direct reference to SoundModuleSO.
    /// </summary>
    public class SoundTestTrigger : MonoBehaviour
    {
        [SerializeField] private SoundEventChannelSO _eventChannel;
        [SerializeField] private SoundCueSO _cueToPlay;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _stopButton;

        private void OnEnable()
        {
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayClicked);

            if (_stopButton != null)
                _stopButton.onClick.AddListener(OnStopClicked);
        }

        private void OnDisable()
        {
            if (_playButton != null)
                _playButton.onClick.RemoveListener(OnPlayClicked);

            if (_stopButton != null)
                _stopButton.onClick.RemoveListener(OnStopClicked);
        }

        private void OnPlayClicked()
        {
            if (_eventChannel != null && _eventChannel.OnPlayRequested != null && _cueToPlay != null)
            {
                _eventChannel.OnPlayRequested.Raise(_cueToPlay);
            }
        }

        private void OnStopClicked()
        {
            if (_eventChannel != null && _eventChannel.OnStopRequested != null && _cueToPlay != null)
            {
                _eventChannel.OnStopRequested.Raise(_cueToPlay);
            }
        }
    }
}
