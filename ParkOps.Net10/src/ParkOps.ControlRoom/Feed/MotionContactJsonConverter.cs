using System.Text.Json;
using System.Text.Json.Serialization;
using ParkOps.Core.Animals;
using ParkOps.Core.Sensors;

namespace ParkOps.ControlRoom.Feed;

// dnedry: written in an afternoon. Works on my machine.
public sealed class MotionContactJsonConverter : JsonConverter<MotionContact>
{
    public override MotionContact Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("TODO (dnedry)");

    public override void Write(Utf8JsonWriter writer, MotionContact value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value.Value)
        {
            case Dinosaur d:
                writer.WriteString("kind", "dinosaur");
                writer.WriteString("species", d.GetType().Name);
                writer.WriteString("tag", d.Tag);
                break;
            case Guest g:
                writer.WriteString("kind", "guest");
                writer.WriteString("name", g.Name);
                break;
            case TourVehicle v:
                writer.WriteString("kind", "vehicle");
                writer.WriteString("callSign", v.CallSign);
                break;
            // dnedry: staff contacts come out as {} until I get back
        }
        writer.WriteEndObject();
    }
}
