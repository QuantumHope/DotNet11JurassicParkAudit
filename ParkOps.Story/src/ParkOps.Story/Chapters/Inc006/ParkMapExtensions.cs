using ParkOps.Core.Mapping;

namespace ParkOps.Story.Fixed.Inc006;

public static class ParkMapExtensions
{
    extension(ParkMap map)
    {
        public Cell this[int x, int y] => map.Cells[y * map.Width + x];
    }
}
