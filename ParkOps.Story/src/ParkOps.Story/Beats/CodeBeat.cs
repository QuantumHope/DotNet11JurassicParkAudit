using ParkOps.Story.Code;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>An annotation on a code beat: which line it points at, and where its card sits.</summary>
public sealed record CodeNote(string Line, string Kicker, string Text, Rectangle Box);

/// <summary>
/// The before code, as it ships: the real file, highlighted, with the guilty line flagged. Each step adds a note.
/// </summary>
public sealed class CodeBeat(string chapter, SourceFile file, IReadOnlyList<CodeNote> notes, Page page) : Beat
{
    public static readonly Rectangle Pane = new(6, 8, 116, 32);

    public override string Label => "Root cause";

    public override int Steps => notes.Count + 1;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        report.Frame("Root cause", page);
        report.Kicker(Pane.X, 6, $"{chapter}  ·  Root cause  ·  as shipped");

        var flagged = notes.Select(n => file.Find(n.Line)).ToHashSet();
        var flagOn = frame.Elapsed > TimeSpan.FromMilliseconds(500);
        var rows = file.Lines.Select((text, i) => new CodeRow(text,
            flagOn && flagged.Contains(i) ? RowStyle.Flagged : RowStyle.Normal, file.FirstLineNumber + i)).ToList();

        var area = CodeView.Window(frame.Canvas, Pane, file, "BEFORE", Brand.Red);
        var starts = CodeView.Draw(frame.Canvas, area, rows);

        foreach (var (index, note) in notes.Index())
        {
            var line = file.Find(note.Line);
            Callout.Draw(frame.Canvas, note.Box, note.Kicker, note.Text, CodeView.GutterLeader(area, note.Box, starts[line]), frame.Since(index + 1));
        }
    }
}
