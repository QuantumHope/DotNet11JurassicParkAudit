using System.Runtime.InteropServices;
using ParkOps.ControlRoom.Ui;
using SadConsole;
using SadRogue.Primitives;

namespace ParkOps.Story.Rendering;

/// <summary>
/// The control room's drawing vocabulary (bevels, CRT screens, chips, buttons, the LEVEL caption, typewriter
/// readouts), ported from the original control room app so the exhibits look like it.
/// Every animation is a function of <paramref name="now"/>, as in the original.
/// </summary>
public sealed class Workstation(Canvas canvas, TimeSpan now)
{
    public const double TypewriterCharsPerMs = 2.2;
    public static readonly TimeSpan WipeTime = TimeSpan.FromMilliseconds(280);

    public static readonly (string Key, string Name)[] Tabs =
        [("F1", "OVERVIEW"), ("F2", "ANIMALS"), ("F3", "SECURITY"), ("F4", "OPERATIONS")];

    public bool Blink => now.TotalMilliseconds % 900 < 500;

    public TimeSpan Now => now;

    public void Print(int x, int y, string text, Color foreground, Color? background = null) =>
        canvas.Print(x, y, text, foreground, background ?? Palette.Background);

    public void TitleBar()
    {
        canvas.Fill(new Rectangle(0, 0, Canvas.Width, 1), Palette.Ink, Palette.MenuBar);
        Print(1, 0, $"{Cp437.Menu}  INGEN // PARK OPERATIONS CONTROL ROOM // TERMINAL CR-01", Palette.Ink, Palette.MenuBar);
        var right = $"{RuntimeInformation.FrameworkDescription}   {DateTime.Now:HH:mm:ss} ";
        Print(Canvas.Width - right.Length, 0, right, Palette.Ink, Palette.MenuBar);
        var live = Canvas.Width - right.Length - 9;
        Print(live, 0, $"{Cp437.Bullet}", Blink ? Palette.Good : Palette.MenuBar, Palette.MenuBar);
        Print(live + 2, 0, "LIVE", Palette.Ink, Palette.MenuBar);
        canvas.Surface.DrawLine(new Point(0, 1), new Point(Canvas.Width - 1, 1), Cp437.SingleHorizontal, Palette.ChromeDark, Palette.Chrome);
    }

    public void NavBar(int selected)
    {
        var x = 1;
        foreach (var (index, (key, name)) in Tabs.Index())
        {
            Button(new Rectangle(x, 2, 20, 3), $"{key}  {name}", index == selected);
            x += 21;
        }
        Button(new Rectangle(Canvas.Width - 15, 2, 14, 3), "H  HELP");
    }

    public void Button(Rectangle r, string label, bool selected = false, bool running = false)
    {
        var (face, ink) = running ? (Blink ? Palette.Amber : Palette.AmberDim, Palette.Ink)
            : selected ? (Palette.Amber, Palette.Ink)
            : (Palette.ButtonFace, Palette.Ink);
        var (lit, shade) = selected ? (Palette.ChromeDark, Color.White) : (Color.White, Palette.ChromeDark);

        canvas.Fill(r, ink, face);
        for (var x = r.X + 1; x < r.MaxExtentX; x++)
        {
            canvas.Glyph(x, r.Y, Cp437.SingleHorizontal, lit);
            canvas.Glyph(x, r.MaxExtentY, Cp437.SingleHorizontal, shade);
        }
        canvas.Glyph(r.X, r.Y + 1, Cp437.SingleVertical, lit);
        canvas.Glyph(r.MaxExtentX, r.Y + 1, Cp437.SingleVertical, shade);
        canvas.Glyph(r.X, r.Y, Cp437.TopLeft, lit);
        canvas.Glyph(r.MaxExtentX, r.Y, Cp437.TopRight, lit);
        canvas.Glyph(r.X, r.MaxExtentY, Cp437.BottomLeft, shade);
        canvas.Glyph(r.MaxExtentX, r.MaxExtentY, Cp437.BottomRight, shade);

        var text = Clip(label, r.Width - 2);
        Print(r.X + (r.Width - text.Length) / 2, r.Y + 1, text, ink, face);
        if (running) canvas.Glyph(r.MaxExtentX - 1, r.Y + 1, Spinner(110), ink, face);
    }

