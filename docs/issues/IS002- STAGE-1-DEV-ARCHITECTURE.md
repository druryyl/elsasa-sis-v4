---
Title: Stage 1 Operational Setup & Master Data Foundation — Target Architecture
Code: CR-STAGE-1
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-19
---

# 1. Overview

This document defines the authoritative target architecture for **CR-STAGE-1** (**Stage 1: Operational Setup & Master Data Foundation**) of **ELSASA-SIS v4 (Basic Level)**, realizing the change request captured in [IS002- STAGE-1-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-ISSUE.md).

Building upon the Stage 0 foundational infrastructure (monorepo layout, ASP.NET Core host, Vue 3 SPA shell, JWT authentication, and Academic Year reference slice), Stage 1 establishes the core institutional master data across three bounded contexts:
1. **School Operational Setup (`opr`):** School calendar day classifications/holidays (`UC-ADM-02`), physical room/facility inventory (`UC-ADM-03`), operational bulletin announcements (`UC-ADM-05`), and role-scoped calendar and bulletin browsing (`UC-ADM-27`, `UC-PRC-07`, `UC-TCH-11`).
2. **Student Lifecycle & Master Data (`std`):** Prospective student candidate intake (`UC-ADM-06`), unified admission review and principal approval/rejection (`UC-ADM-07`, `UC-PRC-01`), atomic enrollment with sequential NIS allocation (`UC-ADM-08`), ongoing student profile and guardian maintenance (`UC-ADM-09`), annual study group (rombel) configuration with homeroom assignment (`UC-ADM-10`), cohort placement (`UC-ADM-11`), and role-scoped student roster directories (`UC-ADM-12`, `UC-HRM-05`).
3. **Teaching Master Data (`tch`):** Teacher profile records (`UC-ADM-13`), subject capability portfolio mappings (`UC-ADM-14`), study group subject teaching allocations (`UC-ADM-15`), teacher roster and assignment directory (`UC-ADM-16`), and leadership workload distribution oversight (`UC-PRC-05`).

Stage 1 realizes exactly **20 actor-driven use cases** across **5 UI screens** (`SCR-OPR-CALENDAR` Full, `SCR-OPR-ANNOUNCEMENTS`, `SCR-STD-ADMISSIONS`, `SCR-STD-DIRECTORY`, and `SCR-TCH-ASSIGNMENTS`), creating two new backend module assemblies (`ElsasaSis.Modules.Std`, `ElsasaSis.Modules.Tch`), expanding `ElsasaSis.Modules.Opr`, and introducing 8 new database migrations defining 12 relational tables.

---

# 2. Architectural Basis

## Business Context

Business concepts, domain invariants, actors, use cases, and screen ergonomics realized by Stage 1 are governed by the upstream product-definition specifications:

- **Domain Knowledge & Invariants:**
  - [operational-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/operational-domain-basic.md) (`CalendarEntry`, `Room`, `Announcement`, `BR-OPS-007`..`BR-OPS-013`).
  - [student-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/student-domain-basic.md) (`Candidate`, `Student`, `StudyGroup`, `Placement`, `Guardian`, `BR-STU-001`..`BR-STU-010`).
  - [teaching-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/teaching-domain-basic.md) (`Teacher`, `TeachingCapability`, `TeacherAssignment`, `BR-TCH-001`..`BR-TCH-008`).
- **Actors & Roles:** [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md) (`SCHOOL_ADMIN`, `PRINCIPAL`, `HOMEROOM_TEACHER`, `TEACHER`).
- **Use Case Taxonomy:** [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (20 in-scope use cases across `opr`, `std`, and `tch`).
- **Screens & UI/UX Standards:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) and [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md) (§2.5 Navigation Taxonomy, §3 Screen Patterns, §8.4 Role Scoping).

## Analysis Input

All architectural gaps (`GAP-001` through `GAP-010`) and open questions (`OQ-001` through `OQ-008`) were formally evaluated and closed in the Feasibility Assessment:

- **Feasibility Assessment:** [IS002- STAGE-1-DEV-FEASIBILITY-ASSESMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-FEASIBILITY-ASSESMENT.md) (`CR-STAGE-1`).
- **Base Architecture & Technical Standards:**
  - [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md) (Modular Monolith, Schemas, Screen Inventory).
  - [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md) (Modular Monolith, VSA, Clean DDD, Dapper Pure Persistence, RFC 7807).
  - [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md) (Vue 3, TanStack Query, Pinia, VeeValidate + Zod, PrimeVue Aura).
  - [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md) (§3.3 `calendar_entries`, §3.4 `rooms`, §3.7 `announcements`).
  - [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md) (§3.1 `candidates`, §3.2 `candidate_guardians`, §3.3 `students`, §3.4 `student_guardians`, §3.5 `study_groups`, §3.6 `student_placements`).
  - [teaching-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/teaching-tables-design.md) (§3.1 `teachers`, §3.2 `teaching_capabilities`, §3.3 `teacher_assignments`).
  - [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md) (§3.1 `acd.subjects` table definition).

