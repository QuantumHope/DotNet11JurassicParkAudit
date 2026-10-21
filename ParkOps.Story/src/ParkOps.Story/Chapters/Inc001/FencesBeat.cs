using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc001;

/// <summary>
/// Lesson 1's orientation page, before any code: what the perimeter fences are, drawn as a live diagram with every
/// paddock powered, and (second step) where the diagnostics script fits: it's what the control room runs to get the
/// fences back online in an outage.
/// </summary>
public sealed class FencesBeat(Finding finding, Page page) : Beat
{
    private static readonly Rectangle Diagram = new(ReportPage.Left, 8, 74, 24);
    private const int ColumnX = 86;
    private const int ColumnWidth = ReportPage.Right - ColumnX;
    private static readonly TimeSpan FenceTime = TimeSpan.FromMilliseconds(90);

    private const char DoubleTopLeft = (char)201, DoubleTopRight = (char)187, DoubleBottomLeft = (char)200, DoubleBottomRight = (char)188;

    public override string Label => "Fences";

    public override int Steps => 2;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(finding.LessonLabel, page);
        report.Kicker(ReportPage.Left, ReportPage.KickerRow, $"{finding.LessonLabel}  ·  Before we start  ·  the perimeter fences");
        report.Heading(ReportPage.Left, ReportPage.HeadingRow, "Isla Nublar's perimeter fences");

        var ws = new Workstation(canvas, frame.Now);
        var inner = ws.Screen(Diagram);
        var bar = new Rectangle(Diagram.X, Diagram.Y - 1, Diagram.Width, 1);
        ws.Print(bar.X, bar.Y, new string(' ', bar.Width), Palette.Ink, Palette.MenuBar);
        ws.Print(bar.X + 1, bar.Y, $"{Cp437.Menu} INGEN // FENCE CONTROL // PERIMETER", Palette.Ink, Palette.MenuBar);
        var online = frame.Elapsed > FenceTime * 10;
        ws.Print(bar.MaxExtentX - 9, bar.Y, online ? " ONLINE " : " ...... ", Color.White, online ? Palette.BadgeGood : Palette.ChromeDark);

        var diag = DrawDiagram(ws, canvas, inner, frame);
        DrawFacts(report, frame);

