using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>A paragraph on a finding page, with an optional headline number in the right column.</summary>
public sealed record FindingSection(string Kicker, string Text, string? Stat = null, string? StatCaption = null);

/// <summary>
/// A lesson's opening page, headlined by the improvement it teaches: platform, the incident behind it, who
/// reported it and the system involved, then one section per step (observation, impact, the numbers).
/// </summary>
public sealed class FindingBeat(Finding finding, string system, string source, IReadOnlyList<FindingSection> sections, Page page) : Beat
{
    public override string Label => "Finding";

    public override int Steps => sections.Count;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(finding.LessonLabel, page);

        report.Kicker(ReportPage.Left, ReportPage.KickerRow, $"{finding.LessonLabel} of {Findings.All.Length:00}  ·  The finding");
        report.Heading(ReportPage.Left, ReportPage.HeadingRow, finding.Feature);
        var x = report.PlatformPill(ReportPage.Left, 7, finding.Platform);
        x = report.SeverityPill(x + 1, 7, finding.Severity);
        report.Pill(x + 1, 7, "OPEN", Brand.DarkGray);

        report.Facts(ReportPage.Left, 9,
        [
            ("Incident", $"{finding.Id}  {finding.Incident}"),
            ("Reported by", $"{finding.ReportedBy}, {finding.Role}"),
            ("System", system),
            ("Source", source),
        ]);
        report.Rule(14);

        const int textWidth = 64;
        var y = 16;
        const int statX = ReportPage.Left + textWidth + 8;
        var statY = 16; // big text needs an even row
        foreach (var (index, section) in sections.Index())
        {
            var since = frame.Since(index);
            if (since < TimeSpan.Zero) break;
            var reveal = (int)(since.TotalMilliseconds * 0.9);

            report.Kicker(ReportPage.Left, y, section.Kicker);
            y = report.Paragraph(ReportPage.Left, y + 1, textWidth, section.Text, Brand.Ink, reveal) + 1;

            if (section.Stat is { } stat)
            {
                var color = index == sections.Count - 1 ? Brand.Red : Brand.Ink;
                canvas.Fill(new Rectangle(statX - 2, statY, 1, 5), Brand.Green, Brand.Green);
                report.Heading(statX, statY, stat, Canvas.Lerp(Brand.Paper, color, since.TotalMilliseconds / 300));
                report.Paragraph(statX, statY + 3, 38, section.StatCaption ?? "", Brand.Muted);
                statY += 8;
            }
        }
    }
}
