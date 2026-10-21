using SadRogue.Primitives;

namespace ParkOps.Story.Rendering;

/// <summary>Leading EDJE theme colours (the <c>--le-theme-*</c> tokens on leadingedje.com), plus the code-view theme built from them.</summary>
public static class Brand
{
    public static readonly Color Green = Hex(0x98C93D);
    public static readonly Color GreenShade = Hex(0x6FB03A);
    // Darker than the site's #6CA437 so kickers and green text on paper stay readable on a projector (about 4.6:1).
    public static readonly Color DarkGreen = Hex(0x4F7D22);
    public static readonly Color DarkGray = Hex(0x4D4E50);
    public static readonly Color Black = Hex(0x353535);
    public static readonly Color GrayShade = Hex(0x2A2A2A);
    public static readonly Color White = Hex(0xF8F8F8);
    public static readonly Color WhiteShade = Hex(0xECECEC);
    public static readonly Color TrueWhite = Hex(0xFFFFFF);
    public static readonly Color Blue = Hex(0x5C95FF);
    public static readonly Color VividBlue = Hex(0x49C6E5);
    public static readonly Color Cyan = Hex(0x54DEFD);
    public static readonly Color Red = Hex(0xDA7552);
    public static readonly Color Beige = Hex(0xC4B1AE);
    public static readonly Color Cream = Hex(0xEADEDA);

    // Report pages
    public static readonly Color Paper = White;
    public static readonly Color Ink = Black;
    // Secondary text, darkened from #7A7B7D: captions and footers have to survive a washed-out projector.
    public static readonly Color Muted = Hex(0x5C5D60);
    public static readonly Color Rule = Hex(0xD6D6D6);

    // Code view
    public static readonly Color CodeBackground = GrayShade;
    public static readonly Color CodeGutter = Black;
    // Secondary text on dark backgrounds (gutters, terminal captions, slide footers), lifted from #85878A.
    public static readonly Color LineNumber = Hex(0xA3A5A8);
    public static readonly Color CodeText = WhiteShade;
    public static readonly Color Keyword = Blue;
    public static readonly Color TypeName = VividBlue;
    public static readonly Color Method = Hex(0xF0DC9A);
    public static readonly Color StringLiteral = Green;
    // Comments carry the ▶ INC markers the story points at, so they need to read at a distance too.
    public static readonly Color Comment = Hex(0xD6C6C2);
    public static readonly Color Number = Cream;
    public static readonly Color Punctuation = Hex(0xB4B4B4);
    public static readonly Color RemovedBackground = Hex(0x5A2A1E);
    public static readonly Color AddedBackground = Hex(0x33461C);
    public static readonly Color AddedSettled = Hex(0x2F3626);
    public static readonly Color Flagged = Hex(0x4A3A1A);

    private static Color Hex(int rgb) => new((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
}
