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
app.MapPost("/system/override", SystemOverride);

[Obsolete("Contractor override endpoint. Scheduled for removal.")]
static IResult SystemOverride() => Results.Ok("Override accepted.");

app.Run();

public record TourBooking([Required] string GuestName, [Required, PaddockOpen] string Paddock);
