using System.Collections.ObjectModel;
using System.ComponentModel;

namespace JarvisAssistant.SpeechToTextTest;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    private bool _isRecording = false;
    private bool _isAdvancedSettingsVisible = false;
    
    public ObservableCollection<RecognitionResult> RecognitionResults { get; set; }

    public MainPage()
    {
        InitializeComponent();
        RecognitionResults = new ObservableCollection<RecognitionResult>();
        BindingContext = this;
        
        // Set default language selection
        LanguagePicker.SelectedIndex = 0;
        
        // Initialize volume status
        UpdateVolumeStatus(0.6);
    }

    private void OnBufferSizeChanged(object sender, ValueChangedEventArgs e)
    {
        BufferSizeValue.Text = $"{(int)e.NewValue}ms";
    }

    private void OnSensitivityChanged(object sender, ValueChangedEventArgs e)
    {
        SensitivityValue.Text = $"{(int)e.NewValue}";
    }

    private void OnRecordButtonClicked(object sender, EventArgs e)
    {
        _isRecording = !_isRecording;
        
        if (_isRecording)
        {
            RecordButton.Text = "Stop Recording";
            RecordButton.BackgroundColor = Color.FromArgb("#ff6b6b");
            StartRecording();
        }
        else
        {
            RecordButton.Text = "Start Recording";
            RecordButton.BackgroundColor = Color.FromArgb("#00adb5");
            StopRecording();
        }
    }

    private void OnClearButtonClicked(object sender, EventArgs e)
    {
        RecognitionResults.Clear();
    }

    private void OnAdvancedSettingsClicked(object sender, EventArgs e)
    {
        _isAdvancedSettingsVisible = !_isAdvancedSettingsVisible;
        AdvancedSettingsPanel.IsVisible = _isAdvancedSettingsVisible;
        
        AdvancedSettingsButton.Text = _isAdvancedSettingsVisible ? "Hide Advanced Settings" : "Advanced Settings";
    }

    private void StartRecording()
    {
        // Simulate recording with sample data
        Dispatcher.StartTimer(TimeSpan.FromSeconds(3), () =>
        {
            if (_isRecording)
            {
                AddSampleResult();
                return true; // Continue timer
            }
            return false; // Stop timer
        });
        
        // Simulate volume updates
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
        {
            if (_isRecording)
            {
                var random = new Random();
                var volume = random.NextDouble();
                UpdateVolumeLevel(volume);
                return true;
            }
            return false;
        });
    }

    private void StopRecording()
    {
        // Recording stopped - reset volume
        UpdateVolumeLevel(0);
    }

    private void AddSampleResult()
    {
        var sampleTexts = new[]
        {
            "Hello, this is a test of the speech recognition system",
            "The weather is beautiful today",
            "I'm testing the microphone input levels",
            "Speech to text is working correctly",
            "This is another sample recognition result"
        };
        
        var random = new Random();
        var text = sampleTexts[random.Next(sampleTexts.Length)];
        var confidence = random.Next(85, 99);
        var duration = TimeSpan.FromSeconds(random.Next(2, 6));
        
        var result = new RecognitionResult
        {
            Text = text,
            Confidence = $"Confidence: {confidence}%",
            Duration = $"Duration: {duration.TotalSeconds:F1}s"
        };
        
        RecognitionResults.Add(result);
    }

    private void UpdateVolumeLevel(double volume)
    {
        VolumeProgressBar.Progress = volume;
        UpdateVolumeStatus(volume);
    }

    private void UpdateVolumeStatus(double volume)
    {
        if (volume < 0.2)
        {
            VolumeStatusLabel.Text = "Volume too low";
            VolumeStatusLabel.TextColor = Color.FromArgb("#ffd93d");
        }
        else if (volume > 0.8)
        {
            VolumeStatusLabel.Text = "Volume too high";
            VolumeStatusLabel.TextColor = Color.FromArgb("#ff6b6b");
        }
        else
        {
            VolumeStatusLabel.Text = "Good volume level";
            VolumeStatusLabel.TextColor = Color.FromArgb("#00cf9f");
        }
    }
}

public class RecognitionResult
{
    public string Text { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}

