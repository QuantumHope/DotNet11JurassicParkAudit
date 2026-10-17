using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Sensors;

namespace ParkOps.ControlRoom.Panels;

public static class MotionPanel
{
    public static Readout Render(IReadOnlyList<SensorPing> pings)
    {
        List<Line> lines = [new($"{"LOCATION",-15}{"CONTACT",-26}THREAT", Tone.Dim)];
        var status = PanelStatus.Nominal;
        foreach (var ping in pings)
        {
            var assessment = ThreatAssessor.Assess(ping.Contact);
            var tone = assessment.Level switch
            {
                Threat.Critical => Tone.Bad,
                Threat.Low => Tone.Warn,
                _ => Tone.Good,
            };
            if (assessment.Level == Threat.Critical) status = PanelStatus.Alert;
            lines.Add(new($"{ping.Location,-15}{assessment.Label,-26}{assessment.Level.ToString().ToUpperInvariant()}", tone));
        }

        return new("MOTION SENSORS", status, lines);
    }
}
