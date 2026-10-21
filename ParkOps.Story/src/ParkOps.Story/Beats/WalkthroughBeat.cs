using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Code;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>
/// One stop on a walkthrough: the lines it's about (from the first containing <paramref name="From"/> through the first
/// after it containing <paramref name="Through"/>; null for none), and what to say about them.
/// </summary>
public sealed record WalkStop(string Title, string Text, string? From = null, string? Through = null);

/// <summary>
/// A whole source file on the left, read through one stop at a time on the right. Each step scrolls the file to the
/// stop's lines and highlights them, so the audience sees the real code while its flaws are discussed.
/// Earlier stops stay listed, ticked, so the argument builds up.
/// </summary>
public sealed class WalkthroughBeat(string chapter, string title, SourceFile file, IReadOnlyList<WalkStop> stops, Page page) : Beat
{
    private static readonly Rectangle Pane = new(6, 5, 80, 28);
    private const int NotesX = 90;
    private const int NotesWidth = 32;
    private static readonly TimeSpan ScrollTime = TimeSpan.FromMilliseconds(350);

    public override string Label => "Script";

    public override int Steps => stops.Count;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(title, page);
        report.Kicker(Pane.X, ReportPage.KickerRow, $"{chapter}  ·  {title}");

        var stop = stops[frame.Step];
        var focus = Range(stop);
        var area = CodeView.Window(canvas, Pane, file, "AS SHIPPED", Brand.Red);
        var visible = area.Height;

        // Scroll so the stop's lines sit a few rows from the top, easing over from where the last stop left it.
        var target = Offset(focus, visible);
        var from = frame.Step > 0 ? Offset(Range(stops[frame.Step - 1]), visible) : target;
        var t = Math.Clamp(frame.Since(frame.Step) / ScrollTime, 0, 1);
        var offset = (int)Math.Round(from + (target - from) * (1 - Math.Pow(1 - t, 3)));

        var lit = t >= 1;
        var rows = file.Lines.Select((text, i) => new CodeRow(text,
            lit && focus is { } f && i >= f.Start && i <= f.End ? RowStyle.Flagged : RowStyle.Normal, file.FirstLineNumber + i))
            .Skip(offset).ToList();
        CodeView.Draw(canvas, area, rows);

        // Lines above and below the window: say so, so the file reads as one piece.
        if (offset > 0) canvas.Print(Pane.MaxExtentX - 32, Pane.Y, $" {Glyphs.ArrowUp} {offset} lines above ", Brand.LineNumber, Brand.Black);

        DrawNotes(report, frame);
    }

    private void DrawNotes(ReportPage report, Frame frame)
    {
        var canvas = report.Canvas;
        canvas.Fill(new Rectangle(NotesX - 2, Pane.Y, 1, Pane.Height), Brand.Green, Brand.Green);
        report.Kicker(NotesX, Pane.Y, "Step by step");

        var y = Pane.Y + 2;
        foreach (var (index, stop) in stops.Index())
        {
            if (index > frame.Step) break;
            if (index < frame.Step)
            {
                // Earlier stops: one ticked line each.
                canvas.Glyph(NotesX, y, Glyphs.Check, Brand.GreenShade, Brand.Paper);
                canvas.Print(NotesX + 2, y, Workstation.Clip(stop.Title, NotesWidth - 2), Brand.Muted, Brand.Paper);
                y += 1;
                continue;
            }

            y += 1;
            canvas.Print(NotesX, y, $" {index + 1} ", Brand.TrueWhite, Brand.DarkGreen);
            var titleEnd = report.Paragraph(NotesX + 4, y, NotesWidth - 4, stop.Title, Brand.Ink);
            var reveal = (int)(Math.Max(0, frame.Since(index).TotalMilliseconds - 250) * 0.15);
            report.Paragraph(NotesX, titleEnd + 1, NotesWidth, stop.Text, Brand.DarkGray, reveal);
        }

        canvas.Print(NotesX, Pane.MaxExtentY, $"{frame.Step + 1} of {stops.Count}", Brand.Muted, Brand.Paper);
    }

    private (int Start, int End)? Range(WalkStop stop)
    {
        if (stop.From is null) return null;
        var start = file.Find(stop.From);
        var end = start;
        if (stop.Through is { } through)
        {
            while (end < file.Lines.Count && !file.Lines[end].Contains(through, StringComparison.Ordinal)) end++;
            if (end == file.Lines.Count) throw new InvalidOperationException($"'{through}' not found after '{stop.From}' in {file.Name}.");
        }
        return (start, end);
    }

    /// <summary>First source line to show: a few lines of context above the focus, never past the end of the file.</summary>
    private int Offset((int Start, int End)? focus, int visible)
    {
        if (focus is not { } f) return 0;
        var max = Math.Max(0, file.Lines.Count - visible + 2);
        var lead = f.End - f.Start + 1 >= visible - 4 ? 1 : 3;
        return Math.Clamp(f.Start - lead, 0, max);
    }
}
