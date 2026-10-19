using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;
using Start = ParkOps.Core.Sensors;

namespace ParkOps.Story.Chapters.Inc003;

/// <summary>
/// Lesson 3, union types. Incident INC-003, "Probably a guest": the motion sensor in the raptor paddock reports a
/// Velociraptor, and a discard arm in ThreatAssessor turns it into a friendly guest.
/// </summary>
public static class Inc003
{
    private const string Sensors = "src/ParkOps.Core/Sensors/";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-003");
        var title = finding.LessonLabel;
        SourceFile Before(string file) => SourceFile.Load($"before/{file}", Sensors + file);
        SourceFile After(string file) => SourceFile.Load($"after/Inc003/{file}", Sensors + file);

        var paddock9 = Start.SensorFeed.Current()[0];
        var assessed = Start.ThreatAssessor.Assess(paddock9.Contact);
        var raptor = (ParkOps.Core.Animals.Dinosaur)paddock9.Contact.Value;

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Motion sensors  (F3 SECURITY)", Sensors + "ThreatAssessor.cs",
            [
                new("Observation",
                    $"The {paddock9.Location} motion sensor has been reporting a contact all morning. The control room shows it as " +
                    $"\"{assessed.Label}\", threat {assessed.Level.ToString().ToUpperInvariant()}. Nobody went to look.",
                    assessed.Level.ToString().ToUpperInvariant(), $"threat level ParkOps gave the {paddock9.Location} contact"),
                new("Why it matters",
                    $"{paddock9.Location} is the raptor paddock. The sensor did its job: it reported exactly what it saw. " +
                    "The software between the sensor and the screen decided it was probably a guest.",
                    $"{raptor.Tag}", $"the {raptor.GetType().Name} the sensor actually reported"),
            ], pages.Next()),

            new MotionExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, Before("ThreatAssessor.cs"),
            [
                new("_                =>", "The discard arm",
                    "MotionContact is an open base type, so the compiler can't know which wrappers exist and the switch needs a " +
                    "catch-all. DinosaurContact was added later and nobody added an arm: it lands in _, labelled a friendly guest.",
                    new Rectangle(28, 28, 90, 7)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(Before("MotionContact.cs"), After("MotionContact.cs"), new("public union MotionContact", "A union type",
                    "One line replaces the base record and four wrappers. A MotionContact is exactly one of these four types: " +
                    "the vendors' own records, unchanged.",
                    new Rectangle(28, 16, 90, 6))),
                new FileFix(Before("ThreatAssessor.cs"), After("ThreatAssessor.cs"), new("Dinosaur d    =>", "Exhaustive, no discard",
                    "The switch matches the case types directly and covers all four, so it needs no _ arm. Carnivores are " +
                    "CRITICAL; everything else is named, not guessed.",
                    new Rectangle(28, 28, 90, 6))),
                new FileFix(Before("SensorFeed.cs"), After("SensorFeed.cs"), new("new Velociraptor(\"V-07\")", "Implicit conversion",
                    "Each case type converts to the union implicitly, so the feed lists plain values with no wrappers.",
                    new Rectangle(28, 34, 90, 5))),
            ], pages.Next()),

            new ConsoleBeat(title, "Build", "A new kind of contact",
                ConsoleCapture.Load("capture/Inc003/unidentified-build.txt"),
                new("warning CS8509", "The compiler finds it",
                    "Add Unidentified to the union and the build points at every switch that doesn't handle it, by name. " +
                    "In the old code it would have been one more friendly guest.",
                    new Rectangle(28, 31, 90, 6)),
                pages.Next()),

            new MotionExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () =>
                {
                    var fixedAssessment = Fixed.Inc003.ThreatAssessor.Assess(Fixed.Inc003.SensorFeed.Current()[0].Contact);
                    return new(assessed.Level.ToString().ToUpperInvariant(), $"\"{assessed.Label}\" for {raptor.GetType().Name} {raptor.Tag} in {paddock9.Location}.",
                        fixedAssessment.Level.ToString().ToUpperInvariant(), $"\"{fixedAssessment.Label}\". New contact types are compile warnings, not guests.");
                },
                "MotionContact became a one-line union of the four contact types; the switch lost its discard arm; the feed lost its wrappers.",
                "When a value is one of a known set of types, say so with a union and let the compiler check every switch. " +
                "Treat a discard arm over domain types as a smell.",
                pages.Next()),
        ]);
    }
}
