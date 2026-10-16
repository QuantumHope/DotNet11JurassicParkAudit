using ParkOps.Core.Animals;

namespace ParkOps.Core.Sensors;

public sealed record TrackedSignal(string Tag);

/// <summary>What the motion grid actually sees, whether or not it's on the roster.</summary>
public static class SensorNetwork
{
    public static IReadOnlyList<TrackedSignal> Sweep() =>
    [
        .. ParkRoster.CoreAnimals().Select(d => new TrackedSignal(d.Tag)),
        .. ParkRoster.Tags("C", 31).Select(t => new TrackedSignal(t)),
        // Life, uh, finds a way.
        .. ParkRoster.Tags("V-J", 6).Select(t => new TrackedSignal(t)),
        .. ParkRoster.Tags("C-J", 10).Select(t => new TrackedSignal(t)),
    ];
}
