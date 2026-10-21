using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>The last slide: how many lessons were resolved, each one ticked off, and the consultant's recommendation.</summary>
public sealed class ClosingBeat : Beat
{
    private static readonly TimeSpan RowTime = TimeSpan.FromMilliseconds(90);

    private static readonly string[] Recommendations =
    [
        "On .NET 8 or 9 with a deadline: move to .NET 10 LTS now. Both go out of support on Nov 10, 2026.",
        "On .NET 10 and shipping continuously: adopt .NET 11 after its first servicing release.",
        "Either way: start modelling with unions and closed hierarchies the day you're on C# 15.",
    ];

    public override string Label => "Closing";

    public override int Steps => 2;

    public override string? Hint => "→ index";

    public override Color Backdrop => Brand.Black;

    public override void Draw(Frame frame)
    {
        var canvas = frame.Canvas;
        canvas.Clear(Brand.Black);

        var resolved = Findings.All.Count(f => Findings.Resolved.Contains(f.Id) && !Findings.Partial.Contains(f.Id));
        Slides.Kicker(canvas, 10, 4, "Report to the board", Brand.Green);
        canvas.Big(10, 6, $"{resolved} of {Findings.All.Length} lessons resolved", Brand.TrueWhite);

        var y = 10;
        foreach (var (index, finding) in Findings.All.Index())
        {
            if (frame.Elapsed < RowTime * index) break;
            var partial = Findings.Partial.Contains(finding.Id);
            var done = Findings.Resolved.Contains(finding.Id);
            var (mark, color) = partial ? ("PARTIAL", Brand.Beige) : done ? ($"{Glyphs.Check} DONE", Brand.Green) : ("open", Brand.LineNumber);
            canvas.Print(10, y, $"{finding.Lesson:00}", Brand.LineNumber, Brand.Black);
            canvas.Print(14, y, Workstation.Clip(finding.Feature, 52), Brand.WhiteShade, Brand.Black);
            canvas.Print(68, y, mark, color, Brand.Black);
            y += 2;
        }

        if (frame.Since(1) is var since && since >= TimeSpan.Zero)
        {
            canvas.Fill(new Rectangle(82, 10, 1, 19), Brand.Green, Brand.Green);
            Slides.Kicker(canvas, 85, 10, "Recommendation", Brand.Green);
            var row = 12;
            foreach (var (index, text) in Recommendations.Index())
            {
                row = Slides.Type(canvas, 85, row, 36, text, Brand.WhiteShade, since - TimeSpan.FromMilliseconds(500 * index), 0.2) + 1;
            }
        }

        canvas.Print(10, 38, "Questions?", Brand.Green, Brand.Black);
        canvas.Print(Canvas.Width - 30, 38, "→  index of all lessons", Brand.LineNumber, Brand.Black);
    }
}
