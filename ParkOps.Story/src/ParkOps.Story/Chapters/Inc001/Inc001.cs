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
            // Orientation first: the fences, and where the script fits. Then the script itself, early.
            new FencesBeat(finding, pages.Next()),

            new WalkthroughBeat(title, "Reading the script", before,
            [
                new("One file, top to bottom",
                    $"This is the whole diagnostic as it ships: {total} lines, a file-based app run with dotnet run scripts/diag.cs. " +
                    "No project file, which is good. Everything else has to live in this one file, and only dnedry has ever changed it.",
                    "#!/usr/bin/env dotnet", "#:package"),
                new("Its job is three lines",
                    "Print a header, show the last five commits, print the runtime table. That's the whole diagnostic.",
                    "AnsiConsole.Write(new Rule", "Diagnostics.PrintRuntimeInfo();"),
                new("A process runner for git log",
                    $"To run one command it carries its own {plumbing.Lines.Count + 1}-line helper. It starts with ProcessStartInfo: " +
                    "redirect stdout and stderr, no shell, no window, then each argument.",
                    "static async Task<string> RunAndCaptureAsync", "foreach (var argument"),
                new("Two streams, wired by hand",
                    "Two StringBuilders, two event handlers with null checks, then Start, BeginOutputReadLine, BeginErrorReadLine " +
                    "and WaitForExitAsync. Every one of those calls has to be there, in that order.",
                    "var output = new StringBuilder();", "await process.WaitForExitAsync();"),
                new("A failure looks like a result",
                    "When git fails, it hands back stderr as if it were the log. The caller can't tell the difference.",
                    "return process.ExitCode"),
                new("And the helpers can't move out",
                    "PrintRuntimeInfo sits at the bottom because a file-based app was one file. Every new helper makes this file " +
                    "longer, which is why only dnedry would touch it.",
                    "// ▶ INC-001b: move this class", "AnsiConsole.Write(table);"),
            ], pages.Next()),

            new ConsoleBeat(title, "Before", "Running it, as shipped",
                ConsoleCapture.Load("capture/Inc001/diag-before.txt"), null, pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(before.Excerpt("#!/usr/bin/env dotnet", "Diagnostics.PrintRuntimeInfo();"), after,
                    new("Process.RunAndCaptureTextAsync", "New in .NET 11",
                        "One call starts the process, captures both streams and waits. The result has StandardOutput, StandardError " +
                        "and ExitCode. #:include brings in a second file.",
                        new Rectangle(28, 23, 90, 6)), Tab: "diag.cs"),
                new FileFix(before.From("static async Task<string> RunAndCaptureAsync"), SourceFile.Empty(ScriptPath), Tab: "diag.cs (helpers)"),
                new FileFix(SourceFile.Empty(HelpersPath), helpers,
                    new("static class Diagnostics", "#:include",
                        "A file-based app can now span files: #:include diag-helpers.cs compiles this one in. The class is unchanged; it " +
                        "just has its own file.",
                        new Rectangle(28, 27, 90, 6))),
            ], pages.Next()),

            new ConsoleBeat(title, "After", "Same diagnostic, a fraction of the code",
                ConsoleCapture.Load("capture/Inc001/diag-after.txt"),
                new("Fence controller", "Identical output",
                    $"Same commits, same runtime table. The script went from {total} lines in one file to {after.Lines.Count} plus a " +
                    $"{helpers.Lines.Count}-line helpers file, and the part only dnedry understood is gone.",
                    new Rectangle(28, 27, 90, 6)),
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
