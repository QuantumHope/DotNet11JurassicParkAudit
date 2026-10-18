using ParkOps.ControlRoom.Ui;
using SadConsole;
using SadRogue.Primitives;

namespace ParkOps.Story.Rendering;

/// <summary>
/// What a beat draws on: the 128x44 cell surface, plus a transparent overlay at twice the font size
/// for big headings. Big text sits on the overlay's 64x22 grid, so it lands on even cell coordinates.
/// </summary>
public sealed class Canvas(ICellSurface surface, ICellSurface big)
{
    public const int Width = 128;
    public const int Height = 44;

    /// <summary>Row 43 is the story bar; beats draw above it.</summary>
    public const int ContentHeight = Height - 1;

    public ICellSurface Surface => surface;

    public void Clear(Color background)
    {
        surface.Fill(Color.White, background, 0);
        big.Fill(Color.White, Color.Transparent, 0);
    }

    public void Fill(Rectangle area, Color foreground, Color background, int glyph = 0) =>
        surface.Fill(area, foreground, background, glyph);

    public void Print(int x, int y, string text, Color foreground, Color? background = null)
    {
        if (y < 0 || y >= Height || x >= Width) return;
        var encoded = Glyphs.Encode(text);
        if (x < 0)
        {
            if (-x >= encoded.Length) return;
            encoded = encoded[-x..];
            x = 0;
        }
        if (x + encoded.Length > Width) encoded = encoded[..(Width - x)];
        if (background is { } bg) surface.Print(x, y, encoded, foreground, bg);
        else surface.Print(x, y, encoded, foreground);
    }

    public void Glyph(int x, int y, char glyph, Color foreground, Color? background = null)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        surface.SetGlyph(x, y, Glyphs.Encode(glyph.ToString())[0], foreground);
        if (background is { } bg) surface.SetBackground(x, y, bg);
    }

    /// <summary>Double-size text at small-grid cell (x, y); both are rounded down to even numbers.</summary>
    public void Big(int x, int y, string text, Color foreground, Color? background = null)
    {
        var bx = x / 2;
        var by = y / 2;
        var encoded = Glyphs.Encode(text);
        if (bx + encoded.Length > big.Width) encoded = encoded[..Math.Max(0, big.Width - bx)];
        big.Print(bx, by, encoded, foreground, background ?? Color.Transparent);
    }

    public static Color Lerp(Color from, Color to, double amount)
    {
        var t = Math.Clamp(amount, 0, 1);
        return new((int)(from.R + (to.R - from.R) * t), (int)(from.G + (to.G - from.G) * t), (int)(from.B + (to.B - from.B) * t));
    }
}

/// <summary>Characters the story uses beyond the control room's set, mapped onto code page 437.</summary>
public static class Glyphs
{
    public const char Wrap = (char)26;     // →, marks a soft-wrapped code line
    public const char Check = (char)251;   // √
    public const char Diamond = (char)4;
    public const char ArrowRight = (char)26;
    public const char ArrowLeft = (char)27;
    public const char ArrowDown = (char)25;
    public const char ArrowUp = (char)24;
    public const char BlockLeft = (char)221;
    public const char UpperHalf = (char)223;
    public const char DarkShade = (char)178;

    public static string Encode(string text) => Cp437.Encode(string.Create(text.Length, text, static (span, source) =>
    {
        for (var i = 0; i < source.Length; i++)
        {
            span[i] = source[i] switch
            {
                '▶' or '►' => Cp437.Play,
                '◀' => Cp437.Rewind,
                '→' => ArrowRight,
                '←' => ArrowLeft,
                '↑' => ArrowUp,
                '↓' => ArrowDown,
                '✓' => Check,
                '•' => Cp437.Bullet,
                '“' or '”' => '"',
                '‘' or '’' => '\'',
                '–' => '-',
                var c => c,
            };
        }
    }));
}
