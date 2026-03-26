# Tabegram MVP Implementation Plan

## Problem Statement
Build a greenfield Instagram-like web application (Tabegram) with 1960/70s flower power theming (yellow, orange, pink, green). Full-stack: .NET 10 backend with Entity Framework Core + SQLite, React/TypeScript frontend with Bootstrap 5. MVP scope: user registration/login, image post upload, paginated feed, like/unlike (no comments).

## Approach
**Three-phase implementation:**
1. **Backend (.NET)** — Models, migrations, auth service (PBKDF2 + JWT), post/image/like endpoints, seed data
2. **Frontend (React)** — Auth context, routing, API layer, pages (login, register, feed, new post, profile)
3. **Tests** — Backend unit/integration tests, frontend component tests with mocks

---

## Implementation Order

### Phase 1: Backend (.NET)

**P1-1: Project Scaffolding**
- Create `Web` project via `dotnet new webapi`
- Install NuGet packages: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`
- Configure `appsettings.json` with: `ConnectionStrings:Default`, JWT settings (Key, Issuer, Audience, ExpiryMinutes), `Uploads:BasePath`
- Configure `appsettings.Development.json` with a dev JWT key (min 32 chars)

**P1-2: Data Models & DbContext**
- Create entities: `User` (id, username, passwordHash, createdAt), `Post` (id, userId, imagePath, description, createdAt), `Like` (id, postId, userId, createdAt)
- Build `AppDbContext` with Fluent API: unique index on `Username`, unique index on `(PostId, UserId)` for Likes, cascade delete rules
- Run migrations: `dotnet ef migrations add InitialCreate && dotnet ef database update`

**P1-3: Auth Service**
- Implement PBKDF2 hashing: 310k iterations, SHA-256, 32-byte salt, format: `base64(salt):base64(hash)`
- Create `IAuthService`/`AuthService`: `RegisterAsync()`, `LoginAsync()`, `VerifyPasswordAsync()`
- Implement JWT generation with claims: `sub` (userId), `unique_name` (username), signed with HmacSha256
- Register in DI

**P1-4: Auth Endpoints**
- `POST /auth/register` — Request: `{username, password}`, Response: `{userId, username, token}`, 201 on success, 409 on duplicate username
- `POST /auth/login` — Request: `{username, password}`, Response: `{userId, username, token}`, 401 on invalid credentials

**P1-5: Image Service**
- Create `IImageService`/`ImageService`: `SaveImageAsync(file)`, `ValidateImageAsync(file)`
- Validation: MIME types (jpg, png, gif, webp), max 10 MB, secure filename
- Save as `{Guid}{ext}`, return filename only (not full path)

**P1-6: Post Endpoints**
- `GET /posts?page=1&pageSize=10` — Public, paginated (newest-first), EF projection includes `LikeCount`, `LikedByCurrentUser` (via SQL EXISTS)
- `POST /posts` — Auth required, multipart (image + description), returns `PostResponse` with 201
- `POST /posts/{id}/like` — Auth required, toggle like (add if absent, remove if present), returns 200
- `GET /users/{id}/posts` — Public, list all posts for user
- `GET /uploads/{filename}` — Serve static image file

**P1-7: Seed Data**
- Create `DbSeeder.cs`: auto-runs on startup if DB is empty
- Seed 3 users + 8 posts with mixed distribution
- Generate placeholder images (solid-color PNGs or simple test images)
- Seed some likes to make feed more interesting

**P1-8: Program.cs Wiring**
- Configure JWT authentication middleware
- Configure CORS if needed (or rely on Vite proxy in dev)
- Register services (AuthService, PostService, ImageService)
- Extension methods: `app.MapAuthEndpoints()`, `app.MapPostEndpoints()`, etc.
- Seed DB on startup
- Add `public partial class Program { }` for test factory

---

### Phase 2: Frontend (React)

**P2-1: Scaffolding**
- Create React + TypeScript project via `npm create vite@latest App -- --template react-ts`
- Install: `axios`, `react-router-dom`, `bootstrap`, `react-bootstrap` (optional)
- Configure `vite.config.ts` with proxy: forward `/posts`, `/auth`, `/users`, `/uploads` → `http://localhost:5000`

**P2-2: Types & API Layer**
- `src/types/index.ts`: Define `User`, `Post`, `PagedResponse<T>`, `AuthResponse`, `CreatePostRequest`
- `src/api/client.ts`: Axios instance with request interceptor (attach Bearer token), response interceptor (redirect to `/login` on 401)
- `src/api/auth.ts`: `register()`, `login()`, `logout()`
- `src/api/posts.ts`: `getPosts()`, `createPost()`, `toggleLike()`, `getUserPosts()`

**P2-3: Auth Context & Protected Routing**
- `src/auth/AuthContext.tsx`: Context with `token`, `userId`, `username`; localStorage persistence; `login()`, `logout()`, `isAuthenticated()`
- `src/auth/ProtectedRoute.tsx`: Redirect to `/login` if no token
- `src/App.tsx`: React Router with routes: `/`, `/login`, `/register`, `/new-post`, `/profile/:id`

