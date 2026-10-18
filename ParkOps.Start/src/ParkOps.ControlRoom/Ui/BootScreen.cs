using System.Runtime.InteropServices;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace ParkOps.ControlRoom.Ui;

/// <summary>Animated boot: the logo scans in, each subsystem fills a progress bar, then a blinking prompt. Any key skips.</summary>
public sealed class BootScreen(Func<IScreenObject> next) : ScreenSurface(ControlRoomScreen.ScreenWidth, ControlRoomScreen.ScreenHeight)
{
    private static readonly string[] Logo =
    [
        @" ____       _      ____    _  __     ___    ____    ____ ",
        @"|  _ \     / \    |  _ \  | |/ /    / _ \  |  _ \  / ___|",
        @"| |_) |   / _ \   | |_) | | ' /    | | | | | |_) | \___ \",
        @"|  __/   / ___ \  |  _ <  | . \    | |_| | |  __/   ___) |",
        @"|_|     /_/   \_\ |_| \_\ |_|\_\    \___/  |_|     |____/ ",
    ];

    private static readonly string[] Steps =
    [
        "Connecting to perimeter fence controllers",
        "Polling motion sensor grid",
        "Reconciling animal census",
        "Loading paddock telemetry",
        "Running scheduled maintenance",
    ];

    private static readonly TimeSpan LogoTime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan StepTime = TimeSpan.FromMilliseconds(420);
    private static readonly TimeSpan HoldTime = TimeSpan.FromMilliseconds(1300);
    private const int BarWidth = 24;

    private TimeSpan elapsed;
    private bool finished;

    public override void Update(TimeSpan delta)
    {
        elapsed += delta;
        Draw();
        if (elapsed >= LogoTime + StepTime * Steps.Length + HoldTime) Finish();
        base.Update(delta);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.KeysPressed.Count > 0) Finish();
        return true;
    }

    private void Draw()
    {
        Surface.Fill(Palette.Text, Palette.Background, 0);
        const int left = 14;

        // Logo scans in line by line.
        var logoRows = (int)Math.Min(Logo.Length, elapsed / LogoTime * Logo.Length);
        for (var i = 0; i < logoRows; i++) Surface.Print(left, 5 + i, Logo[i], Palette.Amber, Palette.Background);
        if (elapsed < LogoTime) return;

        Surface.Print(left, 11, " INGEN // ISLA NUBLAR // CONTROL ROOM ", Color.Black, Palette.Amber);
        Surface.Print(left, 13, Cp437.Encode($"SYSTEM SECURITY INTERFACE  -  {RuntimeInformation.FrameworkDescription}"), Palette.Dim, Palette.Background);

        var sinceSteps = elapsed - LogoTime;
        for (var i = 0; i < Steps.Length; i++)
        {
            var progress = Math.Clamp((sinceSteps - StepTime * i) / StepTime, 0, 1);
            if (progress <= 0) break;

            var y = 16 + i * 2;
            Surface.Print(left, y, Steps[i].PadRight(44, '.'), Palette.Text, Palette.Background);
            var filled = (int)(progress * BarWidth);
            Surface.Print(left + 45, y, new string(Cp437.FullBlock, filled), Palette.Amber, Palette.Background);
            Surface.Print(left + 45 + filled, y, new string(Cp437.LightShade, BarWidth - filled), Palette.GridDot, Palette.Background);
            if (progress >= 1) Surface.Print(left + 46 + BarWidth, y, "OK", Palette.Good, Palette.Background);
        }

        if (sinceSteps >= StepTime * Steps.Length)
        {
            Surface.Print(left, 28, "Welcome, dnedry.", Palette.Amber, Palette.Background);
            var cursorOn = elapsed.TotalMilliseconds % 600 < 320;
            Surface.Print(left, 30, "READY> ", Palette.Good, Palette.Background);
            if (cursorOn) Surface.SetGlyph(left + 7, 30, Cp437.FullBlock, Palette.Good);
        }

        Surface.Print(left, ControlRoomScreen.ScreenHeight - 3, "press any key to skip", Palette.Dim, Palette.Background);
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;
        var screen = next();
        Game.Instance.Screen = screen;
        screen.IsFocused = true;
    }
}
