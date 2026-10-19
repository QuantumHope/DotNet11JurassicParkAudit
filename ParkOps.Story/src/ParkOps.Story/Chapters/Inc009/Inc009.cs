using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc009;

/// <summary>
/// Lesson 9, JSON unions + SerializeAsyncEnumerable. Incident INC-009, "The sensor feed": telemetry is buffered into one
/// late array, and a hand-written converter drops staff contacts. Builds on lesson 3's MotionContact union.
/// </summary>
public static class Inc009
{
    private const string Feed = "src/ParkOps.ControlRoom/Feed/";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-009");
        var title = finding.LessonLabel;
        var before = SourceFile.Load("before/FeedCommand.cs", Feed + "FeedCommand.cs");
        var after = SourceFile.Load("after/Inc009/FeedCommand.cs", Feed + "FeedCommand.cs");
        var converter = SourceFile.Load("before/MotionContactJsonConverter.cs", Feed + "MotionContactJsonConverter.cs");
        var pings = ParkOps.Core.Sensors.SensorFeed.Current();

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Raw telemetry  (F4 OPERATIONS)", Feed + "FeedCommand.cs",
            [
                new("Observation",
                    "Operations streams the motion sensors' raw telemetry to the control room. Nothing shows up until every sensor " +
                    "has reported, then it all arrives at once, and one of the contacts is an empty object.",
                    "{}", "what the game warden's contact looks like on the wire"),
                new("Why it matters",
                    "A feed that waits for the slowest sensor isn't live. And the converter that drops staff was written once, " +
                    "never tested, and has to be updated by hand every time a contact type is added.",
                    $"{pings.Count} → 1", "contacts per message: one array, sent at the end"),
            ], pages.Next()),

            new FeedExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, before,
            [
                new("new MotionContactJsonConverter()", "A hand-written converter",
                    "MotionContact is an open base type, so System.Text.Json can't know its shapes. dnedry's converter writes three " +
                    "of the four by hand. Staff falls out of the switch and is written as {}.",
                    new Rectangle(28, 27, 90, 7)),
                new("await foreach (var ping in SensorFeed.ReadAllAsync()) batch.Add(ping);", "Buffered, then sent",
                    "The feed is an IAsyncEnumerable, but the command collects all of it into a list and serializes the list. " +
                    "Nothing leaves until the last sensor reports.",
                    new Rectangle(28, 27, 90, 7)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(before, after, new("SerializeAsyncEnumerable", "Stream it, one line each",
                    "SerializeAsyncEnumerable writes each contact as the feed yields it. topLevelValues: true makes each one its own " +
                    "line (JSON Lines) instead of an element of one long array.",
                    new Rectangle(28, 26, 90, 7))),
                new FileFix(converter, SourceFile.Empty(Feed + "MotionContactJsonConverter.cs"), new("public sealed class MotionContactJsonConverter",
                    "No converter at all",
                    "With MotionContact a union (lesson 3), System.Text.Json serializes whichever case it holds, Employee included. " +
                    "The hand-written converter is deleted.",
                    new Rectangle(28, 26, 90, 7))),
            ], pages.Next()),

            new FeedExhibit(finding, CodePath.After, pages.Next()),

            new ResolutionBeat(finding, () => new(
                    "1 array", "after every sensor has reported, with the game warden written as {}.",
                    $"{pings.Count} lines", "one per contact, as each arrives, staff included."),
                "FeedCommand streams with JsonSerializer.SerializeAsyncEnumerable(..., topLevelValues: true); " +
                "MotionContactJsonConverter is deleted.",
                "If the source is an IAsyncEnumerable, keep it one all the way to the wire. " +
                "Let the serializer handle closed sets of types instead of hand-written converters.",
                pages.Next()),
        ]);
    }
}
