# Unforeseen Consequences: Restoring Park Operations with .NET 11 and C# 15

One app tells the whole talk: **ParkOps.Story**, a Leading EDJE assessment of InGen's park software, played as a
slideshow. It opens on a title slide, then takes the ten lessons in order, one focused finding at a time, running the
original code and the fixed code side by side, and closes with the report to the board.

```bash
dotnet run --project ParkOps.Story/src/ParkOps.Story -- --fullscreen
```

The screen is 128x36 characters, exactly 16:9, so fullscreen on a 16:9 display or projector fills it edge to edge with
text as large as the layout allows. `F11` toggles fullscreen at any time; drop `--fullscreen` to start in a window.

Needs the .NET 11 RC2 SDK (`11.0.100-rc.2.26504.105`, pinned in `ParkOps.Story/global.json`). Ollama is optional:
lesson 2 uses a labelled stand-in when it isn't running.

## The lessons

The lessons follow the park's day: running normally, then the first signs of danger, then chaos, then getting control
back. Each section slide names its act.

| # | Act | Improvement | Platform | Incident |
|---|---|---|---|---|
| 1 | The park, running | Async validation + OpenAPI 3.2 | ASP.NET Core 11 | Tours to closed paddocks |
| 2 | The park, running | Failover chat client | Microsoft.Extensions.AI | The narrator went silent |
| 3 | Signs of danger | LINQ `FullJoin` | .NET 11 | The headcount |
| 4 | Signs of danger | Union types | C# 15 | "Probably a guest" (a raptor in the kitchen) |
| 5 | Signs of danger | JSON unions + `SerializeAsyncEnumerable` | .NET 11 | The sensor feed |
| 6 | Signs of danger | Closed hierarchies | C# 15 | Unauthorized species |
| 7 | Chaos | Runtime async | .NET 11 | Who turned off the fences? |
| 8 | Chaos | `MaxByAsync` | EF Core 11 | Which fence fails most? |
| 9 | Chaos | Labeled break, extension indexers, `with(...)` | C# 15 | Missing guests |
| 10 | Regaining control | File-based apps: `#:include` + `RunAndCaptureTextAsync` | .NET 11 SDK | Who touched the system? |

Each lesson opens on its own section slide, then runs **finding → before → root cause → fix → after → resolution**.
Some lessons add pages:

- **Lesson 3** draws the concept before the fix (what `Join` keeps versus what `FullJoin` keeps, as overlapping sets
  and as the actual rows), and after the fix takes the `FullJoin` call apart argument by argument, against its real
  signature.
- **Lesson 5** draws buffered versus streamed delivery as a timeline.
- **Lesson 6** draws the species hierarchy (Dinosaur and its species, the lab's compy sent to the greens), and the
  assembly boundary that closed stops the lab from crossing.
- **Lesson 7** opens on the perimeter fences, online, and ends with the tradeoffs of turning runtime async on.
- **Lesson 10** opens on the same fences, offline, and why `scripts/diag.cs` matters in an outage, then reads the whole
  script one flaw at a time before running it.

After the last lesson comes an encore (Dr. Ian Malcolm as an ASCII portrait), then the closing report.

## Keys

| Where | Keys |
|---|---|
| Anywhere | `→` `Space` next · `←` `Backspace` back · `R` replay the slide · `F11` fullscreen · `ESC` quit |
| Lessons | `PgDn` next lesson · `PgUp` start of this lesson, then the previous one |
| Exhibits | `TAB` flip between the original and the fixed code |
| Jumping | `Home` index of lessons (`↑` `↓` pick, `Enter` open) · `1`-`9`, `0` straight to a lesson |

Next and back run straight through the deck, across lessons: title → lessons 1-10 → encore → closing → index. A presentation
clicker that sends PgUp/PgDn will jump lessons; set it to send the arrow keys to step through slides.

## What's where

- `ParkOps.Story/src/ParkOps.Story`: the app. `Chapters/IncNNN/` holds each lesson's pages, exhibit and *fixed* code.
- `ParkOps.Story/src/Before`: the park's original code, with every incident still in it. The story compiles and runs it
  as the *before* side of each exhibit, and shows it on the root-cause pages.
- [ParkOps.Story/PLAN.md](ParkOps.Story/PLAN.md): how each lesson is simulated, and what building it found on RC2.
