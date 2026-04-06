using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.Example
{
    /// <summary>
    /// Example controller that shows the settings panel on Start.
    /// Demonstrates fully decoupled navigation — only raises a parameterless
    /// GameEventSO. Zero knowledge of views, prefabs, definitions, or UIModuleSO.
    /// </summary>
    public class MockController : MonoBehaviour
    {
        [Tooltip("Raise to show the settings panel. Wired to a UIViewDefinitionSO's ShowEvent.")]
        [SerializeField] private GameEventSO _showSettingsEvent;

        private void Start()
        {
            if (_showSettingsEvent != null)
            {
                _showSettingsEvent.Raise();
            }
        }
    }
}
