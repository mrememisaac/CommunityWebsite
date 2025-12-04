using Microsoft.EntityFrameworkCore;
using Serilog;
using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Repositories;
using CommunityWebsite.Core.Repositories.Interfaces;
using CommunityWebsite.Core.Services;
using CommunityWebsite.Core.Services.Interfaces;
using CommunityWebsite.Core.Validators;
using CommunityWebsite.Core.Validators.Interfaces;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Community Website application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add MVC with Razor Views
    builder.Services.AddControllersWithViews();

    // Add DbContext with SQLite
    builder.Services.AddDbContext<CommunityDbContext>(options =>
        options.UseSqlite(
            builder.Configuration.GetConnectionString("DefaultConnection") ??
            "Data Source=CommunityWebsite.db"));

    // Register repositories - Dependency Inversion Principle
    builder.Services.AddScoped<IPostRepository, PostRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ICommentRepository, CommentRepository>();
    builder.Services.AddScoped<IRoleRepository, RoleRepository>();
    builder.Services.AddScoped<IEventRepository, EventRepository>();
    builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

    // Register validators - Separation of Concerns
    builder.Services.AddScoped<IPostValidator, PostValidator>();
    builder.Services.AddScoped<IUserValidator, UserValidator>();
    builder.Services.AddScoped<ICommentValidator, CommentValidator>();

    // Register services - Dependency Inversion Principle
    builder.Services.AddScoped<IPostService, PostService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<IEventService, EventService>();

    // Register authentication services
    var jwtSecret = builder.Configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey must be configured in appsettings.json or environment variables");

    if (jwtSecret.Length < 32)
        throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");

    builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
    builder.Services.AddScoped<ITokenService>(_ => new TokenService(jwtSecret));
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

    // Configure JWT Authentication
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "CommunityWebsite",
                ValidAudience = "CommunityWebsiteUsers",
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtSecret))
            };
        });

    // Register input sanitization
    builder.Services.AddScoped<IInputSanitizer, InputSanitizer>();

    // Register role seeding service
    builder.Services.AddScoped<IRoleSeedService, RoleSeedService>();

    // Add API versioning and documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Community Website API",
            Version = "v1",
            Description = "RESTful API for Community Website - ASP.NET Core 8.0",
            TermsOfService = new Uri("https://example.com/terms"),
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Support",
                Email = "support@example.com"
            }
        });

        // Add JWT authentication to Swagger
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using Bearer scheme",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        });

        // Include XML documentation
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            c.IncludeXmlComments(xmlPath);
    });

    // Add CORS with environment-specific policy
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Development", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://localhost:7001")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });

        options.AddPolicy("Production", policy =>
        {
            // In production, configure specific allowed origins from configuration
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<CommunityDbContext>("database");

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Community Website API v1");
            c.RoutePrefix = string.Empty; // Serve at root
        });
    }

    // Global exception handler for production
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(new { error = "An unexpected error occurred" }));
            });
        });
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSerilogRequestLogging();

    // Apply environment-specific CORS policy
    app.UseCors(app.Environment.IsDevelopment() ? "Development" : "Production");

    app.UseAuthentication();
    app.UseAuthorization();

    // Health check endpoint
    app.MapHealthChecks("/health");

    app.MapControllers();

    // Add default MVC route for Razor views
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // Initialize database
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
        var providerName = dbContext.Database.ProviderName ?? "";

        // Only migrate if using SQL Server (migrations are SQL Server-specific)
        // For in-memory and SQLite databases, use EnsureCreated instead
        var isSqlServer = providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);

        if (dbContext.Database.IsRelational() && isSqlServer)
        {
            dbContext.Database.Migrate();
            Log.Information("Database migrated successfully");
        }
        else
        {
            // For in-memory or SQLite database, ensure it's created from the model
            dbContext.Database.EnsureCreated();
            Log.Information("Database schema ensured from model");
        }

        // Seed default roles and admin user
        var roleSeedService = scope.ServiceProvider.GetRequiredService<IRoleSeedService>();
        await roleSeedService.SeedDefaultRolesAsync();
        await roleSeedService.SeedAdminUserAsync();
        Log.Information("Roles and admin user seeded successfully");
    }

    Log.Information("Application started successfully");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Make Program public for integration testing
public partial class Program { }
