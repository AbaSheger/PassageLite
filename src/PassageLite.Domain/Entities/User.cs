namespace PassageLite.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AccessGrant> AccessGrants { get; set; } = new List<AccessGrant>();
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
