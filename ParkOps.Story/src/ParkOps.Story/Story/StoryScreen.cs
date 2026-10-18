using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Rendering;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace ParkOps.Story.Story;

/// <summary>
/// Plays the report. It opens on the executive summary; a lesson plays one beat at a time, NEXT walking through each
/// beat's steps first, and finishing a lesson returns to the summary with the next one selected. Forward moves wipe
/// in; going back lands on the previous beat already finished. Row 43 is the story bar (where are we, what's next).
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

    /// <summary>How many times a finished lesson has handed back to the summary.</summary>
    public int ReturnsToSummary { get; private set; }

    /// <summary>Back on the summary after finishing a lesson: a full pass through the report.</summary>
    public bool AtEnd => current == 0 && ReturnsToSummary > 0;

    /// <summary>A file-name-friendly name for where we are, e.g. "05-INC-002-Fix-2".</summary>
    public string Place => $"{current + 1:00}-{(Chapter.Id is { Length: > 0 } id ? id : "Summary")}-{Beat.Label}-{step + 1}";

    private void Go(int index, bool forward)
    {
        current = index;
        step = forward ? 0 : Beat.Steps - 1;
        stepStarts = new TimeSpan[Beat.Steps];
        for (var i = 0; i < stepStarts.Length; i++) stepStarts[i] = forward ? now : now - Finished;
        transitionFrom = forward ? now : null;
        Beat.Enter();
    }

    /// <summary>NEXT: the beat's next step, then its next beat; the end of a lesson returns to the summary.</summary>
    public void Next()
    {
        if (Beat.HandleKey(Keys.Enter, this)) return;
        if (step < Beat.Steps - 1)
        {
            step++;
            stepStarts[step] = now;
        }
        else if (current < beats.Count - 1 && beats[current + 1].Chapter == Chapter) Go(current + 1, forward: true);
        else if (Chapter.Number is { } lesson)
        {
            ReturnsToSummary++;
            ToSummary(selectLesson: lesson + 1);
        }
    }

    /// <summary>BACK: the previous step, then the previous beat; back from a lesson's first page is the summary.</summary>
    private void Back()
    {
        if (step > 0) step--;
        else if (current > 0 && beats[current - 1].Chapter == Chapter) Go(current - 1, forward: false);
        else if (Chapter.Number is { } lesson) ToSummary(selectLesson: lesson);
    }

    private void ToSummary(int selectLesson)
    {
        if (beats[0].Beat is Beats.SummaryBeat summary) summary.Select(selectLesson);
        Go(0, forward: true);
    }

    private void Replay() => Go(current, forward: true);

    public bool HasLesson(int lesson) => beats.Any(b => b.Chapter.Number == lesson);

    public void OpenLesson(int lesson)
    {
        var index = beats.FindIndex(b => b.Chapter.Number == lesson);
        if (index >= 0) Go(index, forward: true);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.Right) || keyboard.IsKeyPressed(Keys.Space) || keyboard.IsKeyPressed(Keys.PageDown) || keyboard.IsKeyPressed(Keys.Enter)) Next();
        else if (keyboard.IsKeyPressed(Keys.Left) || keyboard.IsKeyPressed(Keys.Back) || keyboard.IsKeyPressed(Keys.PageUp)) Back();
        else if (keyboard.IsKeyPressed(Keys.Up)) Beat.HandleKey(Keys.Up, this);
        else if (keyboard.IsKeyPressed(Keys.Down)) Beat.HandleKey(Keys.Down, this);
        else if (keyboard.IsKeyPressed(Keys.Home)) ToSummary(selectLesson: Chapter.Number ?? 2);
        else if (keyboard.IsKeyPressed(Keys.Tab)) Beat.Toggle();
        else if (keyboard.IsKeyPressed(Keys.R)) Replay();
        else if (keyboard.IsKeyPressed(Keys.Escape)) Game.Instance.MonoGameInstance.Exit();
        else
        {
            for (var digit = 0; digit <= 9; digit++)
            {
                if (keyboard.IsKeyPressed(Keys.D0 + digit) || keyboard.IsKeyPressed(Keys.NumPad0 + digit))
                {
                    var lesson = digit == 0 ? 10 : digit;
                    if (HasLesson(lesson)) OpenLesson(lesson);
                    else ToSummary(selectLesson: lesson);
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

        var x = 17;
        var title = chapter.Title;
        canvas.Print(x, y, title, Brand.WhiteShade, Brand.Black);
        x += title.Length + 2;

        foreach (var (index, b) in chapter.Beats.Index())
        {
            var isCurrent = b == beat;
            var done = chapter.Beats.IndexOf(beat) > index;
            var (fg, bg) = isCurrent ? (Brand.Black, Brand.Green) : done ? (Brand.Green, Brand.Black) : (Brand.LineNumber, Brand.Black);
            var label = done ? $"{Glyphs.Check}{b.Label}" : b.Label;
            canvas.Print(x, y, $" {label} ", fg, bg);
            x += label.Length + 2;
            if (isCurrent && b.Steps > 1)
            {
                for (var s = 0; s < b.Steps; s++) canvas.Glyph(x + s, y, s <= step ? Cp437.Bullet : Cp437.MiddleDot, s <= step ? Brand.Green : Brand.LineNumber, Brand.Black);
                x += b.Steps;
            }
            x++;
        }

        var keys = $"{beat.Hint ?? "HOME summary"}   {Glyphs.ArrowLeft} {Glyphs.ArrowRight} ";
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
