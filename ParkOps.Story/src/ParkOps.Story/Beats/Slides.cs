using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>Drawing helpers for the dark presentation slides (title, lesson openers, closing).</summary>
public static class Slides
{
    /// <summary>Letter-spaced caps, like the report's kickers, on the dark backdrop.</summary>
    public static void Kicker(Canvas canvas, int x, int y, string text, Color color) =>
        canvas.Print(x, y, string.Join(' ', text.ToUpperInvariant().ToCharArray()), color, Brand.Black);

    /// <summary>A colour that fades up from the backdrop, starting <paramref name="delayMs"/> into the slide.</summary>
    public static Color FadeIn(Color color, TimeSpan elapsed, double delayMs) =>
        Canvas.Lerp(Brand.Black, color, (elapsed.TotalMilliseconds - delayMs) / 350);

    /// <summary>Word-wrapped text that types itself out. Returns the row after the last line.</summary>
    public static int Type(Canvas canvas, int x, int y, int width, string text, Color color, TimeSpan since, double charsPerMs = 0.12)
    {
        var budget = (int)(Math.Max(0, since.TotalMilliseconds) * charsPerMs);
        foreach (var line in ReportPage.WordWrap(text, width))
        {
            if (budget <= 0) return y + 1;
            canvas.Print(x, y, line[..Math.Min(line.Length, budget)], color, Brand.Black);
            budget -= line.Length + 1;
            y++;
        }
        return y;
    }

    /// <summary>The ten lessons as a strip of numbered stops: done ones ticked, the current one lit.</summary>
    public static void Progress(Canvas canvas, int x, int y, int? current)
    {
        foreach (var finding in Findings.All)
        {
            var isCurrent = finding.Lesson == current;
            var done = Findings.Resolved.Contains(finding.Id);
            var label = done && !isCurrent ? $"{Glyphs.Check}{finding.Lesson:00}" : $" {finding.Lesson:00}";
            var (fg, bg) = isCurrent ? (Brand.Black, Brand.Green) : done ? (Brand.Green, Brand.Black) : (Brand.LineNumber, Brand.Black);
            canvas.Print(x, y, $"{label} ", fg, bg);
            if (finding.Lesson < Findings.All.Length) canvas.Fill(new Rectangle(x + 4, y, 6, 1), Brand.DarkGray, Brand.Black, Cp437.SingleHorizontal);
            x += 10;
        }
    }
}
