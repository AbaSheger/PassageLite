namespace PassageLite.Domain.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IAreaRepository Areas { get; }
    IAccessGrantRepository AccessGrants { get; }
    Task<int> SaveChangesAsync();
}
