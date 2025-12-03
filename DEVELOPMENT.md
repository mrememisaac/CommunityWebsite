# Community Website - Development Guide

This guide provides detailed information for understanding and extending the Community Website ASP.NET Core project.

## 📋 Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Code Organization](#code-organization)
3. [LINQ Query Examples](#linq-query-examples)
4. [Entity Framework Patterns](#entity-framework-patterns)
5. [Testing Strategy](#testing-strategy)
6. [Performance Optimization](#performance-optimization)
7. [Extending the Project](#extending-the-project)

## 🏛️ Architecture Overview

### Layered Architecture

```
┌─────────────────────────────────────┐
│   Presentation Layer (UI)           │
│   - Bootstrap 5 HTML/CSS/JS         │
│   - Responsive Design               │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│   API Layer (Controllers)           │
│   - RESTful Endpoints               │
│   - HTTP Method Handling            │
│   - Request/Response Handling       │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│   Business Logic (Services)         │
│   - PostService                     │
│   - Complex Operations              │
│   - DTOs Mapping                    │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│   Data Access (Repositories)        │
│   - PostRepository                  │
│   - UserRepository                  │
│   - LINQ Queries                    │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│   Database (Entity Framework Core)  │
│   - DbContext                       │
│   - Models & Relationships          │
│   - SQL Server                      │
└─────────────────────────────────────┘
```

## 📂 Code Organization

### Core Project Structure

```
CommunityWebsite.Core/
├── Models/
│   └── DomainModels.cs          - Entity definitions
│       - User, Post, Comment, Event, Role
│
├── Data/
│   ├── CommunityDbContext.cs    - EF Core DbContext
│   │   - OnModelCreating()      - Fluent API configuration
│   │   - Seeding                - Initial data
│   │
│   └── Migrations/
│       └── 00001_InitialCreate.cs - Database schema
│
├── Repositories/
│   ├── IRepository.cs           - Generic + specialized interfaces
│   │   - IPostRepository        - Post-specific queries
│   │   - IUserRepository        - User-specific queries
│   │   - ICommentRepository     - Comment-specific queries
│   │   - IEventRepository       - Event-specific queries
│   │
│   └── Repository.cs            - Implementation classes
│       - GenericRepository<T>   - Base CRUD operations
│       - PostRepository         - LINQ examples
│       - UserRepository         - Complex queries
│       - CommentRepository      - Hierarchical data
│       - EventRepository        - Date-based queries
│
└── Services/
    └── PostService.cs           - Business logic
        - DTOs                   - Data transfer objects
        - CreatePostAsync()      - Complex operations
        - SearchPostsAsync()     - Advanced queries
```

### Web Project Structure

```
CommunityWebsite.Web/
├── Controllers/
│   ├── PostsController.cs       - Post endpoints
│   │   [GET /api/posts/{id}]    - Get post detail
│   │   [GET /api/posts/featured]  - Trending posts
│   │   [POST /api/posts]        - Create post
│   │   [PUT /api/posts/{id}]    - Update post
│   │   [DELETE /api/posts/{id}] - Delete post
│   │
│   └── UsersController.cs       - User endpoints
│       [GET /api/users/{id}]    - User profile
│       [GET /api/users/role/{role}] - Users by role
│
├── wwwroot/
│   └── index.html               - Responsive UI
│       - Bootstrap 5 framework
│       - Custom CSS styling
│       - JavaScript interactivity
│
├── Program.cs                   - Startup configuration
│   - Service registration
│   - DbContext setup
│   - CORS configuration
│   - Middleware pipeline
│
└── appsettings.json             - Configuration
    - Connection strings
    - Logging settings
```

## 🔍 LINQ Query Examples

### 1. Pagination Pattern

```csharp
// Implementation in PostRepository.GetActivePostsAsync()
var skip = (pageNumber - 1) * pageSize;

return await _dbSet
    .Where(p => !p.IsDeleted && !p.IsLocked)      // Filter
    .OrderByDescending(p => p.IsPinned)           // Primary sort
    .ThenByDescending(p => p.CreatedAt)           // Secondary sort
    .Skip(skip)                                    // Pagination
    .Take(pageSize)                                // Limit results
    .Include(p => p.Author)                        // Eager load user
    .AsNoTracking()                                // Read-only
    .ToListAsync();
```

**Benefits**:

- Prevents loading all data into memory
- Efficient database execution
- Proper ordering for consistency
- Minimal data transfer

### 2. Eager Loading Pattern

```csharp
// Implementation in PostRepository.GetPostWithCommentsAsync()
return await _dbSet
    .Where(p => p.Id == postId && !p.IsDeleted)
    .Include(p => p.Author)                       // Load author
    .Include(p => p.Comments                      // Load comments
        .Where(c => !c.IsDeleted))                // Filter comments
    .ThenInclude(c => c.Author)                   // Load comment authors
    .AsNoTracking()
    .FirstOrDefaultAsync();
```

**Advantages**:

- Prevents N+1 query problem
- Loads related data in single query
- Better performance than lazy loading

### 3. Complex Filtering Pattern

```csharp
// Implementation in PostRepository.SearchPostsAsync()
var lowerSearchTerm = searchTerm.ToLower();

return await _dbSet
    .Where(p => !p.IsDeleted &&
           (p.Title.ToLower().Contains(lowerSearchTerm) ||
            p.Content.ToLower().Contains(lowerSearchTerm)))
    .OrderByDescending(p => p.CreatedAt)
    .Include(p => p.Author)
    .AsNoTracking()
    .ToListAsync();
```

**Demonstrates**:

- Multiple conditions with OR logic
- String operations in LINQ
- Case-insensitive search
- Proper ordering

### 4. Date-Based Query Pattern

```csharp
// Implementation in PostRepository.GetTrendingPostsAsync()
var startDate = DateTime.UtcNow.AddDays(-days);

return await _dbSet
    .Where(p => p.CreatedAt >= startDate && !p.IsDeleted)
    .OrderByDescending(p => p.ViewCount)
    .ThenByDescending(p => p.CreatedAt)
    .Take(limit)
    .Include(p => p.Author)
    .AsNoTracking()
    .ToListAsync();
```

**Shows**:

- DateTime calculations
- Range filtering
- Combined ordering
- Top-N queries

### 5. Relationship Navigation Pattern

```csharp
// Implementation in UserRepository.GetUsersByRoleAsync()
return await _dbSet
    .Where(u => u.IsActive &&
           u.UserRoles.Any(ur => ur.Role.Name == roleName))
    .Include(u => u.UserRoles)
    .ThenInclude(ur => ur.Role)
    .AsNoTracking()
    .ToListAsync();
```

**Illustrates**:

- Navigation through relationships
- Collection filtering with Any()
- Eager loading collections
- Many-to-many relationship handling

## 🗄️ Entity Framework Patterns

### 1. DbContext Configuration

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure User entity
    modelBuilder.Entity<User>(entity =>
    {
        entity.HasKey(e => e.Id);

        // Unique constraints
        entity.HasIndex(e => e.Email).IsUnique();
        entity.HasIndex(e => e.Username).IsUnique();

        // Performance indexes
        entity.HasIndex(e => e.IsActive);

        // Relationships
        entity.HasMany(e => e.Posts)
            .WithOne(p => p.Author)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
```

### 2. Relationships Configuration

```csharp
// One-to-Many (User -> Posts)
entity.HasMany(e => e.Posts)
    .WithOne(p => p.Author)
    .HasForeignKey(p => p.AuthorId);

// Many-to-Many (User -> Roles)
entity.HasMany(e => e.UserRoles)
    .WithOne(ur => ur.User)
    .HasForeignKey(ur => ur.UserId);

// Self-referencing (Comment -> Replies)
entity.HasMany(e => e.Replies)
    .WithOne(c => c.ParentComment)
    .HasForeignKey(c => c.ParentCommentId);
```

### 3. Data Seeding

```csharp
private static void SeedInitialData(ModelBuilder modelBuilder)
{
    // Seed reference data
    var roles = new[]
    {
        new Role { Id = 1, Name = "Admin" },
        new Role { Id = 2, Name = "Moderator" },
        new Role { Id = 3, Name = "User" }
    };
    modelBuilder.Entity<Role>().HasData(roles);

    // Seed sample users
    var users = new[]
    {
        new User { Id = 1, Username = "admin_user", /* ... */ }
    };
    modelBuilder.Entity<User>().HasData(users);
}
```

## 🧪 Testing Strategy

### Unit Test Structure (AAA Pattern)

```csharp
[Fact]
public async Task CreatePostAsync_WithValidRequest_CreatesPost()
{
    // ARRANGE - Setup test data and mocks
    var request = new CreatePostRequest { /* ... */ };
    _mockUserRepository
        .Setup(r => r.GetByIdAsync(request.AuthorId))
        .ReturnsAsync(user);

    // ACT - Execute the method
    var result = await _postService.CreatePostAsync(request);

    // ASSERT - Verify results
    result.Should().NotBeNull();
    result.Title.Should().Be(request.Title);

    _mockPostRepository.Verify(r => r.AddAsync(It.IsAny<Post>()), Times.Once);
}
```

### Mock Setup Examples

```csharp
// Setup return values
_mockRepository
    .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
    .ReturnsAsync(post);

// Verify method was called
_mockRepository.Verify(r => r.AddAsync(It.IsAny<Post>()), Times.Once);

// Setup exception
_mockRepository
    .Setup(r => r.GetByIdAsync(999))
    .ThrowsAsync(new InvalidOperationException());
```

### Assertion Libraries (FluentAssertions)

```csharp
// Readable assertions
result.Should().NotBeNull();
result.Title.Should().Be(expectedTitle);
result.Comments.Should().HaveCount(3);
result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(999));
```

## ⚡ Performance Optimization

### 1. Indexing Strategy

```csharp
// High-value indexes
entity.HasIndex(e => e.Email).IsUnique();           // Email lookups
entity.HasIndex(e => e.CreatedAt);                  // Sorting/filtering
entity.HasIndex(e => e.IsDeleted);                  // Soft delete filtering
entity.HasIndex(e => new { e.UserId, e.RoleId })   // Unique constraints
    .IsUnique();
```

### 2. Query Optimization Tips

```csharp
// ❌ BAD - N+1 queries, loads all data
var posts = _dbSet.ToList();
foreach (var post in posts)
{
    var author = post.Author; // Separate query for each post
}

// ✅ GOOD - Single query with eager loading
var posts = await _dbSet
    .Include(p => p.Author)
    .AsNoTracking()
    .ToListAsync();

// ❌ BAD - Client-side filtering
var result = _dbSet.ToList().Where(p => p.Title.Contains("test"));

// ✅ GOOD - Server-side filtering
var result = await _dbSet
    .Where(p => p.Title.Contains("test"))
    .ToListAsync();

// ❌ BAD - Tracking unnecessary data
var posts = await _dbSet
    .Include(p => p.Comments)
    .ToListAsync();

// ✅ GOOD - Read-only queries
var posts = await _dbSet
    .Include(p => p.Comments)
    .AsNoTracking()
    .ToListAsync();
```

### 3. Pagination for Performance

```csharp
// Always paginate large result sets
var skip = (pageNumber - 1) * pageSize;
var posts = await _dbSet
    .Skip(skip)
    .Take(pageSize)
    .ToListAsync();
```

## 🚀 Extending the Project

### Adding a New Feature

#### 1. Create Domain Model

```csharp
public class NewEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserId { get; set; }
    public User? User { get; set; }
}
```

#### 2. Add DbSet to Context

```csharp
public DbSet<NewEntity> NewEntities { get; set; } = null!;
```

#### 3. Configure in OnModelCreating

```csharp
modelBuilder.Entity<NewEntity>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasOne(e => e.User)
        .WithMany()
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

#### 4. Create Repository Interface

```csharp
public interface INewEntityRepository : IRepository<NewEntity>
{
    Task<IEnumerable<NewEntity>> GetByUserIdAsync(int userId);
    // Add specialized queries
}
```

#### 5. Implement Repository

```csharp
public class NewEntityRepository : GenericRepository<NewEntity>, INewEntityRepository
{
    public NewEntityRepository(CommunityDbContext context) : base(context) { }

    public async Task<IEnumerable<NewEntity>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(e => e.UserId == userId)
            .AsNoTracking()
            .ToListAsync();
    }
}
```

#### 6. Create Service

```csharp
public class NewEntityService
{
    private readonly INewEntityRepository _repository;

    public async Task<NewEntity> GetAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }
}
```

#### 7. Register in Dependency Injection

```csharp
// In Program.cs
builder.Services.AddScoped<INewEntityRepository, NewEntityRepository>();
builder.Services.AddScoped<INewEntityService, NewEntityService>();
```

#### 8. Create Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class NewEntityController : ControllerBase
{
    private readonly INewEntityService _service;

    [HttpGet("{id}")]
    public async Task<ActionResult<NewEntity>> Get(int id)
    {
        var entity = await _service.GetAsync(id);
        if (entity == null) return NotFound();
        return Ok(entity);
    }
}
```

## 📝 Coding Standards

### Naming Conventions

- **Classes**: PascalCase (User, PostService)
- **Methods**: PascalCase (GetPostAsync, CreatePostAsync)
- **Variables**: camelCase (userId, postService)
- **Constants**: UPPER_SNAKE_CASE or PascalCase
- **Interfaces**: IPascalCase (IPostRepository, IPostService)

### Comments & Documentation

```csharp
/// <summary>
/// Gets a post with all associated comments.
/// </summary>
/// <param name="postId">The ID of the post to retrieve</param>
/// <returns>The post with comments, or null if not found</returns>
/// <remarks>
/// Uses eager loading to prevent N+1 queries.
/// Returns only non-deleted comments.
/// </remarks>
public async Task<Post?> GetPostWithCommentsAsync(int postId)
{
    // Implementation
}
```

### Async/Await Best Practices

```csharp
// Always use async for I/O operations
public async Task<Post?> GetPostAsync(int id)
{
    return await _repository.GetByIdAsync(id);
}

// Use Task.FromResult for sync operations
public Task<string> GetCachedValue(string key)
{
    return Task.FromResult(_cache[key]);
}
```

## 🔗 Common Scenarios

### Implement Search

```csharp
public async Task<IEnumerable<Post>> SearchAsync(string term)
{
    return await _repository.SearchPostsAsync(term);
}
```

### Handle Pagination

```csharp
var posts = await _repository.GetActivePostsAsync(pageNumber: 1, pageSize: 10);
```

### Implement Soft Delete

```csharp
public async Task DeleteAsync(int postId)
{
    var post = await _repository.GetByIdAsync(postId);
    post.IsDeleted = true;
    await _repository.UpdateAsync(post);
    await _repository.SaveChangesAsync();
}
```

### Filter by Date Range

```csharp
public async Task<IEnumerable<Event>> GetEventsByDateAsync(
    DateTime startDate,
    DateTime endDate)
{
    return await _repository.GetAll()
        .Where(e => e.StartDate >= startDate && e.StartDate <= endDate)
        .OrderBy(e => e.StartDate)
        .ToListAsync();
}
```

---

This development guide provides the foundation for understanding and extending the Community Website project. Refer back to specific sections when implementing new features or optimizing existing code.
