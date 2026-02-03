namespace Quater.Shared.Enums;

/// <summary>
/// Defines entity types for audit logging and conflict resolution.
/// Replaces magic strings with type-safe enum references.
/// </summary>
public enum EntityType
{
    Lab = 1,
    Sample = 2,
    TestResult = 3,
    Parameter = 4,
    AuditLog = 5,
    AuditLogArchive = 6,
}
