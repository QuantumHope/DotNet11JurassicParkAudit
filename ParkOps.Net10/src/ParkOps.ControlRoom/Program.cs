using ParkOps.ControlRoom.Feed;
using ParkOps.ControlRoom.Fences;
using ParkOps.ControlRoom.Ui;
using ParkOps.Core.Animals;
using ParkOps.Core.Mapping;
using ParkOps.Data;
using ParkOps.Genetics.Plugin;
using SadConsole;
using SadConsole.Configuration;

// Startup stays synchronous: the game window has to be created on the main thread.
if (args is ["feed"])
{
    using var stdout = System.Console.OpenStandardOutput();
    FeedCommand.RunAsync(stdout).GetAwaiter().GetResult();
    return;
}

// dnedry: scheduled maintenance. Runs on every startup. Leave it alone.
MaintenanceWindow.RunAsync().GetAwaiter().GetResult();

using (var db = ParkDb.Create())
{
    ParkDb.Prepare(db);
}

List<Dinosaur> roster = [.. ParkRoster.CoreAnimals(), .. WuLab.NewArrivals()];
var map = ParkMap.Load();

Settings.WindowTitle = "InGen // Park Ops";
Settings.AllowWindowResize = true;
Settings.ResizeMode = Settings.WindowResizeOptions.Fit;

var startup = new Builder()
    .SetWindowSizeInCells(ControlRoomScreen.ScreenWidth, ControlRoomScreen.ScreenHeight)
    .ConfigureFonts(true)
    .SetStartingScreen(_ => args.Contains("--quick")
        ? new ControlRoomScreen(roster, map)
        : new BootScreen(() => new ControlRoomScreen(roster, map)))
    .IsStartingScreenFocused(true);

Game.Create(startup);
Game.Instance.Run();
Game.Instance.Dispose();
