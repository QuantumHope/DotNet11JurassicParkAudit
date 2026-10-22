using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc005;

/// <summary>
/// Lesson 5, JSON unions + SerializeAsyncEnumerable. Incident INC-005, "The sensor feed": telemetry is buffered into one
/// late array, and a hand-written converter drops staff contacts. Builds on lesson 4's MotionContact union.
/// </summary>
public static class Inc005
{
    private const string Feed = "src/ParkOps.ControlRoom/Feed/";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-005");
        var title = finding.LessonLabel;
        var before = SourceFile.Load("before/FeedCommand.cs", Feed + "FeedCommand.cs");
        var after = SourceFile.Load("after/Inc005/FeedCommand.cs", Feed + "FeedCommand.cs");
        var converter = SourceFile.Load("before/MotionContactJsonConverter.cs", Feed + "MotionContactJsonConverter.cs");
        var pings = ParkOps.Core.Sensors.SensorFeed.Current();

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Raw telemetry  (F4 OPERATIONS)", Feed + "FeedCommand.cs",
            [
                new("Observation",
                    "The control room's live sensor feed isn't live. Nothing shows up until every sensor has reported, then it " +
                    "all arrives at once. And the game warden's contact arrives empty.",
                    "{}", "the game warden's contact, as sent"),
                new("Why it matters",
                    "In an emergency the feed should show where things are now, not where they were when the slowest sensor " +
                    "finished. The empty contact comes from a hand-written converter that forgot staff.",
                    $"{pings.Count} → 1", "contacts sent together, in one late message"),
            ], pages.Next()),

            new FeedTimelineBeat(finding, [.. pings.Select(p => p.Location)], pages.Next()),

            new FeedExhibit(finding, CodePath.Before, pages.Next()),

            new CodeBeat(title, before,
            [
                new("new MotionContactJsonConverter()", "A hand-written converter",
                    "MotionContact is an open base type, so System.Text.Json can't know its shapes. dnedry's converter writes three " +
                    "of the four by hand. Staff falls out of the switch and is written as {}.",
                    new Rectangle(28, 24, 90, 7)),
                new("await foreach (var ping in SensorFeed.ReadAllAsync()) batch.Add(ping);", "Buffered, then sent",
                    "The feed is an IAsyncEnumerable, but the command collects all of it into a list and serializes the list. " +
                    "Nothing leaves until the last sensor reports.",
                    new Rectangle(28, 24, 90, 7)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(before, after, new("SerializeAsyncEnumerable", "Stream it, one line each",
                    "SerializeAsyncEnumerable writes each contact as the feed yields it. topLevelValues: true makes each one its own " +
                    "line (JSON Lines) instead of an element of one long array.",
                    new Rectangle(28, 23, 90, 7))),
                new FileFix(converter, SourceFile.Empty(Feed + "MotionContactJsonConverter.cs"), new("public sealed class MotionContactJsonConverter",
                    "No converter at all",
                    "With MotionContact a union (lesson 4), System.Text.Json serializes whichever case it holds, Employee included. " +
                    "The hand-written converter is deleted.",
                    new Rectangle(28, 23, 90, 7))),
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