```text
DOMAINS + ROADMAP + GUIDELINES (docs/architecture/)
                      +
        IS002 FEASIBILITY-ASSESSMENT
                      ↓
          CR-STAGE-1-ARCHITECTURE
```

---

# 3. Scope

## Included

Stage 1 realizes exactly **20 actor-driven use cases** across **5 UI screens** and supporting backend assemblies:

1. **New Backend Module Assemblies:**
   - `ElsasaSis.Modules.Std` (`Domain/`, `Infrastructure/`, `Features/` for Candidates, Students, Guardians, Study Groups, Placements).
   - `ElsasaSis.Modules.Tch` (`Domain/`, `Infrastructure/`, `Features/` for Teachers, Capabilities, Assignments).
   - Integration into `src/backend/ElsasaSis.sln`, `ElsasaSis.Api`, and `ElsasaSis.ArchitectureTests`.
2. **Expansion of `ElsasaSis.Modules.Opr`:**
   - Slices for Calendar Entries (`ManageCalendarEntries`), Room Master Data (`ManageRooms`), and Announcements (`ManageAnnouncements`, `BrowseAnnouncements`).
3. **Database Migrations (`ElsasaSis.Database`):**
   - `011_CreateOprCalendarAndRoomTables.sql`: `opr.calendar_entries` and `opr.rooms`.
   - `012_CreateOprAnnouncementTables.sql`: `opr.announcements`.
   - `020_CreateStdCandidateTables.sql`: `std.candidates` (with `decision_notes`) and `std.candidate_guardians`.
   - `021_CreateStdStudentTables.sql`: `std.students` and `std.student_guardians`.
   - `022_CreateStdStudyGroupTables.sql`: `std.study_groups` (with `homeroom_teacher_id` and `homeroom_teacher_name`) and `std.student_placements`.
   - `030_CreateAcdSubjectTables.sql`: `acd.subjects` table and standard Indonesian K-12 subjects seed catalog.
   - `040_CreateTchTeacherTables.sql`: `tch.teachers` and `tch.teaching_capabilities`.
   - `041_CreateTchAssignmentTables.sql`: `tch.teacher_assignments`.
   - `099_SeedStage1Data.sql`: Deterministic test accounts (`principal`, `teacher.budi` with `auth.user_actors` linkages, teacher profile, and `7A` study group homeroom assignment).
4. **Minimal Subject Catalog Read Support (`acd`):**
   - Expose minimal `GET /api/acd/subjects` query endpoint for teacher capability selection and assignment dropdowns.
5. **Frontend Screens & Workspaces (5 Screens):**
   - `SCR-OPR-CALENDAR` (Full): 3 cohesive tabs inside `CalendarWorkspaceView.vue` (`/operations/calendar`):
     - Tab 1: Academic Cycle (`UC-ADM-01`)
     - Tab 2: Calendar & Holidays (`UC-ADM-02`, `UC-ADM-27`, `UC-PRC-07`)
     - Tab 3: Rooms & Facilities (`UC-ADM-03`)
   - `SCR-OPR-ANNOUNCEMENTS` (`AnnouncementsView.vue` at `/operations/announcements`):
     - Bulletin feed, audience filtering, draft creation, and publishing workflow (`UC-ADM-05`, `UC-TCH-11`).
   - `SCR-STD-ADMISSIONS` (`AdmissionsQueueView.vue` at `/admissions/queue`):
     - Work Queue pattern (`WorkbenchLayout.vue`): Candidate intake (`UC-ADM-06`), 1-step Accept/Reject decision modal with `decision_notes` (`UC-ADM-07`, `UC-PRC-01`), and atomic Enrollment modal with auto-suggested sequential NIS (`20260001`) and admin override (`UC-ADM-08`).
   - `SCR-STD-DIRECTORY` (`StudentDirectoryView.vue` at `/students/directory`):
     - Tab 1: Student Roster Directory (`UC-ADM-09`, `UC-ADM-12`) with homeroom-scoped data isolation for `HOMEROOM_TEACHER` (`UC-HRM-05`).
     - Tab 2: Study Groups & Placement Registry (`UC-ADM-10`, `UC-ADM-11`) with homeroom teacher picker and student placement modal.
   - `SCR-TCH-ASSIGNMENTS` (`TeacherAssignmentsView.vue` at `/teachers/assignments`):
     - Tab 1: Faculty Directory (`UC-ADM-13`, `UC-ADM-14`, `UC-ADM-16`) with teacher profile management and subject capability tagging.
     - Tab 2: Teaching Allocations & Workload Oversight (`UC-ADM-15`, `UC-PRC-05`) with study group subject assignment and leadership workload distribution matrix.
