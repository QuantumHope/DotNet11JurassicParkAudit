#!/usr/bin/env dotnet
#:package Spectre.Console
// ▶ INC-001b: helpers have to live in this one file (fix: #:include diag-helpers.cs)

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console;

AnsiConsole.Write(new Rule("[yellow]PARK OPS // SYSTEM DIAGNOSTIC[/]"));

// ▶ INC-001a: ProcessStartInfo plumbing only Nedry understood (fix: Process.RunAndCaptureTextAsync)
var log = await RunAndCaptureAsync("git", "log", "-5", "--format=%h  %an  %s");
AnsiConsole.WriteLine(log);

Diagnostics.PrintRuntimeInfo();

static async Task<string> RunAndCaptureAsync(string fileName, params string[] arguments)
{
    var startInfo = new ProcessStartInfo(fileName)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };
    foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

    var output = new StringBuilder();
    var error = new StringBuilder();
    using var process = new Process { StartInfo = startInfo };
    process.OutputDataReceived += (_, e) => { if (e.Data is not null) output.AppendLine(e.Data); };
    process.ErrorDataReceived += (_, e) => { if (e.Data is not null) error.AppendLine(e.Data); };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    await process.WaitForExitAsync();

    return process.ExitCode == 0 ? output.ToString() : error.ToString();
}

// ▶ INC-001b: move this class to diag-helpers.cs
static class Diagnostics
{
    public static void PrintRuntimeInfo()
    {
        var table = new Table().AddColumns("Check", "Value");
        table.AddRow("Runtime", RuntimeInformation.FrameworkDescription);
        table.AddRow("OS", RuntimeInformation.OSDescription);
        table.AddRow("Architecture", RuntimeInformation.ProcessArchitecture.ToString());
        table.AddRow("Server GC", System.Runtime.GCSettings.IsServerGC.ToString());
        table.AddRow("Fence controller", "[red]UNREACHABLE[/]");
        AnsiConsole.Write(table);
    }
}
