using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.AI;
using OllamaSharp;
using ParkOps.ControlRoom.Ui;
using ParkOps.Story.Beats;
using ParkOps.Story.Rendering;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc002;

/// <summary>One model call as the exhibit saw it.</summary>
public sealed record Attempt(string Model, TimeSpan At, string Outcome, bool Failed);

/// <summary>Passes calls through to a real client and records each attempt and how it ended.</summary>
public sealed class TracingChatClient(string model, IChatClient inner, ConcurrentQueue<Attempt> log, Stopwatch clock) : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await base.GetResponseAsync(messages, options, cancellationToken);
            log.Enqueue(new(model, clock.Elapsed, "answered", false));
            return response;
        }
        catch (Exception ex)
        {
            log.Enqueue(new(model, clock.Elapsed, $"{ex.GetType().Name}: {ex.Message}", true));
            throw;
        }
    }
}

/// <summary>
/// Stands in for the backup model when Ollama isn't running on this machine: always answers with the same fixed sentence.
/// The exhibit labels it as a stand-in wherever it appears.
/// </summary>
public sealed class StandInChatClient : IChatClient
{
    public const string Line = "Welcome to the herbivore paddock, where the gentlest giants on Isla Nublar are waiting to meet you!";

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Line)));

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

/// <summary>
/// Exhibit for lesson 2: the tour narrator's call, made live with Microsoft.Extensions.AI. The primary is a real Ollama
/// client asking for "dennis-nedry", the bogus model ParkOps.Narrator's usage line suggests for breaking it on stage.
/// The backup is a real Ollama model when Ollama is reachable, otherwise the labelled stand-in.
/// </summary>
public sealed class NarratorExhibit(Finding finding, CodePath initial, Page page) : ExhibitBeat(finding, initial, page)
{
    private const string Prompt = "Welcome guests to the herbivore paddock in one cheerful sentence.";
    private const string PrimaryModel = "dennis-nedry";
    private const string BackupModel = "llama3.2:1b";
    private static readonly Uri Ollama = new("http://127.0.0.1:11434"); // not "localhost": skip a second refused attempt over IPv6

    private readonly ConcurrentQueue<Attempt> attempts = new();
    private Task<string>? call;
    private bool ollamaUp;

    private string BackupName => ollamaUp ? BackupModel : "stand-in (Ollama not running)";

    protected override string ScreenName => "TOUR VEHICLE // NARRATOR";

    protected override string Heading(CodePath path) =>
        path == CodePath.Before ? "The primary model is down: silence" : "The backup takes over";

    protected override string Caption(CodePath path) =>
        $"The narrator's call, made live with MEAI. Primary: Ollama \"{PrimaryModel}\"; " +
        (path == CodePath.Before ? "no backup." : ollamaUp ? $"backup: {BackupModel}." : "backup: a labelled stand-in (no Ollama here).");

    protected override (string Code, string From) CodeFor(CodePath path) => path == CodePath.Before
        ? ("OllamaApiClient", "ParkOps (original)")
        : ("OrderedFailover...", "MEAI 10.11");

    protected override string Reading(CodePath path) => path == CodePath.Before
        ? $"The narrator has exactly one model. When \"{PrimaryModel}\" can't answer, the call throws, the catch prints a note " +
          "to the console, and the guests hear nothing."
        : "OrderedFailoverChatClient tries each client in order: the primary fails exactly as before, and the backup answers. " +
          (ollamaUp ? "" : "Ollama isn't running here, so the backup is a labelled stand-in; the failover is the real MEAI client.");