6. **Authorization & Security:**
   - Compile-time permission catalog expanded in `ElsasaSis.Modules.Auth` for Operations, Students, and Teaching.
   - Dynamic navigation menu filtering in `AppLayout.vue` by role permissions (`SCHOOL_ADMIN`, `PRINCIPAL`, `HOMEROOM_TEACHER`, `TEACHER`).
   - Data scoping enforcement: `study_groups.homeroom_teacher_id = currentUser.TeacherId` on `UC-HRM-05`.

## Excluded

- **Timetable Scheduling & Rotation Blocks (`UC-ADM-04`, `SCR-OPR-TIMETABLE`):** Deferred in its entirety to Stage 2 (`opr.study_group_schedules`, `opr.study_group_timetables`).
- **Academic Domain Features (Curriculum, Attendance, Grading, Report Cards):** Deferred to Stage 2 (`acd.subject_stages`, `acd.learning_units`, `acd.attendance_records`, `acd.assignments`, `acd.report_cards`, `SCR-ACD-*`), except for `acd.subjects` table and seed lookup.
- **Finance & Pastoral Domains:** Deferred to Stages 3 and 4 (`fin.*`, `dev.*`).
- **Public Self-Service Registration Portal:** Candidate intake remains administrative back-office entry (`SCR-STD-ADMISSIONS`) in Basic Level v1.0.
- **Student Status Transitions beyond Active:** Basic Level v1.0 excludes suspensions, dropouts, transfers, promotions, and graduation.

---

# 4. Technical Decisions

Technical realization of approved analysis decisions (`GAP-001`–`GAP-010`, `OQ-001`–`OQ-008`):

1. **Physical Assemblies & Clean Architecture Boundaries (`GAP-001`, `GAP-003`):**
   - Create `ElsasaSis.Modules.Std` and `ElsasaSis.Modules.Tch` targeting `net8.0`.
   - Each module contains three explicit folders: `Domain/` (Aggregate Roots, Entities, Invariants, zero external dependencies), `Infrastructure/` (Dapper Repositories, SQL mapping), and `Features/` (Vertical Slices: `Endpoint`, `Request`, `Response`, `Validator`, `Handler`).
   - Slices inject handlers directly into Minimal API endpoints without MediatR command dispatch.
   - NetArchTest asserts zero direct references between `Modules.Auth`, `Modules.Opr`, `Modules.Std`, and `Modules.Tch`.
2. **Cross-Schema Queries in Read Slices (`OQ-007`):**
   - In accordance with Modular Monolith pragmatism and VSA CQRS principles, Query (Read) slice handlers using Dapper are permitted to execute direct SQL `SELECT` queries across foreign schemas in the shared SQL Server database (e.g., querying `tch.teachers` to populate teacher names in student study group pickers, or querying `acd.subjects` for capability lists).
   - Module assemblies maintain zero compile-time references to each other.
   - Command (Write) handlers never touch foreign schemas; all mutations remain strictly confined to the module's own schema.
3. **Homeroom Teacher Assignment & Data Scoping (`GAP-002`, `OQ-003`, `RISK-001`):**
   - Add `homeroom_teacher_id UNIQUEIDENTIFIER NULL` and `homeroom_teacher_name NVARCHAR(150) NULL` (snapshot) to `std.study_groups` schema and `StudyGroup` aggregate.
   - When a user with `HOMEROOM_TEACHER` role accesses `GET /api/std/study-groups/{id}/roster` (`UC-HRM-05`), the handler asserts:
     ```sql
     SELECT 1 FROM std.study_groups 
     WHERE id = @StudyGroupId AND homeroom_teacher_id = @CurrentUserTeacherId
     ```
     Returning `403 Forbidden` if the requested study group does not belong to the logged-in homeroom teacher.
4. **Sequential Student Number (NIS) Generation Strategy (`GAP-007`, `OQ-005`, `RISK-004`):**
   - When opening the enrollment dialog in `SCR-STD-ADMISSIONS`, the frontend queries `GET /api/std/candidates/{id}/next-nis`.
   - The backend calculates the default sequence:
     ```sql
     SELECT COALESCE(MAX(CAST(RIGHT(nis, 4) AS INT)), 0) + 1 
     FROM std.students 
     WHERE nis LIKE @YearPrefix + '%'
     ```
     Producing `[EnrollmentYear][4-digit Sequence]` (e.g., `20260001`).
   - The UI displays an editable input pre-filled with the suggested NIS, allowing the administrator to accept or override it.
   - On submission, `EnrollCandidate` executes within an atomic SQL transaction:
     - Re-validates uniqueness (`SELECT 1 FROM std.students WHERE nis = @Nis`).
     - Inserts `std.students` record (`status = 'ACTIVE'`).
     - Copies `std.candidate_guardians` into `std.student_guardians`.
     - Updates `std.candidates` (`admission_status = 'ENROLLED'`, `enrolled_student_id = @StudentId`).
     - Dispatches `StudentEnrolledDomainEvent`.
