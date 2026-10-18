using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>Before/after numbers for the resolution page. Values come from running both code paths.</summary>
public sealed record Outcome(string Before, string BeforeCaption, string After, string AfterCaption);

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

        report.Kicker(ReportPage.Left, 6, $"{finding.LessonLabel}  ·  Resolution  ·  {finding.Id} {finding.Incident}");
        report.Heading(ReportPage.Left, 8, finding.Feature);
        var x = report.PlatformPill(ReportPage.Left, 11, finding.Platform);
        x = report.SeverityPill(x + 1, 11, finding.Severity);
        if (frame.Reached(1)) report.Pill(x + 1, 11, $"{Glyphs.Check} RESOLVED", Brand.GreenShade);
        else report.Pill(x + 1, 11, "OPEN", Brand.DarkGray);

        Card(canvas, report, new Rectangle(ReportPage.Left, 14, 54, 9), "Before", result.Before, result.BeforeCaption, Brand.Red);
        Card(canvas, report, new Rectangle(ReportPage.Left + 58, 14, 54, 9), "After", result.After, result.AfterCaption, Brand.Green);

        report.Kicker(ReportPage.Left, 26, "Change");
        report.Paragraph(ReportPage.Left, 27, 70, change, Brand.Ink);
        report.Kicker(ReportPage.Left, 31, "Recommendation");
        report.Paragraph(ReportPage.Left, 32, 70, recommendation, Brand.Ink);

        report.Kicker(ReportPage.Left + 80, 26, "Fixed with");
        var pill = report.PlatformPill(ReportPage.Left + 80, 27, finding.Platform);
        report.Pill(pill + 1, 27, finding.Feature, Brand.Black);

        if (frame.Reached(1))
        {
            Findings.Resolved.Add(finding.Id);
            report.Stamp(ReportPage.Left + 80, 31, "RESOLVED", Brand.GreenShade, frame.Since(1));
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
