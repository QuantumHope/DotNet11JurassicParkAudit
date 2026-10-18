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
- **Can't break on stage.** No live builds, network, or Ollama. Anything that only happens at build time
  replays a capture.

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
    Beats/                         # reusable page types: Summary (home), Finding, Code, Fix, Resolution
    Chapters/IncNNN/               # one folder per lesson: its "after" code and its exhibit page
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

## A lesson = six pages

| Page | Look | What happens | Steps |
|---|---|---|---|
| **Finding** | Report | Feature as the headline; platform, severity, incident, reporter; observation and impact | reveal paragraphs |
| **Before** | Report + exhibit | The control-room screen runs the real *before* code; caption and code-path panel alongside | the report's reading; lines it refers to are marked |
| **Root cause** | Report + code | The *before* source, syntax highlighted, with the bad line flagged | callout on the root cause |
| **Fix** | Report + code | Animated diff: removed lines go red and collapse, new lines type in | callout explaining the feature |
| **After** | Report + exhibit | Same exhibit with the *after* code active | the report's reading of what's now visible |
| **Resolution** | Report | Before/after metrics, change, recommendation, RESOLVED stamp | stamp lands |

### Switching the active code path

Each exhibit gets its code paths as delegates, e.g.
`Before = ParkOps.ControlRoom.Panels.CensusPanel.Render`, `After = ParkOps.Story.Fixed.CensusPanel.Render`.
The beat decides which one runs. **TAB** on an exhibit flips between them live, which helps with Q&A.

### Code on screen

`SourceFile` reads an embedded `.cs` file, drops the `using`/`namespace` preamble, and unindents.
`CSharpTokenizer` highlights it (keywords, types, methods, strings with interpolation holes, comments,
numbers). `LineDiff` (an LCS over lines) drives the Fix animation. Long lines soft-wrap with a
continuation glyph.

## How each incident is simulated

| Incident | Feature | Before vs. after | Build-time moment |
|---|---|---|---|
| INC-001 | `#:include`, `RunAndCaptureTextAsync` | Terminal beat; the real `git log` runs in-process | none |
| **INC-002** | **LINQ `FullJoin`** | **Both run live** | none |
| INC-003 | C# 15 unions | Both run live (union in `Fixed`) | Replay: `Unidentified` exhaustiveness warnings |
| INC-004 | closed hierarchies | Feeding plan runs live both ways | Replay: plugin build failure |
| INC-005 | runtime-async | Two tiny assemblies, one built with `runtime-async=on`; real stack traces | none (it's a csproj flag) |
| INC-006 | extension indexers, labeled `break`, `with(...)` | Both run live | none |
| INC-007 | EF `MaxByAsync` | Both run live on the same SQLite DB; rows-pulled counter | Replay: migration output |
| INC-008 | async validation, `[Obsolete]` to OpenAPI | In-process TestServer or a replayed HTTP exchange | none |
| INC-009 | `SerializeAsyncEnumerable` | Both stream live into a telemetry pane | none |
| INC-010 | `OrderedFailoverChatClient` | Replayed transcript (no Ollama on stage) | none |

Replays live as text files under `Chapters/IncNNN/Captures/` and are captured from real builds, never made up.

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

1. **Engine + lesson 2 (LINQ FullJoin) end to end**: executive summary hub, six lesson pages, exhibits.
2. Shared drawing: move `Bevel`/`Screen`/`Chip`/`Button` out of `ControlRoomScreen` into a reusable
   `Workstation` in ParkOps.Start so the story stops duplicating it.
3. Runtime-only incidents: INC-006, INC-009, INC-007.
4. Compile-time incidents with replays: INC-003, INC-004, INC-005.
5. Outside-the-control-room incidents: INC-001 (terminal), INC-008 (API), INC-010 (narrator).
6. Presenter extras: speaker-notes window on a second monitor, a timer against the `Slot` times
   in `DemoScript`.
