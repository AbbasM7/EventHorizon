using System;
using System.Collections.Generic;
using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.UI
{
    /// <summary>
    /// UI framework module. Owns a registry of view definitions, subscribes to each
    /// definition's ShowEvent, instantiates prefabs at runtime onto a container
    /// provided by UIRoot, and manages stack-based navigation.
    /// External systems push views by raising a parameterless GameEventSO â€” they never
    /// reference definitions, prefabs, or this module directly.
    /// </summary>
    [CreateAssetMenu(menuName = "EventHorizon/UI/UI Module", order = 0)]
    public class UIModuleSO : ModuleBase
    {
        [Tooltip("All view definitions available to this module. Only registered views can be pushed.")]
        [SerializeField] private List<UIViewDefinitionSO> _registeredViews = new List<UIViewDefinitionSO>();

        [Tooltip("Event channel for pop / popToRoot / clear requests from any system.")]
        [SerializeField] private UIEventChannelSO _eventChannel;

        private UINavigationStack _navigationStack;
        private Transform _viewContainer;
        private readonly HashSet<UIViewBase> _instantiatedViews = new HashSet<UIViewBase>();
        private readonly Dictionary<UIViewDefinitionSO, UIViewBase> _activeInstances = new Dictionary<UIViewDefinitionSO, UIViewBase>();
        private readonly List<(GameEventSO Event, Action Handler)> _showEventBindings = new List<(GameEventSO, Action)>();
        private readonly List<(GameEventSO Event, Action Handler)> _hideEventBindings = new List<(GameEventSO, Action)>();

        /// <summary>
        /// The navigation stack used for push/pop view transitions.
        /// </summary>
        public UINavigationStack NavigationStack => _navigationStack;

        /// <summary>
        /// Sets the container transform where instantiated views will be parented.
        /// Called by UIRoot during scene initialization.
        /// </summary>
        public void SetViewContainer(Transform container)
        {
            _viewContainer = container;
        }

        /// <summary>
        /// Pops the top view from the navigation stack. The instance is destroyed automatically.
        /// </summary>
        public void PopView()
        {
            _navigationStack?.Pop();
        }

        /// <summary>
        /// Pops all views down to the root view. Intermediate instances are destroyed.
        /// </summary>
        public void PopToRoot()
        {
            _navigationStack?.PopToRoot();
        }

        /// <summary>
        /// Clears the entire navigation stack. All instances are destroyed.
        /// </summary>
        public void ClearStack()
        {
            _navigationStack?.Clear();
        }

        /// <summary>
        /// Hides all views without removing them from the stack.
        /// </summary>
        public void HideAllViews()
        {
            _navigationStack?.HideAll();
        }

        /// <summary>
        /// Performs module-specific initialization logic.
        /// </summary>
        protected override void OnInitialize()
        {
            _navigationStack = new UINavigationStack();
            _navigationStack.OnViewRemoved += OnViewRemovedFromStack;

            for (int i = 0; i < _registeredViews.Count; i++)
            {
                if (_registeredViews[i] == null)
                {
                    LogWarning($"Null view definition at index {i}.");
                }
            }
        }

        /// <summary>
        /// Performs module-specific activation logic.
        /// </summary>
        protected override void OnActivate()
        {
            SubscribeToShowEvents();
            SubscribeToHideEvents();
            SubscribeToEventChannel();
        }

        /// <summary>
        /// Performs module-specific cleanup logic.
        /// </summary>
        protected override void OnDispose()
        {
            UnsubscribeFromShowEvents();
            UnsubscribeFromHideEvents();
            UnsubscribeFromEventChannel();

            _navigationStack?.Clear();

            if (_navigationStack != null)
            {
                _navigationStack.OnViewRemoved -= OnViewRemovedFromStack;
            }

            _navigationStack = null;
            _viewContainer = null;
            _instantiatedViews.Clear();
            _activeInstances.Clear();
        }

        /// <summary>
        /// Pushes view onto the stack.
        /// </summary>
        private void PushView(UIViewDefinitionSO definition)
        {
            if (_viewContainer == null)
            {
                LogError("No view container assigned. Add a UIRoot to your scene.");
                return;
            }

            if (definition.Prefab == null)
            {
                LogWarning($"View definition '{definition.name}' has no prefab assigned.");
                return;
            }

            UIViewBase instance = Instantiate(definition.Prefab, _viewContainer);
            instance.gameObject.SetActive(false);
            _instantiatedViews.Add(instance);
            _activeInstances[definition] = instance;
            _navigationStack?.Push(instance);
        }

        /// <summary>
        /// Hides view.
        /// </summary>
        private void HideView(UIViewDefinitionSO definition)
        {
            if (!_activeInstances.TryGetValue(definition, out UIViewBase instance)) return;

            _navigationStack?.Remove(instance);
        }

        /// <summary>
        /// Handles view removed from stack.
        /// </summary>
        private void OnViewRemovedFromStack(IUIView view)
        {
            if (!(view is UIViewBase viewBase)) return;

            if (_instantiatedViews.Remove(viewBase))
            {
                Destroy(viewBase.gameObject);
            }

            UIViewDefinitionSO keyToRemove = null;
            foreach (KeyValuePair<UIViewDefinitionSO, UIViewBase> kvp in _activeInstances)
            {
                if (kvp.Value == viewBase)
                {
                    keyToRemove = kvp.Key;
                    break;
                }
            }

            if (keyToRemove != null)
                _activeInstances.Remove(keyToRemove);
        }

        /// <summary>
        /// Subscribes to to show events.
        /// </summary>
        private void SubscribeToShowEvents()
        {
            for (int i = 0; i < _registeredViews.Count; i++)
            {
                UIViewDefinitionSO definition = _registeredViews[i];
                if (definition == null || definition.ShowEvent == null) continue;

                UIViewDefinitionSO captured = definition;
                Action handler = () => PushView(captured);
                _showEventBindings.Add((definition.ShowEvent, handler));
                definition.ShowEvent.RegisterListener(handler);
            }
        }

        /// <summary>
        /// Unsubscribes from from show events.
        /// </summary>
        private void UnsubscribeFromShowEvents()
        {
            for (int i = 0; i < _showEventBindings.Count; i++)
            {
                _showEventBindings[i].Event.UnregisterListener(_showEventBindings[i].Handler);
            }

            _showEventBindings.Clear();
        }

        /// <summary>
        /// Subscribes to to hide events.
        /// </summary>
        private void SubscribeToHideEvents()
        {
            for (int i = 0; i < _registeredViews.Count; i++)
            {
                UIViewDefinitionSO definition = _registeredViews[i];
                if (definition == null || definition.HideEvent == null) continue;

                UIViewDefinitionSO captured = definition;
                Action handler = () => HideView(captured);
                _hideEventBindings.Add((definition.HideEvent, handler));
                definition.HideEvent.RegisterListener(handler);
            }
        }

        /// <summary>
        /// Unsubscribes from from hide events.
        /// </summary>
        private void UnsubscribeFromHideEvents()
        {
            for (int i = 0; i < _hideEventBindings.Count; i++)
            {
                _hideEventBindings[i].Event.UnregisterListener(_hideEventBindings[i].Handler);
            }

            _hideEventBindings.Clear();
        }

        /// <summary>
        /// Subscribes to to event channel.
        /// </summary>
        private void SubscribeToEventChannel()
        {
            if (_eventChannel == null) return;

            if (_eventChannel.OnPopViewRequested != null)
                _eventChannel.OnPopViewRequested.RegisterListener(OnPopViewRequested);

            if (_eventChannel.OnPopToRootRequested != null)
                _eventChannel.OnPopToRootRequested.RegisterListener(OnPopToRootRequested);

            if (_eventChannel.OnClearStackRequested != null)
                _eventChannel.OnClearStackRequested.RegisterListener(OnClearStackRequested);

            if (_eventChannel.OnHideAllViewsRequested != null)
                _eventChannel.OnHideAllViewsRequested.RegisterListener(HideAllViews);
        }

        /// <summary>
        /// Unsubscribes from from event channel.
        /// </summary>
        private void UnsubscribeFromEventChannel()
        {
            if (_eventChannel == null) return;

            if (_eventChannel.OnPopViewRequested != null)
                _eventChannel.OnPopViewRequested.UnregisterListener(OnPopViewRequested);

            if (_eventChannel.OnPopToRootRequested != null)
                _eventChannel.OnPopToRootRequested.UnregisterListener(OnPopToRootRequested);

            if (_eventChannel.OnClearStackRequested != null)
                _eventChannel.OnClearStackRequested.UnregisterListener(OnClearStackRequested);

            if (_eventChannel.OnHideAllViewsRequested != null)
                _eventChannel.OnHideAllViewsRequested.UnregisterListener(HideAllViews);
        }

        /// <summary>
        /// Handles the pop view request.
        /// </summary>
        private void OnPopViewRequested()
        {
            PopView();
        }

        /// <summary>
        /// Handles the pop to root request.
        /// </summary>
        private void OnPopToRootRequested()
        {
            PopToRoot();
        }

        /// <summary>
        /// Handles the clear stack request.
        /// </summary>
        private void OnClearStackRequested()
        {
            ClearStack();
        }
    }
}
