using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MeetingTranscriber.App.Controls;

public class WaveformControl : Control
{
    public static readonly StyledProperty<float[]> SamplesProperty =
        AvaloniaProperty.Register<WaveformControl, float[]>(nameof(Samples), Array.Empty<float>());

    public float[] Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    private static readonly IBrush BarBrush = new SolidColorBrush(Color.Parse("#00FFC8"));
    private static readonly IBrush BarDimBrush = new SolidColorBrush(Color.FromArgb(100, 0, 255, 200));
    private const int BarCount = 24;

    private readonly float[] _heights = new float[BarCount];

    static WaveformControl()
    {
        SamplesProperty.Changed.AddClassHandler<WaveformControl>((c, _) => c.UpdateHeights());
        AffectsRender<WaveformControl>(SamplesProperty);
    }

    private void UpdateHeights()
    {
        var samples = Samples;
        if (samples.Length == 0)
        {
            var t = DateTime.UtcNow.TimeOfDay.TotalSeconds;
            for (int i = 0; i < BarCount; i++)
                _heights[i] = (float)(0.15 + 0.1 * Math.Sin(t * 2 + i * 0.5));
            return;
        }

        var segLen = Math.Max(1, samples.Length / BarCount);
        for (int i = 0; i < BarCount; i++)
        {
            var seg = samples.Skip(i * segLen).Take(segLen);
            var rms = (float)Math.Sqrt(seg.Average(s => s * s));
            _heights[i] = _heights[i] * 0.6f + Math.Min(rms * 4f, 1f) * 0.4f;
        }
    }

    public override void Render(DrawingContext ctx)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        var totalWidth = Bounds.Width;
        var totalHeight = Bounds.Height;
        var barW = Math.Max(2, (totalWidth / BarCount) - 2);
        var spacing = (totalWidth - barW * BarCount) / Math.Max(1, BarCount - 1);

        for (int i = 0; i < BarCount; i++)
        {
            var h = Math.Max(3, _heights[i] * totalHeight);
            var x = i * (barW + spacing);
            var y = (totalHeight - h) / 2;
            var brush = i % 3 == 0 ? BarDimBrush : BarBrush;
            ctx.DrawRectangle(brush, null, new Rect(x, y, barW, h), 1.5f);
        }
    }
}
