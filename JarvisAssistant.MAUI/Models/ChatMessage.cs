using System.ComponentModel;
using System.Runtime.CompilerServices;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.MAUI.Models
{
    /// <summary>
    /// Represents a chat message in the UI (extends core model with UI-specific properties)
    /// </summary>
    public class ChatMessage : ChatMessageCore
    {
        // UI Helper Properties (MAUI-specific)
        public LayoutOptions MessageAlignment => IsFromUser ? LayoutOptions.End : LayoutOptions.Start;
        
        public Color MessageBackgroundColor => IsFromUser 
            ? Color.FromArgb("#FFD700") // Gold for user messages
            : Color.FromArgb("#4A148C"); // Deep purple for Jarvis
            
        public Color MessageTextColor => IsFromUser 
            ? Color.FromArgb("#1A1A1A") // Dark text for user messages
            : Color.FromArgb("#E1BEE7"); // Light purple text for Jarvis

        public ChatMessage() : base()
        {
        }

        public ChatMessage(string content, bool isFromUser, MessageType type = MessageType.Text) 
            : base(content, isFromUser, type)
        {
        }
    }
}
