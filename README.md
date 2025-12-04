# Community Website - ASP.NET Developer Portfolio Project

A comprehensive ASP.NET Core 8.0 web application demonstrating professional-grade C# development practices, Entity Framework Core integration, and responsive web design with Bootstrap. This project showcases all skills required for the ASP.NET Developer position.

## 🎯 Project Overview

This community website platform demonstrates:

- **Backend**: High-quality C# code with LINQ, Entity Framework Core, and async patterns
- **Architecture**: Clean repository pattern with dependency injection
- **Frontend**: Responsive Bootstrap 5 UI with modern design
- **Testing**: Comprehensive unit tests with xUnit, Moq, and FluentAssertions
- **Performance**: Optimized EF Core queries with indexes and proper pagination
- **User Management**: Complete user profiles, post history, and user-specific content views

## 📦 Tech Stack

### Backend

- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server with Entity Framework Core
- **ORM**: Entity Framework Core 8.0
- **Architecture Pattern**: Repository Pattern + Dependency Injection
- **Testing**: xUnit, Moq, FluentAssertions

### Frontend

- **Framework**: Bootstrap 5.3
- **Markup**: HTML5
- **Styling**: Custom CSS with responsive design
- **Interactivity**: Vanilla JavaScript

### Project Structure

```
CommunityWebsite/
├── src/
│   ├── CommunityWebsite.Web/          # ASP.NET Core Web API
│   │   ├── Controllers/               # API endpoints
│   │   ├── wwwroot/                   # Static files (HTML, CSS, JS)
│   │   ├── Program.cs                 # Startup configuration
│   │   └── appsettings.json          # Configuration
│   └── CommunityWebsite.Core/         # Core business logic
│       ├── Models/                    # Domain models
│       ├── Data/                      # DbContext & migrations
│       ├── Repositories/              # Data access patterns
│       └── Services/                  # Business logic
└── tests/
    └── CommunityWebsite.Tests/        # Unit tests
        ├── Services/                  # Service layer tests
        └── Models/                    # Domain model tests
```

## 🏗️ Architecture Highlights

### Domain Models

Comprehensive domain models in `Models/DomainModels.cs`:

- **User**: Community members with roles and relationships
- **Post**: Forum discussions with threading and soft deletes
- **Comment**: Nested comments with parent-child relationships
- **Event**: Community events with organizers
- **Role**: Role-based access control

### Data Access Layer

`Repositories/Repository.cs` demonstrates:

- **Generic Repository Pattern**: Reusable CRUD operations
- **LINQ Excellence**:
  - Complex queries with `.Include()` for eager loading
  - `.AsNoTracking()` for read-only operations
  - Filtering, ordering, pagination patterns
  - Date calculations and aggregations
- **Performance Optimization**:
  - Strategic indexing on frequently queried columns
  - Pagination to limit data transfers
  - Query optimization with AsNoTracking

Key implementations:

```csharp
// Complex LINQ query with eager loading
public async Task<Post?> GetPostWithCommentsAsync(int postId)
{
    return await _dbSet
        .Where(p => p.Id == postId && !p.IsDeleted)
        .Include(p => p.Author)
        .Include(p => p.Comments.Where(c => !c.IsDeleted))
        .ThenInclude(c => c.Author)
        .AsNoTracking()
        .FirstOrDefaultAsync();
}
```

### Service Layer

Business logic in `Services/PostService.cs`:

- Data transfer objects (DTOs) for API contracts
- Complex business operations
- Transaction management
- Proper error handling

### API Controllers

RESTful endpoints in `Controllers/PostsController.cs`:

- Proper HTTP methods and status codes
- Input validation and error responses
- Logging with ILogger
- Documentation with XML comments

## 💡 Key Features Demonstrating Required Skills

### 1. **Excellent C# Knowledge**

- Nullable reference types (`#nullable enable`)
- LINQ with complex queries and PLINQ patterns
- Async/await patterns throughout
- Dependency injection and IoC principles
- Extension methods and generics

### 2. **Entity Framework Mastery**

- DbContext configuration and relationships
- Lazy vs Eager loading strategies
- Query optimization and performance
- Soft deletes implementation
- Data seeding for development
- Migration patterns (ready for Migrations)

### 3. **LINQ Proficiency**

Examples throughout codebase:

```csharp
// Pagination with ordering
.Skip((pageNumber - 1) * pageSize)
.Take(pageSize)

// Complex filtering
.Where(p => p.CreatedAt >= startDate && !p.IsDeleted)
.OrderByDescending(p => p.ViewCount)

// Grouped and aggregated queries
.Include(c => c.Replies.Where(r => !r.IsDeleted))
```

### 4. **SQL Performance Knowledge**

- Proper indexing strategy in EF Core configuration
- N+1 query prevention with eager loading
- Efficient pagination implementation
- Use of `AsNoTracking()` for read operations

### 5. **High-Quality Code**

- Clean code principles
- Single responsibility pattern
- Proper separation of concerns
- Comprehensive XML documentation
- Consistent naming conventions

