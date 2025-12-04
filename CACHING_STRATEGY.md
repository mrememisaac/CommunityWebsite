# ASP.NET Core Caching Strategy

## Overview

This document outlines the caching implementation strategy for the Community Website application. We implement a multi-layered caching approach using ASP.NET Core's built-in features for optimal performance.

## Caching Types & Implementation

### 1. **In-Memory Cache** (IMemoryCache)

**When to use:** Data that is:

- Reference/lookup data (roles, static content)
- Frequently accessed and changes infrequently
- Expensive to compute/retrieve
- Small in size

**Benefits:**

- Zero external dependencies
- Fastest access (no serialization)
- Simple expiration policies
- Perfect for single-server deployments

#### Best Candidates:

- **Roles** - Accessed on every auth, rarely changes
- **Featured Posts** - Expensive to compute, changes hourly
- **Category/Tag Lists** - Reference data
- **User Statistics** - Derived computations

**Example Usage:**

```csharp
// Get or create cached data
var roles = await _cache.GetOrCreateAsync("all_roles", async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
    entry.SlidingExpiration = TimeSpan.FromMinutes(30);
    return await _roleRepository.GetAllAsync();
});
```

**Cache Keys Convention:**

- `all_roles` - All system roles
- `role_{id}` - Specific role by ID
- `featured_posts` - Featured posts list
- `post_stats_{userId}` - User post statistics
- `user_roles_{userId}` - User's assigned roles

### 2. **Distributed Cache** (IDistributedCache)

**When to use:** Data that needs:

- Sharing across multiple servers
- Persistence across app restarts
- Session-like expiration
- Distributed environments (Azure, load-balanced)

**Implementation Options:**

- Redis (recommended for production)
- SQL Server Cache
- NCache

**Cache Keys Convention:**

- All keys prefixed with environment: `prod_`, `dev_`, `test_`
- Format: `{environment}_{entity}_{identifier}`

### 3. **Response Caching** (HTTP Caching)

**When to use:** For GET endpoints that:

- Return the same data for multiple requests
- Don't depend on user identity
- Should be cacheable by browsers/CDNs
- Have stable URLs

**HTTP Headers:**

- `Cache-Control: public, max-age=300` - 5 minutes
- `ETag` - For conditional requests
- `Last-Modified` - For time-based validation

### 4. **Query Result Caching** (EF Core Compiled Queries)

**When to use:** For frequently executed queries

- Same parameters frequently used
- Reduces query compilation overhead
- Thread-safe and precompiled

---

## Implementation Plan by Feature

### Phase 1: Critical Path (Immediate)

#### 1.1 Role Caching

**Location:** `RoleService.cs` and `AdminUserService.cs`

**Caching Points:**

- `GetAllRolesAsync()` - Cache for 1 hour with sliding 30-minute expiration
- `GetRoleByNameAsync(name)` - Cache by role name, 1-hour TTL
- `GetRoleByIdAsync(id)` - Cache by role ID, 1-hour TTL

**Invalidation:**

- On role creation, modification, deletion - clear `all_roles` cache
- Add cache invalidation in role update/delete endpoints

**Benefit:** Roles are accessed on every request (auth middleware), rarely change. Cache reduces DB calls by 95%+.

---

#### 1.2 Featured Posts Caching

**Location:** `PostService.cs` → `GetFeaturedPostsAsync()`

**Caching Points:**

- Featured posts list - Cache for 1 hour
- Include post counts and author info eagerly

**Implementation:**

```csharp
public async Task<Result<IEnumerable<PostSummaryDto>>> GetFeaturedPostsAsync()
{
    const string cacheKey = "featured_posts";

    if (_cache.TryGetValue(cacheKey, out IEnumerable<PostSummaryDto> cachedPosts))
    {
        _logger.LogDebug("Featured posts retrieved from cache");
        return Result<IEnumerable<PostSummaryDto>>.Success(cachedPosts);
    }

    // Compute featured posts
    var posts = await _postRepository.GetFeaturedPostsAsync();
    var dtos = posts.Select(MapToSummaryDto).ToList();

    // Cache for 1 hour with 30-minute sliding expiration
    _cache.Set(cacheKey, dtos, new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        SlidingExpiration = TimeSpan.FromMinutes(30)
    });

    return Result<IEnumerable<PostSummaryDto>>.Success(dtos);
}
```

