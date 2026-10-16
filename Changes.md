# ParkOps: .NET 10 → .NET 11 changes

> **Draft.** Written against `ParkOps.Net10`. Steps tagged ⚠ use .NET 11 APIs that haven't been compiled yet; confirm them on RC2 when building `ParkOps.Net11`.

Start: `Copy-Item ParkOps.Net10 ParkOps.Net11 -Recurse` (skip `bin`, `obj`, `parkops.db`). Every edit site is marked `▶ INC-0NN` / `▶ STEP-0`.

## Pre-flight (not on stage)

For the INC-001 reveal, `diag.cs` runs `git log` in the current folder:

```bash
git commit --allow-empty --author="dnedry <dnedry@ingen.com>" -m "temporarily disable fence safety checks (will revert)"
```

---

## Step 0: Retarget (before the talk)

`global.json`

```diff
-    "version": "10.0.302",
+    "version": "11.0.100-rc.2.XXXXX.X",
     "rollForward": "latestFeature",
-    "allowPrerelease": false
+    "allowPrerelease": true
```

`Directory.Build.props`

```diff
-    <TargetFramework>net10.0</TargetFramework>
-    <LangVersion>14</LangVersion>
+    <TargetFramework>net11.0</TargetFramework>
+    <LangVersion>15</LangVersion>
```

⚠ Use `preview` if RC2 still gates unions/closed behind it.

`Directory.Packages.props`

```diff
-    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.13" />
-    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.13" />
-    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.13" />
+    <PackageVersion Include="Microsoft.EntityFrameworkCore.Sqlite" Version="11.0.0-rc.2.26504.105" />
+    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="11.0.0-rc.2.26504.105" />
+    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="11.0.0-rc.2.26504.105" />
```

(Microsoft.Extensions.AI / OllamaSharp stay put: the AI stack already runs on .NET 10.)

`.config/dotnet-tools.json`

```diff
-      "version": "10.0.13",
+      "version": "11.0.0-rc.2.26504.105",
```

---

## INC-001: Who touched the system? ⚠

`scripts/diag.cs`: replace the helper call and **delete** the whole `RunAndCaptureAsync` local function.

```diff
-var log = await RunAndCaptureAsync("git", "log", "-5", "--format=%h  %an  %s");
-AnsiConsole.WriteLine(log);
+var log = await Process.RunAndCaptureTextAsync("git", ["log", "-5", "--format=%h  %an  %s"]);
+AnsiConsole.WriteLine(log.StandardOutput);
```

Then cut the `static class Diagnostics { ... }` block into a new file and include it:

```diff
 #:package Spectre.Console
+#:include diag-helpers.cs
```

`scripts/diag-helpers.cs` (new)

```csharp
using System.Runtime.InteropServices;
using Spectre.Console;

static class Diagnostics
{
    // ...pasted PrintRuntimeInfo, unchanged...
}
```

Remove the now-unused `using System.Runtime.InteropServices;` and `using System.Text;` from `diag.cs`.

---

## INC-002: The headcount ⚠

`src/ParkOps.ControlRoom/Panels/CensusPanel.cs`

```csharp
// before
var census = expected.Join(tracked, e => e.Tag, t => t.Tag, (e, t) => e);
var body = new Markup($"[green]{census.Count()} / {expected.Count} ✅ ALL ACCOUNTED FOR[/]");
```

```csharp
// after
var audit = expected.FullJoin(tracked, e => e.Tag, t => t.Tag,
    (e, t) => (Expected: e?.Tag ?? "— NOT ON ROSTER —", Tracked: t?.Tag ?? "— NO SIGNAL —"));
var problems = audit.Where(r => r.Expected.StartsWith('—') || r.Tracked.StartsWith('—')).ToList();
var body = new Markup(
    $"[red]{expected.Count} expected · {tracked.Count} tracked · {problems.Count} unaccounted for[/]\n" +
    string.Join("\n", problems.Select(r => $"{r.Expected,-20} {r.Tracked}")));
```

Result: `238 expected · 254 tracked · 16 unaccounted for`.

---

## INC-003: "Probably a guest" ⭐ ⚠

`src/ParkOps.Core/Sensors/MotionContact.cs`: replace the base record and all four wrappers.

