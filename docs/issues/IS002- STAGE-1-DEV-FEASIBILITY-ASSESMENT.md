---
Title: Stage 1 Operational Setup & Master Data Foundation — Feasibility Assessment
Code: CR-STAGE-1
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-19
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feasibility assessment for **CR-STAGE-1** ([IS002- STAGE-1-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-ISSUE.md)): implementing **Stage 1 ("Operational Setup & Master Data Foundation")** of the ELSASA-SIS v4 development roadmap.

Stage 1 builds directly upon the foundational infrastructure, shared kernel primitives, authentication pipeline, and initial academic cycle slice delivered in Stage 0. It establishes the core master data foundation across three bounded contexts:
1. **School Operational Setup (`opr`):** School calendar day classifications/holidays (`UC-ADM-02`), physical room/facility inventory (`UC-ADM-03`), operational bulletin announcements (`UC-ADM-05`), and role-scoped calendar and bulletin browsing (`UC-ADM-27`, `UC-PRC-07`, `UC-TCH-11`).
2. **Student Lifecycle & Master Data (`std`):** Prospective student applicant intake (`UC-ADM-06`), unified admissions review and principal approval/rejection (`UC-ADM-07`, `UC-PRC-01`), atomic enrollment with sequential NIS allocation (`UC-ADM-08`), ongoing student profile and guardian maintenance (`UC-ADM-09`), annual study group (rombel) configuration with homeroom assignment (`UC-ADM-10`), cohort placement (`UC-ADM-11`), and role-scoped student roster directories (`UC-ADM-12`, `UC-HRM-05`).
3. **Teaching Master Data (`tch`):** Teacher profile records (`UC-ADM-13`), subject capability portfolio mappings (`UC-ADM-14`), study group subject teaching allocations (`UC-ADM-15`), teacher roster and assignment directory (`UC-ADM-16`), and leadership workload distribution oversight (`UC-PRC-05`).

Referenced upstream artifacts:

- ISSUE: [IS002- STAGE-1-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-ISSUE.md) (`CR-STAGE-1`)
- DOMAINS: [operational-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/operational-domain-basic.md), [student-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/student-domain-basic.md), [teaching-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/teaching-domain-basic.md)
- ARCHITECTURE: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md), [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md), [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md), [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md), [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md), [teaching-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/teaching-tables-design.md), [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md), [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)
- PRODUCT SPECS: [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md), [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md), [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md), [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md)
- PREVIOUS STAGE: [IS001- STAGE-0-DEV-FEASIBILITY-ASSESMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-FEASIBILITY-ASSESMENT.md), [IS001- STAGE-0-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-ARCHITECTURE.md), [IS001- STAGE-0-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-IMPLEMENTATION-PLAN.md)

## Objective

Identify all implementation gaps, cross-document discrepancies, schema additions, and unresolved business/architectural questions for `CR-STAGE-1`, and close every decision through stakeholder grilling (`/grill-me`) so downstream `ica-architect` updates, `IMPLEMENTATION-PLAN` creation, and autonomous `ica-developer` slice execution require zero guesswork.

---

# 2. Current State

## Existing Behavior

- **Codebase State:** Stage 0 ("Walking Skeleton & Reference Slice") is implemented, verified, and complete (`COMPLETED`).
  - **Backend (`src/backend`):**
    - `ElsasaSis.SharedKernel`: Common `Result<T>`, `DomainError`, `UuidV7`, `AggregateRoot`, `Entity`, `IEndpoint`, `IDomainEventDispatcher`, `ISqlConnectionFactory`, `ICurrentUser`, and `PaginatedResponse<T>`.
    - `ElsasaSis.Contracts`: Cross-module interfaces and events (`IOperationalContract`, `ActiveAcademicYearDto`, `ActiveOperationalPeriodDto`, `AcademicYearActivatedDomainEvent`, `OperationalPeriodActivatedDomainEvent`).
    - `ElsasaSis.Database`: DbUp runner containing schemas `001_CreateSchemas.sql` (all 8 schemas), `002_CreateAuthTables.sql` (`users`, `user_roles`, `refresh_tokens`, `user_actors`), `003_CreateOutboxTables.sql` (`messages`), `010_CreateOprAcademicYearTables.sql` (`academic_years`, `operational_periods` with `status`), and idempotent default admin user seeding (`admin` / `Admin123!`).
    - `ElsasaSis.Modules.Auth`: JWT login, refresh token cookies (`HttpOnly`), logout, current user claims mapping (`sub`, `roles`, `permissions`, `teacher_id`, `student_id`, `guardian_id`).
    - `ElsasaSis.Modules.Opr`: Academic Year and Operational Period CRUD and activation vertical slices (`UC-ADM-01`).
    - `ElsasaSis.Api`: Minimal API pipeline with RFC 7807 problem details, fluent validation filter with camelCase keys, Swagger/OpenAPI metadata, and CORS/cookie middleware.
    - `ElsasaSis.ArchitectureTests`: NetArchTest suite enforcing module isolation, Dapper-only pure persistence, and EF Core prohibition.
  - **Frontend (`src/frontend`):**
    - Vue 3 + Vite + TypeScript + Tailwind CSS v4 + PrimeVue v4 Aura.
    - App layout with responsive collapsible sidebar, top bar active academic year/semester context switcher (`appContext.store.ts`), Pinia auth store with in-memory access token + automatic silent refresh on `401`.
    - `features/auth`: `LoginView.vue` (`/auth/login`).
    - `features/operations/calendar`: `CalendarWorkspaceView.vue` (`/operations/calendar`) with academic year/semester CRUD drawer, status pills, and activation workflows.
