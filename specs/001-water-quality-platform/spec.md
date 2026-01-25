# Feature Specification: Water Quality Lab Management System

**Feature Branch**: `001-water-quality-platform`  
**Created**: 2025-01-25  
**Status**: Draft  
**Input**: Comprehensive strategic plan for open-source, cross-platform water quality monitoring and lab analysis platform

---

## Executive Summary

This specification defines the Water Quality Lab Management System (WQLMS) - an open-source, cross-platform solution for water quality monitoring and lab analysis targeting small labs, municipalities, and educational institutions in Morocco and beyond. The system comprises three integrated applications (Desktop, Mobile, Backend) with offline-first architecture, bidirectional data synchronization, and built-in compliance reporting for WHO and Moroccan water quality standards.

**Key Characteristics**:
- Cross-platform support (Windows, Linux, macOS desktop; Android mobile)
- Offline-first architecture with SQLite local storage
- Bidirectional sync with conflict resolution
- Extensible parameter system for water quality standards
- Audit trails for regulatory compliance
- Open-source (GNU AGPL) with consulting revenue model

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Lab Technician Collects Field Samples (Priority: P1)

A lab technician visits a water source (well, municipal tap, river) to collect samples for testing. They need to quickly create sample records, capture photos, and generate tracking identifiers without relying on internet connectivity.

**Why this priority**: Field data collection is the foundation of the entire system. Without this, no data enters the system. This is the primary use case for the mobile app and directly supports the core business value.

**Independent Test**: Can be fully tested by: (1) Creating a sample record offline, (2) Attaching a photo, (3) Generating a barcode/QR code, (4) Verifying data persists locally. Delivers: Field technicians can work independently in areas with poor connectivity.

**Acceptance Scenarios**:

1. **Given** a technician is at a water source with no internet, **When** they open the mobile app, **Then** the app loads previously synced data and allows sample creation
2. **Given** a technician creates a new sample, **When** they enter sample type (drinking water, wastewater, surface water), location, and collection time, **Then** the system generates a unique barcode/QR code
3. **Given** a technician captures a photo of the sample, **When** they attach it to the sample record, **Then** the photo is stored locally mple
4. **Given** a technician completes sample entry, **When** they return to an area with connectivity, **Then** they can manually trigger sync to upload the sample to the backend

---

### User Story 2 - Lab Manager Enters Test Results and Validates Data (Priority: P1)

A lab manager receives samples from the field, performs water quality tests (pH, turbidity, chlorine, bacteria, etc.), and enters results into the desktop application. They need to validate results against WHO and Moroccan standards, flag non-compliant samples, and track test hist

**Why this priority**: Test result entry is the core lab workflow. This is where data quality is ensured and compliance is tracked. Without this, the system cannot generate meaningful reports or identify water quality issues.

**Independent Test**: Can be fully tested by: (1) Creating a sample in the system, (2) Entering test results with multiple parameters, (3) Validating against standards, (4) Viewing historical results. Delivers: Lab managers can efficiently record and validate water quality data with built-in compliance checking.

**Acceptance Scenarios**:

1. **Given** a lab manager has a sample record, **When** they select "Enter Test Results", **Then** the system displays a form with WHO + Moroccan standard parameters (pH, turbidity, chlorine, bacteria, etc.)
2. **Given** a lab manager enters a test result value, **When** the value exceeds compliance thresholds, **Then** the system highlights the result in red and displays the threshold
3. **Given** a lab manager completes test entry, **When** they save the results, **Then** the system records the timestamp, technician name, and test method
4. **Given** a lab manager views a sample, **When** they click "History", **Then** they see all previous test results for th sample with dates and technician names

---

### User Story 3 - Lab Manager Generates Compliance Reports (Priority: P1)

A lab manager needs to generate reports for regulatory compliance, showing which samples passed/failed standards, trends over time, and recommendations. These reports must be exportable as PDFs for submission to authorities or sharing with stakeholders.

**Why this priority**: Compliance reporting is the primary deliverable for regulatory bodies and stakeholders. This directly supports the consulting revenue model and is essential for market adoption in regulated environments.

**Independent Test**: Can be fully tested by: (1) Entering multiple test results, (2) Generating a compliance report, (3) Exporting as PDF, (4) Verifying report contains required data. Delivers: Lab managers can quickly generate regulatory-compliant reports without manual compilation.

**Acceptance Scenarios**:

1. **Given** a lab manager has multiple test results in the system, **When** they select "Generate Report", **Then** the system displays options for date range, sample type, and report format
2. **Given** a lab manager selects a date range, **When** they generate a report, **Then** the report shows: sample count, pass/fail breakdown, non-compliant parameters, and trend analysis
3. **Given** a lab manager generates a report, **When** they click "Export as PDF", **Then** the system creates a formatted PDF with lab name, date, and compliance summary
4. **Given** a lab manager views a report, **When** they see non-compliant results, **Then** the report includes WHO/Moroccan standard thresholds for reference

---

### User Story 4 - Lab Administrator Manages Users and Roles (Priority: P2)

A lab administrator needs to create user accounts, assign roles (Admin, Technician, Viewer), and manage permissions. Different roles should have different access levels: Admins can manage users and a, Technicians can enter data, Viewers can only see reports.

**Why this priority**: User management is essential for multi-user labs but not required for initial MVP. Can be implemented after core data collection and entry workflows are working.

**Independent Test**: Can be fully tested by: (1) Creating user accounts with different roles, (2) Logging in as each role, (3) Verifying access restrictions. Delivers: Lab administrators can control who accesses the system and what they can do.

**Acceptance Scenarios**:

1. **Given** an admin is logged in, **When** they navigate to "User Management", **Then** they see a list of existing users with their roles
2. **Given** an admin clicks "Add User", **When** they enter email and select a role, **Then** the system creates the user and sends an invitation
3. **Given** a Technician user is logged in, **When** they try to access "User Management", **Then** the system denies access and shows an error
4. **Given** a Viewer user is logged in, **When** they navigate to the app, **Then** they can only see reports and cannot enter or edit data

---

### User Story 5 - Lab Manager Syncs Data Between Desktop and Backend (Priority: P2)

A lab manager works offline on the desktop app, collecting and entering data. When connectivity is available, they need to sync data with the backend server to: (1) backup data, (2) share data with other labs/authorities, (3) access data from other locations.

**Why this priority**: Sync is critical for the offline-first architecture but can be implemented after core features work locally. Initial MVP can use manual sync; automatic sync can be added later.

**Independent Test**: Can be fully tested by: (1) Creating data offline, (2) Triggering manual sync, (3) Verifying data appears on backend, (4) Handling sync conflicts. Delivers: Lab managers can safely backup and share data while maintaining offline capability.

**Acceptance Scenarios**:

1. **Given** a lab manager has created samples and test results offline, **When** they click "Sync Now", **Then** the system uploads data to the backend
2. **Given** the same sample was edited in two locations, **When** sync occurs, **Then** the system detects the conflict and prompts the user to choose which version to keep
3. **Given** sync completes successfully, **When** the user checks the backend, **Then** all data is visible and searchable
4. **Given** sync fails due to network error, **When** the user retries, **Then** the system resumes from where it left off without duplicating data

---

### User Story 6 - Consultant Customizes System for New Organization (Priority: P3)

A consultant is hired to deploy the system for a new organization with specific water quality parameters (e.g., a mining company testing for heavy metals). They need to: (1) add custom parameters to the system, (2) set compliance thresholds, (3) customize report templates, (4) train staff on the system.

**Why this priority**: Customization is the foundation of the consulting business model but not required for MVP. Initial MVP uses WHO + Moroccan standards; customization can be added in Phase 2.

**Independent Test**: Can be fully tested by: (1) Adding custom parameters, (2) Setting thresholds, (3) Generating reports with custom parameters. Delivers: Consultants can adapt the system for different organizations without code changes.

**Acceptance Scenarios**:

1. **Given** a consultant has admin access, **When** they navigate to "Parameter Configuration", **Then** they can add new water quality parameters with custom names and units
2. **Given** a consultant adds a custom parameter, **When** they set compliance thresholds, **Then** the system uses these thresholds in validation and reporting
3. **Given** a consultant customizes parameters, **When** they generate a report, **Then** the report includes the custom parameters
4. **Given** a consultant trains staff, **When** staff use the system, **Then** they see the customized parameters and thresholds

---

### Edge Cases

