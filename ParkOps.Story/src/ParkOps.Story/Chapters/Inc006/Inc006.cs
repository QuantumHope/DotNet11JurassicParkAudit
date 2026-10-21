using System.Text.RegularExpressions;
using ParkOps.Core.Mapping;
using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc006;

/// <summary>
/// Lesson 6, labeled break + extension indexers + with(...). Incident INC-006, "Missing guests": the locator finds the
/// Murphy children in the raptor paddock, reports the zone clear, and keeps searching after it found them.
/// </summary>
public static class Inc006
{
    private const string Path = "src/ParkOps.ControlRoom/Panels/GuestLocatorPanel.cs";
    private const string ExtensionsPath = "src/ParkOps.ControlRoom/ParkMapExtensions.cs";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-006");
        var title = finding.LessonLabel;
        var before = SourceFile.Load("before/GuestLocatorPanel.cs", Path).Excerpt("public static Readout Render", "if (found is null)");
        var after = SourceFile.Load("after/Inc006/GuestLocatorPanel.cs", Path).Excerpt("public static Readout Render", "if (found is null)");
        // The root-cause page stops at the bug's last line, leaving room for the notes under it.
        var rootCause = SourceFile.Load("before/GuestLocatorPanel.cs", Path).Excerpt("public static Readout Render", "if (foundThem) continue;");
        var extensions = SourceFile.Load("after/Inc006/ParkMapExtensions.cs", ExtensionsPath);

        var map = ParkMap.Load();
        var beforeReadout = ParkOps.ControlRoom.Panels.GuestLocatorPanel.Render(map);
        var afterReadout = Fixed.Inc006.GuestLocatorPanel.Render(map);
        string Scanned(ParkOps.ControlRoom.Ui.Readout r) => Regex.Match(r.Lines[^1].Text, @"\d+").Value;
        var zone = beforeReadout.Lines.First(l => l.Text.StartsWith("Position")).Text.Split(' ')[^1];

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Guest locator  (F3 SECURITY)", Path,
            [
                new("Observation",
                    $"Two guests went missing on the tour. The locator found them, in {zone}, and reported \"{beforeReadout.Lines[^2].Text}\". " +
                    "Paddock 9 is the raptor paddock, and it's on the restricted list.",
                    beforeReadout.Lines[^2].Text, $"what the locator said about {zone}"),
                new("Why it matters",
                    "A locator that finds children in the raptor paddock and calls it safe is worse than no locator. " +
                    "It also kept scanning long after it had found them.",
                    $"{Scanned(beforeReadout)} / {map.Cells.Length}", "cells scanned, after the guests were already found"),
            ], pages.Next()),

            new GuestExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, rootCause,
            [
                new("var restricted = new HashSet", "Case-sensitive",
                    $"The restricted list is upper-case and the set uses the default, case-sensitive comparer. The sensor firmware reports " +
                    $"\"{zone}\", so the raptor paddock never matches.",
                    new Rectangle(40, 27, 78, 6)),
                new("var cell = map.Cells[y * map.Width + x];", "Hand-rolled indexing",
                    "The mapping vendor's ParkMap has no indexer, so every caller does the y * Width + x arithmetic itself. " +
                    "Get the order wrong and you search the wrong cell.",
                    new Rectangle(40, 27, 78, 6)),
                new("if (foundThem) continue;", "The search never stops",
                    "break only leaves the inner loop. The flag meant to stop the outer one says continue, which moves to the next row. " +
                    "Every remaining row gets scanned.",
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
                    beforeReadout.Lines[^2].Text, $"for guests found in {zone}, after scanning {Scanned(beforeReadout)} of {map.Cells.Length} cells.",
                    "RESTRICTED", $"\"{afterReadout.Lines[^2].Text}\", and the search stops at cell {Scanned(afterReadout)}."),
                "One method: a comparer-aware collection expression, an extension indexer for the vendor's map, and a labeled " +
                "break in place of a flag.",
                "Pass comparers wherever text from devices meets text from people. Prefer a labeled break to stop-flags in nested loops.",
                pages.Next()),
        ]);
    }
}
