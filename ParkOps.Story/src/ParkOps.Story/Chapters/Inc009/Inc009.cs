using System.Text.RegularExpressions;
using ParkOps.Core.Mapping;
using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc009;

/// <summary>
/// Lesson 9, labeled break + extension indexers + with(...). Incident INC-009, "Missing guests": the locator passes the
/// Murphy children in the raptor paddock, keeps searching, and reports the last guest it found instead: Dennis Nedry,
/// on the east road, zone clear.
/// </summary>
public static class Inc009
{
    private const string Path = "src/ParkOps.ControlRoom/Panels/GuestLocatorPanel.cs";
    private const string ExtensionsPath = "src/ParkOps.ControlRoom/ParkMapExtensions.cs";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-009");
        var title = finding.LessonLabel;
        var before = SourceFile.Load("before/GuestLocatorPanel.cs", Path).Excerpt("public static Readout Render", "if (found is null)");
        var after = SourceFile.Load("after/Inc009/GuestLocatorPanel.cs", Path).Excerpt("public static Readout Render", "if (found is null)");
        // The root-cause page stops at the bug's last line, leaving room for the notes under it.
        var rootCause = SourceFile.Load("before/GuestLocatorPanel.cs", Path).Excerpt("public static Readout Render", "if (foundThem) continue;");
        var extensions = SourceFile.Load("after/Inc009/ParkMapExtensions.cs", ExtensionsPath);

        var map = ParkMap.Load();
        var beforeReadout = ParkOps.ControlRoom.Panels.GuestLocatorPanel.Render(map);
        var afterReadout = Fixed.Inc009.GuestLocatorPanel.Render(map);
        string Scanned(ParkOps.ControlRoom.Ui.Readout r) => Regex.Match(r.Lines[^1].Text, @"\d+").Value;
        var zone = afterReadout.Lines.First(l => l.Text.StartsWith("Position")).Text.Split(' ')[^1];
        var reportedName = beforeReadout.Lines[1].Text.Trim();

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Guest locator  (F3 SECURITY)", Path,
            [
                new("Observation",
                    "Tim and Lex Murphy went missing on the tour. The locator ran, and sent the search team to the east road: " +
                    $"\"{reportedName}\", \"{beforeReadout.Lines[^2].Text}\". The children were in {zone}, the raptor paddock.",
                    reportedName, "who the locator reported instead of the children"),
                new("Why it matters",
                    "The search found the children, skipped the rest of their row (where a raptor was), and didn't stop. The " +
                    "next guest it came across overwrote the answer, so the screen was confident and wrong.",
                    $"{Scanned(beforeReadout)} / {map.Cells.Length}", "cells scanned: it never stopped"),
            ], pages.Next()),

            new GuestExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, rootCause,
            [
                new("var restricted = new HashSet", "Case-sensitive",
                    $"The restricted list is upper-case and the set uses the default, case-sensitive comparer. The sensor firmware reports " +
                    $"\"{zone}\", so even a search that stopped in the right place would call the raptor paddock clear.",
                    new Rectangle(40, 27, 78, 6)),
                new("var cell = map.Cells[y * map.Width + x];", "Hand-rolled indexing",
                    "The mapping vendor's ParkMap has no indexer, so every caller does the y * Width + x arithmetic itself. " +
                    "Get the order wrong and you search the wrong cell.",
                    new Rectangle(40, 27, 78, 6)),
                new("if (foundThem) continue;", "The search never stops",
                    "break only leaves the inner loop, skipping the rest of the row. The flag meant to stop the outer loop says " +
                    "continue, which moves to the next row, so the scan carries on and the next guest it finds overwrites found.",
                    new Rectangle(40, 27, 78, 6)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(before, after, new("break search;", "Three C# 15 features, three lines",
                    "with(...) passes the comparer to the collection expression. map[x, y] reads through an extension indexer. " +
                    "break search; leaves both loops in one move: no flag.",
                    new Rectangle(40, 27, 78, 6))),
                new FileFix(SourceFile.Empty(ExtensionsPath), extensions, new("public Cell this[int x, int y]", "Extension indexer",
                    "An extension block can now declare an indexer. ParkMap is the vendor's type and we can't change it, " +
                    "but every caller in ParkOps gets map[x, y].",
                    new Rectangle(40, 19, 78, 6))),
            ], pages.Next()),

            new GuestExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () => new(
                    reportedName, $"reported on the east road, \"{beforeReadout.Lines[^2].Text}\", after scanning all {Scanned(beforeReadout)} cells.",
                    "RESTRICTED", $"Tim and Lex in {zone}, and the search stops at cell {Scanned(afterReadout)}."),
                "One method: a comparer-aware collection expression, an extension indexer for the vendor's map, and a labeled " +
                "break in place of a flag.",
                "Pass comparers wherever text from devices meets text from people. Prefer a labeled break to stop-flags in nested loops.",
                pages.Next()),
        ]);
    }
}
