using System.Runtime.InteropServices;
using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadConsole.Input;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>
/// The executive summary, and the report's home page: what we found, how much is resolved, and every lesson
/// headlined by the .NET 11 / C# 15 improvement it teaches. Up/Down picks a lesson, NEXT or Enter opens it;
/// finishing a lesson comes back here with the next one selected.
/// </summary>
public sealed class SummaryBeat(Page page) : Beat
{
    private static readonly TimeSpan RowTime = TimeSpan.FromMilliseconds(60);
    private static readonly Color Selected = new(232, 242, 214);

    private int selected = 1;                 // index into Findings.All; INC-002 is the first lesson built
    private (string Text, TimeSpan At)? notice;
    private TimeSpan now;

    public override string Label => "Summary";

    public override string? Hint => "↑↓ lesson  ENTER open";

    /// <summary>Select a lesson by number (1-10), e.g. the one after the lesson just finished.</summary>
    public void Select(int lesson) => selected = Math.Clamp(lesson - 1, 0, Findings.All.Length - 1);

    public override bool HandleKey(Keys key, IStory story)
    {
        switch (key)
        {
            case Keys.Up:
                selected = (selected + Findings.All.Length - 1) % Findings.All.Length;
                return true;
            case Keys.Down:
                selected = (selected + 1) % Findings.All.Length;
                return true;
            case Keys.Right or Keys.Enter or Keys.Space or Keys.PageDown:
                var lesson = Findings.All[selected];
                if (story.HasLesson(lesson.Lesson)) story.OpenLesson(lesson.Lesson);
                else notice = ($"{lesson.LessonLabel}, {lesson.Feature}, is still in preparation.", now);
                return true;
            default:
                return false;
        }
    }

    public override void Draw(Frame frame)
    {
        now = frame.Now;
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame("Executive summary", page);

        // Title block.
        report.Kicker(ReportPage.Left, 6, "Post-incident technical assessment");
        report.Heading(ReportPage.Left, 8, "Isla Nublar ParkOps");
        report.Paragraph(ReportPage.Left, 11, 76,
            "ParkOps told the control room the park was safe. It wasn't. We traced ten incidents to code, and each one is " +
            "fixed by an improvement in .NET 11 or C# 15. Every lesson below follows the same path: the incident, the " +
            "evidence on the park's own screens, the root cause, the fix, and the result.", Brand.DarkGray);

        // Scorecard.
        const int scoreX = 92;
        var resolved = Findings.All.Count(f => Findings.Resolved.Contains(f.Id));
        canvas.Fill(new Rectangle(scoreX - 2, 6, 1, 8), Brand.Green, Brand.Green);
        report.Kicker(scoreX, 6, "Lessons resolved");
        report.Heading(scoreX, 8, $"{resolved} / {Findings.All.Length}", resolved > 0 ? Brand.DarkGreen : Brand.Ink);
        canvas.Print(scoreX, 11, "Prepared for InGen by Leading EDJE", Brand.DarkGray, Brand.Paper);
        canvas.Print(scoreX, 12, Workstation.Clip(RuntimeInformation.FrameworkDescription, ReportPage.Right - scoreX), Brand.Muted, Brand.Paper);

        // Lessons.
        int[] columns = [ReportPage.Left + 2, ReportPage.Left + 13, ReportPage.Left + 32, ReportPage.Left + 85, ReportPage.Left + 97];
        string[] headers = ["LESSON", "PLATFORM", "IMPROVEMENT  /  INCIDENT", "SEVERITY", "STATUS"];
        foreach (var (i, header) in headers.Index()) canvas.Print(columns[i], 16, header, Brand.Muted, Brand.Paper);
        report.Rule(17);

        var y = 18;
        foreach (var (index, lesson) in Findings.All.Index())
        {
            if (frame.Elapsed < RowTime * index) break;
            var isSelected = index == selected;
            var paper = isSelected ? Selected : index % 2 == 0 ? Brand.Paper : Brand.WhiteShade;
            canvas.Fill(new Rectangle(ReportPage.Left, y, ReportPage.Right - ReportPage.Left, 2), Brand.Ink, paper);
            if (isSelected)
            {
                canvas.Fill(new Rectangle(ReportPage.Left, y, 1, 2), Brand.Green, Brand.Green);
                canvas.Glyph(ReportPage.Left + 1, y, Cp437.Play, Brand.DarkGreen, paper);
            }

            var done = Findings.Resolved.Contains(lesson.Id);
            canvas.Print(columns[0], y, lesson.LessonLabel.ToUpperInvariant(), Brand.DarkGreen, paper);
            report.PlatformPill(columns[1], y, lesson.Platform);
            canvas.Print(columns[2], y, Workstation.Clip(lesson.Feature, columns[3] - columns[2] - 2), Brand.Ink, paper);
            canvas.Print(columns[2], y + 1, Workstation.Clip($"{lesson.Id}  {lesson.Incident}: {lesson.Summary}", ReportPage.Right - columns[2] - 1), Brand.Muted, paper);
            report.SeverityPill(columns[3], y, lesson.Severity);
            if (done) report.Pill(columns[4], y, $"{Glyphs.Check} RESOLVED", Brand.GreenShade);
            else if (BuiltLessons.Contains(lesson.Lesson)) canvas.Print(columns[4] + 1, y, "OPEN", Brand.Ink, paper);
            else canvas.Print(columns[4] + 1, y, "IN PREP", Brand.Muted, paper);
            y += 2;
        }

        var hint = notice is { } n && frame.Now - n.At < TimeSpan.FromSeconds(3)
            ? (n.Text, Brand.Red)
            : ("↑ ↓  choose a lesson     →  or  ENTER  open it     1-9, 0  jump straight to a lesson", Brand.Muted);
        canvas.Print(ReportPage.Left, 39, hint.Item1, hint.Item2, Brand.Paper);
    }

    /// <summary>Lesson numbers that have a chapter in the deck; set by the deck once it's built.</summary>
    public HashSet<int> BuiltLessons { get; } = [];
}
