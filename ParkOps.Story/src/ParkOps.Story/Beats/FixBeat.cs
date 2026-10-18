using ParkOps.Story.Code;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Beats;

/// <summary>
/// The fix, performed on screen: removed lines go red and are struck through, then collapse; the new lines
/// type themselves in, then settle. Both files are the real ones, diffed at runtime. Notes follow as steps.
/// </summary>
public sealed class FixBeat(string chapter, SourceFile before, SourceFile after, string feature, IReadOnlyList<CodeNote> notes, Page page) : Beat
{
    private static readonly TimeSpan TintTime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan StrikeTime = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan CollapseAt = TimeSpan.FromMilliseconds(1300);
    private static readonly TimeSpan TypeFrom = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan LineGap = TimeSpan.FromMilliseconds(110);
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(800);
    private const double CharsPerMs = 0.2;

    private readonly List<DiffLine> diff = LineDiff.Compute(before.Lines, after.Lines);

    public override string Label => "Fix";

    public override int Steps => notes.Count + 1;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame("The fix", page);
        report.Kicker(CodeBeat.Pane.X, 6, $"{chapter}  ·  The fix");

        var t = frame.Elapsed;
        var collapsed = t >= CollapseAt;
        var typingStart = TypeFrom;

        // When each added line starts typing, in order.
        var addedStarts = new Dictionary<int, TimeSpan>();
        foreach (var (index, line) in diff.Index().Where(d => d.Item.Change == Change.Added))
        {
            addedStarts[index] = typingStart;
            typingStart += TimeSpan.FromMilliseconds(Math.Max(line.Text.Length, 1) / CharsPerMs) + LineGap;
        }
        var typedAll = typingStart;

        List<CodeRow> rows = [];
        List<int> rowForDiff = [.. Enumerable.Repeat(-1, diff.Count)];
        var number = collapsed ? after.FirstLineNumber : before.FirstLineNumber;
        foreach (var (index, line) in diff.Index())
        {
            switch (line.Change)
            {
                case Change.Same:
                    rowForDiff[index] = rows.Count;
                    rows.Add(new(line.Text, RowStyle.Normal, number++));
                    break;
                case Change.Removed when !collapsed:
                    var tinted = t >= TintTime * 0.3;
                    var strike = (int)(Math.Clamp((t - TintTime) / StrikeTime, 0, 1) * line.Text.Length);
                    rowForDiff[index] = rows.Count;
                    rows.Add(new(line.Text, tinted ? RowStyle.Removed : RowStyle.Normal, number++, Strike: strike));
                    break;
                case Change.Added when t >= addedStarts[index]:
                    var typed = (int)((t - addedStarts[index]).TotalMilliseconds * CharsPerMs);
                    var settle = Math.Clamp((t - typedAll) / SettleTime, 0, 1);
                    rowForDiff[index] = rows.Count;
                    rows.Add(new(line.Text, RowStyle.Added, number++, Reveal: typed >= line.Text.Length ? int.MaxValue : typed, Settle: settle));
                    break;
            }
        }

        var (badge, badgeColor) = collapsed ? ("AFTER", Brand.GreenShade) : ("BEFORE", Brand.Red);
        var area = CodeView.Window(canvas, CodeBeat.Pane, collapsed ? after : before, badge, badgeColor);
        var starts = CodeView.Draw(canvas, area, rows);

        if (t >= TypeFrom)
        {
            var pill = $" {feature} ";
            canvas.Print(CodeBeat.Pane.MaxExtentX - pill.Length + 1, 6, pill, Brand.TrueWhite, Brand.Blue);
        }

        foreach (var (index, note) in notes.Index())
        {
            var diffIndex = diff.FindIndex(d => d.Change != Change.Removed && d.Text.Contains(note.Line, StringComparison.Ordinal));
            if (diffIndex < 0) throw new InvalidOperationException($"'{note.Line}' not found in {after.Name}.");
            var row = rowForDiff[diffIndex];
            if (row < 0) continue;
            Callout.Draw(canvas, note.Box, note.Kicker, note.Text, CodeView.GutterLeader(area, note.Box, starts[row]), frame.Since(index + 1));
        }
    }
}
