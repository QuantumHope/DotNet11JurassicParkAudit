using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc006;

/// <summary>
/// Lesson 6, the hierarchy drawn: Dinosaur at the top, each species a box under it, coloured by what it eats. The
/// second step adds the lab's Procompsognathus and shows the feeding planner's switch sending it to the greens.
/// </summary>
public sealed class SpeciesTreeBeat(Finding finding, int compies, Page page) : Beat
{
    private const int BoxWidth = 15, Gap = 2, Top = 8;
    private static readonly int TreeX = (Canvas.Width - (7 * BoxWidth + 6 * Gap)) / 2;

    private static readonly (string Name, bool Meat)[] Species =
    [
        ("Velociraptor", true), ("T. rex", true), ("Dilophosaurus", true),
        ("Brachiosaurus", false), ("Triceratops", false), ("Gallimimus", false),
    ];

    private static readonly (string Title, string Text)[] Story =
    [
        ("One base type, many species",
            "Every animal in ParkOps is a Dinosaur. Each species is a type that inherits from it and says whether it eats " +
            "meat. The feeding plan reads the species and picks a diet."),
        ("A new species nobody planned for",
            "The genetics lab added Procompsognathus, the compy, in its own code, and marked it a plant-eater. Compies eat " +
            "meat. The feeding plan lists the meat-eaters it knows and sends anything else to greens."),
    ];

    public override string Label => "Species";

    public override int Steps => Story.Length;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(finding.LessonLabel, page);
        report.Kicker(ReportPage.Left, ReportPage.KickerRow, $"{finding.LessonLabel}  ·  How it's built  ·  the species");
        report.Heading(ReportPage.Left, ReportPage.HeadingRow, "Every species is a kind of Dinosaur");

        var step = frame.Step;
        var since = frame.Since(step);

        // The base type, and the line every species hangs from.
        var root = new Rectangle(Canvas.Width / 2 - 13, Top, 26, 4);
        Diagram.Box(canvas, root, Brand.Ink, Brand.WhiteShade, ("Dinosaur", Brand.Ink), ("abstract  ·  IsCarnivore", Brand.Muted));
        var branchY = Top + 5;
        var lastX = BoxX(step == 0 ? 5 : 6) + BoxWidth / 2;
        Diagram.VLine(canvas, Canvas.Width / 2, root.MaxExtentY + 1, branchY, Brand.DarkGray);
        Diagram.HLine(canvas, BoxX(0) + BoxWidth / 2, lastX, branchY, Brand.DarkGray);

        foreach (var (i, (name, meat)) in Species.Index())
        {
            var x = BoxX(i);
            var shown = frame.Elapsed - TimeSpan.FromMilliseconds(120 * i);
            if (step == 0 && shown < TimeSpan.Zero) continue;
            canvas.Glyph(x + BoxWidth / 2, branchY + 1, Glyphs.ArrowDown, Brand.DarkGray);
            var color = meat ? Brand.Red : Brand.GreenShade;
            Diagram.Box(canvas, new Rectangle(x, branchY + 2, BoxWidth, 5), color, Brand.Paper,
                (name, Brand.Ink), (meat ? "meat-eater" : "plant-eater", color), (meat ? "→ livestock" : "→ greens", Brand.Muted));
        }

        if (step >= 1)
        {
            // The compy: added by the lab, a meat-eater, sent to the greens.
            var x = BoxX(6);
            var fade = Diagram.Fade(since);
            var border = Canvas.Lerp(Brand.Paper, Palette.Amber, fade);
            canvas.Glyph(x + BoxWidth / 2, branchY + 1, Glyphs.ArrowDown, border);
            Diagram.Box(canvas, new Rectangle(x, branchY + 2, BoxWidth, 5), border, Canvas.Lerp(Brand.Paper, Brand.Cream, fade),
                ("Compy", Brand.Ink), ("\"plant-eater\"", Brand.Red), ("→ greens  !!", Brand.Red));
            canvas.Print(x, branchY + 8, "added by the lab", Brand.DarkGray, Brand.Paper);
        }

        DrawPlanner(report, canvas, step, since);

        var (title, text) = Story[step];
        canvas.Print(ReportPage.Left, 24, $" {step + 1} ", Brand.TrueWhite, Brand.DarkGreen);
        canvas.Print(ReportPage.Left + 4, 24, title, Brand.Ink, Brand.Paper);
        report.Paragraph(ReportPage.Left, 26, 56, text, Brand.DarkGray, (int)(Math.Max(0, since.TotalMilliseconds - 200) * 0.15));
        if (step == 1)
            canvas.Print(ReportPage.Left, 31, $"{compies} compies on the plant-eaters' diet.", Brand.Red, Brand.Paper);
    }

    private static int BoxX(int index) => TreeX + index * (BoxWidth + Gap);

    /// <summary>The feeding planner's switch, as a lookup table, with the row the compy falls into lit up.</summary>
    private static void DrawPlanner(ReportPage report, Canvas canvas, int step, TimeSpan since)
    {
        const int x = 70;
        canvas.Fill(new Rectangle(x - 2, 23, 1, 10), Brand.Green, Brand.Green);
        report.Kicker(x, 23, "The feeding plan's switch");
        (string Match, string Diet, bool Hit)[] rows =
        [
            ("Velociraptor, T. rex, Dilophosaurus", "livestock", false),
            ("_  (anything else)", "greens", step >= 1),
        ];
        foreach (var (i, (match, diet, hit)) in rows.Index())
        {
            var y = 25 + i * 2;
            var background = hit && since > TimeSpan.FromMilliseconds(500) ? Brand.Cream : Brand.Paper;
            canvas.Fill(new Rectangle(x, y, ReportPage.Right - x, 1), Brand.Ink, background);
            canvas.Print(x, y, match, hit ? Brand.Red : Brand.Ink, background);
            canvas.Print(ReportPage.Right - 12, y, $"→ {diet}", hit ? Brand.Red : Brand.DarkGray, background);
        }
        if (step >= 1 && since > TimeSpan.FromMilliseconds(500))
            canvas.Print(x, 29, "The compy isn't listed, so it lands in _.", Brand.DarkGray, Brand.Paper);
    }
}
