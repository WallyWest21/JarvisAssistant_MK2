using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Core chat message model without UI dependencies
    /// </summary>
    public class ChatMessageCore : INotifyPropertyChanged
    {
        private string _content = string.Empty;
        private bool _isFromUser;
        private DateTime _timestamp;
        private MessageType _type;
        private bool _isStreaming;
        private string _id = Guid.NewGuid().ToString();
        private Dictionary<string, object>? _metadata;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged();
            }
        }

        public bool IsFromUser
        {
            get => _isFromUser;
            set
            {
                _isFromUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFromJarvis));
            }
        }

        public bool IsFromJarvis => !IsFromUser;

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTime));
            }
        }

        public MessageType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCodeBlock));
                OnPropertyChanged(nameof(IsError));
                OnPropertyChanged(nameof(IsVoiceCommand));
            }
        }

        public bool IsStreaming
        {
            get => _isStreaming;
            set
            {
                _isStreaming = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets metadata associated with the message for enhanced functionality.
        /// </summary>
        public Dictionary<string, object>? Metadata
        {
            get => _metadata;
            set
            {
                _metadata = value;
                OnPropertyChanged();
            }
        }

        // Business logic properties (without UI dependencies)
        public string FormattedTime => Timestamp.ToString("HH:mm");
        public bool IsCodeBlock => Type == MessageType.Code;
        public bool IsError => Type == MessageType.Error;
        public bool IsVoiceCommand => Type == MessageType.Voice;

        public ChatMessageCore()
        {
            Timestamp = DateTime.Now;
        }

        public ChatMessageCore(string content, bool isFromUser, MessageType type = MessageType.Text)
        {
            Content = content;
            IsFromUser = isFromUser;
            Type = type;
            Timestamp = DateTime.Now;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}