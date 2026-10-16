using ParkOps.ControlRoom.Feed;
using ParkOps.ControlRoom.Fences;
using ParkOps.ControlRoom.Panels;
using ParkOps.Core.Animals;
using ParkOps.Core.Mapping;
using ParkOps.Core.Sensors;
using ParkOps.Data;
using ParkOps.Genetics.Plugin;
using Spectre.Console;

if (args is ["feed"])
{
    await FeedCommand.RunAsync();
    return;
}

// dnedry: scheduled maintenance. Runs on every startup. Leave it alone.
await MaintenanceWindow.RunAsync();

using (var db = ParkDb.Create())
{
    ParkDb.Prepare(db);
}

List<Dinosaur> roster = [.. ParkRoster.CoreAnimals(), .. WuLab.NewArrivals()];

AnsiConsole.Write(new Rule("[yellow]INGEN // PARK OPERATIONS // CONTROL ROOM[/]"));

var grid = new Grid().AddColumn().AddColumn();
grid.AddRow(CensusPanel.Render(roster, SensorNetwork.Sweep()), MotionPanel.Render(SensorFeed.Current()));
grid.AddRow(FeedingPanel.Render(roster), FencePanel.Render());
grid.AddRow(GuestLocatorPanel.Render(ParkMap.Load()), await MaintenancePanel.RenderAsync());
AnsiConsole.Write(grid);
