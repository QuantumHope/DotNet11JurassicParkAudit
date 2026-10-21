using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace ParkOps.Story.Story;

/// <summary>
/// Plays the talk as a slideshow: title, each lesson in order, closing, then the index. NEXT walks each beat's steps,
/// then the next beat, straight through from one lesson into the next; BACK does the same in reverse. HOME jumps to
/// the index, where ENTER opens any lesson. Forward moves wipe in; going back lands on the previous beat already
/// finished. Row 43 is the story bar (where are we, what's next).
/// </summary>
public sealed class StoryScreen : ScreenSurface, IStory
{
    private static readonly TimeSpan FrameTime = TimeSpan.FromMilliseconds(33);
    private static readonly TimeSpan TransitionTime = TimeSpan.FromMilliseconds(260);
    private static readonly TimeSpan Finished = TimeSpan.FromHours(1);

    private readonly List<(Chapter Chapter, Beat Beat)> beats;
    private readonly ScreenSurface overlay;
    private readonly Canvas canvas;

    private int current;
    private int step;
    private TimeSpan[] stepStarts = [];
    private TimeSpan? transitionFrom;
    private TimeSpan now;
    private TimeSpan sinceFrame;

    public StoryScreen(IReadOnlyList<Chapter> chapters, int startAt = 0) : base(Canvas.Width, Canvas.Height)
    {
        beats = [.. chapters.SelectMany(c => c.Beats.Select(b => (c, b)))];

        // Big headings: a transparent surface at twice the font size, laid over the whole screen.
        overlay = new ScreenSurface(Canvas.Width / 2, Canvas.Height / 2);
        overlay.FontSize = overlay.Font.GetFontSize(IFont.Sizes.Two);
        overlay.Surface.DefaultBackground = Color.Transparent;
        overlay.Surface.Clear();
        Children.Add(overlay);

        canvas = new Canvas(Surface, overlay.Surface);
        Go(Math.Clamp(startAt, 0, beats.Count - 1), forward: true);
    }

    private Beat Beat => beats[current].Beat;

    private Chapter Chapter => beats[current].Chapter;

    /// <summary>Speeds up the story clock (rehearsal capture). Animations are functions of the clock, so they just run faster.</summary>
    public double TimeScale { get; set; } = 1;

    /// <summary>Whether the current beat has finished any live work it started.</summary>
    public bool Settled => Beat.Settled;

    /// <summary>Lessons NEXT has carried us out of, for <see cref="StopAfterLessons"/>.</summary>
    private int lessonsFinished;

    /// <summary>The index of lessons: the deck's last beat.</summary>
    private int IndexBeat => beats.Count - 1;

    private bool OnLastStepOfLesson =>
        Chapter.Number is not null && step == Beat.Steps - 1 && (current == beats.Count - 1 || beats[current + 1].Chapter != Chapter);

    /// <summary>
    /// The rehearsal is done: on the index after walking the whole deck, or on the last step of the last lesson asked
    /// for by <see cref="StopAfterLessons"/>.
    /// </summary>
    public bool AtEnd => StopAfterLessons is { } lessons
        ? OnLastStepOfLesson && lessonsFinished + 1 >= lessons
        : current == IndexBeat;

    /// <summary>Rehearsal of a single lesson: stop at the end of this many lessons.</summary>
    public int? StopAfterLessons { get; set; }

    /// <summary>--fullscreen: switch to fullscreen on the first frame, once the window exists (same as F11).</summary>
    public bool StartFullScreen { get; set; }

    /// <summary>A file-name-friendly name for where we are, e.g. "05-INC-002-Fix-2".</summary>
    public string Place => $"{current + 1:00}-{Chapter.Id}-{Beat.Label}-{step + 1}";

    private void Go(int index, bool forward)
    {
        current = index;
        step = forward ? 0 : Beat.Steps - 1;
        stepStarts = new TimeSpan[Beat.Steps];
        for (var i = 0; i < stepStarts.Length; i++) stepStarts[i] = forward ? now : now - Finished;
        transitionFrom = forward ? now : null;
        Beat.Enter();
    }

