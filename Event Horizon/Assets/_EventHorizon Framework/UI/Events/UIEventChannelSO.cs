using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.UI
{
    /// <summary>
    /// Event channel for UI navigation requests (pop, popToRoot, clear).
    /// Push is handled per-definition via UIViewDefinitionSO.ShowEvent.
    /// Game systems raise these events — only UIModuleSO listens.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/UI/UI Event Channel", order = 1)]
    public class UIEventChannelSO : ScriptableObject
    {
        [Tooltip("Raise to request popping the top view from the navigation stack.")]
        [SerializeField] private GameEventSO _onPopViewRequested;

        [Tooltip("Raise to request popping all views down to the root view.")]
        [SerializeField] private GameEventSO _onPopToRootRequested;

        [Tooltip("Raise to request clearing the entire navigation stack.")]
        [SerializeField] private GameEventSO _onClearStackRequested;

        [Tooltip("Raise to hide all views without removing them from the stack.")]
        [SerializeField] private GameEventSO _onHideAllViewsRequested;

        /// <summary>Event to request popping the top view.</summary>
        public GameEventSO OnPopViewRequested => _onPopViewRequested;

        /// <summary>Event to request popping to the root view.</summary>
        public GameEventSO OnPopToRootRequested => _onPopToRootRequested;

        /// <summary>Event to request clearing all views.</summary>
        public GameEventSO OnClearStackRequested => _onClearStackRequested;

        /// <summary>Event to hide all views without removing them from the stack.</summary>
        public GameEventSO OnHideAllViewsRequested => _onHideAllViewsRequested;

        /// <summary>
        /// Convenience method to request popping the top view.
        /// </summary>
        public void RequestPopView()
        {
            if (_onPopViewRequested != null)
            {
                _onPopViewRequested.Raise();
            }
        }

        /// <summary>
        /// Convenience method to request popping to the root view.
        /// </summary>
        public void RequestPopToRoot()
        {
            if (_onPopToRootRequested != null)
            {
                _onPopToRootRequested.Raise();
            }
        }

        /// <summary>
        /// Convenience method to request clearing the entire stack.
        /// </summary>
        public void RequestClearStack()
        {
            if (_onClearStackRequested != null)
            {
                _onClearStackRequested.Raise();
            }
        }
    }
}
