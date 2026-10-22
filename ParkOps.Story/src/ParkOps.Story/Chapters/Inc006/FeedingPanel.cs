using ParkOps.ControlRoom.Ui;

namespace ParkOps.Story.Fixed.Inc006;

/// <summary>The original FeedingPanel, unchanged, compiled against the closed hierarchy. Not shown on screen.</summary>
public static class FeedingPanel
{
    public static Readout Render(IReadOnlyList<Dinosaur> roster)
    {
        List<Line> lines = [new($"{"SPECIES",-20}{"HEAD",6}   PLAN", Tone.Dim)];
        foreach (var species in roster.GroupBy(d => d.GetType().Name))
        {
            var (plan, tone) = FeedingPlanner.PlanFor(species.First()) switch
            {
                FeedingPlan.Livestock => ("Goats", Tone.Bad),
                FeedingPlan.SmallPrey => ("Small prey", Tone.Warn),
                _ => ("Lettuce", Tone.Good),
            };
            lines.Add(new($"{species.Key,-20}{species.Count(),6}   {plan}", tone));
        }

        // Sanity check: carnivores should never be on the greens plan.
        var consistent = roster.All(d => d.IsCarnivore == (FeedingPlanner.PlanFor(d) != FeedingPlan.Greens));
        return new("FEEDING PLAN", consistent ? PanelStatus.Nominal : PanelStatus.Alert, lines);
    }
}