    protected override void Run(CodePath path)
    {
        attempts.Clear();
        var clock = Stopwatch.StartNew();
        call = Task.Run(async () =>
        {
            ollamaUp = await IsOllamaUpAsync();
            IChatClient primary = new TracingChatClient(PrimaryModel, new OllamaApiClient(Ollama, PrimaryModel), attempts, clock);
            IChatClient narrator = primary;
            if (path == CodePath.After)
            {
                IChatClient backupClient = ollamaUp ? new OllamaApiClient(Ollama, BackupModel) : new StandInChatClient();
                var backup = new TracingChatClient(BackupName, backupClient, attempts, clock);
#pragma warning disable MEAI001 // experimental, as in the narrator itself
                narrator = new OrderedFailoverChatClient([primary, backup]);
#pragma warning restore MEAI001
            }
            try
            {
                return $"{(char)14} {(await narrator.GetResponseAsync(Prompt)).Text}";
            }
            catch (Exception ex)
            {
                return $"(the narrator falls silent: {ex.GetType().Name})";
            }
        });
    }

    private static async Task<bool> IsOllamaUpAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(600) };
            return (await http.GetAsync(new Uri(Ollama, "/api/tags"))).IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    protected override bool IsBusy(TimeSpan running) => call is not { IsCompleted: true } || running < TimeSpan.FromMilliseconds(Rerunning ? 900 : 2000);

    protected override ScreenState DrawScreen(Workstation ws, Rectangle inner, ExhibitClock clock)
    {
        var area = new Rectangle(inner.X + 1, inner.Y, inner.Width - 2, inner.Height);
        var x = ws.Chip(area.X, area.Y, "NARRATOR", Palette.Ink, Palette.Amber);
        ws.Print(x, area.Y, clock.Busy ? $" {Cp437.Bullet} CALLING " : $" {Cp437.Bullet} DONE ", clock.Busy ? (ws.Blink ? Palette.Amber : Palette.AmberDim) : Palette.Dim);
        ws.Print(area.X, area.Y + 1, Workstation.Clip($"prompt: {Prompt}", area.Width), Palette.Dim);

        ws.Print(area.X, area.Y + 3, "MODELS, IN ORDER", Palette.Dim);
        var models = Path == CodePath.Before ? new[] { PrimaryModel } : [PrimaryModel, BackupName];
        var y = area.Y + 4;
        foreach (var (index, model) in models.Index())
        {
            var attempt = attempts.FirstOrDefault(a => a.Model == model);
            ws.Print(area.X, y, $"{index + 1}. {model}", Palette.Text);
            if (attempt is { } a)
            {
                ws.Print(area.X + 44, y, a.Failed ? " FAILED " : " ANSWERED ", Color.White, a.Failed ? Palette.Bad : Palette.BadgeGood);
                ws.Print(area.X + 55, y, $"{a.At.TotalSeconds:0.00}s", Palette.Dim);
                foreach (var (i, line) in Workstation.Wrap(a.Outcome, area.Width - 3).Take(2).Index())
                    ws.Print(area.X + 3, y + 1 + i, line, a.Failed ? Palette.Warn : Palette.Dim);
            }
            else if (clock.Busy) ws.Print(area.X + 44, y, $" {ws.Spinner(110)} ", Palette.Amber);
            y += 4;
        }

        var spoken = call is { IsCompletedSuccessfully: true } done && !clock.Busy ? done.Result : null;
        var speakerY = area.Y + 14;
        ws.Chip(area.X, speakerY, "WHAT THE GUESTS HEAR", Palette.Ink, Palette.Amber);
        if (spoken is not null)
        {
            var silent = spoken.StartsWith('(');
            foreach (var (i, line) in Workstation.Wrap(spoken, area.Width).Take(4).Index())
                ws.Print(area.X, speakerY + 2 + i, line, silent ? Palette.Bad : Palette.Strong);
            var marks = Workstation.Wrap(spoken, area.Width).Take(4).Select((line, i) => Row(area.X, speakerY + 2 + i, Math.Max(1, line.Length))).ToList();
            return silent ? new("SILENT", ws.Blink ? Palette.Bad : Palette.BadDark, marks) : ScreenState.Clear(marks);
        }
        return new("CALLING", ws.Blink ? Palette.Amber : Palette.AmberDim, []);
    }
}
