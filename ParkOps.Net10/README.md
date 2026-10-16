# ParkOps (.NET 10, as Dennis Nedry left it)

```
dotnet run --project src/ParkOps.ControlRoom          # dashboard (INC-002..007)
dotnet run --project src/ParkOps.ControlRoom -- feed  # sensor feed (INC-009)
dotnet run scripts/diag.cs                            # diagnostics (INC-001)
dotnet run --project src/ParkOps.Api                  # booking API, see ParkOps.Api.http (INC-008)
dotnet run --project src/ParkOps.Narrator -- [primary] [backup]   # needs Ollama (INC-010)
```

Every spot that changes for .NET 11 is marked `▶ INC-0NN` / `▶ STEP-0`. See `../Changes.md`.
Delete `parkops.db` to reseed.
