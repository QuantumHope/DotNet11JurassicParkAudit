# ParkOps.Story: implementation plan

A presenter-driven SadConsole app that tells the talk as a **Leading EDJE post-incident assessment** of
InGen's ParkOps. **The report is the focus**: it opens on the executive summary, and each .NET 11 / C# 15
improvement is a *lesson* in it, named for the feature it teaches. The park's control room supports the report
as *exhibits*: live screens set into report pages. Every fix is already compiled in: moving through the story
switches which code path is active, so there is nothing to rebuild on stage.

## Goals

- **Linear and animated.** Arrow keys or a clicker move forward and back one beat at a time.
- **Code-first.** The source on screen is the source that runs. Snippets are read from the real files
  (embedded at build time), never retyped.
- **Report first, park second.** Every page is a Leading EDJE report page. Before and after evidence is an
  exhibit inside the page, drawn with the control room's own `Palette`, bevels, chips and typewriter reveal,
  with the report's reading of it in a column alongside.
- **Can't break on stage.** No live builds and no outside services. Anything that only happens at build time, in
  a script or in a separate process replays a capture of a real run.

## Layout

```
ParkOps.Story/
  global.json, Directory.*.props   # pin the same SDK and package versions as ParkOps.Start (imports)
  ParkOps.Story.slnx
  src/ParkOps.Story/
    Program.cs                     # SadConsole host, 128x44 cells, same font as the control room
    Story/                         # engine: StoryScreen (navigation, transitions, story bar), Beat, Chapter, Deck
    Rendering/                     # Canvas (cells + 2x overlay for big type), Brand, ReportPage, Workstation
    Code/                          # SourceFile (embedded source), CSharpTokenizer, LineDiff, CodeView
    Beats/                         # page types: Summary (home), Finding, Exhibit (base), Code, Fix, Console, Resolution
    Chapters/IncNNN/               # one folder per lesson: its "after" code, its exhibit, Captures/*.txt
  src/ParkOps.Story.Fences.Before/ # lesson 5: Start's MaintenanceWindow.cs, built as Start builds it
  src/ParkOps.Story.Fences.After/  #           the same file, built with runtime-async=on
```

ParkOps.Start stays untouched. The story **links** the files it needs from it: `Palette`, `Cp437`,
`Readout`, and each incident's *before* source (for example `Panels/CensusPanel.cs`). The *after* version
of each file lives in `Chapters/IncNNN/` under a `ParkOps.Story.Fixed` namespace, so both compile into
one assembly side by side.

## Shape of the report

The app opens on the **executive summary**: the report's title, a short summary, a resolved-lessons
scorecard, and all ten lessons, each headlined by its improvement and platform (`.NET 11`, `C# 15`,
`EF Core 11`...), with the incident beneath it. `↑`/`↓` picks a lesson and `→`/`Enter` opens it. Finishing a
lesson returns here with the next one selected, and its status turns to RESOLVED.

## A lesson = six pages (or a few more)

| Page | Look | What happens | Steps |
|---|---|---|---|
| **Finding** | Report | Feature as the headline; platform, severity, incident, reporter; observation and impact | reveal paragraphs |
| **Before** | Report + exhibit | The control-room screen runs the real *before* code; caption and code-path panel alongside | the report's reading; lines it refers to are marked |
| **Root cause** | Report + code | The *before* source, syntax highlighted, with the bad line flagged | callout on the root cause |
| **Fix** | Report + code | Animated diff: removed lines go red and collapse, new lines type in | callout explaining the feature |
| **After** | Report + exhibit | Same exhibit with the *after* code active | the report's reading of what's now visible |
| **Resolution** | Report | Before/after metrics, change, recommendation, RESOLVED (or PARTIAL) stamp | stamp lands |

Fix pages take several files (editor tabs, one per step), including new files and deletions. Where a lesson's moment
is a build, a script or an HTTP exchange, a **Console** page replays a captured real run (`ConsoleCapture`), and it can
stand in for a Before/After exhibit (lessons 1 and 8). If a fix, run live, doesn't fully fix its incident, the
resolution says so and stamps PARTIAL; the summary shows it.

### Switching the active code path

Each exhibit gets its code paths as delegates, e.g.
`Before = ParkOps.ControlRoom.Panels.CensusPanel.Render`, `After = ParkOps.Story.Fixed.CensusPanel.Render`.
The beat decides which one runs. **TAB** on an exhibit flips between them live, which helps with Q&A.

### Code on screen

`SourceFile` reads an embedded `.cs` file, drops the `using`/`namespace` preamble, and unindents.
`CSharpTokenizer` highlights it (keywords, types, methods, strings with interpolation holes, comments,
numbers). `LineDiff` (an LCS over lines) drives the Fix animation. Long lines soft-wrap with a
continuation glyph.

