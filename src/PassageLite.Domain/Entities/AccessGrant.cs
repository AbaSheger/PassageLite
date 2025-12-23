namespace PassageLite.Domain.Entities;

public class AccessGrant
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AreaId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Area Area { get; set; } = null!;

    public bool IsCurrentlyValid()
    {
        var now = DateTime.UtcNow;
        return !IsRevoked && ValidFrom <= now && ValidTo >= now;
    }
}
