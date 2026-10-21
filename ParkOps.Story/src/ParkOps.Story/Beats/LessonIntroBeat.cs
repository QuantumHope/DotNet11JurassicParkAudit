using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>
/// A lesson's section slide: one finding, nothing else. The lesson number, the improvement it teaches, the incident
/// in one sentence, and where it sits among the ten.
/// </summary>
public sealed class LessonIntroBeat(Finding finding) : Beat
{
    public override string Label => "Intro";

    public override string? Hint => "HOME index";

    public override Color Backdrop => Brand.Black;

    public override void Draw(Frame frame)
    {
        var canvas = frame.Canvas;
        canvas.Clear(Brand.Black);
        var elapsed = frame.Elapsed;

        Slides.Kicker(canvas, 10, 6, $"Lesson {finding.Lesson:00} of {Findings.All.Length:00}", Brand.Green);
        canvas.Big(10, 10, finding.Feature, Slides.FadeIn(Brand.TrueWhite, elapsed, 0));

        var report = new ReportPage(canvas);
        report.PlatformPill(10, 14, finding.Platform);

        canvas.Fill(new Rectangle(10, 18, 1, 6), Brand.Green, Brand.Green);
        canvas.Print(13, 18, finding.Incident.Trim('"').ToUpperInvariant(), Slides.FadeIn(Brand.Beige, elapsed, 300), Brand.Black);
        Slides.Type(canvas, 13, 20, 90, finding.Summary, Brand.WhiteShade, elapsed - TimeSpan.FromMilliseconds(450));
        canvas.Print(13, 23, $"{finding.Id}  ·  reported by {finding.ReportedBy}, {finding.Role}", Slides.FadeIn(Brand.LineNumber, elapsed, 700), Brand.Black);

        Slides.Progress(canvas, 10, 36, finding.Lesson);
    }
}
