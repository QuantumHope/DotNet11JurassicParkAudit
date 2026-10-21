# Unforeseen Consequences: Restoring Park Operations with .NET 11 and C# 15

One app tells the whole talk: **ParkOps.Story**, a Leading EDJE assessment of InGen's park software. It opens on the
executive summary, lists the ten lessons in running order, and walks each one from finding to fix, running the
original code and the fixed code side by side.

```bash
dotnet run --project ParkOps.Story/src/ParkOps.Story
```

Needs the .NET 11 RC2 SDK (`11.0.100-rc.2.26504.105`, pinned in `ParkOps.Story/global.json`). Ollama is optional:
lesson 10 uses a labelled stand-in when it isn't running.

## The lessons

| # | Improvement | Platform | Incident |
|---|---|---|---|
| 1 | File-based apps: `#:include` + `RunAndCaptureTextAsync` | .NET 11 SDK | Who touched the system? |
| 2 | LINQ `FullJoin` | .NET 11 | The headcount |
| 3 | Union types | C# 15 | "Probably a guest" |
| 4 | Closed hierarchies | C# 15 | Unauthorized species |
| 5 | Runtime async | .NET 11 | Who turned off the fences? |
| 6 | Labeled break, extension indexers, `with(...)` | C# 15 | Missing guests |
| 7 | `MaxByAsync` + migrations | EF Core 11 | Which fence fails most? |
| 8 | Async validation + OpenAPI 3.2 | ASP.NET Core 11 | Tours to closed paddocks |
| 9 | JSON unions + `SerializeAsyncEnumerable` | .NET 11 | The sensor feed |
| 10 | Failover chat client | Microsoft.Extensions.AI | The narrator went silent |

Each lesson is six pages: **finding → before → root cause → fix → after → resolution**.

## Keys

| Where | Keys |
|---|---|
| Summary | `↑` `↓` pick a lesson · `→` / `Enter` open it |
| In a lesson | `→` `Space` `PgDn` next · `←` `Backspace` `PgUp` back · `TAB` flip before/after on an exhibit · `R` replay the page |
| Anywhere | `Home` summary · `1`-`9`, `0` jump to a lesson · `ESC` quit |

Finishing a lesson returns to the summary with the next one selected.

## What's where

- `ParkOps.Story/src/ParkOps.Story`: the app. `Chapters/IncNNN/` holds each lesson's pages, exhibit and *fixed* code.
- `ParkOps.Story/src/Before`: the park's original code, with every incident still in it. The story compiles and runs it
  as the *before* side of each exhibit, and shows it on the root-cause pages.
- [ParkOps.Story/PLAN.md](ParkOps.Story/PLAN.md): how each lesson is simulated, and what building it found on RC2.
