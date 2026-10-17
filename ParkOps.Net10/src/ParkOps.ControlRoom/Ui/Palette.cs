using SadRogue.Primitives;

namespace ParkOps.ControlRoom.Ui;

/// <summary>Early-90s workstation look: grey bevelled chrome around dark CRT-style screens.</summary>
public static class Palette
{
    // Chrome
    public static readonly Color Chrome = new(150, 154, 164);
    public static readonly Color ChromeLight = new(214, 217, 224);
    public static readonly Color ChromeDark = new(72, 75, 84);
    public static readonly Color MenuBar = new(232, 232, 232);
    public static readonly Color Ink = Color.Black;
    public static readonly Color Paper = new(246, 246, 240);
    public static readonly Color ButtonFace = new(196, 199, 206);

    // Screens
    public static readonly Color Background = new(8, 11, 22);
    public static readonly Color GridDot = new(28, 34, 52);
    public static readonly Color Text = new(205, 210, 220);
    public static readonly Color Dim = new(115, 125, 150);
    public static readonly Color Amber = new(255, 204, 0);
    public static readonly Color AmberDim = new(190, 140, 0);
    public static readonly Color Good = new(70, 210, 100);
    public static readonly Color Warn = new(255, 150, 30);
    public static readonly Color Bad = new(235, 50, 50);
    public static readonly Color BadDark = new(110, 20, 20);
    public static readonly Color Highlight = new(255, 232, 150);
    public static readonly Color Info = new(20, 60, 140);
    public static readonly Color Strong = Color.White;
    public static readonly Color BadgeGood = new(60, 170, 90);
    public static readonly Color BadgeCheck = new(190, 70, 50);

    public static Color For(Tone tone) => tone switch
    {
        Tone.Dim => Dim,
        Tone.Good => Good,
        Tone.Warn => Warn,
        Tone.Bad => Bad,
        Tone.Strong => Strong,
        _ => Text,
    };

    public static Color For(PanelStatus status) => status switch
    {
        PanelStatus.Alert => Bad,
        PanelStatus.Attention => Warn,
        _ => Good,
    };
}
