using ParkOps.Data;
using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc008;

/// <summary>
/// Lesson 8, EF Core 11 MaxByAsync. Incident INC-008, "Which fence fails most?": naming one fence loads every breach
/// row into memory.
/// </summary>
public static class Inc008
{
    private const string PanelPath = "src/ParkOps.ControlRoom/Panels/MaintenancePanel.cs";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-008");
        var title = finding.LessonLabel;
        var before = SourceFile.Load("before/MaintenancePanel.cs", PanelPath);
        var after = SourceFile.Load("after/Inc008/MaintenancePanel.cs", PanelPath);

        int fences, breaches;
        using (var db = ParkDb.Create())
        {
            fences = db.Fences.Count();
            breaches = db.Breaches.Count();
        }
        var rows = fences + breaches;

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Fence maintenance  (F4 OPERATIONS)", PanelPath,
            [
                new("Observation",
                    "The audit now names who cut the power. The next question is which fence to fix first: which one has failed " +
                    "most often? FIND REPAIR gets the right answer, after a pause, and the workstation's memory climbs every time.",
                    $"{rows:N0}", "rows loaded into memory to name one fence"),
                new("Why it matters",
                    $"There are {fences} fences and {breaches:N0} breach records, and breaches only accumulate. The answer gets " +
                    "slower, and the workstation heavier, every day the park is open.",
                    "1", "fence the question is actually about"),
            ], pages.Next()),

            new MaintenanceExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, before,
            [
                new("var fences = await db.Fences.Include(f => f.Breaches).ToListAsync();", "The database is a list",
                    "Include pulls every breach for every fence and ToListAsync materializes the lot, change-tracked, before " +
                    "MaxBy runs in memory. The database could have answered with one row.",
                    new Rectangle(28, 25, 90, 7)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(before, after, new("MaxByAsync", "New in EF Core 11",
                    "MaxByAsync is translated to SQL: ORDER BY the breach count, take one. Only the winning fence comes back.",
                    new Rectangle(28, 25, 90, 6))),
            ], pages.Next()),

            new MaintenanceExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () => new(
                    $"{rows:N0} rows", "fences and breaches, change-tracked in memory, to name one fence.",
                    "1 row", "the winning fence, computed by the database with MaxByAsync."),
                "MaintenancePanel asks the database with MaxByAsync instead of loading every fence and breach.",
                "Let the database answer aggregate questions. Watch for Include + ToList feeding an in-memory Max, Count or First.",
                pages.Next()),
        ]);
    }
}
