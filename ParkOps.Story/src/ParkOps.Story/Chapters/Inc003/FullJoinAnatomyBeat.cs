using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc003;

/// <summary>
/// The census's FullJoin call taken apart, one argument per step: the code on the left with the current argument lit,
/// a short label beside each argument once it's been covered, the longer explanation underneath, and the method's real
/// signature (checked against .NET 11 RC2) along the bottom with the matching parameter highlighted.
/// </summary>
public sealed class FullJoinAnatomyBeat(Finding finding, Page page) : Beat
{
    private static readonly Rectangle Code = new(ReportPage.Left, 7, 58, 13);
    private const int LabelX = 72;

    // Each argument: its line(s) in the call, its row, the short label, and the explanation.
    private static readonly (string[] Lines, int Row, string Label, string Text, int SignatureLine)[] Parts =
    [
        (["expected.FullJoin("], 9, "outer: the roster",
            "outer is the list you call it on: the roster, every Dinosaur the park expects to have.", 1),
        (["    tracked,"], 11, "inner: the grid's signals",
            "inner is the list to match against: every signal the motion grid picked up in the sweep.", 1),
        (["    e => e.Tag,"], 13, "outer key: each animal's tag",
            "outerKeySelector pulls the key out of each roster entry. Here, the animal's tag.", 2),
        (["    t => t.Tag,"], 15, "inner key: each signal's tag",
            "innerKeySelector pulls the key out of each signal. Rows match where the two keys are equal.", 2),
        (["    (e, t) => (e?.Tag ?? \"NOT ON ROSTER\",", "               t?.Tag ?? \"NO SIGNAL\"))"], 17,
            "result: one row per tag",
            "resultSelector runs once per row. e is null when only the grid has the tag; t is null when only the roster " +
            "has it. That's the difference from Join: you get those rows, so handle both nulls.", 3),
        ([], 0, "",
            "Two extras: an optional comparer for the keys (StringComparer.OrdinalIgnoreCase, say), and an overload with " +
            "no resultSelector that returns (Outer, Inner) tuples.", 4),
    ];

    private static readonly string[] Signature =
    [
        "IEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(",
        "    this IEnumerable<TOuter> outer, IEnumerable<TInner> inner,",
        "    Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,",
        "    Func<TOuter?, TInner?, TResult> resultSelector,",
        "    IEqualityComparer<TKey>? comparer = null)",
    ];

    public override string Label => "Anatomy";

    public override int Steps => Parts.Length;

    public override void Draw(Frame frame)
    {
        var report = new ReportPage(frame.Canvas);
        var canvas = frame.Canvas;
        report.Frame(finding.LessonLabel, page);
        report.Kicker(ReportPage.Left, ReportPage.KickerRow, $"{finding.LessonLabel}  ·  The API  ·  what FullJoin takes");
        report.Heading(ReportPage.Left, ReportPage.HeadingRow, "FullJoin, argument by argument");

        var step = frame.Step;
        var since = frame.Since(step);

        // The call, in a code pane.
        canvas.Fill(Code, Brand.CodeText, Brand.CodeBackground);
        foreach (var (i, (lines, row, label, _, _)) in Parts.Index())
        {
            if (lines.Length == 0) continue;
            var current = i == step;
            var reached = i <= step;
            foreach (var (j, line) in lines.Index())
            {
                var y = row + j;
                if (current) canvas.Fill(new Rectangle(Code.X, y, Code.Width, 1), Brand.CodeText, Brand.Flagged);
                canvas.Print(Code.X + 2, y, line, current ? Brand.TrueWhite : reached ? Brand.CodeText : Brand.LineNumber,
                    current ? Brand.Flagged : Brand.CodeBackground);
            }
            if (!reached) continue;
            // A leader from the pane to the label.
            var color = current ? Brand.DarkGreen : Brand.Muted;
            Diagram.HLine(canvas, Code.MaxExtentX + 2, LabelX - 2, row, current ? Brand.GreenShade : Brand.Rule);
            canvas.Glyph(LabelX - 1, row, Glyphs.ArrowRight, current ? Brand.GreenShade : Brand.Rule);
            canvas.Print(LabelX, row, label, color, Brand.Paper);
        }

        // The explanation for this step.
        var text = Parts[step].Text;
        canvas.Print(ReportPage.Left, 21, $" {step + 1} ", Brand.TrueWhite, Brand.DarkGreen);
        report.Paragraph(ReportPage.Left + 4, 21, ReportPage.Right - ReportPage.Left - 4, text, Brand.Ink,
            (int)(Math.Max(0, since.TotalMilliseconds - 150) * 0.2));

        // The real signature, with the parameter this step is about lit up.
        report.Kicker(ReportPage.Left, 25, "The signature  (System.Linq.Enumerable, .NET 11)");
        foreach (var (i, line) in Signature.Index())
        {
            var lit = i == Parts[step].SignatureLine;
            canvas.Print(ReportPage.Left, 27 + i, line, lit ? Brand.DarkGreen : Brand.Muted, Brand.Paper);
            if (lit) canvas.Glyph(ReportPage.Left - 2, 27 + i, Glyphs.ArrowRight, Brand.DarkGreen);
        }
    }
}
