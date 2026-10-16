namespace ParkOps.Data;

public static class ParkSeeder
{
    private static readonly string[] FenceNames =
    [
        "Paddock 1 East", "Paddock 2 North", "Paddock 3 West", "Paddock 4 South",
        "Paddock 5 Perimeter", "Paddock 7 Gate", "Paddock 9 Raptor Pen", "Paddock 11 Lagoon",
    ];

    private static readonly string[] Causes =
        ["Power fluctuation", "Fallen tree", "Animal contact", "Storm damage", "Unexplained", "Maintenance override"];

    public static void Seed(ParkDb db)
    {
        if (db.Fences.Any()) return;

        var random = new Random(1993);
        foreach (var name in FenceNames)
        {
            var fence = new Fence { Name = name };
            var breaches = name.Contains("Raptor") ? 412 : random.Next(60, 140);
            for (var i = 0; i < breaches; i++)
            {
                fence.Breaches.Add(new Breach
                {
                    OccurredAt = new DateTime(1993, 6, 1).AddMinutes(-random.Next(0, 525_600)),
                    Cause = Causes[random.Next(Causes.Length)],
                });
            }
            db.Fences.Add(fence);
        }

        for (var n = 1; n <= 12; n++)
        {
            db.Paddocks.Add(new Paddock { Code = $"PADDOCK-{n}", Name = $"Paddock {n}", IsOpen = n != 9 });
        }

        db.SaveChanges();
    }
}
