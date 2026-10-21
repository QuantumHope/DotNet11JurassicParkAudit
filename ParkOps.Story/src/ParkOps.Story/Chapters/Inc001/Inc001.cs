using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc001;

/// <summary>
/// Lesson 1, file-based apps: #:include + Process.RunAndCaptureTextAsync. Incident INC-001, "Who touched the system?":
/// the diagnostics script is one file of process plumbing only dnedry understood. Before and after are captures of
/// both scripts really running, from the original code in this repo; the output is the same, the script isn't.
/// </summary>
public static class Inc001
{
    private const string ScriptPath = "scripts/diag.cs";
    private const string HelpersPath = "scripts/diag-helpers.cs";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-001");
        var title = finding.LessonLabel;
        var before = SourceFile.Load("before/diag.cs", ScriptPath);
        var after = SourceFile.Load("after/Inc001/diag.cs", ScriptPath);
        var helpers = SourceFile.Load("after/Inc001/diag-helpers.cs", HelpersPath);
        var plumbing = before.Excerpt("static async Task<string> RunAndCaptureAsync", "return process.ExitCode");
        var total = before.Lines.Count + before.FirstLineNumber - 1;

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Diagnostics script  (scripts/diag.cs)", ScriptPath,
            [
                new("Observation",
                    "When the fences went down, the team needed the diagnostics: recent commits and the runtime's state. The script " +
                    "works, but only dnedry had ever changed it, and nobody else wanted to.",
                    $"{total}", "lines in one file-based script"),
                new("Why it matters",
                    "Most of the script is plumbing: starting git, wiring up two output streams, waiting, picking stdout or stderr. " +
                    "And its helpers can't move to their own file, because a file-based app was one file.",
                    $"{plumbing.Lines.Count + 1}", "of those lines are one hand-rolled 'run a process' helper"),
            ], pages.Next()),

            new ConsoleBeat(title, "Before", "The diagnostic, as shipped",
                ConsoleCapture.Load("capture/Inc001/diag-before.txt"), null, pages.Next()),

            new CodeBeat(title, plumbing,
            [
                new("var startInfo = new ProcessStartInfo(fileName)", "Plumbing nobody else could maintain",
                    "Redirect both streams, collect them through events, start, begin both reads, wait, then pick stdout or stderr " +
                    "by exit code. Every line is easy to get subtly wrong, and this is just to read git log.",
                    new Rectangle(28, 32, 90, 6)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(before.Excerpt("#!/usr/bin/env dotnet", "Diagnostics.PrintRuntimeInfo();"), after,
                    new("Process.RunAndCaptureTextAsync", "New in .NET 11",
                        "One call starts the process, captures both streams and waits. The result has StandardOutput, StandardError " +
                        "and ExitCode. #:include brings in a second file.",
                        new Rectangle(28, 26, 90, 6)), Tab: "diag.cs"),
                new FileFix(before.From("static async Task<string> RunAndCaptureAsync"), SourceFile.Empty(ScriptPath), Tab: "diag.cs (helpers)"),
                new FileFix(SourceFile.Empty(HelpersPath), helpers,
                    new("static class Diagnostics", "#:include",
                        "A file-based app can now span files: #:include diag-helpers.cs compiles this one in. The class is unchanged; it " +
                        "just has its own file.",
                        new Rectangle(28, 30, 90, 6))),
            ], pages.Next()),

            new ConsoleBeat(title, "After", "Same diagnostic, a fraction of the code",
                ConsoleCapture.Load("capture/Inc001/diag-after.txt"),
                new("Fence controller", "Identical output",
                    $"Same commits, same runtime table. The script went from {total} lines in one file to {after.Lines.Count} plus a " +
                    $"{helpers.Lines.Count}-line helpers file, and the part only dnedry understood is gone.",
                    new Rectangle(28, 32, 90, 6)),
                pages.Next()),

            new ResolutionBeat(finding, () => new(
                    $"{total} lines", $"one file, {plumbing.Lines.Count + 1} of them a hand-rolled process runner.",
                    $"{after.Lines.Count} + {helpers.Lines.Count}", "diag.cs plus diag-helpers.cs; the runner is one framework call."),
                "diag.cs calls Process.RunAndCaptureTextAsync and pulls its helpers in with #:include diag-helpers.cs.",
                "Keep scripts as file-based apps, and split them with #:include once they grow. Use RunAndCaptureTextAsync " +
                "instead of hand-wiring ProcessStartInfo.",
                pages.Next()),
        ]);
    }
}
