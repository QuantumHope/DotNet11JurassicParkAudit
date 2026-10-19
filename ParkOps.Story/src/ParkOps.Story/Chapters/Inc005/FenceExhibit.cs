extern alias fencesBefore;
extern alias fencesAfter;

using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Beats;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;
using Before = fencesBefore::ParkOps.ControlRoom.Fences;
using After = fencesAfter::ParkOps.ControlRoom.Fences;

namespace ParkOps.Story.Chapters.Inc005;

/// <summary>What the fence audit recorded on one run of the maintenance routine.</summary>
public sealed record FenceAudit(bool PowerOn, string? DisabledBy, IReadOnlyList<string> Stack);

/// <summary>
/// Runs ParkOps.Start's maintenance routine, unchanged, from the assembly compiled the way ParkOps.Start compiles it, or
/// from the one compiled with runtime-async=on, and returns what the fence controller's audit captured.
/// </summary>
public static class FenceRuns
{
    public static FenceAudit Run(CodePath path)
    {
        if (path == CodePath.Before)
        {
            Before.MaintenanceWindow.RunAsync().GetAwaiter().GetResult();
            return new(Before.FenceController.PowerOn, Before.FenceController.DisabledBy, Before.FenceController.CapturedStack);
        }
        After.MaintenanceWindow.RunAsync().GetAwaiter().GetResult();
        return new(After.FenceController.PowerOn, After.FenceController.DisabledBy, After.FenceController.CapturedStack);
    }

    /// <summary>The calls that actually led to the shutdown, as written in MaintenanceWindow.cs.</summary>
    public static readonly string[] Chain = ["MaintenanceWindow.RunAsync", "WhteRbt.ExecuteAsync", "FenceController.DisableFencesAsync"];
}

/// <summary>
/// Exhibit for lesson 5: F3 SECURITY's fence status after the startup maintenance routine. Beside the audit's captured
/// stack, the call chain from the source, so it's plain which frames name a real method and whether the caller shows up.
/// </summary>
public sealed class FenceExhibit(Finding finding, CodePath initial, Page page) : ExhibitBeat(finding, initial, page)
{
    private FenceAudit audit = new(true, null, []);

    protected override string ScreenName => "CONTROL ROOM // F3 SECURITY";

    protected override string Heading(CodePath path) => audit.DisabledBy is { } who
        ? $"Fences off, by {who}"
        : path == CodePath.Before ? "Fences off, disabled by: unknown" : "Real frames, but still: unknown";

    protected override string Caption(CodePath path) =>
        "FENCE STATUS after startup maintenance. MaintenanceWindow.cs, unchanged, built " +
        (path == CodePath.Before ? "as ParkOps.Start builds it." : "with runtime-async=on.");

    protected override (string Code, string From) CodeFor(CodePath path) => path == CodePath.Before
        ? ("async state machines", "ParkOps.Start's build")
        : ("runtime-async=on", "one csproj line");

    protected override string Reading(CodePath path)
    {
        var top = audit.Stack.FirstOrDefault() ?? "(empty)";
        if (path == CodePath.Before)
        {
            var machine = audit.Stack.FirstOrDefault(f => f.StartsWith("<ExecuteAsync>"));
            return $"The audit walks the live stack for a ParkOps caller. What it finds is compiler plumbing: {top}, then the " +
                   (machine is null ? "runtime's continuation machinery. " : $"async builder, then {machine}: WhteRbt, as a state machine. ") +
                   "The audit won't name a compiler-generated type, so the panel says unknown.";
        }
        return audit.DisabledBy is { } who
            ? $"With runtime async the frames are the methods themselves, and the audit finds its caller: {who}."
            : $"With runtime async the top frame is the method itself, {top}, not a MoveNext. But the caller is still missing: " +
              "the audit captures after its first await, when only the resumed method is on the stack. Runtime async can't " +
              "fix that; capturing before the await would.";
    }

    protected override void Run(CodePath path) => audit = FenceRuns.Run(path);

    protected override ScreenState DrawScreen(Workstation ws, Rectangle inner, ExhibitClock clock)
    {
        var area = new Rectangle(inner.X + 1, inner.Y, inner.Width - 2, 13);
        Line[] lines =
        [
            audit.PowerOn ? new("PERIMETER FENCES: ON", Tone.Good) : new("PERIMETER FENCES: OFF", Tone.Bad),
            new($"Disabled by: {audit.DisabledBy ?? "unknown"}", Tone.Strong),
            new("Captured stack:", Tone.Dim),
            .. audit.Stack.Select(frame => new Line($"  at {frame}", IsChain(frame) ? Tone.Normal : Tone.Dim)),
        ];
        var readout = new Readout("FENCE STATUS", audit.PowerOn ? PanelStatus.Nominal : PanelStatus.Alert, lines);
        var revealFrom = clock.Started + (Rerunning ? TimeSpan.FromMilliseconds(900) : TimeSpan.FromMilliseconds(2600));
        if (clock.Busy)
        {
            var x = ws.Chip(area.X, area.Y, "FENCE STATUS", Palette.Ink, Palette.Amber);
            ws.Print(x, area.Y, $" {Cp437.Bullet} AUDITING ", ws.Blink ? Palette.Amber : Palette.AmberDim);
            ws.Print(area.X, area.Y + 1, "Reading the fence controller's audit", Palette.Dim);
        }
        else ws.Readout(readout, "FENCE STATUS", "Fence power, and the code path that cut it", area, revealFrom, ws.Blink);

        // The real call chain, from the source, each marked by whether the captured stack names it.
        var chainY = inner.Y + 15;
        ws.Chip(area.X, chainY, "CALL CHAIN IN THE SOURCE", Palette.Ink, Palette.Amber);
        ws.Print(area.X + 27, chainY, "does the captured stack name it?", Palette.Dim);
        foreach (var (index, method) in FenceRuns.Chain.Index())
        {
            var y = chainY + 2 + index * 2;
            var named = audit.Stack.Any(f => f == method);
            ws.Print(area.X + index * 4, y, $"{(index == 0 ? " " : Glyphs.ArrowRight)} {method}", Palette.Text);
            if (!clock.Busy) ws.Print(area.MaxExtentX - 9, y, named ? "  NAMED  " : " MISSING ", Color.White, named ? Palette.BadgeGood : Palette.Bad);
        }

        var marks = new List<Rectangle> { Row(area.X, area.Y + 3, area.Width) };
        if (audit.Stack.Count > 0) marks.Add(Row(area.X, area.Y + 5, area.Width));
        return clock.Busy ? new("AUDITING", ws.Blink ? Palette.Amber : Palette.AmberDim, [])
            : new(audit.DisabledBy is null ? "CAUSE UNKNOWN" : "CAUSE FOUND", audit.DisabledBy is null ? Palette.Bad : Palette.BadgeGood, marks);
    }

    private static bool IsChain(string frame) => FenceRuns.Chain.Contains(frame);
}
