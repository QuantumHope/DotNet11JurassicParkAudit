using System.Reflection;
using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Code;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>
/// Real terminal output, captured from a real run and embedded as text (Chapters/IncNNN/Captures/*.txt):
/// <c># </c> lines describe the capture, <c>$ </c> lines are commands, everything else is output.
/// </summary>
public sealed record ConsoleCapture(string Source, IReadOnlyList<(bool Command, string Text)> Lines)
{
    public static ConsoleCapture Load(string resource)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded capture '{resource}' is missing; check the csproj.");
        var all = new StreamReader(stream).ReadToEnd().ReplaceLineEndings("\n").TrimEnd('\n').Split('\n');
        var source = string.Join(" ", all.Where(l => l.StartsWith("# ")).Select(l => l[2..]));
        return new(source, [.. all.Where(l => !l.StartsWith("# ")).Select(l => l.StartsWith("$ ") ? (true, l[2..]) : (false, l))]);
    }
}

/// <summary>
/// A report page showing captured terminal output: the command types itself, the output streams in, and an optional
/// note marks the lines that matter (its Line may list alternatives separated by '|'). Used where the story's moment happens in a build or a script, not the park.
/// </summary>
public sealed class ConsoleBeat(string chapter, string label, string title, ConsoleCapture capture, CodeNote? note, Page page) : Beat
{
    private const double TypeCharsPerMs = 0.05;
    private static readonly TimeSpan LineTime = TimeSpan.FromMilliseconds(35);
    private static readonly TimeSpan CommandPause = TimeSpan.FromMilliseconds(400);
    private static readonly Color Marked = new(70, 58, 26);

    public override string Label => label;

    public override int Steps => note is null ? 1 : 2;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(title, page);
        report.Kicker(CodeBeat.Pane.X, 6, $"{chapter}  ·  {title}");

        var pane = CodeBeat.Pane;
        canvas.Fill(new Rectangle(pane.X + 1, pane.MaxExtentY + 1, pane.Width, 1), Brand.Black, Brand.DarkGray, Cp437.LightShade);
        canvas.Fill(pane, Brand.CodeText, Brand.Black);
        canvas.Fill(new Rectangle(pane.X, pane.Y, pane.Width, 1), Brand.WhiteShade, Brand.GrayShade);
        canvas.Glyph(pane.X, pane.Y, Glyphs.BlockLeft, Brand.Green, Brand.Black);
        canvas.Print(pane.X + 1, pane.Y, " Terminal ", Brand.TrueWhite, Brand.Black);
        canvas.Print(pane.X + 13, pane.Y, Workstation.Clip($"captured: {capture.Source}", pane.Width - 15), Brand.LineNumber, Brand.GrayShade);

        // Lay out every line (wrapped), with the time it appears.
        var width = pane.Width - 4;
        var at = TimeSpan.FromMilliseconds(300);
        List<(string Text, Color Color, TimeSpan From, int Typed, bool Mark)> rows = [];
        foreach (var (command, text) in capture.Lines)
        {
            if (command)
            {
                var typeTime = TimeSpan.FromMilliseconds(text.Length / TypeCharsPerMs);
                var shown = (int)Math.Clamp((frame.Elapsed - at).TotalMilliseconds * TypeCharsPerMs, 0, text.Length);
                rows.Add(($"$ {text}", Brand.Cyan, at, shown + 2, false));
                at += typeTime + CommandPause;
                continue;
            }
            var mark = note is not null && note.Line.Split('|').Any(m => text.Contains(m, StringComparison.Ordinal));
            foreach (var part in Workstation.Wrap(text, width))
            {
                rows.Add((part, ColorFor(text), at, int.MaxValue, mark));
                at += LineTime;
            }
        }

        // While streaming, follow the newest line. Once the note is up, fit above its card and start just before the
        // first marked line, so what the note is about is on screen.
        var visible = rows.Where(r => frame.Elapsed >= r.From).ToList();
        var marking = frame.Reached(1);
        var capacity = marking && note is not null ? note.Box.Y - 1 - (pane.Y + 2) : pane.Height - 3;
        var firstMark = visible.FindIndex(r => r.Mark);
        var first = marking && firstMark >= 0
            ? Math.Clamp(firstMark - 3, 0, Math.Max(0, visible.Count - capacity))
            : Math.Max(0, visible.Count - capacity);
        var y = pane.Y + 2;
        for (var i = first; i < visible.Count && i < first + capacity; i++, y++)
        {
            var (text, color, _, typed, mark) = visible[i];
            if (marking && mark)
            {
                canvas.Fill(new Rectangle(pane.X + 1, y, pane.Width - 2, 1), color, Marked);
                canvas.Glyph(pane.X + 1, y, Cp437.Play, Palette.Amber, Marked);
            }
            var shown = text[..Math.Min(text.Length, typed)];
            canvas.Print(pane.X + 2, y, shown, color);
            if (shown.Length < text.Length || (i == visible.Count - 1 && frame.Elapsed < at && (int)(frame.Now.TotalMilliseconds / 400) % 2 == 0))
                canvas.Glyph(pane.X + 2 + shown.Length, y, Cp437.FullBlock, Brand.Green);
        }

        if (note is not null && marking)
            Callout.Draw(canvas, note.Box, note.Kicker, note.Text, [], frame.Since(1));
    }

    private static Color ColorFor(string line) =>
        line.Contains("error", StringComparison.OrdinalIgnoreCase) ? Brand.Red
        : line.Contains("warning", StringComparison.OrdinalIgnoreCase) ? Palette.Amber
        : line.Contains("succeeded", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Done") ? Brand.Green
        : Brand.CodeText;
}