5. **Unified 1-Step Admission Decision Workflow (`GAP-008`, `OQ-006`):**
   - `std.candidates` includes `decision_notes NVARCHAR(500) NULL` and `decided_at DATETIME2(3) NULL`.
   - The state transition is 1-step: `REGISTERED` $\rightarrow$ `ACCEPTED` or `REJECTED`.
   - `RecordAdmissionDecision` slice endpoint: `POST /api/std/candidates/{id}/decision`.
   - Authorized for `SCHOOL_ADMIN` (`Permissions.Students.Admissions.Decide`) and `PRINCIPAL` (`Permissions.Students.Admissions.Approve`), supporting both `UC-ADM-07` and `UC-PRC-01` uniformly.
6. **Pragmatic Subject Catalog Foundation (`GAP-004`, `OQ-004`, `RISK-002`):**
   - Create `acd.subjects` table in Stage 1 via `030_CreateAcdSubjectTables.sql` with the standard Indonesian K-12 subject seed catalog (`MTK`, `BIND`, `BING`, `IPA`, `IPS`, `PPKN`, `PA-BP`, `PJOK`, `SNB`, `INF`).
   - Expose `GET /api/acd/subjects` Minimal API endpoint and `IAcademicContract` read method, allowing `SCR-TCH-ASSIGNMENTS` to populate teacher capabilities and assignments with real database IDs.
7. **Room Inventory UI Placement (`GAP-006`, `OQ-002`):**
   - Implement `opr.rooms` as a dedicated third tab ("Rooms & Facilities") in `SCR-OPR-CALENDAR` (`CalendarWorkspaceView.vue`).
   - Preserves the strict 5-screen minimalism without creating an orphan screen or prematurely introducing timetable grids.
8. **Deterministic Test & QA Seeding (`GAP-009`, `OQ-008`):**
   - DbUp script `099_SeedStage1Data.sql` creates:
     - `principal` user with role `PRINCIPAL` (password `Principal123!`).
     - `teacher.budi` user with roles `TEACHER` and `HOMEROOM_TEACHER` (password `Teacher123!`).
     - Teacher profile in `tch.teachers` for Budi Santoso linked via `auth.user_actors`.
     - Study group `7A` in `std.study_groups` with `homeroom_teacher_id` pointing to Budi Santoso.
     - Study group `7B` with no homeroom teacher assigned to verify roster isolation.

---

# 5. Component Responsibilities

| Component | Assembly / Path | Responsibility & Ownership |
|---|---|---|
| **`BND-MOD-OPR`** | `ElsasaSis.Modules.Opr` | Owns `CalendarEntry`, `Room`, and `Announcement` Aggregate Roots, repositories, and vertical slices for `UC-ADM-02`, `UC-ADM-03`, `UC-ADM-05`, `UC-ADM-27`, `UC-PRC-07`, `UC-TCH-11`. |
| **`BND-MOD-STD`** | `ElsasaSis.Modules.Std` | Owns `Candidate` (with `CandidateGuardian`), `Student` (with `StudentGuardian`), and `StudyGroup` Aggregate Roots, `Placement` entity, repositories, sequential NIS generation, and vertical slices for `UC-ADM-06..12`, `UC-PRC-01`, `UC-HRM-05`. |
| **`BND-MOD-TCH`** | `ElsasaSis.Modules.Tch` | Owns `Teacher` (with `TeachingCapability`) and `TeacherAssignment` Aggregate Roots, repositories, and vertical slices for `UC-ADM-13..16`, `UC-PRC-05`. |
| **`BND-MOD-ACD-READ`** | `ElsasaSis.Modules.Acd` *(or Host read slice)* | Owns `acd.subjects` table query `GET /api/acd/subjects` providing verified subject list for teaching capabilities and assignments. |
| **`BND-CONTRACTS`** | `ElsasaSis.Contracts` | Owns cross-module interfaces (`IStudentContract`, `ITeachingContract`, `IAcademicContract`) and domain event records (`StudentEnrolledDomainEvent`, `StudentPlacedDomainEvent`, `TeacherAssignedDomainEvent`). |
| **`BND-DATABASE`** | `ElsasaSis.Database` | Owns Stage 1 migration scripts (`011` through `099`) and automated execution on API startup. |
| **`BND-API-HOST`** | `ElsasaSis.Api` | Registers new module DI containers, maps Minimal API endpoints, configures permission policies, and handles RFC 7807 problem details. |
| **`ElsasaSis.ArchitectureTests`** | Tests | Enforces NetArchTest boundary isolation across `Auth`, `Opr`, `Std`, and `Tch`, verifying zero inter-module assembly references and EF Core prohibition. |
| **`BND-FE-OPR`** | `features/operations/` | Owns `SCR-OPR-CALENDAR` Full (Academic Cycle, Calendar, Rooms tabs) and `SCR-OPR-ANNOUNCEMENTS` (`AnnouncementsView.vue`). |
| **`BND-FE-STD`** | `features/student/` | Owns `SCR-STD-ADMISSIONS` (`AdmissionsQueueView.vue` with candidate workbench and enrollment modal) and `SCR-STD-DIRECTORY` (`StudentDirectoryView.vue` with student roster, homeroom scoping, study groups, and placement drawers). |
| **`BND-FE-TCH`** | `features/teaching/` | Owns `SCR-TCH-ASSIGNMENTS` (`TeacherAssignmentsView.vue` with faculty directory, capability tagging, teaching allocations, and workload matrix). |
| **`BND-FE-CORE`** | `src/frontend/src/app/` | Owns Vue Router navigation routes, permission guards, top bar context switcher, and role-filtered navigation taxonomy. |

