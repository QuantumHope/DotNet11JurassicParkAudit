---
name: run-parkops-story
description: Build, launch and drive ParkOps.Story (the Leading EDJE report app, a SadConsole/MonoGame window) on Windows - press real keys in it and screenshot the window, or run its unattended --capture rehearsal. Use to run the story app, check a lesson or page visually, or confirm a change works in the real window. ParkOps.Story is the repo's only app.
---

# Run ParkOps.Story

ParkOps.Story is a desktop window (SadConsole on MonoGame DesktopGL), not a console app: stdout shows
nothing useful. There are two ways to check it, and you should **look at the PNGs** either way. A blank or
black frame means the app failed to start.

Paths are relative to the repo root. The SDK is pinned by `ParkOps.Story/global.json` (.NET 11 RC2).

## 1. Build

```bash
dotnet build ParkOps.Story
```

Exe: `ParkOps.Story/src/ParkOps.Story/bin/Debug/net11.0/ParkOps.Story.exe`.

## 2a. Rehearsal capture (fastest, no key driving)

The app has a built-in unattended mode. It starts on the executive summary, opens every lesson in turn, and
saves a PNG of every step at 1.1s, 3s and 7s into its animation, on a 4x clock. Then it exits.

```bash
dotnet run --project ParkOps.Story/src/ParkOps.Story -- --capture <out-dir>              # all ten lessons, ~5 min
dotnet run --project ParkOps.Story/src/ParkOps.Story -- --capture <out-dir> --lesson 7   # one lesson, ~30 s
```

Files are named `NN-<INC-id>-<Page>-<step>@<t>s.png`. Read the `@7.0s` ones to see each finished step. The
summary is re-shot every time a lesson returns to it, so `01-Summary-*` shows the final state.
Use this after any visual change. It catches crashes on every page too: the run stops early on one, and
`dotnet run` prints the exception.

- Some exhibits do live work on the wall clock, not the 4x story clock: EF queries, the sensor feed, and the
  narrator's model calls. A refused Ollama connection takes about 2.7s on Windows. The rehearsal holds each
  step's last shot until the exhibit settles, for up to 20s.
- Run `dotnet build ParkOps.Story` first if you pass `--no-build`.

## 2b. Drive the real window (keys, TAB, navigation)

Use `scripts/drive.ps1`. It launches the exe, presses real keys and screenshots the window's client area
(1024x704 at default size). Run it from PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude/skills/run-parkops-story/scripts/drive.ps1 `
  -Out <out-dir> -Steps 'wait:1 shot:summary enter wait:1 rightx2 wait:5 shot:exhibit tab wait:4 shot:tab esc'
```

- The example opens lesson 2 from the summary; `rightx2` steps past the Finding page's second section to the
  exhibit. Each `right` is one step, not one page: pages with callouts take two.
- Steps: key names (`right left up down enter tab esc home space back pgup pgdn r 0-9 f1-f5`), `rightx5`
  to repeat, `wait:N` seconds, `shot:NAME`.
- `-AppArgs '--lesson','2'` starts straight inside a lesson.
- End with `esc`. The script reports `exited: code 0` if the app quit, or stops it if it's still running.
- **Non-obvious:** keys must be sent with real scan codes (`MapVirtualKey`). SDL ignores scan code 0 for
  Tab and Esc, although the arrow keys still work. The script handles this; don't swap in `SendKeys`.
- The window is focused before every key and shot, so don't use the desktop while it runs.
- Exhibit pages animate for about 2.6s (longer if they wait on live work); wait 5s before a shot. Fix pages
  type their diff for about 5s; wait 6s.
- The story keeps its own `parkops.db` next to the exe (git-ignored), migrated and seeded at startup.

### App keys (for writing Steps)

Summary: `up`/`down` pick a lesson (lesson 1 is selected at start), `enter` or `right` opens it. In a lesson: `right` next step/page, `left` back (back past the first page returns to the summary),
`tab` flips before/after code on an exhibit, `home` summary, `r` replay the page, `1`-`9`, `0` open a lesson,
`esc` quit. Finishing a lesson's last page returns to the summary with the next lesson selected.

## Where the code is

The *before* side of every exhibit is the park's original code in `ParkOps.Story/src/Before` (see its README); the
*after* side is in `ParkOps.Story/src/ParkOps.Story/Chapters/IncNNN/`. There's no standalone control-room app any
more: the story draws the control room itself (`Rendering/Workstation.cs`).