- **Local Toolchain:** .NET 8 SDK (`net8.0`), Node.js `v22`, `pnpm` `11.9`, Microsoft SQL Server (`MSSQLSERVER` / `MSSQLLocalDB`).

## Existing Constraints

1. **Modular Monolith & VSA + DDD Isolation:** Module assemblies (`ElsasaSis.Modules.Std`, `ElsasaSis.Modules.Tch`, `ElsasaSis.Modules.Opr`) must never reference one another directly (enforced by NetArchTest).
2. **Pure Persistence Layer:** No database triggers, stored procedures, `CHECK` constraints, column `DEFAULT` constraints, or foreign key constraints exist in the database. All invariants, enums, temporal bounds, and relationships are managed in the Application Layer (UUID v7 identifiers, explicit UTC `created_at` / `updated_at`).
3. **CQRS & Thin Handlers:** Write slices mutate Aggregate Roots and persist via Dapper in module Infrastructure repositories. Read slices execute direct Dapper projections to response DTOs.
4. **RFC 7807 Error Contract:** All validation and domain errors map to `ProblemDetails` with `camelCase` field error keys (`422`), `409 Conflict`, `404 NotFound`, `401 Unauthorized`, `403 Forbidden`.
5. **Strict 5-Screen Inventory:** All Stage 1 functionality must map to the 5 designated screens without screen sprawl:
   - `SCR-OPR-CALENDAR` (Full)
   - `SCR-OPR-ANNOUNCEMENTS`
   - `SCR-STD-ADMISSIONS`
   - `SCR-STD-DIRECTORY`
   - `SCR-TCH-ASSIGNMENTS`
