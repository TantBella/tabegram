# Instagram-Like MVP — Implementation Plan

## Context

Building a greenfield Instagram-like web app called Tabegram (colorful theme, yellow, orange, pink and green accent, 1960/1970/flowerpower-inspired) as an MVP. The goal is a runnable, testable full-stack app where users can register, log in, upload image posts, view a paginated feed, and like/unlike posts. Comments are out of scope.

---

## Tech Stack

- **Backend:** .NET 10 Minimal Web API (`Web/`), Entity Framework Core + SQLite
- **Frontend:** React + TypeScript (`Web/App/`), Bootstrap 5
- **Auth:** JWT (PBKDF2 password hashing)
- **Images:** Server filesystem, path stored in DB

---

## Directory Structure

### Backend

```
Web/
├── Web.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/           (EF Core generated)
├── Models/
│   ├── User.cs
│   ├── Post.cs
│   └── Like.cs
├── DTOs/
│   ├── Auth/  (RegisterRequest, LoginRequest, AuthResponse)
│   ├── Posts/ (CreatePostRequest, PostResponse, PagedResponse)
│   └── Users/ (UserPostsResponse)
├── Services/
│   ├── IAuthService.cs / AuthService.cs
│   ├── IPostService.cs / PostService.cs
│   └── IImageService.cs / ImageService.cs
├── Endpoints/
│   ├── AuthEndpoints.cs
│   ├── PostEndpoints.cs
│   ├── UserEndpoints.cs
│   └── UploadEndpoints.cs
├── Validation/
│   └── ImageValidator.cs
├── Seeds/
│   └── DbSeeder.cs
└── uploads/   (runtime, gitignored)

Web.Tests/
├── Auth/  (AuthServiceTests.cs, AuthEndpointTests.cs)
├── Posts/ (PostServiceTests.cs, PostEndpointTests.cs)
└── Helpers/ (TestWebAppFactory.cs, TestDbHelper.cs)
```

### Frontend (`Web/App/`)

```
src/
├── main.tsx
├── App.tsx
├── api/          (client.ts, auth.ts, posts.ts, users.ts)
├── auth/         (AuthContext.tsx, ProtectedRoute.tsx)
├── components/
│   ├── layout/   (Navbar.tsx, Layout.tsx)
│   ├── posts/    (PostCard.tsx, PostFeed.tsx, LikeButton.tsx)
│   └── shared/   (LoadingSpinner.tsx, ErrorAlert.tsx, Pagination.tsx)
├── pages/        (LoginPage, RegisterPage, FeedPage, NewPostPage, ProfilePage)
├── hooks/        (useAuth.ts, usePosts.ts, useProfile.ts)
├── types/        (index.ts)
└── styles/       (theme.css)
```

---

## Data Model

All IDs are `Guid`.

```
Users:  id, username (unique), passwordHash, createdAt
Posts:  id, userId, imagePath, description, createdAt
Likes:  id, postId, userId, createdAt  — unique(postId, userId)
```

---

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/auth/register` | No | Register user |
| POST | `/auth/login` | No | Login → JWT |
| GET | `/posts?page=&pageSize=` | No | Paginated feed |
| POST | `/posts` | Yes | Create post (multipart) |
| POST | `/posts/{id}/like` | Yes | Toggle like |
| GET | `/users/{id}/posts` | No | User's posts |
| GET | `/uploads/{filename}` | No | Serve image |

---

## Implementation Order

### Phase 1 — Backend

**Step 1: Project scaffolding**
- `dotnet new webapi -n Web`
- NuGet: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`
- Configure `appsettings.json`: `ConnectionStrings:Default`, `Jwt:Key/Issuer/Audience/ExpiryMinutes`, `Uploads:BasePath`

**Step 2: Data models and DbContext**
- `User`, `Post`, `Like` entities (Guid IDs, `Guid.NewGuid()` defaults, `DateTime.UtcNow`)
- `AppDbContext` with Fluent API: unique index on `Username`, unique index on `(PostId, UserId)` in Likes, cascade delete
- `dotnet ef migrations add InitialCreate && dotnet ef database update`

**Step 3: Auth**
- PBKDF2 via `Rfc2898DeriveBytes` — 310k iterations, SHA-256, 32-byte salt + hash stored as `base64(salt):base64(hash)`
- JWT generation: claims `sub` (userId), `unique_name` (username), signed with `HmacSha256`
- `POST /auth/register` → 201 or 409 on duplicate username
- `POST /auth/login` → returns `{ token, userId, username }` or 401

**Step 4: Image service**
- Validate: allowed MIME types (jpg/png/gif/webp), allowed extensions, max 10 MB
- Save as `{Guid}{ext}` into configured `Uploads:BasePath` directory
- Return filename (not full path)

**Step 5: Post endpoints**
- `GET /posts` — public, paginated, newest-first, EF Core `Select` projection (includes `LikedByCurrentUser` via SQL `EXISTS`)
- `POST /posts` — auth required, multipart: image + description
- `POST /posts/{id}/like` — auth required, toggle (add if absent, remove if present)
- `GET /users/{id}/posts` — public, all posts for user
- `GET /uploads/{filename}` — minimal endpoint serving files from uploads dir

**Step 6: Seed data**
- `DbSeeder.cs` auto-runs on startup if DB empty; creates 2–3 users + 5–10 posts

**Step 7: Program.cs wiring**
- Extension methods: `app.MapAuthEndpoints()`, `app.MapPostEndpoints()`, etc.
- `public partial class Program { }` for test factory

### Phase 2 — Frontend