**P2-4: Auth Pages**
- `LoginPage.tsx`: Form (username/password), API call, token storage, redirect to feed
- `RegisterPage.tsx`: Form (username/password), validation, API call, redirect to login

**P2-5: Feed Pages**
- `PostCard.tsx`: Display image, description, author, timestamp, like count, `LikeButton`
- `LikeButton.tsx`: Button with heart icon, toggle like on click, show count
- `PostFeed.tsx`: Fetch posts, render PostCard list, pagination
- `Pagination.tsx`: Bootstrap pagination component
- `FeedPage.tsx`: Wraps PostFeed

**P2-6: New Post Page**
- `NewPostPage.tsx`: File input (image), description textarea, client-side validation (file size, type), multipart upload

**P2-7: Profile Page**
- `ProfilePage.tsx`: Fetch `GET /users/{id}/posts`, render grid layout, link to post authors

**P2-8: Navbar & Layout**
- `Navbar.tsx`: Brand (Tabegram with yellow), links to feed, new post, profile, logout
- `Layout.tsx`: Wraps authenticated pages, renders Navbar
- `src/styles/theme.css`: Bootstrap variable overrides (yellow, orange, pink, green accents)
- `src/main.tsx`: Import order — Bootstrap CSS first, then theme.css

---

### Phase 3: Tests

**P3-1: Backend Test Setup**
- Add NuGet: `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory`, `FluentAssertions`
- Create `Web.Tests` project
- `TestWebAppFactory.cs`: Override CreateClient to use in-memory SQLite per test run, seed test data
- `TestDbHelper.cs`: Utilities for DB setup/teardown

**P3-2: Backend Tests**
- `AuthServiceTests.cs`: Hash round-trip, wrong password rejection, JWT claims validation, token expiry
- `AuthEndpointTests.cs`: Register success (201), duplicate username (409), login success, wrong password (401)
- `PostEndpointTests.cs`: Feed public access (200), create without auth (401), valid upload (201), oversized file (400), wrong MIME type (400), like toggle
- `PostServiceTests.cs`: Newest-first sort, pagination, like add/remove logic

**P3-3: Frontend Tests**
- Setup: `vitest` (Vite's test runner) + `@testing-library/react`, mock Axios for API calls
- `AuthContext.test.tsx`: Token storage, login/logout, isAuthenticated
- `PostCard.test.tsx`: Render post, like button interaction (mock toggleLike)
- `PostFeed.test.tsx`: Fetch and render posts, pagination, error handling
- Mock images (solid colors or simple test PNGs)

---

## Key Decisions & Rationale

| Decision | Why |
|----------|-----|
| PBKDF2 (not ASP.NET Identity) | Lighter weight; Identity is overkill for MVP |
| Minimal APIs | Less boilerplate; sufficient for this endpoint count |
| Filesystem images (not blob storage) | Zero external dependencies; can migrate later |
| Guid filenames | Prevent path traversal + collisions |
| React Context (not Redux) | Only auth needs global state; posts fetched per-page |
| Vite proxy in dev | Avoids CORS; in production, serve Vite build via `UseStaticFiles` |
| JWT in localStorage | Simpler MVP; document as security trade-off |
| Customize Bootstrap via variables | Maintains Bootstrap defaults while applying flower-power theme |
| 3 users + 8 posts with generated images | Realistic test data; enough to test pagination and likes |
| Frontend component tests with mocks | Catch UI regressions without backend dependency |

---

## Verification Checklist

1. Backend
   - `dotnet run` from `Web/` — server starts, seed data inserted, no errors
   - `dotnet test` from `Web.Tests/` — all tests pass
   - Manual: register → login → receive JWT token
   - Manual: upload image, verify file saved and DB updated
   - Manual: like/unlike post, verify DB state

2. Frontend
   - `npm run dev` from `Web/App/` — React app loads at `http://localhost:5173`
   - Manual: register, login, logout flow
   - Manual: create post with image + description
   - Manual: view feed, paginate, like/unlike
   - Manual: view profile page
   - `npm run test` — all frontend tests pass
   - Verify theme colors applied (yellow brand, flower-power accents)

3. Integration
   - Backend + frontend running together
   - Full user flow: register → login → create post → view feed → like → profile

---

## Notes

- **Git workflow**: Each major phase will be a commit with "Co-authored-by: Copilot" trailer
- **Error handling**: Consistent JSON error responses; proper HTTP status codes
- **Seed images**: Generate simple test images programmatically (e.g., solid-color PNGs) to avoid file dependencies
- **Styling**: Use Bootstrap 5 component library; override CSS variables for theme colors
- **Security**: JWT secret must be min 32 chars and secure in production (use secrets management)
- **CORS**: Configure only if needed; Vite proxy handles dev case