6. **Data Scoping & Role Isolation:** Security enforces role-level and data-scoped access:
   - `SCHOOL_ADMIN` has full administrative configuration access.
   - `PRINCIPAL` has executive oversight and admission approval capabilities.
   - `HOMEROOM_TEACHER` is strictly scoped to their assigned study group's roster and guardian contact numbers (`UC-HRM-05`).

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| `GAP-001` | CRITICAL | **Missing Backend Modules & Assemblies:** `ElsasaSis.Modules.Std` and `ElsasaSis.Modules.Tch` do not yet exist in `src/backend/src/Modules/`, and `ElsasaSis.sln` / `ElsasaSis.Api` do not yet register them. |
| `GAP-002` | CRITICAL | **Missing Study Group Homeroom Columns in Schema:** `std.study_groups` in `student-tables-design.md` §3.5 lacks `homeroom_teacher_id` and `homeroom_teacher_name`, which are required by `ELSASA-SIS-ARCHITECTURE.md` §13.1, `screen-inventory.md` line 355, and `UC-HRM-05` to scope homeroom teacher queries to `study_groups.homeroom_teacher_id = currentUser.TeacherId`. |
| `GAP-003` | CRITICAL | **Missing Stage 1 DbUp Migration Scripts:** `src/backend/src/ElsasaSis.Database/Scripts` only contains migrations `001`–`010`. Tables for `opr.calendar_entries`, `opr.rooms`, `opr.announcements`, `std.candidates`, `std.candidate_guardians`, `std.students`, `std.student_guardians`, `std.study_groups`, `std.student_placements`, `tch.teachers`, `tch.teaching_capabilities`, and `tch.teacher_assignments` are not yet created. |
| `GAP-004` | MAJOR | **Academic Domain Dependency for Teaching Capabilities:** `tch.teaching_capabilities` and `tch.teacher_assignments` reference `subject_id` and snapshot `subject_name`, but the Academic domain (`acd.subjects`) is scheduled for Stage 2. Stage 1 lacks a subject lookup source for teacher capability mapping and teaching assignments. |
| `GAP-005` | MAJOR | **Use Case Count & Timetable Discrepancy:** `IS002- STAGE-1-DEV-ISSUE.md` description states "23 actor-driven use cases (`UC-ADM-02..16`, ...)", which sequentially includes `UC-ADM-04` ("Configure Study Group Timetables & Block Rotations"). However, `roadmap.md` §3 and `screen-inventory.md` defer `UC-ADM-04` and timetable scheduling to Stage 2 (`SCR-OPR-TIMETABLE`). Stage 1 scope must strictly resolve to the 20 master data use cases. |
| `GAP-006` | MAJOR | **Room Master Data UI Placement in Stage 1:** `screen-inventory.md` colocated `UC-ADM-03` ("Manage Room Master Data") inside `SCR-OPR-TIMETABLE` (deferred to Stage 2). Stage 1 requires Room master data inventory without introducing an unscheduled 6th screen or premature timetable grid. |
| `GAP-007` | MAJOR | **Unspecified Sequential NIS Generation Strategy:** `IS002` lines 22, 36, and 54 mandate "automatic, sequential NIS allocation" upon candidate enrollment (`UC-ADM-08`), but `student-tables-design.md` and `student-domain-basic.md` define `nis` as an application-supplied string without specifying the exact sequence format or whether administrators can manually override it. |
| `GAP-008` | MAJOR | **Candidate Decision Workflow & Remarks Metadata:** `student-tables-design.md` §3.1 defines `std.candidates` with `decided_at` and `admission_status`, but lacks decision notes (`decision_notes`) and does not explicitly define whether the decision transition is a 1-step direct action by Admin or Principal, or a 2-tier recommendation workflow. |
| `GAP-009` | MINOR | **Missing Role & User Actor Seeds for Verification Gate:** Stage 0 only seeded `admin` (`SCHOOL_ADMIN`). Stage 1 exit criteria require a Principal to approve candidates and a Homeroom Teacher to log in and verify study group roster data scoping (`UC-HRM-05`). Seeding `principal` and `teacher.budi` with `auth.user_actors` linkages is required. |
| `GAP-010` | MINOR | **Frontend Navigation & Feature Folder Alignment:** `AppLayout.vue` currently only contains `operations.calendar` in the navigation menu. Routes and views for `SCR-OPR-ANNOUNCEMENTS` (`/operations/announcements`), `SCR-STD-ADMISSIONS` (`/admissions/queue`), `SCR-STD-DIRECTORY` (`/students/directory`), and `SCR-TCH-ASSIGNMENTS` (`/teachers/assignments`) must be added with role permission gates. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| `OQ-001` | Should Stage 1 strictly exclude `UC-ADM-04` (Study Group Timetables & Block Rotations) and focus on the 20 master data use cases, deferring timetable grid scheduling to Stage 2? | Governs whether `opr.study_group_schedules` / `opr.study_group_timetables` are built in Stage 1 or Stage 2. |
| `OQ-002` | Where should Room Master Data management (`UC-ADM-03`, `opr.rooms`) live in the Stage 1 frontend to maintain the 5-screen minimalism? | Determines the UI placement of room inventory inside `SCR-OPR-CALENDAR` vs a separate screen. |
| `OQ-003` | How should Homeroom Teacher assignment be persisted on Study Groups to enable `study_groups.homeroom_teacher_id = currentUser.TeacherId` scoping for `UC-HRM-05`? | Dictates schema updates to `std.study_groups` and alignment with `student-tables-design.md`. |
| `OQ-004` | How should subjects be provided for Teacher Capabilities (`UC-ADM-14`) and Teaching Assignments (`UC-ADM-15`) in Stage 1 before Stage 2 Curriculum setup is implemented? | Determines whether `acd.subjects` table and seeds are created in Stage 1 DbUp scripts. |
| `OQ-005` | What is the exact sequence format and generation strategy for Student Numbers (NIS) upon candidate enrollment (`UC-ADM-08`)? | Defines `EnrollCandidate` vertical slice logic, sequence calculation, and UI enrollment dialog fields. |
| `OQ-006` | How should the candidate admission decision workflow (`UC-ADM-07`, `UC-PRC-01`) operate between School Admin and Principal in `SCR-STD-ADMISSIONS`? | Governs candidate aggregate status transitions, permissions, and decision remarks fields. |
| `OQ-007` | How should cross-module read queries and foreign schema lookups (e.g., rombel needing teacher name, capabilities needing subject name) be executed? | Governs whether read slices query foreign schemas directly via Dapper or use cross-module event synchronization. |
| `OQ-008` | What seed accounts and role-actor linkages are required in DbUp migrations to satisfy the Stage 1 exit verification gate deterministically? | Defines `099_SeedStage1Data.sql` user credentials, roles, teacher actor links, and initial study group assignments. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| `ASM-001` | Local development and Stage 1 automated tests continue to execute on Windows using local SQL Server (`MSSQLSERVER` / `MSSQLLocalDB`), .NET 8 SDK (`net8.0`), Node.js `v22`, and `pnpm` `11.9`. |
| `ASM-002` | The school deployment operates in single-institution mode (ELSASA-SIS Basic v1.0, single K-12 school, no multi-tenant org unit partitioning). |
| `ASM-003` | Timetable scheduling (`UC-ADM-04`, `SCR-OPR-TIMETABLE`, `opr.study_group_schedules`, `opr.study_group_timetables`) is strictly part of Stage 2; Stage 1 delivers only the physical room inventory (`opr.rooms`) and operational calendar/bulletins. |
| `ASM-004` | Frontend HTTP calls continue using the Vite `/api` reverse proxy to `ElsasaSis.Api` with same-origin `HttpOnly` refresh token cookies and in-memory access tokens. |
| `ASM-005` | Candidate intake in Stage 1 is administrative staff-facing (`SCR-STD-ADMISSIONS`); public web-based self-service applicant registration is an intermediate/enterprise feature outside Basic v1.0. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| `RISK-001` | Missing homeroom teacher columns on `std.study_groups` would prevent homeroom teachers from accessing their scoped study group roster (`UC-HRM-05`), breaking the Stage 1 exit gate. | Failed verification gate and broken role-scoped data isolation. | Closed via `GAP-002` / `OQ-003`: add `homeroom_teacher_id UNIQUEIDENTIFIER NULL` and `homeroom_teacher_name NVARCHAR(150) NULL` to `std.study_groups` table definition and DbUp script. |
| `RISK-002` | Unavailability of subjects in Stage 1 would force implementers to either fake subject capabilities with hardcoded strings or block `UC-ADM-14` / `UC-ADM-15`. | Inconsistent teaching capabilities and downstream rework in Stage 2 curriculum. | Closed via `GAP-004` / `OQ-004`: create `acd.subjects` table in Stage 1 DbUp scripts with standard Indonesian K-12 subject seed (`Matematika`, `Bahasa Indonesia`, etc.) and a minimal read contract. |
| `RISK-003` | Premature implementation of timetable scheduling (`UC-ADM-04`) in Stage 1 would expand scope uncontrollably and cause conflict with Stage 2 timetable architecture. | Schedule delays and duplicate slice development. | Closed via `GAP-005` / `OQ-001`: explicitly scope Stage 1 to the 20 master data use cases, deferring `UC-ADM-04` to Stage 2. |
| `RISK-004` | Duplicate NIS generation under concurrent candidate enrollment could cause database constraint violations (`UQ_students_nis`). | Unhandled 500 errors during peak enrollment periods. | Closed via `GAP-007` / `OQ-005`: calculate sequential NIS inside the atomic enrollment transaction with lock or retry handling, and allow admin pre-check/override in the enrollment UI modal. |
| `RISK-005` | NetArchTest module boundary violations if `Std` or `Tch` projects reference other module assemblies directly. | Failed architecture test gate in CI. | Closed via `GAP-001`: add `ElsasaSis.Modules.Std` and `ElsasaSis.Modules.Tch` to `ElsasaSis.ArchitectureTests`, verifying zero inter-module assembly references. |

