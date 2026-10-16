namespace ParkOps.Core.Sensors;

/// <summary>A contact the sensors can't classify. Unused until the INC-003 reveal.</summary>
public sealed record Unidentified(string SensorId);
