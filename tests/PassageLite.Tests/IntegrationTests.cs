using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PassageLite.Application.DTOs;
using PassageLite.Domain.Entities;
using PassageLite.Infrastructure.Data;
using Xunit;

namespace PassageLite.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly string _dbName = "TestDb_" + Guid.NewGuid();

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.RemoveAll(typeof(AppDbContext));

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });
        });
    }

    private async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@passage.local",
            password = "Admin123!"
        });
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.Token;
    }

    private async Task<string> GetUserTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "user@passage.local",
            password = "User123!"
        });
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result!.Token;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@passage.local",
            password = "Admin123!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            email = "admin@passage.local",
            password = "WrongPassword"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAreas_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/areas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAreas_WithAuth_ReturnsAreas()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetUserTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/areas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var areas = await response.Content.ReadFromJsonAsync<IEnumerable<AreaDto>>();
        Assert.NotNull(areas);
        Assert.NotEmpty(areas);
    }

    [Fact]
    public async Task CreateArea_AsUser_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetUserTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/areas", new
        {
            name = "New Area",
            description = "Test"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateArea_AsAdmin_Succeeds()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/areas", new
        {
            name = "New Test Area",
            description = "Created by admin"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GrantAccess_AsUser_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetUserTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/access/grant", new
        {
            userId = DbSeeder.NormalUserId,
            areaId = DbSeeder.LobbyAreaId,
            validFrom = DateTime.UtcNow,
            validTo = DateTime.UtcNow.AddDays(30)
        });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RevokeAccess_AsUser_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetUserTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/access/revoke", new
        {
            userId = DbSeeder.NormalUserId,
            areaId = DbSeeder.LobbyAreaId
        });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithAuth_ReturnsUserInfo()
    {
        // Arrange
        var client = _factory.CreateClient();
        var token = await GetUserTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal("user@passage.local", user.Email);
        Assert.Equal(Roles.User, user.Role);
    }
}
