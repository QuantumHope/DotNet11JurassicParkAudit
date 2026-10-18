using System.Runtime.InteropServices;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>The report's cover: wordmark, title, engagement facts, and a field of green "edge" stripes.</summary>
public sealed class CoverBeat : Beat
{
    public override string Label => "Cover";

    public override void Draw(Frame frame)
    {
        var canvas = frame.Canvas;
        var page = new ReportPage(canvas);
        canvas.Clear(Brand.Paper);
        canvas.Fill(new Rectangle(0, 0, 4, Canvas.ContentHeight), Brand.Green, Brand.Green);

        Stripes(canvas, frame.Elapsed);

        page.Wordmark(10, 4, onDark: false, big: true);
        page.Kicker(10, 10, "Post-incident technical assessment");
        page.Heading(10, 14, "Isla Nublar ParkOps", Brand.Ink);
        page.Heading(10, 18, "Opening-day readiness", Brand.DarkGray);
        page.Paragraph(10, 22, 64,
            "What .NET 11 and C# 15 would have caught before the park opened: ten incidents, the code behind each one, and the fix.",
            Brand.DarkGray);

        page.Rule(27, 10, 74);
        page.Facts(10, 29,
        [
            ("Prepared for", "InGen Corporation, Isla Nublar operations"),
            ("Prepared by", "Leading EDJE"),
            ("Engagement", "Post-incident review of the ParkOps codebase"),
            ("Date", "October 2026"),
            ("Runtime", RuntimeInformation.FrameworkDescription),
        ]);

        canvas.Print(10, 38, "Press  →  to begin", Brand.Muted, Brand.Paper);
    }

    /// <summary>Diagonal stripes filling a triangle at the right, drawing in from the corner.</summary>
    private static void Stripes(Canvas canvas, TimeSpan elapsed)
    {
        var grow = Math.Clamp(elapsed.TotalMilliseconds / 900, 0, 1);
        const int left = 80;
        for (var y = 0; y < Canvas.ContentHeight; y++)
        {
            var start = left + (int)((Canvas.ContentHeight - y) * 1.1);
            for (var x = Math.Max(start, left); x < Canvas.Width; x++)
            {
                var distance = (Canvas.Width - x) + (Canvas.ContentHeight - y);
                if (distance > grow * 110) continue;
                var band = (x + y) % 9;
                var color = band switch { 0 or 1 => Brand.Green, 2 => Brand.GreenShade, 5 => Brand.Rule, _ => Brand.Paper };
                if (color != Brand.Paper) canvas.Glyph(x, y, ' ', color, color);
            }
        }
    }
}
