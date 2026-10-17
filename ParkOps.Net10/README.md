# ParkOps (.NET 10, as Dennis Nedry left it)

```
dotnet run --project src/ParkOps.ControlRoom          # control-room window (INC-002..007, 009); -- --quick skips the boot screen
dotnet run --project src/ParkOps.ControlRoom -- feed  # raw sensor feed to stdout (INC-009)
dotnet run scripts/diag.cs                            # diagnostics (INC-001)
dotnet run --project src/ParkOps.Api                  # booking API, see ParkOps.Api.http (INC-008)
dotnet run --project src/ParkOps.Narrator -- [primary] [backup]   # needs Ollama (INC-010)
```

Control room (SadConsole): F1 OVERVIEW · F2 ANIMALS · F3 SECURITY · F4 OPERATIONS · F5 TASKS, H help, ESC logs off.
F5 TASKS is the demo run sheet: every step in talk order as an acceptance test that goes green as its fix lands.
The steps live in `src/ParkOps.ControlRoom/Ui/DemoScript.cs`.
Hover anything for a hint in SYS MSG; click SYSTEM STATUS rows, paddocks and QUICK LOOK tabs.
The window can be resized or maximized; it scales to fit.

Every spot that changes for .NET 11 is marked `▶ INC-0NN` / `▶ STEP-0`. See `../Changes.md`.
Delete `parkops.db` to reseed.
