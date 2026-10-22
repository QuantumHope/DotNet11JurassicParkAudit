using Microsoft.Extensions.AI;
using OllamaSharp;

// Usage: dotnet run --project src/ParkOps.Narrator -- [primaryModel] [backupModel]
// Break the primary on stage with a bogus model name, e.g. -- dennis-nedry llama3.2:1b
var primaryModel = args.ElementAtOrDefault(0) ?? "llama3.2:1b";
var backupModel = args.ElementAtOrDefault(1) ?? "qwen2.5:0.5b";
var ollama = new Uri("http://localhost:11434");

#pragma warning disable MEAI001 // experimental
IChatClient primary = new OllamaApiClient(ollama, primaryModel);
IChatClient backup = new OllamaApiClient(ollama, backupModel);
IChatClient narrator = new OrderedFailoverChatClient([primary, backup]);

try
{
    var line = await narrator.GetResponseAsync("Welcome guests to the herbivore paddock in one cheerful sentence.");
    Console.WriteLine($"🔊 {line.Text}");
}
catch (Exception ex)
{
    Console.WriteLine($"(the narrator falls silent: {ex.GetType().Name})");
}
