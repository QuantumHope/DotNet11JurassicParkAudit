using ParkOps.Story.Beats;
using ParkOps.Story.Chapters.Inc001;
using ParkOps.Story.Chapters.Inc002;
using ParkOps.Story.Chapters.Inc003;
using ParkOps.Story.Chapters.Inc004;
using ParkOps.Story.Chapters.Inc005;
using ParkOps.Story.Chapters.Inc006;
using ParkOps.Story.Chapters.Inc007;
using ParkOps.Story.Chapters.Inc008;
using ParkOps.Story.Chapters.Inc009;
using ParkOps.Story.Chapters.Inc010;

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
            Inc001.Build(pages),
            Inc002.Build(pages),
            Inc003.Build(pages),
            Inc004.Build(pages),
            Inc005.Build(pages),
            Inc006.Build(pages),
            Inc007.Build(pages),
            Inc008.Build(pages),
            Inc009.Build(pages),
            Inc010.Build(pages),
        ];
        foreach (var lesson in lessons) summary.BuiltLessons.Add(lesson.Number!.Value);
        return [new("", "Executive summary", null, [summary]), .. lessons];
    }
}
