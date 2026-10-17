using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using ParkOps.ControlRoom.Feed;
using ParkOps.ControlRoom.Fences;
using ParkOps.ControlRoom.Panels;
using ParkOps.Core.Animals;
using ParkOps.Core.Mapping;
using ParkOps.Core.Sensors;
using ParkOps.Data;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;

namespace ParkOps.ControlRoom.Ui;

public enum Tab { Overview, Animals, Security, Operations }

public enum QuickLook { Sensors, Fences, Feeding, Repairs }

/// <summary>
/// The control-room workstation: a tab bar across the top, a big viewport on the left with its
/// LEVEL caption, and a quick-look box, command grid and system-status board on the right.
/// Everything is repainted every frame from an animation clock, so blinking, sweeps and
/// typewriter reveals are all just functions of <see cref="now"/>. Anything drawn can register a
/// <see cref="Hotspot"/> to get a hover hint, a highlight and a click action.
/// </summary>
public sealed class ControlRoomScreen : ScreenSurface
{
    public const int ScreenWidth = 128;
    public const int ScreenHeight = 44;

    // Layout (in cells)
    private static readonly Rectangle Viewport = new(1, 6, 74, 28);
    private static readonly Rectangle Caption = new(1, 34, 74, 5);
    private static readonly Rectangle QuickLookBox = new(76, 6, 51, 15);
    private static readonly Rectangle CommandBox = new(76, 21, 51, 7);
    private static readonly Rectangle StatusBoard = new(76, 28, 51, 11);
    private static readonly Rectangle StatusList = new(StatusBoard.X + 2, StatusBoard.Y + 3, StatusBoard.Width - 4, StatusBoard.Height - 5);
    private static readonly Rectangle ScreenBar = new(1, 39, 74, 3);
    private static readonly Rectangle MessageBar = new(76, 39, 51, 3);
    private const int NavRow = 2;
    private const int StatusRow = ScreenHeight - 1;

    // Animation
    private static readonly TimeSpan FrameTime = TimeSpan.FromMilliseconds(40);
    private static readonly TimeSpan WipeTime = TimeSpan.FromMilliseconds(280);
    private static readonly TimeSpan MinimumBusyTime = TimeSpan.FromMilliseconds(900);
    private const double TypewriterCharsPerMs = 2.2;

    private sealed record TabInfo(Tab Tab, string Key, string Name, char Level, string Title, string View, string Hint, QuickLook QuickLook);

    private static readonly TabInfo[] Tabs =
    [
        new(Tab.Overview, "F1", "OVERVIEW", 'G', "Animal Paddocks", "VIEW: isla nublar // all quadrants",
            "Paddock map and park-wide alerts", QuickLook.Sensors),
        new(Tab.Animals, "F2", "ANIMALS", 'A', "Animal Census", "VIEW: roster vs. motion grid",
            "Animal census and feeding plan", QuickLook.Feeding),
        new(Tab.Security, "F3", "SECURITY", 'S', "Security Grid", "VIEW: motion sensors // guest locator",
            "Motion sensors, guest locator, fence power", QuickLook.Fences),
        new(Tab.Operations, "F4", "OPERATIONS", 'O', "Park Operations", "VIEW: maintenance // raw telemetry",
            "Fence maintenance and raw sensor telemetry", QuickLook.Repairs),
    ];

    /// <summary>One line under each panel's title, so a first-time viewer knows what they're looking at.</summary>
    private static readonly Dictionary<string, string> Descriptions = new()
    {
        ["ANIMAL CENSUS"] = "Roster vs. the animals motion sensors detect",
        ["MOTION SENSORS"] = "Latest contact per sensor, with threat level",
        ["FEEDING PLAN"] = "Diet per species: carnivores need meat",
        ["FENCE STATUS"] = "Fence power, and the code path that cut it",
        ["GUEST LOCATOR"] = "Searches the paddock grid for missing guests",
        ["MAINTENANCE"] = "Fence with the most recorded breaches",
        ["SENSOR FEED // RAW TELEMETRY"] = "Raw JSON from the motion sensors, as transmitted",
    };

    private sealed record Command(string Label, string Hint, Action Execute);

    private sealed record Hotspot(Rectangle Area, string Hint, Action? OnClick);

    /// <summary>Commands with their own on-screen animation (beyond the SYS MSG progress bar).</summary>
    private enum Activity { Recount, FeedingPlan }

    private static readonly TimeSpan RecountTime = TimeSpan.FromMilliseconds(2600);
    private static readonly TimeSpan FeedingStepTime = TimeSpan.FromMilliseconds(330);
    private static readonly TimeSpan PressFlashTime = TimeSpan.FromMilliseconds(160);

    private readonly IReadOnlyList<Dinosaur> roster;
    private readonly ParkMap map;
    private readonly List<string> telemetry = [];
    private readonly ConcurrentQueue<string> incomingTelemetry = new();
    private readonly List<Hotspot> hotspots = [];

    private Tab tab;
    private QuickLook quickLook;
    private Readout? census, motion, feeding, fences, guests, maintenance;
    private HashSet<string> closedZones = [];
    private (string Text, Color Color) message = ("", Palette.Ink);
    private (string Label, TimeSpan Since, string? Source)? busy;
    private Command[] commands = [];
    private string? clickedButton;
    private (string Label, TimeSpan At)? pressFlash;
    private bool mouseDown;
    private (Activity Kind, TimeSpan Since)? activity;
    private DateTime? lastRecount, lastFeedingRebuild;
    private Point? mouse;
    private bool helpOpen;
    private bool alarmsAcknowledged;
    private string tickerText = "";
    private TimeSpan tickerSince;

    private TimeSpan now;
    private TimeSpan revealFrom;
    private TimeSpan quickLookRevealFrom;
    private TimeSpan wipeFrom;
    private TimeSpan sinceFrame;

    public ControlRoomScreen(IReadOnlyList<Dinosaur> roster, ParkMap map) : base(ScreenWidth, ScreenHeight)
    {
        this.roster = roster;
        this.map = map;
        Surface.DefaultBackground = Palette.Chrome;
        UseMouse = true;

        Show(Tab.Overview);
        Busy("CONNECTING TO PARK SYSTEMS", "ONLINE - PRESS H FOR HELP", RefreshAsync);
    }

    private bool Blink => now.TotalMilliseconds % 900 < 500;

    /// <summary>Alerts blink until acknowledged.</summary>
    private bool AlertOn => alarmsAcknowledged || Blink;

