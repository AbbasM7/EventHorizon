namespace EventHorizon.UI
{
    /// <summary>
    /// Contract for UI views managed by the navigation stack.
    /// </summary>
    public interface IUIView
    {
        /// <summary>
        /// Shows the view, making it visible to the player.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the view, removing it from the player's sight.
        /// </summary>
        void Hide();

        /// <summary>
        /// Whether the view is currently visible.
        /// </summary>
        bool IsVisible { get; }
    }
}