    public char Spinner(int msPerFrame) => @"|/-\"[(int)(now.TotalMilliseconds / msPerFrame) % 4];

    /// <summary>Raised grey panel: light top/left edge, dark bottom/right edge.</summary>
    public void Bevel(Rectangle r, bool raised = true)
    {
        var (topLeft, bottomRight) = raised ? (Palette.ChromeLight, Palette.ChromeDark) : (Palette.ChromeDark, Palette.ChromeLight);
        canvas.Fill(r, Palette.Ink, Palette.Chrome);
        for (var x = r.X + 1; x < r.MaxExtentX; x++)
        {
            canvas.Glyph(x, r.Y, Cp437.SingleHorizontal, topLeft);
            canvas.Glyph(x, r.MaxExtentY, Cp437.SingleHorizontal, bottomRight);
        }
        for (var y = r.Y + 1; y < r.MaxExtentY; y++)
        {
            canvas.Glyph(r.X, y, Cp437.SingleVertical, topLeft);
            canvas.Glyph(r.MaxExtentX, y, Cp437.SingleVertical, bottomRight);
        }
        canvas.Glyph(r.X, r.Y, Cp437.TopLeft, topLeft);
        canvas.Glyph(r.MaxExtentX, r.Y, Cp437.TopRight, topLeft);
        canvas.Glyph(r.X, r.MaxExtentY, Cp437.BottomLeft, bottomRight);
        canvas.Glyph(r.MaxExtentX, r.MaxExtentY, Cp437.BottomRight, bottomRight);
    }

    /// <summary>Sunken dark "CRT" area inside a bevel, with a faint dot grid. Returns the inner area.</summary>
    public Rectangle Screen(Rectangle r)
    {
        Bevel(r, raised: false);
        var inner = r.Expand(-1, -1);
        canvas.Fill(inner, Palette.Text, Palette.Background);
        for (var y = inner.Y; y <= inner.MaxExtentY; y++)
            for (var x = inner.X; x <= inner.MaxExtentX; x++)
                if ((x - inner.X) % 6 == 3 && (y - inner.Y) % 3 == 1) canvas.Glyph(x, y, Cp437.MiddleDot, Palette.GridDot);
        return inner;
    }

    /// <summary>An amber scanline sweeps down from <paramref name="from"/>, revealing what's beneath.</summary>
    public void Wipe(Rectangle inner, TimeSpan from)
    {
        var progress = (now - from) / WipeTime;
        if (progress >= 1) return;
        var y = inner.Y + (int)(Math.Max(0, progress) * inner.Height);
        if (y < inner.MaxExtentY) canvas.Fill(new Rectangle(inner.X, y + 1, inner.Width, inner.MaxExtentY - y), Palette.Text, Palette.Background);
        canvas.Fill(new Rectangle(inner.X, y, inner.Width, 1), Palette.Amber, Palette.Background, Cp437.LowerHalf);
    }

    public void Legend(Rectangle box, string text) => Print(box.X + 2, box.Y, $" {text} ", Palette.Ink, Palette.Chrome);

    /// <summary>Black chip with white text, like the film's panel labels. Returns the x just past it.</summary>
    public int Chip(int x, int y, string text, Color? foreground = null, Color? background = null)
    {
        Print(x, y, $" {text} ", foreground ?? Color.White, background ?? Palette.Ink);
        return x + text.Length + 2;
    }

    public void Banner(Rectangle r, string text, Color color)
    {
        canvas.Fill(r, Color.White, color);
        var spaced = string.Join(' ', text.ToCharArray());
        Print(r.X + (r.Width - spaced.Length) / 2, r.Y + 1, spaced, Color.White, color);
    }

