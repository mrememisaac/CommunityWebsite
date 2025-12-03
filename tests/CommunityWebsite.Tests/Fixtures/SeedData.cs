using CommunityWebsite.Core.Data;
using CommunityWebsite.Core.Models;
using CommunityWebsite.Core.Services;
using CommunityWebsite.Core.Services.Interfaces;

namespace CommunityWebsite.Tests.Fixtures;

/// <summary>
/// Comprehensive seed data for integration tests
/// Provides realistic sample data for users, roles, posts, and comments
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Seeds the database with sample data
    /// </summary>
    public static async Task SeedDatabaseAsync(CommunityDbContext context, IPasswordHasher passwordHasher)
    {
        // Seed roles
        await SeedRolesAsync(context);

        // Seed users
        await SeedUsersAsync(context, passwordHasher);

        // Seed posts (with owners from seeded users)
        await SeedPostsAsync(context);

        // Seed comments (on posts from seeded users)
        await SeedCommentsAsync(context);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds all application roles
    /// </summary>
    private static async Task SeedRolesAsync(CommunityDbContext context)
    {
        if (context.Roles.Any())
            return; // Already seeded

        var roles = new List<Role>
        {
            new Role { Name = "Admin", Description = "Administrator with full system access" },
            new Role { Name = "Moderator", Description = "Moderator with content management rights" },
            new Role { Name = "User", Description = "Standard community member" }
        };

        await context.Roles.AddRangeAsync(roles);
    }

    /// <summary>
    /// Seeds sample users with different roles
    /// </summary>
    private static async Task SeedUsersAsync(CommunityDbContext context, IPasswordHasher passwordHasher)
    {
        if (context.Users.Any())
            return; // Already seeded

        var users = new List<User>
        {
            new User
            {
                Username = "admin",
                Email = "admin@community.local",
                PasswordHash = passwordHasher.HashPassword("AdminPassword123!"),
                Bio = "System Administrator",
                ProfileImageUrl = "https://avatar.example.com/admin.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new User
            {
                Username = "moderator",
                Email = "mod@community.local",
                PasswordHash = passwordHasher.HashPassword("ModPassword123!"),
                Bio = "Community Moderator",
                ProfileImageUrl = "https://avatar.example.com/mod.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new User
            {
                Username = "john_developer",
                Email = "john@example.com",
                PasswordHash = passwordHasher.HashPassword("Password123!"),
                Bio = "Full Stack Developer",
                ProfileImageUrl = "https://avatar.example.com/john.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new User
            {
                Username = "sarah_designer",
                Email = "sarah@example.com",
                PasswordHash = passwordHasher.HashPassword("Password123!"),
                Bio = "UX/UI Designer",
                ProfileImageUrl = "https://avatar.example.com/sarah.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new User
            {
                Username = "mike_architect",
                Email = "mike@example.com",
                PasswordHash = passwordHasher.HashPassword("Password123!"),
                Bio = "Solutions Architect",
                ProfileImageUrl = "https://avatar.example.com/mike.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Assign roles to users
        var admin = context.Users.First(u => u.Username == "admin");
        var moderator = context.Users.First(u => u.Username == "moderator");
        var user1 = context.Users.First(u => u.Username == "john_developer");
        var user2 = context.Users.First(u => u.Username == "sarah_designer");
        var user3 = context.Users.First(u => u.Username == "mike_architect");

        var adminRole = context.Roles.First(r => r.Name == "Admin");
        var modRole = context.Roles.First(r => r.Name == "Moderator");
        var userRole = context.Roles.First(r => r.Name == "User");

        var userRoles = new List<UserRole>
        {
            new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
            new UserRole { UserId = moderator.Id, RoleId = modRole.Id },
            new UserRole { UserId = user1.Id, RoleId = userRole.Id },
            new UserRole { UserId = user2.Id, RoleId = userRole.Id },
            new UserRole { UserId = user3.Id, RoleId = userRole.Id }
        };

        await context.UserRoles.AddRangeAsync(userRoles);
    }

    /// <summary>
    /// Seeds sample posts across different categories
    /// </summary>
    private static async Task SeedPostsAsync(CommunityDbContext context)
    {
        if (context.Posts.Any())
            return; // Already seeded

        var users = context.Users.ToList();
        if (users.Count < 3)
            return; // Need at least 3 users

        var john = users.FirstOrDefault(u => u.Username == "john_developer");
        var sarah = users.FirstOrDefault(u => u.Username == "sarah_designer");
        var mike = users.FirstOrDefault(u => u.Username == "mike_architect");

        var posts = new List<Post>
        {
            new Post
            {
                Title = "Getting Started with ASP.NET Core 8",
                Content = "Learn the fundamentals of building web applications with ASP.NET Core 8. This comprehensive guide covers project setup, dependency injection, middleware, and routing.",
                Category = "Technology",
                AuthorId = john!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-28),
                UpdatedAt = DateTime.UtcNow.AddDays(-28),
                ViewCount = 145,
                IsPinned = true,
                IsLocked = false,
                IsDeleted = false
            },
            new Post
            {
                Title = "Entity Framework Core Tips and Tricks",
                Content = "Discover advanced techniques for working with Entity Framework Core including lazy loading, eager loading, explicit loading, and query optimization strategies.",
                Category = "Technology",
                AuthorId = john!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20),
                ViewCount = 98,
                IsPinned = false,
                IsLocked = false,
                IsDeleted = false
            },
            new Post
            {
                Title = "Web Design Trends 2025",
                Content = "Explore the latest web design trends including minimalist interfaces, AI-assisted design, micro-interactions, and accessibility-first approaches.",
                Category = "Design",
                AuthorId = sarah!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                UpdatedAt = DateTime.UtcNow.AddDays(-18),
                ViewCount = 203,
                IsPinned = true,
                IsLocked = false,
                IsDeleted = false
            },
            new Post
            {
                Title = "Building Scalable Microservices",
                Content = "A deep dive into microservices architecture, service discovery, API gateways, and distributed tracing for large-scale applications.",
                Category = "Architecture",
                AuthorId = mike!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UpdatedAt = DateTime.UtcNow.AddDays(-12),
                ViewCount = 87,
                IsPinned = false,
                IsLocked = false,
                IsDeleted = false
            },
            new Post
            {
                Title = "RESTful API Design Best Practices",
                Content = "Learn how to design clean, maintainable REST APIs following industry best practices including versioning, error handling, and security.",
                Category = "Technology",
                AuthorId = john!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
                ViewCount = 156,
                IsPinned = false,
                IsLocked = false,
                IsDeleted = false
            },
            new Post
            {
                Title = "CSS Grid vs Flexbox: When to Use Each",
                Content = "Compare CSS Grid and Flexbox layouts to understand which one is best for your specific use case and layout requirements.",
                Category = "Design",
                AuthorId = sarah!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow.AddDays(-8),
                ViewCount = 234,
                IsPinned = false,
                IsLocked = false,
                IsDeleted = false
            },
            new Post
            {
                Title = "Testing in .NET: Unit, Integration, and E2E",
                Content = "Comprehensive guide to implementing different levels of testing in .NET applications using xUnit, Moq, and integration testing frameworks.",
                Category = "Technology",
                AuthorId = john!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                ViewCount = 112,
                IsPinned = false,
                IsLocked = false,
                IsDeleted = false
            },
            new Post
            {
                Title = "Security Best Practices for Web Applications",
                Content = "Essential security considerations for modern web applications including authentication, authorization, HTTPS, CORS, and protecting against common vulnerabilities.",
                Category = "Security",
                AuthorId = mike!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                ViewCount = 178,
                IsPinned = false,
                IsLocked = false,
                IsDeleted = false
            },
            new Post
            {
                Title = "Introduction to Docker and Containerization",
                Content = "Get started with Docker containers and learn how to containerize your applications for consistent deployment across environments.",
                Category = "DevOps",
                AuthorId = mike!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                ViewCount = 94,
                IsPinned = false,
                IsLocked = false,
                IsDeleted = false
            },
            new Post
            {
                Title = "Performance Optimization Strategies",
                Content = "Learn techniques for profiling, analyzing, and optimizing the performance of your .NET applications through caching, async operations, and database optimization.",
                Category = "Technology",
                AuthorId = john!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                ViewCount = 67,
                IsPinned = false,
                IsLocked = false,
                IsDeleted = false
            }
        };

        await context.Posts.AddRangeAsync(posts);
    }

    /// <summary>
    /// Seeds sample comments on posts
    /// </summary>
    private static async Task SeedCommentsAsync(CommunityDbContext context)
    {
        if (context.Comments.Any())
            return; // Already seeded

        var users = context.Users.ToList();
        var posts = context.Posts.ToList();

        if (users.Count < 3 || posts.Count < 5)
            return; // Need sufficient data

        var john = users.FirstOrDefault(u => u.Username == "john_developer");
        var sarah = users.FirstOrDefault(u => u.Username == "sarah_designer");
        var mike = users.FirstOrDefault(u => u.Username == "mike_architect");

        var post1 = posts.FirstOrDefault(p => p.Title == "Getting Started with ASP.NET Core 8");
        var post2 = posts.FirstOrDefault(p => p.Title == "Entity Framework Core Tips and Tricks");
        var post3 = posts.FirstOrDefault(p => p.Title == "Web Design Trends 2025");
        var post4 = posts.FirstOrDefault(p => p.Title == "Building Scalable Microservices");
        var post5 = posts.FirstOrDefault(p => p.Title == "RESTful API Design Best Practices");

        var comments = new List<Comment>();

        if (post1 != null)
            comments.Add(new Comment
            {
                Content = "Great introduction! This really helped me get started with ASP.NET Core.",
                PostId = post1.Id,
                AuthorId = sarah!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-26),
                UpdatedAt = DateTime.UtcNow.AddDays(-26),
                IsDeleted = false
            });

        if (post2 != null)
            comments.Add(new Comment
            {
                Content = "Thanks for the detailed explanation of EF Core concepts. Very useful!",
                PostId = post2.Id,
                AuthorId = mike!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                UpdatedAt = DateTime.UtcNow.AddDays(-18),
                IsDeleted = false
            });

        if (post3 != null)
            comments.Add(new Comment
            {
                Content = "The design trends article is spot on. Minimalism is definitely the way forward.",
                PostId = post3.Id,
                AuthorId = john!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-16),
                UpdatedAt = DateTime.UtcNow.AddDays(-16),
                IsDeleted = false
            });

        if (post4 != null)
            comments.Add(new Comment
            {
                Content = "This microservices guide is comprehensive. Do you have recommendations for service mesh options?",
                PostId = post4.Id,
                AuthorId = john!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
                IsDeleted = false
            });

        if (post5 != null)
            comments.Add(new Comment
            {
                Content = "Excellent breakdown of REST API best practices. Bookmarking this for reference.",
                PostId = post5.Id,
                AuthorId = sarah!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow.AddDays(-8),
                IsDeleted = false
            });

        if (post1 != null)
            comments.Add(new Comment
            {
                Content = "The dependency injection section was particularly helpful.",
                PostId = post1.Id,
                AuthorId = mike!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-24),
                UpdatedAt = DateTime.UtcNow.AddDays(-24),
                IsDeleted = false
            });

        if (post3 != null)
            comments.Add(new Comment
            {
                Content = "Looking forward to implementing these trends in my next project.",
                PostId = post3.Id,
                AuthorId = mike!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-14),
                IsDeleted = false
            });

        if (post5 != null)
            comments.Add(new Comment
            {
                Content = "Don't forget about rate limiting and throttling!",
                PostId = post5.Id,
                AuthorId = mike!.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                UpdatedAt = DateTime.UtcNow.AddDays(-6),
                IsDeleted = false
            });

        await context.Comments.AddRangeAsync(comments);
    }

    /// <summary>
    /// Gets all seeded users for testing reference
    /// </summary>
    public static class Users
    {
        public const string AdminUsername = "admin";
        public const string AdminEmail = "admin@community.local";
        public const string AdminPassword = "AdminPassword123!";

        public const string ModeratorUsername = "moderator";
        public const string ModeratorEmail = "mod@community.local";
        public const string ModeratorPassword = "ModPassword123!";

        public const string User1Username = "john_developer";
        public const string User1Email = "john@example.com";
        public const string User1Password = "Password123!";

        public const string User2Username = "sarah_designer";
        public const string User2Email = "sarah@example.com";
        public const string User2Password = "Password123!";

        public const string User3Username = "mike_architect";
        public const string User3Email = "mike@example.com";
        public const string User3Password = "Password123!";
    }

    /// <summary>
    /// Gets all seeded post titles for testing reference
    /// </summary>
    public static class Posts
    {
        public const string AspNetCoreGuideTitle = "Getting Started with ASP.NET Core 8";
        public const string EfCoreTipsTitle = "Entity Framework Core Tips and Tricks";
        public const string WebDesignTrendsTitle = "Web Design Trends 2025";
        public const string MicroservicesTitle = "Building Scalable Microservices";
        public const string RestApiDesignTitle = "RESTful API Design Best Practices";
        public const string CssGridFlexboxTitle = "CSS Grid vs Flexbox: When to Use Each";
        public const string TestingGuideTitle = "Testing in .NET: Unit, Integration, and E2E";
        public const string SecurityBestPracticesTitle = "Security Best Practices for Web Applications";
        public const string DockerIntroTitle = "Introduction to Docker and Containerization";
        public const string PerformanceOptimizationTitle = "Performance Optimization Strategies";
    }

    /// <summary>
    /// Gets all seeded post categories for testing reference
    /// </summary>
    public static class Categories
    {
        public const string Technology = "Technology";
        public const string Design = "Design";
        public const string Architecture = "Architecture";
        public const string Security = "Security";
        public const string DevOps = "DevOps";
    }
}

