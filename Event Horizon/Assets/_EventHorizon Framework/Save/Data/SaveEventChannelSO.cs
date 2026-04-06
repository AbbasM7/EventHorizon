using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Save
{
    /// <summary>
    /// Event channels for save system operations. Game systems can request
    /// saves/loads through these events without referencing SaveModuleSO directly.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/Save/Save Event Channel", order = 3)]
    public class SaveEventChannelSO : ScriptableObject
    {
        [Tooltip("Raise to request saving all registered saveables.")]
        [SerializeField] private GameEventSO _onSaveRequested;

        [Tooltip("Raise to request loading and restoring all registered saveables.")]
        [SerializeField] private GameEventSO _onLoadRequested;

        [Tooltip("Raise to request deleting all save data.")]
        [SerializeField] private GameEventSO _onDeleteRequested;

        [Tooltip("Raised after a save completes successfully.")]
        [SerializeField] private GameEventSO _onSaveCompleted;

        [Tooltip("Raised after a load completes successfully.")]
        [SerializeField] private GameEventSO _onLoadCompleted;

        /// <summary>Event to request a save operation.</summary>
        public GameEventSO OnSaveRequested => _onSaveRequested;

        /// <summary>Event to request a load operation.</summary>
        public GameEventSO OnLoadRequested => _onLoadRequested;

        /// <summary>Event to request deletion of all save data.</summary>
        public GameEventSO OnDeleteRequested => _onDeleteRequested;

        /// <summary>Raised after a successful save.</summary>
        public GameEventSO OnSaveCompleted => _onSaveCompleted;

        /// <summary>Raised after a successful load.</summary>
        public GameEventSO OnLoadCompleted => _onLoadCompleted;
    }
}
