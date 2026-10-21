namespace ParkOps.Data;

public class Fence
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<Breach> Breaches { get; set; } = [];
    // ▶ INC-007b: the board wants voltage tracked; nobody wrote the migration (fix: dotnet ef migrations add)
}

public class Breach
{
    public int Id { get; set; }
    public int FenceId { get; set; }
    public Fence Fence { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public required string Cause { get; set; }
}

public class Paddock
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsOpen { get; set; }
}
