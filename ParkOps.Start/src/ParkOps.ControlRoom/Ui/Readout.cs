namespace ParkOps.ControlRoom.Ui;

public enum PanelStatus { Nominal, Attention, Alert }

public enum Tone { Normal, Dim, Good, Warn, Bad, Strong }

public sealed record Line(string Text, Tone Tone = Tone.Normal);

/// <summary>What a panel reports. The screen decides how to draw it.</summary>
public sealed record Readout(string Title, PanelStatus Status, IReadOnlyList<Line> Lines);