---

# 6. Integration Design

| Source | Target | Transport | Purpose |
|---|---|---|---|
| `BND-FE-OPR` | `BND-API-HOST` (`/api/opr/*`) | HTTP REST (JSON) | Manage calendar entries (`/calendar-entries`), rooms (`/rooms`), announcements (`/announcements`), and view dashboard summaries. |
| `BND-FE-STD` | `BND-API-HOST` (`/api/std/*`) | HTTP REST (JSON) | Register candidates, record decisions, query next NIS, enroll candidates, maintain student profiles/guardians, manage study groups, assign placements, and fetch homeroom-scoped rosters. |
| `BND-FE-TCH` | `BND-API-HOST` (`/api/tch/*`) | HTTP REST (JSON) | Maintain teacher profiles, assign/remove subject capabilities, allocate study group teaching assignments, and query workload distribution. |
| `BND-FE-TCH` / `BND-FE-STD` | `BND-API-HOST` (`/api/acd/subjects`) | HTTP REST (JSON) | Fetch authoritative subject list for capability multi-select and assignment dropdowns. |
| `BND-MOD-STD` (Enroll) | `BND-SHARED-KERNEL` (`IDomainEventDispatcher`) | In-process MediatR | Publish `StudentEnrolledDomainEvent` post-commit. |
| `BND-MOD-STD` (Placement)| `BND-SHARED-KERNEL` (`IDomainEventDispatcher`) | In-process MediatR | Publish `StudentPlacedDomainEvent` post-commit. |
| `BND-MOD-TCH` (Assign) | `BND-SHARED-KERNEL` (`IDomainEventDispatcher`) | In-process MediatR | Publish `TeacherAssignedDomainEvent` post-commit. |
| Read Slices (`Dapper`) | SQL Server (`opr`, `std`, `tch`, `acd`) | Direct SQL (`SELECT`) | Direct cross-schema read projections without assembly dependencies. |

---

# 7. Data Ownership

| Schema / Data Set | Owning Module | Persistence Responsibility |
|---|---|---|
| `opr.calendar_entries` | `ElsasaSis.Modules.Opr` | Exclusive write ownership via `ICalendarEntryRepository`. |
| `opr.rooms` | `ElsasaSis.Modules.Opr` | Exclusive write ownership via `IRoomRepository`. |
| `opr.announcements` | `ElsasaSis.Modules.Opr` | Exclusive write ownership via `IAnnouncementRepository`. |
| `std.candidates`, `std.candidate_guardians` | `ElsasaSis.Modules.Std` | Exclusive write ownership via `ICandidateRepository`. |
| `std.students`, `std.student_guardians` | `ElsasaSis.Modules.Std` | Exclusive write ownership via `IStudentRepository`. |
| `std.study_groups` | `ElsasaSis.Modules.Std` | Exclusive write ownership via `IStudyGroupRepository`. |
| `std.student_placements` | `ElsasaSis.Modules.Std` | Exclusive write ownership via `IStudentPlacementRepository`. |
| `tch.teachers`, `tch.teaching_capabilities`| `ElsasaSis.Modules.Tch` | Exclusive write ownership via `ITeacherRepository`. |
| `tch.teacher_assignments` | `ElsasaSis.Modules.Tch` | Exclusive write ownership via `ITeacherAssignmentRepository`. |
| `acd.subjects` | `ElsasaSis.Modules.Acd` *(seeded)* | Managed in Stage 1 via migration `030`; read access via `ISubjectReadRepository`. |
| Client Auth & Actor Claims (`teacherId`) | Frontend `useAuthStore` | Populated from JWT token claims issued by `ElsasaSis.Modules.Auth`. |

---

# 8. Database Design

Detailed table schemas follow Pure Persistence Layer standards: UUID v7 primary keys, application-supplied UTC `created_at` and `updated_at`, zero foreign key constraints, zero triggers, zero database defaults, and denormalized snapshots for cross-domain display strings.

## New Tables

