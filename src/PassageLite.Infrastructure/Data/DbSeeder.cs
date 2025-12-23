using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PassageLite.Domain.Entities;

namespace PassageLite.Infrastructure.Data;

public static class DbSeeder
{
    public static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid NormalUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid LobbyAreaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ServerRoomAreaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        try
        {
            // Only run migrations for relational databases (not in-memory)
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrated successfully");
            }
            else
            {
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("In-memory database created");
            }

            if (await context.Users.AnyAsync())
            {
                logger.LogInformation("Database already seeded");
                return;
            }

            // Seed Users
            var adminUser = new User
            {
                Id = AdminUserId,
                Email = "admin@passage.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FullName = "System Administrator",
                Role = Roles.Admin,
                CreatedAt = DateTime.UtcNow
            };

            var normalUser = new User
            {
                Id = NormalUserId,
                Email = "user@passage.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                FullName = "Regular User",
                Role = Roles.User,
                CreatedAt = DateTime.UtcNow
            };

            await context.Users.AddRangeAsync(adminUser, normalUser);

            // Seed Areas
            var lobbyArea = new Area
            {
                Id = LobbyAreaId,
                Name = "Main Lobby",
                Description = "Main entrance lobby area",
                CreatedAt = DateTime.UtcNow
            };

            var serverRoomArea = new Area
            {
                Id = ServerRoomAreaId,
                Name = "Server Room",
                Description = "Secure server room - restricted access",
                CreatedAt = DateTime.UtcNow
            };

            await context.Areas.AddRangeAsync(lobbyArea, serverRoomArea);

            await context.SaveChangesAsync();
            logger.LogInformation("Database seeded with initial data");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
