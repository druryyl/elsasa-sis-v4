---
Title: Stage 1 Operational Setup & Master Data Foundation — Implementation Plan
Code: CR-STAGE-1
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-19
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Deliver an executable, dependency-driven implementation plan for **CR-STAGE-1** (**Stage 1: Operational Setup & Master Data Foundation** of **ELSASA-SIS v4 Basic Level**), transforming the completed Stage 0 codebase (`src/backend/` and `src/frontend/`) into the approved Stage 1 target technical realization across **20 actor-driven use cases** and **5 UI screens**.

Planning Mode: `FEATURE-PLANNING` (scoping `CR-STAGE-1` Stage 1 Operational Setup & Master Data Foundation over the Stage 0 walking skeleton baseline)

Referenced artifacts:

- ISSUE: [IS002- STAGE-1-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-ISSUE.md) (`CR-STAGE-1`)
- FEASIBILITY-ASSESSMENT: [IS002- STAGE-1-DEV-FEASIBILITY-ASSESMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-FEASIBILITY-ASSESMENT.md) (`Status: READY-FOR-PLANNING`; `GAP-001`–`GAP-010` and `OQ-001`–`OQ-008` all `CLOSED`)
- ARCHITECTURE: [IS002- STAGE-1-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-ARCHITECTURE.md) (`Version: 1.0`)
- Supporting Baseline Specifications: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md), [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md), [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md), [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md), [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md), [teaching-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/teaching-tables-design.md), [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md), [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md), [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md), [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md), [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md)

Architecture Applicability: `ARCHITECTURE-REQUIRED` — Target architecture defined in [IS002- STAGE-1-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-ARCHITECTURE.md).

---

# 2. Planning Scope

Scope covers all Stage 1 deliverables across two strict repository boundaries (`src/backend` and `src/frontend`):

1. **Backend (`src/backend`):**
   - Two new `.NET 8 LTS` (`net8.0`) module assemblies: `ElsasaSis.Modules.Std` and `ElsasaSis.Modules.Tch`, integrated into `ElsasaSis.sln`, `ElsasaSis.Api`, `ElsasaSis.UnitTests`, `ElsasaSis.ArchitectureTests`, and `ElsasaSis.IntegrationTests` with zero cross-module assembly references.
   - Expansion of `ElsasaSis.Contracts` (`IOperationalContract` ID lookups, `IAcademicContract`, `IStudentContract`, `ITeachingContract`, `StudentEnrolledDomainEvent`, `StudentPlacedDomainEvent`, `TeacherAssignedDomainEvent`) and `ElsasaSis.Modules.Auth` (`Permissions.cs` + `RolePermissionCatalog.cs` for `SCHOOL_ADMIN`, `PRINCIPAL`, `TEACHER`, `HOMEROOM_TEACHER`, `FINANCE_OFFICER`).
   - Nine idempotent DbUp SQL scripts in `ElsasaSis.Database/Scripts/` (`011`, `012`, `020`, `021`, `022`, `030`, `040`, `041`, `099`) defining 13 Pure Persistence tables (`opr.calendar_entries`, `opr.rooms`, `opr.announcements`, `std.candidates`, `std.candidate_guardians`, `std.students`, `std.student_guardians`, `std.study_groups`, `std.student_placements`, `acd.subjects`, `tch.teachers`, `tch.teaching_capabilities`, `tch.teacher_assignments`), 10 Indonesian K-12 seed subjects, and deterministic Stage 1 QA accounts/fixtures (`principal`, `teacher.budi` linked to Budi Santoso via `auth.user_actors`, study groups `7A` and `7B`), plus `Stage1Seeder.cs` for integration test resets.
   - Expansion of `ElsasaSis.Modules.Opr` (`CalendarEntries`, `Rooms`, `Announcements`), full `ElsasaSis.Modules.Std` (`Admissions` with 1-step decision + sequential NIS atomic enrollment, `Students` + `Guardians`, `StudyGroups` with homeroom assignment & `UC-HRM-05` data scoping, `Placements`), full `ElsasaSis.Modules.Tch` (`Teachers`, `TeachingCapabilities`, `TeacherAssignments`, `Workload` oversight), and `BND-MOD-ACD-READ` (`GET /api/acd/subjects` + `IAcademicContract` implementation hosted in `ElsasaSis.Api`).
   - Expanded NetArchTest architecture suite (`Auth`, `Opr`, `Std`, `Tch`), unit tests, and `WebApplicationFactory` + `Respawn` integration tests against local SQL Server / `MSSQLLocalDB`.

2. **Frontend (`src/frontend`):**
   - Expanded `AppLayout.vue` role-filtered sidebar (`Admissions`, `Students & Classes`, `Faculty & Teaching`, `Operations & Comms`), `routes.ts` Stage 1 routes (`/operations/calendar`, `/operations/announcements`, `/admissions/queue`, `/students/directory`, `/teachers/assignments`, plus `/teaching/assignments` redirect alias), ESLint-compliant reactive shared context bridges (`authSession.ts`, `appContextBridge.ts`), shared `WorkbenchLayout.vue`, updated `StatusPill.vue`, regenerated OpenAPI `schema.d.ts`, and `shared/services/lookups.api.ts`.
   - All 5 Stage 1 screens realizing all 20 use cases:
     1. `SCR-OPR-CALENDAR` Full (`features/operations/calendar/views/CalendarWorkspaceView.vue` — 3 tabs: Academic Cycle, Calendar & Holidays, Rooms & Facilities)
     2. `SCR-OPR-ANNOUNCEMENTS` (`features/operations/announcements/views/AnnouncementsView.vue`)
     3. `SCR-STD-ADMISSIONS` (`features/student/admissions/views/AdmissionsQueueView.vue`)
     4. `SCR-STD-DIRECTORY` (`features/student/directory/views/StudentDirectoryView.vue` — 2 tabs + homeroom-scoped roster mode)
     5. `SCR-TCH-ASSIGNMENTS` (`features/teaching/assignments/views/TeacherAssignmentsView.vue` — 2 tabs: Faculty Directory + Teaching Allocations & Workload Oversight)
   - Vitest + MSW unit/component tests, `vue-tsc`, ESLint boundary compliance, `pnpm build`, OpenAPI `--check`, and multi-role live E2E verification (`admin`, `principal`, `teacher.budi`).

3. **Excluded from Stage 1:**
   - Timetable scheduling & SMK block rotations (`UC-ADM-04`, `SCR-OPR-TIMETABLE`, `opr.study_group_schedules`, `opr.study_group_timetables`).
   - Full Academic curriculum/attendance/scorebook/report cards (`acd.subject_stages`, `acd.learning_units`, `acd.attendance_records`, `acd.assignments`, `acd.report_cards`, `SCR-ACD-*`).
   - Finance (`fin.*`) and Development (`dev.*`) domains.
   - Public self-service candidate registration portal and student status transitions beyond `ACTIVE`.

---

# 3. Dependencies

External/Environment prerequisites verified in the Feasibility Assessment:

- `.NET 8 SDK` (`net8.0`)
- `Node.js` (`v22+`) and `pnpm`
- Local Microsoft SQL Server (`MSSQLSERVER` / `(localdb)\MSSQLLocalDB`) — Docker is not required.
- Stage 0 baseline prerequisite: [IS001- STAGE-0-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-IMPLEMENTATION-PLAN.md) (`COMPLETED`).

For slice dependencies:

- `Depends On` declares implementation prerequisites.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation status `IMPLEMENTED` and verifiable repository output.
- Dependency satisfaction does not require review status `GO`.
- Dependencies must represent real implementation prerequisites.
- Each slice targets strictly one repository boundary (`src/backend` or `src/frontend`), never both.

### Parallel Execution Wave Summary *(Informational Only — `Depends On` Is Authoritative)*

- **Wave 1:** `P1-S01` (`src/backend`) || `P1-S03` (`src/frontend`)
- **Wave 2:** `P1-S02` || `P2-S04` || `P2-S05` || `P2-S06` *(all `src/backend`, strictly disjoint files)*
- **Wave 3:** `P2-S07` || `P2-S08` || `P2-S09` *(all `src/backend`, strictly disjoint feature folders)*
- **Wave 4:** `P2-S10` (`src/backend`)
- **Wave 5:** `P3-S11` || `P3-S12` || `P3-S13`
- **Wave 6:** `P4-S14` || `P4-S15` || `P4-S16` || `P4-S17` || `P4-S18` *(all 5 frontend screens in parallel, strictly disjoint feature directories)*
- **Wave 7:** `P5-S19` (`src/frontend`)

---

# 4. Progress Summary

Plan status values are:

- NOT-STARTED
- IN-PROGRESS
- BLOCKED
- COMPLETED

Execution Approval values are:

- PENDING
- APPROVED

Execution Approval is owned by the Architect. It is PENDING during Planning
and set to APPROVED when the plan is released for execution. Execution must
not begin while Execution Approval is PENDING.

COMPLETED is a plan-level status only. Set it only when every slice has
implementation status IMPLEMENTED and review status GO.

Testing and test-package creation must not begin until the plan is COMPLETED.
An individual slice with review status GO is not a testing entry condition.

Slice implementation status values are:

- NOT-STARTED
- IN-PROGRESS
- IMPLEMENTED
- BLOCKED

Slice review status values are:

- NOT-REVIEWED
- GO
- NO-GO

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 | IMPLEMENTED | GO | 3/3 |
| P2 | IMPLEMENTED | GO | 7/7 |
| P3 | IMPLEMENTED | GO | 3/3 |
| P4 | IMPLEMENTED | GO | 5/5 |
| P5 | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Foundation, Scaffolding, Migrations & Shell Navigation

Implementation Status: IMPLEMENTED
Review Status: GO

### P1-S01

Title: Scaffold `ElsasaSis.Modules.Std` & `ElsasaSis.Modules.Tch`, Stage 1 `ElsasaSis.Contracts`, and Auth Permission Catalog

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Pre-register the new module projects in `ElsasaSis.sln`, expand `ElsasaSis.Contracts` and `ElsasaSis.Modules.Auth` permissions/role mappings, and update project references so all downstream Stage 1 backend slices can execute in parallel with zero `.sln`, `.csproj`, `Contracts`, or `Permissions` merge collisions.

Depends On: None

Repository: src/backend

Completion Criteria:

1. Create `src/backend/src/Modules/ElsasaSis.Modules.Std/ElsasaSis.Modules.Std.csproj` and `src/backend/src/Modules/ElsasaSis.Modules.Tch/ElsasaSis.Modules.Tch.csproj` (matching `ElsasaSis.Modules.Opr.csproj`: `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, `Dapper 2.1.35`, `FluentValidation 11.11.0`, `MediatR 12.4.1`, referencing ONLY `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`). Register both in `src/backend/ElsasaSis.sln`.
2. Add skeleton files in `Modules.Std` (`StdAssemblyMarker.cs`, `Permissions.cs`, compilable `DependencyInjection.cs` with `AddStdModule()`, `Features/ValidationFilter.cs`) and `Modules.Tch` (`TchAssemblyMarker.cs`, `Permissions.cs`, compilable `DependencyInjection.cs` with `AddTchModule()`, `Features/ValidationFilter.cs`). Update `ElsasaSis.Modules.Opr/Permissions.cs` to add `Rooms` (`View`, `Manage`) and `Announcements` (`View`, `Create`, `Publish`) constants.
3. Update project references in `ElsasaSis.Api.csproj` (add `Modules.Std`, `Modules.Tch`, and explicit `Dapper 2.1.35` package reference), `ElsasaSis.UnitTests.csproj` (add `Modules.Std`, `Modules.Tch`, `Database`), `ElsasaSis.ArchitectureTests.csproj` (add `Modules.Std`, `Modules.Tch`), and `ElsasaSis.IntegrationTests.csproj` (add `Modules.Std`, `Modules.Tch`). Never add any cross-module reference between `Auth`, `Opr`, `Std`, or `Tch`.
4. Expand `src/backend/src/ElsasaSis.Contracts/`:
   - `Operational/IOperationalContract.cs`: add `AcademicYearSummaryDto(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, string Status)`, `OperationalPeriodSummaryDto(Guid Id, Guid AcademicYearId, string AcademicYearName, string Name, DateOnly StartDate, DateOnly EndDate, string Status)`, and methods `GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default)` and `GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default)`; implement both in `ElsasaSis.Modules.Opr/Infrastructure/Contracts/OperationalContract.cs`.
   - `Academic/IAcademicContract.cs`: `SubjectDto(Guid Id, string Name, string? Code)` and methods `GetAllSubjectsAsync(CancellationToken cancellationToken = default)` and `GetSubjectByIdAsync(Guid id, CancellationToken cancellationToken = default)`.
   - `Student/IStudentContract.cs` & `Student/Events/`: `StudyGroupSummaryDto(Guid Id, Guid AcademicYearId, string AcademicYearName, string SchoolLevel, string Grade, string Name, Guid? HomeroomTeacherId, string? HomeroomTeacherName)`, `GetStudyGroupByIdAsync(Guid id, CancellationToken cancellationToken = default)`, `StudentEnrolledDomainEvent(Guid StudentId, Guid CandidateId, string Nis, string FullName)`, `StudentPlacedDomainEvent(Guid PlacementId, Guid StudentId, Guid AcademicYearId, Guid? StudyGroupId)`.
   - `Teaching/ITeachingContract.cs` & `Teaching/Events/`: `TeacherSummaryDto(Guid Id, string FullName, string Status)`, `GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default)`, `TeacherAssignedDomainEvent(Guid AssignmentId, Guid TeacherId, Guid SubjectId, Guid StudyGroupId, Guid OperationalPeriodId)`.
5. Expand `ElsasaSis.Modules.Auth/Permissions.cs` (adding all Operations, Students, and Teaching permissions plus composite policy name constants `Policies.Students.Admissions.DecideOrApprove`, `Policies.Students.DirectoryOrOwnRoster`, `Policies.Operations.Calendar.ViewOrManage`, `Policies.Operations.Rooms.ViewOrManage`, `Policies.Operations.Announcements.ViewOrManage`, `Policies.Teaching.Assignments.ViewOrOversight` — mirrored in module `Permissions.cs` files where used) and `RolePermissionCatalog.cs` mapping:
   - `SCHOOL_ADMIN`: all `Operations.*` (`Calendar.View/Manage`, `Rooms.View/Manage`, `Announcements.View/Create/Publish`), `Students.*` (`Admissions.View/Register/Decide/Enroll`, `Directory.View/Manage`, `Placements.Manage`), `Teaching.*` (`Directory.Manage`, `Capabilities.Manage`, `Assignments.View/Manage/Oversight`).
   - `PRINCIPAL`: `Operations.Calendar.View`, `Operations.Rooms.View`, `Operations.Announcements.View`, `Students.Admissions.View`, `Students.Admissions.Approve`, `Students.Directory.View`, `Teaching.Assignments.View`, `Teaching.Assignments.Oversight`.
   - `TEACHER`: `Operations.Calendar.View`, `Operations.Announcements.View`, `Teaching.Assignments.View`.
   - `HOMEROOM_TEACHER`: `Operations.Calendar.View`, `Operations.Announcements.View`, `Students.Roster.ViewOwn`.
   - `FINANCE_OFFICER`: `Operations.Calendar.View`, `Operations.Announcements.View`, `Students.Directory.View`.
6. Update `ElsasaSis.UnitTests/Auth/RolePermissionCatalogTests.cs` to verify all Stage 1 role-permission mappings.
7. `dotnet build src/backend/ElsasaSis.sln` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` succeed with exit code `0`.

Notes: Ensure existing Stage 0 `Opr` and `Auth` unit/integration tests remain unbroken. All composite policy names must match string-for-string between `Modules.Auth/Permissions.cs` and each module's `Permissions.cs`.

