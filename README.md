# Unforeseen Consequences: Restoring Park Operations with .NET 11 and C# 15

One app tells the whole talk: **ParkOps.Story**, a Leading EDJE assessment of InGen's park software, played as a
slideshow. It opens on a title slide, then takes the ten lessons in order, one focused finding at a time, running the
original code and the fixed code side by side, and closes with the report to the board.

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

Each lesson opens on its own section slide, then runs **finding → before → root cause → fix → after → resolution**.
Lesson 1's root cause is a walkthrough of the whole script, one flaw at a time.

## Keys

| Where | Keys |
|---|---|
| Anywhere | `→` `Space` `PgDn` next · `←` `Backspace` `PgUp` back · `R` replay the slide · `ESC` quit |
| Exhibits | `TAB` flip between the original and the fixed code |
| Jumping | `Home` index of lessons (`↑` `↓` pick, `Enter` open) · `1`-`9`, `0` straight to a lesson |

Next and back run straight through the deck, across lessons: title → lessons 1-10 → closing → index.

## What's where

- `ParkOps.Story/src/ParkOps.Story`: the app. `Chapters/IncNNN/` holds each lesson's pages, exhibit and *fixed* code.
- `ParkOps.Story/src/Before`: the park's original code, with every incident still in it. The story compiles and runs it
  as the *before* side of each exhibit, and shows it on the root-cause pages.
- [ParkOps.Story/PLAN.md](ParkOps.Story/PLAN.md): how each lesson is simulated, and what building it found on RC2.
