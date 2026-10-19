using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Story.Beats;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;
using Closed = ParkOps.Story.Fixed.Inc004;

namespace ParkOps.Story.Chapters.Inc004;

/// <summary>
/// Exhibit for lesson 4: F2 ANIMALS' feeding plan, built by ParkOps.Start's FeedingPlanner over an open hierarchy (with
/// the genetics lab's own Procompsognathus) or the fixed one over a closed hierarchy. Beside it, each species' own
/// IsCarnivore, because the open version's sanity check passes: the lab's species says it isn't one.
/// </summary>
public sealed class FeedingExhibit(Finding finding, CodePath initial, Page page) : ExhibitBeat(finding, initial, page)
{
    private static readonly TimeSpan StepTime = TimeSpan.FromMilliseconds(300);
    private Readout readout = null!;
    private (string Species, bool Carnivore, string DefinedIn)[] species = [];

    protected override string ScreenName => "CONTROL ROOM // F2 ANIMALS";

    protected override string Heading(CodePath path) =>
        path == CodePath.Before ? "Thirty-one carnivores on lettuce" : "Every species known, every diet checked";

    protected override string Caption(CodePath path) =>
        "Control room, F2 ANIMALS, FEEDING PLAN. Diets assigned live by " +
        (path == CodePath.Before ? "ParkOps.Start's FeedingPlanner (open hierarchy)." : "the fixed FeedingPlanner (closed hierarchy).");

    protected override (string Code, string From) CodeFor(CodePath path) => path == CodePath.Before
        ? ("abstract record + _ arm", "ParkOps.Start")
        : ("closed record", "ParkOps.Story.Fixed");

    protected override string Reading(CodePath path) => path == CodePath.Before
        ? "The genetics lab declares Procompsognathus in its own assembly, says it isn't a carnivore, and FeedingPlanner's " +
          "discard arm puts the herd on lettuce. The sanity check passes, because it believes the lab. Nothing in Core knew " +
          "the species existed."
        : "Dinosaur is closed, so only Core can declare species: the compies are defined there, as carnivores. The switch " +
          "lists every species and has no discard arm, so they get small prey, and a species added later won't compile " +
          "until someone plans its diet.";

    protected override void Run(CodePath path)
    {
        if (path == CodePath.Before)
        {
            List<Dinosaur> roster = [.. ParkRoster.CoreAnimals(), .. ParkOps.Genetics.Plugin.WuLab.NewArrivals()];
            readout = ParkOps.ControlRoom.Panels.FeedingPanel.Render(roster);
            species = [.. roster.GroupBy(d => d.GetType()).Select(g => (g.Key.Name, g.First().IsCarnivore, g.Key.Assembly.GetName().Name!.Replace("ParkOps.", "")))];
        }
        else
        {
            var roster = Roster();
            readout = Closed.FeedingPanel.Render(roster);
            species = [.. roster.GroupBy(d => d.GetType()).Select(g => (g.Key.Name, g.First().IsCarnivore, "Core"))];
        }
    }

    /// <summary>The same animals, as the closed hierarchy's types: Core's roster plus the lab's arrivals.</summary>
    private static List<Closed.Dinosaur> Roster() =>
    [
        .. ParkRoster.CoreAnimals().Select<Dinosaur, Closed.Dinosaur>(d => d switch
        {
            Velociraptor => new Closed.Velociraptor(d.Tag),
            TyrannosaurusRex => new Closed.TyrannosaurusRex(d.Tag),
            Dilophosaurus => new Closed.Dilophosaurus(d.Tag),
            Brachiosaurus => new Closed.Brachiosaurus(d.Tag),
            Triceratops => new Closed.Triceratops(d.Tag),
            Gallimimus => new Closed.Gallimimus(d.Tag),
            _ => throw new InvalidOperationException($"No closed counterpart for {d.GetType().Name}"),
        }),
        .. Closed.WuLab.NewArrivals(),
    ];

    protected override bool IsBusy(TimeSpan running) => running < StepTime * (Rerunning ? 3 : species.Length + 1);

    protected override ScreenState DrawScreen(Workstation ws, Rectangle inner, ExhibitClock clock)
    {
        var area = new Rectangle(inner.X + 1, inner.Y, 40, inner.Height);
        var revealFrom = clock.Started + StepTime * (Rerunning ? 3 : species.Length + 1);
        var typed = clock.Now - revealFrom > Workstation.RevealTime(readout);
        var alertOn = ws.Blink || !typed;
        var compy = readout.Lines.Index().First(l => l.Item.Text.StartsWith("Procompsognathus")).Index;

        if (clock.Busy) Rebuild(ws, area, clock.Running);
        else ws.Readout(readout, "FEEDING PLAN", "Diet per species: carnivores need meat", area, revealFrom, alertOn);

        // Each species, as its own type describes itself, and who declared it.
        var right = new Rectangle(inner.X + 43, inner.Y, inner.Width - 44, inner.Height);
        ws.Chip(right.X, right.Y, "SPECIES SAYS", Palette.Ink, Palette.Amber);
        ws.Print(right.X, right.Y + 1, "Each type's own answer", Palette.Dim);
        ws.Print(right.X, right.Y + 2, $"{"SPECIES",-17}{"MEAT",-5}FROM", Palette.Dim);
        foreach (var (index, (name, carnivore, from)) in species.Index())
        {
            var y = right.Y + 3 + index;
            var lab = from.Contains("Genetics");
            ws.Print(right.X, y, Workstation.Clip(name, 16), lab ? Palette.Amber : Palette.Text);
            ws.Print(right.X + 17, y, carnivore ? "yes" : "no", carnivore ? Palette.Bad : Palette.Good);
            ws.Print(right.X + 22, y, lab ? "lab" : "Core", lab ? Palette.Amber : Palette.Dim);
        }

        List<Rectangle> marks = [Row(area.X, area.Y + 2 + compy, area.Width), Row(right.X, right.Y + 2 + compy, right.Width)];
        return clock.Busy ? new("ASSIGNING", ws.Blink ? Palette.Amber : Palette.AmberDim, [])
            : readout.Status == PanelStatus.Alert ? new("CHECK FAILED", alertOn ? Palette.Bad : Palette.BadDark, marks)
            : ScreenState.Clear(marks);
    }

    /// <summary>The control room's FEEDING PLAN animation: a cursor walks each species while its diet is assigned.</summary>
    private void Rebuild(Workstation ws, Rectangle area, TimeSpan elapsed)
    {
        const int planColumn = 29; // "{species,-20}{head,6}   " in FeedingPanel
        var x = ws.Chip(area.X, area.Y, "FEEDING PLAN", Palette.Ink, Palette.Amber);
        ws.Print(x, area.Y, $" {Cp437.Bullet} REBUILDING ", ws.Blink ? Palette.Amber : Palette.AmberDim);
        ws.Print(area.X, area.Y + 1, "Assigning a diet to each species in turn", Palette.Dim);

        var current = (int)(elapsed / (Rerunning ? StepTime * 0.35 : StepTime));
        foreach (var (index, line) in readout.Lines.Index())
        {
            var y = area.Y + 2 + index;
            var row = index - 1; // line 0 is the column header
            if (row < 0 || row < current) ws.Print(area.X, y, line.Text, Palette.For(line.Tone));
            else if (row == current) ws.Print(area.X, y, $"{line.Text[..planColumn]}assigning {ws.Spinner(90)}".PadRight(area.Width), Palette.Ink, Palette.Amber);
            else ws.Print(area.X, y, $"{line.Text[..planColumn]}...", Palette.Dim);
        }
    }
}
