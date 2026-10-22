using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Core.Sensors;

namespace ParkOps.Story.Fixed;

public static class CensusPanel
{
    public static Readout Render(IReadOnlyList<Dinosaur> expected, IReadOnlyList<TrackedSignal> tracked)
    {
        var audit = expected.FullJoin(tracked, e => e.Tag, t => t.Tag,
            (e, t) => (Expected: e?.Tag ?? "— NOT ON ROSTER —", Tracked: t?.Tag ?? "— NO SIGNAL —"));
        var problems = audit.Where(r => r.Expected.StartsWith('—') || r.Tracked.StartsWith('—')).ToList();
        Line[] lines =
        [
            new($"{expected.Count} expected / {tracked.Count} tracked / {problems.Count} unaccounted for", Tone.Bad),
            .. problems.Select(r => new Line($"{r.Expected,-20} {r.Tracked}", Tone.Bad)),
        ];
        var status = problems.Count == 0 ? PanelStatus.Nominal : PanelStatus.Alert;

        return new("ANIMAL CENSUS", status, lines);
    }
}
