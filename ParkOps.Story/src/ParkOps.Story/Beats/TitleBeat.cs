using System.Runtime.InteropServices;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>The opening slide: the talk's title and who's presenting. The second step adds the one-line hook.</summary>
public sealed class TitleBeat : Beat
{
    public override string Label => "Title";

    public override int Steps => 2;

    public override string? Hint => "HOME index";

    public override Color Backdrop => Brand.Black;

    public override void Draw(Frame frame)
    {
        var canvas = frame.Canvas;
        canvas.Clear(Brand.Black);
        new ReportPage(canvas).Wordmark(10, 6, onDark: true, big: true);

        Slides.Kicker(canvas, 10, 13, "Post-incident technical assessment  ·  Isla Nublar ParkOps", Brand.Green);
        canvas.Big(10, 16, "Unforeseen Consequences", Slides.FadeIn(Brand.TrueWhite, frame.Elapsed, 0));
        canvas.Print(10, 20, "Restoring Park Operations with .NET 11 and C# 15", Slides.FadeIn(Brand.WhiteShade, frame.Elapsed, 250));
        canvas.Fill(new Rectangle(10, 22, 40, 1), Brand.GreenShade, Brand.Black, Glyphs.UpperHalf);
        canvas.Print(10, 24, "Matthew-Hope Eland  ·  Leading EDJE", Slides.FadeIn(Brand.Beige, frame.Elapsed, 450));

        if (frame.Since(1) is var since && since >= TimeSpan.Zero)
        {
            Slides.Type(canvas, 10, 29, 80,
                "ParkOps told the control room the park was safe. It wasn't. Ten incidents, each traced to code, each fixed " +
                "by something new in .NET 11 or C# 15.", Brand.WhiteShade, since);
        }

        canvas.Print(10, 38, RuntimeInformation.FrameworkDescription, Brand.LineNumber, Brand.Black);
        canvas.Print(Canvas.Width - 46, 38, "→  begin       HOME  index of all lessons", Brand.LineNumber, Brand.Black);
    }
}
