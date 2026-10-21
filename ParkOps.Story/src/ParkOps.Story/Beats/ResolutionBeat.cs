using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>
/// Before/after numbers for the resolution page. Values come from running both code paths; <see cref="Partial"/> says the
/// fix, as it actually ran, didn't fully fix the incident (the page then says so instead of stamping RESOLVED).
/// </summary>
public sealed record Outcome(string Before, string BeforeCaption, string After, string AfterCaption, bool Partial = false);

/// <summary>
/// Closes a lesson: the before and after readings side by side, the change that was made, the recommendation,
/// and (as the last step) the RESOLVED stamp, which also marks the finding resolved on the findings page.
/// </summary>
public sealed class ResolutionBeat(Finding finding, Func<Outcome> outcome, string change, string recommendation, Page page) : Beat
{
    private Outcome? measured;

    public override string Label => "Resolved";

    public override int Steps => 2;

    public override void Enter() => measured = outcome();

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(finding.LessonLabel, page);
        var result = measured ??= outcome();

        report.Kicker(ReportPage.Left, ReportPage.KickerRow, $"{finding.LessonLabel}  ·  Resolution  ·  {finding.Id}");
        report.Heading(ReportPage.Left, ReportPage.HeadingRow, finding.Feature);
        var x = report.PlatformPill(ReportPage.Left, 7, finding.Platform);
        x = report.SeverityPill(x + 1, 7, finding.Severity);
        if (!frame.Reached(1)) report.Pill(x + 1, 7, "OPEN", Brand.DarkGray);
        else if (result.Partial) report.Pill(x + 1, 7, "PARTIAL", Brand.Beige, Brand.Ink);
        else report.Pill(x + 1, 7, $"{Glyphs.Check} RESOLVED", Brand.GreenShade);

        Card(canvas, report, new Rectangle(ReportPage.Left, 10, 54, 9), "Before", result.Before, result.BeforeCaption, Brand.Red);
        Card(canvas, report, new Rectangle(ReportPage.Left + 58, 10, 54, 9), "After", result.After, result.AfterCaption, result.Partial ? Brand.Red : Brand.Green);

        var y = report.Paragraph(ReportPage.Left, 22, 70, change, Brand.Ink);
        report.Kicker(ReportPage.Left, 21, "Change");
        report.Kicker(ReportPage.Left, y + 1, "Recommendation");
        report.Paragraph(ReportPage.Left, y + 2, 70, recommendation, Brand.Ink);

        report.Kicker(ReportPage.Left + 80, 21, "Fixed with");
        var pill = report.PlatformPill(ReportPage.Left + 80, 22, finding.Platform);
        report.Pill(pill + 1, 22, Workstation.Clip(finding.Feature, ReportPage.Right - pill - 3), Brand.Black);

        if (frame.Reached(1))
        {
            Findings.Resolved.Add(finding.Id);
            if (result.Partial) Findings.Partial.Add(finding.Id); else Findings.Partial.Remove(finding.Id);
            report.Stamp(ReportPage.Left + 80, 25, result.Partial ? "PARTIAL" : "RESOLVED", result.Partial ? Brand.Red : Brand.GreenShade, frame.Since(1));
        }
    }

    private static void Card(Canvas canvas, ReportPage report, Rectangle box, string label, string value, string caption, Color accent)
    {
        canvas.Fill(box, Brand.Ink, Brand.WhiteShade);
        canvas.Fill(new Rectangle(box.X, box.Y, box.Width, 1), accent, Brand.WhiteShade, Glyphs.UpperHalf);
        canvas.Print(box.X + 2, box.Y + 1, string.Join(' ', label.ToUpperInvariant().ToCharArray()), Brand.Muted, Brand.WhiteShade);
        report.Heading(box.X + 2, box.Y + 2, value, accent == Brand.Green ? Brand.DarkGreen : Brand.Red);
        foreach (var (i, line) in ReportPage.WordWrap(caption, box.Width - 4).Index().Take(3))
            canvas.Print(box.X + 2, box.Y + 5 + i, line, Brand.DarkGray, Brand.WhiteShade);
    }
}
