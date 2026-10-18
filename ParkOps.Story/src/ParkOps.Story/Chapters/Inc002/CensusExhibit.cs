using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Core.Sensors;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadConsole;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc002;

public enum CodePath { Before, After }

/// <summary>
/// A report page whose exhibit is the control room's F2 ANIMALS screen, running live on whichever CensusPanel is
/// active: ParkOps.Start's (inner Join) or the fixed one (FullJoin). RECOUNT plays on entry; the next step adds the
/// report's reading of the exhibit and marks the lines it refers to. TAB flips the code path and re-runs it.
/// </summary>
public sealed class CensusExhibit(Finding finding, CodePath initial, Page page) : Beat
{
    // The exhibit: a control-room screen set into the page, with the report's commentary in a column beside it.
    private static readonly Rectangle Exhibit = new(ReportPage.Left, 12, 74, 26);
    private const int ColumnX = 86;
    private const int ColumnWidth = ReportPage.Right - ColumnX;
    private const int CensusWidth = 47;

    private static readonly TimeSpan RecountTime = TimeSpan.FromMilliseconds(2600);
    private static readonly TimeSpan RerunTime = TimeSpan.FromMilliseconds(900);
    private static readonly Color Highlight = new(28, 52, 24);

    private readonly IReadOnlyList<Dinosaur> roster = Census.Roster();
    private readonly IReadOnlyList<TrackedSignal> tracked = SensorNetwork.Sweep();

    private CodePath path;           // set in Enter; TAB flips it
    private Readout census = null!;
    private TimeSpan? ranAt;          // when the active path last started running (frame clock)
    private TimeSpan scanTime = RecountTime;
    private bool rerun;

    public override string Label => initial == CodePath.Before ? "Before" : "After";

    public override int Steps => 2;

    public override string? Hint => $"TAB {(path == CodePath.Before ? "after" : "before")} code";

    private string ExhibitNumber => $"{finding.Lesson}.{(initial == CodePath.Before ? 1 : 2)}";

    public override void Enter()
    {
        path = initial;
        census = Census.Render(path, roster, tracked);
        ranAt = null;
        scanTime = RecountTime;
    }

    public override bool Toggle()
    {
        path = path == CodePath.Before ? CodePath.After : CodePath.Before;
        census = Census.Render(path, roster, tracked);
        rerun = true;
        return true;
    }

    public override void Draw(Frame frame)
    {
        if (rerun)
        {
            ranAt = frame.Now;
            scanTime = RerunTime;
            rerun = false;
        }
        var started = ranAt ?? frame.Now - frame.Elapsed;
        var running = frame.Now - started;
        var scanning = running < scanTime;
        var revealFrom = started + scanTime;
        var typed = frame.Now - revealFrom > Workstation.RevealTime(census);

        var report = new ReportPage(frame.Canvas);
        report.Frame(finding.LessonLabel, page);
        report.Kicker(ReportPage.Left, 6, $"{finding.LessonLabel}  ·  Exhibit {ExhibitNumber}  ·  {(path == CodePath.Before ? "as shipped" : "with the fix")}");
        report.Heading(ReportPage.Left, 8, path == CodePath.Before ? "The census can't come out wrong" : "The census tells the truth");

        var ws = new Workstation(frame.Canvas, frame.Now);
        var alertOn = ws.Blink || !typed;
        var censusArea = DrawExhibit(ws, scanning, running, revealFrom, alertOn, started);

        var source = path == CodePath.Before ? "ParkOps.Start's CensusPanel (expected.Join)" : "the fixed CensusPanel (expected.FullJoin)";
        report.Paragraph(ReportPage.Left, Exhibit.MaxExtentY + 2, Exhibit.Width,
            $"Exhibit {ExhibitNumber}. Control room, F2 ANIMALS, after RECOUNT. Census computed live by {source}.", Brand.Muted);

        DrawCodePath(frame, report, frame.Now - started);
        if (!scanning) Reading(frame, report, censusArea);
    }

    /// <summary>The control-room screen as a figure: a menu-bar strip with the screen's status, then the CRT itself.</summary>
    private Rectangle DrawExhibit(Workstation ws, bool scanning, TimeSpan running, TimeSpan revealFrom, bool alertOn, TimeSpan started)
    {
        var bar = new Rectangle(Exhibit.X, Exhibit.Y - 1, Exhibit.Width, 1);
        ws.Print(bar.X, bar.Y, new string(' ', bar.Width), Palette.Ink, Palette.MenuBar);
        ws.Print(bar.X + 1, bar.Y, $"{Cp437.Menu} INGEN // CONTROL ROOM // F2 ANIMALS", Palette.Ink, Palette.MenuBar);
        var (badge, color) = scanning ? ("COUNTING", ws.Blink ? Palette.Amber : Palette.AmberDim)
            : census.Status == PanelStatus.Alert ? ("FAILED", alertOn ? Palette.Bad : Palette.BadDark)
            : ("CLEAR", Palette.BadgeGood);
        ws.Print(bar.MaxExtentX - badge.Length - 1, bar.Y, $" {badge} ", Color.White, color);

        var inner = ws.Screen(Exhibit);
        var censusArea = new Rectangle(inner.X + 1, inner.Y, CensusWidth, inner.Height);
        if (scanning) RecountScan(ws, censusArea, running);
        else
        {
            ws.Readout(census, "ANIMAL CENSUS", "Roster vs. the animals motion sensors detect", censusArea, revealFrom, alertOn);
            ws.Print(censusArea.X, censusArea.MaxExtentY, $"Last recount {DateTime.Now:HH:mm:ss} - {roster.Count} tags checked", Palette.Dim);
        }
        SpeciesChart(ws, new Rectangle(inner.X + CensusWidth + 3, inner.Y, inner.Width - CensusWidth - 4, inner.Height), running, scanning);
        ws.Wipe(inner, started);
        return censusArea;
    }

