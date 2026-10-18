using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Core.Sensors;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc002;

public enum CodePath { Before, After }

/// <summary>
/// F2 ANIMALS as the control room shows it, with the census computed live by whichever CensusPanel is
/// active: ParkOps.Start's (inner Join) or the fixed one (FullJoin). RECOUNT plays on entry, then the readout
/// types out; the step adds the consultant's note. TAB flips the code path and re-runs it.
/// </summary>
public sealed class CensusScene(CodePath initial) : Beat
{
    // Control-room layout (ParkOps.Start's ControlRoomScreen), minus its bottom status row: row 43 is the story bar.
    private static readonly Rectangle Viewport = new(1, 6, 74, 28);
    private static readonly Rectangle Caption = new(1, 34, 74, 5);
    private static readonly Rectangle CodePathBox = new(76, 6, 51, 15);
    private static readonly Rectangle CommandBox = new(76, 21, 51, 7);
    private static readonly Rectangle StatusBoard = new(76, 28, 51, 11);
    private static readonly Rectangle ScreenBar = new(1, 39, 74, 3);
    private static readonly Rectangle MessageBar = new(76, 39, 51, 3);

    private static readonly TimeSpan RecountTime = TimeSpan.FromMilliseconds(2600);
    private static readonly TimeSpan RerunTime = TimeSpan.FromMilliseconds(900);
    private const int CensusWidth = 49;

    private static readonly string[] OtherSystems = ["Motion sensors", "Feeding plan", "Perimeter fences", "Guest locator", "Fence maintenance"];

    private readonly IReadOnlyList<Dinosaur> roster = Census.Roster();
    private readonly IReadOnlyList<TrackedSignal> tracked = SensorNetwork.Sweep();

    private CodePath path;           // set in Enter; TAB flips it
    private Readout census = null!;
    private TimeSpan? ranAt;          // when the active path last started running (frame clock)
    private TimeSpan scanTime = RecountTime;
    private bool rerun;

    public override string Label => initial == CodePath.Before ? "Before" : "After";

    public override int Steps => 2;

    public override Color Backdrop => Palette.Chrome;

    public override string? Hint => $"TAB {(path == CodePath.Before ? "after" : "before")} code";

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

        var ws = new Workstation(frame.Canvas, frame.Now);
        var alertOn = ws.Blink || !typed;

        ws.TitleBar();
        ws.NavBar(selected: 1);

        // Viewport: census (or the recount scan) and the species chart.
        var inner = ws.Screen(Viewport);
        var censusArea = new Rectangle(inner.X + 1, inner.Y, CensusWidth, inner.Height);
        if (scanning) RecountScan(ws, censusArea, running);
        else
        {
            ws.Readout(census, "ANIMAL CENSUS", "Roster vs. the animals motion sensors detect", censusArea, revealFrom, alertOn);
            ws.Print(censusArea.X, censusArea.MaxExtentY, $"Last recount {DateTime.Now:HH:mm:ss} - {roster.Count} tags checked", Palette.Dim);
        }
        SpeciesChart(ws, new Rectangle(inner.X + CensusWidth + 3, inner.Y, inner.Width - CensusWidth - 4, inner.Height), running, scanning);
        ws.Wipe(inner, started);

        ws.Caption(Caption, 'A', "Animal Census", "VIEW: roster vs. motion grid");
        DrawCodePath(ws, frame.Now - started);
        DrawCommands(ws, scanning);
        DrawStatusBoard(ws, scanning ? null : census.Status, alertOn);

        ws.Bar(ScreenBar, $"SCREEN {Cp437.Play}", "Isla Nublar, Costa Rica", Palette.Ink, "F2 ANIMALS");
        if (scanning) BusyBar(ws, "SCANNING ROSTER TAGS", running);
        else ws.Bar(MessageBar, "SYS MSG", $"CENSUS RECOUNTED {DateTime.Now:HH:mm:ss}", Palette.Ink);