## How each lesson is simulated

| Lesson | Before / After | Captured from a real run |
|---|---|---|
| 1 File-based apps | Console pages: both `diag.cs` versions run from ParkOps.Start in this repo | both runs |
| 2 LINQ FullJoin | Exhibit, both run live | |
| 3 Unions | Exhibit, both run live (union in `Fixed.Inc003`) | `Unidentified` added to the union: CS8509 |
| 4 Closed hierarchies | Exhibit, both run live (closed hierarchy in `Fixed.Inc004`) | the plugin build: CS9382 |
| 5 Runtime async | Exhibit: two assemblies from the same `MaintenanceWindow.cs` (extern aliases) | |
| 6 Labeled break, extension indexers, with(...) | Exhibit, both run live, with the scan replayed on a grid | |
| 7 MaxByAsync | Exhibit, both query the same seeded SQLite DB next to the exe | `database update --add` (fails on RC2) |
| 8 Async validation + OpenAPI 3.2 | Console pages: the real API answering ParkOps.Api.http's requests | both runs |
| 9 JSON unions + SerializeAsyncEnumerable | Exhibit, both stream live, timestamped | |
| 10 Failover chat client | Exhibit: real MEAI `OrderedFailoverChatClient`; primary is a real Ollama client for "dennis-nedry"; backup is a real model if Ollama is up, else a labelled stand-in | |

Captures live in `Chapters/IncNNN/Captures/*.txt`: real runs on .NET 11 RC2, with paths made repo-relative and
anything trimmed marked as trimmed.

### What building it found (Changes.md vs. RC2), and what changed

- **INC-005:** `FenceController` captured its stack *after* `await Task.Delay`, so the caller was never on it and the
  audit said "unknown" with or without runtime async. ParkOps.Start now captures before the first await, and the caller
  search skips compiler-generated types: "unknown" (state machines) before, `WhteRbt.ExecuteAsync` with
  `runtime-async=on`. If a change brings back "unknown" after the fix, lesson 5 stamps itself PARTIAL.
- **INC-007:** `dotnet ef database update --add` scaffolds a migration that recreates every table, so applying it
  fails. Changes.md now uses `dotnet ef migrations add` + `dotnet ef database update` (verified: adds `Voltage`).
- **INC-008:** `AsyncValidationAttribute` also requires the synchronous `IsValid` override; Changes.md now includes
  it. Mapping the `[Obsolete]` method raises CS0618, as expected.
- **INC-009:** `SerializeAsyncEnumerable` writes one streamed JSON array; Changes.md now passes `topLevelValues: true`
  for JSON Lines.
- Confirmed as written: unions, closed, extension indexers, labeled break, `with(...)`, `RunAndCaptureTextAsync`,
  `#:include`, `FullJoin`, `MaxByAsync`, `OrderedFailoverChatClient`, OpenAPI `deprecated`.

## Branding (from leadingedje.com `--le-theme-*`)

| Token | Hex | Use |
|---|---|---|
| le-green / shade | `#98C93D` / `#6FB03A` | Header accents, RESOLVED, added lines, strings |
| dark-gray / black | `#4D4E50` / `#353535` | Report ink, header band, code gutter |
| gray-shade | `#2A2A2A` | Code background |
| white / white-shade | `#F8F8F8` / `#ECECEC` | Report paper, code text |
| le-blue / vivid-blue | `#5C95FF` / `#49C6E5` | Keywords, types, feature badges |
| le-red | `#DA7552` | Severity, removed lines |
| beige / cream | `#C4B1AE` / `#EADEDA` | Comments, numbers |

Big headings use SadConsole's 2x font size on a transparent overlay surface. The logo is a text
wordmark: SadConsole can't render League Spartan.

## Controls

Summary: `↑`/`↓` pick a lesson, `→`/`Enter` open it. Lesson: `→` `Space` `PgDn` next · `←` `Backspace` `PgUp`
back (past the first page returns to the summary) · `Home` summary · `TAB` flip before/after on an exhibit ·
`R` replay the page · `1`-`9`, `0` open a lesson · `ESC` quit.
Going back lands on the previous page's final state, with no animation.

## Milestones

1. ~~Engine + lesson 2 end to end~~ (done)
2. ~~All ten lessons~~ (done)
3. Shared drawing: move `Bevel`/`Screen`/`Chip`/`Button` out of `ControlRoomScreen` into a reusable
   `Workstation` in ParkOps.Start so the story stops duplicating it.
4. Presenter extras: speaker-notes window on a second monitor, a timer against the `Slot` times
   in `DemoScript`.
