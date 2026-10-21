using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ParkOps.ControlRoom.Ui;
using ParkOps.Data;
using ParkOps.Story.Beats;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc007;

/// <summary>
/// Exhibit for lesson 7: F4 OPERATIONS' maintenance panel, querying the park database live through the original
/// MaintenancePanel (Include + ToListAsync + MaxBy in memory) or the fixed one (MaxByAsync, translated to SQL). Beside it,
/// breaches per fence; under it, a gauge of the rows each version pulled into memory.
/// </summary>
public sealed partial class MaintenanceExhibit(Finding finding, CodePath initial, Page page) : ExhibitBeat(finding, initial, page)
{
    private Task<Readout>? query;
    private (string Name, int Breaches)[] fences = [];

    private Readout? Result => query is { IsCompletedSuccessfully: true } done ? done.Result : null;

    private int RowsPulled => Result is { } r ? int.Parse(RowsPattern().Match(r.Lines[^1].Text).Groups[1].Value.Replace(",", "")) : 0;

    protected override string ScreenName => "CONTROL ROOM // F4 OPERATIONS";

    protected override string Heading(CodePath path) =>
        path == CodePath.Before ? "Every breach in memory, to find one fence" : "One row, computed in the database";

    protected override string Caption(CodePath path) =>
        "Control room, F4 OPERATIONS, FIND REPAIR, on the seeded park database. Query run live by " +
        (path == CodePath.Before ? "the original MaintenancePanel." : "the fixed MaintenancePanel.");

    protected override (string Code, string From) CodeFor(CodePath path) => path == CodePath.Before
        ? ("Include + ToList + MaxBy", "ParkOps (original)")
        : ("MaxByAsync(...)", "ParkOps.Story.Fixed");

    protected override string Reading(CodePath path)
    {
        var total = fences.Length + fences.Sum(f => f.Breaches);
        return path == CodePath.Before
            ? $"To name one fence, ParkOps loads all {fences.Length} fences and every one of their breaches, {total:N0} rows, " +
              "tracks them all, then picks the maximum in memory. The answer is right; the cost grows with every breach."
            : $"MaxByAsync turns the question into SQL: order fences by their breach count and take the top one. " +
              $"The database does the counting and ParkOps materializes {RowsPulled} row. Same fence, no breach rows.";
    }

    protected override void Run(CodePath path)
    {
        query = path == CodePath.Before
            ? ParkOps.ControlRoom.Panels.MaintenancePanel.RenderAsync()
            : Fixed.Inc007.MaintenancePanel.RenderAsync();
        if (fences.Length == 0)
        {
            using var db = ParkDb.Create();
            fences = [.. db.Fences.Select(f => new { f.Name, Count = f.Breaches.Count() }).AsEnumerable().Select(f => (f.Name, f.Count))];
        }
    }

    protected override bool IsBusy(TimeSpan running) =>
        Result is null || running < (Rerunning ? TimeSpan.FromMilliseconds(900) : TimeSpan.FromMilliseconds(2400));

    protected override ScreenState DrawScreen(Workstation ws, Rectangle inner, ExhibitClock clock)
    {
        var area = new Rectangle(inner.X + 1, inner.Y, 34, 7);
        var readout = Result;
        var revealFrom = clock.Started + (Rerunning ? TimeSpan.FromMilliseconds(900) : TimeSpan.FromMilliseconds(2400));
        if (clock.Busy || readout is null)
        {
            var x = ws.Chip(area.X, area.Y, "MAINTENANCE", Palette.Ink, Palette.Amber);
            ws.Print(x, area.Y, $" {Cp437.Bullet} QUERYING ", ws.Blink ? Palette.Amber : Palette.AmberDim);
            ws.Print(area.X, area.Y + 1, "Querying the maintenance database", Palette.Dim);
        }
        else ws.Readout(readout, "MAINTENANCE", "Fence with the most breaches", area, revealFrom, alertOn: true);

        DrawBreaches(ws, new Rectangle(inner.X + 38, inner.Y, inner.Width - 39, 12), readout);
        var gauge = DrawGauge(ws, new Rectangle(inner.X + 1, inner.Y + 13, inner.Width - 2, 8), clock);

        if (clock.Busy || readout is null) return new("QUERYING", ws.Blink ? Palette.Amber : Palette.AmberDim, []);
        List<Rectangle> marks = [Row(area.X, area.Y + 4, area.Width), gauge];
        return RowsPulled > 1 ? new($"{RowsPulled:N0} ROWS", Palette.BadgeCheck, marks) : ScreenState.Clear(marks);
    }

    /// <summary>Breaches per fence, from the same database; the one the panel names is lit once it answers.</summary>
    private void DrawBreaches(Workstation ws, Rectangle area, Readout? readout)
    {
        ws.Chip(area.X, area.Y, "BREACHES BY FENCE", Palette.Ink, Palette.Amber);
        if (fences.Length == 0) return;
        var most = fences.Max(f => f.Breaches);
        var named = readout?.Lines[1].Text.Trim();
        foreach (var (index, (name, breaches)) in fences.Index())
        {
            var y = area.Y + 2 + index;
            var lit = name == named;
            ws.Print(area.X, y, Workstation.Clip(name, 19), lit ? Palette.Amber : Palette.Text);
            var length = Math.Max(1, breaches * (area.Width - 25) / most);
            ws.Print(area.X + 20, y, new string(Cp437.FullBlock, length), lit ? Palette.Bad : Palette.AmberDim);
            ws.Print(area.X + 21 + length, y, breaches.ToString(), Palette.Dim);
        }
    }

    /// <summary>Rows materialized by the query, counting up while it runs: a block per 10 rows, at least one.</summary>
    private Rectangle DrawGauge(Workstation ws, Rectangle area, ExhibitClock clock)
    {
        ws.Chip(area.X, area.Y, "ROWS PULLED INTO MEMORY", Palette.Ink, Palette.Amber);
        var target = RowsPulled;
        var progress = Math.Clamp(clock.Running / (Rerunning ? TimeSpan.FromMilliseconds(800) : TimeSpan.FromMilliseconds(2200)), 0, 1);
        var shown = (int)(target * progress);
        ws.Print(area.X + 26, area.Y, $"{shown,6:N0}", shown > 1 ? Palette.Bad : Palette.Good);

        var perRow = area.Width;
        var blocks = shown == 0 ? 0 : Math.Max(1, shown / 10);
        for (var i = 0; i < blocks && i < perRow * 4; i++)
            ws.Print(area.X + i % perRow, area.Y + 2 + i / perRow, $"{Cp437.FullBlock}", shown > 1 ? Palette.Bad : Palette.Good);
        ws.Print(area.X, area.Y + 7, "one block = 10 rows tracked by the DbContext", Palette.Dim);
        return Row(area.X, area.Y + 2, Math.Min(Math.Max(blocks, 1), perRow));
    }

    [GeneratedRegex(@"memory: ([\d,]+)")]
    private static partial Regex RowsPattern();
}
