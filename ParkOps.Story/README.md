# ParkOps.Story

The talk as a Leading EDJE post-incident assessment: report pages, control-room simulations, and the real
before/after code, all in one SadConsole window. Every fix is compiled in, so nothing is rebuilt on stage.

```
dotnet run --project src/ParkOps.Story                    # from the cover
dotnet run --project src/ParkOps.Story -- --chapter 2     # straight to INC-002
dotnet run --project src/ParkOps.Story -- --capture out   # unattended run, a PNG of every step (rehearsal / smoke test)
```

Keys: `→` `Space` `PgDn` `Enter` or left click next · `←` `Backspace` `PgUp` or right click back · `Home` `End` ·
`TAB` flip before/after code on a control-room screen · `R` replay the beat · `1`-`9`, `0` jump to an incident · `ESC` quit.

Built so far: cover, findings index, **INC-002** (finding → before → code → fix → after → resolved), summary.
How the rest gets added is in [PLAN.md](PLAN.md).

The *before* code is compiled straight from `../ParkOps.Start` (linked in the csproj); the *after* code lives in
`src/ParkOps.Story/Chapters/IncNNN/` under `ParkOps.Story.Fixed`. Both are embedded as text too, so the code
on screen is the code that runs.
