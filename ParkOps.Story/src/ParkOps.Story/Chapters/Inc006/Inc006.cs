using ParkOps.Core.Animals;
using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc006;

/// <summary>
/// Lesson 6, closed hierarchies. Incident INC-006, "Unauthorized species": the genetics lab declares Procompsognathus in
/// its own assembly, and the feeding planner's discard arm puts thirty-one carnivores on lettuce.
/// </summary>
public static class Inc006
{
    private const string DinosaurPath = "src/ParkOps.Core/Animals/Dinosaur.cs";
    private const string PlannerPath = "src/ParkOps.Core/Feeding/FeedingPlanner.cs";
    private const string LabPath = "src/ParkOps.Genetics.Plugin/WuLab.cs";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-006");
        var title = finding.LessonLabel;
        var dinosaur = SourceFile.Load("before/Dinosaur.cs", DinosaurPath);
        var closedOnly = SourceFile.Load("after/Inc006/DinosaurClosedOnly.cs", DinosaurPath);
        var planner = SourceFile.Load("before/FeedingPlanner.cs", PlannerPath);
        var lab = SourceFile.Load("before/WuLab.cs", LabPath);

        var compies = ParkOps.Genetics.Plugin.WuLab.NewArrivals().ToList();
        var plan = ParkOps.Core.Feeding.FeedingPlanner.PlanFor(compies[0]);

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Feeding plan  (F2 ANIMALS)", PlannerPath,
            [
                new("Observation",
                    $"The lab's newest animals, {compies.Count} Procompsognathus (\"compies\"), are on the plant-eaters' diet. " +
                    "Compies eat meat. The feeding plan's own check, no meat-eater on greens, didn't catch it.",
                    $"{compies.Count}", "compies on the plant-eaters' diet"),
                new("Why it matters",
                    "The feeding plan only knows the species it was written for, and gives anything new greens. The genetics lab " +
                    "can add a species in its own code, without telling anyone.",
                    "0", "reviews before the lab added a species"),
            ], pages.Next()),

            new SpeciesTreeBeat(finding, compies.Count, pages.Next()),

            new FeedingExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, planner,
            [
                new("_                                                  =>", "Unknown means herbivore",
                    "Dinosaur is abstract, so any assembly can derive from it and this switch can't be exhaustive. Anything it " +
                    "doesn't list, including species it has never seen, falls to _ and gets greens.",
                    new Rectangle(28, 23, 90, 7)),
            ], pages.Next()),

            new AssemblyBoundaryBeat(finding, pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(dinosaur, closedOnly, new("public closed record Dinosaur", "Closed",
                    "A closed type can only be derived from in its own assembly. Core now owns the list of species.",
                    new Rectangle(28, 21, 90, 6))),
            ], pages.Next()),

            new ConsoleBeat(title, "Build", "The lab's species stops compiling",
                ConsoleCapture.Load("capture/Inc006/plugin-build.txt"),
                new("error CS9382", "That's the point",
                    "The plugin can no longer invent a species. Procompsognathus has to be declared in Core, where the feeding " +
                    "planner can see it, and reviewed like every other species.",
                    new Rectangle(28, 27, 90, 6)),
                pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(closedOnly, SourceFile.Load("after/Inc006/Dinosaur.cs", DinosaurPath), new("public record Procompsognathus", "Declared in Core",
                    "The compy moves into Core, as a carnivore. The lab can still breed them; it just can't define them.",
                    new Rectangle(28, 21, 90, 6))),
                new FileFix(lab, SourceFile.Load("after/Inc006/WuLab.cs", LabPath)),
                new FileFix(planner, SourceFile.Load("after/Inc006/FeedingPlanner.cs", PlannerPath), new("Procompsognathus                                  =>",
                    "Exhaustive, no discard",
                    "With the hierarchy closed the compiler knows every species, so the switch lists them all and has no _ arm. " +
                    "A new species is a compile warning until someone plans its diet.",
                    new Rectangle(28, 21, 90, 7))),
            ], pages.Next()),

            new FeedingExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () =>
                {
                    var fixedPlan = Fixed.Inc006.FeedingPlanner.PlanFor(new Fixed.Inc006.Procompsognathus("C-01"));
                    return new($"{plan}", $"for {compies.Count} carnivores the lab declared as herbivores, unnoticed by the sanity check.",
                        $"{fixedPlan}", "Procompsognathus declared in Core as a carnivore; every species is planned explicitly.");
                },
                "Dinosaur is closed; Procompsognathus moved from the lab's plugin into Core; FeedingPlanner lists every species " +
                "with no discard arm.",
                "Close hierarchies that stand for a fixed set of domain types. Let other assemblies create instances, not new kinds.",
                pages.Next()),
        ]);
    }
}
