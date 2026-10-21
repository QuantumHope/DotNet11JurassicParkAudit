# Before: the park's original code

ParkOps as Dennis Nedry left it, with every incident still in place. Each spot a lesson fixes is marked `▶ INC-0NN`.

ParkOps.Story uses this code as the *before* side of every lesson:

- `ParkOps.Core`, `ParkOps.Data` and `ParkOps.Genetics.Plugin` are real projects the story references.
- The `ParkOps.ControlRoom` files are compiled into the story by link (see `ParkOps.Story.csproj`); the control-room
  app itself isn't kept.
- `ParkOps.Api/Program.cs`, `ParkOps.Narrator/Program.cs`, `scripts/diag.cs` and
  `ParkOps.ControlRoom/ParkOps.ControlRoom.csproj.txt` are shown on screen only. Their lessons replay captured runs.

The fixed versions live beside each lesson in `../ParkOps.Story/Chapters/IncNNN/`.
