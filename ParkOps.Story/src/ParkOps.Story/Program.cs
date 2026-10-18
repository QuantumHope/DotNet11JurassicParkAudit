using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadConsole;
using SadConsole.Configuration;

// dotnet run -- [--lesson N]     open lesson N straight away (2 for LINQ FullJoin)
// dotnet run -- --capture DIR     play the deck unattended, saving a PNG of every step
var chapters = Deck.Build();
var startAt = 0;
if (args is ["--lesson", var number] && int.TryParse(number, out var n))
{
    var beats = chapters.SelectMany(c => c.Beats.Select(_ => c)).ToList();
    startAt = Math.Max(0, beats.FindIndex(c => c.Number == n));
}
var capture = args is ["--capture", var directory] ? directory : null;

Settings.WindowTitle = "Leading EDJE // ParkOps Assessment";
Settings.AllowWindowResize = true;
Settings.ResizeMode = Settings.WindowResizeOptions.Fit;

var startup = new Builder()
    .SetWindowSizeInCells(Canvas.Width, Canvas.Height)
    .ConfigureFonts(true)
    .SetStartingScreen(_ =>
    {
        var screen = new StoryScreen(chapters, startAt);
        if (capture is not null) new Rehearsal(screen, capture).Start();
        return screen;
    })
    .IsStartingScreenFocused(true);

Game.Create(startup);
Game.Instance.Run();
Game.Instance.Dispose();
