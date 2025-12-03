using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommunityWebsite.Web;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.DTOs.Requests;
using CommunityWebsite.Core.DTOs.Responses;
using CommunityWebsite.Core.Services;
using CommunityWebsite.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Tests.Controllers;

/// <summary>
/// Base class for API integration tests
/// Provides common setup/teardown and helper methods for test infrastructure
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    protected const string TestJwtSecret = "test-secret-key-for-integration-testing-minimum-32-chars";

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected string AuthToken { get; private set; } = string.Empty;
    protected int UserId { get; private set; } = 0;

    private SqliteConnection _connection = null!;

    public async Task InitializeAsync()
    {
        // Create and open a shared SQLite connection that persists for the test lifetime
        // This keeps the in-memory database alive
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Add test configuration for JWT secret - must happen before services are built
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SecretKey"] = TestJwtSecret
                    });
                });

                builder.ConfigureTestServices(services =>
                {
                    // Remove all existing DbContext-related registrations
                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<CommunityDbContext>));
                    if (dbContextDescriptor != null)
                        services.Remove(dbContextDescriptor);

                    // Use SQLite in-memory database with the shared connection
                    // SQLite enforces foreign keys and constraints like a real relational database
                    services.AddDbContext<CommunityDbContext>(options =>
                        options.UseSqlite(_connection));
                });
            });

        Client = Factory.CreateClient();

        // Create database schema
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        // Seed comprehensive test data
        await SeedTestDataAsync();

        // Setup: Create test user and get auth token
        await SetupTestUser();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (Factory != null)
            await Factory.DisposeAsync();
        _connection?.Close();
        _connection?.Dispose();
    }

    protected async Task SetupTestUser()
    {
        var registerRequest = new
        {
            username = "testuser",
            email = "test@example.com",
            password = "TestPassword123!",
            confirmPassword = "TestPassword123!"
        };

        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        var json = await registerResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Auth controller returns the data directly (not wrapped in { data: ... })
        AuthToken = root.GetProperty("token").GetString() ?? "";
        UserId = root.GetProperty("userId").GetInt32();

        // Add auth token to default headers for authenticated requests
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);
    }

    protected async Task SeedTestDataAsync()
    {
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // Seed comprehensive test data
            await SeedData.SeedDatabaseAsync(dbContext, passwordHasher);
        }
    }
}
