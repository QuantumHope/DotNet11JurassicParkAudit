using ParkOps.Core.Animals;
using ParkOps.Core.Feeding;
using Spectre.Console;

namespace ParkOps.ControlRoom.Panels;

public static class FeedingPanel
{
    public static Panel Render(IReadOnlyList<Dinosaur> roster)
    {
        var table = new Table().NoBorder().AddColumns("Species", "Head", "Plan");
        foreach (var species in roster.GroupBy(d => d.GetType().Name))
        {
            var plan = FeedingPlanner.PlanFor(species.First()) switch
            {
                FeedingPlan.Livestock => "Livestock (goats)",
                FeedingPlan.SmallPrey => "Small prey",
                _ => "Greens (lettuce)",
            };
            table.AddRow(species.Key, species.Count().ToString(), plan);
        }

        return new Panel(table).Header("FEEDING PLAN").Expand();
    }
}
