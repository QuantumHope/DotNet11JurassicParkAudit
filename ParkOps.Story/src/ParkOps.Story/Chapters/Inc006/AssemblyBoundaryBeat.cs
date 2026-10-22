using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc006;

/// <summary>
/// Lesson 6, the assembly boundary drawn: Core (Dinosaur, its species, the feeding planner) and the lab's plugin, with
/// the compy deriving from Dinosaur across the line. First step, abstract: the line is open, so Core's compiler can
/// never see every species and the switch needs _. Second step, closed: the crossing is refused, the compy moves into
/// Core, and the switch can list every species.
/// </summary>
public sealed class AssemblyBoundaryBeat(Finding finding, Page page) : Beat
{
    private static readonly Rectangle Core = new(ReportPage.Left, 8, 66, 15);
    private static readonly Rectangle Plugin = new(86, 8, 35, 15);

    private static readonly (string Title, string Text)[] Story =
    [
        ("abstract: any assembly can add a species",
            "Core is compiled first, on its own. The lab's plugin is compiled later, against it. When the compiler checks " +
            "Core's switch it can't know what the lab will add, so the switch needs _ for anything else."),
        ("closed: only Core can add a species",
            "A closed type can only be derived from inside its own assembly. The lab's compy stops compiling, so it moves " +
            "into Core. Now the compiler sees every species, the switch lists them all, and a missing one is a warning."),
    ];

    public override string Label => "Boundary";

    public override int Steps => Story.Length;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(finding.LessonLabel, page);
        report.Kicker(ReportPage.Left, ReportPage.KickerRow, $"{finding.LessonLabel}  ·  How it's built  ·  two assemblies");
        report.Heading(ReportPage.Left, ReportPage.HeadingRow, "Who can add a species?");

        var step = frame.Step;
        var closed = step >= 1;
        var since = frame.Since(step);
        var fade = Diagram.Fade(since, 500);

        // The two assemblies, as labelled regions.
        Region(canvas, Core, "ParkOps.Core.dll", "the park's own code");
        Region(canvas, Plugin, "ParkOps.Genetics.Plugin.dll", "Dr. Wu's lab");

        var dinosaur = new Rectangle(Core.X + 3, Core.Y + 3, 30, 4);
        Diagram.Box(canvas, dinosaur, Brand.Ink, Brand.WhiteShade,
            ($"{(closed ? "closed" : "abstract")} record Dinosaur", closed ? Brand.DarkGreen : Brand.Ink), ("the species list", Brand.Muted));
        canvas.Print(dinosaur.X, dinosaur.MaxExtentY + 2, "Velociraptor  T. rex  Dilophosaurus", Brand.DarkGray, Brand.Paper);
        canvas.Print(dinosaur.X, dinosaur.MaxExtentY + 3, "Brachiosaurus  Triceratops  Gallimimus", Brand.DarkGray, Brand.Paper);
        if (closed && fade > 0.5)
            canvas.Print(dinosaur.X, dinosaur.MaxExtentY + 4, "Procompsognathus  (moved in)", Brand.DarkGreen, Brand.Paper);

        var planner = new Rectangle(Core.X + 43, Core.Y + 6, 21, 6);
        Diagram.Box(canvas, planner, Brand.Ink, Brand.Paper,
            ("FeedingPlanner", Brand.Ink),
            (closed ? "lists every species" : "3 meat-eaters, then", closed ? Brand.DarkGreen : Brand.DarkGray),
            (closed ? "no _ arm" : "_ => greens", closed ? Brand.DarkGreen : Brand.Red));

        // The compy, in the plugin, deriving from Dinosaur across the boundary.
        var compy = new Rectangle(Plugin.X + 3, Plugin.Y + 3, 30, 4);
        var y = dinosaur.Y + 1;
        if (!closed)
        {
            Diagram.Box(canvas, compy, Palette.Amber, Brand.Cream, ("record Procompsognathus", Brand.Ink), (": Dinosaur", Brand.DarkGray));
            Arrow(canvas, compy.X - 1, dinosaur.MaxExtentX + 1, y, Brand.DarkGray);
            canvas.Print(Core.MaxExtentX + 1, y - 1, "derives from", Brand.DarkGray, Brand.Paper);
        }
        else
        {
            var refused = Canvas.Lerp(Brand.DarkGray, Brand.Red, fade);
            Diagram.Box(canvas, compy, refused, Brand.Paper, ("record Procompsognathus", Brand.Muted), (": Dinosaur", Brand.Muted));
            Arrow(canvas, compy.X - 1, Core.MaxExtentX + 2, y, refused);
            canvas.Print(Core.MaxExtentX + 1, y, " X ", Brand.TrueWhite, refused);
            canvas.Print(Core.MaxExtentX + 1, y - 1, "error CS9382", refused, Brand.Paper);
            // The lab still breeds them; it just doesn't define them.
            canvas.Print(compy.X, compy.MaxExtentY + 2, "The lab still breeds compies,", Brand.DarkGray, Brand.Paper);
            canvas.Print(compy.X, compy.MaxExtentY + 3, "using Core's type.", Brand.DarkGray, Brand.Paper);
        }

        var (title, text) = Story[step];
        canvas.Print(ReportPage.Left, 25, $" {step + 1} ", Brand.TrueWhite, Brand.DarkGreen);
        canvas.Print(ReportPage.Left + 4, 25, title, Brand.Ink, Brand.Paper);
        report.Paragraph(ReportPage.Left, 27, ReportPage.Right - ReportPage.Left, text, Brand.DarkGray,
            (int)(Math.Max(0, since.TotalMilliseconds - 200) * 0.15));
    }

    /// <summary>An assembly: a tinted region with its name across the top.</summary>
    private static void Region(Canvas canvas, Rectangle r, string name, string note)
    {
        Diagram.Box(canvas, r, Brand.DarkGray, Brand.Paper);
        canvas.Print(r.X + 2, r.Y, $" {name} ", Brand.TrueWhite, Brand.DarkGray);
        canvas.Print(r.X + 2, r.Y + 1, note, Brand.Muted, Brand.Paper);
    }

    /// <summary>A line from <paramref name="from"/> leftwards to an arrowhead at <paramref name="to"/>.</summary>
    private static void Arrow(Canvas canvas, int from, int to, int y, Color color)
    {
        Diagram.HLine(canvas, to + 1, from, y, color);
        canvas.Glyph(to, y, Glyphs.ArrowLeft, color);
    }
}
