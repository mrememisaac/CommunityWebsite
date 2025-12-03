# Getting Started Guide

Welcome to the **Community Website** project! This guide will walk you through setting up, running, and exploring the application.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start](#quick-start)
3. [Project Setup](#project-setup)
4. [Running the Application](#running-the-application)
5. [Exploring the Application](#exploring-the-application)
6. [Running Tests](#running-tests)
7. [Development Workflow](#development-workflow)
8. [Docker Setup](#docker-setup)
9. [Project Walkthrough](#project-walkthrough)

---

## Prerequisites

Before you begin, ensure you have the following installed:

| Tool                                                 | Version | Purpose                         |
| ---------------------------------------------------- | ------- | ------------------------------- |
| [.NET SDK](https://dotnet.microsoft.com/download)    | 8.0+    | Build and run the application   |
| [Visual Studio Code](https://code.visualstudio.com/) | Latest  | IDE (optional)                  |
| [Docker](https://www.docker.com/)                    | Latest  | Container deployment (optional) |
| [Git](https://git-scm.com/)                          | Latest  | Version control                 |

### Verify Installation

```powershell
# Check .NET version
dotnet --version

# Should output: 8.0.x or higher
```

---

## Quick Start

```powershell
# Clone the repository
git clone <repository-url>
cd captain-of-industry

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/CommunityWebsite.Web

# Open in browser
# Navigate to: http://localhost:5000
```

---

## Project Setup

### 1. Clone and Navigate

```powershell
git clone <repository-url>
cd captain-of-industry
```

### 2. Restore NuGet Packages

```powershell
dotnet restore
```

### 3. Build the Solution

```powershell
dotnet build
```

### 4. Initialize Database (In-Memory by Default)

The application uses Entity Framework Core with an **in-memory database** by default for easy development. No additional database setup required!

For production, configure a real database in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CommunityWebsite;Trusted_Connection=true;"
  }
}
```

---

## Running the Application

### Option 1: Command Line

```powershell
cd src/CommunityWebsite.Web
dotnet run
```

### Option 2: Visual Studio Code

1. Open the folder in VS Code
2. Press `F5` to start debugging
3. Select ".NET Core" if prompted

### Option 3: Visual Studio

1. Open `CommunityWebsite.sln`
2. Set `CommunityWebsite.Web` as startup project
3. Press `F5` to run

### Default URLs

| URL                                   | Description               |
| ------------------------------------- | ------------------------- |
| `http://localhost:5000`               | Swagger API Documentation |
| `http://localhost:5000/Home`          | Home Page (MVC)           |
| `http://localhost:5000/Posts`         | Posts Page                |
| `http://localhost:5000/Events`        | Events Page               |
| `http://localhost:5000/Account/Login` | Login Page                |

---

## Exploring the Application

### 🏠 Home Page

- View recent posts and upcoming events
- Modern responsive design with Bootstrap 5

### 📝 Posts

- Browse, search, and filter posts
- View post details with comments
- Create and edit posts (when authenticated)

### 📅 Events

- View upcoming and past events
- Filter by date and search by keyword
- Register/unregister for events

### 🔐 Authentication

- Register a new account
- Login with email/password
- JWT token-based authentication

### 📡 API Endpoints (Swagger)

- Interactive API documentation at root URL
- Test all endpoints directly in browser
- View request/response schemas

---

## Running Tests

### Run All Tests

```powershell
dotnet test
```

### Run with Coverage

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test Class

```powershell
dotnet test --filter "FullyQualifiedName~PostsControllerTests"
```

### Test Categories

| Category              | Count | Description                          |
| --------------------- | ----- | ------------------------------------ |
| Unit Tests            | ~200  | Service, Repository, Validator tests |
| Controller Tests      | ~50   | API controller tests                 |
| View Controller Tests | ~40   | MVC controller tests                 |
| Integration Tests     | ~15   | WebApplicationFactory tests          |

---

## Development Workflow

### 1. Create a Feature Branch

```powershell
git checkout -b feature/your-feature-name
```

### 2. Make Changes

- Follow existing code patterns
- Add XML documentation to public methods
- Write tests for new functionality

### 3. Run Tests Before Committing

```powershell
dotnet build
dotnet test
```

### 4. Commit and Push

```powershell
git add .
git commit -m "feat: your feature description"
git push origin feature/your-feature-name
```

---

## Docker Setup

### Build and Run with Docker

```powershell
# Build the image
docker build -t community-website .

# Run the container
docker run -p 5000:80 community-website
```

### Using Docker Compose

```powershell
# Build and start all services
docker-compose up --build

# Run in background
docker-compose up -d

# Stop services
docker-compose down
```

---

## Project Walkthrough

This section provides a guided tour of the project architecture for code reviews and walkthroughs.

### 📁 Solution Structure

```
CommunityWebsite.sln
├── src/
│   ├── CommunityWebsite.Core/     # Business logic, models, services
│   └── CommunityWebsite.Web/      # Web API + MVC controllers, views
└── tests/
    └── CommunityWebsite.Tests/    # Unit and integration tests
```

### 🏗️ Architecture Highlights

#### 1. **Clean Architecture**

- Core project has no dependencies on Web project
- Business logic isolated from presentation concerns

#### 2. **Repository Pattern** (`Repositories/`)

- Generic repository with common CRUD operations
- Specialized repositories for complex queries
- Interface-based for testability

#### 3. **Service Layer** (`Services/`)

- Business logic encapsulated in services
- Returns `Result<T>` for consistent error handling
- All services have interfaces for DI

#### 4. **Controller Inheritance** (`Controllers/Base/`)

- `ApiControllerBase` - Common API functionality
- `ViewControllerBase` - Common MVC functionality
- Reduces code duplication, demonstrates inheritance

#### 5. **Specification Pattern** (`Specifications/`)

- Encapsulates business rules for validation
- Reusable across services

### 🔑 Key Files to Review

| File                         | Purpose                               |
| ---------------------------- | ------------------------------------- |
| `Program.cs`                 | Application startup, DI configuration |
| `CommunityDbContext.cs`      | EF Core configuration, seed data      |
| `GenericRepository.cs`       | Base repository implementation        |
| `Result.cs` / `Result<T>.cs` | Result pattern for error handling     |
| `ApiControllerBase.cs`       | Base class for API controllers        |
| `ViewControllerBase.cs`      | Base class for MVC controllers        |
| `ARCHITECTURE.md`            | Detailed architecture documentation   |
| `API_CONVENTIONS.md`         | API design guidelines                 |
| `SOLID.md`                   | SOLID principles implementation       |

### 🧪 Testing Strategy

```
tests/
├── Controllers/          # API and MVC controller tests
├── Services/             # Service layer unit tests
├── Repositories/         # Repository tests
├── Models/               # Model validation tests
└── Fixtures/             # Test fixtures and helpers
```

### 📊 Design Patterns Used

1. **Repository Pattern** - Data access abstraction
2. **Service Layer** - Business logic encapsulation
3. **Result Pattern** - Consistent error handling
4. **Specification Pattern** - Business rule validation
5. **Dependency Injection** - Loose coupling
6. **Base Controller Pattern** - Code reuse

---

## Troubleshooting

### Common Issues

#### Port Already in Use

```powershell
# Find process using port 5000
netstat -ano | findstr :5000

# Kill the process
taskkill /PID <process-id> /F
```

#### Database Connection Failed

- Ensure connection string is correct in `appsettings.json`
- For development, use in-memory database (default)

#### Tests Failing

```powershell
# Clean and rebuild
dotnet clean
dotnet build
dotnet test
```

---

## Additional Resources

- **Architecture Details**: See `ARCHITECTURE.md`
- **API Documentation**: See `API_CONVENTIONS.md`
- **SOLID Principles**: See `SOLID.md`
- **Performance Notes**: See `PERFORMANCE.md`
- **Development Guide**: See `DEVELOPMENT.md`

---

## Need Help?

- Check the documentation files in the project root
- Review the test files for usage examples
- Examine the Swagger UI for API exploration

Happy coding! 🚀
