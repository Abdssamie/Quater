# Research & Technology Decisions

**Feature**: Water Quality Lab Management System  
**Branch**: `001-water-quality-platform`  
**Date**: 2026-01-25  
**Status**: Complete

This document consolidates research findings for all technology decisions required for the MVP implementation.

---

## 1. Desktop/Mobile Framework: Avalonia vs Alternatives

### Decision: Avalonia for Desktop + React Native for Mobile

**Rationale:**
- **Avalonia is production-ready for desktop** (Windows, Linux, macOS) with 29.9k GitHub stars and enterprise adoption
- **Avalonia mobile support is immature** with active bugs (ScrollViewer crashes, pointer events, performance issues)
- **React Native is production-ready for mobile** with massive ecosystem, proven track record (Facebook, Instagram, Airbnb)
- **Shared business logic** via REST API maintains integration while using best tool for each platform

**Alternatives Considered:**
- **Avalonia for both**: Rejected due to mobile immaturity and risk to MVP timeline
- **Full .NET MAUI**: Good mobile support but less polished desktop experience than Avalonia
- **Uno Platform**: Good XAML consistency but smaller community than React Native

**Implementation Notes:**
- Desktop: Avalonia with ReactiveUI for MVVM
- Mobile: React Native with TypeScript for type safety
- Shared: REST API contracts (OpenAPI auto-generated), JSON DTOs
- GPS: react-native-geolocation-service for offline GPS support
- Storage: SQLite for both (react-native-sqlite-storage for mobile)

---

## 2. Authentication: ASP.NET Core Identity + OpenIddict

### Decision: ASP.NET Core Identity (User Management) + OpenIddict OAuth2/OpenID Connect (Token Server)

**Rationale:**
- **Best of both worlds**: Identity handles user management (registration, password reset, roles), OpenIddict handles token issuance (OAuth2/OIDC flows)
- **No vendor lock-in**: Both are open-source (Apache 2.0), you own the code
- **Native .NET integration**: Built for ASP.NET Core, seamless EF Core integration
- **Offline-first compatible**: JWT tokens with offline validation
- **Self-hosted**: Part of your backend, no separate service needed
- **Production-ready**: Used by Microsoft, Stack Overflow, and many enterprises
- **Full control**: Customize auth flows, token lifetimes, claims, user management

**Alternatives Considered:**
- **Clerk**: Rejected due to lack of offline support and desktop/mobile SDK
- **Keycloak**: Enterprise-grade but heavy (Java runtime), overkill for MVP
- **ZITADEL**: Modern but adds complexity (Go runtime, separate service)
- **Auth0**: Cloud-dependent; limited offline capabilities
- **OpenIddict alone**: Would require manual user management implementation

**Implementation Notes:**
- Backend: ASP.NET Core Identity for user management + OpenIddict for OAuth2/OIDC token server
- Tokens: JWT with RS256 signing (offline validation via cached public keys)
- Storage: Platform-specific secure storage (Windows DPAPI, macOS Keychain, Android Keystore)
- Offline: Token caching with 7-day grace period, refresh tokens when online
- Sync: Validate tokens with server when connectivity available
- User Management: Identity provides built-in APIs for registration, password reset, role management

---

## 3. PDF Generation: Typst vs Alternatives

### Decision: QuestPDF

**Rationale:**
- **Typst .NET bindings are immature**: Low adoption (~2,500 downloads), experimental status, native dependency complexity
- **QuestPDF is production-ready**: 13.7k GitHub stars, pure C#, excellent performance
- **Performance meets requirements**: Can generate 100+ PDFs in 2-5 seconds (well under 10-second target)
- **Fluent C# API**: No new markup language to learn, better .NET ecosystem integration

**Alternatives Considered:**
- **Typst (typstsharp)**: Rejected due to immaturity, native dependencies, limited ecosystem
- **iText 7**: Mature but expensive licensing and complex API
- **PdfSharp**: Free but slower and limited layout capabilities
- **HTML-to-PDF (Puppeteer)**: Heavy Chromium dependency, slower

**Implementation Notes:**
- License: Community license (free for <$1M revenue)
- Templates: C# classes with fluent API, version-controlled as code
- Performance: Enable caching, parallel generation for multiple PDFs
- Compliance: PDF/A support available if needed for regulatory requirements

