using Microsoft.Xna.Framework.Graphics;
using SadConsole;

namespace ParkOps.Story.Story;

/// <summary>
/// --capture DIR: plays the whole deck unattended on a fast clock and saves a PNG of every step, at a few
/// moments into its animation. A rehearsal you can flip through, and a check that every beat still draws.
/// </summary>
public sealed class Rehearsal(StoryScreen screen, string directory)
{
    private const double Speed = 4;
    private static readonly TimeSpan[] Moments = [TimeSpan.FromSeconds(1.1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(7)];

    private TimeSpan inState;
    private int moment;
    private bool due;

    public void Start()
    {
        Directory.CreateDirectory(directory);
        screen.TimeScale = Speed;
        Game.Instance.FrameUpdate += (_, host) =>
        {
            inState += host.UpdateFrameDelta * Speed;
            // Live work (queries, streams, model calls) runs on the wall clock, not the 4x story clock: hold the last
            // shot of each step until it settles, for up to 20 real seconds.
            var waiting = moment == Moments.Length - 1 && !screen.Settled && inState < Moments[moment] + TimeSpan.FromSeconds(20 * Speed);
            if (inState >= Moments[moment] && !waiting) due = true;
        };
        Game.Instance.FrameRender += (_, _) =>
        {
            if (!due) return;
            due = false;
            Save($"{screen.Place}@{Moments[moment].TotalSeconds:0.0}s.png");
            if (++moment < Moments.Length) return;

            moment = 0;
            inState = TimeSpan.Zero;
            if (screen.AtEnd) Game.Instance.MonoGameInstance.Exit();
            else screen.Next();
        };
    }

    private void Save(string name)
    {
        var output = SadConsole.Host.Global.RenderOutput;
        using var file = File.Create(Path.Combine(directory, name));
        output.SaveAsPng(file, output.Width, output.Height);
    }
}
