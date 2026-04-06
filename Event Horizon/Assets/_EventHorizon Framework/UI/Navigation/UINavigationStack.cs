using System;
using System.Collections.Generic;

namespace EventHorizon.UI
{
    /// <summary>
    /// Pure C# navigation stack for UI views. Zero Unity dependencies,
    /// fully unit-testable. Manages show/hide transitions when pushing and popping.
    /// </summary>
    public class UINavigationStack
    {
        private readonly Stack<IUIView> _stack = new Stack<IUIView>();

        /// <summary>
        /// Fires after every stack mutation with the new top view (or null if empty).
        /// </summary>
        public event Action<IUIView> OnStackChanged;

        /// <summary>
        /// Fires for each view removed from the stack (pop, popToRoot, clear).
        /// Subscribers can use this to destroy instantiated views.
        /// </summary>
        public event Action<IUIView> OnViewRemoved;

        /// <summary>
        /// The current number of views on the stack.
        /// </summary>
        public int Count => _stack.Count;

        /// <summary>
        /// Pushes a view onto the stack. Hides the current top view and shows the new one.
        /// </summary>
        public void Push(IUIView view)
        {
            if (view == null) return;

            if (_stack.Count > 0)
            {
                _stack.Peek().Hide();
            }

            _stack.Push(view);
            view.Show();
            OnStackChanged?.Invoke(view);
        }

        /// <summary>
        /// Pops the top view off the stack. Hides it and shows the new top view.
        /// </summary>
        public void Pop()
        {
            if (_stack.Count == 0) return;

            IUIView popped = _stack.Pop();
            popped.Hide();
            OnViewRemoved?.Invoke(popped);

            IUIView newTop = _stack.Count > 0 ? _stack.Peek() : null;
            newTop?.Show();
            OnStackChanged?.Invoke(newTop);
        }

        /// <summary>
        /// Pops all views down to the root (first entry). Shows only the root view.
        /// </summary>
        public void PopToRoot()
        {
            if (_stack.Count <= 1) return;

            while (_stack.Count > 1)
            {
                IUIView removed = _stack.Pop();
                removed.Hide();
                OnViewRemoved?.Invoke(removed);
            }

            IUIView root = _stack.Peek();
            root.Show();
            OnStackChanged?.Invoke(root);
        }

        /// <summary>
        /// Removes a specific view from anywhere in the stack.
        /// If it was the top view, the new top is shown. No-op if not found.
        /// </summary>
        public void Remove(IUIView view)
        {
            if (view == null || !_stack.Contains(view)) return;

            bool wasTop = _stack.Count > 0 && _stack.Peek() == view;

            var temp = new Stack<IUIView>();
            while (_stack.Count > 0)
            {
                IUIView item = _stack.Pop();
                if (item == view)
                {
                    item.Hide();
                    OnViewRemoved?.Invoke(item);
                    continue;
                }
                temp.Push(item);
            }

            while (temp.Count > 0)
                _stack.Push(temp.Pop());

            IUIView newTop = _stack.Count > 0 ? _stack.Peek() : null;

            if (wasTop)
                newTop?.Show();

            OnStackChanged?.Invoke(newTop);
        }

        /// <summary>
        /// Hides all views without removing them from the stack.
        /// Call Show() on the top view (or Push a new one) to restore visibility.
        /// </summary>
        public void HideAll()
        {
            foreach (IUIView view in _stack)
            {
                view.Hide();
            }

            OnStackChanged?.Invoke(_stack.Count > 0 ? _stack.Peek() : null);
        }

        /// <summary>
        /// Hides all views and empties the stack completely.
        /// </summary>
        public void Clear()
        {
            while (_stack.Count > 0)
            {
                IUIView removed = _stack.Pop();
                removed.Hide();
                OnViewRemoved?.Invoke(removed);
            }

            OnStackChanged?.Invoke(null);
        }
    }
}
