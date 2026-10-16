namespace ParkOps.Core.Animals;

// dnedry: open for extension so Henry's lab stops filing tickets. You're welcome.
// ▶ INC-004a: anyone, in any assembly, can derive a new species (fix: closed)
public abstract record Dinosaur(string Tag)
{
    public abstract bool IsCarnivore { get; }
}

public record Velociraptor(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => true; }
public record TyrannosaurusRex(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => true; }
public record Dilophosaurus(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => true; }
public record Brachiosaurus(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => false; }
public record Triceratops(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => false; }
public record Gallimimus(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => false; }
