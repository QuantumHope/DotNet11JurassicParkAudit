using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Beats;
using ParkOps.Core.Animals;
using ParkOps.Core.Sensors;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc002;

/// <summary>
/// Exhibit for lesson 2: the control room's F2 ANIMALS screen, its census computed live by ParkOps.Start's
/// CensusPanel (inner Join) or the fixed one (FullJoin). Entry plays the RECOUNT animation, then the readout types out.
/// </summary>
public sealed class CensusExhibit(Finding finding, CodePath initial, Page page) : ExhibitBeat(finding, initial, page)
{
    private const int CensusWidth = 47;
    private static readonly TimeSpan RecountTime = TimeSpan.FromMilliseconds(2600);
    private static readonly TimeSpan RerunTime = TimeSpan.FromMilliseconds(900);

    private readonly IReadOnlyList<Dinosaur> roster = Census.Roster();
    private readonly IReadOnlyList<TrackedSignal> tracked = SensorNetwork.Sweep();
    private Readout census = null!;

    private TimeSpan ScanTime => Rerunning ? RerunTime : RecountTime;

    protected override string ScreenName => "CONTROL ROOM // F2 ANIMALS";

    protected override string Heading(CodePath path) =>
        path == CodePath.Before ? "The census can't come out wrong" : "The census tells the truth";

    protected override string Caption(CodePath path) =>
        "Control room, F2 ANIMALS, after RECOUNT. Census computed live by " +
        (path == CodePath.Before ? "ParkOps.Start's CensusPanel (expected.Join)." : "the fixed CensusPanel (expected.FullJoin).");

    protected override (string Code, string From) CodeFor(CodePath path) => path == CodePath.Before
        ? ("expected.Join(...)", "ParkOps.Start")
        : ("expected.FullJoin(...)", "ParkOps.Story.Fixed");

    protected override string Reading(CodePath path)
    {
        var located = Census.Located(roster, tracked);
        return path == CodePath.Before
            ? $"ParkOps reports {located} of {roster.Count} animals located. The motion grid is tracking {tracked.Count} signals " +
              $"in the same sweep. The census never asks about the other {tracked.Count - located}, so its light can't turn red."
            : $"{Census.Unrostered(roster, tracked).Count} signals aren't on the roster: {Census.Describe(roster, tracked)}. " +
              "The animals are breeding. FullJoin keeps the rows only one side has, so the census can finally say so.";
    }

    protected override void Run(CodePath path) => census = Census.Render(path, roster, tracked);

    protected override bool IsBusy(TimeSpan running) => running < ScanTime;

    protected override ScreenState DrawScreen(Workstation ws, Rectangle inner, ExhibitClock clock)
    {
        var censusArea = new Rectangle(inner.X + 1, inner.Y, CensusWidth, inner.Height);
        var revealFrom = clock.Started + ScanTime;
        var typed = clock.Now - revealFrom > Workstation.RevealTime(census);
        var alertOn = ws.Blink || !typed;
        if (clock.Busy) RecountScan(ws, censusArea, clock.Running);
        else
        {
            ws.Readout(census, "ANIMAL CENSUS", "Roster vs. the animals motion sensors detect", censusArea, revealFrom, alertOn);
            ws.Print(censusArea.X, censusArea.MaxExtentY, $"Last recount {DateTime.Now:HH:mm:ss} - {roster.Count} tags checked", Palette.Dim);
        }
        SpeciesChart(ws, new Rectangle(inner.X + CensusWidth + 3, inner.Y, inner.Width - CensusWidth - 4, inner.Height), clock.Running, clock.Busy);

        var marks = census.Lines.Select((_, i) => Row(censusArea.X, censusArea.Y + 2 + i, CensusWidth)).ToList();
        return clock.Busy ? new("COUNTING", ws.Blink ? Palette.Amber : Palette.AmberDim, [])
            : census.Status == PanelStatus.Alert ? new("FAILED", alertOn ? Palette.Bad : Palette.BadDark, marks)
            : ScreenState.Clear(marks);
    }

    private int ScannedSoFar(TimeSpan elapsed) =>
        (int)(Math.Clamp(elapsed / (ScanTime - TimeSpan.FromMilliseconds(Rerunning ? 150 : 400)), 0, 1) * roster.Count);

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
