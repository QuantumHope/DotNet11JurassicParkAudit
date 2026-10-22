using System.Text.RegularExpressions;
using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Core.Mapping;
using ParkOps.Story.Beats;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc009;

/// <summary>
/// Exhibit for lesson 9: F3 SECURITY's guest locator, run by the original GuestLocatorPanel or the fixed one, beside
/// the paddock grid it searches. The grid replays the real scan: every cell the code visited, in order, up to the count
/// it reported. The original never stops, so it passes the Murphy children (and the raptor beside them) and reports the
/// last guest it found instead: Dennis Nedry, on the east road.
/// </summary>
public sealed partial class GuestExhibit(Finding finding, CodePath initial, Page page) : ExhibitBeat(finding, initial, page)
{
    private const int ReadoutWidth = 29;
    private const char Square = (char)254; // ■
    private readonly ParkMap map = ParkMap.Load();
    private Readout readout = null!;
    private int scanned;

    private TimeSpan ScanTime => Rerunning ? TimeSpan.FromMilliseconds(900) : TimeSpan.FromMilliseconds(2600);

    /// <summary>The missing children's cell, as a scan index: the first cell with guests in it.</summary>
    private int GuestCell => map.Cells.Index().First(c => c.Item.Occupants.OfType<Guest>().Any()).Index;

    /// <summary>The cell the readout reported, as a scan index.</summary>
    private int reported;

    /// <summary>Each cell's place in the order the search visited it, or -1 if it never did.</summary>
    private int[] rank = [];

    protected override string ScreenName => "CONTROL ROOM // F3 SECURITY";

    protected override string Heading(CodePath path) =>
        path == CodePath.Before ? "The search team is sent to the wrong guest" : "Found, flagged, and the search stops";

    protected override string Caption(CodePath path) =>
        "Control room, F3 SECURITY, LOCATE GUESTS. The grid replays the cells the search visited. Run by " +
        (path == CodePath.Before ? "the original GuestLocatorPanel." : "the fixed GuestLocatorPanel.");

    protected override (string Code, string From) CodeFor(CodePath path) => path == CodePath.Before
        ? ("foundThem + Cells[...]", "ParkOps (original)")
        : ("break search; map[x,y]", "ParkOps.Story.Fixed");

    protected override string Reading(CodePath path)
    {
        var guest = map.Cells[GuestCell];
        return path == CodePath.Before
            ? $"The search found Tim and Lex at ({guest.X},{guest.Y}), then skipped the rest of their row, raptor and all, and " +
              "kept going: the 'stop searching' flag only moves to the next row. On the east road it found another guest, " +
              "Dennis Nedry, and overwrote the answer: zone clear."
            : $"A labeled break stops the search at the children, cell {scanned}. map[x, y] reads through an extension indexer, " +
              "and the set built with(StringComparer.OrdinalIgnoreCase) matches the firmware's lower-case zone: RESTRICTED. " +
              "The raptor is in the next cell.";
    }

    protected override void Run(CodePath path)
    {
        readout = path == CodePath.Before
            ? ParkOps.ControlRoom.Panels.GuestLocatorPanel.Render(map)
            : Fixed.Inc009.GuestLocatorPanel.Render(map);
        scanned = int.Parse(ScannedPattern().Match(readout.Lines[^1].Text).Groups[1].Value);
        var position = PositionPattern().Match(readout.Lines.First(l => l.Text.StartsWith("Position")).Text);
        reported = int.Parse(position.Groups[2].Value) * map.Width + int.Parse(position.Groups[1].Value);

        // The cells each version visits, in order, the way its loops walk them: both leave the row at the first cell with
        // guests; the original then carries on with the next row, the fixed one stops.
        rank = Enumerable.Repeat(-1, map.Cells.Length).ToArray();
        var visits = 0;
        for (var y = 0; y < map.Height; y++)
        {
            var stop = false;
            for (var x = 0; x < map.Width; x++)
            {
                var index = y * map.Width + x;
                rank[index] = visits++;
                if (map.Cells[index].Occupants.OfType<Guest>().Any()) { stop = path == CodePath.After; break; }
            }
            if (stop) break;
        }
    }

    protected override bool IsBusy(TimeSpan running) => running < ScanTime;

