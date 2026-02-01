# Feature Specification: Refactor Shared Models for Consistency and Maintainability

**Feature Branch**: `001-refactor-shared-models`  
**Created**: 2025-01-17  
**Status**: Draft  
**Input**: User description: "I am considering updating @shared/Models/ to consider all aspects of it so we never touch it again, when workin on business logic and implmenting services, there are sevelar issues in the current models, such as unclear properties, magic strings which can introduce bugs, /home/abdssamie/ChemforgeProjects/Quater/.opencode/prompts/csharp-coding-style.txt do our models follow these patterns"

## Clarifications

### Session 2025-01-17

- Q: Once a TestResult is marked as final/submitted, can it ever be modified, or must it be immutable for compliance/audit purposes? → A: Immutable after submission - once marked final, cannot be modified (only voided/replaced)
- Q: Should location data be kept as separate primitive properties, or consolidated into a strongly-typed Location value object to prevent invalid coordinates and ensure consistency? → A: Strongly-typed Location value object - consolidate into single immutable Location type with validation
- Q: What should be the default conflict resolution strategy when a Sample or TestResult is modified offline on multiple devices simultaneously? → A: Server wins - server data takes precedence, client changes saved to ConflictBackup for review
- Q: Should test measurement data (parameter name, value, unit) be consolidated into strongly-typed value objects to prevent invalid combinations and ensure consistency? → A: Strongly-typed Measurement value object - consolidates parameter reference, value, and unit with validation
- Q: What is the exact retention period before audit logs are archived, and should this be enforced at the model level? → A: 90 days active, then archive

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Eliminate Property Duplication and Ambiguity (Priority: P1)

As a developer implementing business logic, I need models with clear, non-duplicated properties so that I always know which property to use without confusion or risk of data inconsistency.

**Why this priority**: This is the highest priority because duplicate properties (e.g., `CreatedDate` vs `CreatedAt`, `LastModified` vs `UpdatedAt`) create immediate confusion during development and can lead to data inconsistency bugs where different parts of the codebase update different properties.

**Independent Test**: Can be fully tested by reviewing all model definitions and verifying that each piece of information is represented by exactly one property with a clear, unambiguous name. Delivers immediate value by eliminating confusion during development.

**AcceptancScenarios**:

1. **Given** a model implements IAuditable interface, **When** a developer needs to track creation time, **Then** there is exactly one property for this purpose with a clear name
2. **Given** a model implements IConcurrent interface, **When** a developer needs to handle optimistic concurrency, **Then** there is exactly one concurrency mechanism (not multiple overlapping ones like Version + RowVersion)
3. **Given** a model has audit tracking properties, **When** a developer reads the model definition, **Then** each property's purpose is immediately clear from its name and documentation
4. **Given** multiple models implement the same interface, **When** comparing their property implementations, **Then** all models use consistent property names for the same concepts

---

### User Story 2 - Replace Magic Strings with Type-Safe Alternatives (Priority: P2)

As a developer working with audit logs and conflict resolution, I need type-safe entity type references instead of magic strings so that refactoring is safe and typos are caught at compile time rather than runtime.

**Why this priority**: Magic strings like "Sample", "TestResult" in EntityType properties are error-prone and break during refactoring. This is while it causes bugs, the system can still function with careful manual testing, unlike P1 which causes immediate confusion.

**Independent Test**: Can be tested by attempting to reference an entity type in code and verifying that the compiler catches any typos or invalid references. Delivers value by eliminating an entire class of runtime bugs.

**Acceptance Scenarios**:

1. **Given** a developer needs to log an audit entry for a Sample entity, **When** they specify the entity type, **Then** the compiler prevents them from using an invalid or misspelled entity type name
2. **Givity class is renamed, **When** the code is recompiled, **Then** all references to that entity type in audit logs and conflict backups are automatically updated or cause compilation errors
3. **Given** a developer is writing code that filters audit logs by entity type, **When** they reference an entity type, **Then** they have IDE autocomplete support showing all valid entity types
4. **Given** the system processes an audit log entry, **When** it needs to determine the entity type, **Then** there is no possibility of encountering an unrecognized type string

---

### User Story 3 - Align Models with Established Coding Patterns (Priority: P3)

As a developer maintaining the codebase, I need models that follow established coding standards so that the codebase is consistent, predictable, and easier to maintain over time.

**Why this priority**: This is P3 because while consistency improves long-term maintainability, the current models are functional. However, inconsistency with coding standards creates technical debt that compounds over time.

**Independent Test**: Can be tested by running automated code analysis against the established coding standards document and verifying 100% compliance. De by reducing cognitive load when working across different parts of the codebase.

**Acceptance Scenarios**:

