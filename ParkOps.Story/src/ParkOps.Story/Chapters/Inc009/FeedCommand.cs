using System.Text.Json;
using ParkOps.Story.Fixed.Inc003;

namespace ParkOps.Story.Fixed.Inc009;

public static class FeedCommand
{
    public static async Task RunAsync(Stream output)
    {
        await JsonSerializer.SerializeAsyncEnumerable(output, SensorFeed.ReadAllAsync(), topLevelValues: true);
    }
}
