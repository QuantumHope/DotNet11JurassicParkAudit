using ParkOps.ControlRoom.Fences;
using Spectre.Console;

namespace ParkOps.ControlRoom.Panels;

public static class FencePanel
{
    public static Panel Render()
    {
        var power = FenceController.PowerOn ? "[green]ON[/]" : "[red]OFF[/]";
        var cause = FenceController.DisabledBy ?? "unknown";
        var stack = string.Join("\n", FenceController.CapturedStack.Select(f => $"  at {f}"));

        var body = new Markup($"Perimeter fences: {power}  (cause: [yellow]{Markup.Escape(cause)}[/])\n[grey]{Markup.Escape(stack)}[/]");
        return new Panel(body).Header("FENCE STATUS").Expand();
    }
}