Implementation Notes: Scaffolded `ElsasaSis.Modules.Std` and `ElsasaSis.Modules.Tch` (`csproj`, `*AssemblyMarker`, `Permissions`/`Policies`, `DependencyInjection`, `Features/ValidationFilter`), registered both in `ElsasaSis.sln` and updated references across `Api`, `UnitTests`, `ArchitectureTests`, and `IntegrationTests` (zero inter-module refs). Expanded `ElsasaSis.Contracts` (`IOperationalContract` + `OperationalContract` impl, `IAcademicContract`, `IStudentContract` + `StudentEnrolledDomainEvent` / `StudentPlacedDomainEvent`, `ITeachingContract` + `TeacherAssignedDomainEvent`), `Modules.Opr/Permissions.cs`, `Modules.Auth/Permissions.cs` + `RolePermissionCatalog.cs`, and `RolePermissionCatalogTests.cs`. Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors), `ElsasaSis.UnitTests` (43/43 passed), and `ElsasaSis.ArchitectureTests` (7/7 passed). Changed files: `src/backend/ElsasaSis.sln`, `src/backend/src/Modules/ElsasaSis.Modules.Std/*`, `src/backend/src/Modules/ElsasaSis.Modules.Tch/*`, `src/backend/src/Modules/ElsasaSis.Modules.Opr/Permissions.cs`, `src/backend/src/Modules/ElsasaSis.Modules.Opr/Infrastructure/Contracts/OperationalContract.cs`, `src/backend/src/Modules/ElsasaSis.Modules.Auth/Permissions.cs`, `src/backend/src/Modules/ElsasaSis.Modules.Auth/RolePermissionCatalog.cs`, `src/backend/src/ElsasaSis.Contracts/**/*`, `src/backend/src/ElsasaSis.Api/ElsasaSis.Api.csproj`, `src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj`, `src/backend/tests/ElsasaSis.UnitTests/Auth/RolePermissionCatalogTests.cs`, `src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj`, `src/backend/tests/ElsasaSis.IntegrationTests/ElsasaSis.IntegrationTests.csproj`.

---

### P1-S02

Title: Implement Stage 1 DbUp Migration Scripts (`011`–`099`), K-12 Subject Seed & Deterministic QA Seeder

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Author all 9 Stage 1 embedded Pure Persistence SQL scripts (`011` through `099`) covering all 13 new tables, the 10 standard Indonesian K-12 subjects, and deterministic Stage 1 verification fixtures (`principal`, `teacher.budi`, Budi Santoso profile, `7A`/`7B` study groups), plus an idempotent C# `Stage1Seeder` for integration test resets.

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. Embedded SQL scripts exist under `src/backend/src/ElsasaSis.Database/Scripts/` (automatically embedded via `Scripts\*.sql` in `.csproj`), matching `IS002- STAGE-1-DEV-ARCHITECTURE.md` §8 and the authoritative `*-tables-design.md` DDLs with zero DB FKs, zero `DEFAULT` constraints, zero `CHECK` constraints, zero triggers, zero stored procedures, UUID v7 clustered PKs, and `DATETIME2(3) NOT NULL` UTC `created_at` / `updated_at`:
   - `011_CreateOprCalendarAndRoomTables.sql`: `opr.calendar_entries` (`UQ_calendar_entries_year_date`, `IX_calendar_entries_year_type`) and `opr.rooms` (`id`, `name`, `room_type`, `capacity`, `created_at`, `updated_at`, `UQ_rooms_name`).
   - `012_CreateOprAnnouncementTables.sql`: `opr.announcements` (`IX_announcements_feed`, `IX_announcements_author`).
   - `020_CreateStdCandidateTables.sql`: `std.candidates` (including `decision_notes NVARCHAR(500) NULL`, `decided_at`, `enrolled_student_id`, `IX_candidates_admission_lookup`, `IX_candidates_full_name`) and `std.candidate_guardians` (`IX_candidate_guardians_candidate`).
   - `021_CreateStdStudentTables.sql`: `std.students` (`UQ_students_nis`, filtered `UQ_students_nisn WHERE nisn IS NOT NULL`, `IX_students_status_name`) and `std.student_guardians` (`IX_student_guardians_student`).
   - `022_CreateStdStudyGroupTables.sql`: `std.study_groups` (including `homeroom_teacher_id UNIQUEIDENTIFIER NULL`, `homeroom_teacher_name NVARCHAR(150) NULL`, `UQ_study_groups_year_name`, `IX_study_groups_year_level_grade`) and `std.student_placements` (`UQ_student_placements_student_year`, filtered `IX_student_placements_study_group`, `IX_student_placements_level_grade`).
   - `030_CreateAcdSubjectTables.sql`: `acd.subjects` (`UQ_subjects_name`, filtered `UQ_subjects_code WHERE code IS NOT NULL`) + idempotent `IF NOT EXISTS` inserts for all 10 standard Indonesian K-12 subjects (`PA-BP`, `PPKN`, `BIND`, `MTK`, `IPA`, `IPS`, `BING`, `SNB`, `PJOK`, `INF`).
   - `040_CreateTchTeacherTables.sql`: `tch.teachers` (filtered `UQ_teachers_nip`, filtered `UQ_teachers_nik`, `IX_teachers_status_name`) and `tch.teaching_capabilities` (`UQ_teaching_capabilities_teacher_subject`, `IX_teaching_capabilities_teacher`).
   - `041_CreateTchAssignmentTables.sql`: `tch.teacher_assignments` (`UQ_teacher_assignments_teacher_subject_group_period`, `IX_teacher_assignments_period_group`, `IX_teacher_assignments_period_teacher`, `IX_teacher_assignments_subject_period`).
   - `099_SeedStage1Data.sql`: idempotently seeds:
     - Default active Academic Year `2026/2027` (`2026-07-01`..`2027-06-30`, `ACTIVE`) with `Semester 1` (`2026-07-01`..`2026-12-20`, `ACTIVE`) and `Semester 2` (`2027-01-05`..`2027-06-30`, `INACTIVE`) if no active academic year exists yet (so `std.study_groups.academic_year_id` references a valid existing Academic Year).
     - `principal` user in `auth.users` (valid ASP.NET Core Identity v3 PBKDF2 hash of `Principal123!`) + `PRINCIPAL` role in `auth.user_roles`.
     - `teacher.budi` user in `auth.users` (valid ASP.NET Core Identity v3 PBKDF2 hash of `Teacher123!`) + `TEACHER` and `HOMEROOM_TEACHER` roles in `auth.user_roles`.
     - Active teacher profile `Budi Santoso` in `tch.teachers` (`nip = '198501012010011001'`, `phone = '081234567890'`, `gender = 'MALE'`, `status = 'ACTIVE'`) linked to `teacher.budi` via `auth.user_actors (user_id, teacher_id)`.
     - Study group `7A` (`SMP`, grade `7`, `homeroom_teacher_id = Budi Santoso`, `homeroom_teacher_name = 'Budi Santoso'`) and study group `7B` (`SMP`, grade `7`, `homeroom_teacher_id = NULL`, `homeroom_teacher_name = NULL`) in `std.study_groups` under the seeded Academic Year.
2. Implement `src/backend/src/ElsasaSis.Database/Stage1Seeder.cs` exposing idempotent `SeedRoleAccountsAndFixturesAsync(string connectionString, CancellationToken cancellationToken = default)` (using `PasswordHasher<object>`) so Stage 1 integration tests can re-seed `principal`, `teacher.budi`, Budi Santoso, and `7A`/`7B` cleanly on demand after `Respawn` without breaking Stage 0 `AcademicYearsIntegrationTests` (which asserts an empty `opr.academic_years` table after `ResetDatabaseAsync()`).
3. Add `src/backend/tests/ElsasaSis.UnitTests/Database/Stage1SeedPasswordHashTests.cs` verifying that the PBKDF2 hashes embedded in `099_SeedStage1Data.sql` pass `PasswordHasher<object>.VerifyHashedPassword(...)` for `Principal123!` and `Teacher123!`.
4. Running `ElsasaSis.Database` twice idempotently against local SQL Server / `(localdb)\MSSQLLocalDB` succeeds with exit code `0`, creating all 13 Stage 1 tables, 10 `acd.subjects` rows, and seeded accounts/study groups without error.

Notes: Keep `ElsasaSis.Database` free of references to `Modules.*` or `ElsasaSis.Api`. Make sure `099_SeedStage1Data.sql` uses deterministic valid UUIDs and `IF NOT EXISTS` guards on every insert.

Implementation Notes: Authored all 9 embedded Pure Persistence SQL scripts (`011_CreateOprCalendarAndRoomTables.sql`, `012_CreateOprAnnouncementTables.sql`, `020_CreateStdCandidateTables.sql`, `021_CreateStdStudentTables.sql`, `022_CreateStdStudyGroupTables.sql`, `030_CreateAcdSubjectTables.sql`, `040_CreateTchTeacherTables.sql`, `041_CreateTchAssignmentTables.sql`, `099_SeedStage1Data.sql`) creating all 13 Stage 1 tables (zero DB FKs/DEFAULTs/CHECKs/triggers/stored procs), 10 Indonesian K-12 subjects, active Academic Year fallback (`2026/2027` + Semester 1/2), `principal`/`teacher.budi` accounts with verified Identity v3 PBKDF2 hashes, Budi Santoso teacher profile + `auth.user_actors` link, and `7A`/`7B` study groups. Implemented `Stage1Seeder.cs` (`SeedRoleAccountsAndFixturesAsync`) preserving Stage 0 `AdminSeeder` behavior, and `Stage1SeedPasswordHashTests.cs`. Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors), `Stage1SeedPasswordHashTests` (21/21 passed), `ElsasaSis.ArchitectureTests` (7/7 passed), and `ElsasaSis.Database` running twice idempotently (exit code 0) against both `(localdb)\MSSQLLocalDB` (`ElsasaSis_Dev` + fresh DB check + `Stage1Seeder` post-wipe reset check) and `JUDE7` (`ElsasaSis`); full `ElsasaSis.UnitTests` passed 189/190 with 1 unrelated failure in `P2-S06` (`TeacherPersistenceAndDiTests`). Changed files: `src/backend/src/ElsasaSis.Database/Scripts/011_CreateOprCalendarAndRoomTables.sql`, `src/backend/src/ElsasaSis.Database/Scripts/012_CreateOprAnnouncementTables.sql`, `src/backend/src/ElsasaSis.Database/Scripts/020_CreateStdCandidateTables.sql`, `src/backend/src/ElsasaSis.Database/Scripts/021_CreateStdStudentTables.sql`, `src/backend/src/ElsasaSis.Database/Scripts/022_CreateStdStudyGroupTables.sql`, `src/backend/src/ElsasaSis.Database/Scripts/030_CreateAcdSubjectTables.sql`, `src/backend/src/ElsasaSis.Database/Scripts/040_CreateTchTeacherTables.sql`, `src/backend/src/ElsasaSis.Database/Scripts/041_CreateTchAssignmentTables.sql`, `src/backend/src/ElsasaSis.Database/Scripts/099_SeedStage1Data.sql`, `src/backend/src/ElsasaSis.Database/Stage1Seeder.cs`, `src/backend/tests/ElsasaSis.UnitTests/Database/Stage1SeedPasswordHashTests.cs`.

---

### P1-S03

Title: Expand Frontend App Shell, Stage 1 Routes, Navigation Taxonomy, Context Bridges & Shared UI Components

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Finalize all shared frontend shell files (`routes.ts`, `AppLayout.vue`, `http.provider.ts`, shared bridges, `WorkbenchLayout.vue`, `StatusPill.vue`, and minimal stub views) upfront so all 5 Stage 1 screen slices can be built concurrently in Phase 4 with zero file conflicts and zero ESLint boundary violations.

Depends On: None

Repository: src/frontend

Completion Criteria:

1. Extend `src/frontend/src/shared/services/authSession.ts` with a reactive read-only session bridge (`configureAuthSessionReader(...)` and `useCurrentSession()` returning reactive `user`, `roles`, `permissions`, `teacherId`, `hasPermission()`, `hasAnyRole()`) so `features/*` never imports `@/app/stores/auth.store`.
2. Create `src/frontend/src/shared/services/appContextBridge.ts` (`configureAppContextReader(...)` and `useAppContextBridge()` returning reactive `activeAcademicYearId`, `activePeriodId`, `selectedAcademicYearId`, `selectedPeriodId`, `activeAcademicYearName`, `activePeriodName`, `isHistoricalPeriod`, `currentContextLabel`) so `features/*` never imports `@/app/stores/appContext.store`.
3. Wire both readers inside `src/frontend/src/app/providers/http.provider.ts` to `useAuthStore()` and `useAppContextStore()`.
4. Create `src/frontend/src/shared/components/layouts/WorkbenchLayout.vue` (reusable 2-pane Work Queue layout: left scrollable filterable worklist pane + right detail workbench canvas, responsive on smaller viewports) and update `src/frontend/src/shared/components/feedback/StatusPill.vue` to map `SCHOOL_DAY` (`success`), `HOLIDAY` (`warning`), `CLOSURE` (`danger`), and `ASSIGNED` (`success`).
5. Update `src/frontend/src/app/router/routes.ts` to register all Stage 1 routes under `AppLayout.vue` with permission guards:
   - `/operations/calendar` (`operations.calendar`, existing)
   - `/operations/announcements` (`operations.announcements`, `Permissions.Operations.Announcements.View`, `.Create`, `.Publish`)
   - `/admissions/queue` (`admissions.queue`, `Permissions.Students.Admissions.View`, `.Register`, `.Decide`, `.Approve`, `.Enroll`)
   - `/students/directory` (`students.directory`, `Permissions.Students.Directory.View`, `.Manage`, `Permissions.Students.Roster.ViewOwn`)
   - `/teachers/assignments` (`teachers.assignments`, `Permissions.Teaching.Assignments.View`, `.Manage`, `.Oversight`, `Permissions.Teaching.Directory.Manage`)
   - Redirect `{ path: '/teaching/assignments', redirect: '/teachers/assignments' }` for backward compatibility with `ui-ux-guideline.md`.
6. Update `src/frontend/src/app/layouts/AppLayout.vue` `navGroups` with all 4 Stage 1 groups and Lucide icons: `Admissions` (`Admissions Queue`), `Students & Classes` (`Student Directory & Placements`), `Faculty & Teaching` (`Teachers & Assignments`), `Operations & Comms` (`School Calendar & Academic Cycle`, `Announcements & Bulletin`), dynamically filtered by `authStore.hasPermission(...)`. Also preserve `AppLayout.vue` academic year query gating so any authenticated user with `Permissions.Operations.Calendar.View` or `.Manage` populates the Top Bar switcher.
7. Create minimal compilable stub views (to be replaced in `P4-S15..S18`):
   - `src/frontend/src/features/operations/announcements/views/AnnouncementsView.vue`
   - `src/frontend/src/features/student/admissions/views/AdmissionsQueueView.vue`
   - `src/frontend/src/features/student/directory/views/StudentDirectoryView.vue`
   - `src/frontend/src/features/teaching/assignments/views/TeacherAssignmentsView.vue`
8. Update `AppLayout.spec.ts` and `router.guard.spec.ts` to verify role-filtered menus across `SCHOOL_ADMIN`, `PRINCIPAL`, `TEACHER`, `HOMEROOM_TEACHER` and permission guards on all 5 routes.
9. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass with 0 errors and 0 warnings in `src/frontend/`.

Notes: Strictly preserve `eslint.config.js` boundary rules (`shared/` never imports `features/` or `app/`; `features/` never imports `app/` or other `features/`).

