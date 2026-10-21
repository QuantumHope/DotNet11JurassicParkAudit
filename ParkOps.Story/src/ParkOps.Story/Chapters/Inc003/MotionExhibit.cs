using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Story.Beats;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;
using Start = ParkOps.Core.Sensors;
using Union = ParkOps.Story.Fixed.Inc003;

namespace ParkOps.Story.Chapters.Inc003;

/// <summary>
/// Exhibit for lesson 3: F3 SECURITY's motion sensors, assessed by the original ThreatAssessor (a wrapper hierarchy
/// with a discard arm) or the fixed one (a union, switched exhaustively). Below the readout, each sensor's raw contact
/// sits beside what ParkOps made of it, so the sweep shows the sensor knew all along.
/// </summary>
public sealed class MotionExhibit(Finding finding, CodePath initial, Page page) : ExhibitBeat(finding, initial, page)
{
    private Readout readout = null!;
    private (string Location, string Saw, string Says, bool Wrong)[] contacts = [];

    protected override string ScreenName => "CONTROL ROOM // F3 SECURITY";

    protected override string Heading(CodePath path) =>
        path == CodePath.Before ? "A raptor, assessed as a friendly guest" : "Every contact, named and assessed";

    protected override string Caption(CodePath path) =>
        "Control room, F3 SECURITY, SWEEP SENSORS. Threats assessed live by " +
        (path == CodePath.Before ? "the original ThreatAssessor (wrapper records)." : "the fixed ThreatAssessor (a union).");

    protected override (string Code, string From) CodeFor(CodePath path) => path == CodePath.Before
        ? ("_ => \"GUEST detected\"", "ParkOps (original)")
        : ("union MotionContact", "ParkOps.Story.Fixed");

    protected override string Reading(CodePath path) => path == CodePath.Before
        ? "The Paddock 9 sensor reported a Velociraptor, V-07. ThreatAssessor has no arm for DinosaurContact, so the " +
          "discard arm calls it a friendly guest, threat NONE. The real guest at the Visitor Center gets the same label: " +
          "both just fell through to _."
        : "MotionContact is now a union of the four types the sensors report. The switch has to cover all four, so the " +
          "raptor is CRITICAL and Donald Gennaro is a guest by name. A fifth case added later won't compile quietly.";

    protected override void Run(CodePath path)
    {
        if (path == CodePath.Before)
        {
            var pings = Start.SensorFeed.Current();
            readout = ParkOps.ControlRoom.Panels.MotionPanel.Render(pings);
            contacts = [.. pings.Select(p =>
            {
                var says = Start.ThreatAssessor.Assess(p.Contact);
                return (p.Location, Describe(p.Contact.Value), says.Label, Wrong: p.Contact.Value is Dinosaur);
            })];
        }
        else
        {
            var pings = Union.SensorFeed.Current();
            readout = Union.MotionPanel.Render(pings);
            contacts = [.. pings.Select(p => (p.Location, Describe(Unwrap(p.Contact)), Union.ThreatAssessor.Assess(p.Contact).Label, Wrong: false))];
        }
    }

    private static object Unwrap(Union.MotionContact contact) => contact switch
    {
        Dinosaur d => d,
        Guest g => g,
        TourVehicle v => v,
        Employee e => e,
    };

    private static string Describe(object value) => value switch
    {
        Dinosaur d => $"{d.GetType().Name} {d.Tag}",
        Guest g => $"Guest {g.Name}",
        TourVehicle v => $"Vehicle {v.CallSign}",
        Employee e => $"Employee {e.Name}",
        _ => value.GetType().Name,
    };

    protected override ScreenState DrawScreen(Workstation ws, Rectangle inner, ExhibitClock clock)
    {
        var area = new Rectangle(inner.X + 1, inner.Y, inner.Width - 2, 8);
        var revealFrom = clock.Started + (Rerunning ? TimeSpan.FromMilliseconds(900) : TimeSpan.FromMilliseconds(2600));
        var typed = clock.Now - revealFrom > Workstation.RevealTime(readout);
        var alertOn = ws.Blink || !typed;

        if (clock.Busy) Sweep(ws, area, clock);
        else ws.Readout(readout, "MOTION SENSORS", "Latest contact per sensor, with threat level", area, revealFrom, alertOn);

        // What each sensor actually reported, beside what ParkOps made of it.
        var y = inner.Y + 10;
        ws.Chip(area.X, y, "RAW CONTACTS", Palette.Ink, Palette.Amber);
        ws.Print(area.X + 15, y, "what the sensor reported vs. what ParkOps says", Palette.Dim);
        ws.Print(area.X, y + 2, $"{"SENSOR",-15}{"REPORTED",-26}PARKOPS SAYS", Palette.Dim);
        List<Rectangle> marks = [Row(area.X, area.Y + 3, area.Width)];
        foreach (var (index, (location, saw, says, wrong)) in contacts.Index())
        {
            var row = y + 3 + index;
            var shown = !clock.Busy || clock.Running > TimeSpan.FromMilliseconds(500 + 450 * index) * (Rerunning ? 0.35 : 1);
            if (!shown) continue;
            ws.Print(area.X, row, $"{location,-15}", Palette.Text);
            ws.Print(area.X + 15, row, $"{saw,-26}", Palette.Strong);
            ws.Print(area.X + 41, row, Workstation.Clip(says, area.Width - 41), wrong ? Palette.Bad : Palette.Good);
            if (index == 0) marks.Add(Row(area.X, row, area.Width));
        }

        return clock.Busy ? new("SWEEPING", ws.Blink ? Palette.Amber : Palette.AmberDim, [])
            : readout.Status == PanelStatus.Alert ? new("INTRUSION", alertOn ? Palette.Bad : Palette.BadDark, marks)
            : ScreenState.Clear(marks);
    }

    /// <summary>While sweeping: a radar line across the readout area and a poll counter.</summary>
    private void Sweep(Workstation ws, Rectangle area, ExhibitClock clock)
    {
        var x = ws.Chip(area.X, area.Y, "MOTION SENSORS", Palette.Ink, Palette.Amber);
        ws.Print(x, area.Y, $" {Cp437.Bullet} SWEEPING ", ws.Blink ? Palette.Amber : Palette.AmberDim);
        ws.Print(area.X, area.Y + 1, "Polling every motion sensor", Palette.Dim);
        var position = (int)(clock.Running.TotalMilliseconds / 12) % area.Width;
        for (var row = area.Y + 3; row < area.Y + 7; row++)
        {
            ws.Print(area.X + position, row, $"{Cp437.FullBlock}", Palette.Amber);
            if (position > 0) ws.Print(area.X + position - 1, row, $"{Cp437.MediumShade}", Palette.AmberDim);
        }
    }
}