---

# 7. Recommendations

All major architectural gaps and open questions were systematically evaluated through structured stakeholder decisioning (`/grill-me` interview on 2026-11-19). The following directions were analyzed and resolved in Section 8:

## Option A (Selected Pragmatic Direction): Cohesive Master Data Foundation with Direct Dapper Queries

1. Focus Stage 1 strictly on the 20 master data and operational foundation use cases, excluding `UC-ADM-04` timetable scheduling.
2. Place Room Master Data (`UC-ADM-03`) as a dedicated "Rooms & Facilities" sub-tab in `SCR-OPR-CALENDAR`, preserving the strict 5-screen inventory.
3. Add `homeroom_teacher_id` and `homeroom_teacher_name` to `std.study_groups`, enabling direct `study_groups.homeroom_teacher_id = currentUser.TeacherId` scoping for `UC-HRM-05`.
4. Create `acd.subjects` table in Stage 1 DbUp migrations with standard Indonesian K-12 subject seeds, providing realistic subject capability and assignment bindings.
5. Provide automatic sequential NIS generation (e.g., `20260001`) with manual administrator override in `SCR-STD-ADMISSIONS`.
6. Implement a unified 1-step admission decision (`REGISTERED` $\rightarrow$ `ACCEPTED` / `REJECTED`) with `decision_notes`, executable by Admin and Principal.
7. Allow direct Dapper SQL queries across foreign schemas in read slices and lookup queries, keeping NetArchTest assembly isolation without unnecessary MediatR event synchronization overhead.
8. Seed realistic verification accounts (`admin`, `principal`, `teacher.budi` linked to 7A) in DbUp.

### Advantages

- Highly pragmatic, minimalist, and explicit.
- 100% compliant with Pure Persistence Layer and Clean Architecture assembly boundaries.
- Satisfies all Stage 1 exit criteria and verified use cases without scope creep.
- Avoids over-engineering event synchronization for read lookups while sharing a single SQL Server database.

### Disadvantages

- Requires updating `student-tables-design.md` to document the new `homeroom_teacher_id` and `homeroom_teacher_name` columns on `std.study_groups`.
- Requires running `acd.subjects` table migration in Stage 1 ahead of Stage 2 curriculum.

## Option B (Rejected): Heavy Event Synchronization & Standalone Room Screen

Create a 6th screen for Rooms, introduce a 2-tier admission workflow (`RECOMMENDED` $\rightarrow$ `ACCEPTED`), forbid cross-schema queries in Dapper, and require MediatR event synchronization across all schemas.

### Disadvantages

- Violates the 5-screen constraint from the roadmap.
- Introduces excessive boilerplate and synchronization friction for simple read lookups.
- Delays Stage 1 completion without providing architectural value.

---

# 8. Gap Closure

## GAP-001

Status: CLOSED

### Decision

Create two new module projects in `src/backend/src/Modules/`:
1. `ElsasaSis.Modules.Std` (`Domain/`, `Infrastructure/`, `Features/`)
2. `ElsasaSis.Modules.Tch` (`Domain/`, `Infrastructure/`, `Features/`)

Register both assemblies in `ElsasaSis.sln`, `ElsasaSis.Api`, and `ElsasaSis.ArchitectureTests` (`AssemblyArchitectureContext.cs` and `ModuleBoundaryTests.cs`), enforcing zero cross-module project references. Expand `ElsasaSis.Modules.Opr` with features for Calendar Entries, Rooms, and Announcements.

### Rationale

Establishes the physical module boundaries required by the Modular Monolith architecture and enables vertical slice development for Student and Teaching contexts.

### Impact

- Backend solution expanded with 2 new projects.
- Architecture test suite updated to assert isolation across `Auth`, `Opr`, `Std`, and `Tch`.

