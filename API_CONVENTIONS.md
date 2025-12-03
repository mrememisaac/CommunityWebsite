# API Design Conventions

## RESTful Endpoints

### URL Structure

```
/api/{resource}                 # Collection
/api/{resource}/{id}            # Single item
/api/{resource}/{id}/{relation} # Related resources
```

### HTTP Methods

| Method | Purpose                  | Example               | Success Code   |
| ------ | ------------------------ | --------------------- | -------------- |
| GET    | Retrieve resources       | `GET /api/posts`      | 200 OK         |
| GET    | Retrieve single resource | `GET /api/posts/1`    | 200 OK         |
| POST   | Create new resource      | `POST /api/posts`     | 201 Created    |
| PUT    | Update entire resource   | `PUT /api/posts/1`    | 200 OK         |
| PATCH  | Partial update           | `PATCH /api/posts/1`  | 200 OK         |
| DELETE | Remove resource          | `DELETE /api/posts/1` | 204 No Content |

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

#### Collection Response

```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 100,
    "totalPages": 10
  }
}
```

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

API documentation available at `/swagger` in development mode.

Each endpoint includes:

- Summary and description
- Request/response schemas
- Example values
- Authentication requirements