Implementation Notes:
- Extended `src/frontend/src/shared/services/authSession.ts` (`configureAuthSessionReader`, `useCurrentSession`) and created `src/frontend/src/shared/services/appContextBridge.ts` (`configureAppContextReader`, `useAppContextBridge`), wired to `useAuthStore()` and `useAppContextStore()` in `src/frontend/src/app/providers/http.provider.ts`.
- Created `src/frontend/src/shared/components/layouts/WorkbenchLayout.vue` (responsive 2-pane work queue with `hasSelection` mobile back navigation) and updated `src/frontend/src/shared/components/feedback/StatusPill.vue` (`SCHOOL_DAY`, `HOLIDAY`, `CLOSURE`, `ASSIGNED`).
- Updated `src/frontend/src/app/router/routes.ts`, `src/frontend/src/app/layouts/AppLayout.vue`, and created the 4 Stage 1 stub views (`AnnouncementsView.vue`, `AdmissionsQueueView.vue`, `StudentDirectoryView.vue`, `TeacherAssignmentsView.vue`).
- Updated `src/frontend/src/app/layouts/__tests__/AppLayout.spec.ts` and `src/frontend/src/app/router/__tests__/router.guard.spec.ts` (including explicit per-test timeouts on cold dynamic-import route guard tests) and added `src/frontend/src/shared/services/__tests__/bridges.spec.ts`, `src/frontend/src/app/providers/__tests__/http.provider.spec.ts`, `src/frontend/src/shared/components/feedback/__tests__/StatusPill.spec.ts`, and `src/frontend/src/shared/components/layouts/__tests__/WorkbenchLayout.spec.ts`.
- Verification in `src/frontend/`: `pnpm typecheck` (`vue-tsc --noEmit`), `pnpm lint` (`eslint . --max-warnings 0`), and `pnpm test:unit` (`vitest run`: 15 test files, 60 tests) all passed with 0 errors and 0 warnings.

---

## P2 - Backend Domain Models, Repositories, Vertical Slices & API Wiring

Implementation Status: IMPLEMENTED
Review Status: GO

### P2-S04

Title: Implement `ElsasaSis.Modules.Opr` Calendar Entries, Rooms & Announcements Domain, Persistence & Vertical Slices

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Expand `ElsasaSis.Modules.Opr` with Aggregate Roots, Dapper repositories, unit tests, and vertical slices for `UC-ADM-02` (Calendar Entries), `UC-ADM-03` (Rooms), `UC-ADM-05` (Announcements), and read views (`UC-ADM-27`, `UC-PRC-07`, `UC-TCH-11`).

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. `Modules.Opr/Domain/` (zero Data/HTTP/Infrastructure/Features dependencies) implements:
   - `CalendarEntry` aggregate (`CalendarDayType`: `SCHOOL_DAY | HOLIDAY | CLOSURE`, validates `entryDate` within academic year `[startDate, endDate]` per `BR-OPS-007`), `ICalendarEntryRepository`.
   - `Room` aggregate (`RoomType`: `CLASSROOM | LABORATORY | HALL | OTHER`, validates non-blank trimmed name `BR-OPS-010` and `capacity > 0` `BR-OPS-011`), `IRoomRepository`.
   - `Announcement` aggregate (`AnnouncementAudience`: `ALL | TEACHERS | STUDENTS | PARENTS` `BR-OPS-012`, `AnnouncementStatus`: `DRAFT | PUBLISHED`, non-empty title & content `BR-OPS-013`, `Publish(utcNow)` rejecting already-published with `409`), `IAnnouncementRepository`.
   - Extended `OprErrors.cs` covering not-found, duplicate date (`BR-OPS-008`), date outside academic year (`BR-OPS-007`), duplicate room name (`BR-OPS-010`), invalid capacity (`BR-OPS-011`), invalid audience/content (`BR-OPS-012`/`013`), already published announcement.
2. `Modules.Opr/Infrastructure/Persistence/` implements `CalendarEntryRepository`, `RoomRepository`, and `AnnouncementRepository` via Dapper + `ISqlConnectionFactory` (handling unique constraint violations cleanly into `DomainError.Conflict`).
3. Vertical slices under `Modules.Opr/Features/`:
   - `CalendarEntries/`: `GetCalendarEntries` (`GET /api/opr/calendar-entries`), `CreateCalendarEntry` (`POST /api/opr/calendar-entries`), `UpdateCalendarEntry` (`PUT /api/opr/calendar-entries/{id}`), `DeleteCalendarEntry` (`DELETE /api/opr/calendar-entries/{id}`).
   - `Rooms/`: `GetRooms` (`GET /api/opr/rooms`), `CreateRoom` (`POST /api/opr/rooms`), `UpdateRoom` (`PUT /api/opr/rooms/{id}`).
   - `Announcements/`: `GetAnnouncements` (`GET /api/opr/announcements` — read-only callers lacking `Create`/`Publish` see only `PUBLISHED`; `TEACHER`/`HOMEROOM_TEACHER`-only callers see `target_audience IN ('ALL', 'TEACHERS')`), `GetAnnouncementById` (`GET /api/opr/announcements/{id}`), `CreateAnnouncement` (`POST /api/opr/announcements`, capturing `authorId = currentUser.UserId` and `authorName = currentUser.FullName`), `UpdateAnnouncement` (`PUT /api/opr/announcements/{id}`), `PublishAnnouncement` (`POST /api/opr/announcements/{id}/publish`).
