using Microsoft.EntityFrameworkCore;
using ParkOps.ControlRoom.Ui;
using ParkOps.Data;

namespace ParkOps.Story.Fixed.Inc007;

public static class MaintenancePanel
{
    public static async Task<Readout> RenderAsync()
    {
        await using var db = ParkDb.Create();

        var worst = await db.Fences.MaxByAsync(f => f.Breaches.Count());

        Line[] lines =
        [
            new("Priority repair:", Tone.Dim),
            new($"  {worst?.Name}", Tone.Bad),
            new($"Rows pulled into memory: {db.ChangeTracker.Entries().Count():N0}", Tone.Dim),
        ];
        return new("MAINTENANCE", PanelStatus.Attention, lines);
    }
}
