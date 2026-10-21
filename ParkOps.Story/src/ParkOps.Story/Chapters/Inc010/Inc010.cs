using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc010;

/// <summary>
/// Lesson 10, the Microsoft.Extensions.AI failover chat client. Incident INC-010, "The narrator went silent": the tour
/// narrator has one model, and when it's unavailable the guests hear nothing.
/// </summary>
public static class Inc010
{
    private const string ProgramPath = "src/ParkOps.Narrator/Program.cs";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-010");
        var title = finding.LessonLabel;
        var before = SourceFile.Load("before/Narrator/Program.cs", ProgramPath);
        var after = SourceFile.Load("after/Inc010/Program.cs", ProgramPath);

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Tour narrator  (ParkOps.Narrator)", ProgramPath,
            [
                new("Observation",
                    "The tour vehicles' narrator greets guests at each paddock using a local language model. When that model " +
                    "isn't available, the narrator doesn't fall back to anything: it logs an exception and goes quiet.",
                    "1", "model the narrator can use"),
                new("Why it matters",
                    "Models get updated, renamed and taken down. A second model is already installed on the vehicles; nothing " +
                    "uses it.",
                    "0", "fallbacks configured"),
            ], pages.Next()),

            new NarratorExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, before,
            [
                new("IChatClient narrator = new OllamaApiClient(ollama, primaryModel);", "One model, no plan B",
                    "The narrator talks to exactly one client. backupModel is parsed from the command line, then never used.",
                    new Rectangle(28, 27, 90, 6)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(before, after, new("new OrderedFailoverChatClient", "Failover, in the client pipeline",
                    "OrderedFailoverChatClient wraps the clients in order and moves to the next when one fails. It's experimental " +
                    "(MEAI001), and it isn't tied to .NET 11: MEAI 10.11 runs on .NET 10 too.",
                    new Rectangle(28, 27, 90, 6))),
            ], pages.Next()),

            new NarratorExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () => new(
                    "Silent", "the primary model failed and the guests heard nothing.",
                    "Speaks", "the primary fails exactly as before; OrderedFailoverChatClient moves on to the backup."),
                "ParkOps.Narrator wraps the primary and backup models in an OrderedFailoverChatClient.",
                "Treat model endpoints like any other dependency: plan for them to be down. Failover belongs in the client " +
                "pipeline, not in a catch block.",
                pages.Next()),
        ]);
    }
}