### 6. **HTML, Bootstrap, & Responsive Design**

- Fully responsive Bootstrap 5 layout
- Mobile-first approach
- Custom CSS with CSS variables
- Professional UI/UX patterns
- Accessibility considerations

### 7. **JavaScript Integration**

- Event handling
- Form submission
- API integration ready
- Modern ES6 patterns

### 8. **Unit Testing Excellence**

- Test service layer with business logic
- Mock external dependencies with Moq
- Test data factories and helpers
- Arrange-Act-Assert pattern
- FluentAssertions for readable tests
- Independence and isolation of tests

## 🚀 Running the Project

### Prerequisites

- .NET 8.0 SDK
- SQL Server or LocalDB
- Visual Studio Code or Visual Studio

### Setup Instructions

1. **Restore NuGet packages**:

   ```bash
   dotnet restore
   ```

2. **Create database** (if needed):

   ```bash
   dotnet ef database update -p src/CommunityWebsite.Core -s src/CommunityWebsite.Web
   ```

3. **Run the application**:

   ```bash
   dotnet run -p src/CommunityWebsite.Web
   ```

4. **Run unit tests**:

   ```bash
   dotnet test
   ```

5. **Access the application**:
   - Web: `https://localhost:7000`
   - Swagger API: `https://localhost:7000/swagger`

## 📝 Code Examples

### LINQ Query - Trending Posts

```csharp
public async Task<IEnumerable<Post>> GetTrendingPostsAsync(int days = 7, int limit = 10)
{
    var startDate = DateTime.UtcNow.AddDays(-days);

    return await _dbSet
        .Where(p => p.CreatedAt >= startDate && !p.IsDeleted)
        .OrderByDescending(p => p.ViewCount)
        .ThenByDescending(p => p.CreatedAt)
        .Take(limit)
        .Include(p => p.Author)
        .AsNoTracking()
        .ToListAsync();
}
```

### Service Layer - Create Post

```csharp
public async Task<PostDetailDto> CreatePostAsync(CreatePostRequest request)
{
    var user = await _userRepository.GetByIdAsync(request.AuthorId);
    if (user == null)
        throw new InvalidOperationException("User not found");

    var post = new Post
    {
        Title = request.Title,
        Content = request.Content,
        Category = request.Category,
        AuthorId = request.AuthorId,
        CreatedAt = DateTime.UtcNow
    };

    await _postRepository.AddAsync(post);
    await _postRepository.SaveChangesAsync();

    return MapPostToDetailDto(post);
}
```

### Unit Test - Service Testing

```csharp
[Fact]
public async Task CreatePostAsync_WithValidRequest_CreatesPost()
{
    // Arrange
    var request = new CreatePostRequest { /* ... */ };
    _mockUserRepository
        .Setup(r => r.GetByIdAsync(request.AuthorId))
        .ReturnsAsync(user);

    // Act
    var result = await _postService.CreatePostAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.Title.Should().Be(request.Title);
}
```

## 🎨 UI Features

- **Responsive Navigation**: Mobile-friendly navbar with collapsible menu
- **Hero Section**: Eye-catching landing section with call-to-action
- **Post Cards**: Clean card design with hover effects
- **Search Integration**: Search bar for post discovery
- **Pagination**: Professional pagination controls
- **Sidebar Widgets**: Featured events, categories, top contributors
- **Footer**: Comprehensive footer with links and social media
- **Accessibility**: Semantic HTML and ARIA labels

## 📊 Performance Considerations

1. **Query Optimization**:

   - Indexes on frequently filtered columns
   - Eager loading to prevent N+1 queries
   - AsNoTracking for read-only operations
   - Pagination limits data transfer

2. **Caching Ready**:

   - Architecture supports distributed caching
   - Service layer designed for cache integration

3. **Scalability**:
   - Repository pattern for easy switching storage implementations
   - Async/await for high concurrency
   - Proper connection pooling with EF Core

## 🔒 Security Features

- Soft deletes for data integrity
- Nullable reference types to prevent null reference exceptions
- Role-based access control structure
- Dependency injection prevents tight coupling
- Proper error handling without exposing internals

## 📚 Documentation

- **XML Comments**: Every public class and method documented
- **README**: Comprehensive project documentation (this file)
- **Code Examples**: Inline examples showing best practices
- **Test Examples**: Unit tests serve as documentation

## 🎓 Learning Resources Demonstrated

This project showcases understanding of:

- ASP.NET Core fundamentals and advanced patterns
- Entity Framework Core best practices and performance optimization
- LINQ query complexity and optimization
- SQL and database design principles
- Unit testing and TDD principles
- Clean code and architectural patterns
- Bootstrap responsive design framework
- RESTful API design
- Dependency injection and IoC

## 🤝 Contributing

This is a portfolio project. For improvements or suggestions, please refer to the code and consider how it demonstrates professional practices.

## 📄 License

This project is provided as a portfolio demonstration.

---

**Ready for production?** This codebase demonstrates the high standards expected in professional ASP.NET development. It's structured to scale with additional features while maintaining code quality and performance.
