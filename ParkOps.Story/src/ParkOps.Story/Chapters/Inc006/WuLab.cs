using ParkOps.Core.Animals;

namespace ParkOps.Story.Fixed.Inc006;

public static class WuLab
{
    public static IEnumerable<Dinosaur> NewArrivals() =>
        ParkRoster.Tags("C", 31).Select(t => new Procompsognathus(t));
}