        if (frame.Since(1) is var since && since >= TimeSpan.Zero)
        {
            // Light up the script's box, then point at it from the column.
            Box(canvas, diag, "scripts/diag.cs", Palette.Amber, Palette.Background);
            var card = new Rectangle(ColumnX - 1, 20, ColumnWidth + 1, 12);
            Callout.Draw(canvas, card, "In an outage",
                "When the fences drop, the control room runs scripts/diag.cs first: what changed recently, the runtime, and " +
                "whether the fence controller answers. Getting the fences back online starts with this script.",
                new Point(diag.MaxExtentX + 1, diag.Y + 1), since);
        }
    }

    /// <summary>The paddocks inside their fences, the power path, and the control room's diagnostics. Returns the script's box.</summary>
    private static Rectangle DrawDiagram(Workstation ws, Canvas canvas, Rectangle inner, Frame frame)
    {
        // Ten paddocks, two rows of five, each ringed by a powered fence that draws in one after another.
        const int boxWidth = 13, boxHeight = 4;
        for (var i = 0; i < 10; i++)
        {
            var box = new Rectangle(inner.X + 1 + i % 5 * (boxWidth + 1), inner.Y + 1 + i / 5 * (boxHeight + 1), boxWidth, boxHeight);
            var since = frame.Elapsed - FenceTime * i;
            if (since < TimeSpan.Zero) continue;
            var color = Canvas.Lerp(Palette.Background, Palette.Good, since.TotalMilliseconds / 250);
            Fence(canvas, box, color);
            var name = i == 8 ? "RAPTORS" : $"PADDOCK {i + 1}";
            ws.Print(box.X + (boxWidth - name.Length) / 2, box.Y + 1, name, i == 8 ? Palette.Amber : Palette.Text);
            ws.Print(box.X + 3, box.Y + 2, "10,000 V", Palette.Dim);
        }

        // The power path: substation to fence controller to every paddock, with current running along it.
        var row = inner.Y + 12;
        var substation = new Rectangle(inner.X + 1, row, 14, 3);
        var controller = new Rectangle(inner.X + 23, row, 22, 3);
        Box(canvas, substation, "SUBSTATION", Palette.Text, Palette.Background);
        Box(canvas, controller, "FENCE CONTROLLER", Palette.Good, Palette.Background);
        Current(canvas, substation.MaxExtentX + 1, controller.X - 1, row + 1, frame.Now);
        Current(canvas, controller.MaxExtentX + 1, controller.MaxExtentX + 7, row + 1, frame.Now);
        ws.Print(controller.MaxExtentX + 8, row + 1, "all 10 paddocks", Palette.Good);

        // The control room and its diagnostics script, which checks on the controller.
        var bottom = inner.Y + 17;
        var room = new Rectangle(inner.X + 1, bottom, 18, 3);
        var script = new Rectangle(inner.X + 25, bottom, 19, 3);
        Box(canvas, room, "CONTROL ROOM", Palette.Text, Palette.Background);
        Box(canvas, script, "scripts/diag.cs", Palette.Dim, Palette.Background);
        for (var x = room.MaxExtentX + 1; x < script.X; x++) canvas.Glyph(x, bottom + 1, Cp437.SingleHorizontal, Palette.Dim);
        canvas.Glyph(script.X - 1, bottom + 1, Glyphs.ArrowRight, Palette.Dim);
        for (var y = controller.MaxExtentY + 1; y < script.Y; y++) canvas.Glyph(script.X + 9, y, ':', Palette.Dim);
        ws.Print(script.X + 11, script.Y - 1, "checks", Palette.Dim);
        return script;
    }

    private void DrawFacts(ReportPage report, Frame frame)
    {
        var canvas = report.Canvas;
        canvas.Fill(new Rectangle(ColumnX - 2, Diagram.Y - 1, 1, 11), Brand.Green, Brand.Green);
        report.Kicker(ColumnX, Diagram.Y - 1, "The system");
        report.Facts(ColumnX, Diagram.Y + 1,
        [
            ("Paddocks", "10, each fenced"),
            ("Fence power", "10,000 V"),
            ("Controller", "FenceController"),
            ("Watched from", "Control room CR-01"),
            ("Status", "ONLINE"),
        ], labelWidth: 14);
        report.Paragraph(ColumnX, Diagram.Y + 7, ColumnWidth,
            "If the fences go down, what's in the paddocks gets out. Every minute without power counts.",
            Brand.DarkGray, (int)(frame.Elapsed.TotalMilliseconds * 0.15));
    }

    private static void Fence(Canvas canvas, Rectangle r, Color color)
    {
        for (var x = r.X + 1; x < r.MaxExtentX; x++)
        {
            canvas.Glyph(x, r.Y, Cp437.DoubleHorizontal, color);
            canvas.Glyph(x, r.MaxExtentY, Cp437.DoubleHorizontal, color);
        }
        for (var y = r.Y + 1; y < r.MaxExtentY; y++)
        {
            canvas.Glyph(r.X, y, Cp437.DoubleVertical, color);
            canvas.Glyph(r.MaxExtentX, y, Cp437.DoubleVertical, color);
        }
        canvas.Glyph(r.X, r.Y, DoubleTopLeft, color);
        canvas.Glyph(r.MaxExtentX, r.Y, DoubleTopRight, color);
        canvas.Glyph(r.X, r.MaxExtentY, DoubleBottomLeft, color);
        canvas.Glyph(r.MaxExtentX, r.MaxExtentY, DoubleBottomRight, color);
    }

    private static void Box(Canvas canvas, Rectangle r, string label, Color color, Color background)
    {
        canvas.Fill(r, color, background);
        for (var x = r.X + 1; x < r.MaxExtentX; x++)
        {
            canvas.Glyph(x, r.Y, Cp437.SingleHorizontal, color);
            canvas.Glyph(x, r.MaxExtentY, Cp437.SingleHorizontal, color);
        }
        canvas.Glyph(r.X, r.Y + 1, Cp437.SingleVertical, color);
        canvas.Glyph(r.MaxExtentX, r.Y + 1, Cp437.SingleVertical, color);
        canvas.Glyph(r.X, r.Y, Cp437.TopLeft, color);
        canvas.Glyph(r.MaxExtentX, r.Y, Cp437.TopRight, color);
        canvas.Glyph(r.X, r.MaxExtentY, Cp437.BottomLeft, color);
        canvas.Glyph(r.MaxExtentX, r.MaxExtentY, Cp437.BottomRight, color);
        canvas.Print(r.X + (r.Width - label.Length) / 2, r.Y + 1, label, color, background);
    }

    /// <summary>A powered line from <paramref name="from"/> to <paramref name="to"/>, with a bright pulse travelling along it.</summary>
    private static void Current(Canvas canvas, int from, int to, int y, TimeSpan now)
    {
        var length = to - from + 1;
        var pulse = (int)(now.TotalMilliseconds / 70) % (length + 3);
        for (var x = from; x <= to; x++)
        {
            var glow = Math.Abs(x - from - pulse) <= 1;
            canvas.Glyph(x, y, x == to ? Glyphs.ArrowRight : Cp437.DoubleHorizontal, glow ? Palette.Amber : Palette.Good);
        }
    }
}
