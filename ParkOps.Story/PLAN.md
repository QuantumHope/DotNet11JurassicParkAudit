# ParkOps.Story: implementation plan

A presenter-driven SadConsole app that tells the talk as a **Leading EDJE post-incident assessment** of
InGen's ParkOps, played as a **slideshow**: a title slide, then each .NET 11 / C# 15 improvement as a *lesson*, named
for the feature it teaches and opened by its own section slide, then a closing slide. The park's control room supports the report
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
  global.json, Directory.*.props   # .NET 11 RC2 SDK, net11.0 / C# 15, central package versions
  ParkOps.Story.slnx
  src/Before/                      # the park's original code, every incident in place (see its README)
  src/ParkOps.Story/
    Program.cs                     # SadConsole host, 128x36 cells (16:9), same font as the control room
    Story/                         # engine: StoryScreen (navigation, transitions, story bar), Beat, Chapter, Deck
    Rendering/                     # Canvas (cells + 2x overlay for big type), Brand, ReportPage, Workstation
    Code/                          # SourceFile (embedded source), CSharpTokenizer, LineDiff, CodeView
    Beats/                         # slides: Title, LessonIntro, Closing; pages: Finding, Exhibit (base), Code,
                                   #   Walkthrough, Fix, Console, Tradeoffs, Resolution; Summary (the HOME index)
    Chapters/IncNNN/               # one folder per lesson: its "after" code, its exhibit, Captures/*.txt
  src/ParkOps.Story.Fences.Before/ # lesson 7: the original MaintenanceWindow.cs, built as originally built
  src/ParkOps.Story.Fences.After/  #           the same file, built with runtime-async=on
