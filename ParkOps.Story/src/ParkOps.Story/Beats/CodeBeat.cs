using ParkOps.Story.Code;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>An annotation on a code beat: which line it points at, and where its card sits.</summary>
/// <summary>
/// An annotation on a code page: which line it points at, and where its card sits. <see cref="NoteLeader.Gutter"/>
/// cards sit below the code and lead up the gutter; <see cref="NoteLeader.Line"/> cards sit to the right of the line,
/// spanning its row, and lead straight left to the end of it.
/// </summary>
public sealed record CodeNote(string Line, string Kicker, string Text, Rectangle Box, NoteLeader Leader = NoteLeader.Gutter);

public enum NoteLeader { Gutter, Line }

/// <summary>
/// The before code, as it ships: the real file, highlighted, with the guilty lines flagged. Each step shows the next note.
/// </summary>
public sealed class CodeBeat(string chapter, SourceFile file, IReadOnlyList<CodeNote> notes, Page page) : Beat
{
    public static readonly Rectangle Pane = new(6, 5, 116, 28);

    public override string Label => "Cause";

    public override int Steps => notes.Count + 1;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        report.Frame("Root cause", page);
        report.Kicker(Pane.X, ReportPage.KickerRow, $"{chapter}  ·  Root cause  ·  as shipped");

        var flagged = notes.Select(n => file.Find(n.Line)).ToHashSet();
        var flagOn = frame.Elapsed > TimeSpan.FromMilliseconds(500);
        var rows = file.Lines.Select((text, i) => new CodeRow(text,
            flagOn && flagged.Contains(i) ? RowStyle.Flagged : RowStyle.Normal, file.FirstLineNumber + i)).ToList();

        var area = CodeView.Window(frame.Canvas, Pane, file, "BEFORE", Brand.Red);
        var starts = CodeView.Draw(frame.Canvas, area, rows);

        // One note per step; the lines of every note stay flagged.
        if (frame.Step > 0)
        {
            var note = notes[frame.Step - 1];
            var line = file.Find(note.Line);
            Callout.Draw(frame.Canvas, note.Box, note.Kicker, note.Text, CodeView.Leader(area, note, starts[line], file.Lines[line]), frame.Since(frame.Step));
        }
    }
}
