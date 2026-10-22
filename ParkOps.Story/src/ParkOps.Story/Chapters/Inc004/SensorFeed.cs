using System.Runtime.CompilerServices;
using ParkOps.Core.Animals;

namespace ParkOps.Story.Fixed.Inc004;

public sealed record SensorPing(string Location, MotionContact Contact);

public static class SensorFeed
{
    public static IReadOnlyList<SensorPing> Current() =>
    [
        new("Kitchen",        new Velociraptor("V-07")),
        new("Visitor Center", new Guest("Donald Gennaro")),
        new("Main Road",      new TourVehicle("Explorer 04")),
        new("East Dock",      new Employee("Robert Muldoon", "Game Warden")),
    ];

    /// <summary>Live telemetry: the same contacts, trickling in one at a time.</summary>
    public static async IAsyncEnumerable<SensorPing> ReadAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var ping in Current())
        {
            await Task.Delay(250, ct);
            yield return ping;
        }
    }
}