| Table | Schema | Script | Purpose & Key Columns |
|---|---|---|---|
| `opr.calendar_entries` | `opr` | `011` | School calendar days & holidays. PK: `id`, `academic_year_id`, `entry_date` (DATE), `day_type` (`SCHOOL_DAY` \| `HOLIDAY` \| `CLOSURE`), `description`. UQ: `(academic_year_id, entry_date)`. |
| `opr.rooms` | `opr` | `011` | Physical institutional room inventory. PK: `id`, `name` (NVARCHAR(100)), `room_type` (`CLASSROOM` \| `LABORATORY` \| `HALL` \| `OTHER`), `capacity` (INT). UQ: `(name)`. |
| `opr.announcements` | `opr` | `012` | School announcements and bulletins. PK: `id`, `academic_year_id`, `title`, `content` (NVARCHAR(MAX)), `author_id`, `author_name`, `target_audience` (`ALL` \| `TEACHERS` \| `STUDENTS` \| `PARENTS`), `status` (`DRAFT` \| `PUBLISHED`), `published_at`. |
| `std.candidates` | `std` | `020` | Prospective student applicants. PK: `id`, `admission_year_id`, `admission_year_name`, `full_name`, `nickname`, `date_of_birth`, `gender`, `phone`, `email`, `address`, `target_school_level`, `target_grade`, `admission_status` (`REGISTERED` \| `ACCEPTED` \| `REJECTED` \| `ENROLLED`), `registered_at`, `decided_at`, `decision_notes`, `enrolled_student_id`. |
| `std.candidate_guardians`| `std` | `020` | Pre-enrollment guardian contacts. PK: `id`, `candidate_id`, `full_name`, `relationship_type` (`FATHER` \| `MOTHER` \| `GUARDIAN`), `phone`, `email`, `address`. |
| `std.students` | `std` | `021` | Enrolled official student records. PK: `id`, `candidate_id`, `nis` (UQ), `nisn` (filtered UQ), `full_name`, `nickname`, `date_of_birth`, `gender`, `phone`, `email`, `address`, `enrollment_date`, `status` (`ACTIVE`). |
| `std.student_guardians` | `std` | `021` | Official student guardian contacts. PK: `id`, `student_id`, `full_name`, `relationship_type` (`FATHER` \| `MOTHER` \| `GUARDIAN`), `phone`, `email`, `address`. |
| `std.study_groups` | `std` | `022` | Annual study groups (rombel). PK: `id`, `academic_year_id`, `academic_year_name`, `school_level`, `grade`, `name` (e.g., `7A`), `homeroom_teacher_id` (NULL), `homeroom_teacher_name` (NULL). UQ: `(academic_year_id, name)`. |
| `std.student_placements` | `std` | `022` | Student academic year cohort placement. PK: `id`, `student_id`, `academic_year_id`, `academic_year_name`, `school_level`, `grade`, `study_group_id`, `study_group_name`. UQ: `(student_id, academic_year_id)`. |
| `acd.subjects` | `acd` | `030` | Broad subject catalog. PK: `id`, `name` (NVARCHAR(100), UQ), `code` (NVARCHAR(30), filtered UQ). Seeded with 10 standard Indonesian K-12 subjects. |
| `tch.teachers` | `tch` | `040` | Teacher profiles. PK: `id`, `full_name`, `nickname`, `nip` (filtered UQ), `nik` (filtered UQ), `date_of_birth`, `gender`, `phone`, `email`, `address`, `status` (`ACTIVE` \| `INACTIVE`). |
| `tch.teaching_capabilities`| `tch` | `040` | Teacher qualified subject capabilities. PK: `id`, `teacher_id`, `subject_id`, `subject_name`. UQ: `(teacher_id, subject_id)`. |
| `tch.teacher_assignments`| `tch` | `041` | Subject teaching allocations per study group & period. PK: `id`, `teacher_id`, `teacher_name`, `academic_year_id`, `academic_year_name`, `operational_period_id`, `operational_period_name`, `subject_id`, `subject_name`, `study_group_id`, `study_group_name`, `status` (`ASSIGNED` \| `REMOVED`). UQ: `(teacher_id, subject_id, study_group_id, operational_period_id)`. |

## Modified Tables

None. Stage 0 tables (`auth.*`, `opr.academic_years`, `opr.operational_periods`, `outbox.messages`) remain unchanged.

## Relationships

