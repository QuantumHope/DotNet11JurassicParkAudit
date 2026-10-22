using ParkOps.Core.Animals;

namespace ParkOps.Core.Mapping;

// Ships in the mapping vendor's library: a flat array of cells, no indexer.
public sealed record Cell(int X, int Y, string Zone, IReadOnlyList<object> Occupants);

public sealed class ParkMap(int width, int height, Cell[] cells)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public Cell[] Cells { get; } = cells;

    public static ParkMap Load()
    {
        const int width = 20, height = 20;
        var cells = new Cell[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                // Zone names come from the sensor firmware, lower-case. The bottom two rows are the east road.
                var zone = y >= 18 ? "east-road" : $"paddock-{y / 2 + 1}";
                IReadOnlyList<object> occupants = (x, y) switch
                {
                    (12, 16) => [new Guest("Tim Murphy"), new Guest("Lex Murphy")],
                    (13, 16) => [new Velociraptor("V-03")],
                    (4, 19) => [new Guest("Dennis Nedry")], // contractor, on a visitor pass
                    _ => [],
                };
                cells[y * width + x] = new Cell(x, y, zone, occupants);
            }
        }
        return new ParkMap(width, height, cells);
    }
}
