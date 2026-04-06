namespace EventHorizon.UI
{
    /// <summary>
    /// Contract for views that bind to ScriptableObject data sources.
    /// Only implemented by views that need reactive data binding.
    /// </summary>
    public interface IDataBindable
    {
        /// <summary>
        /// Binds all data sources, subscribing to change notifications.
        /// </summary>
        void Bind();

        /// <summary>
        /// Unbinds all data sources, removing all subscriptions.
        /// </summary>
        void Unbind();
    }
}