### Architecture Impact

Direct alignment with `ELSASA-SIS-ARCHITECTURE.md` §5.1 and `backend-architecture.md` §2.1.

### Resolved By

Architect & Lead Analyst

### Resolved Date

2026-11-19

---

## GAP-002

Status: CLOSED

### Decision

Add `homeroom_teacher_id UNIQUEIDENTIFIER NULL` and `homeroom_teacher_name NVARCHAR(150) NULL` (snapshot) to `std.study_groups` table definition in `student-tables-design.md` §3.5 and in the Stage 1 DbUp migration `022_CreateStdStudyGroupTables.sql`.

### Rationale

Required by `ELSASA-SIS-ARCHITECTURE.md` §13.1 and `screen-inventory.md` line 355 to assign homeroom teachers to annual rombel and enable direct, secure data scoping for `UC-HRM-05`:
`WHERE sg.homeroom_teacher_id = @TeacherId`.

### Impact

- `std.study_groups` schema includes homeroom teacher identity and display name snapshot.
- `StudyGroup` aggregate root in `ElsasaSis.Modules.Std` manages homeroom teacher assignment.
- `SCR-STD-DIRECTORY` rombel drawer includes a Homeroom Teacher picker.

### Architecture Impact

Resolves the contradiction between `ELSASA-SIS-ARCHITECTURE.md` line 392 and `student-tables-design.md`.

### Resolved By

Architect & Lead Analyst

### Resolved Date

2026-11-19

---

## GAP-003

Status: CLOSED

### Decision

Author phased DbUp SQL migration scripts in `src/backend/src/ElsasaSis.Database/Scripts` for Stage 1:
- `011_CreateOprCalendarAndRoomTables.sql`: `opr.calendar_entries` and `opr.rooms`.
- `012_CreateOprAnnouncementTables.sql`: `opr.announcements`.
- `020_CreateStdCandidateTables.sql`: `std.candidates` and `std.candidate_guardians`.
- `021_CreateStdStudentTables.sql`: `std.students` and `std.student_guardians`.
- `022_CreateStdStudyGroupTables.sql`: `std.study_groups` (with homeroom teacher columns) and `std.student_placements`.
- `030_CreateAcdSubjectTables.sql`: `acd.subjects` (table and standard K-12 subject seed).
- `040_CreateTchTeacherTables.sql`: `tch.teachers` and `tch.teaching_capabilities`.
- `041_CreateTchAssignmentTables.sql`: `tch.teacher_assignments`.
- `099_SeedStage1Data.sql`: Seed test users (`principal`, `teacher.budi`) linked via `auth.user_actors`, and initial 7A study group.

### Rationale

Follows the Stage 0 pattern of phased migration scripts matching Pure Persistence Layer specifications (no foreign keys, UUID v7 clustered PKs, application-supplied UTC audit columns).

### Impact

Database schema runner initializes all required Stage 1 tables on startup in Development.

### Architecture Impact

Aligns with Pure Persistence Layer principles across all 6 table designs.

### Resolved By

Architect & Lead Analyst

### Resolved Date

2026-11-19

---

## GAP-004

Status: CLOSED

### Decision

Create the `acd.subjects` table in Stage 1 via `030_CreateAcdSubjectTables.sql` with a standard Indonesian K-12 seed catalog:
- `Pendidikan Agama dan Budi Pekerti` (`PA-BP`)
- `Pendidikan Pancasila dan Kewarganegaraan` (`PPKN`)
- `Bahasa Indonesia` (`BIND`)
- `Matematika` (`MTK`)
- `Ilmu Pengetahuan Alam` (`IPA`)
- `Ilmu Pengetahuan Sosial` (`IPS`)
- `Bahasa Inggris` (`BING`)
- `Seni Budaya` (`SNB`)
- `Pendidikan Jasmani, Olahraga, dan Kesehatan` (`PJOK`)
- `Informatika` (`INF`)

Expose a minimal read query `GET /api/acd/subjects` and `IAcademicContract` for subject lookups. Defer the full curriculum hierarchy (`acd.subject_stages`, `acd.learning_units`, `SCR-ACD-CURRICULUM`) to Stage 2.

### Rationale

Provides authoritative, real subject IDs and names for Teacher Capabilities (`UC-ADM-14`) and Teaching Assignments (`UC-ADM-15`) without mock data or free-text hacks, paving the way for Stage 2 curriculum seamlessly.

### Impact

`SCR-TCH-ASSIGNMENTS` renders an intuitive subject selector from real database records.

### Architecture Impact

Establishes the `acd.subjects` foundation cleanly in accordance with `academic-tables-design.md`.

### Resolved By

Architect & Lead Analyst

### Resolved Date

2026-11-19

---

## GAP-005

Status: CLOSED

### Decision

Strictly exclude `UC-ADM-04` ("Configure Study Group Timetables & Block Rotations") and timetable grid scheduling from Stage 1. Stage 1 delivers exactly **20 actor-driven use cases** across 5 screens:
- **Operational (`opr`, 6 UCs):** `UC-ADM-02`, `UC-ADM-03`, `UC-ADM-05`, `UC-ADM-27`, `UC-PRC-07`, `UC-TCH-11`.
- **Student (`std`, 9 UCs):** `UC-ADM-06`, `UC-ADM-07`, `UC-PRC-01`, `UC-ADM-08`, `UC-ADM-09`, `UC-ADM-10`, `UC-ADM-11`, `UC-ADM-12`, `UC-HRM-05`.
- **Teaching (`tch`, 5 UCs):** `UC-ADM-13`, `UC-ADM-14`, `UC-ADM-15`, `UC-ADM-16`, `UC-PRC-05`.

