using ParkOps.Story.Code;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>One file a fix touches. An empty <see cref="Before"/> is a new file; an empty <see cref="After"/> is a deletion.</summary>
public sealed record FileFix(SourceFile Before, SourceFile After, CodeNote? Note = null, string? Tab = null);

/// <summary>
/// The fix, performed on screen, one file per step (tabs across the top, like an editor): removed lines go red and are
/// struck through, then collapse; the new lines type themselves in, then settle. Both sides are the real files, diffed
/// at runtime. A file's note, if it has one, is the step after its diff.
/// </summary>
public sealed class FixBeat(string chapter, string feature, IReadOnlyList<FileFix> files, Page page) : Beat
{
    private static readonly TimeSpan TintTime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan StrikeTime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan CollapseAt = TimeSpan.FromMilliseconds(1300);
    private static readonly TimeSpan TypeFrom = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan LineGap = TimeSpan.FromMilliseconds(110);
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(800);
    private const double CharsPerMs = 0.2;

    private readonly List<DiffLine>[] diffs = [.. files.Select(f => LineDiff.Compute(f.Before.Lines, f.After.Lines))];

    /// <summary>Each step: which file, and whether it's that file's note.</summary>
    private readonly (int File, bool Note)[] plan =
        [.. files.SelectMany((f, i) => f.Note is null ? new[] { (i, false) } : [(i, false), (i, true)])];

    public FixBeat(string chapter, SourceFile before, SourceFile after, string feature, IReadOnlyList<CodeNote> notes, Page page)
        : this(chapter, feature, [new FileFix(before, after, notes.FirstOrDefault())], page) { }

    public override string Label => "Fix";

    public override int Steps => plan.Length;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame("The fix", page);
        report.Kicker(CodeBeat.Pane.X, 6, $"{chapter}  ·  The fix");

        var (index, showNote) = plan[frame.Step];
        var file = files[index];
        var diff = diffs[index];
        var t = frame.Since(Array.IndexOf(plan, (index, false)));

        var added = file.Before.IsEmpty;
        var deleted = file.After.IsEmpty;
        var collapsed = t >= CollapseAt || added;
        var typingStart = added ? TimeSpan.FromMilliseconds(300) : TypeFrom;

        // When each added line starts typing, in order.
        var addedStarts = new Dictionary<int, TimeSpan>();
        foreach (var (i, line) in diff.Index().Where(d => d.Item.Change == Change.Added))
        {
            addedStarts[i] = typingStart;
            typingStart += TimeSpan.FromMilliseconds(Math.Max(line.Text.Length, 1) / CharsPerMs) + LineGap;
        }
        var typedAll = typingStart;

        List<CodeRow> rows = [];
        List<int> rowForDiff = [.. Enumerable.Repeat(-1, diff.Count)];
        var number = collapsed ? file.After.FirstLineNumber : file.Before.FirstLineNumber;
        foreach (var (i, line) in diff.Index())
        {
            switch (line.Change)
            {
                case Change.Same:
                    rowForDiff[i] = rows.Count;
                    rows.Add(new(line.Text, RowStyle.Normal, number++));
                    break;
                case Change.Removed when !collapsed:
                    var tinted = t >= TintTime * 0.3;
                    var strike = (int)(Math.Clamp((t - TintTime) / StrikeTime, 0, 1) * line.Text.Length);
                    rowForDiff[i] = rows.Count;
                    rows.Add(new(line.Text, tinted ? RowStyle.Removed : RowStyle.Normal, number++, Strike: strike));
                    break;
                case Change.Added when t >= addedStarts[i]:
                    var typed = (int)((t - addedStarts[i]).TotalMilliseconds * CharsPerMs);
                    var settle = Math.Clamp((t - typedAll) / SettleTime, 0, 1);
                    rowForDiff[i] = rows.Count;
                    rows.Add(new(line.Text, RowStyle.Added, number++, Reveal: typed >= line.Text.Length ? int.MaxValue : typed, Settle: settle));
                    break;
            }
        }

        var (badge, badgeColor) = deleted && collapsed ? (file.Before.IsExcerpt ? "REMOVED" : "DELETED", Brand.Red)
            : added ? ("NEW FILE", Brand.GreenShade)
            : collapsed ? ("AFTER", Brand.GreenShade) : ("BEFORE", Brand.Red);
        var shown = deleted ? file.Before : file.After;
        var tabs = files.Select(f => f.Tab ?? (f.After.IsEmpty ? f.Before : f.After).Name).ToList();
        var area = CodeView.Window(canvas, CodeBeat.Pane, tabs, index, shown.Path, badge, badgeColor);
        var starts = CodeView.Draw(canvas, area, rows);
        if (deleted && collapsed)
            canvas.Print(area.X + 9, area.Y + 1, file.Before.IsExcerpt
                ? $"{file.Before.Lines.Count} lines deleted from {shown.Name}, from line {file.Before.FirstLineNumber}."
                : $"{shown.Name} is deleted. Nothing replaces it.", Brand.LineNumber, Brand.CodeBackground);

        if (t >= TypeFrom || added || deleted)
        {
            var pill = $" {feature} ";
            canvas.Print(CodeBeat.Pane.MaxExtentX - pill.Length + 1, 6, pill, Brand.TrueWhite, Brand.Blue);
        }

        if (showNote && file.Note is { } note)
        {
            var since = frame.Since(frame.Step);
            var diffIndex = diff.FindIndex(d => (deleted || d.Change != Change.Removed) && d.Text.Contains(note.Line, StringComparison.Ordinal));
            if (diffIndex < 0 && !deleted) throw new InvalidOperationException($"'{note.Line}' not found in {shown.Name}.");
            var row = diffIndex >= 0 ? rowForDiff[diffIndex] : -1;
            Callout.Draw(canvas, note.Box, note.Kicker, note.Text,
                row >= 0 && starts[row] >= 0 ? CodeView.Leader(area, note, starts[row], diff[diffIndex].Text) : [], since);
        }
    }
}
