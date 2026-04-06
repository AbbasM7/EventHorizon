using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.UI
{
    /// <summary>
    /// Defines a UI view by binding an identity to its prefab and a trigger event.
    /// When the ShowEvent is raised, UIModuleSO instantiates the prefab and pushes it.
    /// External systems only need the GameEventSO — zero knowledge of views or prefabs.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/UI/View Definition", order = 3)]
    public class UIViewDefinitionSO : ScriptableObject
    {
        [Tooltip("The view prefab that will be instantiated when this view is pushed.")]
        [SerializeField] private UIViewBase _prefab;

        [Tooltip("Raise this event from any system to push this view onto the navigation stack.")]
        [SerializeField] private GameEventSO _showEvent;

        [Tooltip("Raise this event from any system to hide and remove this specific view from the stack.")]
        [SerializeField] private GameEventSO _hideEvent;

        /// <summary>The view prefab bound to this definition.</summary>
        public UIViewBase Prefab => _prefab;

        /// <summary>The event that triggers pushing this view.</summary>
        public GameEventSO ShowEvent => _showEvent;

        /// <summary>The event that triggers hiding and removing this specific view.</summary>
        public GameEventSO HideEvent => _hideEvent;
    }
}
