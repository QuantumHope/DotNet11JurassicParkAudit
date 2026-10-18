using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>The findings table: every incident, who reported it, severity, and whether it's been resolved yet.</summary>
public sealed class FindingsIndexBeat(Page page) : Beat
{
    private static readonly TimeSpan RowTime = TimeSpan.FromMilliseconds(70);

    public override string Label => "Findings";

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame("Findings", page);

        report.Kicker(ReportPage.Left, 6, "Summary of findings");
        report.Heading(ReportPage.Left, 8, "Ten incidents, one codebase");
        report.Paragraph(ReportPage.Left, 11, 100,
            "Each finding pairs a production symptom with the code behind it and the .NET 11 or C# 15 feature that fixes it. " +
            "We walk them in this order.", Brand.DarkGray);

        int[] columns = [ReportPage.Left, ReportPage.Left + 10, ReportPage.Left + 40, ReportPage.Left + 59, ReportPage.Left + 92, ReportPage.Left + 102];
        var y = 15;
        string[] headers = ["ID", "FINDING", "REPORTED BY", "FIXED WITH", "SEVERITY", "STATUS"];
        foreach (var (i, header) in headers.Index()) canvas.Print(columns[i], y, header, Brand.Muted, Brand.Paper);
        y++;
        report.Rule(y++);

        foreach (var (index, finding) in Findings.All.Index())
        {
            if (frame.Elapsed < RowTime * index) break;
            var resolved = Findings.Resolved.Contains(finding.Id);
            var shade = index % 2 == 0 ? Brand.Paper : Brand.WhiteShade;
            canvas.Fill(new Rectangle(ReportPage.Left, y, ReportPage.Right - ReportPage.Left, 2), Brand.Ink, shade);

            canvas.Print(columns[0], y, finding.Id, Brand.DarkGreen, shade);
            canvas.Print(columns[1], y, Workstation.Clip(finding.Title, 28), Brand.Ink, shade);
            canvas.Print(columns[1], y + 1, Workstation.Clip(finding.Summary, 92), Brand.Muted, shade);
            canvas.Print(columns[2], y, Workstation.Clip(finding.ReportedBy, 18), Brand.Ink, shade);
            canvas.Print(columns[3], y, Workstation.Clip(finding.Feature, 31), Brand.DarkGray, shade);
            SeverityPill(report, columns[4], y, finding.Severity);
            if (resolved) report.Pill(columns[5], y, $"{Glyphs.Check} RESOLVED", Brand.GreenShade);
            else canvas.Print(columns[5] + 1, y, "OPEN", Brand.Muted, shade);
            y += 2;
        }
    }

    public static int SeverityPill(ReportPage report, int x, int y, Severity severity) => severity switch
    {
        Severity.Critical => report.Pill(x, y, "CRITICAL", Brand.Red),
        Severity.High => report.Pill(x, y, "HIGH", Brand.Beige, Brand.Ink),
        Severity.Medium => report.Pill(x, y, "MEDIUM", Brand.Blue),
        _ => report.Pill(x, y, "LOW", Brand.DarkGray),
    };
}
