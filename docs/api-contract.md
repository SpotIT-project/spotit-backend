# SpotIt — API Contract

Base URL: `https://localhost:7xxx/api`  
Auth: **HttpOnly cookies** — no Authorization header needed. Cookies are set automatically on login/refresh.  
Swagger UI: `/swagger` (development only)

Legend: ✅ Built · 🔜 Planned (Phase 7)

---

## Authentication

### POST /auth/register ✅
Register a new citizen account.

**Request body**
```json
{
  "email": "sava@example.com",
  "password": "Strong@123",
  "fullName": "Sava Alexandru",
  "city": "Oradea"
}
```

**Response 200**
```json
{ "message": "Registration successful" }
```

**Response 400**
```json
[{ "code": "DuplicateEmail", "description": "Email already taken." }]
```

---

### POST /auth/login ✅
Login and receive HttpOnly cookies (`accessToken`, `refreshToken`).

**Request body**
```json
{
  "email": "sava@example.com",
  "password": "Strong@123"
}
```

**Response 200**
```json
{
  "userId": "3fa85f64-...",
  "email": "sava@example.com",
  "fullName": "Sava Alexandru",
  "role": "Citizen"
}
```

**Response 401** — Invalid credentials

---

### GET /auth/me ✅
Get the profile of the currently authenticated user. Requires a valid `accessToken` cookie.

**Response 200**
```json
{
  "id": "3fa85f64-...",
  "email": "sava@example.com",
  "fullName": "Sava Alexandru",
  "city": "Oradea",
  "role": "Citizen"
}
```

**Response 401** — Missing or expired token

---

### POST /auth/refresh ✅
Silently rotate tokens using the `refreshToken` cookie.  
No request body. Reads cookies automatically.

**Response 200**
```json
{ "message": "Token refreshed" }
```

**Response 401** — Missing, used, revoked, or expired refresh token

---

### POST /auth/logout ✅
Clear auth cookies. No request body.

**Response 200**
```json
{ "message": "Logged out" }
```

---

## Posts

All `/posts` endpoints require authentication (valid `accessToken` cookie).

### GET /posts ✅
Get a paginated, filtered list of posts.

**Query params**
| Param | Type | Required | Description |
|---|---|---|---|
| page | int | yes | Page number (starts at 1) |
| pageSize | int | yes | Items per page |
| categoryId | int | no | Filter by category |
| status | string | no | `Pending`, `UnderReview`, `InProgress`, `Resolved`, `Rejected` |
| dateFrom | datetime | no | ISO 8601 — filter by created date |
| dateTo | datetime | no | ISO 8601 |
| sortByPopularity | bool | no | `true` = sort by likes desc; `false` = sort by date desc |
| search | string | no | Case-insensitive text search on title and description |