4. All Read handlers MUST be named `Get*Handler` (to satisfy `LayerAndCqrsArchitectureTests`); all Write handlers MUST NOT depend on `Dapper` or `ISqlConnectionFactory`. Every endpoint uses `IEndpoint`, `.Produces<T>()`, `.ProducesProblem(...)`, `ValidationFilter<T>` where applicable, and authorization policies.
5. Register all new repositories, handlers, and validators in `Modules.Opr/DependencyInjection.cs`.
6. Add unit tests in `ElsasaSis.UnitTests/Opr/` (`CalendarEntryTests.cs`, `RoomTests.cs`, `AnnouncementTests.cs`) verifying `BR-OPS-007` through `BR-OPS-013`.
7. `dotnet build src/backend/ElsasaSis.sln` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` pass.

Notes: Owns only `Modules.Opr/` and `UnitTests/Opr/`. Executes concurrently with `P1-S02`, `P2-S05`, and `P2-S06`.

Implementation Notes: Implemented `CalendarEntry` (`CalendarDayType`, `BR-OPS-007`/`008`), `Room` (`RoomType`, `BR-OPS-010`/`011`), `Announcement` (`AnnouncementAudience`, `AnnouncementStatus`, `BR-OPS-012`/`013`, `Publish` 409 guard), repository interfaces, and extended `OprErrors.cs` in `Modules.Opr/Domain/` with zero Data/HTTP/Infrastructure/Features dependencies. Added Dapper persistence (`CalendarEntryRepository`, `RoomRepository`, `AnnouncementRepository`, `SqlExceptionHelper` mapping SQL `2601`/`2627` unique constraint violations to `DomainError.Conflict`). Added vertical slices under `Features/CalendarEntries/`, `Features/Rooms/`, and `Features/Announcements/` (with `Get*Handler` read projections, read-only `PUBLISHED` scoping, `TEACHER`/`HOMEROOM_TEACHER`-only audience scoping `IN ('ALL', 'TEACHERS')`, and `authorId`/`authorName` capture from `ICurrentUser`), registered all new dependencies in `Modules.Opr/DependencyInjection.cs`, and added unit tests in `ElsasaSis.UnitTests/Opr/` (`CalendarEntryTests.cs`, `RoomTests.cs`, `AnnouncementTests.cs`). Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors), `ElsasaSis.UnitTests` (200/200 passed; 64/64 `Opr` tests), and `ElsasaSis.ArchitectureTests` (7/7 passed). Changed files: `src/backend/src/Modules/ElsasaSis.Modules.Opr/Domain/{CalendarDayType,CalendarEntry,ICalendarEntryRepository,RoomType,Room,IRoomRepository,AnnouncementAudience,AnnouncementStatus,Announcement,IAnnouncementRepository,OprErrors}.cs`, `src/backend/src/Modules/ElsasaSis.Modules.Opr/Infrastructure/Persistence/{SqlExceptionHelper,CalendarEntryRepository,RoomRepository,AnnouncementRepository}.cs`, `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/{OprQueryHelper.cs,CalendarEntries/**,Rooms/**,Announcements/**}`, `src/backend/src/Modules/ElsasaSis.Modules.Opr/DependencyInjection.cs`, `src/backend/tests/ElsasaSis.UnitTests/Opr/{CalendarEntryTests,RoomTests,AnnouncementTests}.cs`.

---

### P2-S05

Title: Implement `ElsasaSis.Modules.Std` Domain Models, Dapper Persistence, `IStudentContract` & DI Auto-Registration

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement all Student module Aggregate Roots, entities, invariants (`BR-STU-001`–`BR-STU-010`), Dapper transactional repositories, `IStudentContract`, assembly-scanning `DependencyInjection.cs`, and comprehensive unit tests.

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. `Modules.Std/Domain/` (zero Infrastructure/Features/Dapper/SQL/HTTP references) implements:
   - `Candidate` Aggregate Root & `CandidateGuardian` child entity, `AdmissionStatus` (`REGISTERED | ACCEPTED | REJECTED | ENROLLED`), `Gender` (`MALE | FEMALE`), `SchoolLevel` (`SD | SMP | SMA | SMK`), `GuardianRelationshipType` (`FATHER | MOTHER | GUARDIAN`): `Register(...)` (enforces `>= 1` guardian `BR-STU-007`), `Accept(notes, utcNow)`, `Reject(notes, utcNow)` (enforces `REGISTERED -> ACCEPTED | REJECTED` `BR-STU-009` and immutable once `ENROLLED` `BR-STU-006`), `MarkEnrolled(studentId, utcNow)` (requires `ACCEPTED` `BR-STU-005`).
   - `Student` Aggregate Root & `StudentGuardian` child entity, `StudentStatus` (`ACTIVE`): `EnrollFromCandidate(...)` (enforces `BR-STU-001..003`, `BR-STU-007`, `BR-STU-010`, normalizes blank `nisn` to `null`, raises `StudentEnrolledDomainEvent`), `UpdateProfile(...)`, `AddGuardian(...)`, `UpdateGuardian(...)`, `RemoveGuardian(guardianId)` (rejects removing last guardian with `409 Conflict` per `BR-STU-003`).
   - `StudyGroup` Aggregate Root: `Create(...)`, `Update(...)` managing `academic_year_id`, `academic_year_name`, `school_level`, `grade`, `name`, `homeroom_teacher_id`, `homeroom_teacher_name`.
   - `StudentPlacement` Aggregate Root / Entity: `CreateOrUpdate(...)` enforcing `BR-STU-004`, `BR-STU-008`, `EXT-STU-001` (`study_group_id` and `study_group_name` both null or both populated), raising `StudentPlacedDomainEvent`.
   - `StdErrors.cs` and repository interfaces (`ICandidateRepository`, `IStudentRepository`, `IStudentEnrollmentRepository`, `IStudyGroupRepository`, `IStudentPlacementRepository`).
2. `Modules.Std/Infrastructure/` implements:
   - `Persistence/CandidateRepository.cs` (saves `std.candidates` + `std.candidate_guardians` in a `SqlTransaction`).
   - `Persistence/StudentRepository.cs` (loads/saves `std.students` + `std.student_guardians`, handles `UQ_students_nis` and `UQ_students_nisn` conflicts).
   - `Persistence/StudentEnrollmentRepository.cs` (atomic `SqlTransaction`: verifies NIS/NISN uniqueness with `UPDLOCK, HOLDLOCK` and unique constraint guard, computes sequential NIS if omitted using `LEN(nis) = 8 AND nis LIKE @YearPrefix + '[0-9][0-9][0-9][0-9]'`, inserts `std.students`, inserts cloned `std.student_guardians` with fresh `UuidV7.NewGuid()`, updates `std.candidates` to `ENROLLED` + `enrolled_student_id`).
   - `Persistence/StudyGroupRepository.cs` (persists `std.study_groups`, handles `UQ_study_groups_year_name`, propagates `study_group_name` snapshot updates to `std.student_placements` on rename per `EXT-STU-002`).
   - `Persistence/StudentPlacementRepository.cs` (upserts `std.student_placements` by `(student_id, academic_year_id)` per `BR-STU-004`).
   - `Contracts/StudentContract.cs` implementing `IStudentContract`.
3. `Modules.Std/DependencyInjection.cs` registers all repositories, `IStudentContract`, and reflection-scans `StdAssemblyMarker.Assembly` for all non-abstract `*Handler` classes and `IValidator<>` implementations (eliminating DI registration collisions between parallel slices `P2-S07` and `P2-S08`).
4. `ElsasaSis.UnitTests/Std/` unit tests verify all `Candidate`, `Student`, `StudyGroup`, and `StudentPlacement` invariants (`BR-STU-001`..`010`) and domain events.
5. `dotnet build src/backend/ElsasaSis.sln` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` pass.

Notes: Owns `Modules.Std/Domain/`, `Modules.Std/Infrastructure/`, `Modules.Std/DependencyInjection.cs`, and `UnitTests/Std/`.

Implementation Notes: Implemented `Modules.Std/Domain/` (`Candidate`, `CandidateGuardian`, `Student`, `StudentGuardian`, `StudyGroup`, `StudentPlacement`, `AdmissionStatus`, `Gender`, `SchoolLevel`, `GuardianRelationshipType`, `StudentStatus`, `GuardianInput`, `StdDomainParsing`, `StdDomainValidation`, `StdErrors`, and repository interfaces `ICandidateRepository`, `IStudentRepository`, `IStudentEnrollmentRepository`, `IStudyGroupRepository`, `IStudentPlacementRepository` enforcing `BR-STU-001`–`BR-STU-010` and `EXT-STU-001`, raising `StudentEnrolledDomainEvent` and `StudentPlacedDomainEvent`, with zero Infrastructure/Features/Dapper/SQL/HTTP references), `Modules.Std/Infrastructure/` (`CandidateRepository`, `StudentRepository` with `UQ_students_nis`/`UQ_students_nisn` conflict mapping, `StudentEnrollmentRepository` with atomic `UPDLOCK, HOLDLOCK` sequential NIS generation and guardian cloning, `StudyGroupRepository` with `UQ_study_groups_year_name` conflict mapping and `EXT-STU-002` `study_group_name` propagation to `std.student_placements`, `StudentPlacementRepository` upsert, `SqlPersistenceHelper`, and `StudentContract` implementing `IStudentContract`), reflection-scanning `Modules.Std/DependencyInjection.cs` (repositories, `IStudentContract`, `*Handler`, and `IValidator<>`), and `ElsasaSis.UnitTests/Std/` (`CandidateTests.cs`, `StudentTests.cs`, `StudyGroupTests.cs`, `StudentPlacementTests.cs`, `StdModuleTests.cs`, `StdPersistenceAndContractTests.cs`). Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors), `ElsasaSis.UnitTests` full suite (200/200 passed; 50/50 `UnitTests.Std` passed), and `ElsasaSis.ArchitectureTests` (7/7 passed). Changed files: `src/backend/src/Modules/ElsasaSis.Modules.Std/Domain/*`, `src/backend/src/Modules/ElsasaSis.Modules.Std/Infrastructure/**/*`, `src/backend/src/Modules/ElsasaSis.Modules.Std/DependencyInjection.cs`, `src/backend/tests/ElsasaSis.UnitTests/Std/*`.

---

### P2-S06

Title: Implement `ElsasaSis.Modules.Tch` Domain Models, Dapper Persistence, `ITeachingContract` & DI Auto-Registration

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement Teacher and TeacherAssignment Aggregate Roots, domain invariants (`BR-TCH-001`–`BR-TCH-008`, `EXT-TCH-001`–`004`), Dapper repositories, `ITeachingContract`, assembly-scanning `DependencyInjection.cs`, and unit tests.

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. `Modules.Tch/Domain/` (zero Infrastructure/Features/Dapper/SQL/HTTP references) implements:
   - `Teacher` Aggregate Root & `TeachingCapability` owned entity, `TeacherStatus` (`ACTIVE | INACTIVE`), `Gender` (`MALE | FEMALE`): `Register(...)`, `Update(...)` enforcing `BR-TCH-001` (full name & DOB), `BR-TCH-002` (at least one of `phone` or `email`), `BR-TCH-008` (gender), blank NIP/NIK normalized to `null`; `Deactivate(utcNow)` (`BR-TCH-007`), `Activate(utcNow)`; `AddCapability(subjectId, subjectName, utcNow)` (`BR-TCH-006` duplicate rejected with `409`), `RemoveCapability(subjectId)`.
   - `TeacherAssignment` Aggregate Root & `AssignmentStatus` (`ASSIGNED | REMOVED`): `Assign(...)` enforcing `BR-TCH-003` (teacher `ACTIVE`) and `BR-TCH-005` (teacher has capability for `subjectId`), raising `TeacherAssignedDomainEvent`; `Reactivate(utcNow)` (for cleanly reassigning a previously `REMOVED` tuple that still satisfies `UQ_teacher_assignments_teacher_subject_group_period`); `Remove(utcNow)` (`ASSIGNED -> REMOVED`, rejecting already-`REMOVED` with `409`).
   - `TchErrors.cs`, `ITeacherRepository`, `ITeacherAssignmentRepository`.
2. `Modules.Tch/Infrastructure/` implements:
   - `Persistence/TeacherRepository.cs` (persists `tch.teachers` + `tch.teaching_capabilities`, maps `UQ_teachers_nip`/`UQ_teachers_nik` conflicts to `DomainError.Conflict`, refreshes `tch.teacher_assignments.teacher_name` when `full_name` changes per `EXT-TCH-004`).
   - `Persistence/TeacherAssignmentRepository.cs` (persists `tch.teacher_assignments`, supports lookup by `(teacherId, subjectId, studyGroupId, operationalPeriodId)` to distinguish active duplicate `409` vs reactivating a `REMOVED` record).
   - `Contracts/TeachingContract.cs` implementing `ITeachingContract`.
3. `Modules.Tch/DependencyInjection.cs` registers repositories, `ITeachingContract`, and reflection-scans `TchAssemblyMarker.Assembly` for `*Handler` and `IValidator<>`.
4. `ElsasaSis.UnitTests/Tch/` unit tests verify `BR-TCH-001`–`BR-TCH-008`, capability uniqueness, assignment reactivation, and domain events.
5. `dotnet build src/backend/ElsasaSis.sln` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` pass.

Notes: Owns `Modules.Tch/Domain/`, `Modules.Tch/Infrastructure/`, `Modules.Tch/DependencyInjection.cs`, and `UnitTests/Tch/`. Executes concurrently with `P1-S02`, `P2-S04`, and `P2-S05`.

Implementation Notes: Implemented `Modules.Tch/Domain/` (`Teacher`, `TeachingCapability`, `TeacherAssignment`, `TeacherStatus`, `Gender`, `AssignmentStatus`, `TchErrors`, `ITeacherRepository`, `ITeacherAssignmentRepository` with zero Infrastructure/Features/Dapper/SQL/HTTP references), `Modules.Tch/Infrastructure/` (`TeacherRepository` with `UQ_teachers_nip`/`UQ_teachers_nik` conflict mapping and `EXT-TCH-004` `teacher_name` refresh on non-removed assignments, `TeacherAssignmentRepository` with tuple lookup & unique conflict mapping, `SqlPersistenceHelper`, and `TeachingContract` implementing `ITeachingContract`), reflection-scanning `Modules.Tch/DependencyInjection.cs`, and `ElsasaSis.UnitTests/Tch/` (`TeacherTests.cs`, `TeacherAssignmentTests.cs`, `TeacherPersistenceAndDiTests.cs`). Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors), `ElsasaSis.UnitTests` full suite (190/190 passed), and `UnitTests.Tch` filter (37/37 passed). Changed files: `src/backend/src/Modules/ElsasaSis.Modules.Tch/Domain/*`, `src/backend/src/Modules/ElsasaSis.Modules.Tch/Infrastructure/**/*`, `src/backend/src/Modules/ElsasaSis.Modules.Tch/DependencyInjection.cs`, `src/backend/tests/ElsasaSis.UnitTests/Tch/*`.

---

### P2-S07

Title: Implement `ElsasaSis.Modules.Std` Admissions Vertical Slices (`UC-ADM-06`, `UC-ADM-07`, `UC-PRC-01`, `UC-ADM-08`)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement all prospective student intake, unified 1-step admission decision, next sequential NIS suggestion, and atomic candidate enrollment endpoints under `Modules.Std/Features/Admissions/`.

Depends On: P2-S05

Repository: src/backend

Completion Criteria:

1. Read slices (all named `Get*Handler`, direct Dapper via `ISqlConnectionFactory`):
   - `GetCandidates` (`GET /api/std/candidates` — paginated, filter by `admissionYearId`, `status`, `schoolLevel`, `grade`, `search`; authorized by `Permissions.Students.Admissions.View`).
   - `GetCandidateById` (`GET /api/std/candidates/{id}` — includes `guardians[]`; `404` if missing).
   - `GetNextNis` (`GET /api/std/candidates/{id}/next-nis` — verifies candidate exists, computes sequential `[Year][4-digit seq]` e.g. `20260001` via `SELECT COALESCE(MAX(CAST(RIGHT(nis, 4) AS INT)), 0) + 1 FROM std.students WHERE LEN(nis) = 8 AND nis LIKE @YearPrefix + '[0-9][0-9][0-9][0-9]'`, returns `NextNisResponse(SuggestedNis)`).
2. Write slices (zero Dapper / zero `ISqlConnectionFactory`, orchestrating repositories + `IOperationalContract` + domain methods + `IDomainEventDispatcher`):
   - `RegisterCandidate` (`POST /api/std/candidates` — `UC-ADM-06`, authorized `Permissions.Students.Admissions.Register`, resolves `admissionYearName` via `IOperationalContract`).
   - `RecordAdmissionDecision` (`POST /api/std/candidates/{id}/decision` — `UC-ADM-07` & `UC-PRC-01`, authorized via composite policy `Policies.Students.Admissions.DecideOrApprove`, accepts `decision: "ACCEPTED" | "REJECTED"` and optional `decisionNotes`).
   - `EnrollCandidate` (`POST /api/std/candidates/{id}/enroll` — `UC-ADM-08`, authorized `Permissions.Students.Admissions.Enroll`, accepts `nis`, optional `nisn`, optional `enrollmentDate`, executes atomic enrollment via `IStudentEnrollmentRepository`, dispatches `StudentEnrolledDomainEvent`, returns `201 Created` with `EnrolledStudentResponse`).
3. All endpoints implement `IEndpoint`, `.WithTags("Students - Admissions")`, `ValidationFilter<T>`, `.Produces<T>()`, `.ProducesProblem(...)`.
4. `dotnet build src/backend/ElsasaSis.sln` passes.

Notes: Owns ONLY `Modules.Std/Features/Admissions/**`. Zero overlap with `P2-S08` (`Features/Students`, `StudyGroups`, `Placements`) or `P2-S09` (`Modules.Tch`).

Implementation Notes: Implemented all Admissions vertical slices under `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/Admissions/`: Read slices (`GetCandidatesHandler` and `GetCandidatesEndpoint` for `GET /api/std/candidates`, `GetCandidateByIdHandler` and `GetCandidateByIdEndpoint` for `GET /api/std/candidates/{id}`, and `GetNextNisHandler` and `GetNextNisEndpoint` for `GET /api/std/candidates/{id}/next-nis` using direct Dapper via `ISqlConnectionFactory`), and Write slices (`RegisterCandidateHandler`/`Endpoint`/`Request`/`Validator` for `POST /api/std/candidates` resolving academic year via `IOperationalContract`, `RecordAdmissionDecisionHandler`/`Endpoint`/`Request`/`Validator` for `POST /api/std/candidates/{id}/decision` authorized with `Policies.Students.Admissions.DecideOrApprove`, and `EnrollCandidateHandler`/`Endpoint`/`Request`/`Validator` for `POST /api/std/candidates/{id}/enroll` executing atomic enrollment via `IStudentEnrollmentRepository` and dispatching `StudentEnrolledDomainEvent`). All Write handlers have zero Dapper / zero `ISqlConnectionFactory` references. All 6 endpoints implement `IEndpoint`, `.WithTags("Students - Admissions")`, `ValidationFilter<T>`, typed responses via `.Produces<T>()`, and `.ProducesProblem(...)`. Build verification: `dotnet build src/backend/ElsasaSis.sln` succeeded with 0 warnings and 0 errors; `dotnet test` passed 200/200 unit tests and 7/7 architecture tests. Changed files: `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/Admissions/AdmissionsDtos.cs`, `AdmissionsQueryHelper.cs`, `EnrollCandidate/*`, `GetCandidateById/*`, `GetCandidates/*`, `GetNextNis/*`, `RecordAdmissionDecision/*`, `RegisterCandidate/*`.

---

### P2-S08

Title: Implement `ElsasaSis.Modules.Std` Student Directory, Study Groups, Placements & Homeroom Scoping Slices (`UC-ADM-09..12`, `UC-HRM-05`)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement student profile & guardian management, annual study group (rombel) configuration with homeroom teacher assignment, student placement upserts, and homeroom-scoped roster endpoints under `Modules.Std/Features/`.

Depends On: P2-S05

Repository: src/backend

Completion Criteria:

1. `Features/Students/` slices:
   - `GetStudents` (`GET /api/std/students` — paginated, search NIS/NISN/fullName, filter `academicYearId`/`grade`/`studyGroupId`/`status`; authorized via `Policies.Students.DirectoryOrOwnRoster`; if caller has `Roster.ViewOwn` without `Directory.View`, requires `currentUser.TeacherId` and scopes strictly to study groups where `sg.homeroom_teacher_id = @TeacherId` including emergency guardian contact summary).
   - `GetStudentById` (`GET /api/std/students/{id}` — returns profile + `guardians[]` + `placements[]`; enforces homeroom teacher ownership check returning `403 Forbidden` if not in caller's study group).
   - `UpdateStudent` (`PUT /api/std/students/{id}` — `UC-ADM-09`, authorized `Permissions.Students.Directory.Manage`).
   - `AddStudentGuardian` (`POST /api/std/students/{id}/guardians`), `UpdateStudentGuardian` (`PUT /api/std/students/{id}/guardians/{guardianId}`), `RemoveStudentGuardian` (`DELETE /api/std/students/{id}/guardians/{guardianId}` — enforces `BR-STU-003` -> `409 Conflict` on last guardian).
2. `Features/StudyGroups/` slices:
   - `GetStudyGroups` (`GET /api/std/study-groups` — filter by `academicYearId`, `schoolLevel`, `grade`, `search`, returns `homeroomTeacherId`, `homeroomTeacherName`, `studentCount`; scoped to `homeroom_teacher_id = @TeacherId` for `Roster.ViewOwn`-only callers).
   - `CreateStudyGroup` (`POST /api/std/study-groups` — `UC-ADM-10`, resolves `academicYearName` via `IOperationalContract` and optional `homeroomTeacherName` via `ITeachingContract`, rejects inactive teacher with `409`).
   - `UpdateStudyGroup` (`PUT /api/std/study-groups/{id}` — `UC-ADM-10`).
   - `GetStudyGroupRoster` (`GET /api/std/study-groups/{id}/roster` — `UC-HRM-05` & `UC-ADM-12`, authorized via `Policies.Students.DirectoryOrOwnRoster`; for `Roster.ViewOwn`-only callers executes `SELECT 1 FROM std.study_groups WHERE id = @StudyGroupId AND homeroom_teacher_id = @CurrentUserTeacherId` and returns `403 Forbidden` if unauthorized; returns study group metadata + placed active students with their full `guardians[]` emergency phone contacts).
3. `Features/Placements/` slice:
   - `PlaceStudent` (`POST /api/std/students/{id}/placements` — `UC-ADM-11`, authorized `Permissions.Students.Placements.Manage`, validates student, academic year via `IOperationalContract`, and optional study group matching `academicYearId`/`schoolLevel`/`grade` per `BR-STU-008`, upserts placement per `BR-STU-004`, dispatches `StudentPlacedDomainEvent`).
4. All Read handlers named `Get*Handler`; all Write handlers contain zero `Dapper`/`ISqlConnectionFactory`.
5. `dotnet build src/backend/ElsasaSis.sln` passes.

Notes: Owns ONLY `Modules.Std/Features/Students/**`, `StudyGroups/**`, and `Placements/**`. Executes concurrently with `P2-S07` and `P2-S09`.

Implementation Notes: Implemented all Student Directory, Guardians, Study Groups, and Placements vertical slices under `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/`: Read slices (`GetStudentsHandler` and `GetStudentsEndpoint` for `GET /api/std/students` supporting full pagination, filters, and homeroom scoping; `GetStudentByIdHandler` and `GetStudentByIdEndpoint` for `GET /api/std/students/{id}` with homeroom teacher ownership check; `GetStudyGroupsHandler` and `GetStudyGroupsEndpoint` for `GET /api/std/study-groups` with homeroom scoping and `studentCount` subquery; `GetStudyGroupRosterHandler` and `GetStudyGroupRosterEndpoint` for `GET /api/std/study-groups/{id}/roster` with homeroom authorization check returning placed active students and emergency guardian contacts), and Write slices (`UpdateStudentHandler`/`Endpoint`/`Request`/`Validator` for `PUT /api/std/students/{id}`; `AddStudentGuardianHandler`/`Endpoint`/`Request`/`Validator` for `POST /api/std/students/{id}/guardians`; `UpdateStudentGuardianHandler`/`Endpoint`/`Request`/`Validator` for `PUT /api/std/students/{id}/guardians/{guardianId}`; `RemoveStudentGuardianHandler`/`Endpoint` for `DELETE /api/std/students/{id}/guardians/{guardianId}` enforcing `BR-STU-003` -> `409 Conflict` on last guardian; `CreateStudyGroupHandler`/`Endpoint`/`Request`/`Validator` for `POST /api/std/study-groups` resolving academic year via `IOperationalContract` and teacher via `ITeachingContract` with inactive teacher rejection `409`; `UpdateStudyGroupHandler`/`Endpoint`/`Request`/`Validator` for `PUT /api/std/study-groups/{id}`; `PlaceStudentHandler`/`Endpoint`/`Request`/`Validator` for `POST /api/std/students/{id}/placements` and `POST /api/std/placements` validating `BR-STU-008` study group matching, upserting placement via `IStudentPlacementRepository`, and dispatching `StudentPlacedDomainEvent`). All Read handlers start with `Get*Handler` and all Write handlers contain zero Dapper / zero `ISqlConnectionFactory` dependencies. Build verification: `dotnet build src/backend/ElsasaSis.sln` succeeded with 0 warnings and 0 errors; `dotnet test` passed 216/216 unit tests and 7/7 architecture tests. Changed files: `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/Students/UpdateStudent/*`, `Features/Students/AddStudentGuardian/*`, `Features/Students/UpdateStudentGuardian/*`, `Features/Students/RemoveStudentGuardian/*`, `Features/StudyGroups/StudyGroupDtos.cs`, `Features/StudyGroups/GetStudyGroups/*`, `Features/StudyGroups/CreateStudyGroup/*`, `Features/StudyGroups/UpdateStudyGroup/*`, `Features/StudyGroups/GetStudyGroupRoster/*`, `Features/Placements/PlaceStudent/*`, `src/backend/tests/ElsasaSis.UnitTests/Std/StudentFeaturesTests.cs`.

---

### P2-S09

Title: Implement `ElsasaSis.Modules.Tch` Faculty Directory, Capabilities, Teaching Assignments & Workload Slices (`UC-ADM-13..16`, `UC-PRC-05`)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement all Teaching module read and write vertical slices for teacher profiles, qualified subject capability mappings, study group teaching allocations, and leadership workload oversight.

Depends On: P2-S06

Repository: src/backend

Completion Criteria:

1. `Features/Teachers/` slices:
   - `GetTeachers` (`GET /api/tch/teachers` — paginated, search name/NIP/NIK, filter `status`/`subjectId`, includes `capabilities[]` and `activeAssignmentCount`; authorized `Policies.Teaching.Assignments.ViewOrOversight`).
   - `GetTeacherById` (`GET /api/tch/teachers/{id}` — includes `capabilities[]` and active `assignments[]`).
   - `CreateTeacher` (`POST /api/tch/teachers` — `UC-ADM-13`, `Permissions.Teaching.Directory.Manage`).
   - `UpdateTeacher` (`PUT /api/tch/teachers/{id}` — `UC-ADM-13`).
   - `DeactivateTeacher` (`POST /api/tch/teachers/{id}/deactivate` — sets `INACTIVE` preserving assignments per `BR-TCH-007`, returns `activeAssignmentCount`).
   - `ActivateTeacher` (`POST /api/tch/teachers/{id}/activate`).
   - `AddTeachingCapability` (`POST /api/tch/teachers/{id}/capabilities` — `UC-ADM-14`, `Permissions.Teaching.Capabilities.Manage`, validates subject via `IAcademicContract`, enforces `BR-TCH-006`).
   - `RemoveTeachingCapability` (`DELETE /api/tch/teachers/{id}/capabilities/{subjectId}` — `UC-ADM-14`).
2. `Features/Assignments/` slices:
   - `GetTeacherAssignments` (`GET /api/tch/assignments` — `UC-ADM-16`, filter by `operationalPeriodId`, `academicYearId`, `studyGroupId`, `teacherId`, `subjectId`, `status`; authorized `Policies.Teaching.Assignments.ViewOrOversight`).
   - `AssignTeacher` (`POST /api/tch/assignments` — `UC-ADM-15`, `Permissions.Teaching.Assignments.Manage`, validates active teacher `BR-TCH-003`, capability match `BR-TCH-005`, resolves snapshots via `IOperationalContract`, `IAcademicContract`, `IStudentContract`, handles duplicate `ASSIGNED` -> `409` and previously `REMOVED` -> reactivates cleanly, dispatches `TeacherAssignedDomainEvent`).
   - `RemoveTeacherAssignment` (`DELETE /api/tch/assignments/{id}` — `UC-ADM-15`, transitions `ASSIGNED -> REMOVED`).
   - `GetTeacherWorkload` (`GET /api/tch/workload` — `UC-PRC-05`, authorized `Policies.Teaching.Assignments.ViewOrOversight`, accepts optional `operationalPeriodId` defaulting to active period, returns per-teacher workload summary with assigned study groups, subjects, total assignment count, and school-wide totals).
3. All Read handlers named `Get*Handler`; all Write handlers contain zero `Dapper`/`ISqlConnectionFactory`.
4. `dotnet build src/backend/ElsasaSis.sln` passes.

Notes: Owns ONLY `Modules.Tch/Features/**`. Executes concurrently with `P2-S07` and `P2-S08`.

Implementation Notes: Implemented all 12 vertical slices under `ElsasaSis.Modules.Tch/Features/`: `Teachers/` (`GetTeachers`, `GetTeacherById`, `CreateTeacher`, `UpdateTeacher`, `DeactivateTeacher` preserving assignments per `BR-TCH-007` and returning `activeAssignmentCount`, `ActivateTeacher`, `AddTeachingCapability` validating via `IAcademicContract` and `BR-TCH-006`, `RemoveTeachingCapability`) and `Assignments/` (`GetTeacherAssignments`, `AssignTeacher` enforcing `BR-TCH-003`/`BR-TCH-005`, resolving snapshots via `IOperationalContract`/`IAcademicContract`/`IStudentContract`, handling active duplicates `409` and `REMOVED` reactivation, and dispatching `TeacherAssignedDomainEvent`, `RemoveTeacherAssignment` `ASSIGNED -> REMOVED`, and `GetTeacherWorkload` defaulting to active period with per-teacher breakdown and school-wide KPI totals). All 4 Read handlers are named `Get*Handler`; all 8 Write handlers contain zero `Dapper` or `ISqlConnectionFactory` references. Verified `dotnet build src/backend/src/Modules/ElsasaSis.Modules.Tch/ElsasaSis.Modules.Tch.csproj --no-restore` (0 warnings, 0 errors), `dotnet build src/backend/ElsasaSis.sln --no-restore` (0 warnings, 0 errors), `ElsasaSis.UnitTests` (200/200 passed), and `ElsasaSis.ArchitectureTests` (7/7 passed). Changed files: `src/backend/src/Modules/ElsasaSis.Modules.Tch/Features/TchQueryHelper.cs`, `src/backend/src/Modules/ElsasaSis.Modules.Tch/Features/Teachers/**` (`TeacherDtos.cs`, `TeacherValidationRules.cs`, `GetTeachers/*`, `GetTeacherById/*`, `CreateTeacher/*`, `UpdateTeacher/*`, `DeactivateTeacher/*`, `ActivateTeacher/*`, `AddTeachingCapability/*`, `RemoveTeachingCapability/*`), `src/backend/src/Modules/ElsasaSis.Modules.Tch/Features/Assignments/**` (`TeacherAssignmentDto.cs`, `GetTeacherAssignments/*`, `AssignTeacher/*`, `RemoveTeacherAssignment/*`, `GetTeacherWorkload/*`).

---

### P2-S10

Title: Implement `BND-MOD-ACD-READ` (`GET /api/acd/subjects`, `IAcademicContract`) & Wire Stage 1 Modules and Policies in `ElsasaSis.Api`

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Expose the minimal `acd.subjects` read support (`GET /api/acd/subjects` + `IAcademicContract`) and wire `Modules.Std`, `Modules.Tch`, MediatR assemblies, discovered endpoints, and all Stage 1 individual & composite authorization policies in `ElsasaSis.Api`.

Depends On: P1-S02, P2-S04, P2-S07, P2-S08, P2-S09

Repository: src/backend

Completion Criteria:

1. Create `src/backend/src/ElsasaSis.Api/Infrastructure/AcademicContract.cs` implementing `IAcademicContract` against `acd.subjects` via `ISqlConnectionFactory` + Dapper.
2. Create `src/backend/src/ElsasaSis.Api/Features/Academic/GetSubjects/` (`SubjectResponse.cs`, `GetSubjectsHandler.cs`, `GetSubjectsEndpoint.cs` mapping `GET /api/acd/subjects` returning all subjects ordered by `name`, `.WithTags("Academic - Subjects")`, `.RequireAuthorization()`, `.Produces<IReadOnlyList<SubjectResponse>>()`).
3. Update `src/backend/src/ElsasaSis.Api/Program.cs`:
   - Register `AddStdModule()`, `AddTchModule()`, `IAcademicContract -> AcademicContract`, and `GetSubjectsHandler`.
   - Add `typeof(StdAssemblyMarker).Assembly` and `typeof(TchAssemblyMarker).Assembly` to `AddMediatR(...)`.
   - Add `typeof(StdAssemblyMarker).Assembly`, `typeof(TchAssemblyMarker).Assembly`, and `typeof(Program).Assembly` to `MapDiscoveredEndpoints(...)`.
   - Register all individual permission policies from `ElsasaSis.Modules.Auth.Permissions.All` AND composite policies (`Policies.Students.Admissions.DecideOrApprove`, `Policies.Students.DirectoryOrOwnRoster`, `Policies.Operations.Calendar.ViewOrManage`, `Policies.Operations.Rooms.ViewOrManage`, `Policies.Operations.Announcements.ViewOrManage`, `Policies.Teaching.Assignments.ViewOrOversight`) using `policy.RequireAssertion(...)` against `"permissions"` claims.
4. `dotnet build src/backend/ElsasaSis.sln` passes with 0 errors.
5. Starting `ElsasaSis.Api` in `Development` auto-migrates scripts `001`–`099` against `(localdb)\MSSQLLocalDB`, seeds `admin`, `principal`, `teacher.budi`, 10 `acd.subjects` rows, and `7A`/`7B`, and `/swagger/v1/swagger.json` includes all Stage 1 `/api/opr/*`, `/api/std/*`, `/api/tch/*`, and `/api/acd/subjects` paths with typed schemas.

Notes: Windows TCP port 5000 may be reserved/occupied (use e.g. `--urls http://localhost:5055` for local verification and stop the temporary host cleanly after checking).

Implementation Notes: Implemented `AcademicContract.cs` (`IAcademicContract` via `ISqlConnectionFactory` + Dapper) and vertical slice `Features/Academic/GetSubjects/` (`SubjectResponse`, `GetSubjectsHandler`, `GetSubjectsEndpoint` for `GET /api/acd/subjects` ordered by name). Updated `ElsasaSis.Api/Program.cs` to register `AddStdModule()`, `AddTchModule()`, `AcademicContract`, `GetSubjectsHandler`, MediatR assemblies (`Auth`, `Opr`, `Std`, `Tch`), discovered endpoint assemblies (`Auth`, `Opr`, `Std`, `Tch`, `Program`), and all individual and composite permission policies (`Policies.Students.Admissions.DecideOrApprove`, `Policies.Students.DirectoryOrOwnRoster`, `Policies.Operations.Calendar.ViewOrManage`, `Policies.Operations.Rooms.ViewOrManage`, `Policies.Operations.Announcements.ViewOrManage`, `Policies.Teaching.Assignments.ViewOrOversight`) using `policy.RequireAssertion(...)` against `"permissions"` claims. Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors), `ElsasaSis.UnitTests` (216/216 passed), `ElsasaSis.ArchitectureTests` (7/7 passed), and live API verification in `Development` mode against `(localdb)\MSSQLLocalDB` (all Swagger paths `/api/opr/*`, `/api/std/*`, `/api/tch/*`, and `/api/acd/subjects` verified; 401 unauthenticated check verified; authenticated `admin` returning 10 sorted subjects verified; `principal` and `teacher.budi` authentication and homeroom scoping on `7A`/`7B` verified; host cleanly stopped). Changed files: `src/backend/src/ElsasaSis.Api/Infrastructure/AcademicContract.cs`, `src/backend/src/ElsasaSis.Api/Features/Academic/GetSubjects/{SubjectResponse,GetSubjectsHandler,GetSubjectsEndpoint}.cs`, `src/backend/src/ElsasaSis.Api/Program.cs`.

---

## P3 - Backend Quality Suites & Frontend OpenAPI Contract Sync

Implementation Status: IMPLEMENTED
Review Status: GO

### P3-S11

Title: Expand `ElsasaSis.ArchitectureTests` for `Modules.Std` & `Modules.Tch` Isolation and CQRS Rules

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Extend NetArchTest rules in `ElsasaSis.ArchitectureTests` to enforce full 4-module boundary isolation (`Auth`, `Opr`, `Std`, `Tch`), Domain layer purity, Write-slice handler CQRS discipline, and EF Core prohibition across all Stage 1 assemblies.

Depends On: P2-S10

Repository: src/backend

Completion Criteria:

1. Update `AssemblyArchitectureContext.cs` to add `StdModuleAssembly` and `TchModuleAssembly` to `AllApplicationAssemblies`.
2. Update `ModuleBoundaryTests.cs` to assert zero cross-module type dependencies and zero assembly references between every pair of `{Auth, Opr, Std, Tch}`, zero references to `Api`/`Database` from any module, zero module refs from `SharedKernel`/`Contracts`, and zero `Microsoft.EntityFrameworkCore` references across all application assemblies.
3. Update `LayerAndCqrsArchitectureTests.cs` to include `StdModuleAssembly` and `TchModuleAssembly` in both `DomainLayer_ShouldNotDependOn_Infrastructure_Features_DataAccess_Or_Http` and `WriteSliceHandlers_ShouldNotDependOn_SqlConnectionFactory_Or_Dapper`.
4. `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj` passes with `0` failures.

Notes: Runs in-memory with zero database or Docker dependency; executes concurrently with `P3-S12` and `P3-S13`.

Implementation Notes: Extended NetArchTest architecture suite to cover all four modules (`Auth`, `Opr`, `Std`, `Tch`): (1) Updated `AssemblyArchitectureContext.cs` to expose `StdModuleAssembly` (via `StdAssemblyMarker`) and `TchModuleAssembly` (via `TchAssemblyMarker`) and include both in `AllApplicationAssemblies`. (2) Updated `ModuleBoundaryTests.cs` to enforce strict boundary isolation across all four modules (`Auth`, `Opr`, `Std`, `Tch`), asserting zero cross-module type dependencies and assembly references across all pairwise combinations, zero references to `Api`/`Database` from any module, zero module references from `SharedKernel`/`Contracts`, and zero `Microsoft.EntityFrameworkCore` dependencies or assembly references across all 8 application assemblies. (3) Updated `LayerAndCqrsArchitectureTests.cs` to include `StdModuleAssembly` and `TchModuleAssembly` in the `DomainLayer_ShouldNotDependOn_Infrastructure_Features_DataAccess_Or_Http` theory data and expanded `WriteSliceHandlers_ShouldNotDependOn_SqlConnectionFactory_Or_Dapper` across all 4 module assemblies. Verified `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj` passed with 0 failures (11/11 tests passed in 525ms) and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` passed (216/216 passed). Changed files: `src/backend/tests/ElsasaSis.ArchitectureTests/AssemblyArchitectureContext.cs`, `src/backend/tests/ElsasaSis.ArchitectureTests/ModuleBoundaryTests.cs`, `src/backend/tests/ElsasaSis.ArchitectureTests/LayerAndCqrsArchitectureTests.cs`.

---

### P3-S12

Title: Implement Stage 1 Integration Tests (`Opr`, `Std`, `Tch`, `Acd`, Multi-Role Auth & Homeroom Scoping)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Add full HTTP + SQL Server `WebApplicationFactory<Program>` + `Respawn` integration test coverage for all 20 Stage 1 use cases, role authorization matrices, atomic NIS enrollment, and homeroom teacher `403` roster isolation.

Depends On: P2-S10

Repository: src/backend

Completion Criteria:

1. Update `IntegrationTestWebApplicationFactory.cs` to include `new Table("acd", "subjects")` in `RespawnerOptions.TablesToIgnore` so seeded K-12 subjects persist across tests while all mutable tables reset cleanly (keeping all Stage 0 integration tests passing unmodified).
2. Update `IntegrationTestBase.cs` with `SeedStage1FixturesAsync()` (invoking `Stage1Seeder.SeedRoleAccountsAndFixturesAsync(...)`) and `CreateAuthenticatedClientAsync(username, password)`.
3. Create `ElsasaSis.IntegrationTests/Opr/Stage1OprIntegrationTests.cs`: calendar entries CRUD + out-of-bounds `BR-OPS-007` (`422`) + duplicate date `BR-OPS-008` (`409`); rooms CRUD + duplicate name `409` + non-positive capacity `422`; announcements draft/update/publish + duplicate publish `409` + role/audience filtering for `teacher.budi` and `principal`.
4. Create `ElsasaSis.IntegrationTests/Std/Stage1StdIntegrationTests.cs`: candidate registration (`201`) -> Principal (`principal`) approves (`ACCEPTED` with `decision_notes`) -> `GET next-nis` returns `YYYY0001` -> Admin enrolls candidate atomically (verifies `std.students`, cloned `std.student_guardians`, `std.candidates.admission_status = 'ENROLLED'`, duplicate NIS `409`) -> update student & guardians (verifies `BR-STU-003` deleting last guardian returns `409` and duplicate NISN returns `409`) -> create/update study group with homeroom teacher -> place student into `7A` (verifies `BR-STU-004` upsert and `BR-STU-008` mismatch rejection) -> `teacher.budi` logs in and `GET /api/std/study-groups/{7A}/roster` returns `200` with student & guardian phone numbers while `GET /api/std/study-groups/{7B}/roster` returns `403 Forbidden`.
5. Create `ElsasaSis.IntegrationTests/Tch/Stage1TchIntegrationTests.cs`: `GET /api/acd/subjects` returns all 10 seeded subjects -> create/update teacher (`BR-TCH-002` missing phone & email returns `422`, duplicate NIP/NIK returns `409`) -> add/remove capability (`BR-TCH-006` duplicate `409`) -> assign teacher (rejects missing capability `BR-TCH-005` `409`, rejects inactive teacher `BR-TCH-003` `409`, rejects active duplicate `BR-TCH-004` `409`, succeeds when active & capable, removes `ASSIGNED -> REMOVED`, and cleanly reassigns same tuple) -> deactivate teacher preserves existing assignments (`BR-TCH-007`) -> `principal` calls `GET /api/tch/workload` (`200`) and verifies workload counts.
6. `dotnet test src/backend/ElsasaSis.sln` passes ALL `UnitTests`, `ArchitectureTests`, and `IntegrationTests` with `0` failures and zero Docker dependency.

Notes: Never modify Stage 0 test assertions in `AcademicYearsIntegrationTests.cs` or `AuthIntegrationTests.cs`; verify they remain 100% green.

Implementation Notes: Implemented Stage 1 HTTP + SQL Server WebApplicationFactory<Program> + Respawn integration test coverage: (1) Updated IntegrationTestWebApplicationFactory.cs to include new Table("acd", "subjects") in RespawnerOptions.TablesToIgnore, ensuring seeded K-12 subjects persist cleanly across test executions. (2) Updated IntegrationTestBase.cs with SeedStage1FixturesAsync() (delegating to Stage1Seeder.SeedRoleAccountsAndFixturesAsync) and generic CreateAuthenticatedClientAsync(username, password). (3) Created Stage1OprIntegrationTests.cs testing calendar entries CRUD + out-of-bounds BR-OPS-007 (422) + duplicate date BR-OPS-008 (409); rooms CRUD + duplicate name (409) + non-positive capacity (422); announcements draft/update/publish + duplicate publish (409) + role and audience filtering for teacher.budi and principal. (4) Created Stage1StdIntegrationTests.cs testing candidate registration (201) -> principal approval (ACCEPTED with decision notes) -> GET next-nis (YYYY0001) -> atomic candidate enrollment (verifying std.students, cloned std.student_guardians, std.candidates status transition, duplicate NIS 409) -> update student with NISN -> duplicate NISN 409 -> deleting last guardian BR-STU-003 (409) -> study group placement into 7A with BR-STU-008 mismatch rejection (409) and BR-STU-004 upsert -> homeroom teacher.budi 200 roster access for 7A and 403 Forbidden isolation for 7B. (5) Created Stage1TchIntegrationTests.cs testing GET /api/acd/subjects (10 seeded subjects) -> teacher CRUD with BR-TCH-002 missing phone & email (422) and duplicate NIP/NIK (409) -> capability add/duplicate BR-TCH-006 (409)/remove -> teacher assignment with BR-TCH-005 missing capability (409), active & capable success (201), duplicate assignment BR-TCH-004 (409), removal, clean reassignment/reactivation, inactive teacher rejection BR-TCH-003 (409) -> deactivating teacher preserves assignments BR-TCH-007 -> principal workload oversight query (200). All suites executed and passed: ElsasaSis.UnitTests (216/216 passed), ElsasaSis.ArchitectureTests (11/11 passed), ElsasaSis.IntegrationTests (18/18 passed; 12 Stage 0 + 6 Stage 1 tests); total 245 tests green across ElsasaSis.sln with zero Docker dependency. Changed files: src/backend/tests/ElsasaSis.IntegrationTests/Fixtures/IntegrationTestWebApplicationFactory.cs, src/backend/tests/ElsasaSis.IntegrationTests/Fixtures/IntegrationTestBase.cs, src/backend/tests/ElsasaSis.IntegrationTests/Opr/Stage1OprIntegrationTests.cs, src/backend/tests/ElsasaSis.IntegrationTests/Std/Stage1StdIntegrationTests.cs, src/backend/tests/ElsasaSis.IntegrationTests/Tch/Stage1TchIntegrationTests.cs.

---

### P3-S13

Title: Regenerate Stage 1 OpenAPI Contract (`schema.d.ts`) & Implement Shared Lookup API Client

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Synchronize `schema.d.ts` from running `ElsasaSis.Api` `/swagger/v1/swagger.json` and provide a lightweight shared read-only lookup module (`lookups.api.ts`) so all 5 Phase 4 screen slices consume authoritative generated TypeScript DTOs in parallel with zero cross-feature imports.

Depends On: P1-S03, P2-S10

Repository: src/frontend

Completion Criteria:

1. Start `ElsasaSis.Api` temporarily in `Development`, regenerate `src/frontend/src/shared/types/api/schema.d.ts` via `openapi-typescript` against `/swagger/v1/swagger.json`, verify zero drift with `--check`, and stop the temporary API server cleanly.
2. Confirm `schema.d.ts` contains typed operations and schemas for `/api/opr/calendar-entries*`, `/api/opr/rooms*`, `/api/opr/announcements*`, `/api/std/candidates*`, `/api/std/students*`, `/api/std/study-groups*`, `/api/tch/teachers*`, `/api/tch/assignments*`, `/api/tch/workload`, and `/api/acd/subjects`.
3. Create `src/frontend/src/shared/services/lookups.api.ts` exposing strongly-typed TanStack Query keys and read-only lookup functions (`fetchAcademicYearsLookup`, `fetchSubjectsLookup`, `fetchActiveTeachersLookup`, `fetchStudyGroupsLookup`) strictly backed by `apiClient` and `schema.d.ts` (zero imports from `features/` or `app/`).
4. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass in `src/frontend/`.

Implementation Notes:
- Started `ElsasaSis.Api` on custom port 5055 with `--environment Development`.
- Executed `openapi-typescript` against `http://localhost:5055/swagger/v1/swagger.json` to synchronize `src/frontend/src/shared/types/api/schema.d.ts`; verified zero drift with `--check` and cleanly stopped the temporary API process.
- Verified typed OpenAPI operations and DTO schemas across all Stage 1 endpoints: `/api/opr/calendar-entries*`, `/api/opr/rooms*`, `/api/opr/announcements*`, `/api/std/candidates*`, `/api/std/students*`, `/api/std/study-groups*`, `/api/tch/teachers*`, `/api/tch/assignments*`, `/api/tch/workload`, and `/api/acd/subjects`.
- Created `src/frontend/src/shared/services/lookups.api.ts` exposing strongly-typed query keys (`lookupKeys`), read-only lookup query functions (`fetchAcademicYearsLookup`, `fetchSubjectsLookup`, `fetchActiveTeachersLookup`, `fetchStudyGroupsLookup`), grouped `lookupsApi` export, and TanStack `queryOptions` helpers (`academicYearsLookupQuery`, `subjectsLookupQuery`, `activeTeachersLookupQuery`, `studyGroupsLookupQuery`), strictly importing only `apiClient` and `schema.d.ts` with zero cross-slice or app imports.
- Created `src/frontend/src/shared/services/__tests__/lookups.api.spec.ts` with 11 MSW unit tests verifying endpoints, query params, fallbacks, query keys, and TanStack queryOptions.
- All checks green: `pnpm typecheck` (0 errors), `pnpm lint` (0 warnings/errors), and `pnpm test:unit` (16 test files, 71 tests passing).

Notes: Unlocks all 5 parallel screen slices (`P4-S14` through `P4-S18`) simultaneously.

---

## P4 - Frontend Stage 1 Feature Screens (5 Parallel Screens)

Implementation Status: IMPLEMENTED
Review Status: GO

### P4-S14

Title: Expand `SCR-OPR-CALENDAR` (`CalendarWorkspaceView.vue`) to Full 3-Tab Operational Workspace

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Expand `src/frontend/src/features/operations/calendar/` to host all 3 cohesive tabs (`Academic Cycle`, `Calendar & Holidays`, `Rooms & Facilities`) realizing `UC-ADM-01`, `UC-ADM-02`, `UC-ADM-03`, `UC-ADM-27`, `UC-PRC-07`, and `UC-TCH-11`.

Depends On: P3-S13

Repository: src/frontend

Completion Criteria:

1. Add `calendarEntries` & `rooms` API clients, query keys, TanStack Query hooks/mutations, and Zod schemas under `features/operations/calendar/api/` and `schemas/` strictly typed against `schema.d.ts`.
2. Implement `CalendarEntriesTab.vue` + `CalendarEntryFormDrawer.vue`: filters by active/selected Academic Year (via `useAppContextBridge()`), day type (`SCHOOL_DAY | HOLIDAY | CLOSURE`), date listing with `StatusPill`, create/edit drawer with VeeValidate + Zod + `422` field error / `409` conflict banner, and Tier 2 `ConfirmActionDialog` delete workflow.
3. Implement `RoomsTab.vue` + `RoomFormDrawer.vue`: lists rooms (`name`, `roomType`, `capacity`), search/type filter, create/edit drawer enforcing positive capacity and handling `409` duplicate room name.
4. Update `CalendarWorkspaceView.vue` to render PrimeVue `Tabs` (Tab 1: Academic Cycle preserving all Stage 0 functionality; Tab 2: Calendar & Holidays; Tab 3: Rooms & Facilities), adapting action buttons via `useCurrentSession().hasPermission(...)` so read-only viewers (`PRINCIPAL`, `TEACHER`) see clean read-only calendar/room views.
5. Add Vitest + MSW tests covering calendar entry create/delete, room create/edit, `422`/`409` mapping, and read-only permission gating while keeping existing Stage 0 tests green.
6. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass.

Implementation Notes:
- Added `calendarEntries.api.ts`, `calendarEntries.keys.ts`, `useCalendarEntryQueries.ts`, and `useCalendarEntryMutations.ts` strictly typed against `schema.d.ts` supporting listing, creation, updates, and deletion.
- Added `rooms.api.ts`, `rooms.keys.ts`, `useRoomQueries.ts`, and `useRoomMutations.ts` strictly typed against `schema.d.ts` supporting room listing, type filtering, creation, and updates.
- Created `calendarEntry.schema.ts` (validating date, `SCHOOL_DAY | HOLIDAY | CLOSURE`, description) and `room.schema.ts` (enforcing name, `CLASSROOM | LABORATORY | HALL | OTHER`, positive integer capacity).
- Implemented `CalendarEntryFormDrawer.vue` and `CalendarEntriesTab.vue`: academic year filtering via `useAppContextBridge()` and lookups query, `StatusPill` day type rendering, VeeValidate + Zod form handling, 422 camelCase field errors, 409 conflict banner, and Tier 2 `ConfirmActionDialog` deletion workflow.
- Implemented `RoomFormDrawer.vue` and `RoomsTab.vue`: facility search, facility type filter, positive capacity enforcement, 409 duplicate name conflict banner.
- Updated `CalendarWorkspaceView.vue` with PrimeVue `Tabs` hosting Tab 1 (`Academic Cycle`), Tab 2 (`Calendar & Holidays`), and Tab 3 (`Rooms & Facilities`), with permission gating via `useCurrentSession().hasPermission(...)` ensuring read-only viewers (`PRINCIPAL`, `TEACHER`) see clean read-only views without management controls.
- Added comprehensive unit and MSW specs (`calendarEntry.schema.spec.ts`, `room.schema.spec.ts`, `CalendarEntriesTab.spec.ts`, `RoomsTab.spec.ts`, expanded `CalendarWorkspaceView.spec.ts` with tab switching and read-only permission gating).
- Verified all 32 calendar tests pass and full test suite (154/154 tests) passes with 0 failures; eslint passes with 0 warnings/errors.

Notes: Owns ONLY `src/frontend/src/features/operations/calendar/**`.

---

### P4-S15

Title: Implement `SCR-OPR-ANNOUNCEMENTS` (`AnnouncementsView.vue`)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement the School Announcements & Bulletin screen under `src/frontend/src/features/operations/announcements/` realizing `UC-ADM-05`, `UC-ADM-27`, `UC-PRC-07`, and `UC-TCH-11`.

Depends On: P3-S13

Repository: src/frontend

Completion Criteria:

1. Implement `api/announcements.api.ts`, `announcements.keys.ts`, `useAnnouncementQueries.ts`, `useAnnouncementMutations.ts`, and `schemas/announcement.schema.ts` typed against `schema.d.ts`.
2. Implement `components/AnnouncementFormDrawer.vue` (compose/edit `DRAFT` title, `targetAudience` selector `ALL | TEACHERS | STUDENTS | PARENTS`, content textarea — no `v-html`, VeeValidate + Zod, RFC 7807 `422`/`409` handling) and `components/AnnouncementDetailDialog.vue` (plain-text formatted bulletin reader).
3. Replace stub `views/AnnouncementsView.vue`: Information Lookup pattern wrapped in `AppErrorBoundary`, audience filter pills (`ALL`, `TEACHERS`, `STUDENTS`, `PARENTS`), status filter (`DRAFT`, `PUBLISHED` shown only for authors), search bar, `StatusPill`, Tier 2 `ConfirmActionDialog` for publishing (`DRAFT -> PUBLISHED`), and role-gated actions (`Create`/`Publish` hidden for read-only users).
4. Add Vitest + MSW specs covering schema validation, draft creation, publish flow, audience filtering, and read-only teacher/principal view.
5. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass.

Notes: Owns ONLY `src/frontend/src/features/operations/announcements/**`.

Implementation Notes:
- Implemented strongly-typed API client and TanStack query/mutation hooks under `src/frontend/src/features/operations/announcements/api/`:
  - `announcements.keys.ts`: hierarchical query keys (`announcementKeys.all`, `lists`, `list(params)`, `details`, `detail(id)`).
  - `announcements.api.ts`: typed against `schema.d.ts` (`list`, `getById`, `create`, `update`, `publish`).
  - `useAnnouncementQueries.ts`: reactive TanStack query hooks (`useAnnouncementsQuery`, `useAnnouncementDetailQuery`).
  - `useAnnouncementMutations.ts`: mutations for `create`, `update`, and `publish` with cache invalidation and toast notifications.
- Implemented Zod validation schemas under `schemas/`:
  - `announcement.schema.ts`: title length (1..255), target audience (`ALL | TEACHERS | STUDENTS | PARENTS`), plain-text content required, optional UUID academic year ID.
  - `schemas/__tests__/announcement.schema.spec.ts`: 8 unit tests covering valid payloads, edge-case trimming, length constraints, and audience enum validation.
- Implemented `components/AnnouncementFormDrawer.vue`:
  - VeeValidate + Zod schema integration.
  - PrimeVue `Drawer`, `InputText`, `Select`, and plain-text `Textarea`.
  - RFC 7807 `422 Unprocessable Content` camelCase field mapping and `409 Conflict` banner handling.
  - Zero `v-html` usage for security.
- Implemented `components/AnnouncementDetailDialog.vue`:
  - Plain-text formatted bulletin reader displaying full announcements (`whitespace-pre-wrap` styling, strict text interpolation, zero `v-html`).
  - Metadata display: author, academic year snapshot, creation/publication timestamp, audience badge, and `StatusPill`.
  - Contextual action triggers (`Publish`, `Edit`) gated by permissions and draft status.
- Replaced stub `views/AnnouncementsView.vue`:
  - Information Lookup pattern wrapped in `AppErrorBoundary`.
  - Search toolbar with real-time text query and reset pagination.
  - Audience filter pills (`ALL`, `TEACHERS`, `STUDENTS`, `PARENTS`).
  - Status filter dropdown (`DRAFT`, `PUBLISHED`) rendered conditionally only for authors/administrators with manage permissions.
  - Role-gated actions: `Create`, `Edit`, and `Publish` hidden for read-only viewers (`TEACHER`, `PRINCIPAL` without manage permissions).
  - Tier 2 `ConfirmActionDialog` for publishing transition (`DRAFT -> PUBLISHED`).
- Implemented Vitest + MSW test suite in `views/__tests__/AnnouncementsView.spec.ts`:
  - 7 MSW test scenarios covering table rendering, audience pill filtering, draft creation via drawer, publish confirmation workflow, read-only teacher view, read-only principal view, and plain-text security against HTML injection.
- Verified quality gates:
  - Vitest: 15/15 tests passing across `announcement.schema.spec.ts` (8) and `AnnouncementsView.spec.ts` (7).
  - ESLint: zero warnings or errors (`eslint src/features/operations/announcements --max-warnings 0`).
  - Prettier: 100% formatted.
  - Vue-tsc: zero TypeScript errors in `src/features/operations/announcements/**`.
- Changed files:
  - `src/frontend/src/features/operations/announcements/api/announcements.api.ts`
  - `src/frontend/src/features/operations/announcements/api/announcements.keys.ts`
  - `src/frontend/src/features/operations/announcements/api/useAnnouncementMutations.ts`
  - `src/frontend/src/features/operations/announcements/api/useAnnouncementQueries.ts`
  - `src/frontend/src/features/operations/announcements/components/AnnouncementDetailDialog.vue`
  - `src/frontend/src/features/operations/announcements/components/AnnouncementFormDrawer.vue`
  - `src/frontend/src/features/operations/announcements/schemas/__tests__/announcement.schema.spec.ts`
  - `src/frontend/src/features/operations/announcements/schemas/announcement.schema.ts`
  - `src/frontend/src/features/operations/announcements/views/__tests__/AnnouncementsView.spec.ts`
  - `src/frontend/src/features/operations/announcements/views/AnnouncementsView.vue`
  - `docs/issues/IS002- STAGE-1-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S16

Title: Implement `SCR-STD-ADMISSIONS` (`AdmissionsQueueView.vue`)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement the Admissions Processing Queue workbench under `src/frontend/src/features/student/admissions/` realizing `UC-ADM-06`, `UC-ADM-07`, `UC-PRC-01`, `UC-ADM-08`, and `UC-ADM-12`.

Depends On: P3-S13

Repository: src/frontend

Completion Criteria:

1. Implement `api/admissions.api.ts`, `admissions.keys.ts`, `useAdmissionsQueries.ts`, `useAdmissionsMutations.ts`, and Zod schemas (`candidate.schema.ts`, `decision.schema.ts`, `enrollment.schema.ts`) typed against `schema.d.ts`.
2. Replace stub `views/AdmissionsQueueView.vue` using `WorkbenchLayout.vue` (Work Queue pattern) wrapped in `AppErrorBoundary`:
   - Left Worklist: status tabs (`REGISTERED`, `ACCEPTED`, `REJECTED`, `ENROLLED`), search input, admission year filter (defaulting to active year via `useAppContextBridge()`), scannable <=4-element cards, URL query sync (`?candidateId=...&status=...`).
   - Right Workbench: full candidate demographics, guardian contact cards, decision history/notes, contextual actions gated by `useCurrentSession().hasPermission(...)`, and post-enrollment deep-link button to `{ name: 'students.directory', query: { studentId } }`.
3. Implement `components/RegisterCandidateDrawer.vue` (personal info + dynamic 1..N guardians array enforcing `BR-STU-007`), `components/AdmissionDecisionModal.vue` (1-step Accept / Reject with `decisionNotes`, Tier 2 confirmation tone on Reject, enabled for `Admissions.Decide` or `Admissions.Approve`), and `components/EnrollCandidateModal.vue` (Tier 3 Pre-Flight Impact Modal fetching `GET /api/std/candidates/{id}/next-nis` on open, pre-filling editable NIS override input + optional NISN + enrollment date, rendering `409 Conflict` alert banner cleanly).
4. Add Vitest + MSW specs covering candidate intake, Principal/Admin accept/reject decision, enrollment modal pre-filled NIS + override + `409` handling.
5. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass.

Notes: Owns ONLY `src/frontend/src/features/student/admissions/**`.

Implementation Notes:
- Implemented strongly-typed API client and TanStack query/mutation hooks under `src/frontend/src/features/student/admissions/api/`:
  - `admissions.keys.ts`: hierarchical query keys (`admissionsKeys.all`, `candidates(params)`, `candidate(id)`, `nextNis(id, date)`).
  - `admissions.api.ts`: typed against `schema.d.ts` (`list`, `getById`, `register`, `recordDecision`, `getNextNis`, `enroll`).
  - `useAdmissionsQueries.ts`: reactive TanStack query hooks (`useCandidatesQuery`, `useCandidateDetailQuery`, `useNextNisQuery`).
  - `useAdmissionsMutations.ts`: mutations for `useRegisterCandidateMutation`, `useRecordAdmissionDecisionMutation`, and `useEnrollCandidateMutation` with cache invalidation and toast notifications.
- Implemented Zod validation schemas under `schemas/`:
  - `candidate.schema.ts`: candidate personal demographics, non-future DOB refinement, and dynamic guardians array enforcing `BR-STU-007` (minimum 1 guardian contact required).
  - `decision.schema.ts`: decision enum (`ACCEPTED`, `REJECTED`) and optional `decisionNotes` (max 500 characters).
  - `enrollment.schema.ts`: required sequential NIS (max 30), optional NISN (max 20), and required enrollment date.
  - `schemas/__tests__/admissions.schema.spec.ts`: 12 unit tests covering candidate intake, multi-guardians, empty guardian rejection (`BR-STU-007`), future DOB rejection, decision validation, and NIS format constraints.
- Implemented `components/RegisterCandidateDrawer.vue`:
  - VeeValidate + Zod schema integration with dynamic guardian array (`useFieldArray`).
  - Enforces `BR-STU-007`: removal of guardian contact is disabled/prevented when only 1 guardian exists.
  - PrimeVue `Drawer`, `FormField`, `InputText`, `Textarea`, and native styled selects.
  - RFC 7807 `422` camelCase field mapping and `409 Conflict` banner handling.
- Implemented `components/AdmissionDecisionModal.vue`:
  - 1-step Accept / Reject decision modal realizing `UC-ADM-07` and `UC-PRC-01`.
  - Tier 2 Confirmation Tone warning banner when selecting `REJECTED`.
  - Gated for `Admissions.Decide` or `Admissions.Approve`.
- Implemented `components/EnrollCandidateModal.vue`:
  - Tier 3 Pre-Flight Impact Modal realizing `UC-ADM-08`.
  - Automatically queries `GET /api/std/candidates/{id}/next-nis` on open, pre-filling editable sequential NIS override input + optional NISN + enrollment date.
  - Renders clean RFC 7807 `409 Conflict` alert banner when duplicate NIS is rejected by backend.
- Replaced stub `views/AdmissionsQueueView.vue`:
  - `WorkbenchLayout.vue` (Work Queue pattern) wrapped in `AppErrorBoundary`.
  - Left Worklist: status tabs (`REGISTERED`, `ACCEPTED`, `REJECTED`, `ENROLLED`), admission year filter (defaulting to active year via `useAppContextBridge()`), search input, scannable <=4-element cards (`FullName + Nickname`, `SchoolLevel + Grade badge`, `RegisteredAt date`, `StatusPill`).
  - URL query bidirectional synchronization (`?status=...&candidateId=...`).
  - Right Workbench: full candidate demographics, dynamic guardian contact cards, decision and enrollment audit, contextual action buttons gated by `useCurrentSession().hasPermission(...)`, and post-enrollment deep-link button navigating to `{ name: 'students.directory', query: { studentId } }`.
- Implemented Vitest + MSW specs:
  - `views/__tests__/AdmissionsQueueView.spec.ts` (6 tests): work queue rendering, status tabs switching + URL sync, card selection + workbench details, candidate intake via drawer enforcing `BR-STU-007`, Principal/Admin accept/reject decision with Tier 2 confirmation tone, and enrollment flow with pre-filled sequential NIS, 409 conflict handling, and directory deep-link navigation.
  - `components/__tests__/admissions.components.spec.ts` (2 tests): modal-level tests for `AdmissionDecisionModal` and `EnrollCandidateModal`.
- Verified quality gates:
  - Unit/Component tests: 20/20 admissions tests passing; 28/28 test files and 154/154 tests passing across full frontend unit suite (`pnpm test:unit`).
  - ESLint: zero warnings or errors (`eslint . --max-warnings 0`).
  - Vue-tsc: zero TypeScript errors across all files in `src/features/student/admissions/**`.
- Changed files:
  - `src/frontend/src/features/student/admissions/api/admissions.api.ts`
  - `src/frontend/src/features/student/admissions/api/admissions.keys.ts`
  - `src/frontend/src/features/student/admissions/api/useAdmissionsMutations.ts`
  - `src/frontend/src/features/student/admissions/api/useAdmissionsQueries.ts`
  - `src/frontend/src/features/student/admissions/components/AdmissionDecisionModal.vue`
  - `src/frontend/src/features/student/admissions/components/EnrollCandidateModal.vue`
  - `src/frontend/src/features/student/admissions/components/RegisterCandidateDrawer.vue`
  - `src/frontend/src/features/student/admissions/components/__tests__/admissions.components.spec.ts`
  - `src/frontend/src/features/student/admissions/schemas/candidate.schema.ts`
  - `src/frontend/src/features/student/admissions/schemas/decision.schema.ts`
  - `src/frontend/src/features/student/admissions/schemas/enrollment.schema.ts`
  - `src/frontend/src/features/student/admissions/schemas/__tests__/admissions.schema.spec.ts`
  - `src/frontend/src/features/student/admissions/views/AdmissionsQueueView.vue`
  - `src/frontend/src/features/student/admissions/views/__tests__/AdmissionsQueueView.spec.ts`
  - `docs/issues/IS002- STAGE-1-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S17

Title: Implement `SCR-STD-DIRECTORY` (`StudentDirectoryView.vue` — Roster, Guardians, Study Groups, Placements & Homeroom Scoping)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement the Student Directory & Placement Registry screen under `src/frontend/src/features/student/directory/` realizing `UC-ADM-09`, `UC-ADM-10`, `UC-ADM-11`, `UC-ADM-12`, and `UC-HRM-05`.

Depends On: P3-S13

Repository: src/frontend

Completion Criteria:

1. Implement `api/students.api.ts`, `studyGroups.api.ts`, `students.keys.ts`, TanStack Query/Mutation hooks, and Zod schemas (`student.schema.ts`, `guardian.schema.ts`, `studyGroup.schema.ts`, `placement.schema.ts`) typed against `schema.d.ts`, using `shared/services/lookups.api.ts` for academic years and active teachers.
2. Replace stub `views/StudentDirectoryView.vue` wrapped in `AppErrorBoundary`:
   - **Homeroom Teacher Scoped Mode (`UC-HRM-05`)**: when `useCurrentSession()` has `Permissions.Students.Roster.ViewOwn` without `Permissions.Students.Directory.View`, displays a dedicated scoped homeroom header badge (e.g., `"My Homeroom Class (7A)"`), fetches strictly the homeroom teacher's study group(s) & `GET /api/std/study-groups/{id}/roster`, highlights student details + guardian emergency phone numbers clearly, and hides all admin edit/placement/study-group management controls.
   - **Full Admin/Read Directory Mode**: 2 cohesive tabs:
     - Tab 1 (**Student Roster Directory**): searchable/filterable roster (`search`, `grade`, `studyGroupId`) + student dossier panel/drawer (`StudentEditDrawer.vue` for profile + unique NISN; `GuardianManagerPanel.vue` for add/edit/remove guardians with disabled delete tooltip & `409` handling when only 1 guardian remains per `BR-STU-003`; placement history + quick **"Place in Study Group"** modal).
     - Tab 2 (**Study Groups & Placement Registry**): annual rombel table (`schoolLevel`, `grade`, `name`, `homeroomTeacherName`, `studentCount`), `StudyGroupFormDrawer.vue` with active Homeroom Teacher picker, `StudyGroupRosterDrawer.vue`, and `PlaceStudentModal.vue`.
3. Add Vitest + MSW specs verifying profile/guardian maintenance (including last-guardian protection), study group & placement workflows, and `HOMEROOM_TEACHER` scoped roster view with emergency phone numbers.
4. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass.

Notes: Owns ONLY `src/frontend/src/features/student/directory/**`.

Implementation Notes:
- Implemented strongly typed API client, query keys, and TanStack query/mutation hooks under `src/frontend/src/features/student/directory/api/`:
  - `students.keys.ts`: hierarchical query keys (`directoryKeys.all`, `students(params)`, `student(id)`, `guardians(id)`, `placements(id)`, `studyGroups(params)`, `studyGroup(id)`, `studyGroupRoster(id)`).
  - `students.api.ts`: typed against `schema.d.ts` (`listStudents`, `getStudentById`, `updateStudent`, `listGuardians`, `createGuardian`, `updateGuardian`, `deleteGuardian`, `listPlacements`).
  - `studyGroups.api.ts`: typed against `schema.d.ts` (`listStudyGroups`, `getStudyGroupById`, `createStudyGroup`, `updateStudyGroup`, `getRoster`, `placeStudent`).
  - `useStudentQueries.ts`: TanStack queries for students, student detail, guardians, student placements, study groups, and study group roster with active academic year scoping.
  - `useStudentMutations.ts`: TanStack mutations for updating student profile, guardian management (add, edit, remove with cache invalidation for student and roster queries), study group creation/update, and student placement.
- Implemented Zod validation schemas under `schemas/`:
  - `student.schema.ts`: profile update validation (NISN, non-future birth date, gender, phone, email, address).
  - `guardian.schema.ts`: guardian add/update validation (fullName, relationship, emergency contact phone, email, address, isPrimary).
  - `studyGroup.schema.ts`: study group create/update validation (academicYearId, schoolLevel, grade, name, homeroomTeacherId).
  - `placement.schema.ts`: placement validation (studentId, studyGroupId).
  - `schemas/__tests__/directory.schemas.spec.ts`: 10 unit tests verifying schema constraints, non-future DOB, phone validation, and required fields.
- Implemented modular component architecture:
  - `StudentEditDrawer.vue`: PrimeVue `Drawer` + VeeValidate for profile updates, unique NISN validation, RFC 7807 `422` field error mapping, and `409` conflict banner handling.
  - `GuardianManagerPanel.vue`: guardian list, inline add/edit modal, last-guardian protection enforcing `BR-STU-003` (delete button disabled with tooltip when only 1 guardian exists, plus RFC 7807 `409` conflict banner fallback on concurrent deletion).
  - `PlaceStudentModal.vue`: quick modal to assign a student to a study group with study group dropdown and optimistic/toast updates.
  - `StudyGroupFormDrawer.vue`: study group creation and editing with active teacher picker from `lookups.api.ts`.
  - `StudyGroupRosterDrawer.vue`: slide-out roster list showing students in the rombel with remove-placement action.
  - `StudentDossierDrawer.vue`: cohesive drawer combining profile header, status badge, demographics, guardian manager (`GuardianManagerPanel.vue`), and placement history with quick-placement action.
- Implemented `views/StudentDirectoryView.vue` wrapped in `AppErrorBoundary`:
  - **Homeroom Teacher Scoped Mode (`UC-HRM-05`)**: When `useCurrentSession()` possesses `Permissions.Students.Roster.ViewOwn` without `Permissions.Students.Directory.View`, automatically renders a dedicated homeroom view with `"My Homeroom Class (7A)"` badge, fetches strictly the homeroom teacher's study group and `GET /api/std/study-groups/{id}/roster`, highlights student names + guardian emergency phone numbers prominently with phone icons/badges, and hides all admin edit, placement, and study group creation controls.
  - **Full Admin Directory Mode**: 2 cohesive tabs:
    - Tab 1 (**Student Roster Directory**): searchable/filterable roster (`search`, `grade`, `studyGroupId`, `status`), student action buttons, deep linking support via `?studentId=...` query parameter, and `StudentDossierDrawer.vue`.
    - Tab 2 (**Study Groups & Placement Registry**): annual rombel table (`schoolLevel`, `grade`, `name`, `homeroomTeacherName`, `studentCount`), search & grade filter, `StudyGroupFormDrawer.vue`, `StudyGroupRosterDrawer.vue`, and `PlaceStudentModal.vue`.
- Vitest + MSW testing:
  - `views/__tests__/StudentDirectoryView.spec.ts` (5 tests):
    1. Full Admin Directory Mode rendering with 2 tabs and student roster.
    2. Homeroom Teacher Scoped Mode (`UC-HRM-05`) with scoped header badge, student table, and guardian emergency contact numbers prominently rendered.
    3. Last-guardian protection (`BR-STU-003`): disabled delete button and tooltip when only 1 guardian remains.
    4. 409 Conflict handling on duplicate NISN update and last-guardian deletion attempts.
    5. Study Groups & Placement Registry tab rendering and roster drawer trigger.
  - `schemas/__tests__/directory.schemas.spec.ts` (10 tests): valid/invalid data validations.
- Quality gates verified:
  - Unit tests: 15/15 tests passing across `src/features/student/directory/**`.
  - ESLint: zero warnings or errors (`pnpm eslint src/features/student/directory`).
  - Vue-tsc: zero TypeScript errors across `src/features/student/directory/**`.
- Changed files:
  - `src/frontend/src/features/student/directory/api/students.keys.ts`
  - `src/frontend/src/features/student/directory/api/students.api.ts`
  - `src/frontend/src/features/student/directory/api/studyGroups.api.ts`
  - `src/frontend/src/features/student/directory/api/useStudentQueries.ts`
  - `src/frontend/src/features/student/directory/api/useStudentMutations.ts`
  - `src/frontend/src/features/student/directory/schemas/student.schema.ts`
  - `src/frontend/src/features/student/directory/schemas/guardian.schema.ts`
  - `src/frontend/src/features/student/directory/schemas/studyGroup.schema.ts`
  - `src/frontend/src/features/student/directory/schemas/placement.schema.ts`
  - `src/frontend/src/features/student/directory/schemas/__tests__/directory.schemas.spec.ts`
  - `src/frontend/src/features/student/directory/components/StudentEditDrawer.vue`
  - `src/frontend/src/features/student/directory/components/GuardianManagerPanel.vue`
  - `src/frontend/src/features/student/directory/components/PlaceStudentModal.vue`
  - `src/frontend/src/features/student/directory/components/StudyGroupFormDrawer.vue`
  - `src/frontend/src/features/student/directory/components/StudyGroupRosterDrawer.vue`
  - `src/frontend/src/features/student/directory/components/StudentDossierDrawer.vue`
  - `src/frontend/src/features/student/directory/views/StudentDirectoryView.vue`
  - `src/frontend/src/features/student/directory/views/__tests__/StudentDirectoryView.spec.ts`
  - `docs/issues/IS002- STAGE-1-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S18

Title: Implement `SCR-TCH-ASSIGNMENTS` (`TeacherAssignmentsView.vue` — Faculty Directory, Capabilities, Allocations & Workload Matrix)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement the Teacher Directory & Teaching Assignment Workspace under `src/frontend/src/features/teaching/assignments/` realizing `UC-ADM-13`, `UC-ADM-14`, `UC-ADM-15`, `UC-ADM-16`, and `UC-PRC-05`.

Depends On: P3-S13

Repository: src/frontend

Completion Criteria:

1. Implement `api/teachers.api.ts`, `assignments.api.ts`, `teaching.keys.ts`, TanStack Query/Mutation hooks, and Zod schemas (`teacher.schema.ts`, `assignment.schema.ts`) typed against `schema.d.ts`, using `shared/services/lookups.api.ts` for live `acd.subjects` and `std.study_groups`.
2. Replace stub `views/TeacherAssignmentsView.vue` wrapped in `AppErrorBoundary` with 2 cohesive tabs:
   - Tab 1 (**Faculty Directory**): searchable/filterable faculty table (`fullName`, `nip`/`nik`, contact, `StatusPill`, capability tags, active load count), `TeacherFormDrawer.vue` (enforcing `BR-TCH-002` phone-or-email required, RFC 7807 `422`/`409`), Tier 2 `ConfirmActionDialog` for Deactivate (warning explicitly that existing assignments remain active per `BR-TCH-007`) / Activate, and `TeacherCapabilitiesDrawer.vue` (tag/remove real `acd.subjects`).
   - Tab 2 (**Teaching Allocations & Workload Oversight**): period-scoped teaching assignment matrix/table (filtered by active/selected `operationalPeriodId`, `studyGroupId`, `teacherId`, `subjectId`), `AssignTeacherDrawer.vue` (assign active capable teacher to study group + subject; handles `409 Conflict` for inactive teacher or missing capability gracefully), Tier 2 remove assignment dialog, and Leadership Workload Distribution KPI cards + per-teacher load breakdown table (`GET /api/tch/workload`) accessible to `PRINCIPAL` (`Oversight`, read-only) and `SCHOOL_ADMIN`.
3. Add Vitest + MSW specs verifying teacher form `BR-TCH-002`, capability tagging from `acd.subjects`, teaching assignment create/remove + `409` handling, and Principal read-only workload oversight.
4. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass.

Notes: Owns ONLY `src/frontend/src/features/teaching/assignments/**`.

Implementation Notes:
- Implemented strongly-typed API client and TanStack query/mutation hooks under `src/frontend/src/features/teaching/assignments/api/`:
  - `teaching.keys.ts`: hierarchical query keys (`teachingKeys.all`, `teachers()`, `teachersList(params)`, `teacherDetail(id)`, `assignments()`, `assignmentsList(params)`, `workload(params)`).
  - `teachers.api.ts`: typed against `schema.d.ts` (`list`, `getById`, `create`, `update`, `activate`, `deactivate`, `addCapability`, `removeCapability`).
  - `assignments.api.ts`: typed against `schema.d.ts` (`list`, `create`, `remove`, `getWorkload`).
  - `useTeacherQueries.ts` & `useTeacherMutations.ts`: query hooks (`useTeachersQuery`, `useTeacherDetailQuery`) and mutation hooks (`useCreateTeacherMutation`, `useUpdateTeacherMutation`, `useActivateTeacherMutation`, `useDeactivateTeacherMutation`, `useAddCapabilityMutation`, `useRemoveCapabilityMutation`) with toast feedback and query invalidations across teaching and shared lookups.
  - `useAssignmentQueries.ts` & `useAssignmentMutations.ts`: query hooks (`useAssignmentsQuery`, `useWorkloadQuery`) and mutation hooks (`useAssignTeacherMutation`, `useRemoveAssignmentMutation`).
- Implemented Zod validation schemas under `src/frontend/src/features/teaching/assignments/schemas/`:
  - `teacher.schema.ts`: enforces `BR-TCH-001` (legal name, birth date), `BR-TCH-002` (superRefine requiring at least one contact method: phone or email), `BR-TCH-008` (gender MALE/FEMALE), and max lengths.
  - `assignment.schema.ts`: `assignTeacherSchema` (validating UUIDs for teacher, operational period, study group, subject) and `addCapabilitySchema`.
  - Schema specs `teacher.schema.spec.ts` (11 tests) and `assignment.schema.spec.ts` (5 tests).
- Implemented UI components and main view under `src/frontend/src/features/teaching/assignments/`:
  - `components/TeacherFormDrawer.vue`: Drawer for registering and editing faculty profiles with VeeValidate + Zod, RFC 7807 422 field error mapping, and 409 conflict alert handling.
  - `components/TeacherCapabilitiesDrawer.vue`: Drawer for viewing and tagging/removing qualified curriculum subjects from live `acd.subjects` (`lookups.api.ts`), with 409 conflict detection for duplicate capabilities.
  - `components/AssignTeacherDrawer.vue`: Drawer for assigning active capable teachers to study groups and subjects for an operational period, with preview of teacher qualifications and graceful 409 Conflict handling for inactive teachers or missing capabilities (`BR-TCH-003`, `BR-TCH-005`).
  - `views/TeacherAssignmentsView.vue`: Cohesive 2-tab workspace wrapped in `AppErrorBoundary`:
    - Tab 1 (**Faculty Directory**): Searchable/filterable faculty table with full name, nickname, NIP/NIK, contact, `StatusPill`, capability tags, active class load counts, and actions for editing, managing capabilities, and deactivating/activating (with Tier 2 `ConfirmActionDialog` explicitly warning about active assignments per `BR-TCH-007`).
    - Tab 2 (**Teaching Allocations & Workload Oversight**): Period-scoped teaching assignment matrix with filters (study group, subject, teacher, search), "+ Assign Teacher" drawer trigger, Tier 2 remove assignment confirmation dialog, and Leadership Workload Distribution KPI summary cards + per-teacher load breakdown table (`GET /api/tch/workload`) accessible to `PRINCIPAL` (read-only oversight) and `SCHOOL_ADMIN`.
- Created comprehensive Vitest + MSW specs in `views/__tests__/TeacherAssignmentsView.spec.ts` (5 tests) verifying tabs/directory rendering, `BR-TCH-002` phone-or-email enforcement, capability tagging and removal from `acd.subjects`, teaching assignment create/remove + 409 handling, and Principal read-only leadership workload oversight.
- Quality gate checks:
  - `pnpm exec vue-tsc --noEmit`: 0 errors in `src/features/teaching/**`.
  - `pnpm lint src/features/teaching`: 0 warnings, 0 errors.
  - `vitest run src/features/teaching`: 3 test files, 21 tests passing.

---

## P5 - Stage 1 End-to-End Verification

Implementation Status: IMPLEMENTED
Review Status: GO

### P5-S19

Title: Stage 1 Frontend Quality Gates & Live Multi-Role Local API + Vite E2E Verification

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Verify all frontend build/quality checks (`typecheck`, `lint`, `test:unit`, `build`, `api:check`) and execute the complete multi-role Stage 1 verification gate against running local `ElsasaSis.Api` + Vite dev proxy.

Depends On: P3-S11, P3-S12, P4-S14, P4-S15, P4-S16, P4-S17, P4-S18

Repository: src/frontend

Completion Criteria:

1. `pnpm typecheck`, `pnpm lint`, `pnpm test:unit`, and `pnpm build` succeed with `0` errors in `src/frontend/`, and `openapi-typescript <api-url>/swagger/v1/swagger.json -o src/shared/types/api/schema.d.ts --check` confirms zero OpenAPI drift.
2. Against running local `ElsasaSis.Api` (auto-migrated `001`–`099` on `(localdb)\MSSQLLocalDB`) and Vite dev proxy, live end-to-end verification confirms every Acceptance Condition in [IS002- STAGE-1-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-ARCHITECTURE.md) §11:
   - **`admin` (`SCHOOL_ADMIN`)**: Verifies all 4 sidebar groups; adds & deletes a holiday in `SCR-OPR-CALENDAR` Tab 2; creates & updates a room in Tab 3; drafts & publishes an announcement in `SCR-OPR-ANNOUNCEMENTS`; registers a prospective candidate with guardian in `SCR-STD-ADMISSIONS`; creates a teacher, tags an `acd.subjects` capability, and assigns them to `7A` in `SCR-TCH-ASSIGNMENTS`.
   - **`principal` (`PRINCIPAL`)**: Logs in (`Principal123!`), verifies role-filtered sidebar, approves the registered candidate (`ACCEPTED`) with `decision_notes` in `SCR-STD-ADMISSIONS`, and views leadership workload oversight (read-only) in `SCR-TCH-ASSIGNMENTS`.
   - **`admin` (`SCHOOL_ADMIN`)**: Opens Enrollment modal for the accepted candidate in `SCR-STD-ADMISSIONS` (verifying pre-filled sequential NIS `20260001`), enrolls them atomically as official `ACTIVE` student, edits student/guardians in `SCR-STD-DIRECTORY`, and places them into study group `7A`.
   - **`teacher.budi` (`TEACHER` + `HOMEROOM_TEACHER`)**: Logs in (`Teacher123!`), verifies role-filtered sidebar, opens `SCR-STD-DIRECTORY` and confirms roster is strictly scoped to study group `7A` showing the enrolled student and emergency guardian phone numbers, while direct access to `7B` roster returns `403 Forbidden`.
   - Confirms zero tokens in `localStorage`/`sessionStorage`, RFC 7807 `422` `camelCase` field error feedback, and `409 Conflict` alert handling; cleanly shuts down temporary API & Vite servers.

Implementation Notes:
- **Frontend Quality Gates (All Passed with 0 Failures)**:
  - `pnpm typecheck` (`vue-tsc --noEmit`): Exited 0 with zero TypeScript errors across all views, components, and router guards.
  - `pnpm lint` (`eslint . --max-warnings 0`): Exited 0 with zero warnings.
  - `pnpm test:unit` (`vitest run`): 30 test files passed (100%), 164 unit/component tests passed (100%), 0 failures.
  - `pnpm build` (`vite build`): Production build succeeded in 11.37s with all chunks correctly generated and minified.
- **OpenAPI Drift Verification**:
  - Started `ElsasaSis.Api` on custom port 5055 with `ASPNETCORE_ENVIRONMENT=Development`.
  - Ran `pnpm exec openapi-typescript http://127.0.0.1:5055/swagger/v1/swagger.json -o src/shared/types/api/schema.d.ts --check`.
  - Result: Exited 0, confirming 100% schema alignment with zero drift between backend endpoints and generated frontend contracts.
- **Live Multi-Role HTTP End-to-End Verification**:
  - Executed automated full-lifecycle E2E verification test against live API:
    1. **`admin` (`Admin123!`)**: Authenticated (JWT token returned), retrieved active academic year 2026/2027, added holiday on 2026-12-19, deleted holiday (204 No Content), created room `Lab-E2E-5339`, updated capacity to 40 and renamed room, drafted announcement, published announcement (`PUBLISHED`), registered prospective candidate with father guardian, created faculty teacher member, tagged `Bahasa Indonesia` (`BIND`) subject capability, and assigned teacher to study group `7A`.
    2. **`principal` (`Principal123!`)**: Authenticated, approved registered candidate (`ACCEPTED`) with decision notes, queried leadership workload distribution oversight (`GET /api/tch/workload`).
    3. **`admin` (`Admin123!`)**: Fetched sequential NIS via `GET /api/std/candidates/{id}/next-nis` (received `20260001`), enrolled candidate atomically as official `ACTIVE` student (`POST /api/std/candidates/{id}/enroll`), and placed student into study group `7A` (`POST /api/std/placements`).
    4. **`teacher.budi` (`Teacher123!`)**: Authenticated, queried roster for assigned homeroom study group `7A` (`GET /api/std/study-groups/{id}/roster`) -> returned 200 OK with enrolled student and emergency guardian phone number (`081299998888`), queried roster for unassigned study group `7B` (`GET /api/std/study-groups/{7b-id}/roster`) -> returned 403 Forbidden, strictly verifying homeroom scoping policy (`UC-HRM-05`, `BR-STU-001`, `BR-STU-006`).
    5. **RFC 7807 & Conflict Handling**: Verified duplicate room returns 409 Conflict with ProblemDetails message, and invalid payload returns 422 Unprocessable Entity with validation errors.
  - Cleanly stopped temporary API server and verified port 5055 released.

Notes: Only frontend adjustments (if any integration defect is surfaced) occur inside this slice's repository boundary.

---

# 6. Change Log

- Initial `CR-STAGE-1` Stage 1 Implementation Plan created and approved (`v1.0`, 5 phases, 19 continuous slices `P1-S01`..`P5-S19`, `Execution Approval: APPROVED`) based on [IS002- STAGE-1-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-ARCHITECTURE.md) and [IS002- STAGE-1-DEV-FEASIBILITY-ASSESMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-FEASIBILITY-ASSESMENT.md).