**Step 8: Scaffolding**
- `npm create vite@latest App -- --template react-ts`
- Install: `axios`, `react-router-dom`, `bootstrap`
- Import order in `main.tsx`: Bootstrap CSS → `theme.css`
- Vite proxy in `vite.config.ts`: forward `/posts`, `/auth`, `/users`, `/uploads` → `http://localhost:5000`

**Step 9: API layer and types**
- `src/types/index.ts`: `User`, `Post`, `PagedResponse<T>`, `AuthResponse`
- Axios client: request interceptor attaches `Bearer` token; response interceptor redirects to `/login` on 401

**Step 10: Auth context and routing**
- `AuthContext.tsx`: store `token/userId/username` in localStorage; expose `login()` and `logout()`
- `ProtectedRoute.tsx`: redirect to `/login` if no token
- `App.tsx`: React Router with routes `/`, `/login`, `/register`, `/new-post`, `/profile/:id`

**Step 11: Auth pages** — Bootstrap forms, call API, store token on success, redirect

**Step 12: Feed**
- `PostCard.tsx`: image, description, like count, `LikeButton`, author link, timestamp
- `PostFeed.tsx`: paginated fetch
- `Pagination.tsx`: Bootstrap pagination
- `FeedPage.tsx`: wraps PostFeed

**Step 13: New post + profile**
- `NewPostPage.tsx`: file input + description, client-side validation before upload
- `ProfilePage.tsx`: fetch `GET /users/{id}/posts`, render grid

**Step 14: Navbar + layout**
- `Navbar.tsx`: brand (yellow), feed link, new post, profile, logout
- `Layout.tsx`: wraps authenticated pages

### Phase 3 — Tests

**Libraries:** `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory`, `FluentAssertions`

`TestWebAppFactory.cs` — replaces SQLite with in-memory SQLite per test run.

| Test file | Tests |
|-----------|-------|
| `AuthServiceTests.cs` | Hash round-trip, wrong password, JWT claims, token expiry |
| `AuthEndpointTests.cs` | Register 201, duplicate 409, login success, wrong password 401 |
| `PostEndpointTests.cs` | Feed public 200, create without auth 401, valid upload 201, oversized 400, wrong MIME 400, like toggle |
| `PostServiceTests.cs` | Newest-first sort, pagination, like add/remove |

---

## Key Implementation Details

### EF Core Feed Query (no N+1)

```csharp
await _db.Posts
    .OrderByDescending(p => p.CreatedAt)
    .Skip((page - 1) * pageSize).Take(pageSize)
    .Select(p => new PostResponse {
        LikeCount = p.Likes.Count,
        LikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
        ImageUrl = $"/uploads/{Path.GetFileName(p.ImagePath)}",
        // ...
    }).ToListAsync();
```

### Like Toggle

```csharp
var existing = await _db.Likes.FirstOrDefaultAsync(l => l.PostId == id && l.UserId == userId);
if (existing is null) _db.Likes.Add(new Like { PostId = id, UserId = userId });
else _db.Likes.Remove(existing);
await _db.SaveChangesAsync();
```

### Theme CSS (Bootstrap 5 override)

```css
:root {
  --bs-primary: #f5f100;
  --bs-primary-rgb: 245, 196, 0;
}
.btn-primary { background-color: #f500d8; border-color: #f5c400; color: #1a1a1a; }
.btn-primary:hover { background-color: #00d415; border-color: #f5f100; color: #1a1a1a; }
.navbar-brand { font-weight: 700; color: #f5c400 !important; }
```

Import order in `main.tsx`: Bootstrap CSS first, then `theme.css`.

---

## Critical Files

| File | Why Critical |
|------|-------------|
| `Web/Program.cs` | DI registration, middleware pipeline |
| `Web/Data/AppDbContext.cs` | Schema, unique constraints, cascade rules |
| `Web/Services/AuthService.cs` | PBKDF2 + JWT — security-critical |
| `Web/Endpoints/PostEndpoints.cs` | Core feature surface (pagination, upload, like toggle) |
| `Web/App/src/auth/AuthContext.tsx` | Central auth state; all protected pages depend on it |
| `Web/App/src/api/client.ts` | JWT interceptor; all API calls flow through it |

---

## Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| No ASP.NET Identity | Overkill; use `Rfc2898DeriveBytes` directly |
| Minimal API (not controllers) | Less boilerplate; sufficient for this surface area |
| Filesystem image storage | Zero dependencies; swap to blob storage later if needed |
| Guid filenames on upload | Prevents path traversal attacks and collisions |
| React Context (not Redux) | Only auth needs global state; posts are per-page |
| Vite proxy | Avoids CORS config in dev; in production serve Vite build via .NET `UseStaticFiles` |
| JWT in localStorage | Simpler for MVP — document as security trade-off in README |

---

## Verification

1. `dotnet run` from `Web/` — server starts, seed data inserted
2. `npm run dev` from `Web/App/` — React app at `http://localhost:5173`
3. Manual flow: register → login → create post with image → view feed → like → unlike
4. Verify 401 on `POST /posts` without auth header
5. Verify 400 on oversized or wrong-type file upload
6. `dotnet test` from `Web.Tests/` — all tests pass

---

## README Sections

1. Prerequisites: .NET 10 SDK, Node 20+
2. Setup: configure `Jwt:Key` (min 32 chars) in `appsettings.Development.json`
3. Run backend: `cd Web && dotnet ef database update && dotnet run`
4. Run frontend: `cd Web/App && npm install && npm run dev`
5. Seed credentials (auto-created on first run)
6. Run tests: `dotnet test`
7. Security note: JWT in localStorage — not production-hardened

