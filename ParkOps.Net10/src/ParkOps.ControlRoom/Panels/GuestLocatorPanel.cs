using ParkOps.Core.Animals;
using ParkOps.Core.Mapping;
using Spectre.Console;

namespace ParkOps.ControlRoom.Panels;

public static class GuestLocatorPanel
{
    public static Panel Render(ParkMap map)
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

        var body = found is null
            ? new Markup("[red]2 guests missing. No signal.[/]")
            : new Markup(
                $"Located: {string.Join(", ", found.Occupants.OfType<Guest>().Select(g => g.Name))}\n" +
                $"Position: ({found.X},{found.Y}) {found.Zone}  " +
                (restricted.Contains(found.Zone) ? "[red]⚠ RESTRICTED ZONE[/]" : "[green]zone OK[/]") + "\n" +
                $"[grey]Cells scanned: {scanned} of {map.Width * map.Height}[/]");

        return new Panel(body).Header("GUEST LOCATOR").Expand();
    }
}
