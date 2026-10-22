using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>
/// A report page that weighs a change: what you get on the left, reasons to wait on the right (revealed on the second
/// step), and a source line along the bottom.
/// </summary>
public sealed class TradeoffsBeat(Finding finding, string heading, string[] gains, string[] costs, string source, Page page) : Beat
{
    private const int ColumnWidth = 52;
    private const int RightX = ReportPage.Left + ColumnWidth + 8;

    public override string Label => "Tradeoffs";

    public override int Steps => 2;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(finding.LessonLabel, page);
        report.Kicker(ReportPage.Left, ReportPage.KickerRow, $"{finding.LessonLabel}  ·  Tradeoffs  ·  {finding.Feature}");
        report.Heading(ReportPage.Left, ReportPage.HeadingRow, heading);

        Column(report, ReportPage.Left, "What you get", Brand.GreenShade, gains, frame.Elapsed);
        if (frame.Since(1) is var since && since >= TimeSpan.Zero)
            Column(report, RightX, "Reasons to wait", Brand.Red, costs, since);

        canvas.Print(ReportPage.Left, ReportPage.Bottom, source, Brand.Muted, Brand.Paper);
    }

    private static void Column(ReportPage report, int x, string title, Color color, string[] items, TimeSpan since)
    {
        var canvas = report.Canvas;
        canvas.Fill(new Rectangle(x - 2, ReportPage.Top, 1, 24), color, color);
        report.Kicker(x, ReportPage.Top, title, color);
        var y = ReportPage.Top + 2;
        foreach (var (i, item) in items.Index())
        {
            var shown = since - TimeSpan.FromMilliseconds(250 * i);
            if (shown < TimeSpan.Zero) break;
            canvas.Print(x, y, "•", color, Brand.Paper);
            report.Paragraph(x + 2, y, ColumnWidth - 2, item, Brand.Ink, (int)(shown.TotalMilliseconds * 0.3));
            y += ReportPage.WordWrap(item, ColumnWidth - 2).Count() + 1;
        }
    }
}
