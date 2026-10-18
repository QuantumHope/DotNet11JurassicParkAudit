using ParkOps.ControlRoom.Fences;

namespace ParkOps.ControlRoom.Ui;

public enum TestResult { NotRun, Fail, Pass, Manual }

/// <summary>What the acceptance tests can see: the latest readouts and the telemetry log.</summary>
public sealed record DemoState(
    Readout? Census, Readout? Motion, Readout? Feeding, Readout? Fences, Readout? Guests, Readout? Maintenance,
    IReadOnlyList<string> Telemetry);

/// <summary>One step of the talk, written as an acceptance test the consultant signs off.</summary>
public sealed record DemoTest(
    string Id,
    string Title,
    string Slot,
    string Stakeholder,
    string Feature,
    bool Shipping,
    Tab? Screen,
    string Symptom,
    string Fix,
    string Files,
    string Show,
    string Expect,
    Func<DemoState, TestResult> Check);

/// <summary>The demo, in running order. Each check reads the app's own state, so tests go green as fixes land.</summary>
public static class DemoScript
{
    private static bool Says(Readout? readout, string text) =>
        readout?.Lines.Any(l => l.Text.Contains(text, StringComparison.OrdinalIgnoreCase)) == true;

    private static TestResult When(Readout? readout, bool passed) =>
        readout is null ? TestResult.NotRun : passed ? TestResult.Pass : TestResult.Fail;

