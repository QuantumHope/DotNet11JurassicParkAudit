using ParkOps.Story.Beats;
using ParkOps.Story.Chapters.Inc002;

namespace ParkOps.Story.Story;

/// <summary>
/// The report: the executive summary first (the home page), then one chapter per lesson. Add a lesson here as each
/// incident is built (see PLAN.md); the summary lists all ten either way.
/// </summary>
public static class Deck
{
    public static IReadOnlyList<Chapter> Build()
    {
        var pages = new Pages();
        var summary = new SummaryBeat(pages.Next());
        Chapter[] lessons =
        [
            Inc002.Build(pages),
        ];
        foreach (var lesson in lessons) summary.BuiltLessons.Add(lesson.Number!.Value);
        return [new("", "Executive summary", null, [summary]), .. lessons];
    }
}
