# Agent Instructions - Quater Water Quality Lab Management System

## Project Overview

**Quater** is an open-source, cross-platform water quality lab management system with three integrated applications:
- **Backend API**: ASP.NET Core 8.0 + PostgreSQL
- **Desktop App**: Avalonia UI 11.x (Windows/Linux/macOS)
- **Mobile App**: React Native 0.73+ (Android, field sample collection only)

**Architecture**: Offline-first with bidirectional sync, Last-Write-Wins conflict resolution with automatic backup.

---

## Build, Lint & Test Commands

### Backend (.NET 8)

```bash
# Build
dotnet build backend/Quater.Backend.sln
dotnet build backend/Quater.Backend.sln --configuration Release

# Run tests
dotnet test backend/tests/Quater.Backend.Api.Tests/
dotnet test backend/tests/Quater.Backend.Core.Tests/
dotnet test backend/tests/Quater.Backend.Sync.Tests/

# Run single test
dotnet test --filter "FullyQualifiedName=Quater.Backend.Api.Tests.SampleControllerTests.CreateSample_ValidData_ReturnsCreated"
dotnet test --filter "FullyQualifiedName~SampleController"  # All tests in class

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Lint (if using dotnet-format)
dotnet format backend/Quater.Backend.sln --verify-no-changes
dotnet format backend/Quater.Backend.sln  # Auto-fix

# Run API locally
cd backend/src/Quater.Backend.Api
dotnet run
```

### Desktop (Avalonia)

```bash
# Build
dotnet build desktop/Quater.Desktop.sln

# Run tests
dotnet test desktop/tests/Quater.Desktop.Tests/
dotnet test desktop/tests/Quater.Desktop.Data.Tests/

# Run single test
dotnet test --filter "FullyQualifiedName=Quater.Desktop.Tests.ViewModels.SampleViewModelTests.CreateSample_ValidData_Success"

# Run desktop app
cd desktop/src/Quater.Desktop
dotnet run
```

### Mobile (React Native)

```bash
cd mobile/

# Install dependencies
npm install

# Lint
npm run lint
npm run lint:fix

# Type check
npm run type-check

# Run tests
npm test
npm test -- SampleScreen.test.tsx  # Single test file
npm test -- --testNamePattern="should create sample"  # Single test

# Run with coverage
npm test -- --coverage

# Build & run Android
npm run android

# Build release APK
cd android && ./gradlew assembleRelease
```

---

## Code Style Guidelines

### .NET (Backend + Desktop)

**Naming Conventions:**
- **PascalCase**: Classes, methods, properties, public fields (`SampleService`, `CreateSample()`)
- **camelCase**: Local variables, parameters, private fields (`sampleId`, `_dbContext`)
- **Interfaces**: Prefix with `I` (`ISampleRepository`, `ISyncService`)
- **Async methods**: Suffix with `Async` (`GetSampleAsync()`, `SyncDataAsync()`)

**Formatting:**
- **Indentation**: 4 spaces (no tabs)
- **Braces**: Allman style (opening brace on new line)
- **Line length**: Max 120 characters
- **File organization**: Usings → namespace → class

**Types:**
- Use `var` for obvious types: `var sample = new Sample();`
- Explicit types for primitives: `int count = 0;`
- Nullable reference types enabled: `string? optionalField`
- Use `Task<T>` for async methods, never `async void` (except event handlers)

**Error Handling:**
- Use specific exceptions: `ArgumentNullException`, `InvalidOperationException`
- Never catch `Exception` unless re-throwing
- Log exceptions before re-throwing
- Use `Result<T>` pattern for business logic errors (not exceptions)

**Imports:**
```csharp
// System namespaces first
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Third-party packages
using Microsoft.EntityFrameworkCore;
using FluentValidation;

// Project namespaces
using Quater.Backend.Core.Models;
using Quater.Backend.Core.Interfaces;
```

**Example:**
```csharp
public class SampleService : ISampleService
{
    private readonly IRepository<Sample> _repository;
    private readonly ILogger<SampleService> _logger;

    public SampleService(IRepository<Sample> repository, ILogger<SampleService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Sample>> CreateSampleAsync(CreateSampleDto dto)
    {
        if (dto == null)
            return Result<Sample>.Failure("Sample data is required");

        try
        {
            var sample = new Sample
            {
                SampleType = dto.SampleType,
                CollectionDate = dto.CollectionDate,
                Location = dto.Location
            };

            await _repository.AddAsync(sample);
            _logger.LogInformation("Sample {SampleId} created successfully", sample.Id);
            
            return Result<Sample>.Success(sample);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sample");
            throw;
        }
    }
}
```

