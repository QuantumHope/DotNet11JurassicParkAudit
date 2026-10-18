namespace ParkOps.Story.Story;

public enum Severity { Critical, High, Medium, Low }

/// <summary>An incident as the assessment lists it. Mirrors the running order in ParkOps.Start's DemoScript.</summary>
public sealed record Finding(string Id, string Title, string ReportedBy, string Role, string Feature, Severity Severity, string Summary);

public static class Findings
{
    public static readonly Finding[] All =
    [
        new("INC-001", "Who touched the system?", "Ray Arnold", "Chief Engineer", "File-based apps: #:include, RunAndCaptureTextAsync", Severity.High,
            "Only one contractor could run the diagnostics script."),
        new("INC-002", "The headcount", "Dr. Ian Malcolm", "Mathematician, chaos theory", "LINQ FullJoin", Severity.Critical,
            "The animal census can never report a missing or extra animal."),
        new("INC-003", "\"Probably a guest\"", "Ray Arnold", "Chief Engineer", "C# 15 union types", Severity.Critical,
            "Unrecognised motion contacts are assumed to be friendly guests."),
        new("INC-004", "Unauthorized species", "Dr. Henry Wu", "Chief Geneticist", "C# 15 closed hierarchies", Severity.High,
            "Any assembly can define a species; the feeding plan guesses."),
        new("INC-005", "Who turned off the fences?", "Ray Arnold", "Chief Engineer", "Runtime async", Severity.Critical,
            "Fence shutdown leaves no usable stack trace."),
        new("INC-006", "Missing guests", "John Hammond", "Founder, InGen", "Labeled break, extension indexers, with(...)", Severity.High,
            "The guest locator finds guests, then reports the zone clear."),
        new("INC-007", "Which fence fails most?", "Ray Arnold", "Chief Engineer", "EF Core 11 MaxByAsync", Severity.Medium,
            "A one-row question pulls every breach record into memory."),
        new("INC-008", "Tours to closed paddocks", "Donald Gennaro", "Counsel, InGen investors", "Async validation, OpenAPI 3.2", Severity.High,
            "The booking API accepts tours into closed paddocks."),
        new("INC-009", "The sensor feed", "Ray Arnold", "Chief Engineer", "JSON union serialization, JSON Lines", Severity.Medium,
            "Telemetry arrives in one late batch and drops staff contacts."),
        new("INC-010", "The narrator went silent", "John Hammond", "Founder, InGen", "MEAI failover chat client", Severity.Low,
            "When the primary model is down, the tour narrator stops."),
    ];

    /// <summary>Findings whose resolution page has been shown in this run.</summary>
    public static readonly HashSet<string> Resolved = [];

    public static Finding Get(string id) => All.Single(f => f.Id == id);
}
