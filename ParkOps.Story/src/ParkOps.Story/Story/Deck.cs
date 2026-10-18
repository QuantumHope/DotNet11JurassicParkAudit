using ParkOps.Story.Beats;
using ParkOps.Story.Chapters.Inc002;

namespace ParkOps.Story.Story;

/// <summary>The running order. Add a chapter here as each incident is built (see PLAN.md).</summary>
public static class Deck
{
    public static IReadOnlyList<Chapter> Build()
    {
        var pages = new Pages();
        return
        [
            new("", "Assessment", null, [new CoverBeat(), new FindingsIndexBeat(pages.Next())]),
            Inc002.Build(pages),
            new("", "Summary", null, [new ClosingBeat(pages.Next())]),
        ];
    }
}
