namespace ParkOps.Story.Fixed.Inc006;

public closed record Dinosaur(string Tag)
{
    public abstract bool IsCarnivore { get; }
}

public record Velociraptor(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => true; }
public record TyrannosaurusRex(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => true; }
public record Dilophosaurus(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => true; }
public record Brachiosaurus(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => false; }
public record Triceratops(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => false; }
public record Gallimimus(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => false; }
public record Procompsognathus(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => true; }