    /// <summary>Which CensusPanel the exhibit is running. Both are compiled into this build.</summary>
    private void DrawCodePath(Frame frame, ReportPage report, TimeSpan sinceRun)
    {
        var canvas = frame.Canvas;
        canvas.Fill(new Rectangle(ColumnX - 2, Exhibit.Y - 1, 1, 9), Brand.Green, Brand.Green);
        report.Kicker(ColumnX, Exhibit.Y - 1, "Code path running");

        (CodePath Path, string Name, string Code, string From, Color Color)[] options =
        [
            (CodePath.Before, "BEFORE", "expected.Join(...)", "ParkOps.Start", Brand.Red),
            (CodePath.After, "AFTER", "expected.FullJoin(...)", "ParkOps.Story.Fixed", Brand.GreenShade),
        ];
        var y = Exhibit.Y + 1;
        foreach (var option in options)
        {
            var active = option.Path == path;
            var flash = active && sinceRun < TimeSpan.FromMilliseconds(500);
            report.Pill(ColumnX, y, $"{(active ? Cp437.Play : ' ')} {option.Name,-6}",
                active ? (flash ? Brand.Green : option.Color) : Brand.Rule, active ? Brand.TrueWhite : Brand.Muted);
            canvas.Print(ColumnX + 12, y, option.Code, active ? Brand.Ink : Brand.Muted, Brand.Paper);
            canvas.Print(ColumnX + 12, y + 1, option.From, active ? Brand.DarkGray : Brand.Rule, Brand.Paper);
            y += 3;
        }
        canvas.Print(ColumnX, y, "TAB switches it live. Both versions", Brand.Muted, Brand.Paper);
        canvas.Print(ColumnX, y + 1, "are compiled in: no rebuild.", Brand.Muted, Brand.Paper);
    }

    /// <summary>The report's reading of the exhibit, with the readout lines it refers to marked on the screen.</summary>
    private void Reading(Frame frame, ReportPage report, Rectangle censusArea)
    {
        var since = frame.Since(1);
        if (since < TimeSpan.Zero) return;
        var canvas = frame.Canvas;

        var located = Census.Located(roster, tracked);
        var text = path == CodePath.Before
            ? $"ParkOps reports {located} of {roster.Count} animals located. The motion grid is tracking {tracked.Count} signals " +
              $"in the same sweep. The census never asks about the other {tracked.Count - located}, so its light can't turn red."
            : $"{Census.Unrostered(roster, tracked).Count} signals aren't on the roster: {Census.Describe(roster, tracked)}. " +
              "The animals are breeding. FullJoin keeps the rows only one side has, so the census can finally say so.";

        var fade = Math.Clamp(since.TotalMilliseconds / 300, 0, 1);
        for (var i = 0; i < census.Lines.Count; i++)
        {
            var y = censusArea.Y + 2 + i;
            for (var x = censusArea.X; x < censusArea.X + CensusWidth; x++)
                canvas.Surface.SetBackground(x, y, Canvas.Lerp(Palette.Background, Highlight, fade));
            canvas.Glyph(censusArea.X - 1, y, Cp437.Play, Brand.Green, Palette.Background);
        }

        var top = Exhibit.Y + 10;
        report.Rule(top - 1, ColumnX, ReportPage.Right);
        canvas.Glyph(ColumnX, top + 1, Cp437.Play, Brand.Green, Brand.Paper);
        report.Kicker(ColumnX + 2, top + 1, "What it shows");
        report.Paragraph(ColumnX, top + 3, ColumnWidth, text, Brand.Ink, (int)(since.TotalMilliseconds * 0.9));
    }

    private int ScannedSoFar(TimeSpan elapsed) =>
        (int)(Math.Clamp(elapsed / (scanTime - TimeSpan.FromMilliseconds(scanTime == RecountTime ? 400 : 150)), 0, 1) * roster.Count);

