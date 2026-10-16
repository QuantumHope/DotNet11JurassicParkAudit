namespace ParkOps.Core.Animals;

// These come from three different vendors' libraries. They share nothing.
public sealed record Guest(string Name);
public sealed record TourVehicle(string CallSign);
public sealed record Employee(string Name, string Role);
