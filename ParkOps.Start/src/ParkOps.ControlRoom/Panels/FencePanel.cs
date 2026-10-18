using ParkOps.ControlRoom.Fences;
using ParkOps.ControlRoom.Ui;

namespace ParkOps.ControlRoom.Panels;

public static class FencePanel
{
    public static Readout Render()
    {
        List<Line> lines =
        [
            FenceController.PowerOn ? new("PERIMETER FENCES: ON", Tone.Good) : new("PERIMETER FENCES: OFF", Tone.Bad),
            new($"Disabled by: {FenceController.DisabledBy ?? "unknown"}", Tone.Strong),
            .. FenceController.CapturedStack.Take(4).Select(frame => new Line($"  at {frame}", Tone.Dim)),
        ];

        return new("FENCE STATUS", FenceController.PowerOn ? PanelStatus.Nominal : PanelStatus.Alert, lines);
    }
}