    public void Caption(Rectangle caption, char level, string title, string view)
    {
        canvas.Fill(caption, Palette.Amber, Palette.Ink);
        canvas.Surface.DrawBox(caption, ShapeParameters.CreateStyledBox(ICellSurface.ConnectedLineThick, new ColoredGlyph(Palette.Amber, Palette.Ink)));
        foreach (var x in new[] { caption.X + 2, caption.MaxExtentX - 8 })
        {
            Print(x, caption.Y + 1, " LEVEL ", Palette.Ink, Palette.Chrome);
            Print(x, caption.Y + 2, "       ", Color.White, Palette.Ink);
            Print(x + 3, caption.Y + 2, level.ToString(), Palette.Amber, Palette.Ink);
            Print(x, caption.Y + 3, "       ", Color.White, Palette.Ink);
        }
        var upper = title.ToUpperInvariant();
        Print(caption.X + (caption.Width - upper.Length) / 2, caption.Y + 1, upper, Color.White, Palette.Ink);
        Print(caption.X + (caption.Width - view.Length) / 2, caption.Y + 3, view, Palette.Dim, Palette.Ink);
    }

    /// <summary>A label chip in the bar, then a paper field with text.</summary>
    public void Bar(Rectangle bar, string chip, string text, Color textColor, string? right = null)
    {
        Bevel(bar);
        var x = Chip(bar.X + 2, bar.Y + 1, chip) + 1;
        var width = bar.MaxExtentX - x - 1;
        Print(x, bar.Y + 1, new string(' ', width), Palette.Ink, Palette.Paper);
        Print(x + 1, bar.Y + 1, Clip(text, width - 2), textColor, Palette.Paper);
        if (right is not null) Print(bar.MaxExtentX - right.Length - 2, bar.Y + 1, right, Palette.ChromeDark, Palette.Paper);
    }

    /// <summary>
    /// Header chip with status light and description, then the lines typed out from <paramref name="revealStart"/>,
    /// exactly like the control room's panels.
    /// </summary>
    public void Readout(Readout? readout, string title, string? description, Rectangle area, TimeSpan revealStart, bool alertOn)
    {
        readout ??= new Readout(title, PanelStatus.Attention, [new("CONNECTING...", Tone.Dim)]);
        var color = readout.Status == PanelStatus.Alert && !alertOn ? Palette.BadDark : Palette.For(readout.Status);

        var x = Chip(area.X, area.Y, readout.Title, Palette.Ink, Palette.Amber);
        Print(x, area.Y, $" {Cp437.Bullet} {readout.Status.ToString().ToUpperInvariant()} ", color);
        if (description is not null) Print(area.X, area.Y + 1, Clip(description, area.Width), Palette.Dim);

        var rows = readout.Lines.SelectMany(line => Wrap(line.Text, area.Width).Select(text => (Text: text, line.Tone))).ToList();
        var top = area.Y + 2;
        var capacity = area.Height - 2;
        var budget = (int)Math.Max(0, (now - revealStart).TotalMilliseconds * TypewriterCharsPerMs);
        for (var i = 0; i < capacity && i < rows.Count && budget > 0; i++)
        {
            var (text, tone) = rows[i];
            var shown = text[..Math.Min(text.Length, budget)];
            Print(area.X, top + i, shown, Palette.For(tone));
            if (shown.Length < text.Length) canvas.Glyph(area.X + shown.Length, top + i, Cp437.FullBlock, Palette.Amber);
            budget -= Math.Max(text.Length, 1);
        }
        if (rows.Count > capacity) Print(area.MaxExtentX - 4, area.MaxExtentY, "...", Palette.Amber);
    }

    /// <summary>How long <see cref="Readout"/> takes to finish typing.</summary>
    public static TimeSpan RevealTime(Readout readout) =>
        TimeSpan.FromMilliseconds(readout.Lines.Sum(l => Math.Max(l.Text.Length, 1)) / TypewriterCharsPerMs);

    public static string Clip(string text, int width) => text.Length <= width ? text : text[..Math.Max(0, width - 1)] + "~";

    public static IEnumerable<string> Wrap(string text, int width)
    {
        if (text.Length == 0) yield return "";
        for (var i = 0; i < text.Length; i += width) yield return text.Substring(i, Math.Min(width, text.Length - i));
    }
}
