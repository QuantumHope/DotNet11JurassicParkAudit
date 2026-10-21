using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc002;

/// <summary>
/// The concept behind lesson 2, drawn rather than coded: two overlapping lists (the roster and the motion grid), what an
/// inner Join keeps (only the overlap) and what FullJoin keeps (every region, the missing side labelled), then the
/// park's real numbers. A small sample on the right shows the actual rows each join returns.
/// </summary>
public sealed class JoinConceptBeat(Finding finding, int rostered, int tracked, int matched, Page page) : Beat
{
    // The Venn diagram: two overlapping boxes. Left only, both, right only.
    private static readonly Rectangle Left = new(10, 9, 28, 9);
    private static readonly Rectangle RightSet = new(26, 9, 28, 9);
    private static readonly Rectangle LeftOnly = new(Left.X + 1, Left.Y + 1, RightSet.X - Left.X - 1, Left.Height - 2);
    private static readonly Rectangle Both = new(RightSet.X, Left.Y + 1, Left.MaxExtentX - RightSet.X + 1, Left.Height - 2);
    private static readonly Rectangle RightOnly = new(Left.MaxExtentX + 1, Left.Y + 1, RightSet.MaxExtentX - Left.MaxExtentX - 1, Left.Height - 2);

    private const int SampleX = 66;

    // Illustrative tags in the park's own format: four animals on both lists, one rostered with no signal, one signal
    // with no roster entry.
    private static readonly string[] Roster = ["V-07", "T-01", "B-03", "G-12", "D-05"];
    private static readonly string[] Grid = ["V-07", "T-01", "B-03", "G-12", "V-J01"];

    private static readonly (string Title, string Text)[] Story =
    [
        ("Two lists, one key",
            "The census compares the roster with what the motion grid can see, matching the two on each animal's tag."),
        ("Join keeps only the overlap",
            "An inner join returns a row only where both sides match. Anything on just one side is dropped without a " +
            "word, so the result can never be bigger than the roster."),
        ("FullJoin keeps both sides",
            "A full join also returns the rows only one side has. The missing side comes back null, so a lost animal and " +
            "an unknown signal both become rows you can report."),
        ("In the park",
            "The roster and the grid agree on every rostered animal. Join counts those and stops. FullJoin finds the " +
            "signals nobody rostered."),
    ];

    public override string Label => "Concept";

    public override int Steps => Story.Length;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(finding.LessonLabel, page);
        report.Kicker(ReportPage.Left, ReportPage.KickerRow, $"{finding.LessonLabel}  ·  The concept  ·  Join vs FullJoin");
        report.Heading(ReportPage.Left, ReportPage.HeadingRow, "Which rows does a join keep?");

        var step = frame.Step;
        var fade = Math.Clamp(frame.Since(step).TotalMilliseconds / 350, 0, 1);
        DrawVenn(canvas, step, fade);
        DrawSample(report, canvas, step, frame.Since(step));

        // The narrative for this step, under the diagram.
        var (title, text) = Story[step];
        canvas.Print(Left.X, 20, $" {step + 1} ", Brand.TrueWhite, Brand.DarkGreen);
        canvas.Print(Left.X + 4, 20, title, Brand.Ink, Brand.Paper);
        report.Paragraph(Left.X, 22, 50, text, Brand.DarkGray, (int)(Math.Max(0, frame.Since(step).TotalMilliseconds - 200) * 0.15));