1. **Given** the coding standards specify immutability preferences, **When** a model is defined, **Then** it uses appropriate immutability patterns for data that shouldn't change after creation
2. **Given** the coding standards specify nullability handling, **When** a model property can be null, **Then** it is explicitly marked as nullable with proper documentation
3. **Given** the coding standards specify avoiding primitive obsession, **When** a model uses identifiers or value types, **Then** they are wrapped in strongly-typed value objects where appropriate
4. **Given** multiple models share common patterns (audit tracking, soft delete, etc.), **When** reviewing their implementations, **Then** they all implement these patterns consistently according to the coding standards

---

### Edge Cases

- What happens when existing data in the database has values in deprecated duplicate properties that need to be migrated?
- How does the system handle backward compatibility if external systems reference old property names?
- What happens when a model needs to support both old and ty names during a transition period?
- How are existing serialized objects (in JSON format in audit logs) handled when property names change?
- How are voided/replaced TestResults tracked and linked to maintain audit trail when corrections are needed?
- What prevents accidental modification of submitted TestResults at the database or application layer?
- How are invalid GPS coordinates (out of range latitude/longitude) prevented from being stored?
- What happens when a Sample is collected at a location without GPS coordinates (e.g., indoor lab testing)?
- How are sync conflicts communicated to users when server data overwrites their offline changes?
- What happens if a conflict occurs on a submitted (immutable) TestResult during sync?
- How are invalid parameter/unit combinations prevented (e.g., measuring pH in kilograms)?
- What happens when a TestResult references a Parameter that doesn't exist in the system?
- How are audit logs older than 90 days automatically archived without data loss?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Models MUST eliminate all duplicate properties that represent the same information (e.g., CreatedDate vs CreatedAt)
- **FR-002**: Mouse exactly one property name for each piece of information, with the name clearly indicating its purpose
- **FR-003**: Models MUST use type-safe references for entity types instead of string literals in properties like EntityType
- **FR-004**: Models implementing IAuditable MUST use consistent property names (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy) across all models
- **FR-005**: Models implementing IConcurrent MUST use a single, consistent concurrency mechanism (not multiple overlapping mechanisms)
- **FR-006**: Models MUST follow the established coding standards regarding immutability, nullability, and type safety
- **FR-007**: Models MUST use strongly-typed value objects for identifiers and domain-specific values to avoid primitive obsession
- **FR-008**: Models MUST have explicit nullability annotations on all properties, with nullable properties only where null is a valid business state
- **FR-009**: Models MUST use collection types that align with their mutability requirements (immutable collections for data that shouldn't change)
- **FR-010**: Models MUST be sealed by default unless inheritance is specifically designed for
- **FR-011**: Models MUST use init-only properties for data that should only be set during construction
- **FR-012**: All property names MUST be refereusing nameof operator in code to enable safe refactoring
- **FR-013**: TestResult model MUST support immutability after submission to comply with regulatory audit requirements (corrections require voiding and creating new records, not editing existing ones)
- **FR-014**: Sample model MUST use a strongly-typed Location value object that validates coordinate ranges (latitude: -90 to 90, longitude: -180 to 180) and consolidates location-related data (coordinates, description, hierarchy)
- **FR-015**: ConflictBackup model MUST support "Server wins" as the default conflict resolution strategy, preserving client changes for manual review while applying server data
- **FR-0esult model MUST use a strongly-typed Measurement value object that consolidates parameter reference, measured value, and unit with validation to prevent invalid parameter/unit combinations
- **FR-017**: AuditLog model MUST support automatic archival to AuditLogArchive after 90 days of retention in active storage

### Key Entities

The following entities require refactoring:

- **Lab**: Represents a water quality lab organization. Currently has duplicate properties (CreatedDate vs CreatedAt) and uses mutable collections.
- **User**: Represents a system user with role-based access. Extends IdentityUser and implements IAuditable and IConcurrent.
- **Sample**: Represents a water sample. Has duplicate properties (CreatedDate vs CreatedAt, LastModified vs UpdatedAt, Version vs RowVersion) and uses string for CreatedBy/LastModifiedBy. Location data (latitude, longitude, description, hierarchy) should be consolidated into a strongly-typed Location value object with coordinate validation.
- **TestResult**: Represents a water quality test. Has the most duplication with CreatedDate, CreatedAt, LastModified, UpdatedAt, Version, RowVersion, and multiple user ID properties. Must support immutability after submission for regulatory compliance (requires status tracking and void/replacement mechanism). Measurement data (ParameterName, Value, Unit) should be consolidated into strongly-typed Measurement value object with validation.
- **Parameter**: Represents a water quality parameter with compliance thresholds. Has duplicate timestamp properties.
- **AuditLog**: Tracks data modifications. Uses magic string for EntityType property. Must support 90-day retention before archival.
- **AuditLogArchive**: Archived audit logs. Uses magic string for EntityType property.
- **ConflictBackup**: Stores backup copies of conflicting records. Uses magic string for EntityType property and has inconsullability on UpdatedBy. Must clearly indicate default resolution strategy is "Server wins" for regulatory compliance.
- **SyncLog**: Tracks synchronization between clients and server. Uses string for UserId instead of strongly-typed identifier.

### Value Objects

New strongly-typed value objects to be introduced:

- **Location**: Encapsulates GPS coordinates (latitude, longitude), human-readable description, and hierarchical path. Validates coordinate ranges at construction and prevents invalid state.
- **Measurement**: Encapsulates parameter reference (linked to Parameter entity), measured value, and unit of measurement. Validates that parameter/unit combinations are valid (e.g., pH must be measured in pH units, not kg) and enforces value ranges per parameter definition.

### Data Consistency Requirements

- **DR-001**: When duplicate properties are removed, existing data MUST be preserved through migration
- **DR-002**: Property renames MUST maintain data integrity during transition
- **DR-003**: Type-safe entity type references MUST map correc to existing string values in the database
- **DR-004**: TestResult immutability enforcement MUST prevent modifications to submitted records at both application and database levels
- **DR-005**: Existing Sample records with separate location properties MUST be migrated to Location value object structure with validation
- **DR-006**: Default conflict resolution strategy (Server wins) MUST be applied consistently across all syncable entities to maintain regulatory compliance
- **DR-007**: Existing TestResult records with separate ParameterNamnit properties MUST be migrated to Measurement value object structure with validation against Parameter definitions
- **DR-008**: AuditLog records older than 90 days MUST be automatically moved to AuditLogArchive without data loss

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero duplicate properties exist across all models (each piece of information has exactly one property)
- **SC-002**: Zero magic strings used for entity type references (100% type-safe references)
- **SC-003**: 100% of models comply with the established coding standards document
- **SC-004**: Zero compilation warnings related to nullability across all model definitions
- **SC-005**: Developers can identify the correct property to use for any given purpose in under 5 seconds (measured through developer surveys)
- **SC-006**: Code refactoring tools (rename, find references) work correctly for 100% of model properties and entity type references
- **SC-007**: Zero runtime errors related to typos in entity type strings after refactoring
- **SC-008**: All interface implementations (IAuditable, ISoftDelete, ISyncable, IConcurrent) use identical property names across all models
- **SC-009**: Submitted TestResults cannot be modified through any application code path (100% immutability enforcement)
- **SC-010**: Invalid GPS coordinates (latitude outside -90 to 90, longitude outside -180 to 180) cannot be stored in Sample records (100% validation enforcement)
- **SC-011**: Sync conflicts default to "Server wins" strategy with 100% consistency across all conflict scenarios
- **SC-012**: Invalid parameter/unit combinations cannot be stored in TestResult records (100% validation enforcement at construction)
- **SC-013**: AuditLog records are automatically archived after exactly 90 days with 100% data preservation

## Assumptions

- The established coding standards document at `/home/abdssamie/ChemforgeProjects/Quater/.opencode/prompts/csharp-coding-style.txt` is the authoritative source for coding patterns
- Existing database schema can be modified through migrations to support property changes
- External systems consuming these models can be updated or adapters can be provided during transition
- The system uses Entity Framework or similar ORM that supports data annotations and migrations
- Backward compatibility can be maintained through a transition period if needed
- The primary goal is long-term maintainability over convenience
- Water quality lab operations require regulatory compliance with immutable test records after submission
- GPS coordinates follow standard WGS84 coordinate system (latitude: -90 to 90, longitude: -180 to 180)
- Server data is considered more authoritative than client data for regulatory compliance purposes
- Parameter entity definitions include valid units and value ranges for each water quality parameter
- 90-day audit log retention meets regulatory compliance requirements for the water quality domain

## Dependencies

- Database migration system must be available to handle schema changes
- All code that references the models must be updated to use new property names
- Serialization/deserialization logic may need updates to handle property name changes
- Any API contracts exposing these models may need versioning or compatibility layers
- Business logic layer must enforce TestResult immutability rules after submission
- ORM must support value object mapping for Location and Measurement types
- Sync engine must implement "Server wins" conflict resolution strategy consistently
- Parameter entity definitions must be complete with valid units and ranges before TestResult migration
d job or background service must exist to perform 90-day audit log archival

## Out of Scope

- Changes to business logic or behavior (only model structure and naming)
- Performance optimization of database queries
- Changes to the interface definitions themselves (IAuditable, ISoftDelete, etc.)
- Addition of new models or removal of existing models
- Changes to the database schema beyond what's needed for property consolidation
- Migration of historical data in audit logs (old JSON serialized data can remain as-is)
- Implementation of the void/replacement workflow for TestResults (model must support it, but workflow is separate feature)
- Geographic coordinate system conversions or transformations (assumes WGS84 standard)
- User notification system for sync conflicts (models must support tracking, but UI/notification is separate)
- Population or validation of Parameter entity definitions (assumes they already exist and are correct)
- Implementation of the 90-day archival background job (model must support it, but job scheduling is separate)