### Rationale

Conforms to the approved Development Roadmap (§3 Stage 1 vs Stage 2) and avoids scope creep. Timetable grid scheduling requires active subject stages and student participations, which belong to Stage 2.

### Impact

Development team focuses strictly on the 20 master data slices without building timetable templates.

### Architecture Impact

Eliminates ambiguity in `IS002- STAGE-1-DEV-ISSUE.md` line 24.

### Resolved By

Lead Analyst & Architect

### Resolved Date

2026-11-19

---

## GAP-006

Status: CLOSED

### Decision

Integrate Room Master Data (`UC-ADM-03`, `opr.rooms`) directly into `SCR-OPR-CALENDAR` as a dedicated third tab:
- Tab 1: **Academic Cycle** (`UC-ADM-01`)
- Tab 2: **Calendar & Holidays** (`UC-ADM-02`)
- Tab 3: **Rooms & Facilities** (`UC-ADM-03`)

The Rooms tab provides room listing (code, name, type: `CLASSROOM`, `LABORATORY`, `HALL`, `OTHER`, capacity, active status) and an add/edit drawer.

### Rationale

Preserves the strict 5-screen minimalism without creating a low-density orphan screen. Groups physical institutional facility configuration under the Operations navigation group.

### Impact

- `CalendarWorkspaceView.vue` expanded to host three cohesive operational configuration tabs.
- No new route or top-level navigation item required.

### Architecture Impact

Fully satisfies `ui-ux-guideline.md` screen economy and `screen-inventory.md` pattern consistency.

### Resolved By

Architect & UI/UX Specialist

### Resolved Date

2026-11-19

---

## GAP-007

Status: CLOSED

### Decision

Implement sequential NIS generation with administrator override:
1. When opening the Enrollment dialog for an `ACCEPTED` candidate in `SCR-STD-ADMISSIONS`, the API provides a suggested sequential NIS formatted as:
   `[EnrollmentYear][4-digit Sequence]` (e.g., `20260001`, `20260002`).
2. The sequence is computed as:
   `SELECT COALESCE(MAX(CAST(RIGHT(nis, 4) AS INT)), 0) + 1 FROM std.students WHERE nis LIKE @Prefix + '%'`.
3. The administrator may accept the suggested NIS or edit/override it with an existing school numbering record.
4. On submission, the `EnrollCandidate` command handler validates uniqueness (`UQ_students_nis`) and atomically creates the student record, copies guardians, and marks candidate `ENROLLED`.

### Rationale

Meets real-world Indonesian school administrative workflows where schools often have existing student registration ledger books, while providing zero-friction sequential numbering for standard intake.

### Impact

- Enrollment modal in `SCR-STD-ADMISSIONS` features an editable, pre-filled NIS input with format validation.
- Command handler enforces `BR-STU-001` uniqueness and atomic enrollment.

### Architecture Impact

Closes technical realization ambiguity for `UC-ADM-08` and `student-domain-basic.md`.

### Resolved By

Lead Analyst & Architect

### Resolved Date

2026-11-19

---

## GAP-008

Status: CLOSED

### Decision

1. Add `decision_notes NVARCHAR(500) NULL` to `std.candidates` table in `020_CreateStdCandidateTables.sql`.
2. Implement candidate admission decisions as a unified 1-step state transition:
   `REGISTERED` $\rightarrow$ `ACCEPTED` or `REJECTED`.
3. Both `SCHOOL_ADMIN` (permission `Permissions.Students.Admissions.Decide`) and `PRINCIPAL` (permission `Permissions.Students.Admissions.Approve`) are authorized to record the decision.
4. When a decision is recorded, the handler updates `admission_status`, `decided_at = SYSUTCDATETIME()`, and `decision_notes`.

### Rationale

Pragmatic, minimalist workflow that avoids unnecessary multi-step status bureaucracy while giving leadership and administration full audit traceability and decision notes.

### Impact

- Single command slice `RecordAdmissionDecision` supports both Admin and Principal use cases (`UC-ADM-07`, `UC-PRC-01`).
- `SCR-STD-ADMISSIONS` workbench provides Accept / Reject action buttons with a decision notes modal.

### Architecture Impact

Aligns `student-domain-basic.md` lifecycle directly with `screen-inventory.md` and role specifications.

### Resolved By

Lead Analyst & Architect

### Resolved Date

2026-11-19

---

## GAP-009

Status: CLOSED

### Decision

Add idempotent seeding in `099_SeedStage1Data.sql` for:
1. `principal` user with role `PRINCIPAL` (password `Principal123!`).
2. `teacher.budi` user with roles `TEACHER` and `HOMEROOM_TEACHER` (password `Teacher123!`).
3. An active teacher record in `tch.teachers` for Budi Santoso (NIP: `198501012010011001`, phone: `081234567890`), linked to `teacher.budi` in `auth.user_actors`.
4. An active study group `7A` in `std.study_groups` with `homeroom_teacher_id` pointing to Budi Santoso.
5. An active study group `7B` with no homeroom teacher (or another teacher) to verify isolation.

