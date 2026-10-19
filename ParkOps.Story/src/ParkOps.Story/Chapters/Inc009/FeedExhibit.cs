using System.Collections.Concurrent;
using System.Diagnostics;
using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Beats;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc009;

/// <summary>
/// Exhibit for lesson 9: F4 OPERATIONS' raw telemetry, streamed live by ParkOps.Start's FeedCommand (a custom converter
/// and one buffered array) or the fixed one (union serialization, one JSON line per contact). Each line is stamped
/// with when it actually arrived, so buffering versus streaming shows up as time.
/// </summary>
public sealed class FeedExhibit(Finding finding, CodePath initial, Page page) : ExhibitBeat(finding, initial, page)
{
    private ConcurrentQueue<(TimeSpan At, string Line)> received = new();
    private Task? feed;
    private int run;

    protected override string ScreenName => "CONTROL ROOM // F4 OPERATIONS";

    protected override string Heading(CodePath path) =>
        path == CodePath.Before ? "Nothing, then everything, minus the staff" : "Every contact, as it happens";

    protected override string Caption(CodePath path) =>
        "Control room, F4 OPERATIONS, STREAM FEED. Telemetry streamed live by " +
        (path == CodePath.Before ? "ParkOps.Start's FeedCommand and its hand-written converter." : "the fixed FeedCommand: unions serialize natively.");

    protected override (string Code, string From) CodeFor(CodePath path) => path == CodePath.Before
        ? ("SerializeAsync(batch)", "ParkOps.Start")
        : ("topLevelValues: true", "ParkOps.Story.Fixed");

    protected override string Reading(CodePath path)
    {
        var lines = received.ToArray();
        var first = lines.Length > 0 ? $"{lines[0].At.TotalSeconds:0.00}s" : "never";
        return path == CodePath.Before
            ? $"Nothing arrives for {first}: the feed buffers every contact, then sends one JSON array. And Robert Muldoon, " +
              "the game warden, arrives as {}: the converter has no case for staff."
            : $"The first contact arrives after {first}, and the rest follow as the sensors report them: one line each. " +
              "The union serializes as whichever case it holds, staff included, with no converter.";
    }

    protected override void Run(CodePath path)
    {
        var queue = new ConcurrentQueue<(TimeSpan At, string Line)>();
        received = queue;
        var clock = Stopwatch.StartNew();
        var thisRun = ++run;
        feed = Task.Run(async () =>
        {
            await using var stream = new LineStream(line => { if (run == thisRun) queue.Enqueue((clock.Elapsed, line)); });
            if (path == CodePath.Before) await ParkOps.ControlRoom.Feed.FeedCommand.RunAsync(stream);
            else await Fixed.Inc009.FeedCommand.RunAsync(stream);
        });
    }

    protected override bool IsBusy(TimeSpan running) => feed is { IsCompleted: false } || running < TimeSpan.FromMilliseconds(1600);

    protected override ScreenState DrawScreen(Workstation ws, Rectangle inner, ExhibitClock clock)
    {
        var area = new Rectangle(inner.X + 1, inner.Y, inner.Width - 2, inner.Height);
        var lines = received.ToArray();

        var x = ws.Chip(area.X, area.Y, "SENSOR FEED // RAW TELEMETRY", Palette.Ink, Palette.Amber);
        ws.Print(x, area.Y, clock.Busy ? $" {Cp437.Bullet} RECEIVING " : $" {Cp437.Bullet} COMPLETE ",
            clock.Busy ? (ws.Blink ? Palette.Amber : Palette.AmberDim) : Palette.Good);
        var elapsed = lines.Length > 0 && !clock.Busy ? lines[^1].At : clock.Running;
        ws.Print(area.X, area.Y + 1, $"{lines.Length} message(s)  ·  listening {elapsed.TotalSeconds:0.00}s", Palette.Dim);

        // Each message: an arrival stamp, then the JSON, wrapped.
        const int stampWidth = 8;
        var width = area.Width - stampWidth;
        var y = area.Y + 3;
        List<Rectangle> marks = [];
        foreach (var (at, line) in lines)
        {
            ws.Print(area.X, y, $"+{at.TotalSeconds:0.00}s", Palette.Amber);
            foreach (var part in Workstation.Wrap(line, width))
            {
                if (y > area.MaxExtentY) break;
                ws.Print(area.X + stampWidth, y, part, Palette.Text);
                if (IsStaff(part)) marks.Add(Row(area.X + stampWidth, y, Math.Max(1, part.Length)));
                y++;
            }
            y++;
        }
        if (lines.Length == 0) ws.Print(area.X + stampWidth, y, "waiting for data...", ws.Blink ? Palette.Dim : Palette.GridDot);

        return clock.Busy ? new("RECEIVING", ws.Blink ? Palette.Amber : Palette.AmberDim, [])
            : lines.Any(l => l.Line.Contains("{}")) ? new("DATA LOSS", Palette.Bad, marks)
            : ScreenState.Clear(marks);
    }

    /// <summary>The staff contact: an empty object from the old converter, Muldoon's record from the union.</summary>
    private static bool IsStaff(string text) => text.Contains("{}") || text.Contains("Muldoon");
}
