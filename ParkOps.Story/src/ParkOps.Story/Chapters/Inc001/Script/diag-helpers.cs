using System.Runtime.InteropServices;
using Spectre.Console;

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
