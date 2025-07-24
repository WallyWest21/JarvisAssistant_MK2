using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.MAUI.Controls;

namespace JarvisAssistant.MAUI.Views;

/// <summary>
/// A page demonstrating the voice mode capabilities with the voice indicator control.
/// </summary>
public partial class VoiceDemoPage : ContentPage
{
    private readonly IVoiceModeManager? _voiceModeManager;
    private readonly IVoiceCommandProcessor? _commandProcessor;
    private readonly IPlatformService? _platformService;
    private VoiceIndicator? _voiceIndicator;
    private Label? _statusLabel;
    private Label? _responseLabel;

    public VoiceDemoPage()
    {
        InitializeComponent();
        
        // Get services from DI
        try
        {
            var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
            _voiceModeManager = services?.GetService(typeof(IVoiceModeManager)) as IVoiceModeManager;
            _commandProcessor = services?.GetService(typeof(IVoiceCommandProcessor)) as IVoiceCommandProcessor;
            _platformService = services?.GetService(typeof(IPlatformService)) as IPlatformService;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting services: {ex}");
        }

        SetupUI();
        SubscribeToEvents();
    }

    private void SetupUI()
    {
        Title = "Voice Mode Demo";
        BackgroundColor = Colors.Black;

        var mainStack = new StackLayout
        {
            Spacing = 20,
            Padding = new Thickness(20),
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
        };

        // Title
        var titleLabel = new Label
        {
            Text = "Jarvis Voice Assistant",
            FontSize = 32,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };

        // Platform info
        var platformLabel = new Label
        {
            Text = $"Platform: {_platformService?.CurrentPlatform ?? Core.Interfaces.PlatformType.Unknown}",
            FontSize = 16,
            TextColor = Colors.LightGray,
            HorizontalOptions = LayoutOptions.Center
        };

        var isTVLabel = new Label
        {
            Text = $"Google TV: {_platformService?.IsGoogleTV() ?? false}",
            FontSize = 16,
            TextColor = Colors.LightGray,
            HorizontalOptions = LayoutOptions.Center
        };

        // Status label
        _statusLabel = new Label
        {
            Text = "Voice mode inactive",
            FontSize = 18,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };

        // Control buttons
        var buttonsStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 15
        };

        var enableButton = new Button
        {
            Text = "Enable Voice",
            BackgroundColor = Colors.Green,
            TextColor = Colors.White,
            FontSize = 16,
            Padding = new Thickness(20, 10)
        };
        enableButton.Clicked += OnEnableVoiceClicked;

        var disableButton = new Button
        {
            Text = "Disable Voice",
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            FontSize = 16,
            Padding = new Thickness(20, 10)
        };
        disableButton.Clicked += OnDisableVoiceClicked;

        var testCommandButton = new Button
        {
            Text = "Test Command",
            BackgroundColor = Colors.Orange,
            TextColor = Colors.White,
            FontSize = 16,
            Padding = new Thickness(20, 10)
        };
        testCommandButton.Clicked += OnTestCommandClicked;

        var backButton = new Button
        {
            Text = "Back to Main",
            BackgroundColor = Colors.DarkBlue,
            TextColor = Colors.White,
            FontSize = 16,
            Padding = new Thickness(20, 10)
        };
        backButton.Clicked += OnBackClicked;

        // Only show toggle buttons for non-TV platforms
        if (_platformService?.IsGoogleTV() != true)
        {
            buttonsStack.Children.Add(enableButton);
            buttonsStack.Children.Add(disableButton);
        }
        buttonsStack.Children.Add(testCommandButton);
        buttonsStack.Children.Add(backButton);

        // Instructions
        var instructionsLabel = new Label
        {
            Text = GetInstructions(),
            FontSize = 14,
            TextColor = Colors.LightGray,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };

        // Response area
        var responseFrame = new Frame
        {
            BackgroundColor = Colors.DarkGray,
            BorderColor = Colors.Gray,
            CornerRadius = 10,
            Padding = new Thickness(15),
            Margin = new Thickness(0, 20, 0, 0)
        };

        _responseLabel = new Label
        {
            Text = "Voice responses will appear here...",
            FontSize = 14,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Fill
        };

        responseFrame.Content = _responseLabel;

        // Add all elements to main stack
        mainStack.Children.Add(titleLabel);
        mainStack.Children.Add(platformLabel);
        mainStack.Children.Add(isTVLabel);
        mainStack.Children.Add(_statusLabel);
        mainStack.Children.Add(buttonsStack);
        mainStack.Children.Add(instructionsLabel);
        mainStack.Children.Add(responseFrame);

