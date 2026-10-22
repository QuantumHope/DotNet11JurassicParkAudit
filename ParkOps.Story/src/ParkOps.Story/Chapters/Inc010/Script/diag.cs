#!/usr/bin/env dotnet
#:package Spectre.Console
#:include diag-helpers.cs

using System.Diagnostics;
using Spectre.Console;

AnsiConsole.Write(new Rule("[yellow]PARK OPS // SYSTEM DIAGNOSTIC[/]"));

var log = await Process.RunAndCaptureTextAsync("git", ["log", "-5", "--format=%h  %an  %s"]);
AnsiConsole.WriteLine(log.StandardOutput);

Diagnostics.PrintRuntimeInfo();