---

## 4. Data Access: EF Core + SQLite

### Decision: Entity Framework Core with SQLite (Timestamp-Based Optimistic Locking)

**Rationale:**
- **EF Core provides mature ORM**: Migration framework, LINQ support, change tracking
- **SQLite ideal for offline-first**: Single-file database, no server required, cross-platform
- **Timestamp-based sync is practical**: Simpler conflict detection, partial sync support, merge-friendly
- **Application-managed concurrency**: SQLite lacks native rowversion; use DateTime LastModified field

**Alternatives Considered:**
- **Dapper/ADO.NET**: Better performance but no migrations, more boilerplate
- **LiteDB**: NoSQL simplicity but harder to sync with SQL backend
- **Realm**: Built-in sync but proprietary, licensing costs, vendor lock-in

**Implementation Notes:**
- Migrations: Apply programmatically at app startup (`Database.MigrateAsync()`)
- Concurrency: `[ConcurrencyCheck]` on `LastModified` DateTime field
- Sync Strategy: Pull changes since last sync timestamp, push local changes
- Performance: Indexes on `LastModified`, `IsSynced`; AsNoTracking for read-only queries
- Soft Deletes: `IsDeleted` flag for sync compatibility
- WAL Mode: Enable Write-Ahead Logging for better concurrency

---

## 5. GPS/Location Services

### Decision: React Native Geolocation Service

**Rationale:**
- **React Native has mature GPS libraries**: react-native-geolocation-service is production-ready
- **Offline support**: GPS works without internet connectivity
- **Cross-platform**: Works on Android (and iOS for Phase 2)
- **Active maintenance**: Well-maintained with 3.5k+ GitHub stars

**Alternatives Considered:**
- **Expo Location**: Good but requires Expo framework
- **react-native-location**: Less maintained than geolocation-service

**Implementation Notes:**
- Mobile (React Native): Use `react-native-geolocation-service`
- Permissions: Request location permissions at runtime (Android 6.0+)
- Fallback Strategy:
  1. Try GPS (most accurate, 10s timeout)
  2. Fallback to Network provider (faster, less accurate, 5s timeout)
  3. Use last known location if recent (<30 minutes)
  4. Allow manual text entry if all fail
- Accuracy: Use medium accuracy for balance between battery and precision
- Battery: Request single location update, not continuous tracking

---

## 6. Conflict Resolution Implementation

### Decision: Last-Write-Wins with Automatic Backup + Optimistic Locking

**Rationale:**
- **Prevents silent data loss**: Detects concurrent modifications before save
- **User-friendly UX**: Automatic backup of overwritten data, user can review if needed
- **Audit trail preserved**: Both versions saved in AuditLog with resolution notes
- **Simpler than full merge UI**: Reduces testing complexity while maintaining data safety

**Implementation Notes:**
- Entity Fields: `Version` (int), `LastModified` (DateTime), `LastModifiedBy` (string)
- EF Core: Mark `LastModified` with `[ConcurrencyCheck]` attribute
- Conflict Detection: Catch `DbUpdateConcurrencyException` on SaveChanges
- Resolution Strategy: Last-Write-Wins (server timestamp wins), but backup local version
- Notification UI: Show user summary of conflict with option to view backup or keep server version
- Optional Notes Field: User can document why they chose to keep/discard changes
- Audit Log: Store old_value, new_value, conflict_resolution_notes

---

## 7. C# to TypeScript Code Generation

### Decision: NSwag for TypeScript Client Generation

**Rationale:**
- **Generates from OpenAPI**: Single source of truth (Swashbuckle-generated OpenAPI spec)
- **Complete client generation**: TypeScript types + API client methods + validation
- **Mature and widely used**: 6.7k GitHub stars, production-ready
- **Automatic synchronization**: Regenerate on backend changes, no manual DTO maintenance
- **Type safety**: Full TypeScript type checking for API calls

**Alternatives Considered:**
- **TypeGen**: Only generates types, not API client; requires manual API calls
- **Typewriter**: Template-based, more complex setup
- **OpenAPI Generator**: More generic, less .NET-specific optimizations
- **Manual TypeScript**: High maintenance burden, prone to drift

