namespace ParkOps.Story.Fixed.Inc006;

public enum FeedingPlan { Greens, Livestock, SmallPrey }

public static class FeedingPlanner
{
    public static FeedingPlan PlanFor(Dinosaur dinosaur) => dinosaur switch
    {
        Velociraptor or TyrannosaurusRex or Dilophosaurus => FeedingPlan.Livestock,
        Procompsognathus                                  => FeedingPlan.SmallPrey,
        Brachiosaurus or Triceratops or Gallimimus        => FeedingPlan.Greens,
    };
}
