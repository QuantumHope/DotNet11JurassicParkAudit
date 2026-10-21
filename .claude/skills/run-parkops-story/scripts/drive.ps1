<#
.SYNOPSIS
  Launches ParkOps.Story (or any SadConsole/MonoGame window), presses real keys in it, and screenshots the window.

.EXAMPLE
  drive.ps1 -Out shots -Steps 'wait:1 shot:summary enter wait:1 shot:lesson tab wait:3 shot:after esc'

  Steps (space-separated, run in order):
    <key>        right left up down enter tab esc home end space back pgup pgdn r h 0-9 f1-f5
    <key>xN      press N times, e.g. rightx5
    wait:N       sleep N seconds (decimals ok)
    shot:NAME    save the window's client area to <Out>\NAME.png
#>
param(
    [string]$Exe,
    [string]$Out = 'shots',
    [string[]]$AppArgs = @(),
    [Parameter(Mandatory)][string]$Steps
)

$ErrorActionPreference = 'Stop'
# Under `powershell -File`, -AppArgs '--lesson','2' arrives as one string "--lesson,2"; split it back into arguments.
$AppArgs = @($AppArgs | ForEach-Object { $_ -split '[,\s]+' } | Where-Object { $_ })
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class W {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint code, uint mapType);
}
'@

# Default exe: resolved here because Windows PowerShell 5.1 leaves $PSScriptRoot empty in param() defaults.
if (-not $Exe) { $Exe = Join-Path $PSScriptRoot '..\..\..\..\ParkOps.Story\src\ParkOps.Story\bin\Debug\net11.0\ParkOps.Story.exe' }
$Exe = (Resolve-Path $Exe).Path
New-Item -ItemType Directory -Force $Out | Out-Null
$startArgs = @{ FilePath = $Exe; WorkingDirectory = (Split-Path $Exe); PassThru = $true }
if ($AppArgs.Count -gt 0) { $startArgs.ArgumentList = $AppArgs }
$p = Start-Process @startArgs
for ($i = 0; $i -lt 80 -and $p.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 250; $p.Refresh() }
if ($p.MainWindowHandle -eq 0) { Stop-Process $p -ErrorAction SilentlyContinue; throw 'No window appeared (crashed on startup?)' }
$h = $p.MainWindowHandle
Start-Sleep -Seconds 2   # first frames

function Focus { [W]::ShowWindow($h, 9) | Out-Null; [W]::SetForegroundWindow($h) | Out-Null; Start-Sleep -Milliseconds 250 }

# SDL (under MonoGame) maps keys by scan code: keybd_event with scan 0 works for arrows but NOT for Tab/Esc/etc.
function Press([byte]$vk) {
    Focus
    $scan = [byte][W]::MapVirtualKey($vk, 0)
    $ext = if ($vk -in 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28) { 1 } else { 0 }   # PgUp/PgDn/End/Home/arrows
    [W]::keybd_event($vk, $scan, $ext, [UIntPtr]::Zero); Start-Sleep -Milliseconds 90      # held across a frame
    [W]::keybd_event($vk, $scan, $ext -bor 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 150
}

function Shot([string]$name) {
    Focus
    $r = New-Object W+RECT; [W]::GetClientRect($h, [ref]$r) | Out-Null
    $pt = New-Object W+POINT; [W]::ClientToScreen($h, [ref]$pt) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($pt.X, $pt.Y, 0, 0, $bmp.Size)
    $path = Join-Path $Out "$name.png"
    $bmp.Save($path); $g.Dispose(); $bmp.Dispose()
    "shot $path"
}

$keys = @{
    right = 0x27; left = 0x25; up = 0x26; down = 0x28; enter = 0x0D; tab = 0x09; esc = 0x1B; home = 0x24; end = 0x23
    space = 0x20; back = 0x08; pgup = 0x21; pgdn = 0x22; r = 0x52; h = 0x48
    f1 = 0x70; f2 = 0x71; f3 = 0x72; f4 = 0x73; f5 = 0x74
}
0..9 | ForEach-Object { $keys["$_"] = 0x30 + $_ }

foreach ($step in $Steps -split '\s+' | Where-Object { $_ }) {
    if ($p.HasExited) { "app exited (code $($p.ExitCode)) before step '$step'"; break }
    if ($step -match '^wait:([\d.]+)$') { Start-Sleep -Milliseconds ([double]$Matches[1] * 1000) }
    elseif ($step -match '^shot:(.+)$') { Shot $Matches[1] }
    elseif ($step -match '^(\w+?)(?:x(\d+))?$' -and $keys.ContainsKey($Matches[1].ToLower())) {
        $count = if ($Matches[2]) { [int]$Matches[2] } else { 1 }
        1..$count | ForEach-Object { Press $keys[$Matches[1].ToLower()] }
    }
    else { throw "Unknown step '$step'" }
}

Start-Sleep -Seconds 1.5
$p.Refresh()
if ($p.HasExited) { "exited: code $($p.ExitCode)" }
else { Stop-Process $p; 'still running: stopped it (end Steps with esc to test quitting)' }