**Invalidation:**

- When post is created/updated: `_cache.Remove("featured_posts")`
- When post view count changes: Use sliding expiration instead of hard invalidation

**Benefit:** Featured posts load on every page visit, reduces DB query by 90%+.

---

#### 1.3 User Statistics Caching

**Location:** `UserService.cs` and `AdminUserService.cs`

**Caching Points:**

- User post count: Cache as `post_count_{userId}` for 30 minutes
- User comment count: Cache as `comment_count_{userId}` for 30 minutes
- User with roles: Cache as `user_roles_{userId}` for 15 minutes

**Why Separate Caches?**

- Post/comment counts change frequently (users create content)
- Role assignments change less frequently
- Different invalidation triggers

**Invalidation:**

- When user creates post: Remove `post_count_{userId}`, `featured_posts`
- When user creates comment: Remove `comment_count_{userId}`
- When role assigned: Remove `user_roles_{userId}`, `all_roles`

**Benefit:** Profile pages won't need to count posts/comments each load, reduces N+1 queries.

---

### Phase 2: API Optimization (High Priority)

#### 2.1 Response Caching for Public Endpoints

**Location:** API Controllers

**Target Endpoints:**

- `GET /api/posts` - List posts (not user-specific)
- `GET /api/posts/{id}` - Post details
- `GET /api/events` - Events list
- `GET /api/users/{id}` - User profile (public data)

**Implementation:**

```csharp
[HttpGet("{id}")]
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // 5 minutes
public async Task<ActionResult<PostDetailDto>> GetPost(int id)
{
    // Implementation
}
```

**Cache-Control Headers:**

- Public data: `public, max-age=300` (5 minutes)
- User-specific: `private, max-age=60` (1 minute)
- No-cache: Search results (too volatile)

**Benefit:** Browser/CDN caching reduces server requests by 60-80% for repeat visitors.

---

#### 2.2 Admin User Search Optimization

**Location:** `AdminUserService.cs` → `GetAllUsersAsync(pageNumber, pageSize, searchTerm)`

**Current Issue:** Loads all users into memory, then filters

**Caching Strategy:**

- Don't cache full user list (too large, changes frequently)
- Cache role list separately
- Cache user counts: `total_users`, `active_users` (15-minute TTL)

**Implementation:**

- Move search to database query (already optimized in Phase 1 fixes)
- Cache only aggregate stats

**Benefit:** Reduces memory usage by 95% and improves search performance 50%.

---

### Phase 3: Advanced Caching (Medium Priority)

#### 3.1 Distributed Cache Setup (Redis)

**When to Implement:** Multi-server deployment

**Configuration:**

```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379";
    options.InstanceName = $"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}_";
});
```

**Migrating from In-Memory to Distributed:**

- Change `IMemoryCache` to `IDistributedCache`
- Add JSON serialization/deserialization
- Implement consistent cache keys with environment prefixes

**Benefit:** Enables horizontal scaling without cache invalidation issues.

---

#### 3.2 Cache Invalidation Strategy

**Pattern: Write-Through Invalidation**

```csharp
public async Task<Result<PostSummaryDto>> CreatePostAsync(CreatePostRequest request)
{
    // Create post
    var post = await _postRepository.CreateAsync(...);

    // Invalidate related caches
    _cache.Remove("featured_posts");
    _cache.Remove($"post_count_{post.AuthorId}");
    _cache.Remove("recent_posts");

    return Result<PostSummaryDto>.Success(MapToDto(post));
}
```

**Cache Tag Strategy (Future):**

- Tag caches by entity type: `tag:post`, `tag:user`, `tag:role`
- Invalidate all by tag when entity type changes
- Requires custom implementation or Redis extensions

---

#### 3.3 Compiled Query Caching

**Location:** Repository layer

**Example - Frequently Used Queries:**

