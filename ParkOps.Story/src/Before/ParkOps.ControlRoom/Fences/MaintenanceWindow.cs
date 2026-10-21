using System.Diagnostics;
using System.Reflection;

namespace ParkOps.ControlRoom.Fences;

public static class MaintenanceWindow
{
    public static async Task RunAsync()
    {
        await Task.Yield();
        await WhteRbt.ExecuteAsync();
    }
}

// dnedry: contractor maintenance routine. Do not touch.
public static class WhteRbt
{
    public static async Task ExecuteAsync()
    {
        await Task.Delay(10);
        await FenceController.DisableFencesAsync();
    }
}

public static class FenceController
{
    public static bool PowerOn { get; private set; } = true;
    public static string? DisabledBy { get; private set; }
    public static IReadOnlyList<string> CapturedStack { get; private set; } = [];

    public static async Task DisableFencesAsync()
    {
        // Audit: who asked? Walk the live stack for the first ParkOps method (one a person wrote, not a
        // compiler-generated type) outside this class. Before the first await, so the caller is still on the stack.
        var methods = new StackTrace().GetFrames().Select(f => f.GetMethod()).OfType<MethodBase>().ToList();
        CapturedStack = [.. methods.Take(6).Select(m => $"{m.DeclaringType?.Name}.{m.Name}")];
        var caller = methods.FirstOrDefault(m =>
            m.DeclaringType?.Namespace?.StartsWith("ParkOps") == true &&
            !m.DeclaringType.Name.StartsWith('<') &&
            m.DeclaringType != typeof(FenceController) &&
            m.DeclaringType.DeclaringType != typeof(FenceController));
        DisabledBy = caller is null ? null : $"{caller.DeclaringType!.Name}.{caller.Name}";

        await Task.Delay(10);
        PowerOn = false;
    }
}