```csharp
// after (keep the using + namespace)
public union MotionContact(Dinosaur, Guest, TourVehicle, Employee);
```

`src/ParkOps.Core/Sensors/ThreatAssessor.cs` (add `using ParkOps.Core.Animals;`)

```csharp
// before
VehicleContact v => new(Threat.None, $"Tour vehicle {v.Vehicle.CallSign}"),
StaffContact s   => new(Threat.None, $"Staff: {s.Employee.Name}"),
_                => new(Threat.None, "GUEST detected (friendly)"), // dnedry: probably a guest
```

```csharp
// after
Dinosaur d    => new(d.IsCarnivore ? Threat.Critical : Threat.Low, $"{d.GetType().Name} {d.Tag}"),
Guest g       => new(Threat.None, $"Guest: {g.Name}"),
TourVehicle v => new(Threat.None, $"Tour vehicle {v.CallSign}"),
Employee e    => new(Threat.None, $"Staff: {e.Name}"),
```

`src/ParkOps.Core/Sensors/SensorFeed.cs`: drop the wrappers.

```csharp
// after
new("Paddock 9",      new Velociraptor("V-07")),
new("Visitor Center", new Guest("Donald Gennaro")),
new("Main Road",      new TourVehicle("Explorer 04")),
new("East Dock",      new Employee("Robert Muldoon", "Game Warden")),
```

**Reveal (demo only, revert afterwards):** add `Unidentified` (already in `Sensors/Unidentified.cs`) and build.

```csharp
public union MotionContact(Dinosaur, Guest, TourVehicle, Employee, Unidentified);
```

---

## INC-004: Unauthorized species ⚠

`src/ParkOps.Core/Animals/Dinosaur.cs`

```diff
-public abstract record Dinosaur(string Tag)
+public closed record Dinosaur(string Tag)
```

Build. `ParkOps.Genetics.Plugin` now fails. Then **move** the `Procompsognathus` record from `src/ParkOps.Genetics.Plugin/WuLab.cs` to the bottom of `Dinosaur.cs`, fixing it:

```csharp
public record Procompsognathus(string Tag) : Dinosaur(Tag) { public override bool IsCarnivore => true; }
```

`src/ParkOps.Core/Feeding/FeedingPlanner.cs`

```csharp
// before
Velociraptor or TyrannosaurusRex or Dilophosaurus => FeedingPlan.Livestock,
_                                                  => FeedingPlan.Greens,
```

```csharp
// after
Velociraptor or TyrannosaurusRex or Dilophosaurus => FeedingPlan.Livestock,
Procompsognathus                                  => FeedingPlan.SmallPrey,
Brachiosaurus or Triceratops or Gallimimus        => FeedingPlan.Greens,
```

