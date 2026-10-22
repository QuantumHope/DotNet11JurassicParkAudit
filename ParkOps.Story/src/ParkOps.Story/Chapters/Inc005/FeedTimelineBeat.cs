using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc005;

/// <summary>
/// Lesson 5's concept, drawn as a timeline: the four sensors report a quarter-second apart. Buffered, the control room
/// gets nothing until the last one, then one array. Streamed, each contact goes out the moment it arrives. A playhead
/// sweeps the second so the wait is felt, not just read.
/// </summary>
public sealed class FeedTimelineBeat(Finding finding, string[] sensors, Page page) : Beat
{
    private const int AxisX = 28;
    private const double ColumnsPerSecond = 72;
    private static readonly TimeSpan SweepTime = TimeSpan.FromMilliseconds(2600);

    private const int SensorRow = 8, AxisRow = 12, BufferedRow = 15, StreamedRow = 20;

    private static readonly (string Title, string Text)[] Story =
    [
        ("Buffered: wait for everyone",
            "The original feed collects every contact into a list, then sends the list. The control room sees nothing until " +
            "the slowest sensor reports, then everything at once."),
        ("Streamed: send each one as it comes",
            "SerializeAsyncEnumerable writes each contact the moment the feed yields it, one JSON line per contact. The " +
            "first one is on screen after a quarter of a second."),
        ("At park scale",
            "Four sensors make it a one-second wait. The park has hundreds, reporting over minutes, and a feed that waits " +
            "for all of them isn't live at all."),
    ];

    public override string Label => "Timeline";

    public override int Steps => Story.Length;

    private static int X(double seconds) => AxisX + (int)Math.Round(seconds * ColumnsPerSecond);

    private static double Seconds(int i) => 0.25 * (i + 1);

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(finding.LessonLabel, page);
        report.Kicker(ReportPage.Left, ReportPage.KickerRow, $"{finding.LessonLabel}  ·  The concept  ·  buffered vs streamed");
        report.Heading(ReportPage.Left, ReportPage.HeadingRow, "When does the control room see a contact?");

        var step = frame.Step;
        // The playhead sweeps on entry, and again when the streamed lane appears.
        var sweepFrom = step >= 1 ? frame.Since(1) : frame.Elapsed;
        var t = Math.Clamp((sweepFrom - TimeSpan.FromMilliseconds(300)) / SweepTime, 0, 1) * 1.1;
        if (step >= 2) t = 1.1;

        // Sensors along the top, each reporting at its time.
        canvas.Print(ReportPage.Left, SensorRow + 1, "SENSORS", Brand.Ink, Brand.Paper);
        foreach (var (i, name) in sensors.Index())
        {
            var x = X(Seconds(i));
            var on = t >= Seconds(i);
            var label = name.Length > 16 ? name[..16] : name;
            canvas.Print(x - label.Length / 2, SensorRow + (i % 2), label, on ? Brand.Ink : Brand.Rule, Brand.Paper);
            canvas.Glyph(x, SensorRow + 2, Glyphs.ArrowDown, on ? Brand.DarkGreen : Brand.Rule);
        }

        // The time axis.
        Diagram.HLine(canvas, AxisX, X(1.1), AxisRow, Brand.DarkGray);
        for (var s = 0.0; s <= 1.01; s += 0.25)
        {
            canvas.Glyph(X(s), AxisRow, (char)194, Brand.DarkGray);
            var tick = s == 0 ? "0s" : $"{s:0.##}s";
            canvas.Print(X(s) - tick.Length / 2, AxisRow + 1, tick, Brand.Muted, Brand.Paper);
        }

        DrawBuffered(canvas, t);
        if (step >= 1) DrawStreamed(canvas, t);

        // The playhead.
        if (t < 1.1)
            for (var y = SensorRow + 2; y <= StreamedRow + 2; y++)
                if (y != AxisRow) canvas.Glyph(X(t), y, ':', Palette.Amber);

        var (title, text) = Story[step];
        canvas.Print(ReportPage.Left, 25, $" {step + 1} ", Brand.TrueWhite, Brand.DarkGreen);
        canvas.Print(ReportPage.Left + 4, 25, title, Brand.Ink, Brand.Paper);
        report.Paragraph(ReportPage.Left, 27, 64, text, Brand.DarkGray, (int)(Math.Max(0, frame.Since(step).TotalMilliseconds - 200) * 0.15));

        if (step >= 2)
        {
            const int x = 84;
            canvas.Fill(new Rectangle(x - 2, 25, 1, 8), Brand.Green, Brand.Green);
            report.Kicker(x, 25, "First on screen");
            report.Heading(x, 27, "1.0s", Brand.Red);
            canvas.Print(x, 29, "buffered", Brand.DarkGray, Brand.Paper);
            report.Heading(x + 18, 27, "0.25s", Brand.DarkGreen);
            canvas.Print(x + 18, 29, "streamed", Brand.DarkGray, Brand.Paper);
        }
    }

    /// <summary>Buffered: an empty wait until the last sensor, then one array with everything, staff written as {}.</summary>
    private void DrawBuffered(Canvas canvas, double t)
    {
        canvas.Print(ReportPage.Left, BufferedRow, "BUFFERED", Brand.Ink, Brand.Paper);
        canvas.Print(ReportPage.Left, BufferedRow + 1, "the original", Brand.Muted, Brand.Paper);
        var last = Seconds(sensors.Length - 1);
        var waitEnd = Math.Min(t, last);
        for (var x = AxisX; x < X(waitEnd); x++) canvas.Glyph(x, BufferedRow + 1, Cp437.LightShade, Brand.Beige);
        if (t > 0.1 && t < last) canvas.Print(AxisX + 1, BufferedRow, "nothing sent yet...", Brand.Muted, Brand.Paper);
        if (t >= last)
        {
            var block = $" [ all {sensors.Length}, at once ] ";
            canvas.Print(X(last), BufferedRow + 1, block, Brand.TrueWhite, Brand.Red);
            canvas.Print(X(last), BufferedRow + 2, "staff sent as {}", Brand.Red, Brand.Paper);
            canvas.Print(AxisX + 1, BufferedRow, "control room waits", Brand.Muted, Brand.Paper);
        }
    }

    /// <summary>Streamed: one line out per contact, at the moment it arrives.</summary>
    private void DrawStreamed(Canvas canvas, double t)
    {
        canvas.Print(ReportPage.Left, StreamedRow, "STREAMED", Brand.Ink, Brand.Paper);
        canvas.Print(ReportPage.Left, StreamedRow + 1, "with the fix", Brand.Muted, Brand.Paper);
        for (var i = 0; i < sensors.Length; i++)
        {
            if (t < Seconds(i)) break;
            canvas.Print(X(Seconds(i)) - 2, StreamedRow + 1, $" #{i + 1} ", Brand.TrueWhite, Brand.GreenShade);
        }
        if (t >= Seconds(0)) canvas.Print(AxisX + 1, StreamedRow, "one JSON line per contact, sent on arrival", Brand.DarkGreen, Brand.Paper);
    }
}
