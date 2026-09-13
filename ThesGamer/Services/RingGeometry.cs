using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ThesGamer.Services;

public static class RingGeometry
{
    public static void SetPercent(Path target, double percent, double size = 40, double thickness = 3.2)
    {
        percent = Math.Clamp(percent, 0, 99.9);
        var radius = (size - thickness) / 2.0;
        var center = size / 2.0;
        var angle = percent / 100.0 * 359.0;
        if (angle <= 0.1)
        {
            target.Data = Geometry.Empty;
            return;
        }

        var start = new Point(center, center - radius);
        var rad = (Math.PI / 180.0) * angle;
        var end = new Point(
            center + radius * Math.Sin(rad),
            center - radius * Math.Cos(rad));

        var fig = new PathFigure { StartPoint = start, IsClosed = false };
        fig.Segments.Add(new ArcSegment
        {
            Point = end,
            Size = new Size(radius, radius),
            IsLargeArc = angle > 180,
            SweepDirection = SweepDirection.Clockwise,
            RotationAngle = 0
        });

        target.Data = new PathGeometry(new[] { fig });
        target.StrokeThickness = thickness;
        target.StrokeStartLineCap = PenLineCap.Round;
        target.StrokeEndLineCap = PenLineCap.Round;
    }
}
