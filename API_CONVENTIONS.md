# API Design Conventions

## RESTful Endpoints

### URL Structure

```
/api/{resource}                 # Collection
/api/{resource}/{id}            # Single item
/api/{resource}/{id}/{relation} # Related resources
```

### HTTP Methods

| Method | Purpose                  | Example                    | Success Code   |
| ------ | ------------------------ | -------------------------- | -------------- |
| GET    | Retrieve resources       | `GET /api/posts`           | 200 OK         |
| GET    | Retrieve single resource | `GET /api/posts/1`         | 200 OK         |
| GET    | Get user's posts         | `GET /api/posts/user/{id}` | 200 OK         |
| POST   | Create new resource      | `POST /api/posts`          | 201 Created    |
| PUT    | Update entire resource   | `PUT /api/posts/1`         | 200 OK         |
| PATCH  | Partial update           | `PATCH /api/posts/1`       | 200 OK         |
| DELETE | Remove resource          | `DELETE /api/posts/1`      | 204 No Content |

### Response Formats

#### Success Response

```json
{
  "id": 1,
  "title": "Post Title",
  "content": "Post content...",
  "createdAt": "2025-12-03T10:30:00Z"
}
```

#### Error Response

```json
{
  "error": "Validation failed",
  "errors": ["Title is required", "Content must be at least 10 characters"],
  "timestamp": "2025-12-03T10:30:00Z"
}
```

#### Collection Response with Pagination

```json
{
  "items": [
    {
      "id": 1,
      "title": "Post Title",
      "content": "Post content...",
      "createdAt": "2025-12-03T10:30:00Z"
    }
  ],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 10
}
```

This pagination structure is implemented via the `PagedResult<T>` class:

```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
```

All list endpoints support pagination with query parameters:

- `pageNumber` - Current page (1-based, default: 1)
- `pageSize` - Items per page (default: 20, max: 100)

## Authentication

### JWT Bearer Token

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Auth Endpoints

```
POST /api/auth/register     # Create new account
POST /api/auth/login        # Authenticate and get token
POST /api/auth/refresh      # Refresh expired token
GET  /api/auth/me           # Get current user info
```

## API Versioning (Future)

When API versioning is needed:

```
/api/v1/posts
/api/v2/posts
```

Or via header:

```http
Api-Version: 1.0
```

## Complete Endpoint Reference

All implemented endpoints follow the conventions above:

### Authentication (3 endpoints)

- `POST /api/auth/register` - 201 Created
- `POST /api/auth/login` - 200 OK
- `GET /api/auth/me` - 200 OK (requires auth)

### Posts (8 endpoints)

- `GET /api/posts/{id}` - 200 OK
- `GET /api/posts/featured` - 200 OK (paginated)
- `GET /api/posts/category/{category}` - 200 OK (paginated)
- `GET /api/posts/search` - 200 OK (paginated)
- `GET /api/posts/user/{userId}` - 200 OK (paginated)
- `POST /api/posts` - 201 Created (requires auth)
- `PUT /api/posts/{id}` - 200 OK (requires ownership)
- `DELETE /api/posts/{id}` - 204 No Content (requires ownership)

### Comments (5 endpoints)

- `GET /api/posts/{postId}/comments` - 200 OK (paginated)
- `GET /api/comments/{id}` - 200 OK
- `POST /api/posts/{postId}/comments` - 201 Created (requires auth)
- `PUT /api/comments/{id}` - 200 OK (requires ownership)
- `DELETE /api/comments/{id}` - 204 No Content (requires ownership)

### Events (6 endpoints)

- `GET /api/events/upcoming` - 200 OK (paginated)
- `GET /api/events/past` - 200 OK (paginated)
- `GET /api/events/{id}` - 200 OK
- `POST /api/events` - 201 Created (requires auth)
- `PUT /api/events/{id}` - 200 OK (requires ownership)
- `DELETE /api/events/{id}` - 204 No Content (requires ownership)

### Users (5 endpoints)

- `GET /api/users` - 200 OK (paginated)
- `GET /api/users/{id}` - 200 OK
- `POST /api/users` - 201 Created
- `PUT /api/users/{id}` - 200 OK (requires auth)
- `DELETE /api/users/{id}` - 204 No Content (requires ownership)

### Roles (5 endpoints)

- `GET /api/roles` - 200 OK
- `GET /api/roles/{id}` - 200 OK
- `POST /api/roles` - 201 Created (admin only)
- `POST /api/roles/{roleId}/users/{userId}` - 204 No Content (admin only)
- `DELETE /api/roles/{roleId}/users/{userId}` - 204 No Content (admin only)

### Admin (4 endpoints)

- `GET /api/admin/users` - 200 OK (admin only, paginated)
- `GET /api/admin/users/{id}` - 200 OK (admin only)
- `PUT /api/admin/users/{id}` - 200 OK (admin only)
- `DELETE /api/admin/users/{id}` - 204 No Content (admin only)

**Total: 36+ endpoints fully implemented and documented**

---

## Status Codes

| Code | Meaning               | When to Use                               |
| ---- | --------------------- | ----------------------------------------- |
| 200  | OK                    | Successful GET, PUT, PATCH                |
| 201  | Created               | Successful POST with new resource         |
| 204  | No Content            | Successful DELETE                         |
| 400  | Bad Request           | Validation errors, malformed request      |
| 401  | Unauthorized          | Missing or invalid authentication         |
| 403  | Forbidden             | Authenticated but not authorized          |
| 404  | Not Found             | Resource doesn't exist                    |
| 409  | Conflict              | Duplicate resource (e.g., username taken) |
| 422  | Unprocessable Entity  | Semantic validation errors                |
| 500  | Internal Server Error | Unexpected server error                   |

## Swagger Documentation

Interactive API documentation available at:

- Development: `https://localhost:7000/swagger`
- Docker: `http://localhost:5000/swagger`

Features:

- Visual endpoint explorer
- Request/response schemas
- Try-it-out functionality with response display
- Authentication token management (JWT Bearer)
- OpenAPI v3.0 specification
- Automatic documentation from code attributes

Each endpoint includes:

- Summary and detailed description
- Request body schema with example values
- Response schema with example values
- Required authentication and authorization info
- HTTP status codes and error responses
- Query parameter documentation
- Path parameter documentation

See `API_ENDPOINTS.md` for complete endpoint reference guide with curl examples.
