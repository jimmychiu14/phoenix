namespace Phoenix.Domain.Entities;

public class Asset
{
    public int Id { get; set; }
    public required string Symbol { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    
    public decimal Quantity { get; set; } = 0;

    public int UserId { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