    /// <summary>The control room's RECOUNT animation: tag counter, progress bar, scrolling log of tags checked.</summary>
    private void RecountScan(Workstation ws, Rectangle area, TimeSpan elapsed)
    {
        var x = ws.Chip(area.X, area.Y, "ANIMAL CENSUS", Palette.Ink, Palette.Amber);
        ws.Print(x, area.Y, $" {Cp437.Bullet} COUNTING ", ws.Blink ? Palette.Amber : Palette.AmberDim);
        ws.Print(area.X, area.Y + 1, "Checking rostered tags against motion grid", Palette.Dim);

        var scanned = ScannedSoFar(elapsed);
        ws.Print(area.X, area.Y + 3, $"{scanned,3} of {roster.Count} tags checked", Palette.Strong);
        const int barWidth = 40;
        var filled = scanned * barWidth / roster.Count;
        ws.Print(area.X, area.Y + 4, new string(Cp437.FullBlock, filled), Palette.Amber);
        ws.Print(area.X + filled, area.Y + 4, new string(Cp437.LightShade, barWidth - filled), Palette.GridDot);

        var log = Math.Min(scanned, area.Height - 7);
        for (var i = 0; i < log; i++)
        {
            var animal = roster[scanned - 1 - i];
            var line = $"{animal.Tag,-7}{animal.GetType().Name,-20}";
            if (i == 0) ws.Print(area.X, area.Y + 6 + i, $"{line}checking", Palette.Ink, Palette.Amber);
            else ws.Print(area.X, area.Y + 6 + i, $"{line}located", i < 4 ? Palette.Good : Palette.Dim);
        }
    }

    /// <summary>ON THE ROSTER bar chart; during a recount the bars refill in roster order as tags are scanned.</summary>
    private void SpeciesChart(Workstation ws, Rectangle area, TimeSpan running, bool scanning)
    {
        ws.Chip(area.X, area.Y, "ON THE ROSTER", Palette.Ink, Palette.Amber);
        ws.Print(area.X, area.Y + 1, Workstation.Clip("Head count by species", area.Width), Palette.Dim);

        var species = roster.GroupBy(d => d.GetType().Name).Select(g => (Name: g.Key, Count: g.Count())).ToList();
        var most = species.Max(s => s.Count);
        var barWidth = area.Width - 5;
        int? scanned = scanning ? ScannedSoFar(running) : null;

        var y = area.Y + 3;
        var before = 0;
        foreach (var (name, count) in species)
        {
            var shown = scanned is { } n ? Math.Clamp(n - before, 0, count) : count;
            var counting = scanned is not null && shown > 0 && shown < count;
            ws.Print(area.X, y, Workstation.Clip(name, area.Width), counting ? Palette.Amber : Palette.Text);
            var length = (int)(shown / (double)most * barWidth);
            ws.Print(area.X, y + 1, new string(Cp437.FullBlock, Math.Max(length, shown > 0 ? 1 : 0)), counting ? Palette.Amber : Palette.AmberDim);
            ws.Print(area.X + length + 1, y + 1, scanned is null ? count.ToString() : $"{shown}/{count}", counting ? Palette.Amber : Palette.Dim);
            before += count;
            y += 3;
        }
    }
}

/// <summary>The census, both ways, plus the facts the report quotes. All computed from the real roster and sensor sweep.</summary>
public static class Census
{
    public static IReadOnlyList<Dinosaur> Roster() => [.. ParkRoster.CoreAnimals(), .. Genetics.Plugin.WuLab.NewArrivals()];

    public static Readout Render(CodePath path, IReadOnlyList<Dinosaur> roster, IReadOnlyList<TrackedSignal> tracked) => path switch
    {
        CodePath.Before => ParkOps.ControlRoom.Panels.CensusPanel.Render(roster, tracked),
        _ => Fixed.CensusPanel.Render(roster, tracked),
    };

    public static int Located(IReadOnlyList<Dinosaur> roster, IReadOnlyList<TrackedSignal> tracked) =>
        roster.Join(tracked, e => e.Tag, t => t.Tag, (e, _) => e).Count();

    public static List<string> Unrostered(IReadOnlyList<Dinosaur> roster, IReadOnlyList<TrackedSignal> tracked) =>
        [.. tracked.Select(t => t.Tag).Except(roster.Select(d => d.Tag))];

    /// <summary>"6 juvenile Velociraptor (V-J) and 10 juvenile Procompsognathus (C-J)".</summary>
    public static string Describe(IReadOnlyList<Dinosaur> roster, IReadOnlyList<TrackedSignal> tracked)
    {
        var species = roster.GroupBy(d => d.Tag.Split('-')[0]).ToDictionary(g => g.Key, g => g.First().GetType().Name);
        var groups = Unrostered(roster, tracked)
            .GroupBy(tag => tag[..tag.LastIndexOf('-')])
            .Select(g =>
            {
                var parent = species.GetValueOrDefault(g.Key.Split('-')[0], "unknown");
                return $"{g.Count()} juvenile {parent} ({g.Key})";
            })
            .ToList();
        return groups.Count <= 1 ? string.Concat(groups) : $"{string.Join(", ", groups[..^1])} and {groups[^1]}";
    }
}