All inter-table relationships are logical and managed in the Application Layer:
- `opr.calendar_entries.academic_year_id` $\rightarrow$ `opr.academic_years.id`
- `opr.announcements.academic_year_id` $\rightarrow$ `opr.academic_years.id`
- `std.candidates.admission_year_id` $\rightarrow$ `opr.academic_years.id`
- `std.candidate_guardians.candidate_id` $\rightarrow$ `std.candidates.id`
- `std.students.candidate_id` $\rightarrow$ `std.candidates.id`
- `std.student_guardians.student_id` $\rightarrow$ `std.students.id`
- `std.study_groups.academic_year_id` $\rightarrow$ `opr.academic_years.id`
- `std.study_groups.homeroom_teacher_id` $\rightarrow$ `tch.teachers.id`
- `std.student_placements.student_id` $\rightarrow$ `std.students.id`
- `std.student_placements.study_group_id` $\rightarrow$ `std.study_groups.id`
- `tch.teaching_capabilities.teacher_id` $\rightarrow$ `tch.teachers.id`
- `tch.teaching_capabilities.subject_id` $\rightarrow$ `acd.subjects.id`
- `tch.teacher_assignments.teacher_id` $\rightarrow$ `tch.teachers.id`
- `tch.teacher_assignments.subject_id` $\rightarrow$ `acd.subjects.id`
- `tch.teacher_assignments.study_group_id` $\rightarrow$ `std.study_groups.id`
- `tch.teacher_assignments.operational_period_id` $\rightarrow$ `opr.operational_periods.id`

## Migration Considerations

Scripts are authored as idempotent T-SQL executed via DbUp:
1. `011_CreateOprCalendarAndRoomTables.sql`
2. `012_CreateOprAnnouncementTables.sql`
3. `020_CreateStdCandidateTables.sql`
4. `021_CreateStdStudentTables.sql`
5. `022_CreateStdStudyGroupTables.sql`
6. `030_CreateAcdSubjectTables.sql` (includes `IF NOT EXISTS` check + `INSERT INTO acd.subjects` seed)
7. `040_CreateTchTeacherTables.sql`
8. `041_CreateTchAssignmentTables.sql`
9. `099_SeedStage1Data.sql` (seeds `principal`, `teacher.budi`, `tch.teachers` Budi Santoso record, `auth.user_actors`, and `7A` study group with homeroom teacher).

---

# 9. Cross-Cutting Concerns

- **Role & Permission Governance:**
  - Expanded permissions:
    - Operations: `Permissions.Operations.Calendar.Manage`, `Permissions.Operations.Rooms.Manage`, `Permissions.Operations.Announcements.Create`, `Permissions.Operations.Announcements.Publish`, `Permissions.Operations.Announcements.View`.
    - Students: `Permissions.Students.Admissions.Register`, `Permissions.Students.Admissions.Decide`, `Permissions.Students.Admissions.Approve`, `Permissions.Students.Admissions.Enroll`, `Permissions.Students.Directory.View`, `Permissions.Students.Directory.Manage`, `Permissions.Students.Placements.Manage`, `Permissions.Students.Roster.ViewOwn`.
    - Teaching: `Permissions.Teaching.Directory.Manage`, `Permissions.Teaching.Capabilities.Manage`, `Permissions.Teaching.Assignments.Manage`, `Permissions.Teaching.Assignments.View`, `Permissions.Teaching.Assignments.Oversight`.
- **Data Scoping & Boundary Security (`UC-HRM-05`):**
  - Handlers extracting `ICurrentUser` inspect `TeacherId` claim from `auth.user_actors`.
  - When a user possesses only `HOMEROOM_TEACHER` (without `SCHOOL_ADMIN`), student queries enforce `WHERE sg.homeroom_teacher_id = @TeacherId`.
- **Atomic Operations & Concurrency:**
  - `EnrollCandidate` executes candidate deactivation, student insertion, guardian clone, and NIS uniqueness verification inside a single `SqlTransaction`.
  - Sequential NIS computation uses `UPDLOCK, HOLDLOCK` or unique index violation retry to avoid concurrency duplicates.
- **Error Handling (RFC 7807):**
  - FluentValidation format errors map to `422 Unprocessable Entity` with `camelCase` field property paths.
  - Domain rule conflicts (e.g., duplicate NIS, candidate not accepted, teacher inactive, uncertified subject assignment, non-empty study group name conflict) map to `409 Conflict`.
- **UI Ergonomics & 3-Tier Confirmation:**
  - Direct Actions: Browsing, filtering, sorting, tab switching.
  - Tier 2 Confirmation Dialog: Deleting a calendar entry, rejecting a candidate, deactivating a teacher, removing a teaching assignment.
  - Tier 3 Pre-Flight Impact Modal: Enrolling an accepted candidate (confirming official student creation and NIS allocation).

---

# 10. Implementation Constraints

1. **Strict Assembly Isolation (NetArchTest):**
   - Zero project references between `ElsasaSis.Modules.Opr`, `ElsasaSis.Modules.Std`, and `ElsasaSis.Modules.Tch`.
   - References are restricted strictly to `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`.
2. **Pure Persistence Layer SQL Discipline:**
   - Zero database foreign keys, stored procedures, triggers, default constraints, or check constraints.
   - All primary keys must use `UuidV7.NewGuid()`.
   - All `created_at` and `updated_at` timestamps must be supplied in UTC by C# code (`DateTime.UtcNow`).
