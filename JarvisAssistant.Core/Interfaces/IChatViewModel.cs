using System.Collections.ObjectModel;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Interface for the chat view model to enable testing.
    /// Uses object for Messages to avoid MAUI-specific types in Core project.
    /// </summary>
    public interface IChatViewModel
    {
        /// <summary>
        /// Gets the collection of chat messages as objects (to avoid MAUI dependencies).
        /// </summary>
        ObservableCollection<object> Messages { get; }

        /// <summary>
        /// Gets or sets the current message being typed.
        /// </summary>
        string CurrentMessage { get; set; }

        /// <summary>
        /// Gets a value indicating whether the chat is currently processing a message.
        /// </summary>
        bool IsProcessing { get; }

        /// <summary>
        /// Gets a value indicating whether the chat is connected to the LLM service.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the current connection status message.
        /// </summary>
        string ConnectionStatus { get; }

        /// <summary>
        /// Sends a message to the chat.
        /// </summary>
        /// <returns>A task representing the send operation.</returns>
        Task SendMessageAsync();

        /// <summary>
        /// Clears all chat messages.
        /// </summary>
        Task ClearChatAsync();

        /// <summary>
        /// Checks the connection status to the LLM service.
        /// </summary>
        /// <returns>A task representing the connection check.</returns>
        Task CheckConnectionAsync();
    }
}