(`WuLab.NewArrivals()` still compiles: the lab can *breed* compies, it just can't *define* species.)
⚠ If RC2 needs a `ClosedAttribute` polyfill, add it to Core.

---

## INC-005: Who turned off the fences? ⚠

`src/ParkOps.ControlRoom/ParkOps.ControlRoom.csproj`, under the marker:

```xml
<Features>$(Features);runtime-async=on</Features>
```

No code changes. Fence panel goes from `cause: unknown` + `MoveNext` frames to `cause: WhteRbt.ExecuteAsync`.

---

## INC-006: Missing guests ⚠

`src/ParkOps.ControlRoom/ParkMapExtensions.cs` (new)

```csharp
using ParkOps.Core.Mapping;

namespace ParkOps.ControlRoom;

public static class ParkMapExtensions
{
    extension(ParkMap map)
    {
        public Cell this[int x, int y] => map.Cells[y * map.Width + x];
    }
}
```

`src/ParkOps.ControlRoom/Panels/GuestLocatorPanel.cs`

```csharp
// before
var restricted = new HashSet<string> { "PADDOCK-9", "RAPTOR-PEN", "VISITOR-CENTER-KITCHEN" };

Cell? found = null;
var scanned = 0;
var foundThem = false;
for (var y = 0; y < map.Height; y++)
{
    for (var x = 0; x < map.Width; x++)
    {
        scanned++;
        var cell = map.Cells[y * map.Width + x];
        if (cell.Occupants.Any(o => o is Guest)) { found = cell; foundThem = true; break; }
    }
    if (foundThem) continue; // dnedry: stop searching
}
```

```csharp
// after
HashSet<string> restricted = [with(StringComparer.OrdinalIgnoreCase), "PADDOCK-9", "RAPTOR-PEN", "VISITOR-CENTER-KITCHEN"];

Cell? found = null;
var scanned = 0;
search:
for (var y = 0; y < map.Height; y++)
{
    for (var x = 0; x < map.Width; x++)
    {
        scanned++;
        if (map[x, y].Occupants.Any(o => o is Guest)) { found = map[x, y]; break search; }
    }
}
```

Result: `⚠ RESTRICTED ZONE`, cells scanned 393 → 333.

---

## INC-007: Which fence fails most? ⚠

`src/ParkOps.ControlRoom/Panels/MaintenancePanel.cs`

```csharp
// before
var fences = await db.Fences.Include(f => f.Breaches).ToListAsync();
var worst = fences.MaxBy(f => f.Breaches.Count);
```

```csharp
// after
var worst = await db.Fences.MaxByAsync(f => f.Breaches.Count());
```

Rows pulled into memory: 1,138 → 1.

`src/ParkOps.Data/Fence.cs`, under the marker:

```csharp
public int Voltage { get; set; }
```

```bash
dotnet ef database update --add AddFenceVoltage --project src/ParkOps.Data
```

---

## INC-008: Tours to closed paddocks ⚠

`src/ParkOps.Api/PaddockOpenAttribute.cs` (new)

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ParkOps.Data;

public sealed class PaddockOpenAttribute : AsyncValidationAttribute
{
    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        var db = context.GetRequiredService<ParkDb>();
        var code = value as string;
        return await db.Paddocks.AnyAsync(p => p.Code == code && p.IsOpen, cancellationToken)
            ? ValidationResult.Success
            : new ValidationResult($"Paddock '{code}' is closed. Tour denied.");
    }
}
```

`src/ParkOps.Api/Program.cs`

```diff
-public record TourBooking([Required] string GuestName, [Required] string Paddock);
+public record TourBooking([Required] string GuestName, [Required, PaddockOpen] string Paddock);
```

```csharp
// before
app.MapPost("/system/override", () => Results.Ok("Override accepted."));
```

```csharp
// after
app.MapPost("/system/override", SystemOverride);

[Obsolete("Contractor override endpoint. Scheduled for removal.")]
static IResult SystemOverride() => Results.Ok("Override accepted.");
```

Check with `ParkOps.Api.http`: PADDOCK-9 → 400; `/openapi/v1.json` shows `"openapi": "3.2.x"` and `"deprecated": true`.

---

## INC-009: The sensor feed (cuttable) ⚠

Delete `src/ParkOps.ControlRoom/Feed/MotionContactJsonConverter.cs`.

`src/ParkOps.ControlRoom/Feed/FeedCommand.cs`

```csharp
// before
var options = new JsonSerializerOptions { Converters = { new MotionContactJsonConverter() } };

var batch = new List<SensorPing>();
await foreach (var ping in SensorFeed.ReadAllAsync()) batch.Add(ping);
Console.WriteLine(JsonSerializer.Serialize(batch, options));
```

```csharp
// after
await JsonSerializer.SerializeAsyncEnumerable(Console.OpenStandardOutput(), SensorFeed.ReadAllAsync());
```

Run: `dotnet run --project src/ParkOps.ControlRoom -- feed`. One line per contact, staff included.

---

## INC-010: The narrator went silent (cut first)

`src/ParkOps.Narrator/Program.cs`

```csharp
// before
IChatClient narrator = new OllamaApiClient(ollama, primaryModel);
```

```csharp
// after
#pragma warning disable MEAI001 // experimental
IChatClient primary = new OllamaApiClient(ollama, primaryModel);
IChatClient backup = new OllamaApiClient(ollama, backupModel);
IChatClient narrator = new OrderedFailoverChatClient([primary, backup]);
```

Run: `dotnet run --project src/ParkOps.Narrator -- dennis-nedry llama3.2:1b`. (Verified to compile on .NET 10 with MEAI 10.11.)