    private TabInfo Current => Tabs[(int)tab];

    private (string Name, Tab Tab, Readout? Readout)[] Systems =>
    [
        ("Animal census", Tab.Animals, census),
        ("Motion sensors", Tab.Security, motion),
        ("Feeding plan", Tab.Animals, feeding),
        ("Perimeter fences", Tab.Security, fences),
        ("Guest locator", Tab.Security, guests),
        ("Fence maintenance", Tab.Operations, maintenance),
    ];

    private IEnumerable<string> Faults =>
        Systems.Where(s => s.Readout?.Status == PanelStatus.Alert).Select(s => s.Name);

    // ---- Navigation and commands -------------------------------------------------------------

    private void Show(Tab next)
    {
        tab = next;
        quickLook = Current.QuickLook;
        wipeFrom = now;
        revealFrom = quickLookRevealFrom = now + WipeTime;

        commands = tab switch
        {
            Tab.Animals =>
            [
                new("RECOUNT", "Scan every rostered tag against the motion grid", Recount),
                new("FEEDING PLAN", "Rebuild the diet for every species, one at a time", RebuildFeedingPlan),
            ],
            Tab.Security =>
            [
                new("SWEEP SENSORS", "Re-poll every motion sensor and assess threats",
                    () => Busy("SWEEPING MOTION GRID", "MOTION GRID SWEPT", RefreshAsync)),
                new("LOCATE GUESTS", "Search the paddock grid for missing guests",
                    () => Busy("SEARCHING PADDOCK GRID", "GUEST SEARCH COMPLETE", RefreshAsync)),
                new("FENCE POWER", "Try to restore power to the perimeter fences",
                    () => Busy("REQUESTING FENCE CONTROL", "", () => Task.CompletedTask,
                        then: () => Say("ACCESS DENIED - FENCE CONTROL LOCKED BY dnedry", Palette.Bad))),
            ],
            Tab.Operations =>
            [
                new("FIND REPAIR", "Find the fence with the most recorded breaches",
                    () => Busy("QUERYING MAINTENANCE DB", "MAINTENANCE QUERY COMPLETE", RefreshAsync)),
                new("STREAM FEED", "Stream raw motion-sensor telemetry into the log", StreamTelemetry),
                new("CLEAR LOG", "Clear the telemetry log", () => { telemetry.Clear(); Say("TELEMETRY LOG CLEARED", Palette.Ink); }),
            ],
            _ =>
            [
                new("REFRESH ALL", "Re-run every park system check",
                    () => Busy("POLLING ALL SYSTEMS", "ALL SYSTEMS REFRESHED", RefreshAsync)),
                new("ACK ALARMS", "Acknowledge alarms: stop the blinking (faults stay listed)", AcknowledgeAlarms),
                new("LOCATE GUESTS", "Jump to Security and search for missing guests", () =>
                {
                    Show(Tab.Security);
                    Busy("SEARCHING PADDOCK GRID", "GUEST SEARCH COMPLETE", RefreshAsync);
                }),
            ],
        };
    }

    /// <summary>Scan animation in the census panel and species chart, then the real recount.</summary>
    private void Recount()
    {
        activity = (Activity.Recount, now);
        Busy("SCANNING ROSTER TAGS", "CENSUS RECOUNTED", RefreshAsync, hold: RecountTime, then: () =>
        {
            lastRecount = DateTime.Now;
            Say($"CENSUS RECOUNTED {DateTime.Now:HH:mm:ss}", Palette.Ink);
        });
    }

    /// <summary>Walks each species in the feeding plan (quick look) and lights up its bar, then the real rebuild.</summary>
    private void RebuildFeedingPlan()
    {
        quickLook = QuickLook.Feeding;
        activity = (Activity.FeedingPlan, now);
        var species = roster.Select(d => d.GetType()).Distinct().Count();
        Busy("ASSIGNING DIETS", "FEEDING PLAN REBUILT", RefreshAsync, hold: FeedingStepTime * (species + 1), then: () =>
        {
            lastFeedingRebuild = DateTime.Now;
            Say($"FEEDING PLAN REBUILT {DateTime.Now:HH:mm:ss}", Palette.Ink);
        });
    }

    private void AcknowledgeAlarms()
    {
        alarmsAcknowledged = true;
        var faults = Faults.Count();
        Say(faults == 0 ? "NO ACTIVE ALARMS" : $"ALARMS ACKNOWLEDGED - {faults} FAULT(S) STILL ACTIVE", Palette.Ink);
    }

    private void ToggleHelp() => helpOpen = !helpOpen;

    /// <summary>Function keys work no matter where the mouse is.</summary>
    private void HandleHotKeys()
    {
        var keyboard = GameHost.Instance.Keyboard;
        if (helpOpen)
        {
            if (keyboard.KeysPressed.Count > 0) ToggleHelp();
            return;
        }

        if (keyboard.IsKeyPressed(Keys.F1)) Show(Tab.Overview);
        else if (keyboard.IsKeyPressed(Keys.F2)) Show(Tab.Animals);
        else if (keyboard.IsKeyPressed(Keys.F3)) Show(Tab.Security);
        else if (keyboard.IsKeyPressed(Keys.F4)) Show(Tab.Operations);
        else if (keyboard.IsKeyPressed(Keys.H) || keyboard.IsKeyPressed(Keys.OemQuestion)) ToggleHelp();
        else if (keyboard.IsKeyPressed(Keys.Escape)) LogOff();
    }

    /// <summary>Hover and click for everything drawn as a <see cref="Hotspot"/>: buttons, status rows, paddocks, quick-look tabs.</summary>
    public override bool ProcessMouse(MouseScreenObjectState state)
    {
        mouse = state.IsOnScreenObject ? state.CellPosition : null;
        mouseDown = state.Mouse.LeftButtonDown;

        if (helpOpen)
        {
            if (state.Mouse.LeftClicked) ToggleHelp();
            return true;
        }

        if (state.Mouse.LeftClicked && HoveredHotspot() is { OnClick: { } click }) click();
        return true;
    }

    private Hotspot? HoveredHotspot() => mouse is { } m ? hotspots.LastOrDefault(h => h.Area.Contains(m)) : null;

    private bool IsHovered(Rectangle area) => !helpOpen && mouse is { } m && area.Contains(m);

    private static void LogOff() => Game.Instance.MonoGameInstance.Exit();

    // ---- Data ----------------------------------------------------------------------------------