        if (!scanning) Note(frame, censusArea);
    }

    /// <summary>The consultant's note, under the readout line it's about, with the leader pointing up at it.</summary>
    private void Note(Frame frame, Rectangle censusArea)
    {
        var since = frame.Since(1);
        if (since < TimeSpan.Zero) return;

        var located = Census.Located(roster, tracked);
        var (kicker, text) = path == CodePath.Before
            ? ("What we found",
               $"ParkOps reports {located} of {roster.Count} animals located. The motion grid is tracking {tracked.Count} signals. " +
               $"The census never asks about the other {tracked.Count - located}.")
            : ("Now visible",
               $"{Census.Unrostered(roster, tracked).Count} signals that aren't on the roster: {Census.Describe(roster, tracked)}. " +
               "The animals are breeding, and the census can finally say so.");

        // Before: under the two-line readout. After: beside the end of the long list of unrostered tags.
        var lastLine = censusArea.Y + 2 + census.Lines.Count - 1;
        Point target;
        Rectangle box;
        if (path == CodePath.Before)
        {
            target = new(censusArea.X + 6, lastLine + 1);
            box = new(censusArea.X + 2, target.Y + 2, CensusWidth - 4, Callout.HeightFor(text, CensusWidth - 4));
        }
        else
        {
            const int width = 40;
            target = new(censusArea.X + 28, lastLine);
            var height = Callout.HeightFor(text, width);
            box = new(censusArea.X + 31, lastLine - height + 2, width, height);
        }
        Callout.Draw(frame.Canvas, box, kicker, text, target, since);
    }

    /// <summary>Replaces QUICK LOOK: which CensusPanel is running right now. Both are compiled into this build.</summary>
    private void DrawCodePath(Workstation ws, TimeSpan sinceRun)
    {
        ws.Bevel(CodePathBox);
        ws.Legend(CodePathBox, "ACTIVE CODE PATH - TAB to flip");
        var inner = ws.Screen(new Rectangle(CodePathBox.X + 1, CodePathBox.Y + 2, CodePathBox.Width - 2, CodePathBox.Height - 3));
        var x = inner.X + 1;
        var y = inner.Y;
        ws.Chip(x, y, "CensusPanel.Render", Palette.Ink, Palette.Amber);
        ws.Print(x, y + 1, "Both versions are compiled in. No rebuild.", Palette.Dim);

        (CodePath Path, string Name, string Code, string From)[] options =
        [
            (CodePath.Before, "BEFORE", "expected.Join(tracked, ...)", "ParkOps.Start"),
            (CodePath.After, "AFTER", "expected.FullJoin(tracked, ...)", "ParkOps.Story.Fixed"),
        ];
        y += 3;
        foreach (var option in options)
        {
            var active = option.Path == path;
            var flash = active && sinceRun < TimeSpan.FromMilliseconds(400) && ws.Blink;
            var (fg, bg) = active ? (Palette.Ink, flash ? Palette.Highlight : Palette.Amber) : (Palette.Dim, Palette.Background);
            ws.Print(x, y, $" {(active ? Cp437.Play : ' ')} {option.Name,-7}", fg, bg);
            ws.Print(x + 12, y, option.Code, active ? Palette.Strong : Palette.Dim);
            ws.Print(x + 12, y + 1, option.From, active ? Palette.Amber : Palette.GridDot);
            y += 3;
        }
        ws.Print(x, inner.MaxExtentY, $"{Cp437.Bullet} {(path == CodePath.Before ? "as dnedry shipped it" : "with the Leading EDJE fix")}",
            path == CodePath.Before ? Palette.Warn : Palette.Good);
    }

    private static void DrawCommands(Workstation ws, bool scanning)
    {
        ws.Bevel(CommandBox);
        ws.Legend(CommandBox, "COMMANDS");
        Rectangle Slot(int slot, int row) => new(CommandBox.X + 1 + slot * 16, CommandBox.Y + 1 + row * 3, 15, 3);
        ws.Button(Slot(0, 0), "RECOUNT", running: scanning);
        ws.Button(Slot(1, 0), "FEEDING PLAN");
        ws.Button(Slot(0, 1), $"{Cp437.Rewind}{Cp437.Rewind} PREV");
        ws.Button(Slot(1, 1), $"NEXT {Cp437.Play}{Cp437.Play}");
        ws.Button(Slot(2, 1), "QUIT");
    }

    /// <summary>SYSTEM STATUS with only the census live; the other systems are outside this finding.</summary>
    private static void DrawStatusBoard(Workstation ws, PanelStatus? status, bool alertOn)
    {
        ws.Bevel(StatusBoard);
        ws.Chip(StatusBoard.X + 2, StatusBoard.Y + 1, $"SYSTEM STATUS {Cp437.DoubleHorizontal}");
        var list = new Rectangle(StatusBoard.X + 2, StatusBoard.Y + 3, StatusBoard.Width - 4, StatusBoard.Height - 5);
        ws.Bevel(list.Expand(1, 1), raised: false);

        var y = list.Y;
        var (badge, color) = status switch
        {
            PanelStatus.Nominal => ("CLEAR", Palette.BadgeGood),
            PanelStatus.Alert => ("FAILED", alertOn ? Palette.Bad : Palette.BadDark),
            PanelStatus.Attention => ("CHECK", Palette.BadgeCheck),
            _ => ("WAIT", ws.Blink ? Palette.ChromeDark : Palette.Chrome),
        };
        Row(ws, list, y++, badge, color, "Animal census", Palette.Ink);
        foreach (var name in OtherSystems) Row(ws, list, y++, "  --", Palette.ChromeLight, name, Palette.ChromeDark);
    }

    private static void Row(Workstation ws, Rectangle list, int y, string badge, Color badgeColor, string name, Color ink)
    {
        ws.Print(list.X, y, new string(' ', list.Width), Palette.Ink, Palette.Paper);
        ws.Print(list.X, y, $" {badge,-6} ", Color.White, badgeColor);
        ws.Print(list.X + 9, y, $"- {name}", ink, Palette.Paper);
    }

    private static void BusyBar(Workstation ws, string label, TimeSpan running)
    {
        ws.Bar(MessageBar, "SYS MSG", label, Palette.Ink);
        const int track = 14;
        var trackX = MessageBar.MaxExtentX - track - 2;
        ws.Print(trackX, MessageBar.Y + 1, new string(Cp437.LightShade, track), Palette.ChromeDark, Palette.Paper);
        var step = (int)(running.TotalMilliseconds / 70) % ((track - 3) * 2);
        var pos = step < track - 3 ? step : (track - 3) * 2 - step;
        ws.Print(trackX + pos, MessageBar.Y + 1, new string(Cp437.FullBlock, 3), Palette.Amber, Palette.Paper);
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

/// <summary>The census, both ways, plus the facts the story quotes. All computed from the real roster and sensor sweep.</summary>
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

    /// <summary>"6 juvenile Velociraptors (V-J) and 10 juvenile Procompsognathus (C-J)".</summary>
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
