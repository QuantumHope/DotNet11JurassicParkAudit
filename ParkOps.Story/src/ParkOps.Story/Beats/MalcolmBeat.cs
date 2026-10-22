using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>
/// The encore after the last lesson: Dr. Ian Malcolm, reclining, shirt open, breathing in ASCII. The second step types
/// in a column of Jurassic Park lines, lightly patched for the occasion.
/// </summary>
public sealed class MalcolmBeat : Beat
{
    private const int ArtX = 6, ArtY = 8, QuotesX = 74;
    private static readonly TimeSpan Breath = TimeSpan.FromMilliseconds(1100);

    // Two frames of the same pose: chest in, chest out. Only the torso rows differ.
    private static readonly string[] Exhale =
    [
        @"            ___",
        @"         .-'~~~'-.",
        @"        /  _   _  \",
        @"       |  (o)-(o)  |",
        @"       |     >     |",
        @"        \   ---   /",
        @"         '-.___.-'",
        @"           /  |\_______________________",
        @"          /   | \ ~ ~ ~ ~ /  \          '------.______",
        @"         |    |  \ ~ ~ ~ /    \                       '--.",
        @"         |    |   \ ~ ~ /      )   ===================     )",
        @"         |    |    '---'      /                  ______.-'",
        @"     ____|____|______________/__________________/_______",
        @"    |____________________________________________________|",
    ];

    private static readonly string[] Inhale =
    [
        @"            ___",
        @"         .-'~~~'-.",
        @"        /  _   _  \",
        @"       |  (o)-(o)  |",
        @"       |     >     |",
        @"        \   ---   /",
        @"         '-.___.-'",
        @"           /  |\________________________",
        @"          /   | \ ~ ~ ~ ~ ~ /  \         '------.______",
        @"         |    |  \ ~ ~ ~ ~ /     \                      '--.",
        @"         |    |   \ ~ ~ ~ /       )  ===================     )",
        @"         |    |    '-----'       /                ______.-'",
        @"     ____|____|_______________/_________________/_______",
        @"    |____________________________________________________|",
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

        // Breathing: the frames swap on a slow clock, and the chest hair shimmers with it.
        var inhaling = (int)(frame.Now / Breath) % 2 == 1;
        var art = inhaling ? Inhale : Exhale;
        foreach (var (i, line) in art.Index())
        {
            var color = i is >= 8 and <= 11 ? Brand.Beige : Brand.Cream;
            canvas.Print(ArtX, ArtY + i, line, Slides.FadeIn(color, frame.Elapsed, 100 + 60 * i), Brand.Black);
        }
        var sigh = inhaling ? "hm." : "hm. hm.";
        canvas.Print(ArtX + 24, ArtY + 3, sigh.PadRight(8), Slides.FadeIn(Brand.LineNumber, frame.Elapsed, 900), Brand.Black);
        canvas.Print(ArtX + 1, ArtY + art.Length + 1, "Dr. Ian Malcolm, reviewing the ParkOps codebase.",
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
