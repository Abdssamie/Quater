# Model Synchronization Guide

## ⚠️ CRITICAL: Model Duplication Strategy

This project currently maintains **duplicate models** across 3 platforms for offline-first architecture. This is a **temporary solution** until Phase 3 refactoring.

## Model Locations

### Shared Models (4 entities)
These models exist in all 3 platforms:

| Model | Backend (Master) | Desktop | Mobile (Future) |
|-------|-----------------|---------|-----------------|
| **Sample** | `backend/src/Quater.Backend.Core/Models/Sample.cs` | `desktop/src/Quater.Desktop.Data/Models/Sample.cs` | `mobile/src/models/Sample.ts` |
| **TestResult** | `backend/src/Quater.Backend.Core/Models/TestResult.cs` | `desktop/src/Quater.Desktop.Data/Models/TestResult.cs` | `mobile/src/models/TestResult.ts` |
| **Parameter** | `backend/src/Quater.Backend.Core/Models/Parameter.cs` | `desktop/src/Quater.Desktop.Data/Models/Parameter.cs` | `mobile/src/models/Parameter.ts` |
| **SyncLog** | `backend/src/Quater.Backend.Core/Models/SyncLog.cs` | `desktop/src/Quater.Desktop.Data/Models/SyncLog.cs` | `mobile/src/models/SyncLog.ts` |

### Backend-Only Models (4 entities)
These models only exist in the backend:

- **User** - `backend/src/Quater.Backend.Core/Models/User.cs` (extends IdentityUser)
- **Lab** - `backend/src/Quater.Backend.Core/Models/Lab.cs`
- **AuditLog** - `backend/src/Quater.Backend.Core/Models/AuditLog.cs`
- **AuditLogArchive** - `backend/src/Quater.Backend.Core/Models/AuditLogArchive.cs`

## Synchronization Checklist

### When Modifying a Shared Model

**Backend is the MASTER** - Always start changes here.

#### Step 1: Update Backend Model
```bash
# Edit the backend model
vim backend/src/Quater.Backend.Core/Models/[ModelName].cs
```

#### Step 2: Update Desktop Model
```bash
# Copy schema changes to desktop
vim desktop/src/Quater.Desktop.Data/Models/[ModelName].cs
```

**Important Desktop Conversions:**
- C# enums → string properties (SQLite compatibility)
- Keep all other fields identical (names, types, attributes)

**Enum Mapping Examples:**
```csharp
// Backend (C# enum)
public SampleType Type { get; set; }

// Desktop (string)
public string Type { get; set; } = string.Empty;
// Valid values: "DrinkingWater", "Wastewater", "SurfaceWater", "Groundwater", "IndustrialWater"
```

#### Step 3: Update DbContext Configurations
```bash
# If relationships or indexes changed
vim backend/src/Quater.Backend.Data/QuaterDbContext.cs
vim desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs
```

#### Step 4: Create Migrations
```bash
# Backend migration
dotnet ef migrations add [MigrationName] \
  --project backend/src/Quater.Backend.Data \
  --startup-project backend/src/Quater.Backend.Api

# Desktop migration
dotnet ef migrations add [MigrationName] \
  --project desktop/src/Quater.Desktop.Data \
  --startup-project desktop/src/Quater.Desktop
```

#### Step 5: Verify Builds
```bash
# Build backend
dotnet build backend/Quater.Backend.sln

# Build desktop
dotnet build desktop/Quater.Desktop.sln
```

#### Step 6: Update Mobile (When Ready)
```bash
# Regenerate TypeScript types from OpenAPI/Swagger
npx openapi-typescript-codegen \
  --input http://localhost:5000/swagger.json \
  --output mobile/src/api
```

## Key Differences Between Platforms

### Backend (.NET + PostgreSQL)
- Uses C# enums (SampleType, SampleStatus, TestMethod, ComplianceStatus, UserRole)
- Navigation properties for relationships
- Inherits from IdentityDbContext<User>
- Full audit trail with AuditLog and AuditLogArchive

### Desktop (.NET + SQLite)
- Uses string properties instead of enums (SQLite compatibility)
- Same navigation properties
- Inherits from DbContext
- No User/Lab/Audit entities (syncs from backend)

### Mobile (React Native + TypeScript + SQLite)
- TypeScript interfaces (generated from API)
- No navigation properties (uses IDs)
- SQLite storage via react-native-sqlite-storage
- Syncs with backend API

## Enum Value Reference

### SampleType
- Backend: `SampleType.DrinkingWater`
- Desktop/Mobile: `"DrinkingWater"`
- Values: DrinkingWater, Wastewater, SurfaceWater, Groundwater, IndustrialWater

### SampleStatus
- Backend: `SampleStatus.Pending`
- Desktop/Mobile: `"Pending"`
- Values: Pending, Completed, Archived

### TestMethod
- Backend: `TestMethod.Titration`
- Desktop/Mobile: `"Titration"`
- Values: Titration, Spectrophotometry, Chromatography, Microscopy, Electrode, Culture, Other

### ComplianceStatus
- Backend: `ComplianceStatus.Pass`
- Desktop/Mobile: `"pass"`
- Values: pass, fail, warning

### UserRole (Backend Only)
- Backend: `UserRole.Admin`
- Values: Admin, Technician, Viewer

## Common Pitfalls

### ❌ DON'T:
- Change desktop models without updating backend first
- Add fields to only one platform
- Change field names or tyently
- Forget to run migrations after model changes
- Use different enum string values between platforms

### ✅ DO:
- Always update backend first (it's the master)
- Keep field names and types identical (except enum → string)
- Document why you're making the change
- Test sync after model changes
- Run migrations for both backend and desktop

## Future Refactoring (Phase 3)

### Planned Improvements

1. **Shared .NET Models Project**
   ```
   shared/Quater.Shared.Models/
   ├── Sample.cs
   ├── TestResult.cs
   ├── Parameter.cs
   ├── SyncLog.cs
   └── Converters/
       └── EnumConverter.cs  # Handle enum ↔ string conversion
   ```

2. **Automated Mobile Type Generation**
   - Add OpenAPI/Swagger to backend
   - Generate TypeScript types in CI/CD pipeline
   - No manual mobile model maintenance

3. **Benefits**
   - Single source of truth for .NET projects
   - Automated synchronization
   - Compile-time safety
   - Reduced maintenance burden

## Questions?

If you're unsure about a model change:
1. Check the data model specification: `specs/001-water-quality-platform/data-model.md`
2. Look at existing model documentation (each model has sync warnings)
3. Test locally before committing
 your changes in the migration name

## Related Files

- **Data Model Spec**: `specs/001-water-quality-platform/data-model.md`
- **Backend DbContext**: `backend/src/Quater.Backend.Data/QuaterDbContext.cs`
- **Desktop DbContext**: `desktop/src/Quater.Desktop.Data/QuaterLocalContext.cs`
- **Implementation Plan**: `specs/001-water-quality-platform/plan.md`

---

**Last Updated**: 2026-01-27  
**Status**: Phase 2 Complete - Models documented with sync warnings  
**Next**: Phase 3 - Refactor to shared models project
