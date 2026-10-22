namespace ParkOps.Story.Story;

public enum Severity { Critical, High, Medium, Low }

/// <summary>
/// One lesson in the report: the .NET 11 / C# 15 improvement it teaches (the headline), and the park incident
/// that shows why it matters. This is the talk's running order.
/// </summary>
public sealed record Finding(
    int Lesson, string Id, string Platform, string Feature, string Incident,
    string ReportedBy, string Role, Severity Severity, string Summary)
{
    /// <summary>"Lesson 02".</summary>
    public string LessonLabel => $"Lesson {Lesson:00}";
}

public static class Findings
{
    public static readonly Finding[] All =
    [
        new(1, "INC-001", "ASP.NET Core 11", "Async validation + OpenAPI 3.2", "Tours to closed paddocks",
            "Donald Gennaro", "Counsel, InGen investors", Severity.High,
            "The booking API accepts tours into closed paddocks."),
        new(2, "INC-002", "MEAI", "Failover chat client", "The narrator went silent",
            "John Hammond", "Founder, InGen", Severity.Low,
            "When the primary model is down, the tour narrator stops."),
        new(3, "INC-003", ".NET 11", "LINQ FullJoin", "The headcount",
            "Dr. Ian Malcolm", "Mathematician, chaos theory", Severity.Critical,
            "The animal census can never report a missing or extra animal."),
        new(4, "INC-004", "C# 15", "Union types", "\"Probably a guest\"",
            "Ray Arnold", "Chief Engineer", Severity.Critical,
            "Unrecognised motion contacts are assumed to be friendly guests."),
        new(5, "INC-005", ".NET 11", "JSON unions + SerializeAsyncEnumerable", "The sensor feed",
            "Ray Arnold", "Chief Engineer", Severity.Medium,
            "The live sensor feed waits for every sensor, then sends it all at once."),
        new(6, "INC-006", "C# 15", "Closed hierarchies", "Unauthorized species",
            "Dr. Henry Wu", "Chief Geneticist", Severity.High,
            "The lab added a species, and the feeding plan put meat-eaters on salad."),
        new(7, "INC-007", ".NET 11", "Runtime async", "Who turned off the fences?",
            "Ray Arnold", "Chief Engineer", Severity.Critical,
            "Fence shutdown leaves no usable stack trace."),
        new(8, "INC-008", "EF Core 11", "MaxByAsync", "Which fence fails most?",
            "Ray Arnold", "Chief Engineer", Severity.Medium,
            "A one-row question pulls every breach record into memory."),
        new(9, "INC-009", "C# 15", "Labeled break, extension indexers, with(...)", "Missing guests",
            "John Hammond", "Founder, InGen", Severity.High,
            "The guest locator sends the search team to the wrong guest."),
        new(10, "INC-010", ".NET 11 SDK", "File-based apps: #:include + RunAndCaptureTextAsync", "Who touched the system?",
            "Ray Arnold", "Chief Engineer", Severity.High,
            "Only one contractor could run the diagnostics script."),
    ];

    /// <summary>Lessons whose resolution page has been shown in this run.</summary>
    public static readonly HashSet<string> Resolved = [];

    /// <summary>Of those, the ones whose fix, run live, only partly fixed the incident.</summary>
    public static readonly HashSet<string> Partial = [];

    public static Finding Get(string id) => All.Single(f => f.Id == id);

    /// <summary>The part of the story a lesson belongs to: the park running, then danger, chaos, and control regained.</summary>
    public static string Act(int lesson) => lesson switch
    {
        <= 2 => "The park, running",
        <= 6 => "Signs of danger",
        <= 9 => "Chaos",
        _ => "Regaining control",
    };
}