### Rationale

Enables immediate, fully deterministic manual and automated end-to-end testing of the Stage 1 exit criteria (principal approves admissions, homeroom teacher logs in and sees only their assigned study group's roster and guardian emergency contacts).

### Impact

Test automation and manual verification gates can execute immediately upon deployment without complex manual setup.

### Architecture Impact

Ensures complete compliance with `ELSASA-SIS-ARCHITECTURE.md` §13.1 and `actors-roles-specification-basic.md`.

### Resolved By

Architect & Verification Lead

### Resolved Date

2026-11-19

---

## GAP-010

Status: CLOSED

### Decision

Update `src/frontend/src/app/router/routes.ts` and `src/frontend/src/app/layouts/AppLayout.vue` to add Stage 1 routes and navigation groups:
1. **Admissions (`Group 1`):**
   - Route `/admissions/queue` (`SCR-STD-ADMISSIONS`), View: `AdmissionsQueueView.vue`
   - Permissions: `Permissions.Students.Admissions.View`, `Manage`, `Approve`
2. **Students & Classes (`Group 2`):**
   - Route `/students/directory` (`SCR-STD-DIRECTORY`), View: `StudentDirectoryView.vue`
   - Permissions: `Permissions.Students.Directory.View`, `Manage`, `Permissions.Students.Roster.ViewOwn`
3. **Faculty & Teaching (`Group 3`):**
   - Route `/teachers/assignments` (`SCR-TCH-ASSIGNMENTS`), View: `TeacherAssignmentsView.vue`
   - Permissions: `Permissions.Teaching.Assignments.View`, `Manage`, `Oversight`
4. **Operations & Comms (`Group 6`):**
   - Route `/operations/calendar` (`SCR-OPR-CALENDAR` Full)
   - Route `/operations/announcements` (`SCR-OPR-ANNOUNCEMENTS`), View: `AnnouncementsView.vue`
   - Permissions: `Permissions.Operations.Announcements.View`, `Manage`

### Rationale

Strictly follows the navigation taxonomy and URL structure mandated in `ui-ux-guideline.md` §2.5 and `frontend-architecture.md` §12.1.

### Impact

Sidebar dynamically displays relevant navigation groups based on the logged-in user's role permissions (`SCHOOL_ADMIN`, `PRINCIPAL`, `HOMEROOM_TEACHER`, `TEACHER`).

### Architecture Impact

Ensures consistent UI shell navigation across Stages 0 and 1.

### Resolved By

UI/UX Specialist & Architect

### Resolved Date

2026-11-19

---

## OQ-001

Status: CLOSED

### Decision

Stage 1 strictly excludes `UC-ADM-04`. Timetable scheduling is deferred in its entirety to Stage 2 (`SCR-OPR-TIMETABLE`).

### Rationale

Confirmed by stakeholder interview. Eliminates premature dependencies on subject stages and student participations.

### Impact

Stage 1 scope is bounded to 20 use cases across 5 screens.

### Architecture Impact

Preserves roadmap sequencing integrity.

### Resolved By

Stakeholder & Lead Analyst

### Resolved Date

2026-11-19

---

## OQ-002

Status: CLOSED

### Decision

Room Master Data (`opr.rooms`) is housed in a dedicated tab inside `SCR-OPR-CALENDAR`.

### Rationale

Maintains the exact 5-screen minimalism, groups operational physical setup under Operations, and prevents orphan routes.

### Impact

`CalendarWorkspaceView.vue` hosts three tabs: Academic Cycle, Calendar & Holidays, and Rooms & Facilities.

### Architecture Impact

Zero screen sprawl; compliant with `screen-inventory.md`.

### Resolved By

Stakeholder & Architect

### Resolved Date

2026-11-19

---

## OQ-003

Status: CLOSED

### Decision

Add `homeroom_teacher_id UNIQUEIDENTIFIER NULL` and `homeroom_teacher_name NVARCHAR(150) NULL` directly to `std.study_groups`.

### Rationale

Allows direct, highly efficient role-scoped queries (`WHERE homeroom_teacher_id = @TeacherId`) for `UC-HRM-05` and eliminates the need for a separate join table or synthetic assignment type.

### Impact

`std.study_groups` schema and `StudyGroup` aggregate root updated.

### Architecture Impact

Resolves gap against `ELSASA-SIS-ARCHITECTURE.md` §13.1.

### Resolved By

Stakeholder & Architect

### Resolved Date

2026-11-19

---

## OQ-004

Status: CLOSED

### Decision

Create `acd.subjects` table in Stage 1 DbUp scripts with standard Indonesian K-12 subject seed catalog and minimal read query.

### Rationale

Ensures Teacher Capabilities and Teaching Assignments bind to real, validated subject records rather than arbitrary mock data.

### Impact

`030_CreateAcdSubjectTables.sql` created in Stage 1; full curriculum management remains in Stage 2.

### Architecture Impact

Prepares Academic bounded context schema cleanly and predictably.

### Resolved By

Stakeholder & Architect

### Resolved Date

2026-11-19

---

## OQ-005

Status: CLOSED

### Decision

Auto-generate sequential default NIS formatted as `[Year][4-digit sequence]` (e.g., `20260001`) with manual administrator override in the enrollment dialog.

### Rationale

Matches school operational realities where physical numbering books or existing student records may need to be honored while keeping standard enrollment effortless.

### Impact

Backend sequence generation service + enrollment UI dialog with pre-filled editable field.

### Architecture Impact

Explicit realization of `UC-ADM-08` and `BR-STU-001`.

### Resolved By

Stakeholder & Lead Analyst

### Resolved Date

2026-11-19

---

## OQ-006

Status: CLOSED

### Decision

Unified 1-step admission decision (`REGISTERED` $\rightarrow$ `ACCEPTED` or `REJECTED`) with `decision_notes`, executable by both `SCHOOL_ADMIN` and `PRINCIPAL` via permissions.

### Rationale

Pragmatic, minimalist workflow without unnecessary status fragmentation.

### Impact

Single vertical slice handler `RecordAdmissionDecision` serving both admin and principal use cases.

### Architecture Impact

Full alignment with `student-domain-basic.md` state machine.

### Resolved By

Stakeholder & Lead Analyst

### Resolved Date

2026-11-19

---

## OQ-007

Status: CLOSED

### Decision

Read queries and lookup handlers in Dapper are permitted to query foreign schemas directly (e.g., querying `tch.teachers` inside `std` or `opr`), while module assemblies maintain zero direct project references to other module assemblies.

### Rationale

Maximally pragmatic and performant in a modular monolith sharing a single SQL Server database. Avoids unnecessary MediatR synchronization overhead for simple read lookups while preserving compile-time and runtime NetArchTest assembly boundaries.

### Impact

Read slice handlers write direct SQL projections using clean cross-schema `SELECT` statements.

### Architecture Impact

Upholds modular monolith efficiency and VSA read principles.

### Resolved By

Stakeholder & Architect

### Resolved Date

2026-11-19

---

## OQ-008

Status: CLOSED

### Decision

Seed `admin` (`SCHOOL_ADMIN`), `principal` (`PRINCIPAL`), and `teacher.budi` (`TEACHER` + `HOMEROOM_TEACHER`) with `auth.user_actors` linkages in DbUp script `099_SeedStage1Data.sql`.

### Rationale

Guarantees 100% deterministic verification of Stage 1 exit criteria (principal approval, homeroom roster isolation).

### Impact

Zero-friction automated integration testing and immediate manual QA readiness.

### Architecture Impact

Demonstrates end-to-end multi-role authorization and data scoping in action.

### Resolved By

Stakeholder & Architect

### Resolved Date

2026-11-19

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Stage 1 introduces two brand-new bounded context module assemblies (`ElsasaSis.Modules.Std` and `ElsasaSis.Modules.Tch`), major expansions to `ElsasaSis.Modules.Opr`, 8 new database migration scripts defining 12 relational tables, cross-domain snapshot denormalizations, new role-based authorization permissions across 5 roles, and 4 new frontend feature modules across 5 screens. Formal target-state architecture documentation is required to specify the slice inventory, API contracts, domain aggregate roots, and test matrices before autonomous planning and execution can commence.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved (`GAP-001` through `GAP-010` closed)
- [x] All required decisions recorded (`OQ-001` through `OQ-008` closed)
- [x] All blocking open questions resolved via structured stakeholder interview (`/grill-me`)
- [x] Architecture updates defined (schema additions for homeroom teacher, room tab placement, NIS sequence, and subject seed)
- [x] Ready for `ica-architect` to produce `IS002- STAGE-1-DEV-ARCHITECTURE.md`

## Status

**READY-FOR-PLANNING**

## Notes

Feasibility is completely resolved. There are **zero open architectural or business questions** remaining. All decisions are explicit, pragmatic, and minimalist. Downstream `ica-architect` can finalize the Stage 1 Architecture specification, followed by `IMPLEMENTATION-PLAN` generation and autonomous `ica-developer` slice execution without making ad-hoc design decisions.

---

# 11. References

### Upstream Specifications
- ISSUE: [IS002- STAGE-1-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-ISSUE.md)
- ROADMAP: [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md)
- USE CASES: [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md)
- SCREENS: [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md)
- ACTORS: [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md)
- SYSTEM ARCHITECTURE: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
- BACKEND ARCHITECTURE: [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
- FRONTEND ARCHITECTURE: [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
- UI/UX GUIDELINES: [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)
- TABLE DESIGNS:
  - [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md)
  - [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md)
  - [teaching-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/teaching-tables-design.md)
  - [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md)

### Codebase Locations
- Backend Modules: `src/backend/src/Modules/` (`ElsasaSis.Modules.Opr`, `ElsasaSis.Modules.Std`, `ElsasaSis.Modules.Tch`, `ElsasaSis.Modules.Auth`)
- Database Migrations: `src/backend/src/ElsasaSis.Database/Scripts/`
- Contracts: `src/backend/src/ElsasaSis.Contracts/`
- Architecture Tests: `src/backend/tests/ElsasaSis.ArchitectureTests/`
- Frontend Shell & Router: `src/frontend/src/app/`
- Frontend Features: `src/frontend/src/features/` (`operations/`, `students/`, `teachers/`, `auth/`)
