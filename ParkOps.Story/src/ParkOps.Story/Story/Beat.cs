using ParkOps.Story.Rendering;
using SadConsole.Input;
using SadRogue.Primitives;

namespace ParkOps.Story.Story;

/// <summary>
/// One screen of the story. A beat can have several <see cref="Steps"/> (callouts, reveals) that NEXT walks
/// through before moving on. Drawing is a pure function of the <see cref="Frame"/> clock, so going back,
/// replaying and jumping all just reset times.
/// </summary>
public abstract class Beat
{
    /// <summary>Short label for the story bar: Finding, Before, Code, Fix, After, Resolved.</summary>
    public abstract string Label { get; }

    public virtual int Steps => 1;

    /// <summary>The colour the transition wipes in with.</summary>
    public virtual Color Backdrop => Brand.Paper;

    /// <summary>Called whenever the beat becomes current (forward, back or jump).</summary>
    public virtual void Enter() { }

    /// <summary>TAB: flip the active code path, if this beat has one. Returns whether anything changed.</summary>
    public virtual bool Toggle() => false;

    /// <summary>False while the beat is still waiting on live work (a query, a stream, a model call).</summary>
    public virtual bool Settled => true;

    /// <summary>A key the beat handles itself (before NEXT/BACK see it). Returns whether it was used.</summary>
    public virtual bool HandleKey(Keys key, IStory story) => false;

    /// <summary>Extra hint for the story bar, e.g. "TAB before/after".</summary>
    public virtual string? Hint => null;

    public abstract void Draw(Frame frame);
}

/// <summary>Everything a beat needs to draw itself at this instant.</summary>
public sealed class Frame(Canvas canvas, TimeSpan now, TimeSpan[] stepStarts, int step)
{
    public Canvas Canvas => canvas;

    /// <summary>The app clock; use it for blinking and spinners.</summary>
    public TimeSpan Now => now;

    public int Step => step;

    /// <summary>Time since the beat was entered. Large if it was entered by going back (animations finished).</summary>
    public TimeSpan Elapsed => now - stepStarts[0];

    /// <summary>Time since <paramref name="index"/> was reached; negative if it hasn't been yet.</summary>
    public TimeSpan Since(int index) => index <= step ? now - stepStarts[index] : TimeSpan.FromHours(-1);

    public bool Reached(int index) => index <= step;
}

/// <summary>What a beat can ask of the story: open a lesson from the summary.</summary>
public interface IStory
{
    bool HasLesson(int lesson);

    void OpenLesson(int lesson);
}

public sealed record Chapter(string Id, string Title, int? Number, IReadOnlyList<Beat> Beats);

/// <summary>Numbers report pages as the deck is built; the total is read at draw time, once the deck is complete.</summary>
public sealed class Pages
{
    public int Total { get; private set; }

    public Page Next() => new(this, ++Total);
}

public readonly record struct Page(Pages Of, int Number);
