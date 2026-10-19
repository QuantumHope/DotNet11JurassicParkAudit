using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Core.Mapping;

namespace ParkOps.Story.Fixed.Inc006;

public static class GuestLocatorPanel
{
    public static Readout Render(ParkMap map)
    {
        HashSet<string> restricted = [with(StringComparer.OrdinalIgnoreCase), "PADDOCK-9", "RAPTOR-PEN", "VISITOR-CENTER-KITCHEN"];

        Cell? found = null;
        var scanned = 0;
        search:
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                scanned++;
                if (map[x, y].Occupants.Any(o => o is Guest)) { found = map[x, y]; break search; }
            }
        }

        if (found is null)
        {
            return new("GUEST LOCATOR", PanelStatus.Alert, [new("2 guests missing. No signal.", Tone.Bad)]);
        }

        var inRestrictedZone = restricted.Contains(found.Zone);
        Line[] lines =
        [
            new("Located:", Tone.Dim),
            .. found.Occupants.OfType<Guest>().Select(g => new Line($"  {g.Name}", Tone.Strong)),
            new($"Position: ({found.X},{found.Y}) {found.Zone}"),
            inRestrictedZone ? new("!! RESTRICTED ZONE !!", Tone.Bad) : new("Zone clear", Tone.Good),
            new($"Cells scanned: {scanned} of {map.Width * map.Height}", Tone.Dim),
        ];
        return new("GUEST LOCATOR", inRestrictedZone ? PanelStatus.Alert : PanelStatus.Nominal, lines);
    }
}
