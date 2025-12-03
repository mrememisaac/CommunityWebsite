# Architecture Overview

## Project Structure

This project demonstrates best practices for combining **Web API** and **MVC** in a single ASP.NET Core 8.0 application.

```
CommunityWebsite/
├── src/
│   ├── CommunityWebsite.Core/          # Business logic layer
│   │   ├── Common/                     # Result pattern, shared utilities
│   │   ├── Data/                       # EF Core DbContext, migrations
│   │   ├── DTOs/                       # Data Transfer Objects
│   │   │   ├── Requests/               # Input DTOs (Create/Update)
│   │   │   └── Responses/              # Output DTOs (Display)
│   │   ├── Models/                     # Domain entities
│   │   ├── Repositories/               # Data access layer
│   │   │   └── Interfaces/
│   │   ├── Services/                   # Business logic
│   │   │   └── Interfaces/
│   │   ├── Specifications/             # Business rule specifications
│   │   └── Validators/                 # Input validation
│   │       └── Interfaces/
│   │
│   └── CommunityWebsite.Web/           # Presentation layer
│       ├── Controllers/                # API + MVC controllers
│       ├── Views/                      # Razor views
│       │   ├── Shared/                 # Layout, partials
│       │   ├── Home/
│       │   ├── Posts/
│       │   ├── Events/
│       │   └── Account/
│       └── wwwroot/                    # Static assets
│           ├── css/
│           └── js/
│
└── tests/
    └── CommunityWebsite.Tests/         # Unit & integration tests
```

## API vs MVC Controller Separation

### API Controllers (`/api/*`)

RESTful endpoints for programmatic access (mobile apps, SPAs, third-party integrations):

| Controller           | Route           | Purpose                                   |
| -------------------- | --------------- | ----------------------------------------- |
| `AuthController`     | `/api/auth`     | Authentication (login, register, refresh) |
| `PostsController`    | `/api/posts`    | CRUD operations for posts                 |
| `CommentsController` | `/api/comments` | Comment management                        |
| `EventsController`   | `/api/events`   | Event management                          |
| `UsersController`    | `/api/users`    | User profiles                             |
| `RolesController`    | `/api/roles`    | Role management (admin)                   |

**Characteristics:**

- Inherit from `ControllerBase`
- Use `[ApiController]` attribute
- Return JSON (`Produces("application/json")`)
- Stateless authentication (JWT Bearer)
- RESTful routing conventions

### MVC View Controllers

Server-rendered HTML pages with Razor views:

| Controller             | Route        | Purpose                        |
| ---------------------- | ------------ | ------------------------------ |
| `HomeController`       | `/`, `/Home` | Landing page, about, contact   |
| `PostsViewController`  | `/Posts`     | Browse and view posts          |
| `EventsViewController` | `/Events`    | Browse and view events         |
| `AccountController`    | `/Account`   | Login, register, profile pages |

**Characteristics:**

- Inherit from `Controller`
- Return `ViewResult` with Razor views
- Use ViewBag/ViewData for dynamic content
- Client-side API calls via JavaScript

## Why Combine API + MVC?

### Advantages

1. **Single Deployment** - One application to deploy and maintain
2. **Shared Services** - Same business logic for both API and views
3. **Consistent Security** - Unified authentication/authorization
4. **SEO-Friendly** - Server-rendered pages for search engines
5. **Progressive Enhancement** - Works without JavaScript, enhanced with it

### When to Separate

Consider separate projects when:

- API needs independent scaling
- Different teams own API vs frontend
- Multiple frontends (React, mobile apps) are primary
- Microservices architecture is required

## Design Patterns Used

### 1. Repository Pattern

Abstracts data access from business logic:

```csharp
public interface IPostRepository : IRepository<Post>
{
    Task<IEnumerable<Post>> GetByAuthorIdAsync(int authorId);
    Task<IEnumerable<Post>> SearchAsync(string searchTerm);
}
```

### 2. Service Layer Pattern

Encapsulates business logic:

