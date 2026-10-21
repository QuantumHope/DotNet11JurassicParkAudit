using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc005;

/// <summary>
/// Lesson 5, runtime async. Incident INC-005, "Who turned off the fences?": the fence audit walks the live stack for its
/// caller and finds only async state-machine plumbing. Both versions run the original MaintenanceWindow.cs unchanged;
/// the only difference is the compiler switch. Whatever the audit actually captures is what this lesson reports (and it
/// marks itself PARTIAL if the fixed build still can't name the caller).
/// </summary>
public static class Inc005
{
    private const string SourcePath = "src/ParkOps.ControlRoom/Fences/MaintenanceWindow.cs";
    private const string ProjectPath = "src/ParkOps.ControlRoom/ParkOps.ControlRoom.csproj";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-005");
        var title = finding.LessonLabel;
        var source = SourceFile.Load("before/MaintenanceWindow.cs", SourcePath).Excerpt("public static class FenceController", "DisabledBy = caller is null");

        var before = FenceRuns.Run(CodePath.Before);
        var after = FenceRuns.Run(CodePath.After);
        var plumbing = before.Stack.Count(f => !FenceRuns.Chain.Contains(f));

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Fence status  (F3 SECURITY)", SourcePath,
            [
                new("Observation",
                    "The perimeter fences went down during the scheduled maintenance that runs at startup. The fence panel's audit " +
                    $"line reads \"Disabled by: {before.DisabledBy ?? "unknown"}\", over a stack that names no ParkOps code.",
                    before.DisabledBy ?? "unknown", "who the audit says cut the fence power"),
                new("Why it matters",
                    "The audit exists to answer exactly one question after an incident. It can't, because the stack it captures is " +
                    "the compiler's async plumbing, not the methods that were running.",
                    $"{plumbing} / {before.Stack.Count}", "captured frames that aren't a method in the call chain"),
            ], pages.Next()),

            new FenceExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, source,
            [
                new("var methods = new StackTrace()", "A live stack, of state machines",
                    "Each async method compiles to a state machine, so the frames on a live stack are <Method>d__N.MoveNext and " +
                    "the builder's plumbing. WhteRbt is on the stack, but only as its state machine's MoveNext.",
                    new Rectangle(28, 31, 90, 7)),
                new("!m.DeclaringType.Name.StartsWith('<')", "Only methods people wrote",
                    "The audit won't report a compiler-generated type as the culprit, so with state machines there's no frame it can " +
                    "name: unknown. It runs before the first await, while the caller is still on the stack.",
                    new Rectangle(28, 31, 90, 7)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(SourceFile.Load("before/ParkOps.ControlRoom.csproj", ProjectPath), SourceFile.Load("after/Inc005/ParkOps.ControlRoom.csproj", ProjectPath),
                    new("runtime-async=on", "No C# changes",
                        "One property: the compiler emits async methods as runtime-async methods instead of state machines, and the " +
                        "runtime runs them. Stack frames become the methods themselves.",
                        new Rectangle(28, 24, 90, 6))),
            ], pages.Next()),

            new FenceExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () =>
                {
                    var b = FenceRuns.Run(CodePath.Before);
                    var a = FenceRuns.Run(CodePath.After);
                    var partial = a.DisabledBy is null;
                    return new(b.DisabledBy ?? "unknown", $"top frame {b.Stack.FirstOrDefault()}: compiler plumbing, no caller.",
                        a.DisabledBy ?? "unknown", partial
                            ? $"top frame {a.Stack.FirstOrDefault()}: a real method name, but the caller still isn't on the stack."
                            : $"the audit names the caller: {a.DisabledBy}.",
                        partial);
                },
                "ParkOps.ControlRoom.csproj gains <Features>$(Features);runtime-async=on</Features>. MaintenanceWindow.cs is unchanged.",
                after.DisabledBy is null
                    ? "Turn on runtime async for readable stacks. To identify a caller, capture the stack before the first await, " +
                      "or pass the caller in: a live stack only holds the synchronous part of an async call."
                    : "Turn on runtime async for readable stacks, and keep audits that walk the stack before the first await.",
                pages.Next()),
        ]);
    }
}