    protected override ScreenState DrawScreen(Workstation ws, Rectangle inner, ExhibitClock clock)
    {
        var area = new Rectangle(inner.X + 1, inner.Y, ReadoutWidth, inner.Height);
        var revealFrom = clock.Started + ScanTime;
        var typed = clock.Now - revealFrom > Workstation.RevealTime(readout);
        var alertOn = ws.Blink || !typed;
        if (clock.Busy)
        {
            var x = ws.Chip(area.X, area.Y, "GUEST LOCATOR", Palette.Ink, Palette.Amber);
            ws.Print(x, area.Y, $" {Cp437.Bullet} SEARCHING ", ws.Blink ? Palette.Amber : Palette.AmberDim);
            ws.Print(area.X, area.Y + 1, "Scanning the paddock grid", Palette.Dim);
            ws.Print(area.X, area.Y + 3, $"{Visited(clock),3} cells scanned", Palette.Strong);
        }
        else ws.Readout(readout, "GUEST LOCATOR", "Searches the grid for guests", area, revealFrom, alertOn);

        DrawGrid(ws, new Rectangle(inner.X + ReadoutWidth + 3, inner.Y, map.Width * 2, inner.Height), clock);

        var marks = new[] { readout.Lines.Count - 2, readout.Lines.Count - 1 }.Select(i => Row(area.X, area.Y + 2 + i, ReadoutWidth)).ToList();
        return clock.Busy ? new("SEARCHING", ws.Blink ? Palette.Amber : Palette.AmberDim, [])
            : readout.Status == PanelStatus.Alert ? new("RESTRICTED", alertOn ? Palette.Bad : Palette.BadDark, marks)
            : ScreenState.Clear(marks);
    }

    private int Visited(ExhibitClock clock) =>
        clock.Busy ? (int)(Math.Clamp(clock.Running / (ScanTime * 0.92), 0, 1) * scanned) : scanned;

    /// <summary>
    /// The 20x20 grid, two characters per cell: visited cells light up in scan order. The children and the raptor are
    /// drawn once the scan reaches them; the cell the readout reported is marked once the scan is done.
    /// </summary>
    private void DrawGrid(Workstation ws, Rectangle area, ExhibitClock clock)
    {
        ws.Chip(area.X, area.Y, "PADDOCK GRID", Palette.Ink, Palette.Amber);
        var visited = Visited(clock);
        var guest = GuestCell;
        ws.Print(area.X + 15, area.Y, $"{visited,3} / {map.Cells.Length} visited", clock.Busy ? Palette.Amber : Palette.Dim);

        foreach (var (index, cell) in map.Cells.Index())
        {
            var x = area.X + cell.X * 2;
            var y = area.Y + 1 + cell.Y;
            var restrictedRow = cell.Zone == map.Cells[guest].Zone;
            var ground = restrictedRow ? new Color(48, 14, 14) : Palette.Background;
            var seen = rank[index] >= 0 && rank[index] < visited;
            if (index == reported && !clock.Busy && index != guest)
                ws.Print(x, y, $"{Cp437.Smiley}{Cp437.Play}", Palette.Ink, ws.Blink ? Palette.Amber : Palette.AmberDim);
            else if (index == guest && seen)
                ws.Print(x, y, $"{Cp437.Smiley}{Cp437.Smiley}", Color.White, ws.Blink ? Palette.Bad : Palette.BadDark);
            else if (cell.Occupants.OfType<Dinosaur>().Any() && !clock.Busy)
                ws.Print(x, y, "R ", Color.White, ws.Blink ? Palette.Bad : Palette.BadDark);
            else if (seen && rank[index] == visited - 1 && clock.Busy)
                ws.Print(x, y, $"{Cp437.FullBlock}{Cp437.FullBlock}", Palette.Amber, ground);
            else if (seen)
                ws.Print(x, y, $"{Square} ", rank[index] > rank[guest] ? Palette.Bad : Palette.AmberDim, ground);
            else
                ws.Print(x, y, $"{Cp437.MiddleDot} ", Palette.Dim, ground);
        }

        var legend = area.Y + 1 + map.Height;
        ws.Print(area.X, legend, $"{Cp437.Smiley}", Palette.Bad);
        ws.Print(area.X + 2, legend, "Tim, Lex", Palette.Dim);
        ws.Print(area.X + 11, legend, "R", Palette.Bad);
        ws.Print(area.X + 13, legend, "raptor", Palette.Dim);
        ws.Print(area.X + 20, legend, $"{Square}", Palette.Bad);
        ws.Print(area.X + 22, legend, "after found", Palette.Dim);
        ws.Print(area.X + 34, legend, "  ", Palette.Dim, new Color(48, 14, 14));
        ws.Print(area.X + 37, legend, "p-9", Palette.Dim);
    }

    [GeneratedRegex(@"Cells scanned: (\d+)")]
    private static partial Regex ScannedPattern();

    [GeneratedRegex(@"Position: \((\d+),(\d+)\)")]
    private static partial Regex PositionPattern();
}