- **Offline data loss**: What happens if a technician's phone loses power before syncing? → System should persist data to SQLite; sync resumes when device restarts
- **Conflicting edits**: What if two technicians edit the same sample simultaneously? → System detects conflict during sync and prompts user to resolve
- **Invalid test results**: What if a technician enters a result outside the valid range for a parameter? → System should validate and reject with clear error message
- **Network interruption during sync**: What if sync starts but network drops mid-transfer? → System should resume from checkpoint without duplicating data
- **Expired authentication**: What if a user's session expires while they're working offline? → System should allow offline work; re-authenticate when sync is attempted
- **Concurrent user access**: What if multiple users try to edit the same sample simultaneously? → System should use optimistic locking or last-write-wins with audit trail
- **Large photo attachments**: Whachnician attaches a 10MB photo on a slow connection? → System should compress photos and allow resumable uploads
- **Missing required parameters**: What if a technician tries to save a sample without entering all required fields? → System should validate and show which fields are missing

---

## Requirements *(mandatory)*

### Functional Requirements

#### Core Data Model

- **FR-001**: System MUST support the following water quality parameters: pH, turbidity, chlorine (free/total), bacteria (E. coli, total coliforms), temperature, conductivity, dissolved oxygen, and hardness
- **FR-002**: System MUST store WHO drinking water quality standards and Moroccan water quality standards as configurable thresholds for each parameter
- **FR-003**: System MUST support multiple sample types: drinking water, wastewater, surface water, groundwater, and industrial water
- **FR-004**: System MUST track sample metadata: unique ID, collection date/time, location, sample type, collector name, and photos
- **FR-005**: System MUST record test results with: parameter name, value, unit, test date/time, technician name, test method, and compliance status

#### Desktop Application (Lab Manager Interface)

- **FR-010**: Desktop app MUST provide user authenticatmail/password login
- **FR-011**: Desktop app MUST display a dashboard showing: total samples, pass/fail breakdown, recent test results, and compliance status
- **FR-012**: Desktop app MUST allow lab managers to create, view, edit, and delete sample records
- **FR-013**: Desktop app MUST provide a form for entering test results with validation against WHO + Moroccan standards
- **FR-014**: Desktop app MUST highlight non-compliant results in red with threshold information
- **FR-015**: Desktop app MUST allow searching and filtering samples by: date range, sample type, location, and compliance status
- **FR-016**: Desktop app MUST generate PDF reports showing: sample summary, test results, compliance status, and trends
- **FR-017**: Desktop app MUST support manual sync with backend (upload/download button)
- **FR-018**: Desktop app MUST display sync status and handle sync errors gracefully
- **FR-019**: Desktop app MUST work offline with SQLite database; all features except sync should function without internet
- **FR-020**: Desktop app MUST run on Windows, Linux, and macOS

#### Mobile Application (Field Data Collection)

- **FR-030**: Mobile app MUST allow technicians to create sample records with: sample type, location, collection time, anotes
- **FR-031**: Mobile app MUST generate unique barcode/QR code for each sample
- **FR-032**: Mobile app MUST allow attaching photos to samples
- **FR-033**: Mobile app MUST provide a simplified form for quick test result entry in the field
- **FR-034**: Mobile app MUST display sample history and previously entered results
- **FR-035**: Mobile app MUST support manual sync trigger to upload data to backend
- **FR-036**: Mobile app MUST work offline with SQLite database
- **FR-037**: Mobile app MUST run on Android devices
- **FR-038**: Mobile app MUST display sync status and handle errors

#### Backend API

