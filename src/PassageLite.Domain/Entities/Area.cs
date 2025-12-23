namespace PassageLite.Domain.Entities;

public class Area
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AccessGrant> AccessGrants { get; set; } = new List<AccessGrant>();
}
