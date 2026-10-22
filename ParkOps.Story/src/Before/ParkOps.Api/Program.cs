using System.ComponentModel.DataAnnotations;
using ParkOps.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddScoped(_ => ParkDb.Create());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    ParkDb.Prepare(scope.ServiceProvider.GetRequiredService<ParkDb>());
}

app.MapOpenApi();

app.MapPost("/tours", (TourBooking booking) => Results.Created($"/tours/{booking.GuestName}", booking));

// dnedry: emergency access. Leave it in.
// ▶ INC-001b: contractor endpoint still live, nothing marks it deprecated (fix: [Obsolete] → OpenAPI "deprecated")
app.MapPost("/system/override", () => Results.Ok("Override accepted."));

app.Run();

// ▶ INC-001a: validation is sync, so it can't ask the database if the paddock is open (fix: AsyncValidationAttribute)
public record TourBooking([Required] string GuestName, [Required] string Paddock); // dnedry: TODO check paddock is open
