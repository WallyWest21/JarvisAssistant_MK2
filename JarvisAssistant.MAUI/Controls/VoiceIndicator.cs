using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.ComponentModel;

namespace JarvisAssistant.MAUI.Controls
{
    /// <summary>
    /// A custom control that displays an animated microphone icon with voice activity visualization.
    /// </summary>
    public class VoiceIndicator : SKCanvasView, INotifyPropertyChanged
    {
        // Bindable Properties for XAML binding
        public static readonly BindableProperty ActivityLevelProperty =
            BindableProperty.Create(nameof(ActivityLevel), typeof(double), typeof(VoiceIndicator), 0.0,
                propertyChanged: OnActivityLevelChanged);

        public static readonly BindableProperty IsActiveProperty =
            BindableProperty.Create(nameof(IsActive), typeof(bool), typeof(VoiceIndicator), false,
                propertyChanged: OnIsActiveChanged);

        public double ActivityLevel
        {
            get => (double)GetValue(ActivityLevelProperty);
            set => SetValue(ActivityLevelProperty, value);
        }

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        private static void OnActivityLevelChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is VoiceIndicator indicator && newValue is double level)
            {
                indicator.AudioLevel = (float)Math.Max(0.0, Math.Min(1.0, level));
            }
        }

        private static void OnIsActiveChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is VoiceIndicator indicator && newValue is bool isActive)
            {
                indicator.State = isActive ? VoiceIndicatorState.Listening : VoiceIndicatorState.Inactive;
            }
        }

        private readonly ILogger<VoiceIndicator>? _logger;
        private readonly Timer _animationTimer;
        
        // Animation properties
        private float _animationProgress = 0f;
        private float _audioLevel = 0f;
        private float _pulseScale = 1f;
        private DateTime _lastUpdateTime = DateTime.Now;
        private bool _isAnimating = false;

        // Visual properties - Afrofuturistic theme
        private VoiceIndicatorState _state = VoiceIndicatorState.Inactive;
        private SKColor _primaryColor = SKColor.Parse("#4A148C"); // Deep purple
        private SKColor _accentColor = SKColor.Parse("#FFD700"); // Gold
        private SKColor _glowColor = SKColor.Parse("#00E5FF"); // Glow blue
        private SKColor _errorColor = SKColor.Parse("#FF5722");
        private SKColor _inactiveColor = SKColor.Parse("#9E9E9E");

        // Platform-specific sizing
        private float _tvMultiplier = 1.5f;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceIndicator"/> class.
        /// </summary>
        public VoiceIndicator()
        {
            try
            {
                var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                _logger = services?.GetService(typeof(ILogger<VoiceIndicator>)) as ILogger<VoiceIndicator>;
            }
            catch
            {
                // Logger not available, continue without logging
            }

            // Setup animation timer
            _animationTimer = new Timer(OnAnimationTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(16)); // ~60 FPS

            // Configure for platform
            ConfigureForPlatform();

            _logger?.LogDebug("VoiceIndicator initialized");
        }

        /// <summary>
        /// Gets or sets the current state of the voice indicator.
        /// </summary>
        public VoiceIndicatorState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                    OnStateChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current audio level for visualization (0.0 to 1.0).
        /// </summary>
        public float AudioLevel
        {
            get => _audioLevel;
            set
            {
                var clampedValue = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(_audioLevel - clampedValue) > 0.01f)
                {
                    _audioLevel = clampedValue;
                    OnPropertyChanged();
                    InvalidateSurface();
                }
            }
        }

        /// <summary>
        /// Gets or sets the primary color for the voice indicator.
        /// </summary>
        public Color PrimaryColor
        {
            get => _primaryColor.ToFormsColor();
            set
            {
                _primaryColor = value.ToSKColor();
                OnPropertyChanged();
                InvalidateSurface();
            }
        }

        /// <summary>
        /// Gets or sets the accent color for animations.
        /// </summary>
        public Color AccentColor
        {
            get => _accentColor.ToFormsColor();
            set
            {
                _accentColor = value.ToSKColor();
                OnPropertyChanged();
                InvalidateSurface();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the indicator should use TV-optimized sizing.
        /// </summary>
        public bool UseTVSizing { get; set; }

        /// <summary>
        /// Event raised when the voice indicator is tapped.
        /// </summary>
        public event EventHandler? Tapped;

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            try
            {
                var canvas = e.Surface.Canvas;
                var info = e.Info;

                canvas.Clear(SKColors.Transparent);

                var centerX = info.Width / 2f;
                var centerY = info.Height / 2f;
                var size = Math.Min(info.Width, info.Height) * 0.8f;

                if (UseTVSizing)
                {
                    size *= _tvMultiplier;
                }

                DrawVoiceIndicator(canvas, centerX, centerY, size);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error painting voice indicator");
            }
        }

        private void DrawVoiceIndicator(SKCanvas canvas, float centerX, float centerY, float size)
        {
            var microphoneSize = size * 0.6f;
            var currentColor = GetCurrentColor();

            // Draw pulse effect for active states
            if (_state == VoiceIndicatorState.Listening || _state == VoiceIndicatorState.Processing)
            {
                DrawPulseEffect(canvas, centerX, centerY, size);
            }

            // Draw audio level visualization
            if (_state == VoiceIndicatorState.Listening && _audioLevel > 0.1f)
            {
                DrawAudioLevelBars(canvas, centerX, centerY, microphoneSize);
            }

            // Draw microphone icon
            DrawMicrophone(canvas, centerX, centerY, microphoneSize, currentColor);

            // Draw state indicator
            DrawStateIndicator(canvas, centerX, centerY, size);
        }

        private void DrawMicrophone(SKCanvas canvas, float centerX, float centerY, float size, SKColor color)
        {
            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            // Microphone body (rounded rectangle)
            var micBodyWidth = size * 0.4f;
            var micBodyHeight = size * 0.6f;
            var micBodyRect = new SKRect(
                centerX - micBodyWidth / 2,
                centerY - micBodyHeight / 2,
                centerX + micBodyWidth / 2,
                centerY + micBodyHeight / 2 - size * 0.1f);

            canvas.DrawRoundRect(micBodyRect, micBodyWidth * 0.3f, micBodyWidth * 0.3f, paint);

            // Microphone stand
            var standHeight = size * 0.15f;
            var standRect = new SKRect(
                centerX - size * 0.02f,
                centerY + micBodyHeight / 2 - size * 0.1f,
                centerX + size * 0.02f,
                centerY + micBodyHeight / 2 - size * 0.1f + standHeight);

            canvas.DrawRect(standRect, paint);

            // Microphone base
            var baseWidth = size * 0.25f;
            var baseRect = new SKRect(
                centerX - baseWidth / 2,
                centerY + micBodyHeight / 2 - size * 0.1f + standHeight,
                centerX + baseWidth / 2,
                centerY + micBodyHeight / 2 - size * 0.1f + standHeight + size * 0.05f);

            canvas.DrawRect(baseRect, paint);

            // Add some detail lines on the microphone
            using var detailPaint = new SKPaint
            {
                Color = color.WithAlpha(128),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            };

            for (int i = 0; i < 3; i++)
            {
                var y = centerY - micBodyHeight * 0.25f + (i * micBodyHeight * 0.2f);
                canvas.DrawLine(
                    centerX - micBodyWidth * 0.3f,
                    y,
                    centerX + micBodyWidth * 0.3f,
                    y,
                    detailPaint);
            }
        }

        private void DrawPulseEffect(SKCanvas canvas, float centerX, float centerY, float size)
        {
            var pulseRadius = size * 0.5f * _pulseScale;
            var alpha = (byte)(255 * (1f - (_pulseScale - 1f) / 0.5f));

            using var pulsePaint = new SKPaint
            {
                Color = _accentColor.WithAlpha(Math.Max((byte)0, alpha)),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f,
                IsAntialias = true
            };

            canvas.DrawCircle(centerX, centerY, pulseRadius, pulsePaint);
        }

        private void DrawAudioLevelBars(SKCanvas canvas, float centerX, float centerY, float micSize)
        {
            var barCount = 5;
            var barSpacing = micSize * 0.3f;
            var startX = centerX - (barCount - 1) * barSpacing / 2;

            using var barPaint = new SKPaint
            {
                Color = _accentColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            for (int i = 0; i < barCount; i++)
            {
                var x = startX + i * barSpacing;
                var barHeight = micSize * 0.1f + (_audioLevel * micSize * 0.4f * (float)Math.Sin(_animationProgress + i));
                var y = centerY + micSize * 0.6f;

                canvas.DrawRect(x - 2f, y, 4f, barHeight, barPaint);
            }
        }

        private void DrawStateIndicator(SKCanvas canvas, float centerX, float centerY, float size)
        {
            if (_state == VoiceIndicatorState.Error)
            {
                // Draw error indicator (exclamation mark)
                using var errorPaint = new SKPaint
                {
                    Color = _errorColor,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                var indicatorSize = size * 0.2f;
                var indicatorX = centerX + size * 0.3f;
                var indicatorY = centerY - size * 0.3f;

                canvas.DrawCircle(indicatorX, indicatorY, indicatorSize, errorPaint);

                using var textPaint = new SKPaint
                {
                    Color = SKColors.White,
                    TextSize = indicatorSize,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                };

                canvas.DrawText("!", indicatorX, indicatorY + textPaint.TextSize * 0.3f, textPaint);
            }
            else if (_state == VoiceIndicatorState.Processing)
            {
                // Draw processing indicator (spinner)
                using var processingPaint = new SKPaint
                {
                    Color = _primaryColor,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 3f,
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round
                };

                var indicatorRadius = size * 0.15f;
                var indicatorX = centerX + size * 0.3f;
                var indicatorY = centerY - size * 0.3f;

                var startAngle = _animationProgress * 360f;
                var sweepAngle = 90f;

                canvas.DrawArc(new SKRect(
                    indicatorX - indicatorRadius,
                    indicatorY - indicatorRadius,
                    indicatorX + indicatorRadius,
                    indicatorY + indicatorRadius),
                    startAngle, sweepAngle, false, processingPaint);
            }
        }

        private SKColor GetCurrentColor()
        {
            return _state switch
            {
                VoiceIndicatorState.Inactive => _inactiveColor,
                VoiceIndicatorState.Listening => _primaryColor,
                VoiceIndicatorState.Processing => _accentColor,
                VoiceIndicatorState.Error => _errorColor,
                _ => _inactiveColor
            };
        }

        private void OnStateChanged()
        {
            _logger?.LogDebug("Voice indicator state changed to: {State}", _state);

            _isAnimating = _state == VoiceIndicatorState.Listening || _state == VoiceIndicatorState.Processing;

            InvalidateSurface();
        }

        private void OnAnimationTick(object? state)
        {
            if (!_isAnimating)
                return;

            try
            {
                var now = DateTime.Now;
                var deltaTime = (float)(now - _lastUpdateTime).TotalSeconds;
                _lastUpdateTime = now;

                // Update animation progress
                _animationProgress += deltaTime * 2f; // 2 seconds per full cycle
                if (_animationProgress > 6.28f) // 2π
                {
                    _animationProgress = 0f;
                }

                // Update pulse scale
                _pulseScale = 1f + 0.5f * (float)Math.Sin(_animationProgress * 3f);

                // Request redraw on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!IsLoaded) return;
                    InvalidateSurface();
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in voice indicator animation tick");
            }
        }

        private void ConfigureForPlatform()
        {
            try
            {
                // Try to get platform service to determine if we're on TV
                var serviceProvider = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                var platformService = serviceProvider?.GetService(typeof(IPlatformService)) as IPlatformService;

                if (platformService?.IsGoogleTV() == true)
                {
                    UseTVSizing = true;
                }
            }
            catch
            {
                // Platform service not available, use default sizing
            }
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            
            if (Handler == null)
            {
                // Control is being unloaded, dispose resources
                _animationTimer?.Dispose();
            }
        }

        protected override void OnTouch(SKTouchEventArgs e)
        {
            if (e.ActionType == SKTouchAction.Released)
            {
                Tapped?.Invoke(this, EventArgs.Empty);
            }

            e.Handled = true;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents the different states of the voice indicator.
    /// </summary>
    public enum VoiceIndicatorState
    {
        /// <summary>
        /// Voice mode is inactive.
        /// </summary>
        Inactive,

        /// <summary>
        /// Voice mode is active and listening.
        /// </summary>
        Listening,

        /// <summary>
        /// Voice mode is processing a command.
        /// </summary>
        Processing,

        /// <summary>
        /// Voice mode encountered an error.
        /// </summary>
        Error
    }
}

// Extension methods for color conversion
internal static class ColorExtensions
{
    public static SKColor ToSKColor(this Color color)
    {
        return new SKColor(
            (byte)(color.Red * 255),
            (byte)(color.Green * 255),
            (byte)(color.Blue * 255),
            (byte)(color.Alpha * 255));
    }

    public static Color ToFormsColor(this SKColor color)
    {
        return Color.FromRgba(color.Red / 255f, color.Green / 255f, color.Blue / 255f, color.Alpha / 255f);
    }
}
