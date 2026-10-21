using ParkOps.Data;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadConsole;
using SadConsole.Configuration;

// dotnet run -- [--lesson N]                open lesson N straight away (2 for LINQ FullJoin)
// dotnet run -- --capture DIR [--lesson N]  play the report (or one lesson) unattended, saving a PNG of every step
string? Option(string name) => args.SkipWhile(a => a != name).Skip(1).FirstOrDefault();
int? lesson = int.TryParse(Option("--lesson"), out var n) ? n : null;
var capture = Option("--capture") is { } directory ? Path.GetFullPath(directory) : null;

// The park database lives next to the exe (ParkDb falls back to the working directory when it can't find
// ParkOps.slnx), so the original code and the fixed code query the same seeded data.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
using (var db = ParkDb.Create()) ParkDb.Prepare(db);

var chapters = Deck.Build();
var startAt = 0;
if (lesson is { } number)
{
    var beats = chapters.SelectMany(c => c.Beats.Select(_ => c)).ToList();
    startAt = Math.Max(0, beats.FindIndex(c => c.Number == number));
}

Settings.WindowTitle = "Leading EDJE // ParkOps Assessment";
Settings.AllowWindowResize = true;
Settings.ResizeMode = Settings.WindowResizeOptions.Fit;

var startup = new Builder()
    .SetWindowSizeInCells(Canvas.Width, Canvas.Height)
    .ConfigureFonts(true)
    .SetStartingScreen(_ =>
    {
        var screen = new StoryScreen(chapters, startAt);
        if (capture is not null)
        {
            if (lesson is not null) screen.StopAfterLessons = 1;
            new Rehearsal(screen, capture).Start();
        }
        return screen;
    })
    .IsStartingScreenFocused(true);

Game.Create(startup);
Game.Instance.Run();
Game.Instance.Dispose();
