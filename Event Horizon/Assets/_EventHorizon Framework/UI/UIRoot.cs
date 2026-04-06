using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.UI
{
    /// <summary>
    /// Scene-side component that registers a container transform with UIModuleSO.
    /// Place on a Canvas (or a child panel) where instantiated views should appear.
    /// Contains zero game logic â€” pure registration relay.
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        [Tooltip("The UI Module asset that will receive this transform as its view container.")] [SerializeField]
        private UIModuleSO _uiModule;

        [SerializeField] private Transform _uiRoot;

        /// <summary>
        /// Initializes references when the component awakens.
        /// </summary>
        private void Awake()
        {
            if (_uiModule != null)
            {
                _uiModule.SetViewContainer(_uiRoot);
            }
            else
            {
                SingularityConsole.LogError<UIRoot>("No UI Module assigned.", this);
            }
        }
    }
}