    private async Task RefreshAsync()
    {
        census = CensusPanel.Render(roster, SensorNetwork.Sweep());
        motion = MotionPanel.Render(SensorFeed.Current());
        feeding = FeedingPanel.Render(roster);
        fences = FencePanel.Render();
        guests = GuestLocatorPanel.Render(map);
        maintenance = await MaintenancePanel.RenderAsync();

        await using var db = ParkDb.Create();
        closedZones = [.. db.Paddocks.Where(p => !p.IsOpen).Select(p => p.Code.ToLower())];
        alarmsAcknowledged = false;
    }

    private void StreamTelemetry()
    {
        telemetry.Clear();
        Busy("RECEIVING TELEMETRY", "TELEMETRY STREAM COMPLETE", async () =>
        {
            await using var stream = new LineStream(incomingTelemetry.Enqueue);
            await FeedCommand.RunAsync(stream);
        });
    }

    /// <summary>
    /// Runs work off the render loop with a progress bar in SYS MSG. Short jobs are held for a moment
    /// so the audience can see something happen; afterwards the panels type themselves out again.
    /// </summary>
    private void Busy(string working, string done, Func<Task> work, Action? then = null, TimeSpan? hold = null)
    {
        busy = (working, now, clickedButton);
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(work(), Task.Delay(hold ?? MinimumBusyTime));
                activity = null;
                revealFrom = quickLookRevealFrom = now;
                if (then is null) Say($"{done} {DateTime.Now:HH:mm:ss}", Palette.Ink);
                else then();
            }
            catch (Exception ex)
            {
                activity = null;
                Say($"SYSTEM FAULT: {ex.GetType().Name}: {ex.Message}", Palette.Bad);
            }
            finally
            {
                busy = null;
            }
        });
    }

    private void Say(string text, Color color) => message = (text, color);

    // ---- Frame loop ------------------------------------------------------------------------------

    public override void Update(TimeSpan delta)
    {
        now += delta;
        HandleHotKeys();

        while (incomingTelemetry.TryDequeue(out var line)) telemetry.Add(line);

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
        hotspots.Clear();
        Surface.Fill(Palette.Ink, Palette.Chrome, 0);

        DrawTitleBar();
        DrawNavBar();
        DrawViewport();
        DrawCaption();
        DrawQuickLook();
        DrawCommands();
        DrawStatusBoard();
        DrawScreenBar();
        DrawStatusRow();
        if (helpOpen) DrawHelp();
    }

    // ---- Buttons -------------------------------------------------------------------------------------

    private enum ButtonLook { Normal, Selected, Running }

    /// <summary>
    /// A bevelled push button: raised at rest, amber on hover, sunk while the mouse is held on it
    /// (and briefly after a click), pulsing with a spinner while the command it started is running.
    /// </summary>
    private void Button(Rectangle r, string label, string hint, Action onClick, ButtonLook look = ButtonLook.Normal)
    {
        var hovered = IsHovered(r);
        var pressed = (hovered && mouseDown) || (pressFlash is { } f && f.Label == label && now - f.At < PressFlashTime);

        var (face, ink) = pressed ? (Palette.ChromeDark, Color.White)
            : look == ButtonLook.Running ? (Blink ? Palette.Amber : Palette.AmberDim, Palette.Ink)
            : look == ButtonLook.Selected ? (Palette.Amber, Palette.Ink)
            : hovered ? (Palette.Highlight, Palette.Ink)
            : (Palette.ButtonFace, Palette.Ink);
        var (lit, shade) = pressed || look == ButtonLook.Selected
            ? (Palette.ChromeDark, Color.White)
            : (Color.White, Palette.ChromeDark);

        Surface.Fill(r, ink, face, 0);
        for (var x = r.X + 1; x < r.MaxExtentX; x++)
        {
            Surface.SetGlyph(x, r.Y, Cp437.SingleHorizontal, lit);
            Surface.SetGlyph(x, r.MaxExtentY, Cp437.SingleHorizontal, shade);
        }
        Surface.SetGlyph(r.X, r.Y + 1, Cp437.SingleVertical, lit);
        Surface.SetGlyph(r.MaxExtentX, r.Y + 1, Cp437.SingleVertical, shade);
        Surface.SetGlyph(r.X, r.Y, Cp437.TopLeft, lit);
        Surface.SetGlyph(r.MaxExtentX, r.Y, Cp437.TopRight, lit);
        Surface.SetGlyph(r.X, r.MaxExtentY, Cp437.BottomLeft, shade);
        Surface.SetGlyph(r.MaxExtentX, r.MaxExtentY, Cp437.BottomRight, shade);

        var text = Clip(label, r.Width - 2);
        var textX = r.X + (r.Width - text.Length) / 2 + (pressed ? 1 : 0);
        Print(textX, r.Y + 1, text, ink, face);
        if (look == ButtonLook.Running) Surface.SetGlyph(r.MaxExtentX - 1, r.Y + 1, @"|/-\"[(int)(now.TotalMilliseconds / 110) % 4], ink, face);

        hotspots.Add(new(r, hint, () =>
        {
            pressFlash = (label, now);
            clickedButton = label;
            onClick();
            clickedButton = null;
        }));
    }

    private void DrawNavBar()
    {
        var x = 1;
        foreach (var info in Tabs)
        {
            Button(new Rectangle(x, NavRow, 20, 3), $"{info.Key}  {info.Name}", $"{info.Key} {info.Name}: {info.Hint}",
                () => Show(info.Tab), info.Tab == tab ? ButtonLook.Selected : ButtonLook.Normal);
            x += 21;
        }
        Button(new Rectangle(ScreenWidth - 15, NavRow, 14, 3), "H  HELP", "H HELP: How to use this terminal", ToggleHelp);
    }

    /// <summary>Row 1: this screen's commands. Row 2: moving between screens.</summary>
    private void DrawCommands()
    {
        Bevel(CommandBox);
        Legend(CommandBox, "COMMANDS");

        Rectangle Slot(int slot, int row) => new(CommandBox.X + 1 + slot * 16, CommandBox.Y + 1 + row * 3, 15, 3);

        foreach (var (slot, command) in commands.Index())
        {
            var running = busy is { Source: { } source } && source == command.Label;
            Button(Slot(slot, 0), command.Label, $"{command.Label}: {command.Hint}", command.Execute,
                running ? ButtonLook.Running : ButtonLook.Normal);
        }

        var previous = Tabs[((int)tab + Tabs.Length - 1) % Tabs.Length];
        var following = Tabs[((int)tab + 1) % Tabs.Length];
        Button(Slot(0, 1), $"{Cp437.Rewind}{Cp437.Rewind} PREV", $"Previous screen: {previous.Key} {previous.Name}", () => Show(previous.Tab));
        Button(Slot(1, 1), $"NEXT {Cp437.Play}{Cp437.Play}", $"Next screen: {following.Key} {following.Name}", () => Show(following.Tab));
        Button(Slot(2, 1), "QUIT", "QUIT: Log off the control room (ESC)", LogOff);
    }

    // ---- Chrome ----------------------------------------------------------------------------------

    private void DrawTitleBar()
    {
        Surface.Fill(new Rectangle(0, 0, ScreenWidth, 1), Palette.Ink, Palette.MenuBar, 0);
        Print(1, 0, $"{Cp437.Menu}  INGEN // PARK OPERATIONS CONTROL ROOM // TERMINAL CR-01", Palette.Ink, Palette.MenuBar);
        var right = $"{RuntimeInformation.FrameworkDescription}   {DateTime.Now:HH:mm:ss} ";
        Print(ScreenWidth - right.Length, 0, right, Palette.Ink, Palette.MenuBar);
        var live = ScreenWidth - right.Length - 9;
        Print(live, 0, $"{Cp437.Bullet}", Blink ? Palette.Good : Palette.MenuBar, Palette.MenuBar);
        Print(live + 2, 0, "LIVE", Palette.Ink, Palette.MenuBar);
        hotspots.Add(new(new Rectangle(live, 0, 6, 1), "LIVE: readouts refresh when you run a command", null));
        Surface.DrawLine(new Point(0, 1), new Point(ScreenWidth - 1, 1), Cp437.SingleHorizontal, Palette.ChromeDark, Palette.Chrome);
    }

    /// <summary>Raised grey panel: light top/left edge, dark bottom/right edge.</summary>
    private void Bevel(Rectangle r, bool raised = true)
    {
        var (topLeft, bottomRight) = raised ? (Palette.ChromeLight, Palette.ChromeDark) : (Palette.ChromeDark, Palette.ChromeLight);
        Surface.Fill(r, Palette.Ink, Palette.Chrome, 0);
        for (var x = r.X + 1; x < r.MaxExtentX; x++)
        {
            Surface.SetGlyph(x, r.Y, Cp437.SingleHorizontal, topLeft);
            Surface.SetGlyph(x, r.MaxExtentY, Cp437.SingleHorizontal, bottomRight);
        }
        for (var y = r.Y + 1; y < r.MaxExtentY; y++)
        {
            Surface.SetGlyph(r.X, y, Cp437.SingleVertical, topLeft);
            Surface.SetGlyph(r.MaxExtentX, y, Cp437.SingleVertical, bottomRight);
        }
        Surface.SetGlyph(r.X, r.Y, Cp437.TopLeft, topLeft);
        Surface.SetGlyph(r.MaxExtentX, r.Y, Cp437.TopRight, topLeft);
        Surface.SetGlyph(r.X, r.MaxExtentY, Cp437.BottomLeft, bottomRight);
        Surface.SetGlyph(r.MaxExtentX, r.MaxExtentY, Cp437.BottomRight, bottomRight);
    }

    /// <summary>Sunken dark "CRT" area inside a bevel, with a faint dot grid.</summary>
    private Rectangle Screen(Rectangle r)
    {
        Bevel(r, raised: false);
        var inner = r.Expand(-1, -1);
        Surface.Fill(inner, Palette.Text, Palette.Background, 0);
        for (var y = inner.Y; y <= inner.MaxExtentY; y++)
            for (var x = inner.X; x <= inner.MaxExtentX; x++)
                if ((x - inner.X) % 6 == 3 && (y - inner.Y) % 3 == 1) Surface.SetGlyph(x, y, Cp437.MiddleDot, Palette.GridDot);
        return inner;
    }

    /// <summary>Repaint wipe after a tab switch: an amber scanline sweeps down, revealing the new screen.</summary>
    private void Wipe(Rectangle inner)
    {
        var progress = (now - wipeFrom) / WipeTime;
        if (progress >= 1) return;
        var y = inner.Y + (int)(progress * inner.Height);
        if (y < inner.MaxExtentY) Surface.Fill(new Rectangle(inner.X, y + 1, inner.Width, inner.MaxExtentY - y), Palette.Text, Palette.Background, 0);
        Surface.Fill(new Rectangle(inner.X, y, inner.Width, 1), Palette.Amber, Palette.Background, Cp437.LowerHalf);
    }

    /// <summary>A label set into a bevel's top edge, like a group-box caption.</summary>
    private void Legend(Rectangle box, string text) => Print(box.X + 2, box.Y, $" {text} ", Palette.Ink, Palette.Chrome);

    /// <summary>Black chip with white text, like the film's panel labels.</summary>
    private int Chip(int x, int y, string text, Color? foreground = null, Color? background = null)
    {
        Print(x, y, $" {text} ", foreground ?? Color.White, background ?? Palette.Ink);
        return x + text.Length + 2;
    }

    // ---- Left: viewport and caption ----------------------------------------------------------------

    private void DrawViewport()
    {
        var inner = Screen(Viewport);
        switch (tab)
        {
            case Tab.Overview:
                var faults = Faults.ToList();
                var loading = Systems.Any(s => s.Readout is null);
                var (banner, color) = loading ? ("CONNECTING", Palette.ChromeDark)
                    : faults.Count == 0 ? ("SYSTEM SECURED", Palette.BadgeGood)
                    : ("SECURITY ALERT", AlertOn ? Palette.Bad : Palette.BadDark);
                Banner(new Rectangle(inner.X, inner.Y, inner.Width, 3), banner, color);

                var reason = loading ? "Polling park systems..."
                    : faults.Count == 0 ? "Every monitored system reports CLEAR."
                    : $"FAILED: {string.Join(", ", faults)}. Click it in SYSTEM STATUS.";
                Print(inner.X + (inner.Width - reason.Length) / 2, inner.Y + 4, Clip(reason, inner.Width), faults.Count == 0 ? Palette.Good : Palette.Bad);

                DrawParkGrid(new Rectangle(inner.X + 1, inner.Y + 6, inner.Width - 2, 13));
                Banner(new Rectangle(inner.X, inner.MaxExtentY - 2, inner.Width, 3), banner, color);
                break;
            case Tab.Animals:
                var censusArea = new Rectangle(inner.X + 1, inner.Y, 44, inner.Height);
                if (activity is { Kind: Activity.Recount } recount) DrawRecountScan(censusArea, now - recount.Since);
                else
                {
                    DrawReadout(census, "ANIMAL CENSUS", censusArea, revealFrom);
                    if (lastRecount is { } at)
                    {
                        Print(censusArea.X, censusArea.MaxExtentY, $"Last recount {at:HH:mm:ss} - {roster.Count} tags checked", Palette.Dim);
                    }
                }
                DrawSpeciesChart(new Rectangle(inner.X + 47, inner.Y, inner.Width - 48, inner.Height));
                break;
            case Tab.Security:
                DrawReadout(motion, "MOTION SENSORS", new Rectangle(inner.X + 1, inner.Y, inner.Width - 2, 8), revealFrom);
                DrawReadout(guests, "GUEST LOCATOR", new Rectangle(inner.X + 1, inner.Y + 9, inner.Width - 2, inner.Height - 9), revealFrom);
                break;
            case Tab.Operations:
                var feed = new Readout("SENSOR FEED // RAW TELEMETRY", PanelStatus.Nominal,
                    telemetry.Count == 0
                        ? [new("No data yet. Press STREAM FEED to listen to the sensors.", Tone.Dim)]
                        : [.. telemetry.Select(t => new Line(t))]);
                DrawReadout(feed, feed.Title, inner.Expand(-1, 0), revealFrom, tail: true);
                break;
        }
        Wipe(inner);
    }

    private void Banner(Rectangle r, string text, Color color)
    {
        Surface.Fill(r, Color.White, color, 0);
        var spaced = string.Join(' ', text.ToCharArray());
        Print(r.X + (r.Width - spaced.Length) / 2, r.Y + 1, spaced, Color.White, color);
    }

    private void DrawCaption()
    {
        Surface.Fill(Caption, Palette.Amber, Palette.Ink, 0);
        Surface.DrawBox(Caption, ShapeParameters.CreateStyledBox(ICellSurface.ConnectedLineThick, new ColoredGlyph(Palette.Amber, Palette.Ink)));

        foreach (var x in new[] { Caption.X + 2, Caption.MaxExtentX - 8 })
        {
            Print(x, Caption.Y + 1, " LEVEL ", Palette.Ink, Palette.Chrome);
            Print(x, Caption.Y + 2, "       ", Color.White, Palette.Ink);
            Print(x + 3, Caption.Y + 2, Current.Level.ToString(), Palette.Amber, Palette.Ink);
            Print(x, Caption.Y + 3, "       ", Color.White, Palette.Ink);
        }

        var title = Current.Title.ToUpperInvariant();
        Print(Caption.X + (Caption.Width - title.Length) / 2, Caption.Y + 1, title, Color.White, Palette.Ink);
        Print(Caption.X + (Caption.Width - Current.View.Length) / 2, Caption.Y + 3, Current.View, Palette.Dim, Palette.Ink);
    }

    // ---- Right: quick look and system status ------------------------------------------------------

    private void DrawQuickLook()
    {
        Bevel(QuickLookBox);
        Legend(QuickLookBox, "QUICK LOOK - click to swap");
        var x = QuickLookBox.X + 1;
        foreach (var view in Enum.GetValues<QuickLook>())
        {
            var label = view.ToString().ToUpperInvariant();
            var area = new Rectangle(x + 1, QuickLookBox.Y + 1, label.Length + 2, 1);
            var (fg, bg) = view == quickLook ? (Palette.Ink, Palette.Amber)
                : IsHovered(area) ? (Palette.Ink, Palette.Highlight)
                : (Palette.Ink, Palette.ChromeLight);
            x = Chip(x + 1, QuickLookBox.Y + 1, label, fg, bg);
            hotspots.Add(new(area, $"QUICK LOOK: show {label.ToLowerInvariant()} in this box", () =>
            {
                quickLook = view;
                quickLookRevealFrom = now;
            }));
        }

        var readout = quickLook switch
        {
            QuickLook.Fences => fences,
            QuickLook.Feeding => feeding,
            QuickLook.Repairs => maintenance,
            _ => motion,
        };
        var title = quickLook switch
        {
            QuickLook.Fences => "FENCE STATUS",
            QuickLook.Feeding => "FEEDING PLAN",
            QuickLook.Repairs => "MAINTENANCE",
            _ => "MOTION SENSORS",
        };

        var inner = Screen(new Rectangle(QuickLookBox.X + 1, QuickLookBox.Y + 2, QuickLookBox.Width - 2, QuickLookBox.Height - 3));
        if (quickLook == QuickLook.Feeding && activity is { Kind: Activity.FeedingPlan } plan && feeding is not null)
        {
            DrawFeedingRebuild(feeding, inner, now - plan.Since);
        }
        else
        {
            DrawReadout(readout, title, inner, quickLookRevealFrom);
            if (quickLook == QuickLook.Feeding && lastFeedingRebuild is { } at)
            {
                var stamp = $"rebuilt {at:HH:mm:ss}";
                Print(inner.MaxExtentX - stamp.Length + 1, inner.Y, stamp, Palette.Dim);
            }
        }
        Wipe(inner);
    }

    /// <summary>
    /// The feeding plan while FEEDING PLAN runs: a cursor walks each species, its plan column
    /// shows "assigning..." and then the diet the planner picked.
    /// </summary>
    private void DrawFeedingRebuild(Readout plan, Rectangle area, TimeSpan elapsed)
    {
        const int planColumn = 29; // "{species,-20}{head,6}   " in FeedingPanel
        var x = Chip(area.X, area.Y, plan.Title, Palette.Ink, Palette.Amber);
        Print(x, area.Y, $" {Cp437.Bullet} REBUILDING ", Blink ? Palette.Amber : Palette.AmberDim);
        Print(area.X, area.Y + 1, "Assigning a diet to each species in turn", Palette.Dim);

        var current = (int)(elapsed / FeedingStepTime);
        foreach (var (index, line) in plan.Lines.Index().Take(area.Height - 2))
        {
            var y = area.Y + 2 + index;
            var species = index - 1; // line 0 is the column header
            if (species < 0 || species < current)
            {
                Print(area.X, y, line.Text, Palette.For(line.Tone));
            }
            else if (species == current)
            {
                var spinner = @"|/-\"[(int)(elapsed.TotalMilliseconds / 90) % 4];
                Print(area.X, y, $"{line.Text[..planColumn]}assigning {spinner}".PadRight(area.Width), Palette.Ink, Palette.Amber);
            }
            else
            {
                Print(area.X, y, $"{line.Text[..planColumn]}...", Palette.Dim);
            }
        }
    }

    private void DrawStatusBoard()
    {
        Bevel(StatusBoard);
        Chip(StatusBoard.X + 2, StatusBoard.Y + 1, $"SYSTEM STATUS {Cp437.DoubleHorizontal}");
        const string tip = "click a row to open it";
        Print(StatusBoard.MaxExtentX - tip.Length - 1, StatusBoard.Y + 1, tip, Palette.ChromeDark, Palette.Chrome);

        Bevel(StatusList.Expand(1, 1), raised: false);
        Surface.Fill(StatusList, Palette.Ink, Palette.Paper, 0);

        var y = StatusList.Y;
        foreach (var (name, target, readout) in Systems)
        {
            var (badge, color, meaning) = readout?.Status switch
            {
                PanelStatus.Nominal => ("CLEAR", Palette.BadgeGood, "reports normal"),
                PanelStatus.Alert => ("FAILED", AlertOn ? Palette.Bad : Palette.BadDark, "reports a fault"),
                PanelStatus.Attention => ("CHECK", Palette.BadgeCheck, "needs attention"),
                _ => ("WAIT", Blink ? Palette.ChromeDark : Palette.Chrome, "still connecting"),
            };
            var row = new Rectangle(StatusList.X, y, StatusList.Width, 1);
            var hovered = IsHovered(row);
            var paper = hovered ? Palette.Highlight : Palette.Paper;
            var targetTab = Tabs[(int)target];

            Surface.Fill(row, Palette.Ink, paper, 0);
            Print(StatusList.X, y, $" {badge,-6} ", Color.White, color);
            Print(StatusList.X + 9, y, $"- {name}", Palette.Ink, paper);
            if (hovered) Print(StatusList.MaxExtentX - 12, y, $"OPEN {targetTab.Key} {Cp437.Play}", Palette.Ink, paper);
            hotspots.Add(new(row, $"{name} {meaning} - click to open {targetTab.Key} {targetTab.Name}", () => Show(target)));
            y++;
        }
    }

    // ---- Bottom bars -------------------------------------------------------------------------------

    private void DrawScreenBar()
    {
        Bevel(ScreenBar);
        Chip(ScreenBar.X + 2, ScreenBar.Y + 1, $"SCREEN {Cp437.Play}");
        Print(ScreenBar.X + 14, ScreenBar.Y + 1, new string(' ', ScreenBar.Width - 16), Palette.Ink, Palette.Paper);
        Print(ScreenBar.X + 16, ScreenBar.Y + 1, "Isla Nublar, Costa Rica", Palette.Ink, Palette.Paper);
        Print(ScreenBar.MaxExtentX - 18, ScreenBar.Y + 1, $"{Current.Key} {Current.Name}", Palette.ChromeDark, Palette.Paper);

        Bevel(MessageBar);
        Chip(MessageBar.X + 2, MessageBar.Y + 1, "SYS MSG");
        var x = MessageBar.X + 12;
        var width = MessageBar.Width - 14;
        Print(x, MessageBar.Y + 1, new string(' ', width), Palette.Ink, Palette.Paper);

        if (busy is { } job)
        {
            // Bouncing block in a track, like an old indeterminate progress bar.
            const int track = 14;
            var label = Clip(job.Label, width - track - 3);
            Print(x + 1, MessageBar.Y + 1, label, Palette.Ink, Palette.Paper);
            var trackX = x + width - track - 1;
            Print(trackX, MessageBar.Y + 1, new string(Cp437.LightShade, track), Palette.ChromeDark, Palette.Paper);
            var step = (int)((now - job.Since).TotalMilliseconds / 70) % ((track - 3) * 2);
            var pos = step < track - 3 ? step : (track - 3) * 2 - step;
            Print(trackX + pos, MessageBar.Y + 1, new string(Cp437.FullBlock, 3), Palette.Amber, Palette.Paper);
        }
        else if (HoverText() is { } text)
        {
            Print(x + 1, MessageBar.Y + 1, Ticker($"? {text}", width - 2), Palette.Info, Palette.Paper);
        }
        else
        {
            Print(x + 1, MessageBar.Y + 1, Ticker(message.Text, width - 2), message.Color, Palette.Paper);
        }
    }

    /// <summary>Text that doesn't fit scrolls like a ticker: hold at the start, scroll through, repeat.</summary>
    private string Ticker(string text, int width)
    {
        if (text != tickerText)
        {
            tickerText = text;
            tickerSince = now;
        }
        if (text.Length <= width) return text;

        const double holdMs = 1200, stepMs = 110;
        var loop = text + "     ";
        var elapsed = (now - tickerSince).TotalMilliseconds % (holdMs + loop.Length * stepMs);
        var offset = (int)Math.Max(0, (elapsed - holdMs) / stepMs);
        return (loop + loop)[offset..(offset + width)];
    }

    private string? HoverText() => helpOpen ? null : HoveredHotspot()?.Hint;

    private void DrawStatusRow()
    {
        var x = Chip(1, StatusRow, "OPERATOR");
        Print(x, StatusRow, " dnedry (out of office) ", Palette.Ink, Palette.Chrome);
        x = Chip(x + 26, StatusRow, "KEYS");
        Print(x, StatusRow, " F1-F4 screens   H help   hover anything for a hint   ESC quit ", Palette.Ink, Palette.Chrome);
    }

    // ---- Help overlay ------------------------------------------------------------------------------

    private void DrawHelp()
    {
        var box = new Rectangle(14, 5, 100, 34);
        Surface.Fill(new Rectangle(0, 1, ScreenWidth, ScreenHeight - 1), Palette.Dim, Palette.Background, Cp437.LightShade);
        Bevel(box);
        Chip(box.X + 2, box.Y + 1, $"HELP {Cp437.DoubleHorizontal} HOW TO USE THIS TERMINAL");
        var inner = Screen(new Rectangle(box.X + 1, box.Y + 2, box.Width - 2, box.Height - 3));

        var x = inner.X + 2;
        var y = inner.Y + 1;
        void Heading(string text) { Print(x, y, text, Palette.Amber); y++; }
        void Text(string text, Color? color = null) { Print(x + 2, y, text, color ?? Palette.Text); y++; }
        void Gap() => y++;

        Heading("WHAT THIS IS");
        Text("ParkOps is the island's control-room software. Each screen shows live readouts");
        Text("from park systems: animals, motion sensors, fences, guests and maintenance.");
        Gap();
        Heading("SCREENS");
        Text("F1 OVERVIEW    paddock map and park-wide alerts");
        Text("F2 ANIMALS     animal census and feeding plan");
        Text("F3 SECURITY    motion sensors, guest locator and fence power");
        Text("F4 OPERATIONS  fence maintenance and the raw sensor feed");
        Text("Click the tabs across the top, use F1-F4, or PREV / NEXT in the command grid.", Palette.Dim);
        Gap();
        Heading("SYSTEM STATUS (bottom right)");
        Print(x + 2, y, " CLEAR  ", Color.White, Palette.BadgeGood);
        Print(x + 11, y++, "the system reports normal", Palette.Text);
        Print(x + 2, y, " CHECK  ", Color.White, Palette.BadgeCheck);
        Print(x + 11, y++, "the system needs attention", Palette.Text);
        Print(x + 2, y, " FAILED ", Color.White, Palette.Bad);
        Print(x + 11, y++, "the system reports a fault (blinks until ACK ALARMS on F1)", Palette.Text);
        Text("Click any row to jump to the screen for that system.", Palette.Dim);
        Gap();
        Heading("ON EVERY SCREEN");
        Text("QUICK LOOK (top right)  click SENSORS / FENCES / FEEDING / REPAIRS to swap it");
        Text("COMMANDS (right)        buttons that act on the current screen");
        Text("PADDOCK GRID (F1)       hover a paddock for details, click to search for guests");
        Text("SYS MSG (bottom right)  results, progress, and a hint for whatever is under the mouse");
        Gap();
        Text("Press any key or click to close.", Palette.Amber);
    }

    // ---- Content -----------------------------------------------------------------------------------

    /// <summary>Header chip with status light and a one-line description, then the lines, typed out from <paramref name="revealStart"/>.</summary>
    private void DrawReadout(Readout? readout, string title, Rectangle area, TimeSpan revealStart, bool tail = false)
    {
        readout ??= new Readout(title, PanelStatus.Attention, [new("CONNECTING...", Tone.Dim)]);
        var color = readout.Status == PanelStatus.Alert && !AlertOn ? Palette.BadDark : Palette.For(readout.Status);

        var x = Chip(area.X, area.Y, readout.Title, Palette.Ink, Palette.Amber);
        Print(x, area.Y, $" {Cp437.Bullet} {readout.Status.ToString().ToUpperInvariant()} ", color);
        if (Descriptions.TryGetValue(readout.Title, out var description))
        {
            Print(area.X, area.Y + 1, Clip(description, area.Width), Palette.Dim);
        }

        var rows = readout.Lines
            .SelectMany(line => Wrap(line.Text, area.Width).Select(text => (Text: text, line.Tone)))
            .ToList();

        var top = area.Y + 2;
        var capacity = area.Height - 2;
        var start = tail ? Math.Max(0, rows.Count - capacity) : 0;
        var budget = tail ? int.MaxValue : (int)Math.Max(0, (now - revealStart).TotalMilliseconds * TypewriterCharsPerMs);
        for (var i = 0; i < capacity && start + i < rows.Count && budget > 0; i++)
        {
            var (text, tone) = rows[start + i];
            var shown = text[..Math.Min(text.Length, budget)];
            Print(area.X, top + i, shown, Palette.For(tone));
            if (shown.Length < text.Length) Surface.SetGlyph(area.X + shown.Length, top + i, Cp437.FullBlock, Palette.Amber);
            budget -= Math.Max(text.Length, 1);
        }
        if (!tail && rows.Count > capacity) Print(area.MaxExtentX - 4, area.MaxExtentY, "...", Palette.Amber);
    }

    /// <summary>Bar chart of the roster, one bar per species, growing in as the screen reveals.</summary>
    private void DrawSpeciesChart(Rectangle area)
    {
        Chip(area.X, area.Y, "ON THE ROSTER", Palette.Ink, Palette.Amber);
        Print(area.X, area.Y + 1, "Head count by species", Palette.Dim);

        var species = Species();
        var most = species.Max(s => s.Count);
        var grow = Math.Clamp((now - revealFrom).TotalMilliseconds / 600, 0, 1);
        var barWidth = area.Width - 5;

        // During a recount the bars refill in roster order as tags are scanned;
        // during a feeding-plan rebuild the species being assigned is lit up.
        var scanned = activity is { Kind: Activity.Recount } recount ? ScannedSoFar(now - recount.Since) : (int?)null;
        var assigning = activity is { Kind: Activity.FeedingPlan } plan ? (int)((now - plan.Since) / FeedingStepTime) : -1;

        var y = area.Y + 3;
        var before = 0;
        foreach (var (index, (name, count)) in species.Index())
        {
            var row = new Rectangle(area.X, y, area.Width, 2);
            var lit = IsHovered(row) || index == assigning;
            var shown = scanned is { } n ? Math.Clamp(n - before, 0, count) : count;
            var fraction = scanned is null ? grow : 1;

            Print(area.X, y, Clip(name, area.Width), lit ? Palette.Amber : Palette.Text);
            if (index == assigning) Print(area.MaxExtentX - 9, y, "DIET...", Palette.Ink, Palette.Amber);

            var length = (int)(shown / (double)most * barWidth * fraction);
            var counting = scanned is not null && shown > 0 && shown < count;
            Print(area.X, y + 1, new string(Cp437.FullBlock, Math.Max(length, shown > 0 ? 1 : 0)),
                counting ? Palette.Amber : lit ? Palette.Amber : Palette.AmberDim);
            Print(area.X + length + 1, y + 1, scanned is null ? count.ToString() : $"{shown}/{count}", counting ? Palette.Amber : Palette.Dim);

            hotspots.Add(new(row, $"{name}: {count} on the roster", null));
            before += count;
            y += 3;
        }
    }

    private List<(string Name, int Count)> Species() =>
        [.. roster.GroupBy(d => d.GetType().Name).Select(g => (g.Key, g.Count()))];

    /// <summary>How many rostered tags the recount animation has scanned after <paramref name="elapsed"/>.</summary>
    private int ScannedSoFar(TimeSpan elapsed) =>
        (int)(Math.Clamp(elapsed / (RecountTime - TimeSpan.FromMilliseconds(400)), 0, 1) * roster.Count);

    /// <summary>The census panel while RECOUNT runs: a tag counter, progress bar and a scrolling log of tags checked.</summary>
    private void DrawRecountScan(Rectangle area, TimeSpan elapsed)
    {
        var x = Chip(area.X, area.Y, "ANIMAL CENSUS", Palette.Ink, Palette.Amber);
        Print(x, area.Y, $" {Cp437.Bullet} COUNTING ", Blink ? Palette.Amber : Palette.AmberDim);
        Print(area.X, area.Y + 1, "Checking rostered tags against motion grid", Palette.Dim);

        var scanned = ScannedSoFar(elapsed);
        Print(area.X, area.Y + 3, $"{scanned,3} of {roster.Count} tags checked", Palette.Strong);

        const int barWidth = 40;
        var filled = scanned * barWidth / roster.Count;
        Print(area.X, area.Y + 4, new string(Cp437.FullBlock, filled), Palette.Amber);
        Print(area.X + filled, area.Y + 4, new string(Cp437.LightShade, barWidth - filled), Palette.GridDot);

        // Most recent tags first; the newest one is still "checking".
        var log = Math.Min(scanned, area.Height - 7);
        for (var i = 0; i < log; i++)
        {
            var animal = roster[scanned - 1 - i];
            var line = $"{animal.Tag,-7}{animal.GetType().Name,-20}";
            if (i == 0) Print(area.X, area.Y + 6 + i, $"{line}checking", Palette.Ink, Palette.Amber);
            else Print(area.X, area.Y + 6 + i, $"{line}located", i < 4 ? Palette.Good : Palette.Dim);
        }
    }

    /// <summary>The big board: one strip per paddock with a radar sweep, guests and fence power overlaid.</summary>
    private void DrawParkGrid(Rectangle area)
    {
        var powered = FenceController.PowerOn;
        var fenceColor = powered ? Palette.Good : Palette.Bad;
        var sweep = (int)(now.TotalMilliseconds % 2400 / 2400 * map.Width);

        var y = area.Y;
        foreach (var (row, strip) in map.Cells.GroupBy(c => c.Zone).Index())
        {
            var closed = closedZones.Contains(strip.Key);
            var people = strip.SelectMany(c => c.Occupants.OfType<Guest>()).Select(g => g.Name).ToList();
            var rowArea = new Rectangle(area.X, y, area.Width, 1);
            var hovered = IsHovered(rowArea);

            var x = area.X;
            var label = strip.Key.ToUpperInvariant().PadRight(11);
            Print(x, y, label, hovered ? Palette.Ink : Palette.Amber, hovered ? Palette.Amber : Palette.Background);
            x += 11;

            // Dead fences spark and flicker.
            var flicker = !powered && ((int)(now.TotalMilliseconds / 90) + row * 7) % 6 == 0;
            var fence = powered ? Cp437.DoubleVertical : flicker ? '*' : ':';
            var fenceGlyphColor = flicker ? Palette.Amber : fenceColor;

            Surface.SetGlyph(x++, y, fence, fenceGlyphColor);
            foreach (var column in strip.GroupBy(c => c.X).OrderBy(g => g.Key))
            {
                if (column.Any(c => c.Occupants.OfType<Guest>().Any()))
                {
                    var (fg, bg) = Blink ? (Color.White, Palette.Bad) : (Palette.Bad, Color.White);
                    Print(x, y, $"{Cp437.Smiley}{Cp437.Smiley}", fg, bg);
                }
                else
                {
                    var (ground, soil, glow) = closed
                        ? (new Color(200, 60, 60), new Color(60, 14, 14), new Color(255, 170, 160))
                        : (new Color(60, 170, 90), new Color(10, 45, 22), new Color(190, 255, 200));
                    var behind = (sweep - column.Key + map.Width) % map.Width;
                    var boost = behind switch { 0 => 1f, 1 => 0.55f, 2 => 0.25f, _ => 0f };
                    if (hovered) boost = Math.Max(boost, 0.35f);
                    Print(x, y, $"{Cp437.MediumShade}{Cp437.MediumShade}", Lerp(ground, glow, boost), soil);
                }
                x += 2;
            }
            Surface.SetGlyph(x++, y, fence, fenceGlyphColor);
            Print(x + 1, y, closed ? "CLOSED" : "OPEN", closed ? Palette.Bad : Palette.Good);
            Print(x + 8, y, powered ? "FENCE ON" : "FENCE OFF", powered || AlertOn ? fenceColor : Palette.BadDark);

            var details = $"{strip.Key.ToUpperInvariant()}: {(closed ? "closed to tours" : "open")}, fence {(powered ? "on" : "OFF")}, " +
                (people.Count > 0 ? $"guests: {string.Join(" & ", people)}" : "no guests") + " - click to search";
            hotspots.Add(new(rowArea, details, () =>
            {
                Show(Tab.Security);
                Busy("SEARCHING PADDOCK GRID", "GUEST SEARCH COMPLETE", RefreshAsync);
            }));
            y++;
        }

        y++;
        Print(area.X, y, $"{Cp437.MediumShade}{Cp437.MediumShade}", new Color(60, 170, 90), new Color(10, 45, 22));
        Print(area.X + 3, y, "open", Palette.Dim);
        Print(area.X + 9, y, $"{Cp437.MediumShade}{Cp437.MediumShade}", new Color(200, 60, 60), new Color(60, 14, 14));
        Print(area.X + 12, y, "closed", Palette.Dim);
        Print(area.X + 20, y, $"{Cp437.Smiley}{Cp437.Smiley}", Color.White, Palette.Bad);
        Print(area.X + 23, y, "guests", Palette.Dim);
        Surface.SetGlyph(area.X + 31, y, Cp437.DoubleVertical, Palette.Good);
        Print(area.X + 33, y, "fence on", Palette.Dim);
        Surface.SetGlyph(area.X + 43, y, ':', Palette.Bad);
        Print(area.X + 45, y, "fence off", Palette.Dim);
        Print(area.X + 56, y, "hover = info", Palette.Dim);
    }

    private void Print(int x, int y, string text, Color foreground, Color? background = null) =>
        Surface.Print(x, y, Cp437.Encode(text), foreground, background ?? Palette.Background);

    private static Color Lerp(Color from, Color to, float amount) => new(
        (int)(from.R + (to.R - from.R) * amount),
        (int)(from.G + (to.G - from.G) * amount),
        (int)(from.B + (to.B - from.B) * amount));

    private static string Clip(string text, int width) => text.Length <= width ? text : text[..(width - 1)] + "~";

    private static IEnumerable<string> Wrap(string text, int width)
    {
        if (text.Length == 0) yield return "";
        for (var i = 0; i < text.Length; i += width) yield return text.Substring(i, Math.Min(width, text.Length - i));
    }
}