- **FR-050**: Backend MUST provide RESTful API endpoints for: user authentication, sample CRUD, test result CRUD, and sync operations
- **FR-051**: Backend MUST implement JWT token-based authentication
- **FR-052**: Backend MUST validate all incoming data against WHO + Moroccan standards
- **FR-053**: Backend MUST implement bidirectional sync with conflict resolution (last-write-wins or user-prompted resolution)
- **FR-054**: Backend MUST store data in PostgreSQL database
- **FR-055**: Backend MUST maintain audit logs for all data modifications (who, what, when-056**: Backend MUST generate compliance reports on demand
- **FR-057**: Backend MUST support role-based access control (Admin, Technician, Viewer)
- **FR-058**: Backend MUST provide OpenAPI/Swagger documentation for all endpoints

#### Data Synchronization

- **FR-070**: System MUST implement bidirectional sync: desktop/mobile → backend and backend → desktop/mobile
- **FR-071**: System MUST detect and handle sync conflicts (same record edited in multiple locations)
- **FR-072**: System MUST support manual sync trigger (user clicks "Sync Now")
- **FR-073**: System MUST resume interrupted syncs without duplicating data
- **FR-074**: System MUSTk sync status and timestamp of last successful sync
- **FR-075**: System MUST allow offline work; sync is not required for core functionality

#### Compliance & Audit

- **FR-090**: System MUST maintain audit trail for all data modifications with: user, timestamp, old value, new value
- **FR-091**: System MUST generate compliance reports showing: pass/fail breakdown, non-compliant parameters, and WHO/Moroccan standard thresholds
- **FR-092**: System MUST support exporting reports as PDF
- **FR-093**: System MUST track which samples passed/failed compliance
- **FR-094**: System MUST allow filtering reports by date range, sample type, and location

#### User Management

- **FR-110**: System MUST support user roles: Admin, Technician, Viewer
- **FR-111**: System MUST allow Admins to create, edit, and delete user accounts
- **FR-112**: System MUST enforce role-based permissions: Admins can manage users and view all data, Technicians can enter data, Viewers can only see reports
- **FR-113**: System MUST support password reset functionality
- **FR-114**: System MUST track user activity in audit logs

### Key Entities

- **Sample**: Represents a water sample collected from a specific location at a specific time. Attributes: ID, type (drinking/wastewater/surface/groundwater/industrial), location, collection_date, collector_name, photos, status (pending/completed/archived)

- **TestResult**: Represents a single water quality test performed on a sample. Attributes: ID, sample_id, parameter_name, value, unit, test_date, technician_name, test_method, compliance_status (pass/fail/warning)

- **Parameter**: Represents a water quality parameter (pH, turbidity, chlorine, etc.). Attributes: ID, name, unit, WHO_threshold, moroccan_threshold, min_value, max_value

- **User**: Represents a system user. Attributes: ID, email, password_hash, role (Admin/Technician/Viewer), lab_id, created_date, last_login

- **Lab**: Represents a water quality lab organization. Attributes: ID, name, location, contact_info, created_date

- **SyncLog**: Tracks synchronization events. Attributes: ID, device_id, last_sync_timestamp, status (success/failed/in_progress), error_message

- **AuditLog**: Tracks all data modifications. Attributes: ID, user_id, entity_type, entity_id, action (create/update/delete), old_value, new_value, timestamp

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Lab technicians can create a sample record with photo in under 2 minutes using e app, even without internet connectivity
- **SC-002**: Lab managers can enter test results for a sample in under 3 minutes using the desktop app
- **SC-003**: System can generate a compliance report for 100+ samples in under 10 seconds
- **SC-004**: System successfully syncs 1,000 samples between desktop/mobile and backend without data loss or duplication
- **SC-005**: 95% of test result entries pass validation on first attempt (data quality metric)
- **SC-006**: System handles 10,000+ samples in the database without performance degradation
- **SC-007**: Sync conflicts are resolved correctly 100% of the time (no data corruption)
- **SC-008**: Desktop app works offline for at least 8 hours of continuous use without internet
- **SC-009**: Mobile app works offline for at least 24 hours of continuous use without internet
- **SC-010**: 90% of pilot organization users successfully complete their primary task (sample entry or report generation) on first attempt
- **SC-011**: System uptime is 99.5% during pilot phase (backend availability)
- **SC-012**: Documentation is rated as "clear and helpful" by 80% of pilot users
- **SC-013**: First consulting contract is signed within 3 months of MVP launch
- **SC-014**: System achieves 100+ GitHub stars within 6 months of open-source launch
- **SC-015**: 3-5 pilot organizations are actively using the system by end of Month 3

### Quality Metrics

- **SC-020**: All functional requirements have automated test coverage (unit + integration tests)
- **SC-021**: Code is documented with inline comments for complex logic
- **SC-022**: API endpoints are fully documented in OpenAPI/Swagger format
- **SC-023**: User guide covers all major workflows with screenshots
- **SC-024**: Deployment guide includes Docker setup and step-by-step instructions

---

## Assumptions

1. **Moroccan Standards**: Moroccan water quality standards are available and documented; if not, WHO standards will be used as default with notes for future localization
2. **Pilot Partners**: At least 2-3 pilot organizations in Morocco are willing to test the MVP and provide feedback
3. **Technology Stack**: .NET/Avalonia is the chosen tech stack; ASP.NET Core for backend, Entity Framework for data access, Minimal APIs for API framework
4. **Deployment**: Backend will be deployed on DigitalOcean or similar cloud provider; self-hosted option will be documented
5. **Timeline**: 3-month MVP timeline is realistic given dedicated development effort; assumes no major scope changes
6. **Data Retention**: Test results and samples are retained indefinitely; audit logs are retained for 7 years (standard compliance requirement)
7. **Offline Sync**: Bidirectional sync uses last-write-wins conflict resolution for MVP; user-prompted resolution can be added in Phase 2
8. **Authentication**: JWT tokens expire after 24 hours; refresh tokens are used for long-lived sessions
9. **Compliance**: System focuses on WHO + Moroccan standards; other regional standards can be added in Phase 2
10. **Open Source**: GNU AGPL license is chosen; consulting services are the primary revenue model

---

## Constraints & Dependencies

### Technical Constraints

- Desktop app must support Windows, Linux, macOS (Avalonia framework requirement)
- Mobile app must support Android (Avalonia/MAUI framework limitation; iOS support deferred to Phase 2)
- Offline-first architecture requires SQLite on all clients
- Sync engine must handle network interruptions gracefully
- Backend must support PostgreSQL (no other database engines in MVP)

### Business Constraints

- MVP must be completed in 3 months to meet market window
Consulting revenue model requires system to be customizable without code changes
- Open-source license (GNU AGPL) must be maintained; no proprietary forks allowed
- Pilot organizations must be identified and committed before development starts

### Regulatory Constraints

- System must comply with WHO water quality standards
- System must comply with Moroccan water quality standards (if available)
- Audit trails must be maintained for regulatory compliance
- Data retention policies must follow local regulations

---

## Out of Scope (Phase 2+)

The following features are explicitly excluded from MVP and will be addressed in Phase 2 or later:

- Real-time IoT sensor integration
- Advanced GIS mapping and geospatial analysis
- Multi-language UI (English + French planned for Phase 2)
- Automated alerts and notifications
- Complex predictive analytics and machine learning
- Mobile app for iOS
- Advanced data visualization (charts, graphs beyond basic trends)
- Integration with external lab management systems
- Automated email notifications
- SMS alerts for non-compliant results

---

## Clarification Decisions

The following strategic decisions have been made:

### Decision 1: Moroccan Water Quality Standards

**Decision**: Use WHO standards for MVP; add Moroccan standards in Phase 2

**Rationale**: Faster MVP launch allows market validation and pilot feedback before investing in standards research. WHO standards provide a solid foundation; Moroccan-specific thresholds can be added once pilot organizations provide feedback on their specific needs.

**Implementation Impact**:
- Month 1: Build parameter database with WHO standards only
- Phase 2: Research Moroccan standards and add as configurable thresholds
- System design: Parameter thresholds are configurable, so adding Moroccan standards requires no code changes

---

### Decision 2: Pilot Organization Recruitment

**Decision**: Pilot testing will happen after MVP launch

**Rationale**: MVP will be built based on current assumptions and best practices. Post-launch pilot recruitment allows for a more polished product and clearer value proposition. Pilot feedback will inform Phase 2 features and customization options.

**Implementation Impact**:
- Month 3: MVP launch with documentation and demo materials
- Post-launch: Outreach to internship contacts and potential pilot organizations
- Success metric: 3-5 pilot organizations active by end of Month 4 (1 month post-launch)
- Pilot feedback will guide Phase 2 roadmap

---

### Decision 3: Backend Deployment & Hosting

**Decision**: Self-hosted deployment with Docker

**Rationale**: Lowest cost option; provides maximum control and flexibility. Docker setup guide will enable organizations to deploy on their own infrastructure.

**Implementation Impact**:
- Month 3: Create Docker Compose configuration for backend deployment
- Deployment guide: Step-by-step instructions for self-hosted setup
- Documentation: Include troubleshooting guide for common deployment issues
- Future option: Managed hosting service can be added in Phase 2 if demand exists

---

## Next Steps

1. **Respond to the 3 clarification questions above** - Your answers will refine the specification and inform the planning phase
2. **Validate Moroccan standards** - Research or provide official Moroccan water quality standards documents
3. **Identify pilot organizations** - Reach out to internship contacts to gauge interest and secure commitment
4. **Review specification** - Ensure all user scenarios, requirements, and success criteria align with your vision
5. **Proceed to planning** - Once clarifications are resolved, we'll create a detailed implementation plan with task breakdown, timeline, and resource allocation

---

## Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-25 | AI Assistant | Initial specification based on strategic plan |
