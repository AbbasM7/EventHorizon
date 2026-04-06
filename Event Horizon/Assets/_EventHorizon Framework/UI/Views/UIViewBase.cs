using System;
using System.Collections.Generic;
using EventHorizon.Core;
using UnityEngine;

namespace EventHorizon.UI
{
    /// <summary>
    /// Abstract base for all UI views. Provides visibility management,
    /// reactive data binding to VariableSO assets, and automatic cleanup.
    /// Views never call Show/Hide on themselves â€” the UIModuleSO drives visibility.
    /// </summary>
    public abstract class UIViewBase : MonoBehaviour, IUIView, IDataBindable
    {
        private readonly List<Action> _unbindActions = new List<Action>();

        /// <summary>
        /// Whether this view is currently visible.
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Shows the view and invokes the OnShow hook.
        /// </summary>
        public void Show()
        {
            IsVisible = true;
            gameObject.SetActive(true);
            OnShow();
        }

        /// <summary>
        /// Hides the view and invokes the OnHide hook.
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
            OnHide();
        }

        /// <summary>
        /// Binds all data sources. Called automatically in OnEnable.
        /// Subclasses should call BindVariable inside their override.
        /// </summary>
        public virtual void Bind()
        {
        }

        /// <summary>
        /// Unbinds all data sources and clears subscriptions. Called automatically in OnDisable.
        /// </summary>
        public virtual void Unbind()
        {
            for (int i = 0; i < _unbindActions.Count; i++)
            {
                _unbindActions[i]?.Invoke();
            }

            _unbindActions.Clear();
        }

        /// <summary>
        /// Override for custom show transition logic.
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// Override for custom hide transition logic.
        /// </summary>
        protected virtual void OnHide()
        {
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Binds a VariableSO to a callback. Immediately fires with the current value
        /// and subscribes for future changes. Automatically cleaned up on Unbind.
        /// </summary>
        protected void BindVariable<T>(VariableSO<T> variable, Action<T> onChanged)
        {
            if (variable == null || onChanged == null) return;

            onChanged.Invoke(variable.RuntimeValue);

            if (variable.OnValueChanged != null)
            {
                variable.OnValueChanged.RegisterListener(onChanged);
                _unbindActions.Add(() => variable.OnValueChanged.UnregisterListener(onChanged));
            }
        }

        /// <summary>
        /// Binds state when the component is enabled.
        /// </summary>
        private void OnEnable()
        {
            Bind();
        }

        /// <summary>
        /// Releases bindings when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            Unbind();
        }
    }
}