**Implementation Notes:**
- Backend: Swashbuckle.AspNetCore generates OpenAPI spec at `/swagger/v1/swagger.json`
- Build Process: NSwag CLI generates TypeScript client during mobile app build
- Output: Single `.ts` file with all DTOs, enums, and API client class
- Mobile App: Import generated client, configure base URL, use typed methods
- Regeneration: Run `nswag run nswag.json` when backend API changes
- Configuration: `nswag.json` file specifies input (OpenAPI URL) and output (TypeScript file path)

---

## Architecture Summary

### Technology Stack

**Backend:**
- ASP.NET Core 8.0 Web API
- Entity Framework Core 8.0 + PostgreSQL 15+
- ASP.NET Core Identity (user management) + OpenIddict OAuth2/OpenID Connect (token server)
- QuestPDF for report generation
- Swashbuckle.AspNetCore for auto-generated OpenAPI/Swagger
- NSwag for TypeScript client generation
- Docker + Docker Compose for deployment

**Desktop:**
- Avalonia UI 11.x (Windows, Linux, macOS)
- ReactiveUI for MVVM
- Entity Framework Core 8.0 + SQLite
- QuestPDF for local report generation

**Mobile:**
- React Native 0.73+ (Android 8.0+, iOS in Phase 2)
- TypeScript for type safety
- React Native SQLite Storage (offline)
- react-native-geolocation-service (GPS)
- AsyncStorage for token caching
- NSwag-generated TypeScript client (from backend OpenAPI spec)

**Shared:**
- REST API (OpenAPI contract, auto-generated via Swashbuckle)
- JSON DTOs (language-agnostic)
- NSwag-generated TypeScript client for mobile app
- Sync protocol (HTTP + JSON)

**Testing:**
- xUnit + FluentAssertions (backend unit tests)
- Testcontainers (integration tests with PostgreSQL)
- Avalonia UI Testing (desktop E2E)
- Jest + React Native Testing Library (mobile tests)

### Project Structure

```
backend/
├── Quater.Backend.Api/          # ASP.NET Core Web API
├── Quater.Backend.Core/         # Business logic
├── Quater.Backend.Data/         # EF Core + PostgreSQL
└── Quater.Backend.Sync/         # Sync engine

desktop/
├── Quater.Desktop/              # Avalonia UI app
├── Quater.Desktop.Data/         # EF Core + SQLite
└── Quater.Desktop.Sync/         # Client-side sync

mobile/
├── src/
│   ├── components/              # React Native components
│   ├── screens/                 # App screens
│   ├── services/                # API client, sync service
│   ├── storage/                 # SQLite wrapper
│   └── utils/                   # Helpers
└── __tests__/                   # Jest tests

shared/
└── api-contracts/               # OpenAPI spec (auto-generated from backend)
```
├── Quater.Desktop.Data/         # EF Core + SQLite
└── Quater.Desktop.Sync/         # Client-side sync

mobile/
├── Quater.Mobile/               # .NET MAUI app
├── Quater.Mobile.Data/          # EF Core + SQLite
└── Quater.Mobile.Sync/          # Client-side sync

shared/
└── api-contracts/               # OpenAPI spec (auto-generated from backend)
```

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Avalonia mobile immaturity | Use React Native for mobile instead |
| OpenIddict complexity | Well-documented, production-ready, native .NET integration; ASP.NET Core Identity handles user management |
| QuestPDF licensing | Community license free for <$1M revenue |
| Sync conflicts | Last-Write-Wins with automatic backup + user notification UI |
| GPS unavailable | Multi-tier fallback + manual entry option |
| Performance at scale | Indexes, AsNoTracking, batch operations, WAL mode |
| React Native + .NET integration | REST API provides clean separation; NSwag generates TypeScript client from OpenAPI |
| TypeScript/C# type drift | NSwag auto-generates TypeScript from OpenAPI; regenerate on backend changes |

---

## Next Steps

1. ✅ Research complete
2. ⏳ Create data model (data-model.md)
3. ⏳ Define API contracts (contracts/api.openapi.yaml)
4. ⏳ Create developer quickstart guide (quickstart.md)
5. ⏳ Update agent context with technology stack

---

**Research Status**: ✅ Complete - All technology decisions finalized and documented
