using ParkOps.ControlRoom.Ui;
using SadRogue.Primitives;

namespace ParkOps.Story.Rendering;

/// <summary>
/// The consultant's annotation: a paper card with a green edge, and a leader line that draws itself out
/// to the thing being pointed at. Used on every kind of beat, so it reads as "Leading EDJE speaking".
/// </summary>
public static class Callout
{
    private static readonly TimeSpan LineTime = TimeSpan.FromMilliseconds(220);
    private static readonly TimeSpan TextTime = TimeSpan.FromMilliseconds(500);

    /// <summary>A callout with an elbow leader (horizontal, then vertical) from the card to <paramref name="target"/>.</summary>
    public static void Draw(Canvas canvas, Rectangle box, string kicker, string text, Point target, TimeSpan elapsed) =>
        Draw(canvas, box, kicker, text, Elbow(box, target), elapsed);

    /// <summary>
    /// A callout whose leader follows <paramref name="leader"/>: axis-aligned corner points from the card's edge to
    /// the target, which gets the arrowhead. <paramref name="elapsed"/> drives the draw-in.
    /// </summary>
    public static void Draw(Canvas canvas, Rectangle box, string kicker, string text, IReadOnlyList<Point> leader, TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero) return;

        Leader(canvas, leader, Math.Clamp(elapsed / LineTime, 0, 1));
        if (elapsed < LineTime * 0.6) return;

        // Shadow, card, green edge.
        canvas.Fill(new Rectangle(box.X + 1, box.MaxExtentY + 1, box.Width, 1), Palette.Ink, Brand.Black, Cp437.LightShade);
        canvas.Fill(new Rectangle(box.MaxExtentX + 1, box.Y + 1, 1, box.Height), Palette.Ink, Brand.Black, Cp437.LightShade);
        canvas.Fill(box, Brand.Ink, Brand.Paper);
        canvas.Fill(new Rectangle(box.X, box.Y, 1, box.Height), Brand.Green, Brand.Green);

        canvas.Print(box.X + 2, box.Y + 1, string.Join(' ', kicker.ToUpperInvariant().ToCharArray()), Brand.DarkGreen, Brand.Paper);
        var reveal = (int)(Math.Clamp((elapsed - LineTime * 0.6) / TextTime, 0, 1) * text.Length);
        var y = box.Y + 2;
        foreach (var line in ReportPage.WordWrap(text, box.Width - 4))
        {
            if (reveal <= 0 || y > box.MaxExtentY - 1) break;
            canvas.Print(box.X + 2, y++, line[..Math.Min(line.Length, reveal)], Brand.Ink, Brand.Paper);
            reveal -= line.Length + 1;
        }
    }

    /// <summary>Rows a callout needs for <paramref name="text"/> at <paramref name="width"/>.</summary>
    public static int HeightFor(string text, int width) => ReportPage.WordWrap(text, width - 4).Count() + 3;

    /// <summary>Leave the card's nearest edge, go horizontally to the target's column, then vertically to it.</summary>
    public static Point[] Elbow(Rectangle box, Point target)
    {
        Point start;
        if (target.X < box.X) start = new(box.X - 1, Math.Clamp(target.Y, box.Y, box.MaxExtentY));
        else if (target.X > box.MaxExtentX) start = new(box.MaxExtentX + 1, Math.Clamp(target.Y, box.Y, box.MaxExtentY));
        else if (target.Y < box.Y) start = new(target.X, box.Y - 1);
        else start = new(target.X, box.MaxExtentY + 1);
        return start.Y == target.Y || start.X == target.X ? [start, target] : [start, new(target.X, start.Y), target];
    }

    private static void Leader(Canvas canvas, IReadOnlyList<Point> points, double progress)
    {
        List<(Point At, char Glyph)> path = [];
        for (var i = 0; i < points.Count - 1; i++)
        {
            var (from, to) = (points[i], points[i + 1]);
            var step = Direction(from, to);
            var at = from;
            if (i > 0)
            {
                path.Add((from, Corner(Direction(points[i - 1], from), step)));
                at += step;
            }
            for (; at != to; at += step) path.Add((at, step.Y == 0 ? Cp437.SingleHorizontal : Cp437.SingleVertical));
        }

        var last = points.Count >= 2 ? Direction(points[^2], points[^1]) : new Point(1, 0);
        var head = last.Y > 0 ? Glyphs.ArrowDown : last.Y < 0 ? Glyphs.ArrowUp : last.X < 0 ? Glyphs.ArrowLeft : Glyphs.ArrowRight;
        path.Add((points[^1], head));

        var shown = (int)Math.Ceiling(path.Count * progress);
        foreach (var (at, glyph) in path.Take(shown)) canvas.Glyph(at.X, at.Y, glyph, Brand.Green);
    }

    private static Point Direction(Point from, Point to) => new(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

    /// <summary>The box-drawing corner joining a segment travelling <paramref name="incoming"/> to one travelling <paramref name="outgoing"/>.</summary>
    private static char Corner(Point incoming, Point outgoing)
    {
        // A corner glyph connects the side we came from (opposite of incoming) and the side we leave by.
        var left = incoming.X > 0 || outgoing.X < 0;
        var up = incoming.Y > 0 || outgoing.Y < 0;
        return (left, up) switch
        {
            (true, true) => Cp437.BottomRight,   // ┘
            (true, false) => Cp437.TopRight,     // ┐
            (false, true) => Cp437.BottomLeft,   // └
            _ => Cp437.TopLeft,                  // ┌
        };
    }
}
