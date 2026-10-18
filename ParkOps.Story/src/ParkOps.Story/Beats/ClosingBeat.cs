using ParkOps.Story.Rendering;
using ParkOps.Story.Story;

namespace ParkOps.Story.Beats;

/// <summary>The last page: where the assessment stands, and how to reach us.</summary>
public sealed class ClosingBeat(Page page) : Beat
{
    public override string Label => "Close";

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        report.Frame("Summary", page);

        var resolved = Findings.All.Count(f => Findings.Resolved.Contains(f.Id));
        report.Kicker(ReportPage.Left, 6, "Where the assessment stands");
        report.Heading(ReportPage.Left, 8, $"{resolved} of {Findings.All.Length} findings resolved");
        var y = report.Paragraph(ReportPage.Left, 12, 90,
            "Every fix in this report shipped without a rebuild on stage: both code paths are compiled into the same binary, " +
            "and the story only changes which one runs.", Brand.DarkGray) + 1;

        foreach (var finding in Findings.All)
        {
            var done = Findings.Resolved.Contains(finding.Id);
            frame.Canvas.Print(ReportPage.Left, y, done ? $"{Glyphs.Check}" : "·", done ? Brand.GreenShade : Brand.Muted, Brand.Paper);
            frame.Canvas.Print(ReportPage.Left + 2, y, finding.Id, Brand.DarkGreen, Brand.Paper);
            frame.Canvas.Print(ReportPage.Left + 11, y, finding.Title, done ? Brand.Ink : Brand.Muted, Brand.Paper);
            frame.Canvas.Print(ReportPage.Left + 42, y, finding.Feature, Brand.Muted, Brand.Paper);
            y++;
        }

        report.Rule(y + 1);
        report.Wordmark(ReportPage.Left, y + 3, onDark: false);
        frame.Canvas.Print(ReportPage.Left + 16, y + 3, "leadingedje.com", Brand.DarkGray, Brand.Paper);
    }
}
