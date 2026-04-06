using System;
using System.Collections.Generic;
using UnityEngine;

namespace EventHorizon.Core
{
    /// <summary>
    /// A ScriptableObject that maintains a runtime list of active objects.
    /// Items register on enable and unregister on disable, providing a
    /// singleton-free "get all active X" pattern. Resets on play-mode entry.
    /// </summary>
    public abstract class RuntimeSetSO<T> : ScriptableObject
    {
        private readonly List<T> _items = new List<T>();

        /// <summary>
        /// Fires when an item is added to the set.
        /// </summary>
        public event Action<T> OnItemAdded;

        /// <summary>
        /// Fires when an item is removed from the set.
        /// </summary>
        public event Action<T> OnItemRemoved;

        /// <summary>
        /// Read-only access to the current set contents.
        /// </summary>
        public IReadOnlyList<T> Items => _items;

        /// <summary>
        /// The current number of items in the set.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Adds an item to the set if not already present.
        /// </summary>
        public void Add(T item)
        {
            if (item == null || _items.Contains(item)) return;

            _items.Add(item);
            OnItemAdded?.Invoke(item);
        }

        /// <summary>
        /// Removes an item from the set.
        /// </summary>
        public void Remove(T item)
        {
            if (item == null) return;

            if (_items.Remove(item))
            {
                OnItemRemoved?.Invoke(item);
            }
        }

        /// <summary>
        /// Returns true if the item is currently in the set.
        /// </summary>
        public bool Contains(T item)
        {
            return item != null && _items.Contains(item);
        }

        /// <summary>
        /// Iterates all items and invokes an action. Avoids allocation from foreach on List.
        /// </summary>
        public void ForEach(Action<T> action)
        {
            if (action == null) return;

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                action.Invoke(_items[i]);
            }
        }

        /// <summary>
        /// Returns the first item matching the predicate, or default.
        /// </summary>
        public T Find(Predicate<T> predicate)
        {
            if (predicate == null) return default;

            for (int i = 0; i < _items.Count; i++)
            {
                if (predicate(_items[i]))
                {
                    return _items[i];
                }
            }

            return default;
        }

        /// <summary>
        /// Binds state when the component is enabled.
        /// </summary>
        private void OnEnable()
        {
            _items.Clear();
        }

        /// <summary>
        /// Releases bindings when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            _items.Clear();
        }
    }
}
