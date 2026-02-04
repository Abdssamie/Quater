# Refactor Soft Delete Logic

## Problem
Services are currently manually implementing soft delete logic (`IsDeleted = true`, `DeletedAt = ...`) instead of relying on the global `SoftDeleteInterceptor`. This causes code redundancy, potential inconsistencies (e.g., DateTime kinds), and unnecessary "TODO" comments.

## Goal
Refactor all `DeleteAsync` methods in services to use `context.Remove()`, delegating the soft delete logic to the existing `SoftDeleteInterceptor`.

## Affected Files
1.  `backend/src/Quater.Backend.Services/ParameterService.cs`
2.  `backend/src/Quater.Backend.Services/LabService.cs`
3.  `backend/src/Quater.Backend.Services/SampleService.cs`
4.  `backend/src/Quater.Backend.Services/TestResultService.cs`

## Plan
For each service:
1.  Locate `DeleteAsync` method.
2.  Replace the manual property setting:
    ```csharp
    entity.IsDeleted = true;
    entity.DeletedAt = ...;
    await context.SaveChangesAsync(ct);
    ```
    with:
    ```csharp
    context.Set<T>().Remove(entity);
    await context.SaveChangesAsync(ct);
    ```

## Verification
1.  Run existing integration tests (which verify `IsDeleted` becomes true).
    *   `LabServiceIntegrationTests`
    *   `ParameterServiceIntegrationTests`
    *   (Create/Run) `SampleServiceIntegrationTests`
2.  Verify `SoftDeleteInterceptor` logic works as expected (tests already exist/pass).
