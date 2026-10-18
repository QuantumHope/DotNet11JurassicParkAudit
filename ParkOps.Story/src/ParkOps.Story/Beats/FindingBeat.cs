using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>A paragraph on a finding page, with an optional headline number in the right column.</summary>
public sealed record FindingSection(string Kicker, string Text, string? Stat = null, string? StatCaption = null);

/// <summary>
/// A finding's opening page: number, title, who reported it, and the system involved, then one section per step
/// (observation, impact, the numbers). The fix is not named here; that's the reveal.
/// </summary>
public sealed class FindingBeat(Finding finding, string system, string source, IReadOnlyList<FindingSection> sections, Page page) : Beat
{
    public override string Label => "Finding";

    public override int Steps => sections.Count;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame($"Finding {finding.Id[^2..]}", page);

        var number = Array.IndexOf(Findings.All, finding) + 1;
        report.Kicker(ReportPage.Left, 6, $"Finding {number:00} of {Findings.All.Length:00}  ·  {finding.Id}");
        report.Heading(ReportPage.Left, 8, finding.Title);
        var x = FindingsIndexBeat.SeverityPill(report, ReportPage.Left, 11, finding.Severity);
        report.Pill(x + 1, 11, "OPEN", Brand.DarkGray);

        report.Facts(ReportPage.Left, 13,
        [
            ("Reported by", $"{finding.ReportedBy}, {finding.Role}"),
            ("System", system),
            ("Source", source),
        ]);
        report.Rule(17);

        const int textWidth = 64;
        var y = 19;
        const int statX = ReportPage.Left + textWidth + 8;
        var statY = 20; // big text needs an even row
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
