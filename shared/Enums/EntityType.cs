namespace Quater.Shared.Enums;

/// <summary>
/// Defines entity types for audit logging and conflict resolution.
/// Replaces magic strings with type-safe enum references.
/// </summary>
public enum EntityType
{
    Lab = 1,
    User = 2,
    Sample = 3,
    TestResult = 4,
    Parameter = 5,
    AuditLog = 6,
    AuditLogArchive = 7,

}
