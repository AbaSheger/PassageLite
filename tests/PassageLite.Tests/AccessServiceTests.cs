using Microsoft.EntityFrameworkCore;
using PassageLite.Application.DTOs;
using PassageLite.Application.Events;
using PassageLite.Application.Interfaces;
using PassageLite.Application.Services;
using PassageLite.Domain.Entities;
using PassageLite.Infrastructure;
using PassageLite.Infrastructure.Data;
using Xunit;

namespace PassageLite.Tests;

public class AccessServiceTests
{
    private sealed class TestAccessEventPublisher : IAccessEventPublisher
    {
        public List<AccessGrantedEvent> AccessGrantedEvents { get; } = [];

        public Task PublishAccessGrantedAsync(AccessGrantedEvent accessGrantedEvent, CancellationToken cancellationToken = default)
        {
            AccessGrantedEvents.Add(accessGrantedEvent);
            return Task.CompletedTask;
        }
    }

    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private async Task<(AppDbContext context, AccessService service, TestAccessEventPublisher publisher, Guid userId, Guid areaId)> SetupTestDataAsync()
    {
        var context = CreateInMemoryContext();
        var unitOfWork = new UnitOfWork(context);
        var publisher = new TestAccessEventPublisher();
        var service = new AccessService(unitOfWork, publisher);

        // Create test user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = Roles.User
        };
        await context.Users.AddAsync(user);

        // Create test area
        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = "Test Area",
            Description = "Test Description"
        };
        await context.Areas.AddAsync(area);

        await context.SaveChangesAsync();

        return (context, service, publisher, user.Id, area.Id);
    }

    [Fact]
    public async Task CheckAccess_ReturnsTrue_WhenGrantIsValid()
    {
        // Arrange
        var (context, service, _, userId, areaId) = await SetupTestDataAsync();

        var grant = new AccessGrant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AreaId = areaId,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };
        await context.AccessGrants.AddAsync(grant);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CheckAccessAsync(userId, areaId);

        // Assert
        Assert.True(result.HasAccess);
        Assert.Contains("Access granted", result.Reason);
    }

    [Fact]
    public async Task CheckAccess_ReturnsFalse_WhenGrantIsExpired()
    {
        // Arrange
        var (context, service, _, userId, areaId) = await SetupTestDataAsync();

        var grant = new AccessGrant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AreaId = areaId,
            ValidFrom = DateTime.UtcNow.AddDays(-10),
            ValidTo = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            IsRevoked = false
        };
        await context.AccessGrants.AddAsync(grant);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CheckAccessAsync(userId, areaId);

        // Assert
        Assert.False(result.HasAccess);
        Assert.Contains("expired", result.Reason);
    }

    [Fact]
    public async Task CheckAccess_ReturnsFalse_WhenGrantIsRevoked()
    {
        // Arrange
        var (context, service, _, userId, areaId) = await SetupTestDataAsync();

        var grant = new AccessGrant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AreaId = areaId,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1),
            IsRevoked = true
        };
        await context.AccessGrants.AddAsync(grant);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CheckAccessAsync(userId, areaId);

        // Assert
        Assert.False(result.HasAccess);
        Assert.Contains("No access grant found", result.Reason);
    }

    [Fact]
    public async Task CheckAccess_ReturnsFalse_WhenNoGrantExists()
    {
        // Arrange
        var (context, service, _, userId, areaId) = await SetupTestDataAsync();

        // Act - No grant added
        var result = await service.CheckAccessAsync(userId, areaId);

        // Assert
        Assert.False(result.HasAccess);
        Assert.Contains("No access grant found", result.Reason);
    }

    [Fact]
    public async Task CheckAccess_ReturnsFalse_WhenGrantNotYetValid()
    {
        // Arrange
        var (context, service, _, userId, areaId) = await SetupTestDataAsync();

        var grant = new AccessGrant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AreaId = areaId,
            ValidFrom = DateTime.UtcNow.AddDays(1), // Starts tomorrow
            ValidTo = DateTime.UtcNow.AddDays(10),
            IsRevoked = false
        };
        await context.AccessGrants.AddAsync(grant);
        await context.SaveChangesAsync();

        // Act
        var result = await service.CheckAccessAsync(userId, areaId);

        // Assert
        Assert.False(result.HasAccess);
        Assert.Contains("not yet valid", result.Reason);
    }

    [Fact]
    public async Task GrantAccess_CreatesNewGrant()
    {
        // Arrange
        var (context, service, publisher, userId, areaId) = await SetupTestDataAsync();

        var request = new GrantAccessRequest(
            userId,
            areaId,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)
        );

        // Act
        var result = await service.GrantAccessAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(areaId, result.AreaId);
        Assert.True(result.IsCurrentlyValid);
        var accessGrantedEvent = Assert.Single(publisher.AccessGrantedEvents);
        Assert.Equal(result.Id, accessGrantedEvent.GrantId);
        Assert.Equal(userId, accessGrantedEvent.UserId);
        Assert.Equal(areaId, accessGrantedEvent.AreaId);
        Assert.Equal("Test Area", accessGrantedEvent.AreaName);
    }

    [Fact]
    public async Task GrantAccess_PublishesAccessGrantedEvent_WhenUpdatingExistingGrant()
    {
        // Arrange
        var (context, service, publisher, userId, areaId) = await SetupTestDataAsync();

        var grant = new AccessGrant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AreaId = areaId,
            ValidFrom = DateTime.UtcNow.AddDays(-10),
            ValidTo = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };
        await context.AccessGrants.AddAsync(grant);
        await context.SaveChangesAsync();

        var request = new GrantAccessRequest(
            userId,
            areaId,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)
        );

        // Act
        var result = await service.GrantAccessAsync(request);

        // Assert
        Assert.Equal(grant.Id, result.Id);
        var accessGrantedEvent = Assert.Single(publisher.AccessGrantedEvents);
        Assert.Equal(grant.Id, accessGrantedEvent.GrantId);
        Assert.Equal("Test Area", accessGrantedEvent.AreaName);
    }

    [Fact]
    public async Task RevokeAccess_RevokesExistingGrant()
    {
        // Arrange
        var (context, service, _, userId, areaId) = await SetupTestDataAsync();

        var grant = new AccessGrant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AreaId = areaId,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(1),
            IsRevoked = false
        };
        await context.AccessGrants.AddAsync(grant);
        await context.SaveChangesAsync();

        var request = new RevokeAccessRequest(userId, areaId);

        // Act
        var success = await service.RevokeAccessAsync(request);

        // Assert
        Assert.True(success);
        var revokedGrant = await context.AccessGrants.FindAsync(grant.Id);
        Assert.True(revokedGrant!.IsRevoked);
    }
}
