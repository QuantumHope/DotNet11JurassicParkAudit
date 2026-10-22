using ParkOps.Core.Sensors;
using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc003;

/// <summary>Lesson 3, LINQ FullJoin. Incident INC-003, "The headcount": an inner join that can only ever count the animals it expected to find.</summary>
public static class Inc003
{
    private const string Path = "src/ParkOps.ControlRoom/Panels/CensusPanel.cs";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-003");
        var before = SourceFile.Load("before/CensusPanel.cs", Path);
        var after = SourceFile.Load("after/CensusPanel.cs", Path);

        var roster = Census.Roster();
        var tracked = SensorNetwork.Sweep();
        var located = Census.Located(roster, tracked);
        var missing = Census.Unrostered(roster, tracked).Count;

        var title = finding.LessonLabel;
        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Animal census  (F2 ANIMALS)", Path,
            [
                new("Observation",
                    $"The control room's census has reported every rostered animal located, {roster.Count} of {roster.Count}, " +
                    "every day since go-live. The census status light has never been anything but green.",
                    $"{located} / {roster.Count}", "animals located, according to ParkOps"),
                new("Why it matters",
                    "The census is how the park knows whether animals are getting out, or getting in. " +
                    "A count that can't come out wrong isn't a count. It just repeats the roster back to you.",
                    $"{tracked.Count}", "distinct signals on the motion grid, from the same sweep"),
            ], pages.Next()),

            new CensusExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, before,
            [
                new("expected.Join(", "Root cause",
                    "Join is an inner join. A sensor signal with no roster entry has nothing to match, so it's dropped " +
                    "before anything counts it. The query can only ever find animals it already expected.",
                    new Rectangle(52, 21, 62, 7)),
            ], pages.Next()),

            // The idea, drawn, before the code changes: what each kind of join keeps.
            new JoinConceptBeat(finding, roster.Count, tracked.Count, located, pages.Next()),

            new FixBeat(title, before, after, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new("expected.FullJoin(", "New in .NET 11",
                    "FullJoin keeps rows from both sides. Where there's no match, that side comes through as null, " +
                    "so a missing animal and an unknown signal both turn into rows you can report on.",
                    new Rectangle(56, 26, 60, 7)),
            ], pages.Next()),

            // The call just written, taken apart: what each argument is.
            new FullJoinAnatomyBeat(finding, pages.Next()),

            new CensusExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () =>
                {
                    var fixedCensus = Census.Render(CodePath.After, roster, tracked);
                    var beforeCensus = Census.Render(CodePath.Before, roster, tracked);
                    return new($"{located} / {roster.Count}", $"\"{beforeCensus.Lines[^1].Text}\". The light stays green whatever the motion grid sees.",
                        $"{missing} found", $"\"{fixedCensus.Lines[0].Text}\". The census goes red and lists each tag.");
                },
                "One file, one operator: expected.Join(...) became expected.FullJoin(...), with each side's missing value labelled.",
                "Reconcile against both sources. Any time a report compares two lists, ask what happens to rows that only appear on one side.",
                pages.Next()),
        ]);
    }
}