### TypeScript (Mobile)

**Naming Conventions:**
- **PascalCase**: Components, types, interfaces (`SampleScreen`, `Sample`, `ISampleService`)
- **camelCase**: Variables, functions, properties (`sampleId`, `createSample()`)
- **UPPER_SNAKE_CASE**: Constants (`API_BASE_URL`, `MAX_RETRY_COUNT`)

**Formatting:**
- **Indentation**: 2 spaces
- **Semicolons**: Required
- **Quotes**: Single quotes for strings
- **Line length**: Max 100 characters

**Types:**
- Always use TypeScript, never `any` (use `unknown` if truly unknown)
- Define interfaces for all data structures
- Use type inference where obvious: `const count = 0;`
- Explicit return types for functions

**Imports:**
```typescript
// React/React Native first
import React, { useState, useEffect } from 'react';
import { View, Text, Button } from 'react-native';

// Third-party packages
import { useNavigation } from '@react-navigation/native';

// Project imports (absolute paths via tsconfig)
import { Sample } from '@/types/sample';
import { useSampleService } from '@/services/sampleService';
import { SampleForm } from '@/components/SampleForm';
```

---

## Working with Beads (Issue Tracking)

Beads is used for task tracking and dependencies. **Use beads for strategic work** (multi-session, dependencies, discovered work).

### Quick Commands

```bash
bd ready                              # Find available work
bd show quater-1                      # View issue details
bd update quater-1 --status=in_progress  # Claim work
bd close quater-1                     # Complete work
bd create --title="..." --type=task --priority=2  # Create issue
bd dep add quater-2 quater-1          # quater-2 depends on quater-1
bd sync --from-main                   # Sync beads from main branch
```

**Priority Levels**: 0 (critical) → 1 (high) → 2 (medium) → 3 (low) → 4 (backlog)

### Session Close Protocol (MANDATORY)

Before ending a session, you MUST:
1. Close completed beads issues: `bd close quater-1 quater-2 ...`
2. Run `bd sync --from-main` to pull latest beads updates
3. Commit code changes: `git add . && git commit -m "..."`
4. **DO NOT push to remote** - This is an ephemeral branch, merge to main locally

---

## Working with Speckit (Specifications)

Speckit manages feature specifications. All specs are in `specs/001-water-quality-platform/`.

### Key Files

- **spec.md**: User stories and requirements (v1.2)
- **plan.md**: Implementation plan, tech stack, project structure
- **data-model.md**: Complete data model for all components
- **research.md**: Technology decisions and rationale
- **ARCHITECTURE_DECISIONS.md**: 10 validated architecture decisions
- **contracts/sync.schema.json**: Bidirectional sync protocol

### Generate Tasks

```bash
# Generate tasks.md from specifications
/speckit.tasks
```

This creates a structured task breakdown organized by user story with dependencies and parallel execution opportunities.

---

## Key Architecture Decisions

1. **Authentication**: ASP.NET Core Identity + OpenIddict (OAuth2/OIDC)
2. **Mobile Framework**: React Native (rejected .NET MAUI for reliability)
3. **Mobile Scope**: Field sample collection ONLY (no test entry/reporting)
4. **Conflict Resolution**: Last-Write-Wins with automatic backup
5. **TypeScript Generation**: NSwag auto-generates from OpenAPI (eliminates contract drift)
6. **API Versioning**: `/api/v1/` prefix for all endpoints
7. **Test Methods**: Enumeration (7 standard methods + Other)
8. **Audit Archival**: 90-day hot/cold split with nightly background job

See `specs/001-water-quality-platform/ARCHITECTURE_DECISIONS.md` for full details.

---

## Project Status

- **Specifications**: ✅ Complete (v1.2)
- **Architecture**: ✅ Validated
- **Implementation**: ⏳ Not started (ready to begin)
- **Branch**: `001-water-quality-platform` (monolithic feature approach)

---

## Next Steps

1. Generate task breakdown: `/speckit.tasks`
2. Create beads issues from tasks
3. Start implementation with backend API setup
