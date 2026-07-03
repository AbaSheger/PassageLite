using Microsoft.EntityFrameworkCore;
using PassageLite.Domain.Entities;
using PassageLite.Infrastructure.Data;
using Xunit;

namespace PassageLite.Tests;

/// <summary>
/// Verifies AppDbContext actually works against a real SQL Server instance
/// (not the in-memory provider used by IntegrationTests). This proves the
/// SQL Server EF Core provider genuinely functions: schema creation, writes,
/// reads, relationships, and a unique-index constraint.
///
/// Runs only when PASSAGELITE_SQLSERVER_TEST_CONNECTION is set to a real
/// connection string. In CI, this is provided by a SQL Server service
/// container (see .github/workflows/azure-deploy.yml, test-sqlserver job).
/// Locally it's skipped by default so normal `dotnet test` runs are
/// unaffected; to run it locally, start SQL Server via
/// `docker compose --profile sqlserver up sqlserver` and set the env var.
/// </summary>
public class SqlServerProviderTests : IAsyncLifetime
{
    private static readonly string? ConnectionString =
        Environment.GetEnvironmentVariable("PASSAGELITE_SQLSERVER_TEST_CONNECTION");

    private AppDbContext? _context;

    public async Task InitializeAsync()
    {
        if (ConnectionString is null) return;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        _context = new AppDbContext(options);

        // Fresh schema for each test run: drop then recreate against the real SQL Server instance.
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task CanCreateSchemaAndWriteUser()
    {
        Skip.If(ConnectionString is null, "PASSAGELITE_SQLSERVER_TEST_CONNECTION not set; skipping SQL Server test.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "sqlserver-test@passage.local",
            PasswordHash = "hash",
            FullName = "SQL Server Test User",
            Role = Roles.User
        };

        _context!.Users.Add(user);
        await _context.SaveChangesAsync();

        var fetched = await _context.Users.FindAsync(user.Id);

        Assert.NotNull(fetched);
        Assert.Equal("sqlserver-test@passage.local", fetched!.Email);
    }

    [SkippableFact]
    public async Task EnforcesUniqueEmailConstraint()
    {
        Skip.If(ConnectionString is null, "PASSAGELITE_SQLSERVER_TEST_CONNECTION not set; skipping SQL Server test.");

        var first = new User
        {
            Id = Guid.NewGuid(),
            Email = "duplicate@passage.local",
            PasswordHash = "hash",
            FullName = "First",
            Role = Roles.User
        };
        _context!.Users.Add(first);
        await _context.SaveChangesAsync();

        var duplicate = new User
        {
            Id = Guid.NewGuid(),
            Email = "duplicate@passage.local",
            PasswordHash = "hash",
            FullName = "Duplicate",
            Role = Roles.User
        };
        _context.Users.Add(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task CanCreateAreaAndAccessGrantWithRelationship()
    {
        Skip.If(ConnectionString is null, "PASSAGELITE_SQLSERVER_TEST_CONNECTION not set; skipping SQL Server test.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "grant-test@passage.local",
            PasswordHash = "hash",
            FullName = "Grant Test User",
            Role = Roles.User
        };

        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = "Test Area",
            Description = "Created by SQL Server provider test"
        };

        var grant = new AccessGrant
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AreaId = area.Id,
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddDays(1)
        };

        _context!.Users.Add(user);
        _context.Areas.Add(area);
        _context.AccessGrants.Add(grant);
        await _context.SaveChangesAsync();

        var fetchedGrant = await _context.AccessGrants
            .Include(g => g.User)
            .Include(g => g.Area)
            .FirstOrDefaultAsync(g => g.Id == grant.Id);

        Assert.NotNull(fetchedGrant);
        Assert.Equal(user.Email, fetchedGrant!.User.Email);
        Assert.Equal(area.Name, fetchedGrant.Area.Name);
    }
}