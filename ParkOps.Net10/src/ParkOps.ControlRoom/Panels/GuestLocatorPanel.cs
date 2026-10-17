using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Core.Mapping;

namespace ParkOps.ControlRoom.Panels;

public static class GuestLocatorPanel
{
    public static Readout Render(ParkMap map)
    {
        // ▶ INC-006a: case-sensitive, but the firmware reports "paddock-9" (fix: collection expression with a comparer)
        var restricted = new HashSet<string> { "PADDOCK-9", "RAPTOR-PEN", "VISITOR-CENTER-KITCHEN" };

        Cell? found = null;
        var scanned = 0;
        var foundThem = false;
        // ▶ INC-006c: flag variable checked in two places; the outer loop never stops (fix: labeled break)
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                scanned++;
                // ▶ INC-006b: hand-rolled index math into the vendor's type (fix: extension indexer)
                var cell = map.Cells[y * map.Width + x];
                if (cell.Occupants.Any(o => o is Guest)) { found = cell; foundThem = true; break; }
            }
            if (foundThem) continue; // dnedry: stop searching
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
