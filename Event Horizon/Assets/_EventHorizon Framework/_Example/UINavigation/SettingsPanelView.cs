using EventHorizon.Core;
using EventHorizon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EventHorizon.Example
{
    /// <summary>
    /// Minimal settings panel that demonstrates push/pop navigation.
    /// The close button raises a parameterless GameEventSO — wired in the Inspector
    /// to the same pop event that UIModuleSO listens to via the UIEventChannelSO.
    /// Zero direct reference to UIModuleSO or any navigation class.
    /// </summary>
    public class SettingsPanelView : UIViewBase
    {
        [Header("UI")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _closeButton;

        [Header("Events")]
        [Tooltip("Raise to request closing this view (typically the pop event from UIEventChannelSO).")]
        [SerializeField] private GameEventSO _closeRequestedEvent;

        public override void Bind()
        {
            base.Bind();

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
        }

        public override void Unbind()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnCloseClicked()
        {
            if (_closeRequestedEvent != null)
            {
                _closeRequestedEvent.Raise();
            }
        }
    }
}
