# Complete API Endpoints Reference

**Last Updated:** December 2024  
**Base URL:** `http://localhost:5000/api`  
**Documentation:** Available at `http://localhost:5000/swagger` (Swagger UI)

---

## 📚 Table of Contents

1. [Authentication Endpoints](#authentication-endpoints)
2. [Posts Endpoints](#posts-endpoints)
3. [Comments Endpoints](#comments-endpoints)
4. [Events Endpoints](#events-endpoints)
5. [Users Endpoints](#users-endpoints)
6. [Roles Endpoints](#roles-endpoints)
7. [Admin Endpoints](#admin-endpoints)

---

## Authentication Endpoints

### POST /api/auth/register

**Description:** Register a new user account

**Request:**

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response (201 Created):**

```json
{
  "id": 1,
  "username": "john_doe",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "createdAt": "2024-12-03T10:30:00Z"
}
```

**Status Codes:**

- 201 Created - Registration successful
- 400 Bad Request - Validation error
- 409 Conflict - User already exists

---

### POST /api/auth/login

**Description:** Authenticate and receive JWT token

**Request:**

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**

```json
{
  "id": 1,
  "username": "john_doe",
  "email": "john@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

**Status Codes:**

- 200 OK - Login successful
- 400 Bad Request - Invalid credentials
- 401 Unauthorized - User inactive

---

### GET /api/auth/me

**Description:** Get current authenticated user

**Request:**

```http
GET /api/auth/me
Authorization: Bearer {token}
```

**Response (200 OK):**

```json
{
  "id": 1,
  "username": "john_doe",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "bio": "Software developer",
  "createdAt": "2024-12-01T08:00:00Z"
}
```

**Status Codes:**

- 200 OK - User found
- 401 Unauthorized - Invalid token

---

## Posts Endpoints

### GET /api/posts/{id}

**Description:** Get a specific post with all comments

**Request:**

```http
GET /api/posts/1
```

**Response (200 OK):**

```json
{
  "id": 1,
  "title": "Getting Started with ASP.NET Core",
  "content": "A comprehensive guide...",
  "category": "Technology",
  "author": {
    "id": 1,
    "username": "john_doe",
    "firstName": "John"
  },
  "comments": [
    {
      "id": 1,
      "content": "Great post!",
      "author": {
        "id": 2,
        "username": "jane_doe"
      },
      "createdAt": "2024-12-02T14:30:00Z"
    }
  ],
  "viewCount": 125,
  "createdAt": "2024-12-01T10:30:00Z",
  "updatedAt": "2024-12-02T15:00:00Z"
}
```

**Cache:** 5 minutes  
**Status Codes:**

- 200 OK - Post found
- 404 Not Found - Post doesn't exist

---

### GET /api/posts/featured

**Description:** Get featured/trending posts with pagination

**Request:**

```http
GET /api/posts/featured?pageNumber=1&pageSize=20
```

**Response (200 OK):**

```json
{
  "items": [
    {
      "id": 1,
      "title": "Post Title",
      "content": "Summary...",
      "author": {
        "id": 1,
        "username": "john_doe"
      },
      "viewCount": 250,
      "commentCount": 5,
      "createdAt": "2024-12-01T10:30:00Z"
    }
  ],
  "totalCount": 45,
  "pageNumber": 1,
  "pageSize": 20
}
```

**Cache:** 1 hour  
**Status Codes:**

- 200 OK - Success

---

### GET /api/posts/category/{category}

**Description:** Get posts by category with pagination

**Request:**

```http
GET /api/posts/category/Technology?pageNumber=1&pageSize=20
```

**Response:** Similar to `/featured` with paginated results  
**Cache:** 5 minutes

---

### GET /api/posts/search

**Description:** Search posts by query term

**Request:**

```http
GET /api/posts/search?q=asp.net
```

**Parameters:**

- `q` (required) - Search query

**Response:** Paginated list of matching posts  
**Cache:** 5 minutes

---

### GET /api/posts/user/{userId}

**Description:** Get all posts by a specific user

**Request:**

```http
GET /api/posts/user/1?pageNumber=1&pageSize=20
```

**Response:** Paginated list of user's posts

---

### POST /api/posts

**Description:** Create a new post (requires authentication)

**Request:**

```http
POST /api/posts
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "My New Post",
  "content": "Post content here...",
  "category": "Technology",
  "authorId": 1
}
```

**Response (201 Created):**

```json
{
  "id": 42,
  "title": "My New Post",
  "content": "Post content here...",
  "category": "Technology",
  "author": {...},
  "createdAt": "2024-12-03T10:30:00Z"
}
```

**Status Codes:**

- 201 Created - Post created
- 400 Bad Request - Validation error
- 401 Unauthorized - Not authenticated

---

### PUT /api/posts/{id}

**Description:** Update a post (requires ownership)

**Request:**

```http
PUT /api/posts/1
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Updated Title",
  "content": "Updated content...",
  "category": "Technology"
}
```

**Response (200 OK):** Updated post object

**Status Codes:**

- 200 OK - Updated successfully
- 400 Bad Request - Validation error
- 401 Unauthorized - Not authenticated
- 403 Forbidden - Not owner
- 404 Not Found - Post doesn't exist

---

### DELETE /api/posts/{id}

**Description:** Delete a post (soft delete, requires ownership)

**Request:**

```http
DELETE /api/posts/1
Authorization: Bearer {token}
```

**Response:** 204 No Content

**Status Codes:**

- 204 No Content - Deleted successfully
- 401 Unauthorized - Not authenticated
- 403 Forbidden - Not owner
- 404 Not Found - Post doesn't exist

---

## Comments Endpoints

### GET /api/posts/{postId}/comments

**Description:** Get all comments for a post

**Request:**

```http
GET /api/posts/1/comments?pageNumber=1&pageSize=20
```

**Response (200 OK):**

```json
{
  "items": [
    {
      "id": 1,
      "content": "Great post!",
      "author": {
        "id": 2,
        "username": "jane_doe"
      },
      "createdAt": "2024-12-02T14:30:00Z",
      "updatedAt": "2024-12-02T14:30:00Z"
    }
  ],
  "totalCount": 5,
  "pageNumber": 1,
  "pageSize": 20
}
```

---

### GET /api/comments/{id}

**Description:** Get a specific comment with replies

**Request:**

```http
GET /api/comments/1
```

**Response (200 OK):** Comment with nested replies array

---

### POST /api/posts/{postId}/comments

**Description:** Create a new comment (requires authentication)

**Request:**

```http
POST /api/posts/1/comments
Authorization: Bearer {token}
Content-Type: application/json

{
  "content": "This is a comment",
  "authorId": 2
}
```

**Response (201 Created):** Created comment object

**Status Codes:**

- 201 Created - Comment created
- 400 Bad Request - Validation error
- 401 Unauthorized - Not authenticated
- 404 Not Found - Post doesn't exist

---

### PUT /api/comments/{id}

**Description:** Update a comment (requires ownership)

**Request:**

```http
PUT /api/comments/1
Authorization: Bearer {token}
Content-Type: application/json

{
  "content": "Updated comment"
}
```

**Response (200 OK):** Updated comment

---

### DELETE /api/comments/{id}

**Description:** Delete a comment (soft delete, requires ownership)

**Request:**

```http
DELETE /api/comments/1
Authorization: Bearer {token}
```

**Response:** 204 No Content

---

## Events Endpoints

### GET /api/events/upcoming

**Description:** Get upcoming events

**Request:**

```http
GET /api/events/upcoming?limit=20&pageNumber=1&pageSize=20
```

**Parameters:**

- `limit` (optional) - Number of events to fetch (1-100)
- `pageNumber` (optional) - Page number (default: 1)
- `pageSize` (optional) - Items per page (default: 20)

**Response (200 OK):**

```json
[
  {
    "id": 1,
    "title": "Community Meetup",
    "description": "Monthly community gathering...",
    "startDate": "2024-12-15T18:00:00Z",
    "endDate": "2024-12-15T20:00:00Z",
    "location": "Community Center",
    "organizer": {
      "id": 1,
      "username": "john_doe"
    },
    "registeredCount": 25,
    "createdAt": "2024-12-01T10:00:00Z"
  }
]
```

---

### GET /api/events/past

**Description:** Get past events

**Request:**

```http
GET /api/events/past?pageNumber=1&pageSize=20
```

**Response:** Similar to upcoming events

---

### GET /api/events/{id}

**Description:** Get event details

**Request:**

```http
GET /api/events/1
```

**Response:** Event object with full details

---

### POST /api/events

**Description:** Create a new event (requires authentication)

**Request:**

```http
POST /api/events
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Tech Meetup",
  "description": "Join us for...",
  "startDate": "2024-12-15T18:00:00Z",
  "endDate": "2024-12-15T20:00:00Z",
  "location": "Community Center",
  "organizerId": 1
}
```

**Response (201 Created):** Created event

---

### PUT /api/events/{id}

**Description:** Update an event (requires ownership)

**Response (200 OK):** Updated event

---

### DELETE /api/events/{id}

**Description:** Delete an event (soft delete, requires ownership)

**Response:** 204 No Content

---

## Users Endpoints

### GET /api/users

**Description:** Get all active users (with pagination)

**Request:**

```http
GET /api/users?pageNumber=1&pageSize=20
```

**Response:**

```json
{
  "items": [
    {
      "id": 1,
      "username": "john_doe",
      "firstName": "John",
      "lastName": "Doe",
      "email": "john@example.com",
      "bio": "Developer",
      "createdAt": "2024-12-01T08:00:00Z"
    }
  ],
  "totalCount": 15,
  "pageNumber": 1,
  "pageSize": 20
}
```

---

### GET /api/users/{id}

**Description:** Get user profile

**Request:**

```http
GET /api/users/1
```

**Response:** User profile object

---

### POST /api/users

**Description:** Create a new user

**Request:**

```http
POST /api/users
Content-Type: application/json

{
  "username": "new_user",
  "email": "new@example.com",
  "password": "SecurePassword123!",
  "firstName": "New",
  "lastName": "User"
}
```

**Response (201 Created):** Created user

---

### PUT /api/users/{id}

**Description:** Update user profile

**Request:**

```http
PUT /api/users/1
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "Johnny",
  "lastName": "Doe",
  "bio": "Updated bio"
}
```

**Response (200 OK):** Updated user

---

### DELETE /api/users/{id}

**Description:** Delete/deactivate user (soft delete)

**Response:** 204 No Content

---

## Roles Endpoints

### GET /api/roles

**Description:** Get all roles

**Request:**

```http
GET /api/roles
```

**Response (200 OK):**

```json
[
  {
    "id": 1,
    "name": "Admin"
  },
  {
    "id": 2,
    "name": "Moderator"
  },
  {
    "id": 3,
    "name": "User"
  }
]
```

---

### GET /api/roles/{id}

**Description:** Get role details with assigned users

**Request:**

```http
GET /api/roles/1
```

**Response:** Role with users array

---

### POST /api/roles

**Description:** Create a new role (Admin only)

**Request:**

```http
POST /api/roles
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "name": "Contributor"
}
```

**Response (201 Created):** Created role

---

### POST /api/roles/{roleId}/users/{userId}

**Description:** Assign role to user (Admin only)

**Request:**

```http
POST /api/roles/1/users/5
Authorization: Bearer {admin_token}
```

**Response (204 No Content):** Role assigned

---

### DELETE /api/roles/{roleId}/users/{userId}

**Description:** Remove role from user (Admin only)

**Request:**

```http
DELETE /api/roles/1/users/5
Authorization: Bearer {admin_token}
```

**Response (204 No Content):** Role removed

---

## Admin Endpoints

### GET /api/admin/users

**Description:** Get all users with pagination (Admin only)

**Request:**

```http
GET /api/admin/users?pageNumber=1&pageSize=20
Authorization: Bearer {admin_token}
```

**Response:** Paginated list of all users including inactive

---

### GET /api/admin/users/{id}

**Description:** Get detailed user information (Admin only)

**Request:**

```http
GET /api/admin/users/1
Authorization: Bearer {admin_token}
```

**Response:** User details with role assignments and activity logs

---

### PUT /api/admin/users/{id}

**Description:** Update user details (Admin only)

**Request:**

```http
PUT /api/admin/users/1
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "username": "updated_username",
  "email": "updated@example.com",
  "isActive": true
}
```

**Response (200 OK):** Updated user

---

### DELETE /api/admin/users/{id}

**Description:** Delete user (Admin only, soft delete)

**Request:**

```http
DELETE /api/admin/users/1
Authorization: Bearer {admin_token}
```

**Response:** 204 No Content

---

## Common Response Patterns

### Pagination Structure

All list endpoints return paginated results in this format:

```json
{
  "items": [...],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 20
}
```

**Parameters:**

- `pageNumber` - Current page (1-based)
- `pageSize` - Items per page (default: 20, max: 100)

### Error Response

All error responses follow this format:

```json
{
  "error": "Descriptive error message",
  "errors": ["Field1 is required", "Field2 must be unique"],
  "timestamp": "2024-12-03T10:30:00Z"
}
```

### Status Code Reference

| Code | Meaning       | Use Case                          |
| ---- | ------------- | --------------------------------- |
| 200  | OK            | Successful GET, PUT, PATCH        |
| 201  | Created       | Successful POST (new resource)    |
| 204  | No Content    | Successful DELETE                 |
| 400  | Bad Request   | Validation or malformed request   |
| 401  | Unauthorized  | Missing or invalid authentication |
| 403  | Forbidden     | Insufficient permissions          |
| 404  | Not Found     | Resource doesn't exist            |
| 409  | Conflict      | Duplicate resource                |
| 422  | Unprocessable | Semantic validation error         |
| 500  | Server Error  | Unexpected server error           |

---

## Authentication

### JWT Bearer Token

Include token in all authenticated requests:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Development Credentials

**Admin User:**

- Email: `admin@example.com`
- Password: `AdminPassword123!`
- Role: Admin

---

## Rate Limiting

Currently not rate-limited. Production deployment should implement:

- 60 requests per minute per IP (general)
- 10 requests per minute per IP (auth endpoints)
- 100 requests per minute per authenticated user (general)

---

## Caching

Responses are cached using HTTP cache headers:

| Endpoint       | Cache Duration | Location |
| -------------- | -------------- | -------- |
| Single post    | 5 minutes      | Any      |
| Featured posts | 1 hour         | Any      |
| Category posts | 5 minutes      | Any      |
| User profile   | 5 minutes      | Any      |
| Comments       | Not cached     | -        |
| Events         | Not cached     | -        |

---

## Swagger Documentation

Interactive API documentation is available at:

```
http://localhost:5000/swagger
```

Features:

- Visual endpoint explorer
- Request/response schemas
- Try-it-out functionality
- Authentication token management
