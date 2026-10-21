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
/// The talk as one slideshow: a title slide, then each lesson (its section slide first), then the closing slide.
/// NEXT and BACK just walk it in order. The index (every lesson at a glance) comes last, after the closing slide,
/// and HOME jumps to it from anywhere.
/// </summary>
public static class Deck
{
    public static IReadOnlyList<Chapter> Build()
    {
        var pages = new Pages();
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
        var index = new SummaryBeat(pages.Next());
        foreach (var lesson in lessons) index.BuiltLessons.Add(lesson.Number!.Value);

        return
        [
            new("Title", "Unforeseen Consequences", null, [new TitleBeat()]),
            .. lessons.Select(lesson => lesson with { Beats = [new LessonIntroBeat(Findings.Get(lesson.Id)), .. lesson.Beats] }),
            new("Closing", "Report to the board", null, [new ClosingBeat()]),
            new("Index", "Index of lessons", null, [index]),
        ];
    }
}