```csharp
private static readonly Func<CommunityDbContext, int, Task<User>> GetUserWithRolesCompiled =
    EF.CompileAsyncQuery((CommunityDbContext db, int userId) =>
        db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefault(u => u.Id == userId));

public async Task<User> GetUserWithRolesAsync(int userId)
{
    return await GetUserWithRolesCompiled(this, userId);
}
```

**Queries to Compile:**

- `GetUserWithRolesAsync`
- `GetPostWithCommentsAsync`
- `GetRoleByNameAsync`
- `GetAllRolesAsync`

**Benefit:** Query compilation cached, reduces CPU by 15-20% for repeated queries.

---

## Caching Configuration

### In-Memory Cache Settings

| Entity         | TTL    | Sliding Expiration | Max Size |
| -------------- | ------ | ------------------ | -------- |
| Roles          | 1 hour | 30 min             | 1 MB     |
| Featured Posts | 1 hour | 30 min             | 10 MB    |
| User Stats     | 30 min | 15 min             | 50 MB    |
| Search Results | 5 min  | None               | 100 MB   |
| Post Details   | 1 hour | None               | 200 MB   |

### Invalidation Triggers

| Event                | Caches to Invalidate                                    |
| -------------------- | ------------------------------------------------------- |
| Post Created         | `featured_posts`, `post_count_{userId}`, `recent_posts` |
| Post Updated         | `post_{id}`, `featured_posts`                           |
| Role Assigned        | `user_roles_{userId}`, `all_roles`                      |
| Role Created         | `all_roles`, `role_{id}`                                |
| User Comment Created | `comment_count_{userId}`, `post_{postId}`               |
| Featured Post Added  | `featured_posts`                                        |

---

## Performance Targets

### With Caching Implementation:

- **Home Page Load:** 200ms → 50ms (75% improvement)
- **Post Details:** 300ms → 80ms (73% improvement)
- **User List (Admin):** 500ms → 150ms (70% improvement)
- **Auth Checks:** 100ms → 5ms (95% improvement)
- **Database Load:** Reduced by 80% for read-heavy operations

### Cache Hit Rates:

- Roles: >95% (stable data)
- Featured Posts: >90% (hourly updates)
- User Stats: >70% (changes with user activity)
- Search Results: >50% (user-dependent)

---

## Monitoring & Debugging

### Cache Statistics to Track:

- Hit/Miss Ratio by key
- Cache size by entity type
- Eviction count per hour
- Average entry lifetime

### Logging Recommendations:

```csharp
_logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
_logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);
_logger.LogInformation("Cache invalidated for key: {CacheKey}", cacheKey);
```

### Enable Cache Debugging:

```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    services.AddLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Debug);
    });
}
```

---

## Testing Cache Implementation

### Unit Tests:

- Test cache hit/miss scenarios
- Test expiration policies
- Test cache invalidation triggers
- Test fallback on cache failures

### Load Tests:

- Measure cache hit ratio under load
- Verify memory usage doesn't exceed limits
- Test cache invalidation performance

### Integration Tests:

- Test cache with actual data
- Verify data consistency after invalidation
- Test distributed cache scenarios

---

## Summary: Quick Implementation Checklist

### Week 1: Critical (Phase 1)

- [ ] Add `IMemoryCache` to Program.cs
- [ ] Implement role caching in `RoleService`
- [ ] Implement featured posts caching in `PostService`
- [ ] Add cache invalidation to update/create endpoints
- [ ] Test cache hit rates

### Week 2: Important (Phase 2)

- [ ] Add response caching headers to API endpoints
- [ ] Implement user statistics caching
- [ ] Optimize admin user search queries
- [ ] Add cache statistics logging

### Week 3+: Advanced (Phase 3)

- [ ] Setup Redis for distributed cache
- [ ] Implement cache tag strategy
- [ ] Compile frequently-used LINQ queries
- [ ] Add cache warming on startup

---

## References

- [ASP.NET Core Caching Documentation](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory)
- [Response Caching Middleware](https://docs.microsoft.com/en-us/aspnet/core/performance/response-caching)
- [Distributed Caching with Redis](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed)
- [Entity Framework Core Compiled Queries](https://docs.microsoft.com/en-us/ef/core/performance/advanced-performance-topics#compiled-queries)