        if (step == 3) DrawParkNumbers(report, canvas, frame.Since(3));
    }

    private static void DrawVenn(Canvas canvas, int step, double fade)
    {
        canvas.Print(Left.X, Left.Y - 1, "ROSTER  (expected)", Brand.Ink, Brand.Paper);
        var gridLabel = "MOTION GRID  (tracked)";
        canvas.Print(RightSet.MaxExtentX - gridLabel.Length + 1, RightSet.Y - 1, gridLabel, Brand.Ink, Brand.Paper);

        // Which regions the current step keeps: Join only the overlap, FullJoin all three.
        (bool Left, bool Both, bool Right) kept = step switch { 0 => (false, false, false), 1 => (false, true, false), _ => (true, true, true) };
        Region(canvas, LeftOnly, "roster", "only", kept.Left, Brand.Beige, step, fade);
        Region(canvas, Both, "on", "both", kept.Both, Brand.GreenShade, step, fade);
        Region(canvas, RightOnly, "grid", "only", kept.Right, Brand.Red, step, fade);

        Outline(canvas, Left, Brand.DarkGray);
        Outline(canvas, RightSet, Brand.DarkGray);

        var caption = step switch
        {
            0 => "",
            1 => "Join: the overlap only",
            _ => "FullJoin: all three regions",
        };
        canvas.Print(Left.X, Left.MaxExtentY + 1, caption, step == 1 ? Brand.DarkGreen : Brand.Red, Brand.Paper);
    }

    /// <summary>One region of the diagram: filled in its colour once a step keeps it, greyed and marked "dropped" once it doesn't.</summary>
    private static void Region(Canvas canvas, Rectangle area, string top, string bottom, bool kept, Color color, int step, double fade)
    {
        var dropped = step > 0 && !kept;
        var fill = kept ? Canvas.Lerp(Brand.WhiteShade, color, fade) : dropped ? Brand.Rule : Brand.WhiteShade;
        var ink = kept ? Brand.TrueWhite : dropped ? Brand.Muted : Brand.Ink;
        canvas.Fill(area, ink, fill);
        var mid = area.Y + area.Height / 2 - 1;
        canvas.Print(area.X + (area.Width - top.Length) / 2, mid, top, ink, fill);
        canvas.Print(area.X + (area.Width - bottom.Length) / 2, mid + 1, bottom, ink, fill);
        if (dropped) canvas.Print(area.X + (area.Width - 7) / 2, mid + 3, "dropped", Brand.Red, fill);
    }

    private static void Outline(Canvas canvas, Rectangle r, Color color)
    {
        for (var x = r.X + 1; x < r.MaxExtentX; x++)
        {
            canvas.Glyph(x, r.Y, Cp437.SingleHorizontal, color);
            canvas.Glyph(x, r.MaxExtentY, Cp437.SingleHorizontal, color);
        }
        for (var y = r.Y + 1; y < r.MaxExtentY; y++)
        {
            canvas.Glyph(r.X, y, Cp437.SingleVertical, color);
            canvas.Glyph(r.MaxExtentX, y, Cp437.SingleVertical, color);
        }
        canvas.Glyph(r.X, r.Y, Cp437.TopLeft, color);
        canvas.Glyph(r.MaxExtentX, r.Y, Cp437.TopRight, color);
        canvas.Glyph(r.X, r.MaxExtentY, Cp437.BottomLeft, color);
        canvas.Glyph(r.MaxExtentX, r.MaxExtentY, Cp437.BottomRight, color);
    }

    /// <summary>The two sample lists with their matches drawn across, then the rows each join returns.</summary>
    private static void DrawSample(ReportPage report, Canvas canvas, int step, TimeSpan since)
    {
        canvas.Fill(new Rectangle(SampleX - 2, 8, 1, 25), Brand.Green, Brand.Green);
        report.Kicker(SampleX, 8, "A sample, row by row");

        canvas.Print(SampleX, 10, "ROSTER", Brand.Muted, Brand.Paper);
        canvas.Print(SampleX + 34, 10, "MOTION GRID", Brand.Muted, Brand.Paper);
        foreach (var (i, (left, right)) in Roster.Zip(Grid).Index())
        {
            var y = 11 + i;
            var match = left == right;
            canvas.Print(SampleX, y, left, match ? Brand.Ink : Brand.DarkGray, Brand.Paper);
            canvas.Print(SampleX + 34, y, right, match ? Brand.Ink : Brand.DarkGray, Brand.Paper);
            if (match)
            {
                for (var x = SampleX + 6; x < SampleX + 32; x++) canvas.Glyph(x, y, Cp437.SingleHorizontal, Brand.GreenShade, Brand.Paper);
                canvas.Glyph(SampleX + 32, y, Glyphs.ArrowRight, Brand.GreenShade, Brand.Paper);
            }
            else canvas.Print(SampleX + 14, y, "no match", Brand.Muted, Brand.Paper);
        }

        if (step == 0)
        {
            report.Paragraph(SampleX, 18, ReportPage.Right - SampleX, "Match each tag on the roster with a signal on the grid.", Brand.DarkGray);
            return;
        }

        // The join's actual output for the sample.
        var full = step >= 2;
        canvas.Print(SampleX, 18, full ? "expected.FullJoin(tracked, ...)" : "expected.Join(tracked, ...)", Brand.Blue, Brand.Paper);
        canvas.Print(SampleX, 19, "EXPECTED".PadRight(22) + "TRACKED", Brand.Muted, Brand.Paper);

        List<(string Left, string Right, Color Color)> rows = [.. Roster.Zip(Grid).Where(p => p.First == p.Second).Select(p => (p.First, p.Second, Brand.DarkGreen))];
        if (full)
        {
            rows.Add(("D-05", "— NO SIGNAL —", Brand.Beige));
            rows.Add(("— NOT ON ROSTER —", "V-J01", Brand.Red));
        }

        var shown = (int)Math.Clamp(since.TotalMilliseconds / 150, 0, rows.Count);
        foreach (var (i, (left, right, color)) in rows.Take(shown).Index())
        {
            var ink = color == Brand.Beige ? Brand.Ink : Brand.TrueWhite;
            var background = color == Brand.DarkGreen ? Brand.Paper : color;
            canvas.Print(SampleX, 20 + i, $"{left,-22}{right}".PadRight(42), color == Brand.DarkGreen ? Brand.DarkGreen : ink, background);
        }
        if (shown == rows.Count)
            canvas.Print(SampleX, 21 + rows.Count, $"{rows.Count} rows{(full ? ": unmatched rows kept, missing side null" : ": unmatched rows dropped")}",
                Brand.DarkGray, Brand.Paper);
        canvas.Print(SampleX, ReportPage.Bottom, "Sample tags, for illustration.", Brand.Muted, Brand.Paper);
    }

    /// <summary>Step 4: the real census, both ways.</summary>
    private void DrawParkNumbers(ReportPage report, Canvas canvas, TimeSpan since)
    {
        if (since < TimeSpan.FromMilliseconds(300)) return;
        report.Heading(Left.X, 28, $"{matched}", Brand.DarkGreen);
        canvas.Print(Left.X, 30, "rows from Join", Brand.DarkGray, Brand.Paper);
        report.Heading(Left.X + 22, 28, $"{tracked}", Brand.Red);
        canvas.Print(Left.X + 22, 30, "rows from FullJoin", Brand.DarkGray, Brand.Paper);
        canvas.Print(Left.X, 32, $"{rostered} rostered, {matched} matched, {tracked - matched} only on the grid.", Brand.Ink, Brand.Paper);
    }
}