    public static readonly DemoTest[] Tests =
    [
        new("PREP", "Pre-flight: running on .NET 11 RC2", "before",
            "You", "Pre-flight checklist", Shipping: true, Screen: null,
            Symptom: "Anything but .NET 11 in the title bar means the wrong SDK or a stale build.",
            Fix: "global.json pins 11.0.100-rc.2; dotnet --version in the repo should agree.",
            Files: "global.json, Directory.Build.props, Directory.Packages.props",
            Show: "Before the talk: warm NuGet, delete parkops.db, make the dnedry commit, run diag.cs once.",
            Expect: "Title bar reads .NET 11.",
            Check: _ => Environment.Version.Major >= 11 ? TestResult.Pass : TestResult.Fail),

        new("INC-001", "Who touched the system?", "0:07",
            "Ray Arnold", "Multi-file dotnet run, RunAndCaptureTextAsync", Shipping: true, Screen: null,
            Symptom: "Only Nedry could run the 40-line ProcessStartInfo diagnostics script.",
            Fix: "Process.RunAndCaptureTextAsync one-liner; move helpers to diag-helpers.cs via #:include.",
            Files: "scripts/diag.cs, scripts/diag-helpers.cs (new)",
            Show: "dotnet run scripts/diag.cs - no .csproj; the top commit is dnedry's.",
            Expect: "Script runs and shows dnedry's 'disable fence safety checks' commit.",
            Check: _ => TestResult.Manual),

        new("INC-002", "The headcount", "0:11",
            "Ian Malcolm", "LINQ FullJoin", Shipping: true, Screen: Tab.Animals,
            Symptom: "Census says 238 of 238 located, ALL ACCOUNTED FOR.",
            Fix: "expected.Join(...) becomes expected.FullJoin(...) with null-coalesced labels.",
            Files: "ControlRoom/Panels/CensusPanel.cs",
            Show: "F2 ANIMALS, press RECOUNT. Pause for the room.",
            Expect: "238 expected / 254 tracked / 16 unaccounted for.",
            Check: s => When(s.Census, Says(s.Census, "unaccounted"))),

        new("INC-003", "\"Probably a guest\"  (never cut)", "0:16",
            "Ray Arnold", "C# 15 union types", Shipping: true, Screen: Tab.Security,
            Symptom: "Motion sensor in Paddock 9 reports GUEST detected (friendly). It's a raptor.",
            Fix: "union MotionContact(Dinosaur, Guest, TourVehicle, Employee); exhaustive switch; drop wrappers.",
            Files: "Core/Sensors/MotionContact.cs, ThreatAssessor.cs, SensorFeed.cs",
            Show: "F3 SECURITY, SWEEP SENSORS. Then add Unidentified to the union and build: warnings light up.",
            Expect: "Paddock 9 reads Velociraptor V-07, threat CRITICAL.",
            Check: s => When(s.Motion, Says(s.Motion, "CRITICAL"))),

        new("INC-004", "Unauthorized species", "0:24",
            "Henry Wu", "C# 15 closed hierarchies", Shipping: true, Screen: Tab.Animals,
            Symptom: "Feeding plan puts the Procompsognathus herd on lettuce.",
            Fix: "closed record Dinosaur; move Procompsognathus into Core; exhaustive feeding switch.",
            Files: "Core/Animals/Dinosaur.cs, Core/Feeding/FeedingPlanner.cs, Genetics.Plugin/WuLab.cs",
            Show: "Build first: the plugin fails. Then F2 ANIMALS, press FEEDING PLAN.",
            Expect: "Procompsognathus gets Small prey.",
            Check: s => When(s.Feeding, s.Feeding!.Lines.Any(l => l.Text.StartsWith("Procompsognathus") && l.Text.Contains("Small prey")))),

        new("INC-005", "Who turned off the fences?", "0:30",
            "Ray Arnold", "Runtime async (opt-in preview)", Shipping: false, Screen: Tab.Security,
            Symptom: "Fences OFF, disabled by: unknown. The stack is all MoveNext frames.",
            Fix: "Add <Features>$(Features);runtime-async=on</Features>. No C# changes.",
            Files: "ControlRoom/ParkOps.ControlRoom.csproj",
            Show: "F3 SECURITY, quick look FENCES. Show the two stack traces side by side.",
            Expect: "Disabled by: WhteRbt.ExecuteAsync.",
            Check: s => When(s.Fences, FenceController.DisabledBy is not null)),

        new("INC-006", "Missing guests", "0:34",
            "John Hammond", "Labeled break, extension indexer, with(...)", Shipping: true, Screen: Tab.Security,
            Symptom: "Guests found in paddock-9 but 'Zone clear'; search scans 393 of 400 cells.",
            Fix: "[with(StringComparer.OrdinalIgnoreCase), ...], extension indexer map[x, y], break search.",
            Files: "ControlRoom/Panels/GuestLocatorPanel.cs, ControlRoom/ParkMapExtensions.cs (new)",
            Show: "F3 SECURITY, LOCATE GUESTS.",
            Expect: "!! RESTRICTED ZONE !!, 333 cells scanned.",
            Check: s => When(s.Guests, Says(s.Guests, "RESTRICTED"))),

        new("INC-007", "Which fence fails most?", "0:38",
            "Ray Arnold", "EF Core 11 MaxByAsync, migrate --add", Shipping: true, Screen: Tab.Operations,
            Symptom: "Finding the worst fence pulls 1,138 rows into memory.",
            Fix: "await db.Fences.MaxByAsync(f => f.Breaches.Count()); add Fence.Voltage + one-step migration.",
            Files: "ControlRoom/Panels/MaintenancePanel.cs, Data/Fence.cs",
            Show: "F4 OPERATIONS, FIND REPAIR. Then: dotnet ef database update --add AddFenceVoltage",
            Expect: "Rows pulled into memory: 1.",
            Check: s => When(s.Maintenance, s.Maintenance!.Lines.Any(l => l.Text.EndsWith("memory: 1")))),

        new("INC-008", "Tours to closed paddocks", "0:43",
            "Donald Gennaro", "Async validation, OpenAPI 3.2 deprecated", Shipping: true, Screen: null,
            Symptom: "POST /tours to PADDOCK-9 (closed) returns 201. Override endpoint not marked deprecated.",
            Fix: "PaddockOpenAttribute : AsyncValidationAttribute; [Obsolete] SystemOverride endpoint.",
            Files: "Api/Program.cs, Api/PaddockOpenAttribute.cs (new)",
            Show: "dotnet run --project src/ParkOps.Api, then send ParkOps.Api.http.",
            Expect: "400 'Paddock is closed'; openapi 3.2.x with \"deprecated\": true.",
            Check: _ => TestResult.Manual),

        new("INC-009", "The sensor feed  (cuttable)", "0:47",
            "Ray Arnold", "JSON union serialization, JSON Lines", Shipping: true, Screen: Tab.Operations,
            Symptom: "Feed arrives as one giant array; staff contacts serialize as {}.",
            Fix: "Delete MotionContactJsonConverter; JsonSerializer.SerializeAsyncEnumerable.",
            Files: "ControlRoom/Feed/FeedCommand.cs, Feed/MotionContactJsonConverter.cs (delete)",
            Show: "F4 OPERATIONS, STREAM FEED.",
            Expect: "One line per contact, trickling in, staff included.",
            Check: s => s.Telemetry.Count == 0 ? TestResult.NotRun
                : s.Telemetry.Count >= 4 && !s.Telemetry.Any(t => t.StartsWith('[')) ? TestResult.Pass
                : TestResult.Fail),

        new("INC-010", "The narrator went silent  (cut first)", "0:50",
            "John Hammond", "MEAI failover chat client (experimental)", Shipping: false, Screen: null,
            Symptom: "Primary model down: the tour narrator falls silent.",
            Fix: "new OrderedFailoverChatClient([primary, backup]) with MEAI001 suppressed.",
            Files: "Narrator/Program.cs",
            Show: "dotnet run --project src/ParkOps.Narrator -- dennis-nedry llama3.2:1b (needs Ollama).",
            Expect: "The backup model narrates. Runs on .NET 10 too.",
            Check: _ => TestResult.Manual),
    ];
}
