using SkiaSharp.Views.Maui.Controls;
using SkiaSharp.Views.Maui;
using SkiaSharp;

namespace JarvisAssistant.MAUI.Controls
{
    /// <summary>
    /// A control that displays a geometric pattern background for the Jarvis interface.
    /// </summary>
    public class GeometricPatternView : SKCanvasView
    {
        // Bindable Properties
        public static readonly BindableProperty PatternColorProperty =
            BindableProperty.Create(nameof(PatternColor), typeof(Color), typeof(GeometricPatternView), Colors.Purple);

        public static readonly BindableProperty PatternOpacityProperty =
            BindableProperty.Create(nameof(PatternOpacity), typeof(double), typeof(GeometricPatternView), 0.1);

        public Color PatternColor
        {
            get => (Color)GetValue(PatternColorProperty);
            set => SetValue(PatternColorProperty, value);
        }

        public double PatternOpacity
        {
            get => (double)GetValue(PatternOpacityProperty);
            set => SetValue(PatternOpacityProperty, value);
        }

        public GeometricPatternView()
        {
            PaintSurface += OnCanvasViewPaintSurface;
        }

        private void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();

            var info = e.Info;
            var width = info.Width;
            var height = info.Height;

            if (width <= 0 || height <= 0) return;

            // Convert MAUI color to SKColor with opacity
            var skColor = PatternColor.ToSKColor();
            var paintColor = new SKColor(skColor.Red, skColor.Green, skColor.Blue, (byte)(255 * PatternOpacity));

            using var paint = new SKPaint
            {
                Color = paintColor,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };

            // Draw a simple geometric pattern (hexagons/circuits)
            DrawCircuitPattern(canvas, paint, width, height);
        }

        private void DrawCircuitPattern(SKCanvas canvas, SKPaint paint, int width, int height)
        {
            var spacing = 40;
            var radius = 15;

            for (int x = 0; x < width; x += spacing)
            {
                for (int y = 0; y < height; y += spacing)
                {
                    // Draw hexagonal nodes
                    var centerX = x + (y / spacing % 2) * (spacing / 2);
                    var centerY = y;

                    // Draw hexagon
                    var path = new SKPath();
                    for (int i = 0; i < 6; i++)
                    {
                        var angle = i * Math.PI / 3;
                        var pointX = centerX + radius * (float)Math.Cos(angle);
                        var pointY = centerY + radius * (float)Math.Sin(angle);

                        if (i == 0)
                            path.MoveTo(pointX, pointY);
                        else
                            path.LineTo(pointX, pointY);
                    }
                    path.Close();

                    canvas.DrawPath(path, paint);

                    // Draw connecting lines to create circuit effect
                    if (x + spacing < width)
                    {
                        canvas.DrawLine(centerX + radius, centerY, centerX + spacing - radius, centerY, paint);
                    }
                }
            }
        }
    }
}