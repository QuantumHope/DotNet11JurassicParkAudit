# ParkOps.Story: implementation plan

A presenter-driven SadConsole app that tells the talk as a **Leading EDJE post-incident assessment** of
InGen's ParkOps. Every fix is already compiled in: moving through the story switches which code path is
active, so there is nothing to rebuild on stage.

## Goals

- **Linear and animated.** Arrow keys or a clicker move forward and back one beat at a time.
- **Code-first.** The source on screen is the source that runs. Snippets are read from the real files
  (embedded at build time), never retyped.
- **Looks like the product.** Before and after screens use the control room's own `Palette`, bevels,
  chips and typewriter reveal. The report pages use Leading EDJE branding.
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
    Beats/                         # reusable beat types: Cover, FindingsIndex, Finding, Code, Fix, Resolution
    Chapters/IncNNN/               # one folder per incident: its "after" code and its simulation beat
```

ParkOps.Start stays untouched. The story **links** the files it needs from it: `Palette`, `Cp437`,
`Readout`, and each incident's *before* source (for example `Panels/CensusPanel.cs`). The *after* version
of each file lives in `Chapters/IncNNN/` under a `ParkOps.Story.Fixed` namespace, so both compile into
one assembly side by side.

## A chapter = six beats

| Beat | Look | What happens | Steps |
|---|---|---|---|
| **Finding** | LE report page | Finding number, reporter, severity, observation, impact | reveal paragraphs |
| **Before** | Control room | The real *before* code runs and its readout animates in | then a callout on the lie |
| **Code** | LE code view | The *before* source, syntax highlighted, with the bad line flagged | callout on the root cause |
| **Fix** | LE code view | Animated diff: removed lines go red and collapse, new lines type in | callout naming the .NET 11 feature |
| **After** | Control room | Same screen with the *after* code active | callout on what's now visible |
| **Resolution** | LE report page | Before/after metrics, recommendation, RESOLVED stamp | stamp lands |

The deck opens with a **cover** and a **findings index** (all ten incidents, with status), and closes with
a summary page.

### Switching the active code path

Each simulation beat gets its code paths as delegates, e.g.
`Before = ParkOps.ControlRoom.Panels.CensusPanel.Render`, `After = ParkOps.Story.Fixed.CensusPanel.Render`.
The beat decides which one runs. **TAB** on a simulation beat flips between them live, which helps with Q&A.

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

`→` `Space` `PgDn` next · `←` `Backspace` `PgUp` back · `Home`/`End` · `TAB` flip before/after on a
simulation · `R` replay the beat · `1`-`9`, `0` jump to chapter · `ESC` quit.
Going back lands on the previous beat's final state, with no animation.

## Milestones

1. **Engine + INC-002 end to end** (this change): cover, findings index, six INC-002 beats, closing page.
2. Shared drawing: move `Bevel`/`Screen`/`Chip`/`Button` out of `ControlRoomScreen` into a reusable
   `Workstation` in ParkOps.Start so the story stops duplicating it.
3. Runtime-only incidents: INC-006, INC-009, INC-007.
4. Compile-time incidents with replays: INC-003, INC-004, INC-005.
5. Outside-the-control-room incidents: INC-001 (terminal), INC-008 (API), INC-010 (narrator).
6. Presenter extras: speaker-notes window on a second monitor, a timer against the `Slot` times
   in `DemoScript`.
