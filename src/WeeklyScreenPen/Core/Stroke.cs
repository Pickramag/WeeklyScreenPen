using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace WeeklyScreenPen.Core;

public sealed class Stroke
{
    public Tool Tool { get; init; } = Tool.Pen;
    public Color Color { get; init; } = Colors.Black;
    public double Width { get; init; } = 4;
    public double Opacity { get; init; } = 1.0;
    public List<Point> Points { get; } = new();
}
