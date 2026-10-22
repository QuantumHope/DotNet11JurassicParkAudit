using Microsoft.EntityFrameworkCore;
using ParkOps.ControlRoom.Ui;
using ParkOps.Data;

namespace ParkOps.ControlRoom.Panels;

public static class MaintenancePanel
{
    public static async Task<Readout> RenderAsync()
    {
        await using var db = ParkDb.Create();

        // ▶ INC-008: loads every fence and every breach into memory to find one row (fix: MaxByAsync)
        var fences = await db.Fences.Include(f => f.Breaches).ToListAsync();
        var worst = fences.MaxBy(f => f.Breaches.Count);

        Line[] lines =
        [
            new("Priority repair:", Tone.Dim),
            new($"  {worst?.Name}", Tone.Bad),
            new($"Rows pulled into memory: {db.ChangeTracker.Entries().Count():N0}", Tone.Dim),
        ];
        return new("MAINTENANCE", PanelStatus.Attention, lines);
    }
}
