using System.Text.Json;
using ParkOps.Story.Fixed.Inc004;

namespace ParkOps.Story.Fixed.Inc005;

public static class FeedCommand
{
    public static async Task RunAsync(Stream output)
    {
        await JsonSerializer.SerializeAsyncEnumerable(output, SensorFeed.ReadAllAsync(), topLevelValues: true);
    }
}
