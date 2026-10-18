using ParkOps.ControlRoom.Ui;
using SadConsole;
using SadRogue.Primitives;

namespace ParkOps.Story.Rendering;

/// <summary>Leading EDJE report chrome and typography: header band, wordmark, rules, paragraphs, stamps.</summary>
public sealed class ReportPage(Canvas canvas)
{
    public const int Left = 8;
    public const int Right = Canvas.Width - 8;
    public const int Top = 6;
    public const int FooterRow = 41;
    public const string ReportTitle = "ParkOps Post-Incident Technical Assessment";

    public Canvas Canvas => canvas;

    /// <summary>Paper, header band and footer. Returns the body area.</summary>
    public Rectangle Frame(string section, Story.Page page)
    {
        canvas.Clear(Brand.Paper);
        canvas.Fill(new Rectangle(0, 0, Canvas.Width, 3), Brand.White, Brand.Black);
        Wordmark(3, 1, onDark: true);
        var right = $"{ReportTitle}  {Cp437.SingleVertical}  {section.ToUpperInvariant()}";
        canvas.Print(Canvas.Width - right.Length - 3, 1, right, Brand.Beige, Brand.Black);
        canvas.Fill(new Rectangle(0, 3, Canvas.Width, 1), Brand.Green, Brand.Paper, Glyphs.UpperHalf);

        canvas.Fill(new Rectangle(Left, FooterRow, Right - Left, 1), Brand.Rule, Brand.Paper, Cp437.SingleHorizontal);
        canvas.Print(Left, FooterRow + 1, "Prepared for InGen Corporation by Leading EDJE  ·  Confidential", Brand.Muted, Brand.Paper);
        var pageText = $"{page.Number} / {page.Of.Total}";
        canvas.Print(Right - pageText.Length, FooterRow + 1, pageText, Brand.Muted, Brand.Paper);
        return new Rectangle(Left, Top, Right - Left, FooterRow - Top - 1);
    }

    /// <summary>LEADING EDJE, with the green edge mark.</summary>
    public void Wordmark(int x, int y, bool onDark, bool big = false)
    {
        var ink = onDark ? Brand.TrueWhite : Brand.Black;
        if (big)
        {
            canvas.Big(x, y, "LEADING", ink);
            canvas.Big(x + 16, y, "EDJE", Brand.GreenShade);
            canvas.Fill(new Rectangle(x - 2, y, 1, 2), Brand.Green, Brand.Green);
            return;
        }
        var bg = onDark ? Brand.Black : Brand.Paper;
        canvas.Glyph(x, y, Glyphs.BlockLeft, Brand.Green, bg);
        canvas.Print(x + 2, y, "LEADING", ink, bg);
        canvas.Print(x + 10, y, "EDJE", Brand.Green, bg);
    }

    public void Kicker(int x, int y, string text, Color? color = null) =>
        canvas.Print(x, y, string.Join(' ', text.ToUpperInvariant().ToCharArray()), color ?? Brand.DarkGreen, Brand.Paper);

    public void Heading(int x, int y, string text, Color? color = null) => canvas.Big(x, y, text, color ?? Brand.Ink);

    public void Rule(int y, int x = Left, int right = Right) =>
        canvas.Fill(new Rectangle(x, y, right - x, 1), Brand.Rule, Brand.Paper, Cp437.SingleHorizontal);

    /// <summary>Word-wrapped text; returns the row after the last line.</summary>
    public int Paragraph(int x, int y, int width, string text, Color? color = null, int? reveal = null)
    {
        var budget = reveal ?? int.MaxValue;
        foreach (var line in WordWrap(text, width))
        {
            if (budget <= 0) return y + 1;
            canvas.Print(x, y, line[..Math.Min(line.Length, budget)], color ?? Brand.Ink, Brand.Paper);
            budget -= line.Length + 1;
            y++;
        }
        return y;
    }

    /// <summary>A filled pill. Returns the x just past it.</summary>
    public int Pill(int x, int y, string text, Color background, Color? foreground = null)
    {
        canvas.Print(x, y, $" {text} ", foreground ?? Brand.TrueWhite, background);
        return x + text.Length + 2;
    }

    public int SeverityPill(int x, int y, Story.Severity severity) => severity switch
    {
        Story.Severity.Critical => Pill(x, y, "CRITICAL", Brand.Red),
        Story.Severity.High => Pill(x, y, "HIGH", Brand.Beige, Brand.Ink),
        Story.Severity.Medium => Pill(x, y, "MEDIUM", Brand.Blue),
        _ => Pill(x, y, "LOW", Brand.DarkGray),
    };

    /// <summary>The platform a lesson's improvement ships in: .NET 11, C# 15, EF Core 11...</summary>
    public int PlatformPill(int x, int y, string platform) => platform switch
    {
        "C# 15" => Pill(x, y, platform, Brand.VividBlue, Brand.Ink),
        var p when p.StartsWith(".NET") => Pill(x, y, platform, Brand.Blue),
        "MEAI" => Pill(x, y, platform, Brand.Beige, Brand.Ink),
        _ => Pill(x, y, platform, Brand.DarkGray),
    };

    /// <summary>Label/value pairs in a column, e.g. REPORTED BY / SEVERITY.</summary>
    public int Facts(int x, int y, IEnumerable<(string Label, string Value)> facts, int labelWidth = 16)
    {
        foreach (var (label, value) in facts)
        {
            canvas.Print(x, y, label.ToUpperInvariant(), Brand.Muted, Brand.Paper);
            canvas.Print(x + labelWidth, y, value, Brand.Ink, Brand.Paper);
            y++;
        }
        return y;
    }

    /// <summary>A rubber stamp that inks in over a moment: a thick box around big text. Pass an even x and an odd y.</summary>
    public void Stamp(int x, int y, string text, Color color, TimeSpan elapsed)
    {
        var t = elapsed.TotalMilliseconds / 220;
        if (t < 0) return;
        var box = new Rectangle(x, y, text.Length * 2 + 4, 4);
        var ink = Canvas.Lerp(Brand.Paper, color, t);
        canvas.Surface.DrawBox(box, ShapeParameters.CreateStyledBox(ICellSurface.ConnectedLineThick,
            new ColoredGlyph(ink, Brand.Paper)));
        canvas.Big(x + 2, y + 1, text, ink);
    }

    public static IEnumerable<string> WordWrap(string text, int width)
    {
        foreach (var paragraph in text.Split('\n'))
        {
            var line = "";
            foreach (var word in paragraph.Split(' '))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > width)
                {
                    yield return line;
                    line = "";
                }
                line = line.Length == 0 ? word : $"{line} {word}";
            }
            yield return line;
        }
    }
}
