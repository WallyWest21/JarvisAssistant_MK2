using JarvisAssistant.MAUI.Models;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace JarvisAssistant.MAUI.Controls
{
    public class ChatBubbleControl : ContentView
    {
        public static readonly BindableProperty MessageProperty =
            BindableProperty.Create(nameof(Message), typeof(ChatMessage), typeof(ChatBubbleControl), 
                propertyChanged: OnMessageChanged);

        public static readonly BindableProperty ShowAnimationProperty =
            BindableProperty.Create(nameof(ShowAnimation), typeof(bool), typeof(ChatBubbleControl), true);

        public ChatMessage? Message
        {
            get => (ChatMessage?)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public bool ShowAnimation
        {
            get => (bool)GetValue(ShowAnimationProperty);
            set => SetValue(ShowAnimationProperty, value);
        }

        private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ChatBubbleControl control && newValue is ChatMessage message)
            {
                control.UpdateBubbleContent();
            }
        }

        public ChatBubbleControl()
        {
            UpdateBubbleContent();
        }

        private void UpdateBubbleContent()
        {
            if (Message == null) return;

            var isDesktop = DeviceInfo.Idiom == DeviceIdiom.Desktop;
            var isTv = DeviceInfo.Idiom == DeviceIdiom.TV;
            var maxWidth = GetMaxWidth();

            Content = new Frame
            {
                BackgroundColor = Message.MessageBackgroundColor,
                Padding = GetPadding(),
                Margin = GetMargin(),
                CornerRadius = GetCornerRadius(),
                HorizontalOptions = Message.MessageAlignment,
                MaximumWidthRequest = maxWidth,
                HasShadow = !Message.IsFromUser,
                Content = CreateBubbleContent()
            };

            // Apply entrance animation
            if (ShowAnimation)
            {
                ApplyEntranceAnimation();
            }
        }

        private View CreateBubbleContent()
        {
            var stackLayout = new StackLayout
            {
                Spacing = 4
            };

            // Add message content
            if (Message!.IsCodeBlock)
            {
                stackLayout.Children.Add(CreateCodeBlock());
            }
            else
            {
                stackLayout.Children.Add(CreateTextContent());
            }

            // Add timestamp and status
            stackLayout.Children.Add(CreateMetadataRow());

            return stackLayout;
        }

        private View CreateTextContent()
        {
            var fontSize = GetFontSize();
            var fontFamily = GetFontFamily();

            return new Label
            {
                Text = Message!.Content,
                TextColor = Message.MessageTextColor,
                FontSize = fontSize,
                FontFamily = fontFamily,
                FontAttributes = Message.IsFromJarvis ? FontAttributes.None : FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap
            };
        }

        private View CreateCodeBlock()
        {
            var fontSize = GetFontSize() * 0.9;

            return new Frame
            {
                BackgroundColor = Color.FromArgb("#1A1A1A"),
                Padding = 8,
                CornerRadius = 4,
                Content = new Label
                {
                    Text = Message!.Content,
                    TextColor = Color.FromArgb("#E1BEE7"),
                    FontSize = fontSize,
                    FontFamily = "Courier",
                    LineBreakMode = LineBreakMode.NoWrap
                }
            };
        }

        private View CreateMetadataRow()
        {
            var stackLayout = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = Message!.IsFromUser ? LayoutOptions.End : LayoutOptions.Start,
                Spacing = 8
            };

            // Type indicator for special messages
            if (Message.IsVoiceCommand)
            {
                stackLayout.Children.Add(new Label
                {
                    Text = "🎤",
                    FontSize = 12,
                    VerticalOptions = LayoutOptions.Center
                });
            }
            else if (Message.IsError)
            {
                stackLayout.Children.Add(new Label
                {
                    Text = "⚠",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#FF5722"),
                    VerticalOptions = LayoutOptions.Center
                });
            }

            // Streaming indicator
            if (Message.IsStreaming)
            {
                stackLayout.Children.Add(CreateStreamingIndicator());
            }

            // Timestamp
            stackLayout.Children.Add(new Label
            {
                Text = Message.FormattedTime,
                FontSize = 10,
                TextColor = Color.FromArgb("#9E9E9E"),
                VerticalOptions = LayoutOptions.Center,
                Opacity = 0.7
            });

            return stackLayout;
        }

        private View CreateStreamingIndicator()
        {
            var indicator = new Label
            {
                Text = "●●●",
                FontSize = 12,
                TextColor = Color.FromArgb("#4CAF50"),
                VerticalOptions = LayoutOptions.Center
            };

            // Animate the dots
            var animation = new Animation(v =>
            {
                indicator.Opacity = v;
            }, 0.3, 1.0);

            animation.Commit(indicator, "StreamingAnimation", 16, 800, Easing.SinInOut, repeat: () => true);

            return indicator;
        }

        private double GetMaxWidth()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                return 600;
            else if (DeviceInfo.Idiom == DeviceIdiom.TV)
                return 800;
            else
                return 280;
        }

        private Thickness GetPadding()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                return new Thickness(16, 12);
            else if (DeviceInfo.Idiom == DeviceIdiom.TV)
                return new Thickness(24, 16);
            else
                return new Thickness(12, 8);
        }

        private Thickness GetMargin()
        {
            double baseMargin;
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                baseMargin = 8;
            else if (DeviceInfo.Idiom == DeviceIdiom.TV)
                baseMargin = 12;
            else
                baseMargin = 6;

            return Message!.IsFromUser 
                ? new Thickness(40, baseMargin, baseMargin, baseMargin)
                : new Thickness(baseMargin, baseMargin, 40, baseMargin);
        }

        private int GetCornerRadius()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.TV)
                return 12;
            else
                return 18;
        }

        private double GetFontSize()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                return 14;
            else if (DeviceInfo.Idiom == DeviceIdiom.TV)
                return 20;
            else
                return 16;
        }

        private string GetFontFamily()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.TV)
                return "OpenSans-Semibold";
            else
                return "OpenSans-Regular";
        }

        private async void ApplyEntranceAnimation()
        {
            if (Content == null) return;

            // Start invisible and small
            Content.Opacity = 0;
            Content.Scale = 0.8;
            Content.TranslationY = 20;

            // Animate in
            var tasks = new[]
            {
                Content.FadeTo(1, 300, Easing.SinOut),
                Content.ScaleTo(1, 300, Easing.BounceOut),
                Content.TranslateTo(0, 0, 300, Easing.SinOut)
            };

            await Task.WhenAll(tasks);
        }
    }
}
