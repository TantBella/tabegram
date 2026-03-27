# Tabegram MVP Implementation Plan

## Problem Statement
Build a greenfield Instagram-like web application (Tabegram) with 1960/70s flower power theming (yellow, orange, pink, green). Full-stack: .NET 10 backend with Entity Framework Core + SQLite, React/TypeScript frontend with Bootstrap 5. MVP scope: user registration/login, image post upload, paginated feed, like/unlike (no comments).

## Approach
**Three-phase implementation:**
1. **Backend (.NET)** — Models, migrations, auth service (PBKDF2 + JWT), post/image/like endpoints, seed data
2. **Frontend (React)** — Auth context, routing, API layer, pages (login, register, feed, new post, profile)
3. **Tests** — Backend unit/integration tests, frontend component tests with mocks

---

## Implementation Status

✅ **Phase 1: Backend (.NET)** - COMPLETE  
✅ **Phase 2: Frontend (React)** - COMPLETE  
⏳ **Phase 3: Tests** - PENDING  

---

## Implementation Order

### Phase 1: Backend (.NET) ✅ COMPLETE

**P1-1: Project Scaffolding** ✅
- ✅ Created `Web` project via `dotnet new webapi`
- ✅ Installed NuGet packages
- ✅ Configured `appsettings.json` and `appsettings.Development.json`

**P1-2: Data Models & DbContext** ✅
- ✅ Created entities: `User`, `Post`, `Like`
- ✅ Built `AppDbContext` with Fluent API constraints
- ✅ Ran migrations successfully

**P1-3: Auth Service** ✅
- ✅ Implemented PBKDF2 hashing (310k iterations, SHA-256, 32-byte salt)
- ✅ Created `IAuthService`/`AuthService` with full auth flow
- ✅ JWT generation with proper claims and signing

**P1-4: Auth Endpoints** ✅
- ✅ `POST /auth/register` — 201/409 responses
- ✅ `POST /auth/login` — 200/401 responses

**P1-5: Image Service** ✅
- ✅ Validation: MIME types, size limits, secure filenames
- ✅ Saves as Guid + extension

**P1-6: Post Endpoints** ✅
- ✅ `GET /posts` — Paginated, 10 per page default
- ✅ `POST /posts` — Multipart image + description upload
- ✅ `POST /posts/{id}/like` — Toggle like endpoint
- ✅ `GET /users/{id}/posts` — User's post list
- ✅ `GET /uploads/{filename}` — Image serving

**P1-7: Seed Data** ✅
- ✅ 3 users (alice, bob, charlie) with test passwords
- ✅ 8 posts distributed across users
- ✅ Generated gradient PNG images
- ✅ Cross-user likes

**P1-8: Program.cs Wiring** ✅
- ✅ JWT middleware configured
- ✅ CORS enabled (wildcard for MVP)
- ✅ Services registered
- ✅ Seed runs on startup

**Verification:** Backend running on http://localhost:5264, database seeded, all endpoints tested ✅

---

### Phase 2: Frontend (React) ✅ COMPLETE

**P2-1: Scaffolding** ✅
- ✅ Vite React+TypeScript project
- ✅ Dependencies: axios, react-router-dom, bootstrap, react-bootstrap
- ✅ Vite proxy configured for /posts, /auth, /users, /uploads

**P2-2: Types & API Layer** ✅
- ✅ Full TypeScript types (User, Post, PagedResponse)
- ✅ Axios client with JWT bearer interceptor
- ✅ 401 redirect on auth failure
- ✅ API functions: register, login, getPosts, createPost, toggleLike, getUserPosts

**P2-3: Auth Context & Routing** ✅
- ✅ AuthContext with localStorage persistence
- ✅ ProtectedRoute wrapper
- ✅ React Router with all pages

**P2-4: Auth Pages** ✅
- ✅ LoginPage with form validation
- ✅ RegisterPage with password confirmation
- ✅ Error handling on auth failures

**P2-5: Feed Pages** ✅
- ✅ PostCard component (image, description, like button)
- ✅ Paginated feed with bootstrap pagination
- ✅ Like toggle with optimistic UI updates

**P2-6: New Post Page** ✅
- ✅ File input with client-side validation (10MB limit)
- ✅ Multipart form upload
- ✅ Description textarea

**P2-7: Profile Page** ✅
- ✅ User's posts grid layout
- ✅ Fetch from `/users/{id}/posts`

**P2-8: Navbar & Layout** ✅
- ✅ Navigation bar with links and logout
- ✅ Layout wrapper for all pages
- ✅ Bootstrap 5 theme with flower-power colors (yellow, orange, pink, green)

**Verification:** Frontend running on http://localhost:5173, all features tested ✅

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