        Content = new ScrollView { Content = mainStack };
    }

    private string GetInstructions()
    {
        if (_platformService?.IsGoogleTV() == true)
        {
            return "Voice mode is always active on Google TV.\n" +
                   "Press the voice button on your remote or say \"Hey Jarvis\" to give commands.\n" +
                   "Try: \"What's my status\", \"Generate code\", \"Help\"";
        }
        else
        {
            return "Use the buttons to control voice mode.\n" +
                   "When active, say \"Hey Jarvis\" followed by your command.\n" +
                   "Try: \"What's my status\", \"Generate code\", \"Help\"";
        }
    }

    private void SubscribeToEvents()
    {
        if (_voiceModeManager != null)
        {
            _voiceModeManager.StateChanged += OnVoiceModeStateChanged;
            _voiceModeManager.WakeWordDetected += OnWakeWordDetected;
            _voiceModeManager.VoiceActivityDetected += OnVoiceActivityDetected;
        }

        if (_commandProcessor != null)
        {
            _commandProcessor.CommandReceived += OnCommandReceived;
            _commandProcessor.CommandProcessed += OnCommandProcessed;
        }
    }

    private async void OnEnableVoiceClicked(object? sender, EventArgs e)
    {
        if (_voiceModeManager != null)
        {
            await _voiceModeManager.EnableVoiceModeAsync();
        }
        else
        {
            UpdateResponseLabel("Voice services are not available.");
        }
    }

    private async void OnDisableVoiceClicked(object? sender, EventArgs e)
    {
        if (_voiceModeManager != null)
        {
            await _voiceModeManager.DisableVoiceModeAsync();
        }
        else
        {
            UpdateResponseLabel("Voice services are not available.");
        }
    }

    private async void OnTestCommandClicked(object? sender, EventArgs e)
    {
        if (_commandProcessor != null)
        {
            var testCommands = new[]
            {
                "what's my status",
                "generate code",
                "help",
                "analyze this",
                "search for something"
            };

            var random = new Random();
            var command = testCommands[random.Next(testCommands.Length)];

            try
            {
                var result = await _commandProcessor.ProcessTextCommandAsync(
                    command, 
                    Core.Models.VoiceCommandSource.Manual);

                UpdateResponseLabel($"Test Command: \"{command}\"\nResponse: {result.Response}");
            }
            catch (Exception ex)
            {
                UpdateResponseLabel($"Error processing command: {ex.Message}");
            }
        }
        else
        {
            UpdateResponseLabel("Voice command processor is not available.");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error navigating back: {ex}");
        }
    }

    private void OnVoiceModeStateChanged(object? sender, Core.Interfaces.VoiceModeStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateStatusLabel($"Voice mode: {e.NewState}");
        });
    }

    private void OnWakeWordDetected(object? sender, Core.Interfaces.WakeWordDetectedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateResponseLabel($"Wake word detected: \"{e.WakeWord}\" (confidence: {e.Confidence:P1})");
        });
    }

    private void OnVoiceActivityDetected(object? sender, Core.Interfaces.VoiceActivityDetectedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update any voice activity indicators if needed
        });
    }

    private void OnCommandReceived(object? sender, Core.Interfaces.VoiceCommandReceivedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateResponseLabel($"Command received: \"{e.Command.Text}\" (type: {e.Command.CommandType})");
        });
    }

    private void OnCommandProcessed(object? sender, Core.Interfaces.VoiceCommandProcessedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var status = e.Result.Success ? "?" : "?";
            UpdateResponseLabel($"{status} \"{e.Command.Text}\"\nResponse: {e.Result.Response}\nTime: {e.ProcessingTime.TotalMilliseconds:F0}ms");
        });
    }

    private void UpdateStatusLabel(string text)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = text;
        }
    }

    private void UpdateResponseLabel(string text)
    {
        if (_responseLabel != null)
        {
            _responseLabel.Text = text;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe from events
        if (_voiceModeManager != null)
        {
            _voiceModeManager.StateChanged -= OnVoiceModeStateChanged;
            _voiceModeManager.WakeWordDetected -= OnWakeWordDetected;
            _voiceModeManager.VoiceActivityDetected -= OnVoiceActivityDetected;
        }

        if (_commandProcessor != null)
        {
            _commandProcessor.CommandReceived -= OnCommandReceived;
            _commandProcessor.CommandProcessed -= OnCommandProcessed;
        }
    }
}