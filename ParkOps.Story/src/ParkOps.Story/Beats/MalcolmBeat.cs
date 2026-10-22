using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>
/// The encore after the last lesson: Dr. Ian Malcolm, as an ASCII portrait, breathing. The second step types
/// in a column of Jurassic Park lines, lightly patched for the occasion.
/// </summary>
public sealed class MalcolmBeat : Beat
{
    private const int ArtX = 6, ArtY = 8, QuotesX = 74;
    private static readonly TimeSpan Breath = TimeSpan.FromMilliseconds(1100);

    // A head-and-shoulders portrait: slicked hair, dark round glasses (# is drawn as a shaded block), open jacket. The
    // shoulders rise a row on each breath in.
    private static readonly string[] Head =
    [
        @"        .-~~~~~~~~-.",
        @"      .'  ~~~~~~~~  '.",
        @"     /                \",
        @"    |   ___      ___   |",
        @"    |  (###)----(###)  |",
        @"    |   '-'   ,  '-'   |",
        @"     \       (_       /",
        @"      \    .____.    /",
        @"       '.   '--'   .'",
        @"         '-.____.-'",
    ];

    private const string Neck = @"          |      |";

    private static readonly string[] Shoulders =
    [
        @"    __.--'\      /'--.__",
        @"  .'      |\    /|      '.",
        @" /    |   | \  / |   |    \",
        @"|     |   |  \/  |   |     |",
    ];

    private static readonly (string Line, string Who)[] Quotes =
    [
        ("Bugs, uh... find a way.", "Dr. Ian Malcolm"),
        ("Your engineers were so preoccupied with whether they could ship on RC2, they didn't stop to think if they should.",
            "Dr. Ian Malcolm"),
        ("Ah ah ah, you didn't add the switch arm.", "Dennis Nedry"),
        ("Clever compiler.", "Robert Muldoon, reading CS8509"),
        ("Hold on to your stack traces.", "Ray Arnold, flipping runtime-async=on"),
        ("We spared no expense. Except on the unit tests.", "John Hammond"),
    ];

    public override string Label => "Encore";

    public override int Steps => 2;

    public override Color Backdrop => Brand.Black;

    public override void Draw(Frame frame)
    {
        var canvas = frame.Canvas;
        canvas.Clear(Brand.Black);
        Slides.Kicker(canvas, 10, 3, "One more thing", Brand.Green);
        canvas.Big(10, 4, "Life, uh, finds a way", Brand.TrueWhite);

        // Breathing: on each breath in, the shoulders rise a row over the neck.
        var inhaling = (int)(frame.Now / Breath) % 2 == 1;
        var x = ArtX + 8;
        foreach (var (i, line) in Head.Index())
            canvas.Print(x, ArtY + i, line.Replace('#', Glyphs.DarkShade), Slides.FadeIn(Brand.Cream, frame.Elapsed, 100 + 60 * i), Brand.Black);
        var shoulders = ArtY + Head.Length + (inhaling ? 0 : 1);
        if (!inhaling) canvas.Print(x, ArtY + Head.Length, Neck, Slides.FadeIn(Brand.Cream, frame.Elapsed, 700), Brand.Black);
        foreach (var (i, line) in Shoulders.Index())
            canvas.Print(x, shoulders + i, line, Slides.FadeIn(Brand.Beige, frame.Elapsed, 760 + 60 * i), Brand.Black);
        var sigh = inhaling ? "hm." : "hm. hm.";
        canvas.Print(x + 27, ArtY + 4, sigh.PadRight(8), Slides.FadeIn(Brand.LineNumber, frame.Elapsed, 900), Brand.Black);
        canvas.Print(ArtX + 1, ArtY + Head.Length + Shoulders.Length + 3, "Dr. Ian Malcolm, reviewing the ParkOps codebase.",
            Slides.FadeIn(Brand.LineNumber, frame.Elapsed, 800), Brand.Black);

        if (frame.Since(1) is var since && since >= TimeSpan.Zero)
        {
            canvas.Fill(new Rectangle(QuotesX - 2, 8, 1, 24), Brand.Green, Brand.Green);
            var y = 8;
            foreach (var (i, (line, who)) in Quotes.Index())
            {
                var shown = since - TimeSpan.FromMilliseconds(700 * i);
                if (shown < TimeSpan.Zero) break;
                var width = ReportPage.Right + 4 - QuotesX;
                Slides.Type(canvas, QuotesX, y, width, $"\"{line}\"", Brand.TrueWhite, shown, 0.2);
                y += ReportPage.WordWrap($"\"{line}\"", width).Count();
                canvas.Print(QuotesX + 2, y, $"- {who}", Slides.FadeIn(Brand.Green, shown, 400), Brand.Black);
                y += 2;
            }
        }
    }
}
