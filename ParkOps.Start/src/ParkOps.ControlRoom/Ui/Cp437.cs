namespace ParkOps.ControlRoom.Ui;

/// <summary>SadConsole's IBM font is code page 437: map the few non-ASCII characters we use onto its glyphs.</summary>
public static class Cp437
{
    public const char Smiley = (char)2;
    public const char Bullet = (char)7;
    public const char Play = (char)16;
    public const char Rewind = (char)17;
    public const char LightShade = (char)176;
    public const char MediumShade = (char)177;
    public const char LowerHalf = (char)220;
    public const char SingleVertical = (char)179;
    public const char DoubleVertical = (char)186;
    public const char TopRight = (char)191;
    public const char BottomLeft = (char)192;
    public const char SingleHorizontal = (char)196;
    public const char DoubleHorizontal = (char)205;
    public const char BottomRight = (char)217;
    public const char TopLeft = (char)218;
    public const char FullBlock = (char)219;
    public const char Menu = (char)240;
    public const char MiddleDot = (char)250;

    public static string Encode(string text) => string.Create(text.Length, text, static (span, source) =>
    {
        for (var i = 0; i < source.Length; i++) span[i] = Map(source[i]);
    });

    private static char Map(char c) => c switch
    {
        '—' or '─' => SingleHorizontal,
        '═' => DoubleHorizontal,
        '●' => Bullet,
        '·' => MiddleDot,
        < (char)256 => c, // ASCII, plus CP437 glyph codes passed through on purpose
        _ => '?',
    };
}
