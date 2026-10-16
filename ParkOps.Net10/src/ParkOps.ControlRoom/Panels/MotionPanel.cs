using ParkOps.Core.Sensors;
using Spectre.Console;

namespace ParkOps.ControlRoom.Panels;

public static class MotionPanel
{
    public static Panel Render(IReadOnlyList<SensorPing> pings)
    {
        var table = new Table().NoBorder().AddColumns("Location", "Contact", "Threat");
        foreach (var ping in pings)
        {
            var assessment = ThreatAssessor.Assess(ping.Contact);
            var color = assessment.Level switch
            {
                Threat.Critical => "red",
                Threat.Low => "yellow",
                _ => "green",
            };
            table.AddRow(ping.Location, assessment.Label, $"[{color}]{assessment.Level}[/]");
        }

        return new Panel(table).Header("MOTION SENSORS").Expand();
    }
}
