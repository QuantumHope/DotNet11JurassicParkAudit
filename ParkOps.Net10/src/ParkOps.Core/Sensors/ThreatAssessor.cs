namespace ParkOps.Core.Sensors;

public enum Threat { None, Low, Critical }

public sealed record Assessment(Threat Level, string Label);

public static class ThreatAssessor
{
    // ▶ INC-003b: discard arm swallows every contact type nobody listed (fix: exhaustive union switch)
    public static Assessment Assess(MotionContact contact) => contact switch
    {
        VehicleContact v => new(Threat.None, $"Tour vehicle {v.Vehicle.CallSign}"),
        StaffContact s   => new(Threat.None, $"Staff: {s.Employee.Name}"),
        _                => new(Threat.None, "GUEST detected (friendly)"), // dnedry: probably a guest
    };
}
