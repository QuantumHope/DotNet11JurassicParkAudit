namespace ParkOps.Core.Animals;

/// <summary>The animals InGen has on the books. Species from the genetics lab are added by the lab itself.</summary>
public static class ParkRoster
{
    public static IEnumerable<Dinosaur> CoreAnimals() =>
    [
        .. Tags("V", 8).Select(t => new Velociraptor(t)),
        .. Tags("T", 1).Select(t => new TyrannosaurusRex(t)),
        .. Tags("D", 15).Select(t => new Dilophosaurus(t)),
        .. Tags("B", 18).Select(t => new Brachiosaurus(t)),
        .. Tags("TR", 45).Select(t => new Triceratops(t)),
        .. Tags("G", 120).Select(t => new Gallimimus(t)),
    ];

    public static IEnumerable<string> Tags(string prefix, int count) =>
        Enumerable.Range(1, count).Select(i => $"{prefix}-{i:00}");
}
