using Microsoft.EntityFrameworkCore;
using CommunityWebsite.Core.Models;

namespace CommunityWebsite.Core.Data;

/// <summary>
/// Entity Framework Core context for the community website.
/// Demonstrates proper EF Core configuration and performance considerations.
/// </summary>
public class CommunityDbContext : DbContext
{
    public CommunityDbContext(DbContextOptions<CommunityDbContext> options)
        : base(options)
    {
    }

    // DbSets for each entity
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.IsActive);

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            // Define relationships
            entity.HasMany(e => e.Posts)
                .WithOne(p => p.Author)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Comments)
                .WithOne(c => c.Author)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Post entity
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsPinned);
            entity.HasIndex(e => e.IsDeleted);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.HasMany(e => e.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Comment entity
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.PostId);
            entity.HasIndex(e => e.IsDeleted);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.HasMany(e => e.Replies)
                .WithOne(c => c.ParentComment)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure UserRole entity (many-to-many)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Event entity
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.IsCancelled);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .IsRequired();

            entity.HasOne(e => e.Organizer)
                .WithMany()
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed initial data
        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        // Seed roles
        var roles = new[]
        {
            new Role { Id = 1, Name = "Admin", Description = "Administrator with full access" },
            new Role { Id = 2, Name = "Moderator", Description = "Moderator can manage posts and comments" },
            new Role { Id = 3, Name = "User", Description = "Regular community member" }
        };
        modelBuilder.Entity<Role>().HasData(roles);

        // Seed sample users
        var users = new[]
        {
            new User
            {
                Id = 1,
                Username = "admin_user",
                Email = "admin@community.local",
                PasswordHash = "hashed_password_1", // In production, use proper hashing
                Bio = "Community Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Username = "john_doe",
                Email = "john@community.local",
                PasswordHash = "hashed_password_2",
                Bio = "Passionate community member",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
        modelBuilder.Entity<User>().HasData(users);

        // Seed user-role assignments
        var userRoles = new[]
        {
            new UserRole { Id = 1, UserId = 1, RoleId = 1, AssignedAt = DateTime.UtcNow },
            new UserRole { Id = 2, UserId = 2, RoleId = 3, AssignedAt = DateTime.UtcNow }
        };
        modelBuilder.Entity<UserRole>().HasData(userRoles);

        // Seed sample posts
        var posts = new[]
        {
            new Post
            {
                Id = 1,
                Title = "Welcome to the Community!",
                Content = "This is a welcome post introducing our amazing community. Feel free to introduce yourself and connect with other members.",
                AuthorId = 1,
                Category = "Announcements",
                CreatedAt = DateTime.UtcNow,
                IsPinned = true,
                ViewCount = 150
            },
            new Post
            {
                Id = 2,
                Title = "Tips for New Members",
                Content = "Here are some helpful tips for getting started in our community...",
                AuthorId = 2,
                Category = "General",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ViewCount = 75
            }
        };
        modelBuilder.Entity<Post>().HasData(posts);
    }
}
