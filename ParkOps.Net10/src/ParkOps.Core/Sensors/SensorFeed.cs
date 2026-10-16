using System.Runtime.CompilerServices;
using ParkOps.Core.Animals;

namespace ParkOps.Core.Sensors;

public sealed record SensorPing(string Location, MotionContact Contact);

public static class SensorFeed
{
    public static IReadOnlyList<SensorPing> Current() =>
    [
        // ▶ INC-003c: every case has to be wrapped (fix: implicit conversion to the union)
        new("Paddock 9",      new DinosaurContact(new Velociraptor("V-07"))),
        new("Visitor Center", new GuestContact(new Guest("Donald Gennaro"))),
        new("Main Road",      new VehicleContact(new TourVehicle("Explorer 04"))),
        new("East Dock",      new StaffContact(new Employee("Robert Muldoon", "Game Warden"))),
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
