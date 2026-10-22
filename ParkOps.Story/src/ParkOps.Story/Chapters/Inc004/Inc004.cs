using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;
using Start = ParkOps.Core.Sensors;

namespace ParkOps.Story.Chapters.Inc004;

/// <summary>
/// Lesson 4, union types. Incident INC-004, "Probably a guest": the Visitor Center kitchen's motion sensor reports a
/// Velociraptor, and a discard arm in ThreatAssessor turns it into a friendly guest.
/// </summary>
public static class Inc004
{
    private const string Sensors = "src/ParkOps.Core/Sensors/";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-004");
        var title = finding.LessonLabel;
        SourceFile Before(string file) => SourceFile.Load($"before/{file}", Sensors + file);
        SourceFile After(string file) => SourceFile.Load($"after/Inc004/{file}", Sensors + file);

        var kitchen = Start.SensorFeed.Current()[0];
        var assessed = Start.ThreatAssessor.Assess(kitchen.Contact);
        var raptor = (ParkOps.Core.Animals.Dinosaur)kitchen.Contact.Value;

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Motion sensors  (F3 SECURITY)", Sensors + "ThreatAssessor.cs",
            [
                new("Observation",
                    "The Visitor Center kitchen's motion sensor has a contact, moving between the steel counters. The control room " +
                    $"shows it as \"{assessed.Label}\", threat {assessed.Level.ToString().ToUpperInvariant()}. Nobody went to look.",
                    assessed.Level.ToString().ToUpperInvariant(), "threat level ParkOps gave the kitchen contact"),
                new("Why it matters",
                    "It's a Velociraptor, out of its paddock and in the kitchen. The sensor did its job: it reported exactly what " +
                    "it saw. The software between the sensor and the screen decided it was probably a guest.",
                    $"{raptor.Tag}", $"the {raptor.GetType().Name} the sensor actually reported"),
            ], pages.Next()),

            new MotionExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, Before("ThreatAssessor.cs"),
            [
                new("_                =>", "The discard arm",
                    "MotionContact is an open base type, so the compiler can't know which wrappers exist and the switch needs a " +
                    "catch-all. DinosaurContact was added later and nobody added an arm: it lands in _, labelled a friendly guest.",
                    new Rectangle(28, 25, 90, 7)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(Before("MotionContact.cs"), After("MotionContact.cs"), new("public union MotionContact", "A union type",
                    "One line replaces the base record and four wrappers. A MotionContact is exactly one of these four types: " +
                    "the vendors' own records, unchanged.",
                    new Rectangle(28, 13, 90, 6))),
                new FileFix(Before("ThreatAssessor.cs"), After("ThreatAssessor.cs"), new("Dinosaur d    =>", "Exhaustive, no discard",
                    "The switch matches the case types directly and covers all four, so it needs no _ arm. Carnivores are " +
                    "CRITICAL; everything else is named, not guessed.",
                    new Rectangle(28, 25, 90, 6))),
                new FileFix(Before("SensorFeed.cs"), After("SensorFeed.cs"), new("new Velociraptor(\"V-07\")", "Implicit conversion",
                    "Each case type converts to the union implicitly, so the feed lists plain values with no wrappers.",
                    new Rectangle(28, 28, 90, 5))),
            ], pages.Next()),

            new ConsoleBeat(title, "Build", "A new kind of contact",
                ConsoleCapture.Load("capture/Inc004/unidentified-build.txt"),
                new("warning CS8509", "The compiler finds it",
                    "Add Unidentified to the union and the build points at every switch that doesn't handle it, by name. " +
                    "In the old code it would have been one more friendly guest.",
                    new Rectangle(28, 27, 90, 6)),
                pages.Next()),

            new MotionExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () =>
                {
                    var fixedAssessment = Fixed.Inc004.ThreatAssessor.Assess(Fixed.Inc004.SensorFeed.Current()[0].Contact);
                    return new(assessed.Level.ToString().ToUpperInvariant(), $"\"{assessed.Label}\" for {raptor.GetType().Name} {raptor.Tag} in the {kitchen.Location.ToLowerInvariant()}.",
                        fixedAssessment.Level.ToString().ToUpperInvariant(), $"\"{fixedAssessment.Label}\". New contact types are compile warnings, not guests.");
                },
                "MotionContact became a one-line union of the four contact types; the switch lost its discard arm; the feed lost its wrappers.",
                "When a value is one of a known set of types, say so with a union and let the compiler check every switch. " +
                "Treat a discard arm over domain types as a smell.",
                pages.Next()),
        ]);
    }
}
