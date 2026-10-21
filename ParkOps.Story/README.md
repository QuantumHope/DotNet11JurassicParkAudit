# ParkOps.Story

The talk as a Leading EDJE post-incident assessment, played as a slideshow: a title slide, then each .NET 11 / C# 15
improvement as a lesson (its own section slide, then the report pages), then the closing report to the board. The
park's control room appears as exhibits inside the report, running the real before/after code. Every fix is compiled
in, so nothing is rebuilt on stage.

```
dotnet run --project src/ParkOps.Story                    # opens on the title slide, in a 1024x576 window
dotnet run --project src/ParkOps.Story -- --fullscreen    # ...or fullscreen (F11 toggles either way)
dotnet run --project src/ParkOps.Story -- --lesson 2      # straight into lesson 2 (LINQ FullJoin)
dotnet run --project src/ParkOps.Story -- --capture out   # unattended run of every lesson, a PNG of every step
dotnet run --project src/ParkOps.Story -- --capture out --lesson 5   # ...or just one lesson
```

Keys: `→` `Space` or left click next, `←` `Backspace` or right click back. Both run straight through the deck, across
lessons: title → lessons 1-10 → closing → index. `PgDn` next lesson · `PgUp` start of this lesson, then the previous
one · `Home` index of lessons (`↑` `↓` pick, `Enter` open) · `TAB` flip before/after code on an exhibit · `R` replay
the slide · `1`-`9`, `0` open a lesson · `F11` fullscreen · `ESC` quit.

The canvas is 128x36 cells (16:9), so fullscreen on a 16:9 screen fills it with no bars. Page layouts use the rows
named in `ReportPage` (`KickerRow`, `HeadingRow`, `Top`, `Bottom`, `FooterRow`).

All ten lessons are built: section slide → finding → before → root cause → fix → after → resolution, with build or
console pages where the lesson's moment happens outside the park. Lesson 1 opens with a fence orientation page
(`FencesBeat`) and reads the whole script one flaw at a time (`WalkthroughBeat`) instead of a finding and root cause;
lesson 2 adds a Join vs FullJoin concept page (`JoinConceptBeat`). See [PLAN.md](PLAN.md) for how each is simulated
and what building them found on RC2.

The story keeps its own copy of the park database (`parkops.db`) next to the exe, migrated and seeded at startup.

The *before* code is the park's original code in [`src/Before`](src/Before) (projects referenced, control-room files
linked into the csproj); the *after* code lives in `src/ParkOps.Story/Chapters/IncNNN/` under `ParkOps.Story.Fixed`.
Both are embedded as text too, so the code on screen is the code that runs.
