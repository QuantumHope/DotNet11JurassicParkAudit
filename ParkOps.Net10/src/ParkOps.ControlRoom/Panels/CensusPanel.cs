using ParkOps.Core.Animals;
using ParkOps.Core.Sensors;
using Spectre.Console;

namespace ParkOps.ControlRoom.Panels;

public static class CensusPanel
{
    public static Panel Render(IReadOnlyList<Dinosaur> expected, IReadOnlyList<TrackedSignal> tracked)
    {
        // ▶ INC-002: inner join only counts the animals we expected to find (fix: FullJoin)
        var census = expected.Join(tracked, e => e.Tag, t => t.Tag, (e, t) => e);
        var body = new Markup($"[green]{census.Count()} / {expected.Count} ✅ ALL ACCOUNTED FOR[/]");

        return new Panel(body).Header("ANIMAL CENSUS").Expand();
    }
}