**Response 200**
```json
{
  "items": [
    {
      "id": "3fa85f64-...",
      "title": "Pothole on Str. Republicii",
      "description": "Large pothole near intersection...",
      "categoryId": 1,
      "categoryName": "Infrastructure",
      "status": "Pending",
      "isAnonymous": false,
      "authorId": "user-guid",
      "authorName": "Sava Alexandru",
      "likesCount": 12,
      "photoUrl": "/uploads/3fa85f64-....jpg",
      "createdAt": "2026-04-21T10:00:00Z"
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

> If `isAnonymous` is `true`, `authorId` and `authorName` will be `null`.

---

### GET /posts/{id} ✅
Get a single post with full details.

**Response 200** — Same shape as a single item above  
**Response 404**
```json
{ "error": "Post with id '3fa85f64-...' was not found." }
```

---

### POST /posts ✅
Create a new post. `authorId` is taken from the JWT — never sent by the client.

**Request body**
```json
{
  "title": "Broken street light on Calea Aradului",
  "description": "The light has been out for two weeks...",
  "categoryId": 2,
  "isAnonymous": false
}
```

**Validation rules**
- `title`: required, max 200 chars
- `description`: required, max 4000 chars
- `categoryId`: must be > 0

**Response 201** — Empty body  
**Response 400**
```json
{ "errors": ["Title must not be empty.", "Description must not exceed 4000 characters."] }
```

---

### DELETE /posts/{id} ✅
Delete a post. Only the post author can delete their own post.

No request body.

**Response 204** — No content  
**Response 401** — Not authenticated  
**Response 403** — Caller is not the post author  
**Response 404** — Post not found

---

### PATCH /posts/{id}/status ✅
Update the status of a post. Roles: `CityHallEmployee`, `Admin`.

**Request body**
```json
{
  "newStatus": "InProgress",
  "note": "Assigned to road maintenance team."
}
```

**Validation rules**
- `newStatus`: required, valid `PostStatus` value
- `note`: optional, max 1000 chars

**Response 204** — No content  
**Response 400** — Validation error  
**Response 403** — Caller is not Employee or Admin  
**Response 404** — Post not found

---

### POST /posts/{id}/photo ✅
Upload or replace the photo for a post. Only the post author can do this.

**Content-Type:** `multipart/form-data`

| Field | Type | Required |
|---|---|---|
| photo | file | yes |

**Validation rules**
- Allowed extensions: `.jpg`, `.jpeg`, `.png`, `.webp`
- Max size: 5 MB

**Response 200**
```json
{ "url": "/uploads/3fa85f64-....jpg" }
```

**Response 400** — Validation error (wrong type, too large)  
**Response 403** — Caller is not the post author  
**Response 404** — Post not found

---

### POST /posts/{id}/likes ✅
Like a post. Duplicate likes are rejected at DB level.

No request body.

**Response 204** — No content  
**Response 409** — Already liked

---

### DELETE /posts/{id}/likes ✅
Remove a like from a post.

No request body.

**Response 204** — No content  
**Response 404** — Like not found

---

## Comments

### GET /posts/{id}/comments ✅
Get comments for a post, ordered by date ascending.

**Response 200**
```json
[
  {
    "id": "3fa85f64-...",
    "content": "This needs to be fixed urgently.",
    "authorName": "Ion Popescu",
    "isOfficial": false,
    "createdAt": "2026-04-21T11:00:00Z"
  }
]
```

> Official comments (from city hall) have `isOfficial: true` and are visually distinguished in the UI.

---

### POST /posts/{id}/comments ✅
Add a comment. Roles: any authenticated user. Set `isOfficial: true` only for Employees/Admins.

**Request body**
```json
{
  "content": "We have logged this issue.",
  "isOfficial": true
}
```

**Validation rules**
- `content`: required, max 2000 chars
- `isOfficial`: ignored if caller is `Citizen`

**Response 201** — Empty body  
**Response 400** — Validation error

---

## Categories

### GET /categories ✅
Get all categories. Used to populate dropdowns in the frontend.

No auth required (public endpoint).

**Response 200**
```json
[
  { "id": 1, "name": "Infrastructure" },
  { "id": 2, "name": "Public Transport" },
  { "id": 3, "name": "Parks & Recreation" },
  { "id": 4, "name": "Safety" },
  { "id": 5, "name": "Environment" }
]
```

---

## Analytics (Admin only)

### GET /admin/analytics/by-status ✅
Count of posts grouped by status.

**Response 200**
```json
[
  { "status": "Pending", "count": 34 },
  { "status": "InProgress", "count": 12 },
  { "status": "Resolved", "count": 87 }
]
```

---

### GET /admin/analytics/top-categories ✅
Top 5 categories by post count.

**Response 200**
```json
[
  { "categoryName": "Infrastructure", "postCount": 54 },
  { "categoryName": "Safety", "postCount": 31 }
]
```

---

## Roles

| Role | Registered via |
|---|---|
| `Citizen` | Assigned on register |
| `CityHallEmployee` | Assigned by Admin |
| `Admin` | Seeded at startup |

---

## Global Error Responses

| Status | Trigger |
|---|---|
| 400 | Validation failure — body: `{ "errors": ["..."] }` |
| 401 | Missing or expired access token |
| 403 | Authenticated but wrong role, or not the resource owner |
| 404 | Resource not found — body: `{ "error": "..." }` |
| 409 | Conflict — e.g. duplicate like — body: `{ "error": "..." }` |
| 500 | Unhandled server error — body: `{ "error": "..." }` |