```

The story is the only app. The park's original code lives in `src/Before`: the story references its Core, Data and
Genetics.Plugin projects and **links** the control-room files it needs (`Palette`, `Cp437`, `Readout`, and each
incident's *before* source, for example `Panels/CensusPanel.cs`). Treat `src/Before` as the "as found" evidence:
change it only to stage an incident. The *after* version
of each file lives in `Chapters/IncNNN/` under a `ParkOps.Story.Fixed` namespace, so both compile into
one assembly side by side.

## Shape of the deck

One straight line, so the talk is just "next": **title** (the talk's name, the hook) → each lesson → **closing**
(resolved count, every lesson ticked off, the recommendation) → **index**. Next and back cross lesson boundaries.
The index is the old executive summary table: every lesson by improvement, platform, severity and status. It's for
jumping (`Home`, then `↑`/`↓` and `Enter`), not part of the talk.

## A lesson = a section slide and six pages (or a few more)

| Page | Look | What happens | Steps |
|---|---|---|---|
| **Intro** | Dark slide | Lesson number, the improvement, the incident in one sentence, a 1-10 progress strip | types in |
| **Finding** | Report | Feature as the headline; platform, severity, incident, reporter; observation and impact | reveal paragraphs |
| **Before** | Report + exhibit | The control-room screen runs the real *before* code; caption and code-path panel alongside | the report's reading; lines it refers to are marked |
| **Root cause** | Report + code | The *before* source, syntax highlighted, with the bad line flagged | callout on the root cause |
| **Fix** | Report + code | Animated diff: removed lines go red and collapse, new lines type in | callout explaining the feature |
| **After** | Report + exhibit | Same exhibit with the *after* code active | the report's reading of what's now visible |
| **Resolution** | Report | Before/after metrics, change, recommendation, RESOLVED (or PARTIAL) stamp | stamp lands |

Fix pages take several files (editor tabs, one per step), including new files and deletions. Where a lesson's moment
is a build, a script or an HTTP exchange, a **Console** page replays a captured real run (`ConsoleCapture`), and it can
stand in for a Before/After exhibit (lessons 1 and 10). If a fix, run live, doesn't fully fix its incident, the
resolution says so and stamps PARTIAL; the closing slide and the index show it.

A **Walkthrough** page can stand in for Root cause (lesson 10): the whole before file on the left, scrolled to and
highlighted at one stop per step, with a numbered narrative on the right that ticks off the earlier stops.

Lesson-specific pages:

- **Lesson 3, Concept and Anatomy** (`Chapters/Inc003/JoinConceptBeat`, `FullJoinAnatomyBeat`): between root cause and
  fix, two overlapping sets (roster, motion grid) and a six-tag sample: the lists, what Join keeps, what FullJoin keeps
  (all three regions, the missing side null), then the park's real counts. After the fix, the FullJoin call one argument
  per step (outer, inner, both key selectors, the result selector and its nulls, then the optional comparer and the
  tuple overload), with the real RC2 signature along the bottom.
- **Lesson 5, Timeline** (`Chapters/Inc005/FeedTimelineBeat`): after the finding. Four sensors on a time axis, then
  buffered (nothing until the last, one array) and streamed (one line per contact on arrival), with a sweeping playhead.
- **Lesson 6, Species and Boundary** (`Chapters/Inc006/SpeciesTreeBeat`, `AssemblyBoundaryBeat`): the hierarchy as a
  tree (Dinosaur, each species and its diet, then the lab's compy sent to the greens), after the finding; and, before
  the fix, Core and the lab's plugin as two assemblies, abstract (the compy derives across the line) then closed (CS9382,
  the compy moves into Core).
- **Lesson 7, Fences and Tradeoffs** (`Chapters/Inc010/FencesBeat`, `Beats/TradeoffsBeat`): the lesson opens on the
  fence diagram, online, with a callout on the controller's audit line, and closes with what runtime async gets you
  versus reasons to wait. There's no root-cause code page: the lesson is about what the audit can see.
- **Lesson 10, Fences** (`FencesBeat` again, offline): dark fences, a dead power path, and the callout that getting them
  back starts with `scripts/diag.cs`. Then straight to the script walkthrough, with no separate finding page.
- **Encore** (`Beats/MalcolmBeat`), after the last lesson: Dr. Ian Malcolm as an ASCII portrait, breathing, then a column
  of patched Jurassic Park lines.

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
| 1 Async validation + OpenAPI 3.2 | Console pages: the real API answering ParkOps.Api.http's requests | both runs |
| 2 Failover chat client | Exhibit: real MEAI `OrderedFailoverChatClient`; primary is a real Ollama client for "dennis-nedry"; backup is a real model if Ollama is up, else a labelled stand-in | |
| 3 LINQ FullJoin | Exhibit, both run live | |
| 4 Unions | Exhibit, both run live (union in `Fixed.Inc004`), with the kitchen camera | `Unidentified` added to the union: CS8509 |
| 5 JSON unions + SerializeAsyncEnumerable | Exhibit, both stream live, timestamped | |
| 6 Closed hierarchies | Exhibit, both run live (closed hierarchy in `Fixed.Inc006`) | the plugin build: CS9382 |
| 7 Runtime async | Exhibit: two assemblies from the same `MaintenanceWindow.cs` (extern aliases) | |
| 8 MaxByAsync | Exhibit, both query the same seeded SQLite DB next to the exe | |
| 9 Labeled break, extension indexers, with(...) | Exhibit, both run live, with each version's real visit order replayed on a grid | |
| 10 File-based apps | Console pages: both `diag.cs` versions run in this repo | both runs |

The order follows the park's day: the park running (1-2), signs of danger (3-6), chaos (7-9), regaining control
(10). `Findings.Act` names each act on the section slides.

Captures live in `Chapters/IncNNN/Captures/*.txt`: real runs on .NET 11 RC2, with paths made repo-relative and
anything trimmed marked as trimmed.

### What building it found on RC2

- **INC-007:** `FenceController` captured its stack *after* `await Task.Delay`, so the caller was never on it and the
  audit said "unknown" with or without runtime async. The original code now captures before the first await, and the
  caller search skips compiler-generated types: "unknown" (state machines) before, `WhteRbt.ExecuteAsync` with
  `runtime-async=on`. If a change brings back "unknown" after the fix, lesson 7 stamps itself PARTIAL.
- **INC-008:** `dotnet ef database update --add` scaffolds a migration that recreates every table, so applying it
  fails on RC2 (the two-step `migrations add` works). The talk no longer covers migrations.
- **INC-001:** `AsyncValidationAttribute` also requires the synchronous `IsValid` override. Mapping the `[Obsolete]`
  method raises CS0618, as expected.
- **INC-005:** `SerializeAsyncEnumerable` writes one streamed JSON array; `topLevelValues: true` gives JSON Lines.
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

`→` `Space` next · `←` `Backspace` back, straight through the deck across lessons · `PgDn` next lesson · `PgUp` start
of this lesson, then the previous one · `Home` index (`↑`/`↓` pick, `→`/`Enter` open) · `TAB` flip before/after on an
exhibit · `R` replay the slide · `1`-`9`, `0` open a lesson at its section slide · `F11` fullscreen · `ESC` quit.

The canvas is 128x36 cells, exactly 16:9 at the 8x16 font, so a 16:9 projector shows it edge to edge. That's 8 rows
fewer than the original 128x44, which buys about 22% larger text on a 1080p screen. Pages lay out on the rows named in
`ReportPage`; exhibits get a 72x22 screen.
Going back lands on the previous page's final state, with no animation.

## Milestones

1. ~~Engine + the FullJoin lesson end to end~~ (done)
2. ~~All ten lessons~~ (done)
3. ~~One app: fold the original code into `src/Before` and retire the standalone control room~~ (done;
   `Rendering/Workstation` is now the only copy of the control-room look)
4. Presenter extras: speaker-notes window on a second monitor, a timer against each lesson's slot in the talk.
