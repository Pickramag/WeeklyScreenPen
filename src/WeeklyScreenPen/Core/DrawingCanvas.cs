using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;

namespace WeeklyScreenPen.Core;

public sealed class DrawingCanvas
{
    private readonly Canvas _surface;
    private readonly List<Stroke> _strokes = new();
    private readonly Dictionary<Stroke, Path> _paths = new();
    private Stroke? _active;
    private PolyLineSegment? _activeSegment;

    public DrawingCanvas(Canvas surface)
    {
        _surface = surface;
    }

    public event Action? Changed;

    public Tool Tool { get; set; } = Tool.Pen;
    public Color Color { get; set; } = Color.FromRgb(0xE5, 0x48, 0x4D);
    public double Size { get; set; } = 4;

    public int Count => _strokes.Count;

    public double EffectiveWidth => Tool switch
    {
        Tool.Marker => Math.Min(Size * 1.5, 72),
        Tool.Highlighter => Math.Min(Size * 3, 96),
        _ => Math.Max(1, Size)
    };

    public double PreviewWidth => Tool switch
    {
        Tool.Eraser => Math.Min(Size * 2 + 6, 48),
        _ => Math.Max(1, EffectiveWidth)
    };

    public double EraserRadius => Size * 2 + 6;

    private static double OpacityFor(Tool tool) => tool switch
    {
        Tool.Highlighter => 0.32,
        Tool.Marker => 0.92,
        _ => 1.0
    };

    public void Begin(Point p)
    {
        if (Tool == Tool.Eraser)
        {
            EraseAt(p);
            return;
        }

        var stroke = new Stroke
        {
            Tool = Tool,
            Color = Color,
            Width = EffectiveWidth,
            Opacity = OpacityFor(Tool)
        };
        stroke.Points.Add(p);

        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = p, IsClosed = false, IsFilled = false };
        _activeSegment = new PolyLineSegment();
        figure.Segments.Add(_activeSegment);
        geometry.Figures.Add(figure);

        var path = new Path
        {
            Data = geometry,
            Stroke = new SolidColorBrush(Color),
            StrokeThickness = stroke.Width,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Opacity = stroke.Opacity,
            Fill = null,
            IsHitTestVisible = false,
            SnapsToDevicePixels = false
        };

        _surface.Children.Add(path);
        _strokes.Add(stroke);
        _paths[stroke] = path;
        _active = stroke;
        Changed?.Invoke();
    }

    public void Move(Point p)
    {
        if (Tool == Tool.Eraser)
        {
            if (_active != null) return;
            if (_strokes.Count == 0) return;
            EraseAt(p);
            return;
        }

        if (_active == null || _activeSegment == null) return;

        var last = _active.Points[^1];
        double dx = p.X - last.X, dy = p.Y - last.Y;
        if (dx * dx + dy * dy < 2.0) return;

        _active.Points.Add(p);
        _activeSegment.Points.Add(p);
    }

    public void End()
    {
        _active = null;
        _activeSegment = null;
    }

    public void Undo()
    {
        if (_strokes.Count == 0) return;
        var stroke = _strokes[^1];
        _strokes.RemoveAt(_strokes.Count - 1);
        if (ReferenceEquals(stroke, _active)) End();
        RemovePath(stroke);
        Changed?.Invoke();
    }

    public void Clear()
    {
        if (_strokes.Count == 0) return;
        foreach (var stroke in _strokes) RemovePath(stroke);
        _strokes.Clear();
        _paths.Clear();
        End();
        Changed?.Invoke();
    }

    private void RemovePath(Stroke stroke)
    {
        if (_paths.Remove(stroke, out var path))
            _surface.Children.Remove(path);
    }

    private void EraseAt(Point p)
    {
        double radius = EraserRadius;
        bool removed = false;

        for (int i = _strokes.Count - 1; i >= 0; i--)
        {
            var s = _strokes[i];
            if (DistanceToStroke(s, p) <= radius + s.Width / 2)
            {
                bool wasActive = ReferenceEquals(s, _active);
                _strokes.RemoveAt(i);
                RemovePath(s);
                if (wasActive) End();
                removed = true;
            }
        }

        if (removed) Changed?.Invoke();
    }

    private static double DistanceToStroke(Stroke s, Point p)
    {
        var pts = s.Points;
        if (pts.Count == 0) return double.MaxValue;
        if (pts.Count == 1) return (pts[0] - p).Length;

        double min = double.MaxValue;
        for (int i = 1; i < pts.Count; i++)
            min = Math.Min(min, DistanceToSegment(pts[i - 1], pts[i], p));
        return min;
    }

    private static double DistanceToSegment(Point a, Point b, Point p)
    {
        double vx = b.X - a.X, vy = b.Y - a.Y;
        double wx = p.X - a.X, wy = p.Y - a.Y;
        double len2 = vx * vx + vy * vy;
        if (len2 < 1e-9) return (p - a).Length;

        double t = Math.Clamp((wx * vx + wy * vy) / len2, 0, 1);
        double cx = a.X + t * vx, cy = a.Y + t * vy;
        return Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Y - cy) * (p.Y - cy));
    }
}