```csharp
public interface IPostService
{
    Task<Result<PostDetailDto>> GetPostDetailAsync(int id);
    Task<Result<Post>> CreatePostAsync(CreatePostDto dto, int authorId);
}
```

### 3. Result Pattern

Explicit success/failure handling without exceptions:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }
}
```

### 4. Specification Pattern

Encapsulates business rules:

```csharp
public class ValidPostSpecification : ISpecification<CreatePostDto>
{
    public bool IsSatisfiedBy(CreatePostDto dto) =>
        !string.IsNullOrWhiteSpace(dto.Title) &&
        dto.Title.Length <= 300 &&
        !string.IsNullOrWhiteSpace(dto.Content);
}
```

### 5. Dependency Injection

All services registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
```

## SOLID Principles

| Principle                 | Implementation                                                         |
| ------------------------- | ---------------------------------------------------------------------- |
| **S**ingle Responsibility | Each service handles one domain (PostService, EventService)            |
| **O**pen/Closed           | Services extensible via interfaces without modification                |
| **L**iskov Substitution   | All repositories implement IRepository<T> correctly                    |
| **I**nterface Segregation | Specific interfaces (IPostRepository) vs generic (IRepository<T>)      |
| **D**ependency Inversion  | Controllers depend on abstractions (IPostService), not implementations |

## Authentication Flow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Client    │────▶│  /api/auth  │────▶│ Auth Service│
│  (Browser)  │     │   /login    │     │             │
└─────────────┘     └─────────────┘     └─────────────┘
       │                   │                   │
       │                   ▼                   ▼
       │            ┌─────────────┐     ┌─────────────┐
       │            │    JWT      │     │  Password   │
       │            │   Token     │     │   Hasher    │
       │            └─────────────┘     └─────────────┘
       │                   │
       ▼                   ▼
┌─────────────┐     ┌─────────────┐
│ localStorage│◀────│   Return    │
│  (token)    │     │   Token     │
└─────────────┘     └─────────────┘
       │
       ▼
┌─────────────┐
│ API Calls   │ ──▶ Authorization: Bearer <token>
│ via fetch() │
└─────────────┘
```

## Database Schema

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│    Users     │     │    Posts     │     │   Comments   │
├──────────────┤     ├──────────────┤     ├──────────────┤
│ Id (PK)      │◀───┐│ Id (PK)      │◀───┐│ Id (PK)      │
│ Username     │    ││ Title        │    ││ Content      │
│ Email        │    ││ Content      │    ││ PostId (FK)  │─┘
│ PasswordHash │    ││ AuthorId(FK) │─┘  ││ AuthorId(FK) │─┐
│ CreatedAt    │    │└──────────────┘    │└──────────────┘ │
└──────────────┘    │                    │                 │
       │            │                    │                 │
       │            │  ┌──────────────┐  │                 │
       │            │  │    Events    │  │                 │
       │            │  ├──────────────┤  │                 │
       │            │  │ Id (PK)      │  │                 │
       │            │  │ Title        │  │                 │
       │            └──│ OrganizerId  │──┘                 │
       │               │ StartDate    │                    │
       │               └──────────────┘                    │
       │                                                   │
       └───────────────────────────────────────────────────┘
```

## Testing Strategy

| Test Type             | Coverage              | Location                                  |
| --------------------- | --------------------- | ----------------------------------------- |
| **Unit Tests**        | Services, Validators  | `/tests/Services/`, `/tests/Models/`      |
| **Repository Tests**  | Data access           | `/tests/Repositories/`                    |
| **Controller Tests**  | API endpoints         | `/tests/Controllers/`                     |
| **Integration Tests** | Full request pipeline | `/tests/Controllers/*IntegrationTests.cs` |

**Total: 282 tests**

## Running the Application

```bash
# Development
dotnet run --project src/CommunityWebsite.Web

# Run tests
dotnet test

# Build for production
dotnet publish -c Release
```

## API Documentation

Swagger UI available at: `http://localhost:5000/` (development mode)

API endpoints documented with XML comments and OpenAPI specifications.
