using ParkOps.Core.Animals;

namespace ParkOps.Core.Sensors;

// ▶ INC-004a: marker base type + one wrapper per case (fix: union)
public abstract record MotionContact
{
    public abstract object Value { get; }
}

public sealed record GuestContact(Guest Guest) : MotionContact { public override object Value => Guest; }
public sealed record VehicleContact(TourVehicle Vehicle) : MotionContact { public override object Value => Vehicle; }
public sealed record StaffContact(Employee Employee) : MotionContact { public override object Value => Employee; }
public sealed record DinosaurContact(Dinosaur Dinosaur) : MotionContact { public override object Value => Dinosaur; } // dnedry: added last sprint
