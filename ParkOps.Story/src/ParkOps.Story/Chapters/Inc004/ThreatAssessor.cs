using ParkOps.Core.Animals;

namespace ParkOps.Story.Fixed.Inc004;

public enum Threat { None, Low, Critical }

public sealed record Assessment(Threat Level, string Label);

public static class ThreatAssessor
{
    public static Assessment Assess(MotionContact contact) => contact switch
    {
        Dinosaur d    => new(d.IsCarnivore ? Threat.Critical : Threat.Low, $"{d.GetType().Name} {d.Tag}"),
        Guest g       => new(Threat.None, $"Guest: {g.Name}"),
        TourVehicle v => new(Threat.None, $"Tour vehicle {v.CallSign}"),
        Employee e    => new(Threat.None, $"Staff: {e.Name}"),
    };
}
