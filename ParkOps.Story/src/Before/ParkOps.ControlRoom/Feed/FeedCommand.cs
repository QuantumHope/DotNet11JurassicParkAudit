using System.Text.Json;
using ParkOps.Core.Sensors;

namespace ParkOps.ControlRoom.Feed;

public static class FeedCommand
{
    public static async Task RunAsync(Stream output)
    {
        // ▶ INC-005a: hand-written converter that nobody maintains (fix: unions serialize natively)
        var options = new JsonSerializerOptions { Converters = { new MotionContactJsonConverter() } };

        // ▶ INC-005b: buffer the whole feed, then ship one giant array (fix: stream JSON Lines)
        var batch = new List<SensorPing>();
        await foreach (var ping in SensorFeed.ReadAllAsync()) batch.Add(ping);
        await JsonSerializer.SerializeAsync(output, batch, options);
    }
}
