using Microsoft.EntityFrameworkCore;
using ParkOps.Data;
using Spectre.Console;

namespace ParkOps.ControlRoom.Panels;

public static class MaintenancePanel
{
    public static async Task<Panel> RenderAsync()
    {
        await using var db = ParkDb.Create();

        // ▶ INC-007a: loads every fence and every breach into memory to find one row (fix: MaxByAsync)
        var fences = await db.Fences.Include(f => f.Breaches).ToListAsync();
        var worst = fences.MaxBy(f => f.Breaches.Count);

        var body = new Markup(
            $"Priority repair: [red]{worst?.Name}[/]\n" +
            $"[grey]Rows pulled into memory: {db.ChangeTracker.Entries().Count():N0}[/]");
        return new Panel(body).Header("MAINTENANCE").Expand();
    }
}
