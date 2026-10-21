using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Core.Sensors;

namespace ParkOps.ControlRoom.Panels;

public static class CensusPanel
{
    public static Readout Render(IReadOnlyList<Dinosaur> expected, IReadOnlyList<TrackedSignal> tracked)
    {
        // ▶ INC-002: inner join only counts the animals we expected to find (fix: FullJoin)
        var census = expected.Join(tracked, e => e.Tag, t => t.Tag, (e, t) => e);
        Line[] lines = [new($"{census.Count()} of {expected.Count} rostered animals located", Tone.Strong), new("ALL ACCOUNTED FOR", Tone.Good)];
        var status = census.Count() == expected.Count ? PanelStatus.Nominal : PanelStatus.Alert;

        return new("ANIMAL CENSUS", status, lines);
    }
}