    /// <summary>NEXT: the beat's next step, then the next beat, carrying straight on into the next lesson.</summary>
    public void Next()
    {
        if (Beat.HandleKey(Keys.Enter, this)) return;
        if (step < Beat.Steps - 1)
        {
            step++;
            stepStarts[step] = now;
        }
        else if (current < beats.Count - 1)
        {
            if (OnLastStepOfLesson) lessonsFinished++;
            if (current + 1 == IndexBeat && beats[IndexBeat].Beat is Beats.SummaryBeat index) index.Select(1);
            Go(current + 1, forward: true);
        }
    }

    /// <summary>BACK: the previous step, then the previous beat, back across lessons to the title slide.</summary>
    private void Back()
    {
        if (step > 0) step--;
        else if (current > 0) Go(current - 1, forward: false);
    }

    /// <summary>HOME: the index of lessons, with <paramref name="selectLesson"/> highlighted.</summary>
    private void ToIndex(int selectLesson)
    {
        if (beats[IndexBeat].Beat is Beats.SummaryBeat index) index.Select(selectLesson);
        Go(IndexBeat, forward: true);
    }

    private void Replay() => Go(current, forward: true);

    public bool HasLesson(int lesson) => beats.Any(b => b.Chapter.Number == lesson);

    /// <summary>PAGE DOWN: the next lesson's section slide (from the title, lesson 1; after the last lesson, the closing slide).</summary>
    private void NextLesson()
    {
        var next = beats.FindIndex(current + 1, b => b.Chapter != Chapter && b.Chapter.Number is not null);
        if (next < 0) next = beats.FindIndex(b => b.Chapter.Id == "Closing");
        if (next > current) Go(next, forward: true);
    }

    /// <summary>PAGE UP: back to the start of this lesson, or from its section slide to the previous lesson's.</summary>
    private void PreviousLesson()
    {
        var start = beats.FindIndex(b => b.Chapter == Chapter);
        if (Chapter.Number is not null && (current > start || step > 0))
        {
            Go(start, forward: true);
            return;
        }
        var previous = beats.FindLastIndex(start - 1 < 0 ? 0 : start - 1, b => b.Chapter.Number is not null);
        if (previous < 0) Go(0, forward: true);
        else Go(beats.FindIndex(b => b.Chapter == beats[previous].Chapter), forward: true);
    }

