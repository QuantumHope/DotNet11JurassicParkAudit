using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadConsole;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

public enum CodePath { Before, After }

/// <summary>What an exhibit's screen reports this frame: the status badge on its strip, and rows the reading refers to.</summary>
public sealed record ScreenState(string Badge, Color BadgeColor, IReadOnlyList<Rectangle> Marks)
{
    public static ScreenState Clear(IReadOnlyList<Rectangle>? marks = null) => new("CLEAR", Palette.BadgeGood, marks ?? []);
}

/// <summary>Timing an exhibit's screen draws against: when its code path last ran, and whether it's still running.</summary>
public sealed record ExhibitClock(TimeSpan Now, TimeSpan Started, bool Busy, bool AlertOn)
{
    public TimeSpan Running => Now - Started;
}

/// <summary>
/// A report page whose exhibit is a park screen running live on whichever code path is active: the code ParkOps.Start
/// shipped, or the fixed code in ParkOps.Story.Fixed. Both are compiled in. On entry the exhibit runs; the next step adds
/// the report's reading and marks what it refers to. TAB flips the code path and runs it again.
/// Subclasses supply the screen, its wording, and how to run each path.
/// </summary>
public abstract class ExhibitBeat(Finding finding, CodePath initial, Page page) : Beat
{
    protected static readonly Rectangle Exhibit = new(ReportPage.Left, 12, 74, 26);
    private const int ColumnX = 86;
    private const int ColumnWidth = ReportPage.Right - ColumnX;
    private static readonly Color MarkColor = new(28, 52, 24);

    private TimeSpan? ranAt;          // frame clock when the active path last started running
    private bool rerun;
    private bool busy = true;

    public override bool Settled => !busy;

    protected CodePath Path { get; private set; }

    /// <summary>True after TAB: exhibits can shorten their run animation.</summary>
    protected bool Rerunning { get; private set; }

    protected Finding Finding => finding;

    public override string Label => initial == CodePath.Before ? "Before" : "After";

    public override int Steps => 2;

    public override string? Hint => $"TAB {(Path == CodePath.Before ? "after" : "before")} code";

    private string ExhibitNumber => $"{finding.Lesson}.{(initial == CodePath.Before ? 1 : 2)}";

    /// <summary>The park screen shown, e.g. "CONTROL ROOM // F2 ANIMALS".</summary>
    protected abstract string ScreenName { get; }

    protected abstract string Heading(CodePath path);

    /// <summary>What the exhibit is and which code computed it (follows "Exhibit 2.1.").</summary>
    protected abstract string Caption(CodePath path);

    /// <summary>The code path, as shown in the CODE PATH RUNNING panel: a call and where it comes from.</summary>
    protected abstract (string Code, string From) CodeFor(CodePath path);

    /// <summary>The report's reading of the exhibit (step 2).</summary>
    protected abstract string Reading(CodePath path);

    /// <summary>Run (or start running) the code for <paramref name="path"/>. Called on entry and after TAB.</summary>
    protected abstract void Run(CodePath path);

    /// <summary>Whether the screen is still mid-run (no reading or marks yet).</summary>
    protected virtual bool IsBusy(TimeSpan running) => running < (Rerunning ? TimeSpan.FromMilliseconds(900) : TimeSpan.FromMilliseconds(2600));

    /// <summary>Draws the screen's contents inside the CRT area.</summary>
    protected abstract ScreenState DrawScreen(Workstation ws, Rectangle inner, ExhibitClock clock);

    public override void Enter()
    {
        Path = initial;
        Rerunning = false;
        ranAt = null;
        Run(Path);
    }

    public override bool Toggle()
    {
        Path = Path == CodePath.Before ? CodePath.After : CodePath.Before;
        Rerunning = true;
        rerun = true;
        Run(Path);
        return true;
    }

