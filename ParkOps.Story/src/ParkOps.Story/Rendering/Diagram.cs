using ParkOps.ControlRoom.Ui;
using SadRogue.Primitives;

namespace ParkOps.Story.Rendering;

/// <summary>Line-drawing helpers for diagrams on a report page: outlined boxes with text, and connecting lines.</summary>
public static class Diagram
{
    /// <summary>A single-line box, filled, with each of <paramref name="lines"/> centred inside it from the second row.</summary>
    public static void Box(Canvas canvas, Rectangle r, Color border, Color fill, params (string Text, Color Color)[] lines)
    {
        canvas.Fill(r, border, fill);
        for (var x = r.X + 1; x < r.MaxExtentX; x++)
        {
            canvas.Glyph(x, r.Y, Cp437.SingleHorizontal, border, fill);
            canvas.Glyph(x, r.MaxExtentY, Cp437.SingleHorizontal, border, fill);
        }
        for (var y = r.Y + 1; y < r.MaxExtentY; y++)
        {
            canvas.Glyph(r.X, y, Cp437.SingleVertical, border, fill);
            canvas.Glyph(r.MaxExtentX, y, Cp437.SingleVertical, border, fill);
        }
        canvas.Glyph(r.X, r.Y, Cp437.TopLeft, border, fill);
        canvas.Glyph(r.MaxExtentX, r.Y, Cp437.TopRight, border, fill);
        canvas.Glyph(r.X, r.MaxExtentY, Cp437.BottomLeft, border, fill);
        canvas.Glyph(r.MaxExtentX, r.MaxExtentY, Cp437.BottomRight, border, fill);
        foreach (var (i, (text, color)) in lines.Index())
        {
            if (r.Y + 1 + i >= r.MaxExtentY) break;
            var shown = text.Length > r.Width - 2 ? text[..(r.Width - 2)] : text;
            canvas.Print(r.X + (r.Width - shown.Length) / 2, r.Y + 1 + i, shown, color, fill);
        }
    }

    /// <summary>A horizontal line from <paramref name="from"/> to <paramref name="to"/> inclusive.</summary>
    public static void HLine(Canvas canvas, int from, int to, int y, Color color, char glyph = Cp437.SingleHorizontal)
    {
        for (var x = Math.Min(from, to); x <= Math.Max(from, to); x++) canvas.Glyph(x, y, glyph, color);
    }

    /// <summary>A vertical line from <paramref name="from"/> to <paramref name="to"/> inclusive.</summary>
    public static void VLine(Canvas canvas, int x, int from, int to, Color color, char glyph = Cp437.SingleVertical)
    {
        for (var y = Math.Min(from, to); y <= Math.Max(from, to); y++) canvas.Glyph(x, y, glyph, color);
    }

    /// <summary>0 before <paramref name="since"/> reaches zero, rising to 1 over <paramref name="ms"/> milliseconds.</summary>
    public static double Fade(TimeSpan since, double ms = 350) => Math.Clamp(since.TotalMilliseconds / ms, 0, 1);
}
