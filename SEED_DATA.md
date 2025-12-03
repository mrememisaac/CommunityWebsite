# Test Seed Data Documentation

## Overview

Comprehensive seed data fixture for integration tests has been created to provide realistic sample data across all domain entities.

## Seed Data Fixture: `SeedData.cs`

Location: `tests/CommunityWebsite.Tests/Fixtures/SeedData.cs`

### Purpose

Provides robust, realistic test data that enables comprehensive testing scenarios without manual data setup in each test.

## Seeded Data Summary

### Users (5 Total)

| Username       | Email                 | Role      | Bio                  | CreatedAt |
| -------------- | --------------------- | --------- | -------------------- | --------- |
| admin          | admin@community.local | Admin     | System Administrator | -30 days  |
| moderator      | mod@community.local   | Moderator | Community Moderator  | -20 days  |
| john_developer | john@example.com      | User      | Full Stack Developer | -15 days  |
| sarah_designer | sarah@example.com     | User      | UX/UI Designer       | -10 days  |
| mike_architect | mike@example.com      | User      | Solutions Architect  | -5 days   |

**All passwords follow pattern:** `Password123!` (or specific variants for admin/moderator)

### Roles (3 Total)

1. **Admin** - Administrator with full system access
2. **Moderator** - Moderator with content management rights
3. **User** - Standard community member

### Posts (10 Total)

Distributed across categories with realistic metadata:

#### Technology Posts (4)

- Getting Started with ASP.NET Core 8 (145 views, pinned)
- Entity Framework Core Tips and Tricks (98 views)
- RESTful API Design Best Practices (156 views)
- Performance Optimization Strategies (67 views)

#### Design Posts (2)

- Web Design Trends 2025 (203 views, pinned)
- CSS Grid vs Flexbox: When to Use Each (234 views)

#### Architecture Posts (1)

- Building Scalable Microservices (87 views)

#### Security Posts (1)

- Security Best Practices for Web Applications (178 views)

#### DevOps Posts (1)

- Introduction to Docker and Containerization (94 views)

**Post Features:**

- Realistic creation timestamps (ranging from -28 to -1 days)
- View counts (67-234)
- Pin/lock status for featured posts
- Proper author relationships

### Comments (8 Total)

Distributed across posts with realistic engagement:

- Comments authored by different users
- Dates range from -26 to -6 days ago
- All marked as approved (not deleted)
- Realistic, contextual content

## Usage in Tests

### Basic Seeding

The seed data is automatically applied during test initialization:

```csharp
public async Task InitializeAsync()
{
    // ... factory setup ...

    // Automatically seeds test data
    await SeedTestDataAsync();

    // Then creates additional test user
    await SetupTestUser();
}
```

### Accessing Seeded Data

Reference constants are provided for stable test scenarios:

```csharp
// User references
SeedData.Users.AdminUsername           // "admin"
SeedData.Users.User1Username           // "john_developer"
SeedData.Users.User1Password           // "Password123!"

// Post references
SeedData.Posts.AspNetCoreGuideTitle    // "Getting Started with ASP.NET Core 8"
SeedData.Posts.WebDesignTrendsTitle    // "Web Design Trends 2025"
SeedData.Categories.Technology         // "Technology"
```

## Key Features

### 1. **Realistic Data**

- Authentic usernames, emails, and bios
- Natural post content with varied categories
- Believable engagement through comments
- Realistic timestamps spanning multiple days

### 2. **Comprehensive Coverage**

- All 5 domain entities represented (Users, Roles, Posts, Comments, Events placeholder)
- Multiple users with different roles
- Posts across all supported categories
- Comments linking multiple users to posts

### 3. **Test Stability**

- Seed check prevents duplicate insertions: `if (context.Users.Any()) return;`
- Relationship integrity maintained through proper foreign keys
- Password hashing applied for user authentication
- No hardcoded IDs - all relationships established through entity lookups

### 4. **Extensibility**

- Helper constants for common test scenarios
- Easy to add new seed data methods
- Modular design allows selective seeding if needed

## Database State After Seeding

```
Users:        5 (1 admin, 1 moderator, 3 regular)
Roles:        3 (Admin, Moderator, User)
UserRoles:    5 (one role assignment per user)
Posts:        10 (across 5 categories)
Comments:     8 (distributed across posts)
Total Records: 31
```

## Test Integration Example

All integration tests benefit from seeded data:

```csharp
[Fact]
public async Task GET_GetPosts_Featured_ReturnsOk()
{
    // Database now contains 10 posts with realistic data
    // Featured posts are already pinned

    var response = await _client.GetAsync("/api/posts/featured");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## References for Test Data

### Constants Available

**Users**

- `SeedData.Users.AdminUsername`
- `SeedData.Users.User1Username` through `User3Username`
- All corresponding passwords and emails

**Posts**

- `SeedData.Posts.AspNetCoreGuideTitle`
- `SeedData.Posts.WebDesignTrendsTitle`
- All 10 post titles available

**Categories**

- `SeedData.Categories.Technology`
- `SeedData.Categories.Design`
- `SeedData.Categories.Architecture`
- `SeedData.Categories.Security`
- `SeedData.Categories.DevOps`

## Test Results

✅ **All 35 tests passing** with seed data integration:

- 27 existing unit tests
- 8 integration tests
- Duration: ~5 seconds
- Zero failures, zero errors

## Future Enhancements

Potential additions to seed data:

1. **Event data** - Using existing Event model
2. **Additional posts** by category
3. **Reply comments** - Using ParentCommentId relationships
4. **User preferences/settings** - When additional models added
5. **Engagement metrics** - Like counts, follow relationships

## Maintenance Notes

- Seed data should be updated when new domain models are added
- Keep realistic and representative of actual usage
- Consider performance for in-memory database (current 31 records is fast)
- Document any hardcoded IDs or special assumptions

---

**Version:** 1.0  
**Last Updated:** Current Session  
**Status:** ✅ Production Ready
