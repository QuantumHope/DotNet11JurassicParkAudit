using ParkOps.Story.Beats;
using ParkOps.Story.Code;
using ParkOps.Story.Story;
using SadRogue.Primitives;

namespace ParkOps.Story.Chapters.Inc008;

/// <summary>
/// Lesson 8, ASP.NET Core 11 async validation + OpenAPI 3.2. Incident INC-008, "Tours to closed paddocks": the booking
/// API accepts a tour into the closed raptor paddock, and the contractor's override endpoint isn't marked deprecated.
/// Before and after are captures of the real API answering the requests in ParkOps.Api.http.
/// </summary>
public static class Inc008
{
    private const string ProgramPath = "src/ParkOps.Api/Program.cs";
    private const string AttributePath = "src/ParkOps.Api/PaddockOpenAttribute.cs";

    public static Chapter Build(Pages pages)
    {
        var finding = Findings.Get("INC-008");
        var title = finding.LessonLabel;
        var before = SourceFile.Load("before/Api/Program.cs", ProgramPath).From("app.MapPost(\"/tours\"");
        var after = SourceFile.Load("after/Inc008/Program.cs", ProgramPath).From("app.MapPost(\"/tours\"");
        var attribute = SourceFile.Load("after/Inc008/PaddockOpenAttribute.cs", AttributePath);

        return new(finding.Id, $"{finding.LessonLabel}  {finding.Feature}", finding.Lesson,
        [
            new FindingBeat(finding, "Tour booking API  (ParkOps.Api)", ProgramPath,
            [
                new("Observation",
                    "The booking API accepted a tour for Lex Murphy into PADDOCK-9, the raptor paddock, which has been closed to " +
                    "tours all season. And the contractor's /system/override endpoint is still live, published as a normal operation.",
                    "201", "the API's answer to a tour of a closed paddock"),
                new("Why it matters",
                    "The paddock's status is in the database, but the request's validation attributes run synchronously, so they " +
                    "can't ask it. Clients reading the OpenAPI document have no way to know the override is on its way out.",
                    "0", "validation rules that check whether a paddock is open"),
            ], pages.Next()),

            new ConsoleBeat(title, "Before", "Booking a closed paddock",
                ConsoleCapture.Load("capture/Inc008/api-before.txt"),
                new("201 Created|/system/override:", "Accepted",
                    "Both tours are accepted, the closed paddock included. The OpenAPI document is already 3.2.0, but nothing " +
                    "marks /system/override as deprecated.",
                    new Rectangle(28, 33, 90, 5)),
                pages.Next()),

            new CodeBeat(title, before,
            [
                new("public record TourBooking", "Validation can't reach the database",
                    "[Required] only checks the request itself. Whether PADDOCK-9 is open lives in ParkDb, and a synchronous " +
                    "validation attribute has no good way to ask it. So nothing asks.",
                    new Rectangle(28, 24, 90, 6)),
                new("app.MapPost(\"/system/override\"", "Live, and undocumented as such",
                    "The emergency endpoint is mapped like any other. Nothing in the code or the published contract says clients " +
                    "should stop using it.",
                    new Rectangle(28, 24, 90, 6)),
            ], pages.Next()),

            new FixBeat(title, $"{finding.Platform}  ·  {finding.Feature}",
            [
                new FileFix(before, after, new("[Obsolete(", "[Obsolete] flows into OpenAPI",
                    "Map the endpoint to a named method marked [Obsolete] and the OpenAPI 3.2 document marks the operation " +
                    "\"deprecated\": true. TourBooking's Paddock gains [PaddockOpen].",
                    new Rectangle(28, 24, 90, 6))),
                new FileFix(SourceFile.Empty(AttributePath), attribute, new("IsValidAsync", "New in .NET 11",
                    "AsyncValidationAttribute lets a validation attribute await: it gets services from the ValidationContext and " +
                    "asks the database. On RC2 it must also override the synchronous IsValid.",
                    new Rectangle(28, 31, 90, 6))),
            ], pages.Next()),

            new ConsoleBeat(title, "After", "The same requests, validated",
                ConsoleCapture.Load("capture/Inc008/api-after.txt"),
                new("is closed. Tour denied.|\"deprecated\": true", "Denied, with a reason",
                    "PADDOCK-9 now gets 400 with the attribute's message; PADDOCK-1 is still booked. And the published contract " +
                    "marks /system/override \"deprecated\": true.",
                    new Rectangle(28, 33, 90, 5)),
                pages.Next()),

            new ResolutionBeat(finding, () => new(
                    "201", "for a tour of the closed raptor paddock; the override endpoint looks like any other.",
                    "400", "\"Paddock 'PADDOCK-9' is closed. Tour denied.\" The override is \"deprecated\": true in OpenAPI."),
                "PaddockOpenAttribute (AsyncValidationAttribute) checks ParkDb; TourBooking.Paddock uses it. /system/override maps to " +
                "an [Obsolete] method.",
                "Put rules that need data into async validators rather than endpoint code. Deprecate with [Obsolete] so the " +
                "published contract says so.",
                pages.Next()),
        ]);
    }
}