    public void OpenLesson(int lesson)
    {
        var index = beats.FindIndex(b => b.Chapter.Number == lesson);
        if (index >= 0) Go(index, forward: true);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.Right) || keyboard.IsKeyPressed(Keys.Space) || keyboard.IsKeyPressed(Keys.Enter)) Next();
        else if (keyboard.IsKeyPressed(Keys.Left) || keyboard.IsKeyPressed(Keys.Back)) Back();
        else if (keyboard.IsKeyPressed(Keys.PageDown)) NextLesson();
        else if (keyboard.IsKeyPressed(Keys.PageUp)) PreviousLesson();
        else if (keyboard.IsKeyPressed(Keys.Up)) Beat.HandleKey(Keys.Up, this);
        else if (keyboard.IsKeyPressed(Keys.Down)) Beat.HandleKey(Keys.Down, this);
        else if (keyboard.IsKeyPressed(Keys.Home)) ToIndex(selectLesson: Chapter.Number ?? 1);
        else if (keyboard.IsKeyPressed(Keys.Tab)) Beat.Toggle();
        else if (keyboard.IsKeyPressed(Keys.R)) Replay();
        else if (keyboard.IsKeyPressed(Keys.F11)) Game.Instance.ToggleFullScreen();
        else if (keyboard.IsKeyPressed(Keys.Escape)) Game.Instance.MonoGameInstance.Exit();
        else
        {
            for (var digit = 0; digit <= 9; digit++)
            {
                if (keyboard.IsKeyPressed(Keys.D0 + digit) || keyboard.IsKeyPressed(Keys.NumPad0 + digit))
                {
                    var lesson = digit == 0 ? 10 : digit;
                    if (HasLesson(lesson)) OpenLesson(lesson);
                    else ToIndex(selectLesson: lesson);
                    break;
                }
            }
        }
        return true;
    }

    public override bool ProcessMouse(MouseScreenObjectState state)
    {
        if (state.Mouse.LeftClicked) Next();
        else if (state.Mouse.RightClicked) Back();
        return true;
    }

    public override void Update(TimeSpan delta)
    {
        if (StartFullScreen)
        {
            StartFullScreen = false;
            Game.Instance.ToggleFullScreen();
        }
        delta *= TimeScale;
        now += delta;
        sinceFrame += delta;
        if (sinceFrame >= FrameTime)
        {
            sinceFrame = TimeSpan.Zero;
            Repaint();
        }
        base.Update(delta);
    }

    private void Repaint()
    {
        canvas.Clear(Beat.Backdrop);
        Beat.Draw(new Frame(canvas, now, stepStarts, step));
        Transition();
        StoryBar();
        IsDirty = true;
        overlay.IsDirty = true;
    }

    /// <summary>Forward moves sweep in left to right behind a green edge, in the Leading EDJE accent.</summary>
    private void Transition()
    {
        if (transitionFrom is not { } from) return;
        var progress = (now - from) / TransitionTime;
        if (progress >= 1)
        {
            transitionFrom = null;
            return;
        }
        var eased = 1 - Math.Pow(1 - progress, 3);
        var edge = (int)(eased * Canvas.Width);
        canvas.Fill(new Rectangle(edge, 0, Canvas.Width - edge, Canvas.ContentHeight), Beat.Backdrop, Beat.Backdrop);
        overlay.Surface.Fill(new Rectangle(edge / 2, 0, overlay.Surface.Width - edge / 2, overlay.Surface.Height), Color.Transparent, Color.Transparent, 0);
        canvas.Fill(new Rectangle(edge, 0, 2, Canvas.ContentHeight), Brand.Green, Brand.Green);
    }

    /// <summary>LEADING EDJE · chapter · one pill per beat · position and keys.</summary>
    private void StoryBar()
    {
        const int y = Canvas.Height - 1;
        var (chapter, beat) = beats[current];
        canvas.Fill(new Rectangle(0, y, Canvas.Width, 1), Brand.WhiteShade, Brand.Black);
        canvas.Glyph(1, y, Glyphs.BlockLeft, Brand.Green, Brand.Black);
        canvas.Print(3, y, "LEADING", Brand.TrueWhite, Brand.Black);
        canvas.Print(11, y, "EDJE", Brand.Green, Brand.Black);

        // Right: hint and arrows. Middle: one pill per beat. Left: the chapter title, clipped to whatever room is left.
        var keys = $"{beat.Hint ?? "HOME index"}   {Glyphs.ArrowLeft} {Glyphs.ArrowRight} ";
        var pills = chapter.Beats.Select((b, index) =>
        {
            var done = chapter.Beats.IndexOf(beat) > index;
            return (Beat: b, Label: b.Label, Done: done, Width: b.Label.Length + 2 + (b == beat && b.Steps > 1 ? b.Steps + 1 : 0));
        }).ToList();
        var pillsWidth = pills.Sum(p => p.Width);
        if (17 + 12 + pillsWidth + keys.Length > Canvas.Width) keys = $"{Glyphs.ArrowLeft} {Glyphs.ArrowRight} ";
        var titleWidth = Math.Max(8, Canvas.Width - keys.Length - pillsWidth - 17 - 2);

        var x = 17;
        var title = Workstation.Clip(chapter.Title, titleWidth);
        canvas.Print(x, y, title, Brand.WhiteShade, Brand.Black);
        x += title.Length + 2;

        foreach (var pill in pills)
        {
            var isCurrent = pill.Beat == beat;
            var (fg, bg) = isCurrent ? (Brand.Black, Brand.Green) : pill.Done ? (Brand.Green, Brand.Black) : (Brand.LineNumber, Brand.Black);
            canvas.Print(x, y, $" {pill.Label} ", fg, bg);
            x += pill.Label.Length + 2;
            if (isCurrent && pill.Beat.Steps > 1)
            {
                for (var s = 0; s < pill.Beat.Steps; s++) canvas.Glyph(x + s, y, s <= step ? Cp437.Bullet : Cp437.MiddleDot, s <= step ? Brand.Green : Brand.LineNumber, Brand.Black);
                x += pill.Beat.Steps + 1;
            }
        }

        canvas.Print(Canvas.Width - keys.Length, y, keys, Brand.LineNumber, Brand.Black);
    }
}

file static class ListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
    {
        for (var i = 0; i < list.Count; i++)
            if (EqualityComparer<T>.Default.Equals(list[i], item)) return i;
        return -1;
    }
}
