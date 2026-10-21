using ParkOps.Data;
using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc007;

/// <summary>
/// Lesson 7, EF Core 11 MaxByAsync + migrate --add. Incident INC-007, "Which fence fails most?": naming one fence loads
/// every breach row; and the board's Voltage column is waiting on a migration.
/// </summary>
public static class Inc007
{
    private const string PanelPath = "src/ParkOps.ControlRoom/Panels/MaintenancePanel.cs";
    private const string FencePath = "src/ParkOps.Data/Fence.cs";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-007");
        var title = finding.LessonLabel;
        var before = SourceFile.Load("before/MaintenancePanel.cs", PanelPath);
        var after = SourceFile.Load("after/Inc007/MaintenancePanel.cs", PanelPath);

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
                    "FIND REPAIR answers one question: which fence has failed most often? It gets the right answer, after a pause, " +
                    "and the operations workstation's memory climbs every time someone asks.",
                    $"{rows:N0}", "rows loaded into memory to name one fence"),
                new("Why it matters",
                    $"There are {fences} fences and {breaches:N0} breach records, and breaches only accumulate. The board also " +
                    "wants fence voltage recorded, and nobody has written the migration.",
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
                new FileFix(SourceFile.Load("before/Fence.cs", FencePath), SourceFile.Load("after/Inc007/Fence.cs", FencePath),
                    new("public int Voltage { get; set; }", "The board's column",
                        "One property. It still needs a migration before the database has the column.",
                        new Rectangle(66, 10, 52, 6), NoteLeader.Line)),
            ], pages.Next()),

            new ConsoleBeat(title, "Migrate", "One-step migration (RC2 caveat)",
                ConsoleCapture.Load("capture/Inc007/migrate-add.txt"),
                new("already exists", "Not ready on RC2",
                    "database update --add scaffolds and applies in one step, but on RC2 the migration it scaffolds recreates every " +
                    "table, so applying it fails. The two-step dotnet ef migrations add AddFenceVoltage produces the correct " +
                    "AddColumn. Use that until it's fixed.",
                    new Rectangle(28, 25, 90, 8)),
                pages.Next()),

            new MaintenanceExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () => new(
                    $"{rows:N0} rows", "fences and breaches, change-tracked in memory, to name one fence.",
                    "1 row", "the winning fence, computed by the database with MaxByAsync."),
                "MaintenancePanel asks the database with MaxByAsync. Fence gained Voltage; on RC2, add its migration with the " +
                "two-step migrations add, not database update --add.",
                "Let the database answer aggregate questions. Watch for Include + ToList feeding an in-memory Max, Count or First.",
                pages.Next()),
        ]);
    }
}