    public override void Draw(Frame frame)
    {
        if (rerun)
        {
            ranAt = frame.Now;
            rerun = false;
        }
        var started = ranAt ?? frame.Now - frame.Elapsed;
        busy = IsBusy(frame.Now - started);
        var ws = new Workstation(frame.Canvas, frame.Now);
        var clock = new ExhibitClock(frame.Now, started, busy, ws.Blink || busy);

        var report = new ReportPage(frame.Canvas);
        report.Frame(finding.LessonLabel, page);
        report.Kicker(ReportPage.Left, 6, $"{finding.LessonLabel}  ·  Exhibit {ExhibitNumber}  ·  {(Path == CodePath.Before ? "as shipped" : "with the fix")}");
        report.Heading(ReportPage.Left, 8, Heading(Path));

        // The exhibit: a menu-bar strip naming the screen and its status, then the CRT.
        var inner = ws.Screen(Exhibit);
        var state = DrawScreen(ws, inner, clock);
        ws.Wipe(inner, started);

        var bar = new Rectangle(Exhibit.X, Exhibit.Y - 1, Exhibit.Width, 1);
        ws.Print(bar.X, bar.Y, new string(' ', bar.Width), Palette.Ink, Palette.MenuBar);
        ws.Print(bar.X + 1, bar.Y, $"{Cp437.Menu} INGEN // {ScreenName}", Palette.Ink, Palette.MenuBar);
        ws.Print(bar.MaxExtentX - state.Badge.Length - 1, bar.Y, $" {state.Badge} ", Color.White, state.BadgeColor);

        // Two lines at most: the footer rule is directly below.
        foreach (var (i, line) in ReportPage.WordWrap($"Exhibit {ExhibitNumber}. {Caption(Path)}", Exhibit.Width).Take(2).Index())
            frame.Canvas.Print(ReportPage.Left, Exhibit.MaxExtentY + 2 + i, line, Brand.Muted, Brand.Paper);

        DrawCodePath(frame, report, frame.Now - started);
        if (!busy) DrawReading(frame, report, state.Marks);
    }

    private void DrawCodePath(Frame frame, ReportPage report, TimeSpan sinceRun)
    {
        var canvas = frame.Canvas;
        canvas.Fill(new Rectangle(ColumnX - 2, Exhibit.Y - 1, 1, 9), Brand.Green, Brand.Green);
        report.Kicker(ColumnX, Exhibit.Y - 1, "Code path running");

        var y = Exhibit.Y + 1;
        foreach (var option in new[] { CodePath.Before, CodePath.After })
        {
            var active = option == Path;
            var (code, from) = CodeFor(option);
            var color = option == CodePath.Before ? Brand.Red : Brand.GreenShade;
            var flash = active && sinceRun < TimeSpan.FromMilliseconds(500);
            report.Pill(ColumnX, y, $"{(active ? Cp437.Play : ' ')} {(option == CodePath.Before ? "BEFORE" : "AFTER"),-6}",
                active ? (flash ? Brand.Green : color) : Brand.Rule, active ? Brand.TrueWhite : Brand.Muted);
            canvas.Print(ColumnX + 12, y, Workstation.Clip(code, ColumnWidth - 12), active ? Brand.Ink : Brand.Muted, Brand.Paper);
            canvas.Print(ColumnX + 12, y + 1, Workstation.Clip(from, ColumnWidth - 12), active ? Brand.DarkGray : Brand.Rule, Brand.Paper);
            y += 3;
        }
        canvas.Print(ColumnX, y, "TAB switches it live. Both versions", Brand.Muted, Brand.Paper);
        canvas.Print(ColumnX, y + 1, "are compiled in: no rebuild.", Brand.Muted, Brand.Paper);
    }

    /// <summary>The reading, with the screen areas it refers to tinted and arrowed.</summary>
    private void DrawReading(Frame frame, ReportPage report, IReadOnlyList<Rectangle> marks)
    {
        var since = frame.Since(1);
        if (since < TimeSpan.Zero) return;
        var canvas = frame.Canvas;

        var fade = Math.Clamp(since.TotalMilliseconds / 300, 0, 1);
        foreach (var mark in marks)
        {
            for (var y = mark.Y; y <= mark.MaxExtentY; y++)
            {
                for (var x = mark.X; x <= mark.MaxExtentX; x++)
                    canvas.Surface.SetBackground(x, y, Canvas.Lerp(Palette.Background, MarkColor, fade));
                canvas.Glyph(mark.X - 1, y, Cp437.Play, Brand.Green, Palette.Background);
            }
        }

        var top = Exhibit.Y + 10;
        report.Rule(top - 1, ColumnX, ReportPage.Right);
        canvas.Glyph(ColumnX, top + 1, Cp437.Play, Brand.Green, Brand.Paper);
        report.Kicker(ColumnX + 2, top + 1, "What it shows");
        report.Paragraph(ColumnX, top + 3, ColumnWidth, Reading(Path), Brand.Ink, (int)(since.TotalMilliseconds * 0.9));
    }

    /// <summary>One row of a screen, as a mark rectangle.</summary>
    protected static Rectangle Row(int x, int y, int width) => new(x, y, width, 1);
}
