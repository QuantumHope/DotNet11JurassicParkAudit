# ParkOps.Story

The talk as a Leading EDJE post-incident assessment. The report leads: it opens on the executive summary, and each
.NET 11 / C# 15 improvement is a lesson in it. The park's control room appears as exhibits inside the report,
running the real before/after code. Every fix is compiled in, so nothing is rebuilt on stage.

```
dotnet run --project src/ParkOps.Story                    # opens on the executive summary
dotnet run --project src/ParkOps.Story -- --lesson 2      # straight into lesson 2 (LINQ FullJoin)
dotnet run --project src/ParkOps.Story -- --capture out   # unattended run, a PNG of every step (rehearsal / smoke test)
```

Keys: on the summary, `↑` `↓` pick a lesson and `→` / `Enter` opens it. In a lesson, `→` `Space` `PgDn` or left click
next, `←` `Backspace` `PgUp` or right click back; finishing a lesson (or going back past its start) returns to the
summary. `Home` summary · `TAB` flip before/after code on an exhibit · `R` replay the page · `1`-`9`, `0` open a
lesson · `ESC` quit.

Built so far: the executive summary (all ten lessons listed) and **Lesson 2, LINQ FullJoin**: finding → exhibit
(before) → root cause → fix → exhibit (after) → resolution.
How the rest gets added is in [PLAN.md](PLAN.md).

The *before* code is compiled straight from `../ParkOps.Start` (linked in the csproj); the *after* code lives in
`src/ParkOps.Story/Chapters/IncNNN/` under `ParkOps.Story.Fixed`. Both are embedded as text too, so the code
on screen is the code that runs.
