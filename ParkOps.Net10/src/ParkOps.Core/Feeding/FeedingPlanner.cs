using ParkOps.Core.Animals;

namespace ParkOps.Core.Feeding;

public enum FeedingPlan { Greens, Livestock, SmallPrey }

public static class FeedingPlanner
{
    // ▶ INC-004b: unknown species fall through to the herbivore plan (fix: exhaustive switch over a closed hierarchy)
    public static FeedingPlan PlanFor(Dinosaur dinosaur) => dinosaur switch
    {
        Velociraptor or TyrannosaurusRex or Dilophosaurus => FeedingPlan.Livestock,
        _                                                  => FeedingPlan.Greens,
    };
}