3. **No Entity Framework Core:**
   - Dapper only via `ISqlConnectionFactory`.
4. **Mandatory CQRS Handler Discipline:**
   - Write Slices must load an Aggregate Root, mutate via domain methods, save via Repository, and dispatch domain events.
   - Read Slices must execute direct Dapper SQL projections into DTOs; never instantiate Aggregate Roots on read.
5. **Strict 5-Screen Inventory:**
   - No additional top-level routes or orphan screens. All 20 use cases must live in the 5 assigned screens (`SCR-OPR-CALENDAR`, `SCR-OPR-ANNOUNCEMENTS`, `SCR-STD-ADMISSIONS`, `SCR-STD-DIRECTORY`, `SCR-TCH-ASSIGNMENTS`).
6. **Frontend State & Typing Discipline:**
   - Zero `localStorage` or `sessionStorage` token caching.
   - Server state managed strictly via TanStack Query hooks; client shell context via Pinia; form state via VeeValidate + Zod.
   - Generated TypeScript contracts (`openapi-typescript`) must drive all API DTO consumption.

---

# 11. Acceptance Conditions

Stage 1 implementation is architecturally complete when all of the following conditions hold:

1. **Solution & Architecture Verification:**
   - `src/backend/ElsasaSis.sln` builds cleanly under `.NET 8` (`net8.0`).
   - `ElsasaSis.ArchitectureTests` passes NetArchTest rules confirming zero cross-module dependencies between `Auth`, `Opr`, `Std`, and `Tch`, and zero EF Core usage.
2. **Database Migrations & Seeding:**
   - DbUp executes migrations `011` through `099` successfully on local SQL Server.
   - Seed accounts exist: `admin` (`SCHOOL_ADMIN`), `principal` (`PRINCIPAL`), `teacher.budi` (`TEACHER` + `HOMEROOM_TEACHER`), teacher Budi Santoso linked via `user_actors`, and study group `7A` with Budi Santoso as homeroom teacher.
   - Table `acd.subjects` contains the 10 standard K-12 subject seed rows.
3. **Operational Context (`opr`) Verification:**
   - `UC-ADM-02`: School calendar entries (school days, holidays, closures) can be created, listed by academic year, and deleted.
   - `UC-ADM-03`: Rooms (classroom, lab, hall) can be created, updated, and listed.
   - `UC-ADM-05`: Announcements can be authored as `DRAFT`, updated, published as `PUBLISHED`, and filtered by audience (`ALL`, `TEACHERS`).
   - `SCR-OPR-CALENDAR` Full renders 3 tabs (Academic Cycle, Calendar & Holidays, Rooms & Facilities).
4. **Student Context (`std`) Verification:**
   - `UC-ADM-06`: Candidate intake registers applicant personal info and guardian contacts (`REGISTERED`).
   - `UC-ADM-07` / `UC-PRC-01`: Candidate admission decision transitions status to `ACCEPTED` or `REJECTED` with `decision_notes`, verified for both Admin and Principal.
   - `UC-ADM-08`: Accepted candidate can be enrolled with suggested sequential NIS (`20260001`) or admin override, atomically creating `std.students`, cloning guardians, and marking candidate `ENROLLED`.
   - `UC-ADM-09`: Student personal details and guardians can be updated.
   - `UC-ADM-10`: Study groups (rombel) can be configured with homeroom teacher assignment.
   - `UC-ADM-11`: Enrolled students can be placed into academic year, grade, and study group.
   - `UC-ADM-12` / `UC-HRM-05`: Student directory lists students; logging in as `teacher.budi` verifies that roster view is strictly scoped to `7A` study group and includes emergency guardian phone numbers.
5. **Teaching Context (`tch`) Verification:**
   - `UC-ADM-13`: Teacher profile can be registered (`ACTIVE`) with NIP/NIK, personal data, and contact info, and deactivated.
   - `UC-ADM-14`: Qualified subject capabilities can be added to and removed from a teacher profile using real `acd.subjects` records.
   - `UC-ADM-15`: Teachers can be assigned to teach a subject in a study group for an operational period, enforcing that the teacher is active and has the required capability (`BR-TCH-003`, `BR-TCH-005`). Assignments can be removed.
   - `UC-ADM-16` / `UC-PRC-05`: Faculty directory displays teachers, capabilities, and study group assignments; leadership workload view calculates teaching distribution per teacher.
6. **Frontend Quality & Shell Integration:**
   - Frontend passes `pnpm typecheck`, `pnpm lint`, and unit tests.
   - Sidebar displays relevant menu groups based on logged-in role (`Admissions`, `Students & Classes`, `Faculty & Teaching`, `Operations & Comms`).
   - All forms enforce VeeValidate + Zod client validation and handle backend RFC 7807 `422` field errors and `409 Conflict` alerts gracefully.