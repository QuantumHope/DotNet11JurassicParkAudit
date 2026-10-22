using ParkOps.Core.Animals;

namespace ParkOps.Genetics.Plugin;

// dnedry: Henry's lab ships species straight from here. No review needed.
// ▶ INC-006c: species declared outside Core, never reviewed (fix: move into Core)
public record Procompsognathus(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => false; } // "basically scavengers" - H.W.

public static class WuLab
{
    public static IEnumerable<Dinosaur> NewArrivals() =>
        ParkRoster.Tags("C", 31).Select(t => new Procompsognathus(t));
}
