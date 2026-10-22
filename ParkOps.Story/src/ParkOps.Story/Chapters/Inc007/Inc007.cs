using ParkOps.Story.Beats;
using ParkOps.Story.Chapters.Inc010;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc007;

/// <summary>
/// Lesson 7, runtime async. Incident INC-007, "Who turned off the fences?": the fence audit walks the live stack for its
/// caller and finds only async state-machine plumbing. Both versions run the original MaintenanceWindow.cs unchanged;
/// the only difference is the compiler switch. Whatever the audit actually captures is what this lesson reports (and it
/// marks itself PARTIAL if the fixed build still can't name the caller).
/// </summary>
public static class Inc007
{
    private const string SourcePath = "src/ParkOps.ControlRoom/Fences/MaintenanceWindow.cs";
    private const string ProjectPath = "src/ParkOps.ControlRoom/ParkOps.ControlRoom.csproj";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-007");
        var title = finding.LessonLabel;

        var before = FenceRuns.Run(CodePath.Before);
        var after = FenceRuns.Run(CodePath.After);
        var plumbing = before.Stack.Count(f => !FenceRuns.Chain.Contains(f));

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FencesBeat(finding, offline: false, "Isla Nublar's perimeter fences",
                ("The off switch",
                    "The fence controller can cut power to every paddock. When it does, it writes an audit line saying who " +
                    "asked. After an incident, that line is all anyone has to go on.", AtScript: false),
                pages.Next()),

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

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(SourceFile.Load("before/ParkOps.ControlRoom.csproj", ProjectPath), SourceFile.Load("after/Inc007/ParkOps.ControlRoom.csproj", ProjectPath),
                    new("runtime-async=on", "No C# changes",
                        "One property: the compiler emits async methods as runtime-async methods instead of state machines, and the " +
                        "runtime runs them. Stack frames become the methods themselves.",
                        new Rectangle(28, 21, 90, 6))),
            ], pages.Next()),

            new FenceExhibit(finding, CodePath.After, pages.Next()),

            new TradeoffsBeat(finding, "Before you flip the switch",
            [
                "Live stacks name the methods you wrote: the debugger, profilers, new StackTrace(), audit logs.",
                "Less overhead per await, and fewer allocations, in most async-heavy code.",
                "No code changes. One project property, and the .NET 11 libraries are already built this way.",
            ],
            [
                "It's a preview feature in .NET 11: opt-in, not yet the default for your code.",
                "net11.0 only. A library that also targets older frameworks still ships state machines there.",
                "Anything that reads stack shapes will see different frames: log parsers, tests that assert on frames, " +
                "tools that look for <Method>d__N.MoveNext.",
                "Measure first. Gains depend on the workload, and early benchmarks found some mixed paths slower.",
                "Exception stack traces already looked clean. This changes what you see while code runs, not crash logs.",
            ],
            "Source: Microsoft Learn, What's new in the .NET 11 runtime (RC2).",
            pages.Next()),

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
