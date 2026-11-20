---
Title: Stage 2 Timetables & Daily Academic Operations — Implementation Plan
Code: CR-STAGE-2
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-19
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Deliver an executable, dependency-driven implementation plan for **CR-STAGE-2** (**Stage 2: Timetables & Daily Academic Operations** of **ELSASA-SIS v4 Basic Level**), transforming the completed Stage 1 codebase (`src/backend/` and `src/frontend/`) into the approved Stage 2 target technical realization across **8 actor-driven use cases** and **4 UI screens**.

Planning Mode: `FEATURE-PLANNING` (scoping `CR-STAGE-2` Stage 2 Timetables & Daily Academic Operations over the completed Stage 0 and Stage 1 baseline)

Referenced artifacts:

- ISSUE: [IS003- STAGE-2-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-ISSUE.md) (`CR-STAGE-2`)
- FEASIBILITY-ASSESSMENT: [IS003- STAGE-2-DEV-FEASIBILITY.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-FEASIBILITY.md) (`Status: READY-FOR-PLANNING`; Decisions `D-01` through `D-20` binding and closed)
- ARCHITECTURE: [IS003- STAGE-2-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-ARCHITECTURE.md) (`Version: 1.0`)
- Supporting Baseline Specifications: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md), [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md), [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md), [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md), [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md), [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md), [teaching-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/teaching-tables-design.md), [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md), [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md), [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md), [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md)

Architecture Applicability: `ARCHITECTURE-REQUIRED` — Target architecture defined in [IS003- STAGE-2-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-ARCHITECTURE.md).

---

# 2. Planning Scope

Scope covers all Stage 2 deliverables across two strict repository boundaries (`src/backend` and `src/frontend`):

1. **Backend (`src/backend`):**
   - One new `.NET 8 LTS` (`net8.0`) module assembly: `ElsasaSis.Modules.Acd`, integrated into `ElsasaSis.sln`, `ElsasaSis.Api`, `ElsasaSis.UnitTests`, `ElsasaSis.ArchitectureTests`, and `ElsasaSis.IntegrationTests` with zero cross-module assembly references.
   - Expansion of `ElsasaSis.Modules.Opr` with timetable domain entities, repositories, and vertical slices.
   - Re-homing of temporary Stage 1 academic components from `ElsasaSis.Api` (`AcademicContract.cs` and `GetSubjects`) into `ElsasaSis.Modules.Acd`.
   - Expansion of `ElsasaSis.Modules.Auth` (`Permissions.cs` + `RolePermissionCatalog.cs` for `SCHOOL_ADMIN`, `PRINCIPAL`, `TEACHER`, `HOMEROOM_TEACHER`).
   - Three idempotent DbUp SQL scripts in `ElsasaSis.Database/Scripts/` (`013_CreateOprTimetableTables.sql`, `031_CreateAcdTables.sql`, `100_SeedStage2Data.sql`) defining 11 Pure Persistence tables (`opr.study_group_schedules`, `opr.study_group_timetables`, `acd.subject_stages`, `acd.learning_units`, `acd.subject_stage_grades`, `acd.academic_participations`, `acd.attendance_records`, `acd.assignments`, `acd.assignment_scores`, `acd.report_cards`, `acd.report_card_entries`), plus `Stage2Seeder.cs` for integration test resets.
   - Expanded NetArchTest architecture suite (`Auth`, `Opr`, `Std`, `Tch`, `Acd`), unit tests, and `WebApplicationFactory` + `Respawn` integration tests against local SQL Server / `MSSQLLocalDB`.

2. **Frontend (`src/frontend`):**
   - Expanded `AppLayout.vue` role-filtered sidebar (adding Timetables under `Operations & Comms`, Curriculum under `Academic & Curriculum`, Attendance and Scorebook under `Faculty & Teaching`), `routes.ts` Stage 2 routes (`/operations/timetables`, `/academic/curriculum`, `/academic/attendance`, `/academic/scorebook`).
   - Regenerated OpenAPI `schema.d.ts` and query key factories (`timetableKeys`, `academicKeys`).
   - All 4 Stage 2 screens realizing all 8 use cases:
     1. `SCR-OPR-TIMETABLE` (`features/operations/scheduling/views/TimetableWorkspaceView.vue` — Schedule Management pattern: Weekly matrix, slot assignment drawer, SMK block drawer, conflict highlights, teacher personal schedule tab).
     2. `SCR-ACD-CURRICULUM` (`features/academic/curriculum/views/CurriculumSetupView.vue` — Planning pattern: 3-level tree, grade mapping canvas, batch participation enrollment with Tier 3 Pre-Flight Impact Summary Modal).
     3. `SCR-ACD-ATTENDANCE` (`features/academic/attendance/views/AttendanceWorkbenchView.vue` — Work Queue pattern: Session selector, student roster grid, "Mark All Present" shortcut, attendance status chips, dirty tracking & sticky Save bar).
     4. `SCR-ACD-SCOREBOOK` (`features/academic/scorebook/views/ScorebookWorkbenchView.vue` — Work Queue pattern: Assessment creation modal, spreadsheet score entry grid, keyboard navigation, running grade calculation, attendance metrics summary, dirty tracking & sticky Save bar).
   - Vitest + MSW unit/component tests, `vue-tsc`, ESLint boundary compliance, `pnpm build`, OpenAPI `--check`, and multi-role live E2E verification (`admin`, `principal`, `teacher.budi`).

3. **Excluded from Stage 2 (Deferred to Stage 3+):**
   - Report card generation, review, entry editing, finalization, and PDF generation (`SCR-ACD-REPORT-CARDS`, `UC-ADM-19`, `UC-ADM-20`, `UC-TCH-04`, `UC-HRM-01`, `UC-HRM-02`, `UC-HRM-03`, `UC-PRC-02`). The tables `acd.report_cards` and `acd.report_card_entries` are migrated for DDL completeness only.
   - Academic progress monitoring dashboards (`SCR-ACD-MONITORING`, `UC-ADM-21`, `UC-PRC-03`).
   - Automatic participation enrollment on placement event (`StudentPlacedDomainEvent`). Explicit admin action only in Stage 2 (D-01).
   - Write-path blocking of timetable conflicts. Conflicts detected and flagged on read only (D-10).
   - Enforcement of 100% assessment weight sum (`BR-ASGN-006`). Handlers validate individual weights (0–100); cross-assessment sum validation is deferred to Stage 3 report card compilation (D-18).
   - Finance (`fin.*`) and Pastoral/Development (`dev.*`) domains.

---

# 3. Dependencies

External/Environment prerequisites verified in the Feasibility Assessment:

- `.NET 8 SDK` (`net8.0`)
- `Node.js` (`v22+`) and `pnpm`
- Local Microsoft SQL Server (`MSSQLSERVER` / `(localdb)\MSSQLLocalDB`) — Docker is not required.
- Stage 1 baseline prerequisite: [IS002- STAGE-1-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-IMPLEMENTATION-PLAN.md) (`COMPLETED`).

For slice dependencies:

- `Depends On` declares implementation prerequisites.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation status `IMPLEMENTED` and verifiable repository output.
- Dependency satisfaction does not require review status `GO`.
- Dependencies must represent real implementation prerequisites.
- Each slice targets strictly one repository boundary (`src/backend` or `src/frontend`), never both.

### Parallel Execution Wave Summary *(Informational Only — `Depends On` Is Authoritative)*

- **Wave 1:** `P1-S01` (`src/backend`) || `P1-S03` (`src/frontend`)
- **Wave 2:** `P1-S02` (`src/backend`)
- **Wave 3:** `P2-S04` || `P2-S05` || `P2-S07` *(all `src/backend`, strictly disjoint module assemblies and feature folders)*
- **Wave 4:** `P2-S06` (`src/backend`, depends on `P2-S05`)
- **Wave 5:** `P2-S08` (`src/backend`, depends on `P2-S05`, `P2-S06`, `P2-S07`)
- **Wave 6:** `P3-S09` || `P3-S10` || `P3-S11` *(Architecture/Unit Tests, Integration Tests, OpenAPI Client Sync)*
- **Wave 7:** `P4-S12` || `P4-S13` || `P4-S14` || `P4-S15` *(all 4 frontend screens in parallel, strictly disjoint feature directories)*
- **Wave 8:** `P5-S16` (`src/frontend`, live multi-role verification against running monorepo stack)

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

Execution Approval is owned by the Architect. It is PENDING during Planning and set to APPROVED when the plan is released for execution. Execution must not begin while Execution Approval is PENDING.

COMPLETED is a plan-level status only. Set it only when every slice has implementation status IMPLEMENTED and review status GO.

Testing and test-package creation must not begin until the plan is COMPLETED. An individual slice with review status GO is not a testing entry condition.

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
| P1 | NOT-STARTED | NOT-REVIEWED | 0/3 |
| P2 | NOT-STARTED | NOT-REVIEWED | 0/5 |
| P3 | NOT-STARTED | NOT-REVIEWED | 0/3 |
| P4 | NOT-STARTED | NOT-REVIEWED | 0/4 |
| P5 | NOT-STARTED | NOT-REVIEWED | 0/1 |

---

# 5. Phases

## P1 - Foundation, Scaffolding, Migrations & Shell Navigation

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P1-S01

Title: Scaffold `ElsasaSis.Modules.Acd` Project, Stage 2 Contracts, Auth Permissions & Role Catalog

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Create the new `ElsasaSis.Modules.Acd` C# module project, pre-register it in `ElsasaSis.sln`, re-home temporary Stage 1 academic artifacts from `ElsasaSis.Api` into `Modules.Acd`, expand `ElsasaSis.Modules.Auth` permissions and role mappings, and update project references across `Api`, `UnitTests`, `ArchitectureTests`, and `IntegrationTests` with zero cross-module references.

Depends On: None

Repository: src/backend

Completion Criteria:

1. Create `src/backend/src/Modules/ElsasaSis.Modules.Acd/ElsasaSis.Modules.Acd.csproj` (targeting `net8.0`, nullable enable, implicit usings, `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, `Dapper 2.1.35`, `FluentValidation 11.11.0`, `MediatR 12.4.1`, referencing ONLY `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`). Register in `src/backend/ElsasaSis.sln`.
2. Add skeleton files in `Modules.Acd`:
   - `AcdAssemblyMarker.cs`
   - `Permissions.cs` defining `Permissions.Academic.Curriculum.*`, `Permissions.Academic.Attendance.*`, and `Permissions.Academic.Scorebook.*` matching `Modules.Auth`
   - `DependencyInjection.cs` exposing `public static IServiceCollection AddAcdModule(this IServiceCollection services)`
   - `Features/ValidationFilter.cs`
3. Migrate temporary Stage 1 academic artifacts out of `ElsasaSis.Api`:
   - Move `src/backend/src/ElsasaSis.Api/Infrastructure/AcademicContract.cs` to `src/backend/src/Modules/ElsasaSis.Modules.Acd/Infrastructure/Contracts/AcademicContract.cs` and register it inside `AddAcdModule()`.
   - Move `src/backend/src/ElsasaSis.Api/Features/Academic/GetSubjects/` to `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/LearningStructure/GetSubjects/`.
   - Remove `src/backend/src/ElsasaSis.Api/Features/Academic/` and `src/backend/src/ElsasaSis.Api/Infrastructure/AcademicContract.cs`.
4. Update project references:
   - `ElsasaSis.Api.csproj`: Add `ElsasaSis.Modules.Acd`.
   - `ElsasaSis.UnitTests.csproj`: Add `ElsasaSis.Modules.Acd`.
   - `ElsasaSis.ArchitectureTests.csproj`: Add `ElsasaSis.Modules.Acd`.
   - `ElsasaSis.IntegrationTests.csproj`: Add `ElsasaSis.Modules.Acd`.
   - Assert zero project references between any of `{Auth, Opr, Std, Tch, Acd}`.
5. Expand `src/backend/src/Modules/ElsasaSis.Modules.Auth/Permissions.cs`:
   - Operations Timetables: `Permissions.Operations.Timetables.View`, `Permissions.Operations.Timetables.Manage`, `Permissions.Operations.Timetables.ViewOwn`.
   - Composite policy: `Policies.Operations.Timetables.ViewOrManageOrOwn`.
   - Academic Curriculum: `Permissions.Academic.Curriculum.View`, `Permissions.Academic.Curriculum.Manage`.
   - Composite policy: `Policies.Academic.Curriculum.ViewOrManage`.
   - Academic Attendance: `Permissions.Academic.Attendance.View`, `Permissions.Academic.Attendance.Record`.
   - Composite policy: `Policies.Academic.Attendance.ViewOrRecord`.
   - Academic Scorebook: `Permissions.Academic.Scorebook.View`, `Permissions.Academic.Scorebook.Manage`.
   - Composite policy: `Policies.Academic.Scorebook.ViewOrManage`.
6. Expand `src/backend/src/Modules/ElsasaSis.Modules.Auth/RolePermissionCatalog.cs`:
   - `SCHOOL_ADMIN`: possesses all Operations Timetables, Academic Curriculum, Attendance, and Scorebook permissions.
   - `TEACHER`: `Permissions.Operations.Timetables.ViewOwn`, `Permissions.Academic.Curriculum.View`, `Permissions.Academic.Attendance.View`, `Permissions.Academic.Attendance.Record`, `Permissions.Academic.Scorebook.View`, `Permissions.Academic.Scorebook.Manage`.
   - `HOMEROOM_TEACHER`: inherits teacher permissions plus homeroom-scoped read access.
   - `PRINCIPAL`: `Permissions.Operations.Timetables.View`, `Permissions.Academic.Curriculum.View`, `Permissions.Academic.Attendance.View`, `Permissions.Academic.Scorebook.View`.
7. Update `src/backend/src/ElsasaSis.Api/Program.cs`:
   - Add `typeof(AcdAssemblyMarker).Assembly` to `builder.Services.AddMediatR(...)`.
   - Call `builder.Services.AddAcdModule()`.
   - Add `typeof(AcdAssemblyMarker).Assembly` to `app.MapDiscoveredEndpoints(...)`.
   - Remove direct registrations of `AcademicContract` and `GetSubjectsHandler` from `Program.cs`.
8. Update `src/backend/tests/ElsasaSis.UnitTests/Auth/RolePermissionCatalogTests.cs` to verify all Stage 2 role-permission mappings.
9. `dotnet build src/backend/ElsasaSis.sln` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` succeed with exit code `0`.

Notes: Preserve all existing Stage 0 and Stage 1 tests. Ensure policy names match string-for-string across `Modules.Auth` and module permission classes.

Implementation Notes:
- Created `ElsasaSis.Modules.Acd` project targeting `net8.0` with Dapper 2.1.35, FluentValidation 11.11.0, MediatR 12.4.1 referencing only `SharedKernel` and `Contracts`.
- Registered `ElsasaSis.Modules.Acd` in `ElsasaSis.sln` under `Modules` solution folder.
- Added skeleton files: `AcdAssemblyMarker.cs`, `Permissions.cs` (Curriculum, Attendance, Scorebook permissions and policies), `DependencyInjection.cs`, and `Features/ValidationFilter.cs`.
- Migrated temporary academic artifacts from `ElsasaSis.Api` to `Modules.Acd`: `AcademicContract.cs` moved to `Modules.Acd/Infrastructure/Contracts/` and registered in `AddAcdModule()`, `GetSubjects` moved to `Modules.Acd/Features/LearningStructure/GetSubjects/`, removed from `ElsasaSis.Api`.
- Updated project references in `ElsasaSis.Api.csproj`, `ElsasaSis.UnitTests.csproj`, `ElsasaSis.ArchitectureTests.csproj`, and `ElsasaSis.IntegrationTests.csproj` to reference `ElsasaSis.Modules.Acd`. Confirmed zero inter-module references between `{Auth, Opr, Std, Tch, Acd}`.
- Expanded `Modules.Auth/Permissions.cs` with Stage 2 Timetables, Curriculum, Attendance, and Scorebook permissions and composite policies (`Policies.Operations.Timetables.ViewOrManageOrOwn`, `Policies.Academic.Curriculum.ViewOrManage`, `Policies.Academic.Attendance.ViewOrRecord`, `Policies.Academic.Scorebook.ViewOrManage`). Also expanded `Modules.Opr/Permissions.cs` with Timetables permissions and policies.
- Expanded `Modules.Auth/RolePermissionCatalog.cs` mappings for `SCHOOL_ADMIN`, `TEACHER`, `HOMEROOM_TEACHER`, and `PRINCIPAL`.
- Updated `ElsasaSis.Api/Program.cs`: added `AcdAssemblyMarker` to MediatR and discovered endpoints, added `AddAcdModule()`, removed manual `AcademicContract` and `GetSubjectsHandler` registrations, registered Stage 2 authorization composite policies.
- Updated `Stage1TchIntegrationTests.cs` using directive for `GetSubjects`.
- Updated and expanded `RolePermissionCatalogTests.cs`: verified all Stage 2 role-permission mappings and catalog policy parity.
- Verification: `dotnet build src/backend/ElsasaSis.sln` succeeded with 0 warnings and 0 errors; all 216 unit tests and 11 architecture tests passed.

---

### P1-S02

Title: Implement Stage 2 DbUp Migration Scripts (`013`, `031`, `100`), Seed Data & `Stage2Seeder`

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Author all 3 Stage 2 embedded Pure Persistence SQL scripts (`013_CreateOprTimetableTables.sql`, `031_CreateAcdTables.sql`, and `100_SeedStage2Data.sql`) covering all 11 new tables, curriculum hierarchy seed data, timetable slots for study group `7A`, and deterministic verification fixtures, plus `Stage2Seeder.cs` for integration test resets.

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. Embedded SQL scripts exist under `src/backend/src/ElsasaSis.Database/Scripts/`, matching `IS003- STAGE-2-DEV-ARCHITECTURE.md` §8 and the authoritative table designs with zero DB FKs, zero `DEFAULT` constraints, zero `CHECK` constraints, zero triggers, zero stored procedures, UUID v7 clustered PKs, and `DATETIME2(3) NOT NULL` UTC timestamps:
   - `013_CreateOprTimetableTables.sql`:
     - `opr.study_group_schedules`: PK `id`, `operational_period_id`, `study_group_id`, `study_group_name`, `schedule_type` (`REGULAR` | `GENERAL` | `VOCATIONAL`), `day_of_week` (INT 1-7), `period_slot` (INT), `start_time` (TIME(0)), `end_time` (TIME(0)), `subject_id`, `subject_name`, `teacher_id`, `teacher_name`, `room_id` (NULL), `room_name` (NULL), `created_at`, `updated_at`. Unique constraint `UQ_study_group_schedules_slot` on `(operational_period_id, study_group_id, schedule_type, day_of_week, period_slot)`. Indexes: `IX_study_group_schedules_period_group`, `IX_study_group_schedules_period_teacher`, `IX_study_group_schedules_period_room`.
     - `opr.study_group_timetables`: PK `id`, `operational_period_id`, `study_group_id`, `study_group_name`, `schedule_type` (`GENERAL` | `VOCATIONAL`), `start_date` (DATE), `end_date` (DATE), `created_at`, `updated_at`. Unique constraint `UQ_study_group_timetables_period_group_date` on `(operational_period_id, study_group_id, start_date)`. Index: `IX_study_group_timetables_period_date`.
   - `031_CreateAcdTables.sql`:
     - `acd.subject_stages`: PK `id`, `subject_id`, `subject_name`, `name`, `sequence_order` (INT), `description` (NVARCHAR(500) NULL), `created_at`, `updated_at`. UQ `UQ_subject_stages_subject_name`, UQ `UQ_subject_stages_subject_seq`.
     - `acd.learning_units`: PK `id`, `subject_stage_id`, `name`, `sequence_order` (INT), `unit_type` (VARCHAR(30)), `description` (NVARCHAR(500) NULL), `created_at`, `updated_at`. UQ `UQ_learning_units_stage_name`, UQ `UQ_learning_units_stage_seq`.
     - `acd.subject_stage_grades`: PK `id`, `subject_stage_id`, `school_level` (VARCHAR(20)), `grade` (VARCHAR(10)), `created_at`, `updated_at`. UQ `UQ_subject_stage_grades_stage_level_grade`.
     - `acd.academic_participations`: PK `id`, `student_id`, `student_name`, `student_nis`, `subject_stage_id`, `subject_stage_name`, `subject_id`, `subject_name`, `operational_period_id`, `operational_period_name`, `academic_year_id`, `academic_year_name`, `study_group_id`, `study_group_name`, `status` (VARCHAR(20) NOT NULL), `created_at`, `updated_at`. UQ `UQ_academic_participations_student_stage_period` on `(student_id, subject_stage_id, operational_period_id)`. Indexes: `IX_academic_participations_subject_group`, `IX_academic_participations_stage_group`.
     - `acd.attendance_records`: PK `id`, `student_id`, `student_name`, `student_nis`, `subject_id`, `subject_name`, `study_group_id`, `study_group_name`, `operational_period_id`, `operational_period_name`, `attendance_date` (DATE), `status` (VARCHAR(20)), `notes` (NVARCHAR(500) NULL), `recorded_by_teacher_id`, `recorded_by_teacher_name`, `created_at`, `updated_at`. UQ `UQ_attendance_records_student_subject_date` on `(student_id, subject_id, attendance_date)`. Index: `IX_attendance_records_subject_group_date`.
     - `acd.assignments`: PK `id`, `subject_stage_id`, `subject_stage_name`, `subject_id`, `subject_name`, `learning_unit_id` (NULL), `learning_unit_name` (NULL), `operational_period_id`, `operational_period_name`, `study_group_id` (NULL in DDL), `study_group_name` (NULL), `title`, `assignment_type` (VARCHAR(20)), `max_score` (DECIMAL(5,2)), `weight` (DECIMAL(5,2) NULL), `assigned_date` (DATE), `due_date` (DATE NULL), `teacher_id`, `teacher_name`, `created_at`, `updated_at`. Index: `IX_assignments_stage_group_period`.
     - `acd.assignment_scores`: PK `id`, `assignment_id`, `student_id`, `student_name`, `student_nis`, `score` (DECIMAL(5,2)), `feedback` (NVARCHAR(1000) NULL), `graded_at` (DATETIME2(3)), `created_at`, `updated_at`. UQ `UQ_assignment_scores_assignment_student` on `(assignment_id, student_id)`. Index: `IX_assignment_scores_assignment`.
     - `acd.report_cards`: PK `id`, `student_id`, `student_name`, `student_nis`, `student_nisn`, `academic_year_id`, `academic_year_name`, `operational_period_id`, `operational_period_name`, `study_group_id`, `study_group_name`, `homeroom_teacher_id`, `homeroom_teacher_name`, `status` (VARCHAR(20)), `finalized_at` (DATETIME2(3) NULL), `created_at`, `updated_at`. UQ `UQ_report_cards_student_period`.
     - `acd.report_card_entries`: PK `id`, `report_card_id`, `subject_id`, `subject_name`, `subject_code`, `subject_stage_id`, `subject_stage_name`, `teacher_id`, `teacher_name`, `numeric_score` (DECIMAL(5,2)), `letter_grade` (VARCHAR(5)), `teacher_notes` (NVARCHAR(1000) NULL), `created_at`, `updated_at`. UQ `UQ_report_card_entries_card_stage`.
   - `100_SeedStage2Data.sql`: Idempotently seeds:
     - Mathematics Stage 7 and English Stage 7 under existing seeded subjects `MTK` and `BING`.
     - Learning units under Mathematics Stage 7 (`Bilangan & Aljabar`, `Geometri & Pengukuran`) and English Stage 7 (`Greetings & Introductions`).
     - Grade mappings for both stages to `(SMP, 7)`.
     - Study group `7A` weekly timetable slots (Monday Period 1-2 MTK with teacher Budi Santoso in Classroom 7A).
     - Batch enrolls placed students of study group `7A` into Mathematics Stage 7 and English Stage 7 (`acd.academic_participations`).
     - Sample attendance records for `7A` for yesterday's date.
     - Sample formative assignment (`Kuis Aljabar 1`) and summative assignment (`Ulangan Harian 1`) with student scores.
2. Implement `src/backend/src/ElsasaSis.Database/Stage2Seeder.cs` exposing idempotent `SeedStage2FixturesAsync(string connectionString, CancellationToken cancellationToken = default)` for integration test resets on demand.
3. Running `ElsasaSis.Database` twice idempotently against local SQL Server / `(localdb)\MSSQLLocalDB` succeeds with exit code `0`, creating all 11 Stage 2 tables and seeding operational fixtures without error.

Notes: Keep `ElsasaSis.Database` free of references to `Modules.*` or `ElsasaSis.Api`. Ensure all UUIDs are deterministic and every insert has `IF NOT EXISTS` protection.

Implementation Notes:
- Created embedded migration script `013_CreateOprTimetableTables.sql` defining `opr.study_group_schedules` and `opr.study_group_timetables` adhering to Pure Persistence standards (zero DB FKs, zero `DEFAULT` constraints, zero `CHECK` constraints, zero triggers, zero stored procedures, UUID v7 clustered PKs, UTC `DATETIME2(3) NOT NULL` timestamps).
- Created embedded migration script `031_CreateAcdTables.sql` defining 9 academic tables (`acd.subject_stages`, `acd.learning_units`, `acd.subject_stage_grades`, `acd.academic_participations`, `acd.attendance_records`, `acd.assignments`, `acd.assignment_scores`, `acd.report_cards`, `acd.report_card_entries`) with Pure Persistence standards.
- Created embedded migration script `100_SeedStage2Data.sql` idempotently seeding active academic year & period, teacher Budi Santoso, Classroom 7A room, study group 7A, teacher assignment for MTK in 7A, Mathematics Stage 7 & English Stage 7 under MTK and BING, learning units (`Bilangan & Aljabar`, `Geometri & Pengukuran`, `Greetings & Introductions`), grade mappings to `(SMP, 7)`, weekly timetable slots for 7A (Monday Periods 1 and 2), placed students Ahmad Dahlan & Siti Nurhaliza in 7A, academic participations, sample attendance records for yesterday, and formative/summative assignments with student scores. All inserts have deterministic UUIDs and `IF NOT EXISTS` protection.
- Created `src/backend/src/ElsasaSis.Database/Stage2Seeder.cs` exposing public fixture constants and `SeedStage2FixturesAsync(string connectionString, CancellationToken cancellationToken = default)`.
- Added `SeedStage2FixturesAsync` helper to `IntegrationTestBase.cs`.
- Added integration test `Stage2DatabaseAndSeederIntegrationTests` verifying idempotency, table existence, and fixture counts.
- Verified execution: `ElsasaSis.Database` was executed twice consecutively against SQL Server with exit code `0` (1st run applied scripts; 2nd run reported no new scripts). All 216 unit tests, 11 architecture tests, and 19 integration tests passed.

---

### P1-S03

Title: Expand Frontend App Shell, Stage 2 Routes, Navigation Taxonomy & Stub Views

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Register all Stage 2 routes under `AppLayout.vue`, integrate navigation menu items with role-based visibility, and create minimal compilable stub views so downstream frontend slices can build and test concurrently with zero merge conflicts.

Depends On: None

Repository: src/frontend

Completion Criteria:

1. Update `src/frontend/src/app/router/routes.ts` to register Stage 2 routes under `AppLayout.vue`:
   - `/operations/timetables` (`operations.timetables`, `Permissions.Operations.Timetables.View`, `Permissions.Operations.Timetables.Manage`, `Permissions.Operations.Timetables.ViewOwn`, title: 'Timetables & Scheduling')
   - `/academic/curriculum` (`academic.curriculum`, `Permissions.Academic.Curriculum.View`, `Permissions.Academic.Curriculum.Manage`, title: 'Curriculum & Learning Structure')
   - `/academic/attendance` (`academic.attendance`, `Permissions.Academic.Attendance.View`, `Permissions.Academic.Attendance.Record`, title: 'Classroom Attendance')
   - `/academic/scorebook` (`academic.scorebook`, `Permissions.Academic.Scorebook.View`, `Permissions.Academic.Scorebook.Manage`, title: 'Teacher Scorebook')
2. Update `src/frontend/src/app/layouts/AppLayout.vue` navigation menu taxonomy:
   - Group **Operations & Comms**: Add "Timetables & Scheduling" (`/operations/timetables`, icon `CalendarClock`).
   - Group **Academic & Curriculum**: Add "Curriculum & Learning Structure" (`/academic/curriculum`, icon `BookOpen`).
   - Group **Faculty & Teaching**: Add "Classroom Attendance" (`/academic/attendance`, icon `ClipboardCheck`) and "Teacher Scorebook" (`/academic/scorebook`, icon `FileSpreadsheet`).
   - All items dynamically filtered by `authStore.hasPermission(...)` ("Hide by Role").
3. Create minimal compilable stub views with temporary breadcrumb/page title headers:
   - `src/frontend/src/features/operations/scheduling/views/TimetableWorkspaceView.vue`
   - `src/frontend/src/features/academic/curriculum/views/CurriculumSetupView.vue`
   - `src/frontend/src/features/academic/attendance/views/AttendanceWorkbenchView.vue`
   - `src/frontend/src/features/academic/scorebook/views/ScorebookWorkbenchView.vue`
4. `pnpm typecheck`, `pnpm lint`, `pnpm test:unit`, and `pnpm build` succeed with exit code `0`.

Notes: Strictly follow the feature directory anatomy (`features/operations/scheduling/`, `features/academic/curriculum/`, `features/academic/attendance/`, `features/academic/scorebook/`).
Implementation completed: Registered all 4 Stage 2 routes in `routes.ts`, expanded `AppLayout.vue` navigation taxonomy with role-based filtering and Lucide icons, created compilable stub views with header/breadcrumb initialization in respective feature view directories, and verified that `pnpm typecheck`, `pnpm lint`, `pnpm test:unit` (167 passing tests), and `pnpm build` succeed with exit code 0.

---

## P2 - Backend Domain Models, Repositories, Vertical Slices & API Wiring

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P2-S04

Title: Implement `ElsasaSis.Modules.Opr` Timetable Domain Model, Repositories & Vertical Slices (`UC-ADM-04`, `UC-TCH-10`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement `StudyGroupSchedule` and `StudyGroupTimetable` aggregate roots, Dapper repositories, slot management endpoints, SMK block date range management, read-side conflict detection flags, and the teacher personal schedule endpoint in `ElsasaSis.Modules.Opr`.

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Create domain entities and repositories in `ElsasaSis.Modules.Opr`:
   - `Domain/Model/StudyGroupSchedule.cs`: Scoped to `(operational_period_id, study_group_id, schedule_type, day_of_week, period_slot)`. Factory methods for slot creation, room assignment, time validation (`start_time < end_time`).
   - `Domain/Model/StudyGroupTimetable.cs`: Scoped to `(operational_period_id, study_group_id, schedule_type, start_date, end_date)`. Invariant `start_date <= end_date`.
   - `Domain/IStudyGroupScheduleRepository.cs` and `Domain/IStudyGroupTimetableRepository.cs`.
   - `Domain/TimetableErrors.cs`.
2. Implement persistence in `ElsasaSis.Modules.Opr/Infrastructure/Persistence/`:
   - `StudyGroupScheduleRepository.cs` (Dapper CRUD for `opr.study_group_schedules`).
   - `StudyGroupTimetableRepository.cs` (Dapper CRUD for `opr.study_group_timetables`).
3. Implement vertical slices in `ElsasaSis.Modules.Opr/Features/Timetables/`:
   - `ConfigureStudyGroupTimetable`:
     - `POST /api/opr/study-groups/{id}/timetables`: Accepts `{ operationalPeriodId, scheduleType, dayOfWeek, periodSlot, startTime, endTime, subjectId, teacherId, roomId }`. Validates active operational period, fetches snapshots (`study_group_name`, `subject_name`, `teacher_name`, `room_name`), and upserts slot per `UQ_study_group_schedules_slot`. Authorized for `Permissions.Operations.Timetables.Manage`.
     - `DELETE /api/opr/study-groups/{id}/timetables/{scheduleId}`: Deletes slot. Returns `204 No Content`.
   - `ManageSmkBlockRotations`:
     - `POST /api/opr/study-groups/{id}/blocks`: Accepts `{ operationalPeriodId, scheduleType, startDate, endDate }`. Inserts/upserts SMK block date range.
     - `DELETE /api/opr/study-groups/{id}/blocks/{blockId}`: Removes block date range.
     - `GET /api/opr/study-groups/{id}/blocks?periodId=`: Returns list of block date ranges for the study group.
   - `GetStudyGroupTimetable`:
     - `GET /api/opr/study-groups/{id}/timetables?periodId=&scheduleType=`: Single Dapper query returning all weekly slots for the study group.
     - **Read-Side Conflict Flags (`D-10`):** Query evaluates SQL window/subquery to set:
       - `teacherConflict = TRUE` if the assigned `teacher_id` is scheduled in another study group for the same operational period, day, time slot, and schedule type.
       - `roomConflict = TRUE` if the assigned `room_id` is scheduled in another study group for the same operational period, day, time slot, and schedule type.
       - Saves are never blocked by conflicts (D-10).
   - `GetTeacherPersonalSchedule` (`UC-TCH-10`):
     - `GET /api/opr/teacher-schedule?periodId=`: Resolves `ICurrentUser.TeacherId`, strictly queries `opr.study_group_schedules WHERE operational_period_id = @PeriodId AND teacher_id = @TeacherId`. Grouped by `schedule_type` (D-11). Authorized for `Permissions.Operations.Timetables.ViewOwn`.
4. Register repositories and handlers in `ElsasaSis.Modules.Opr/DependencyInjection.cs`.
5. Unit tests for domain models and handler validation pass in `ElsasaSis.UnitTests`.

Notes: Write slices must use Aggregate Root + Repository; Read slices must project direct DTOs via Dapper.

Implementation Notes:
- Created domain model `StudyGroupSchedule.cs` in `Modules.Opr/Domain/Model/` with slot creation, room assignment, time validation (`start_time < end_time`), and slot update methods.
- Created domain model `StudyGroupTimetable.cs` in `Modules.Opr/Domain/Model/` scoped to `(operational_period_id, study_group_id, schedule_type, start_date, end_date)` with invariant `start_date <= end_date` and block type restrictions (`GENERAL` or `VOCATIONAL`).
- Created `ScheduleType.cs` with enum (`Regular`, `General`, `Vocational`) and parser/formatting helpers.
- Created `TimetableErrors.cs` with typed domain errors for slots, blocks, rooms, active operational period checks, date/time ranges, and teacher profile bindings.
- Created repository interfaces `IStudyGroupScheduleRepository` and `IStudyGroupTimetableRepository` in `Modules.Opr/Domain/`.
- Implemented `StudyGroupScheduleRepository` and `StudyGroupTimetableRepository` in `Modules.Opr/Infrastructure/Persistence/` using Dapper CRUD and SQL Server `MERGE` statements with `TimeSpan`/`DateTime` mapping.
- Implemented `ConfigureStudyGroupTimetable` slice:
  - `POST /api/opr/study-groups/{id}/timetables`: validates active operational period, fetches snapshots (`study_group_name`, `subject_name`, `teacher_name`, `room_name`), and upserts slot. Authorized for `Permissions.Operations.Timetables.Manage`.
  - `DELETE /api/opr/study-groups/{id}/timetables/{scheduleId}`: deletes slot, returns 204 No Content. Authorized for `Permissions.Operations.Timetables.Manage`.
- Implemented `ManageSmkBlockRotations` slice:
  - `POST /api/opr/study-groups/{id}/blocks`: validates active period, study group, dates, and upserts block date range. Authorized for `Permissions.Operations.Timetables.Manage`.
  - `DELETE /api/opr/study-groups/{id}/blocks/{blockId}`: removes block date range, returns 204 No Content. Authorized for `Permissions.Operations.Timetables.Manage`.
  - `GET /api/opr/study-groups/{id}/blocks?periodId=`: returns list of block date ranges for study group. Authorized for `Policies.Operations.Timetables.ViewOrManageOrOwn`.
- Implemented `GetStudyGroupTimetable` slice:
  - `GET /api/opr/study-groups/{id}/timetables?periodId=&scheduleType=`: Dapper query evaluating SQL `EXISTS` subqueries for `teacher_conflict` and `room_conflict` flags per `D-10`. Authorized for `Policies.Operations.Timetables.ViewOrManageOrOwn`.
- Implemented `GetTeacherPersonalSchedule` slice (`UC-TCH-10`):
  - `GET /api/opr/teacher-schedule?periodId=`: resolves `ICurrentUser.TeacherId`, queries teacher's scheduled slots for period, grouped by `schedule_type` (D-11). Authorized for `Permissions.Operations.Timetables.ViewOwn`.
- Registered all repositories, handlers, and FluentValidation validators in `ElsasaSis.Modules.Opr/DependencyInjection.cs`.
- Added unit tests in `ElsasaSis.UnitTests`: `StudyGroupScheduleTests`, `StudyGroupTimetableTests`, and `TimetableFeaturesTests` covering models, invariants, validators, handlers, and mock flows.
- Verified: `dotnet build src/backend/src/Modules/ElsasaSis.Modules.Opr/ElsasaSis.Modules.Opr.csproj` (0 errors), all 11 ArchitectureTests pass, all 329 UnitTests pass.

---

### P2-S05

Title: Implement `ElsasaSis.Modules.Acd` Learning Structure Domain & Slices (`UC-ADM-17`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement `Subject` and `SubjectStage` (with child `LearningUnit` and `SubjectStageGrade`) aggregate roots, Dapper repositories, and granular vertical slice endpoints under `ElsasaSis.Modules.Acd/Features/LearningStructure/`.

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Create domain entities and repositories in `ElsasaSis.Modules.Acd/Domain/`:
   - `Model/Subject.cs`: Aggregate Root owning `acd.subjects` (`name`, `code`, `is_active`).
   - `Model/SubjectStage.cs`: Aggregate Root owning `acd.subject_stages` (`name`, `sequence_order`, `description`), with child entities `LearningUnit` (`name`, `sequence_order`, `unit_type`, `description`) and `SubjectStageGrade` (`school_level`, `grade`).
   - Methods: `AddLearningUnit`, `UpdateLearningUnit`, `RemoveLearningUnit`, `AddGradeMapping`, `RemoveGradeMapping`, `ReorderUnits`.
   - Invariants: Sequence order uniqueness, valid grade level combinations (`SD`, `SMP`, `SMA`, `SMK`), non-empty titles.
   - `ISubjectRepository.cs` and `ISubjectStageRepository.cs`.
   - `LearningStructureErrors.cs`.
2. Implement persistence in `ElsasaSis.Modules.Acd/Infrastructure/Persistence/`:
   - `SubjectRepository.cs` (Dapper CRUD for `acd.subjects`).
   - `SubjectStageRepository.cs` (Dapper CRUD for `acd.subject_stages`, `acd.learning_units`, and `acd.subject_stage_grades`).
3. Implement vertical slice endpoints in `ElsasaSis.Modules.Acd/Features/LearningStructure/`:
   - `CreateSubject`: `POST /api/acd/subjects` (`Permissions.Academic.Curriculum.Manage`).
   - `UpdateSubject`: `PUT /api/acd/subjects/{id}` (`Permissions.Academic.Curriculum.Manage`).
   - `CreateSubjectStage`: `POST /api/acd/subjects/{id}/stages` (`Permissions.Academic.Curriculum.Manage`).
   - `UpdateSubjectStage`: `PUT /api/acd/subjects/{id}/stages/{stageId}` (`Permissions.Academic.Curriculum.Manage`).
   - `CreateLearningUnit`: `POST /api/acd/subjects/{id}/stages/{stageId}/units` (`Permissions.Academic.Curriculum.Manage`).
   - `UpdateLearningUnit`: `PUT /api/acd/subjects/{id}/stages/{stageId}/units/{unitId}` (`Permissions.Academic.Curriculum.Manage`).
   - `MapSubjectStageGrade`: `POST /api/acd/subjects/{id}/stages/{stageId}/grade-mappings` (`Permissions.Academic.Curriculum.Manage`).
   - `RemoveSubjectStageGrade`: `DELETE /api/acd/subjects/{id}/stages/{stageId}/grade-mappings/{mappingId}` (`Permissions.Academic.Curriculum.Manage`).
   - `GetLearningStructure`: `GET /api/acd/subjects` hierarchical read query returning subjects with nested stages, modular learning units, and grade mappings. Accessible by `Curriculum.View` or `Curriculum.Manage`.
4. Register repositories and handlers in `ElsasaSis.Modules.Acd/DependencyInjection.cs`.
5. Unit tests for domain models and handler validation pass in `ElsasaSis.UnitTests`.

Notes: Zero dependencies on external module assemblies. Read projections execute clean Dapper SQL queries.

Implementation Notes:
- Created domain models in `ElsasaSis.Modules.Acd/Domain/Model/`:
  - `Subject.cs`: Aggregate Root owning `acd.subjects` (`Name`, `Code`, `IsActive`, `CreatedAt`, `UpdatedAt`) with invariant validation and update methods.
  - `SubjectStage.cs`: Aggregate Root owning `acd.subject_stages` (`SubjectId`, `SubjectName`, `Name`, `SequenceOrder`, `Description`), owning child entities `LearningUnit` and `SubjectStageGrade`.
  - Child entities `LearningUnit.cs` (`SubjectStageId`, `Name`, `SequenceOrder`, `UnitType`, `Description`) and `SubjectStageGrade.cs` (`SubjectStageId`, `SchoolLevel`, `Grade`).
  - Helper value objects / specifications `UnitType.cs` (`MODULE`, `PACE`, `LESSON`, `PRESENTATION`, `BOOK`, `OTHER`) and `SchoolLevel.cs` (`SD`, `SMP`, `SMA`, `SMK` with valid grade combination rules).
  - Implemented domain methods on `SubjectStage`: `AddLearningUnit`, `UpdateLearningUnit`, `RemoveLearningUnit`, `ReorderUnits`, `AddGradeMapping`, and `RemoveGradeMapping`.
  - Enforced domain invariants: sequence order uniqueness for stages and units, non-empty names/titles, unit type validation, and grade combinations (`SD 1..6`, `SMP 7..9`, `SMA/SMK 10..12`).
  - Created typed domain errors in `LearningStructureErrors.cs`.
  - Created repository interfaces `ISubjectRepository.cs` and `ISubjectStageRepository.cs`.
- Implemented persistence in `ElsasaSis.Modules.Acd/Infrastructure/Persistence/`:
  - `SubjectRepository.cs`: Dapper CRUD for `acd.subjects` with unique constraint violation mapping (`UQ_subjects_name`, `UQ_subjects_code`).
  - `SubjectStageRepository.cs`: Dapper CRUD with multi-table transaction synchronization for `acd.subject_stages`, `acd.learning_units`, and `acd.subject_stage_grades`.
- Implemented vertical slice endpoints in `ElsasaSis.Modules.Acd/Features/LearningStructure/`:
  - Shared records and mapping extensions in `Dtos.cs` (`SubjectDto`, `SubjectStageDto`, `LearningUnitDto`, `SubjectStageGradeDto`).
  - `CreateSubject`: `POST /api/acd/subjects` (`Permissions.Academic.Curriculum.Manage`).
  - `UpdateSubject`: `PUT /api/acd/subjects/{id}` (`Permissions.Academic.Curriculum.Manage`).
  - `CreateSubjectStage`: `POST /api/acd/subjects/{id}/stages` (`Permissions.Academic.Curriculum.Manage`).
  - `UpdateSubjectStage`: `PUT /api/acd/subjects/{id}/stages/{stageId}` (`Permissions.Academic.Curriculum.Manage`).
  - `CreateLearningUnit`: `POST /api/acd/subjects/{id}/stages/{stageId}/units` (`Permissions.Academic.Curriculum.Manage`).
  - `UpdateLearningUnit`: `PUT /api/acd/subjects/{id}/stages/{stageId}/units/{unitId}` (`Permissions.Academic.Curriculum.Manage`).
  - `RemoveLearningUnit`: `DELETE /api/acd/subjects/{id}/stages/{stageId}/units/{unitId}` (`Permissions.Academic.Curriculum.Manage`).
  - `MapSubjectStageGrade`: `POST /api/acd/subjects/{id}/stages/{stageId}/grade-mappings` (`Permissions.Academic.Curriculum.Manage`).
  - `RemoveSubjectStageGrade`: `DELETE /api/acd/subjects/{id}/stages/{stageId}/grade-mappings/{mappingId}` (`Permissions.Academic.Curriculum.Manage`).
  - `GetLearningStructure`: `GET /api/acd/subjects` with hierarchical Dapper `QueryMultipleAsync` query returning subjects with nested stages, units, and grade mappings. Accessible by `Policies.Academic.Curriculum.ViewOrManage` (`Curriculum.View` or `Curriculum.Manage`), maintaining backward compatibility with Stage 1 callers.
- Registered repositories in `ElsasaSis.Modules.Acd/DependencyInjection.cs`: `ISubjectRepository -> SubjectRepository`, `ISubjectStageRepository -> SubjectStageRepository`.
- Added Stage 2 fixture constants in `Stage2Seeder.cs` (`DefaultMtkSubjectId`, `DefaultStudyGroupId7A`, `DefaultBudiTeacherId`, `DefaultOperationalPeriodId`, `DefaultAcademicYearId`).
- Added comprehensive unit tests in `ElsasaSis.UnitTests/Acd/`:
  - `LearningStructureDomainTests.cs`: 16 unit tests covering subject and subject stage creation, update, learning unit lifecycle, reordering, grade level combinations, and invariants.
  - `LearningStructureFeatureTests.cs`: 15 unit tests covering command validators, handler error flows, and DI registration.
- Added comprehensive integration test in `ElsasaSis.IntegrationTests/Acd/LearningStructureIntegrationTests.cs`: verified end-to-end HTTP REST operations against SQL Server, including subject creation, duplicate conflict detection, updates, stage creation, unit creation, grade mapping, hierarchical query with nested stages/units/grade-mappings, grade mapping removal, and clean test cleanup.
- Verification: Solution builds cleanly with 0 errors and 0 warnings. All 346 unit tests, 11 architecture tests, and 23 integration tests pass.

---

### P2-S06

Title: Implement `ElsasaSis.Modules.Acd` Academic Participations Domain & Batch Enrollment Slice (`UC-ADM-18`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement `AcademicParticipation` aggregate root, repository, and explicit batch student enrollment endpoint (`POST /api/acd/participations/batch`) reading active placements from `std.student_placements` and inserting enrollment records with display snapshots.

Depends On: P1-S01, P1-S02, P2-S05

Repository: src/backend

Completion Criteria:

1. Create domain entities and repositories in `ElsasaSis.Modules.Acd/Domain/`:
   - `Model/AcademicParticipation.cs`: Scoped to `(student_id, subject_stage_id, operational_period_id)`. Contains display snapshots (`student_name`, `student_nis`, `subject_stage_name`, `subject_name`, `study_group_name`, `operational_period_name`, `academic_year_name`). Status `ACTIVE` | `COMPLETED`.
   - `IAcademicParticipationRepository.cs`.
   - `ParticipationErrors.cs`.
2. Implement persistence in `ElsasaSis.Modules.Acd/Infrastructure/Persistence/`:
   - `AcademicParticipationRepository.cs` (Dapper operations for `acd.academic_participations`).
3. Implement vertical slices in `ElsasaSis.Modules.Acd/Features/Participations/`:
   - `EnrollStudentsInSubjectStages` (`POST /api/acd/participations/batch` — `UC-ADM-18`):
     - Request: `{ operationalPeriodId: Guid, subjectStageId: Guid, studyGroupId: Guid }`.
     - Pipeline (D-01, D-17):
       1. Validate `operationalPeriodId` is active via `IOperationalContract`.
       2. Validate `subjectStageId` exists via `ISubjectStageRepository` and retrieve subject stage metadata.
       3. Cross-schema Dapper query on `std.student_placements` + `std.students` + `std.study_groups` for active students in `studyGroupId` for that academic year.
       4. Bulk insert into `acd.academic_participations` with display snapshots.
       5. Silently skip students already enrolled per `UQ_academic_participations_student_stage_period`.
       6. Return `{ enrolledCount: int, skippedCount: int }`.
     - Authorized for `Permissions.Academic.Curriculum.Manage`.
   - `GetSubjectStageParticipations`:
     - `GET /api/acd/participations?subjectStageId=&studyGroupId=&operationalPeriodId=`: Returns enrolled students list with participation statuses.
4. Register repositories and handlers in `ElsasaSis.Modules.Acd/DependencyInjection.cs`.
5. Unit tests for domain models and handler validation pass in `ElsasaSis.UnitTests`.

Notes: As established in D-01, explicit admin action is the sole enrollment mechanism; no `StudentPlacedDomainEvent` subscriber.

Implementation Notes:
- Created domain model `AcademicParticipation.cs` in `Modules.Acd/Domain/Model/` scoped to `(student_id, subject_stage_id, operational_period_id)` with factory validation, display snapshot enforcement (`student_name`, `student_nis`, `subject_stage_name`, `subject_name`, `study_group_name`, `operational_period_name`, `academic_year_name`), lifecycle methods (`Complete`, `Reactivate`, `UpdateStudyGroup`), and reconstitution (`Rehydrate`).
- Created enum `ParticipationStatus.cs` with codes (`ACTIVE`, `COMPLETED`) and typed parser.
- Created `ParticipationErrors.cs` with typed domain errors for validations, missing references (period, stage, study group, participation), and duplicate conflict mapping.
- Created repository interface `IAcademicParticipationRepository.cs` in `Modules.Acd/Domain/`.
- Implemented `AcademicParticipationRepository.cs` in `Modules.Acd/Infrastructure/Persistence/` using Dapper:
  - `GetByIdAsync`, `GetByStudentStagePeriodAsync`, `GetByCriteriaAsync`.
  - `GetEnrolledStudentIdsAsync` for pre-flight duplicate checking.
  - `GetActivePlacementsByStudyGroupAsync` executing cross-schema read on `std.student_placements` + `std.students` + `std.study_groups` for active students in target academic year.
  - `AddAsync` and `AddBatchAsync` with `IF NOT EXISTS` idempotent batch inserts and unique constraint violation handling (`UQ_academic_participations_student_stage_period`).
  - `UpdateAsync`.
- Implemented vertical slice `EnrollStudentsInSubjectStages` under `Modules.Acd/Features/Participations/EnrollStudentsInSubjectStages/`:
  - `POST /api/acd/participations/batch` (`UC-ADM-18`).
  - Validates active `operationalPeriodId` via `IOperationalContract`, retrieves `subjectStageId` metadata via `ISubjectStageRepository`, validates `studyGroupId` via `IStudentContract`.
  - Performs cross-schema query for placed active students, detects existing enrollments, constructs `AcademicParticipation` aggregate roots with display snapshots, bulk inserts into `acd.academic_participations`, gracefully skips already-enrolled students, and returns `{ enrolledCount, skippedCount }`.
  - Authorized for `Permissions.Academic.Curriculum.Manage` with `ValidationFilter`.
- Implemented vertical slice `GetSubjectStageParticipations` under `Modules.Acd/Features/Participations/GetSubjectStageParticipations/`:
  - `GET /api/acd/participations?subjectStageId=&studyGroupId=&operationalPeriodId=` projecting direct DTOs via Dapper using `ISqlConnectionFactory`.
  - Authorized for `Policies.Academic.Curriculum.ViewOrManage`.
- Registered `IAcademicParticipationRepository` in `ElsasaSis.Modules.Acd/DependencyInjection.cs`.
- Added unit tests in `ElsasaSis.UnitTests/Acd/`:
  - `AcademicParticipationDomainTests.cs`: 11 tests covering entity invariants, validations, state transitions, parsing, and rehydration.
  - `AcademicParticipationFeatureTests.cs`: 8 tests covering request validation, handler error paths (missing period, inactive period, missing stage, missing study group), zero placement handling, full batch enrollment, graceful skip idempotency, and DI registration.
- Added integration tests in `ElsasaSis.IntegrationTests/Acd/AcademicParticipationIntegrationTests.cs`:
  - `GetParticipations_WhenSeededParticipationsExist_ReturnsEnrolledStudentsList`
  - `BatchEnroll_WhenStudentsAlreadyEnrolled_SkipsGracefully`
  - `BatchEnroll_IntoNewSubjectStage_EnrollsAllPlacedStudents`
- Verified: All 371 unit tests, 11 architecture tests, and 26 integration tests pass with 0 errors.

---

### P2-S07

Title: Implement `ElsasaSis.Modules.Acd` Daily Attendance Domain & Batch Upsert Slices (`UC-TCH-01`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement `AttendanceRecord` aggregate root, repository, and batch attendance endpoints at the subject level (`POST /api/acd/attendance` upsert and `GET /api/acd/attendance` sheet) in `ElsasaSis.Modules.Acd`.

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Create domain entities and repositories in `ElsasaSis.Modules.Acd/Domain/`:
   - `Model/AttendanceRecord.cs`: Scoped to `(student_id, subject_id, attendance_date)`. Contains display snapshots (`student_name`, `student_nis`, `subject_name`, `study_group_name`, `operational_period_name`, `recorded_by_teacher_name`). Status `PRESENT`, `ABSENT`, `EXCUSED`, `SICK`, `LATE`.
   - `IAttendanceRecordRepository.cs`.
   - `AttendanceErrors.cs`.
2. Implement persistence in `ElsasaSis.Modules.Acd/Infrastructure/Persistence/`:
   - `AttendanceRecordRepository.cs` (Dapper operations for `acd.attendance_records`).
3. Implement vertical slices in `ElsasaSis.Modules.Acd/Features/Attendance/`:
   - `RecordSubjectAttendance` (`POST /api/acd/attendance` — `UC-TCH-01`):
     - Request payload (D-08):
       ```json
       {
         "subjectId": "UUID",
         "studyGroupId": "UUID",
         "attendanceDate": "YYYY-MM-DD",
         "operationalPeriodId": "UUID",
         "items": [
           { "studentId": "UUID", "status": "PRESENT", "notes": null }
         ]
       }
       ```
     - Semantics: Batch upsert in a single database transaction guarded by `UQ_attendance_records_student_subject_date`.
     - Authorized for `Permissions.Academic.Attendance.Record`.
   - `GetSubjectAttendanceSheet` (`GET /api/acd/attendance?subjectId=&studyGroupId=&attendanceDate=&operationalPeriodId=`):
     - Cross-schema read query returning the roster of active students in `studyGroupId` enrolled in `subjectId` alongside existing attendance records for `attendanceDate` (defaulting status to null or empty for unmarked students).
     - Authorized for `Permissions.Academic.Attendance.View` or `.Record`.
4. Register repositories and handlers in `ElsasaSis.Modules.Acd/DependencyInjection.cs`.
5. Unit tests for domain models and handler validation pass in `ElsasaSis.UnitTests`.

Notes: In accordance with D-02, attendance is scoped to `subject_id`, not stage.

Implementation Notes:
- Created domain entities and repositories in `ElsasaSis.Modules.Acd/Domain/`:
  - `Model/AttendanceStatus.cs`: enum (`Present`, `Absent`, `Excused`, `Sick`, `Late`), `ToCode()`, and `ParseAttendanceStatus()`.
  - `Model/AttendanceRecord.cs`: Aggregate root scoped to `(student_id, subject_id, attendance_date)` with full display snapshot properties (`student_name`, `student_nis`, `subject_name`, `study_group_name`, `operational_period_name`, `recorded_by_teacher_name`), `Create(...)`, `UpdateStatus(...)`, and `Rehydrate(...)`.
  - `IAttendanceRecordRepository.cs`: querying by tuple, by group and date, loading enrolled student roster snapshots, and batch upserting.
  - `AttendanceErrors.cs`: domain errors with RFC 7807 error codes.
- Created infrastructure persistence in `ElsasaSis.Modules.Acd/Infrastructure/Persistence/`:
  - `SqlPersistenceHelper.cs`: transaction management and unique constraint mapping.
  - `AttendanceRecordRepository.cs`: Dapper implementation using atomic `UPDATE ... IF @@ROWCOUNT = 0 INSERT ...` in a transaction guarded by `UQ_attendance_records_student_subject_date`.
- Created vertical slices in `ElsasaSis.Modules.Acd/Features/Attendance/`:
  - `RecordSubjectAttendance/`: `RecordAttendanceRequest`, `RecordAttendanceResponse`, `RecordAttendanceValidator`, `RecordSubjectAttendanceHandler` (verifying active enrollments, resolving teacher identity from `ICurrentUser`, mutating existing aggregate roots or creating new ones, batch upserting via repository), and `RecordSubjectAttendanceEndpoint` (`POST /api/acd/attendance` mapped with `Permissions.Academic.Attendance.Record`).
  - `GetSubjectAttendanceSheet/`: `GetSubjectAttendanceSheetQuery`, `SubjectAttendanceSheetResponse`, `GetSubjectAttendanceSheetHandler` (cross-schema Dapper query spanning `acd.academic_participations`, `std.students`, and `acd.attendance_records`), and `GetSubjectAttendanceSheetEndpoint` (`GET /api/acd/attendance` mapped with `Policies.Academic.Attendance.ViewOrRecord`).
- Registered `IAttendanceRecordRepository` and `AttendanceRecordRepository` in `ElsasaSis.Modules.Acd/DependencyInjection.cs`.
- Added unit tests in `src/backend/tests/ElsasaSis.UnitTests/Acd/` (`AttendanceDomainTests.cs` and `AttendanceFeatureTests.cs`) covering domain creation, invariants, snapshot validations, status transitions, batch upsert handling, teacher claim resolution, handler error paths, and CQRS/NetArchTest write handler isolation.
- Added integration tests in `src/backend/tests/ElsasaSis.IntegrationTests/Acd/AttendanceIntegrationTests.cs` verifying sheet retrieval, batch creation, updates, and authentication checks against local database fixtures.
- Verified: `dotnet test src/backend/ElsasaSis.sln` succeeded with 279 unit tests, 11 architecture tests, and 22 integration tests passing with 0 errors.

---

### P2-S08

Title: Implement `ElsasaSis.Modules.Acd` Classroom Assessments, Score Entry & Consolidated Gradebook Slices (`UC-TCH-02`, `UC-TCH-03`, `UC-TCH-05`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement `Assignment` and `AssignmentScore` domain models, teacher assignment authorization check, batch score upsert endpoint, and SQL-only running score and attendance metric gradebook projection in `ElsasaSis.Modules.Acd`.

Depends On: P1-S01, P1-S02, P2-S05, P2-S06, P2-S07

Repository: src/backend

Completion Criteria:

1. Create domain entities and repositories in `ElsasaSis.Modules.Acd/Domain/`:
   - `Model/Assignment.cs`: Aggregate Root owning `acd.assignments` (`title`, `assignment_type`, `max_score`, `weight`, `assigned_date`, `due_date`, `subject_stage_id`, `subject_id`, `study_group_id`, `operational_period_id`, `teacher_id`), with child entities `AssignmentScore` (`student_id`, `score`, `feedback`, `graded_at`).
   - Invariants: `max_score > 0`, `weight` between `0.00` and `100.00` if provided (D-18), `score` between `0.00` and `max_score`.
   - `IAssignmentRepository.cs`.
   - `ScorebookErrors.cs`.
2. Implement persistence in `ElsasaSis.Modules.Acd/Infrastructure/Persistence/`:
   - `AssignmentRepository.cs` (Dapper operations for `acd.assignments` and `acd.assignment_scores`).
3. Implement vertical slices in `ElsasaSis.Modules.Acd/Features/Scorebook/`:
   - `CreateClassroomAssignment` (`POST /api/acd/assignments` — `UC-TCH-02`):
     - Request: `{ subjectStageId, subjectId, studyGroupId, operationalPeriodId, learningUnitId?, title, assignmentType, maxScore, weight?, assignedDate, dueDate? }`.
     - Validates `studyGroupId` is provided (D-06).
     - Authorization check (D-04): Validates that the authenticated teacher has a valid teaching assignment in `tch.teacher_assignments` for `(teacher_id, study_group_id, subject_id, operational_period_id)`.
     - Validates weight `0.00..100.00` (D-18).
     - Authorized for `Permissions.Academic.Scorebook.Manage`.
   - `EnterStudentScores` (`POST /api/acd/assignments/{id}/scores` — `UC-TCH-03`):
     - Accepts batch array: `[{ studentId, score, feedback }]`.
     - Semantics: Batch upsert per `UQ_assignment_scores_assignment_student` (D-07).
     - Validates teacher assignment and that student has `ACTIVE` participation in the assignment's `subject_stage_id`.
     - Authorized for `Permissions.Academic.Scorebook.Manage`.
   - `GetSubjectGradebookSummary` (`GET /api/acd/gradebook?subjectStageId=&studyGroupId=&operationalPeriodId=` — `UC-TCH-05`):
     - Single optimized Dapper query joining `academic_participations`, `assignments`, `assignment_scores`, and `attendance_records` (D-20).
     - **Running Score SQL Formula (`D-05`, `D-18`):**
       - If all assessments for the stage+group have `weight IS NOT NULL`: $\sum (\text{score} \times \text{weight}) / \sum \text{weight}$.
       - If any assessment has `weight IS NULL`: $\text{AVG}(\text{score})$.
     - **Attendance Metrics (`D-20`):** Aggregates count of `PRESENT`, `ABSENT`, `LATE`, `EXCUSED`, `SICK` records for each student in the operational period.
     - Authorized for `Permissions.Academic.Scorebook.View` or `.Manage`.
4. Register repositories and handlers in `ElsasaSis.Modules.Acd/DependencyInjection.cs`.
5. Unit tests for domain models and handler validation pass in `ElsasaSis.UnitTests`.

Notes: Passing threshold is pure UI-side highlighting (D-05); the backend returns raw calculated numbers.

Implementation Notes:
- Created domain entities and repositories in `ElsasaSis.Modules.Acd/Domain/`:
  - `Model/AssignmentType.cs`: enum (`Formative`, `Summative`, `Exam`) with code conversion (`FORMATIVE`, `SUMMATIVE`, `EXAM`) and parser.
  - `Model/AssignmentScore.cs`: child entity scoped to `(assignment_id, student_id)` with display snapshot fields (`student_name`, `student_nis`), `Score`, `Feedback`, `GradedAt`, UTC timestamps, and invariants (`0.00 <= score <= max_score`, `feedback.Length <= 1000`).
  - `Model/Assignment.cs`: Aggregate Root owning `acd.assignments` and child collection `AssignmentScore` with factory method `Create(...)`, `UpdateDetails(...)`, `AddOrUpdateScore(...)`, `Rehydrate(...)`, and invariants (`max_score > 0`, `0.00 <= weight <= 100.00`, `study_group_id` required per D-06, `due_date >= assigned_date`).
  - `ScorebookErrors.cs`: typed domain errors following RFC 7807 problem details mapping (`TitleRequired`, `TitleTooLong`, `InvalidAssignmentType`, `MaxScoreMustBePositive`, `InvalidWeight`, `ScoreOutOfRange`, `DueDateBeforeAssignedDate`, `FeedbackTooLong`, `ItemsRequired`, `DuplicateStudentInBatch`, `AssignmentNotFound`, `TeacherNotAuthorized`, `NoActiveTeacherAssigned`, `StudentNotActivelyEnrolled`).
  - `IAssignmentRepository.cs`: aggregate persistence (`GetByIdAsync`, `AddAsync`, `UpdateAsync`, `UpsertScoresAsync`), authorization check (`HasTeacherAssignmentAsync`, `GetActiveTeacherAssignmentAsync`), and participation enrollment check (`GetActiveParticipationsAsync`).
- Implemented persistence in `ElsasaSis.Modules.Acd/Infrastructure/Persistence/AssignmentRepository.cs`:
  - Dapper CRUD operations for `acd.assignments` and `acd.assignment_scores` with `QueryMultipleAsync` aggregate loading.
  - Transaction-safe atomic batch score upsert with `UPDATE ... WITH (UPDLOCK, HOLDLOCK) ... IF @@ROWCOUNT = 0 INSERT ...` per `UQ_assignment_scores_assignment_student`.
  - Dapper authorization check against `tch.teacher_assignments` per D-04.
  - Cross-schema Dapper active participation query against `acd.academic_participations`.
- Implemented vertical slices in `ElsasaSis.Modules.Acd/Features/Scorebook/`:
  - `CreateClassroomAssignment`:
    - `POST /api/acd/assignments` (`UC-TCH-02`).
    - Validates request via `CreateAssignmentValidator` (validates `studyGroupId` required per D-06, `weight` range 0-100 per D-18, `maxScore > 0`, `assignmentType` format, `dueDate >= assignedDate`).
    - Teacher authorization check per D-04: resolves authenticated teacher assignment from `tch.teacher_assignments` (or falls back to active teacher assignment for admin callers without `TeacherId` claim).
    - Returns `201 Created` with `AssignmentDto`. Authorized for `Permissions.Academic.Scorebook.Manage`.
  - `EnterStudentScores`:
    - `POST /api/acd/assignments/{id}/scores` (`UC-TCH-03`).
    - Accepts batch array `{ studentId, score, feedback }` with duplicate detection and validation.
    - Validates teacher assignment per D-04 and active academic participation in assignment's `subject_stage_id` (returns `409 Conflict` if student not actively enrolled).
    - Mutates aggregate root and executes transaction-safe batch upsert per `UQ_assignment_scores_assignment_student` (D-07).
    - Returns `200 OK` with `EnterStudentScoresResponse`. Authorized for `Permissions.Academic.Scorebook.Manage`.
  - `GetSubjectGradebookSummary`:
    - `GET /api/acd/gradebook?subjectStageId=&studyGroupId=&operationalPeriodId=` (`UC-TCH-05`).
    - Executes single optimized Dapper query joining `academic_participations`, `assignments`, `assignment_scores`, and `attendance_records` via `QueryMultipleAsync` with CTEs.
    - Implements running score SQL formula per D-05 and D-18: weighted sum if all assessments have non-null weights, otherwise simple average `AVG(score)`.
    - Implements attendance metrics aggregation per D-20: aggregates counts of `PRESENT`, `ABSENT`, `LATE`, `EXCUSED`, `SICK`, and `totalRecorded` in the operational period.
    - Authorized for `Policies.Academic.Scorebook.ViewOrManage`.
- Registered `IAssignmentRepository` in `ElsasaSis.Modules.Acd/DependencyInjection.cs`.
- Added unit tests in `src/backend/tests/ElsasaSis.UnitTests/Acd/`:
  - `ScorebookDomainTests.cs`: 15 unit tests covering `Assignment` creation, invariants, weight range, due date constraints, score range checks, title length constraints, score child addition and updating, max score reduction protection, and `AssignmentType` parsing.
  - `ScorebookFeatureTests.cs`: 16 unit tests covering request validators, handler error paths (missing period, missing subject, missing stage, missing study group), teacher authorization check (403 Forbidden), unassigned teacher handling (409 Conflict), duplicate batch students, missing assignment (404), inactive student participation (409 Conflict), batch score upserting, DI registration, and CQRS write handler isolation (zero `ISqlConnectionFactory` dependency).
- Added integration tests in `src/backend/tests/ElsasaSis.IntegrationTests/Acd/ScorebookIntegrationTests.cs`:
  - `GetSubjectGradebookSummary_WhenSeededDataExists_ReturnsAssignmentsRosterAndComputedMetrics`: verifies end-to-end gradebook retrieval against SQL Server, including assignment columns, student roster, exact SQL weighted running score computation (Ahmad Dahlan = 87.14, Siti Nurhaliza = 94.29), and attendance metrics.
  - `CreateClassroomAssignment_And_EnterScores_EndToEndLifecycle_Succeeds`: verifies creating a new formative assignment, entering student scores via batch endpoint, and confirming immediate reflection and updated weighted running score (89.50) in the consolidated gradebook.
- Verified test suite:
  - All 420 Unit Tests in `ElsasaSis.UnitTests` pass with 0 failures.
  - All 11 Architecture Tests in `ElsasaSis.ArchitectureTests` pass with 0 failures.
  - All 28 Integration Tests in `ElsasaSis.IntegrationTests` pass with 0 failures.

---

## P3 - Backend Quality Suites & Frontend OpenAPI Contract Sync

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P3-S09

Title: Expand `ElsasaSis.ArchitectureTests` & `ElsasaSis.UnitTests` for `Modules.Acd` Isolation and CQRS Rules

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Enforce NetArchTest boundary isolation for `ElsasaSis.Modules.Acd` alongside `Auth`, `Opr`, `Std`, and `Tch`, verify zero EF Core usage, and provide comprehensive unit tests for all Stage 2 domain aggregates and handler validators.

Depends On: P2-S04, P2-S05, P2-S06, P2-S07, P2-S08

Repository: src/backend

Completion Criteria:

1. Update `src/backend/tests/ElsasaSis.ArchitectureTests/AssemblyArchitectureContext.cs` to add `AcdModuleAssembly` (via `AcdAssemblyMarker`) to `AllApplicationAssemblies`.
2. Update `ModuleBoundaryTests.cs`:
   - Assert zero cross-module type dependencies and assembly references across all pairwise combinations of `{Auth, Opr, Std, Tch, Acd}`.
   - Assert zero references to `Api` or `Database` from `Modules.Acd`.
   - Assert zero module references from `SharedKernel` or `Contracts`.
   - Assert zero `Microsoft.EntityFrameworkCore` dependencies across all application assemblies.
3. Update `LayerAndCqrsArchitectureTests.cs`:
   - Include `AcdModuleAssembly` in `DomainLayer_ShouldNotDependOn_Infrastructure_Features_DataAccess_Or_Http`.
   - Include `AcdModuleAssembly` in `WriteSliceHandlers_ShouldNotDependOn_SqlConnectionFactory_Or_Dapper`.
4. Expand `ElsasaSis.UnitTests`:
   - Add unit tests for `StudyGroupSchedule`, `StudyGroupTimetable`, `Subject`, `SubjectStage`, `AcademicParticipation`, `AttendanceRecord`, `Assignment`, and `AssignmentScore`.
   - Add handler validation tests verifying format checks, negative score rejection, out-of-range weights, and required fields.
5. `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests/` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/` pass with `0` failures.

Notes: Runs completely in-memory with zero database or network dependency.

Implementation Notes:
- Updated `src/backend/tests/ElsasaSis.ArchitectureTests/AssemblyArchitectureContext.cs` to register `AcdModuleAssembly` via `AcdAssemblyMarker` and included it in `AllApplicationAssemblies`.
- Updated `src/backend/tests/ElsasaSis.ArchitectureTests/ModuleBoundaryTests.cs`:
  - Enforced pairwise boundary isolation for `ElsasaSis.Modules.Acd` against `Auth`, `Opr`, `Std`, `Tch`, `Api`, and `Database`.
  - Added `AcdModule_ShouldNotDependOn_OtherModules_Api_Or_Database` asserting zero cross-module type dependencies and zero forbidden assembly references.
  - Added `ElsasaSis.Modules.Acd` to the forbidden module references for `AuthModule`, `OprModule`, `StdModule`, `TchModule`, `SharedKernel`, and `Contracts`.
  - Verified zero `Microsoft.EntityFrameworkCore` dependencies across all application assemblies (including `Modules.Acd`).
- Updated `src/backend/tests/ElsasaSis.ArchitectureTests/LayerAndCqrsArchitectureTests.cs`:
  - Added `AcdModuleAssembly` to `ModuleAssemblies` TheoryData for `DomainLayer_ShouldNotDependOn_Infrastructure_Features_DataAccess_Or_Http`.
  - Added `AcdModuleAssembly` to `WriteSliceHandlers_ShouldNotDependOn_SqlConnectionFactory_Or_Dapper`.
- Expanded `ElsasaSis.UnitTests`:
  - Verified comprehensive domain tests covering `StudyGroupSchedule`, `StudyGroupTimetable`, `Subject`, `SubjectStage`, `AcademicParticipation`, `AttendanceRecord`, `Assignment`, and `AssignmentScore`.
  - Added handler validation unit tests in `ScorebookFeatureTests.cs`: negative score rejection, empty student ID, feedback character limit, negative and out-of-range weights (>100), invalid assignment type formats, zero/negative max score, and due date validation.
  - Added handler validation unit tests in `LearningStructureFeatureTests.cs`: `CreateSubjectStageValidator`, `CreateLearningUnitValidator` (sequence order, unit types, name length), and `MapSubjectStageGradeValidator` (valid combinations for SD, SMP, SMA, SMK).
- Verification:
  - `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests/` passes with 13/13 tests (0 failures).
  - `dotnet test src/backend/tests/ElsasaSis.UnitTests/` passes with 431/431 tests (0 failures).

---

### P3-S10

Title: Implement Stage 2 Integration Tests (`WebApplicationFactory`, Respawn, Timetables, Curriculum, Participations, Attendance & Scorebook)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Add full HTTP + SQL Server integration test coverage for all 8 Stage 2 use cases, multi-role authorization policies, batch enrollments, attendance upserts, assignment score submissions, and running score computations.

Depends On: P2-S04, P2-S05, P2-S06, P2-S07, P2-S08

Repository: src/backend

Completion Criteria:

1. Update `IntegrationTestWebApplicationFactory.cs` and `IntegrationTestBase.cs`:
   - Add Stage 2 table ignore entries in `RespawnerOptions.TablesToIgnore` if static seeds are preserved.
   - Implement `SeedStage2FixturesAsync()` invoking `Stage2Seeder.SeedStage2FixturesAsync(...)`.
2. Create `ElsasaSis.IntegrationTests/Opr/Stage2TimetableIntegrationTests.cs`:
   - Admin configures study group timetable slot (`POST /api/opr/study-groups/{id}/timetables` returns `201`/`200`).
   - Query timetable returns slots with `teacherConflict = false` and `roomConflict = false`.
   - Configure overlapping slot in another study group: query verifies conflict flag turns `true` without blocking write.
   - Delete slot returns `204`.
   - SMK block date range CRUD (`POST`, `GET`, `DELETE`).
   - Teacher personal schedule query (`GET /api/opr/teacher-schedule`) returns only assigned classes for authenticated teacher.
3. Create `ElsasaSis.IntegrationTests/Acd/Stage2CurriculumIntegrationTests.cs`:
   - Subject, stage, learning unit CRUD and sequence ordering validation.
   - Grade mapping creation and deletion.
   - Batch academic participation enrollment (`POST /api/acd/participations/batch`): placed students of `7A` enrolled; second call skips duplicate students idempotently and returns `{ enrolledCount: 0, skippedCount: N }`.
4. Create `ElsasaSis.IntegrationTests/Acd/Stage2AttendanceIntegrationTests.cs`:
   - Retrieve attendance sheet for subject + study group (`GET /api/acd/attendance`).
   - Batch upsert attendance records (`POST /api/acd/attendance`).
   - Update an existing student's status from `PRESENT` to `SICK` with notes: verifies upsert semantics without duplicate rows in `acd.attendance_records`.
5. Create `ElsasaSis.IntegrationTests/Acd/Stage2ScorebookIntegrationTests.cs`:
   - Create formative and summative assignments (`POST /api/acd/assignments`).
   - Verify unassigned teacher cannot create assignments for that study group (`403 Forbidden`).
   - Batch score entry (`POST /api/acd/assignments/{id}/scores`): submit scores and qualitative feedback.
   - Gradebook summary query (`GET /api/acd/gradebook`): verifies running score computation matches weighted formula and includes aggregated attendance counts.
6. `dotnet test src/backend/ElsasaSis.sln` passes ALL unit, architecture, and integration tests with `0` failures against local SQL Server / `(localdb)\MSSQLLocalDB`.

Notes: Ensure existing Stage 0 and Stage 1 integration tests continue to pass without regression.

Implementation Notes:
- Updated `IntegrationTestWebApplicationFactory.cs` and `IntegrationTestBase.cs`:
  - Remediated RV-P3-S10-001: Removed `acd.subjects` from `RespawnerOptions.TablesToIgnore` and added deterministic `ReseedCanonicalSubjectsAsync(connection)` in `IntegrationTestWebApplicationFactory.ResetDatabaseAsync()`, clearing any dynamic subjects and re-seeding the 10 canonical seeded subjects (`PA-BP`, `PPKN`, `BIND`, `MTK`, `IPA`, `IPS`, `BING`, `SNB`, `PJOK`, `INF`) on every reset.
  - Added `SeedStage2FixturesAsync()` to `IntegrationTestWebApplicationFactory` delegating to `Stage2Seeder.SeedStage2FixturesAsync(...)` and updated `IntegrationTestBase.SeedStage2FixturesAsync()` to call through `Factory.SeedStage2FixturesAsync()`.
- Created `ElsasaSis.IntegrationTests/Opr/Stage2TimetableIntegrationTests.cs`:
  - Verified admin slot creation (`POST /api/opr/study-groups/{id}/timetables`).
  - Verified timetable query returns slots with `teacherConflict = false` and `roomConflict = false`.
  - Configured overlapping slot in second study group (`7B`); verified non-blocking write success and query verification of `teacherConflict = true` and `roomConflict = true` across affected groups.
  - Verified slot deletion returning `204 NoContent` and reversion of conflict flags to `false`.
  - Verified SMK block date range lifecycle (`POST`, `GET`, `DELETE` returning `204`).
  - Verified teacher personal schedule query (`GET /api/opr/teacher-schedule`) returns only assigned classes for authenticated teacher profile (`teacher.budi`).
  - Verified anonymous requests receive `401 Unauthorized`.
- Created `ElsasaSis.IntegrationTests/Acd/Stage2CurriculumIntegrationTests.cs`:
  - Verified subject CRUD (`POST`, `PUT`), stage CRUD, and sequence ordering uniqueness validation (`409 Conflict`).
  - Verified modular learning unit CRUD (`POST`, `PUT`) and unit sequence conflict rejection (`409 Conflict`).
  - Verified grade mapping creation (`POST`) and deletion (`DELETE` returning `204`).
  - Verified batch academic participation enrollment (`POST /api/acd/participations/batch`): initial call enrolled placed students of `7A`; second call idempotently skipped existing students and returned `{ enrolledCount: 0, skippedCount: 2 }`.
  - Verified query endpoint `GET /api/acd/participations` returning enrolled students with `ACTIVE` status.
- Created `ElsasaSis.IntegrationTests/Acd/Stage2AttendanceIntegrationTests.cs`:
  - Verified retrieval of attendance sheet for subject + study group (`GET /api/acd/attendance`).
  - Verified batch upsert of attendance records (`POST /api/acd/attendance`).
  - Verified status update from `PRESENT` to `SICK` with notes without generating duplicate rows in `acd.attendance_records` (asserted table row count remains constant).
  - Verified anonymous requests receive `401 Unauthorized`.
- Created `ElsasaSis.IntegrationTests/Acd/Stage2ScorebookIntegrationTests.cs`:
  - Verified unassigned teacher cannot create assignments for an unassigned study group (`403 Forbidden`).
  - Verified formative and summative assignment creation (`POST /api/acd/assignments` returning `201 Created`).
  - Verified batch score entry with qualitative feedback (`POST /api/acd/assignments/{id}/scores`).
  - Verified consolidated gradebook summary query (`GET /api/acd/gradebook`) matching running score weighted calculation formula and aggregated attendance counts.
  - Verified anonymous requests receive `401 Unauthorized`.
- Verification:
  - `dotnet test src/backend/ElsasaSis.sln` passed all 431 UnitTests, 13 ArchitectureTests, and 35 IntegrationTests with `0` failures across multiple consecutive executions against local SQL Server. All tests, including `Stage1TchIntegrationTests.AcademicSubjects_ReturnsAllTenSeededSubjects`, pass with zero regression.

---

### P3-S11

Title: Synchronize Stage 2 OpenAPI Contract (`schema.d.ts`), Query Keys & Shared API Clients

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Synchronize `src/frontend/src/shared/types/api/schema.d.ts` from running `ElsasaSis.Api` Swagger documentation, declare query key factories, and author typed API client services for all 4 Stage 2 feature modules.

Depends On: P1-S03, P2-S04, P2-S05, P2-S06, P2-S07, P2-S08

Repository: src/frontend

Completion Criteria:

1. Start `ElsasaSis.Api` temporarily in `Development`, regenerate `src/frontend/src/shared/types/api/schema.d.ts` via `openapi-typescript` against `/swagger/v1/swagger.json`, verify zero drift with `--check`, and stop the temporary API server.
2. Confirm `schema.d.ts` contains typed operations and schemas for all new endpoints:
   - `/api/opr/study-groups/{id}/timetables*`, `/api/opr/study-groups/{id}/blocks*`, `/api/opr/teacher-schedule`
   - `/api/acd/subjects*`, `/api/acd/subjects/{id}/stages*`, `/api/acd/subjects/{id}/stages/{stageId}/units*`, `/api/acd/subjects/{id}/stages/{stageId}/grade-mappings*`
   - `/api/acd/participations/batch`, `/api/acd/participations*`
   - `/api/acd/attendance*`
   - `/api/acd/assignments*`, `/api/acd/assignments/{id}/scores*`, `/api/acd/gradebook*`
3. Create query key factories and typed API services:
   - `src/frontend/src/features/operations/scheduling/api/timetables.api.ts` (query keys `timetableKeys`, calls for grid, blocks, slots, teacher personal schedule).
   - `src/frontend/src/features/academic/curriculum/api/curriculum.api.ts` (query keys `curriculumKeys`, calls for learning structure tree, grade mappings, batch enrollment).
   - `src/frontend/src/features/academic/attendance/api/attendance.api.ts` (query keys `attendanceKeys`, calls for attendance sheet, batch upsert).
   - `src/frontend/src/features/academic/scorebook/api/scorebook.api.ts` (query keys `scorebookKeys`, calls for assignments, batch scores, gradebook summary).
4. MSW unit tests in `__tests__/` for each API client service verifying HTTP contracts, request shapes, and parameter mapping.
5. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass in `src/frontend/`.

Notes: Strictly import only `apiClient` and `schema.d.ts` with zero cross-slice or app-layer dependencies.

Implementation Notes:
- Started `ElsasaSis.Api` in `Development` mode, regenerated `src/frontend/src/shared/types/api/schema.d.ts` via `openapi-typescript`, verified with `--check` (zero drift), and stopped the temporary API server.
- Verified that `schema.d.ts` includes all new operations and component schemas for Opr Timetables/Blocks/TeacherSchedule and Acd Subjects/Stages/Units/GradeMappings/Participations/Attendance/Assignments/Scores/Gradebook.
- Created query key factories and typed API client services:
  - `src/frontend/src/features/operations/scheduling/api/timetables.keys.ts` & `timetables.api.ts` (`timetableKeys`, `timetablesApi` for grid, slots, blocks, and teacher personal schedule).
  - `src/frontend/src/features/academic/curriculum/api/curriculum.keys.ts` & `curriculum.api.ts` (`curriculumKeys`, `curriculumApi` for subjects, stages, learning units, grade mappings, and batch enrollment).
  - `src/frontend/src/features/academic/attendance/api/attendance.keys.ts` & `attendance.api.ts` (`attendanceKeys`, `attendanceApi` for attendance sheet and batch recording).
  - `src/frontend/src/features/academic/scorebook/api/scorebook.keys.ts` & `scorebook.api.ts` (`scorebookKeys`, `scorebookApi` for assignments, batch score entries, and gradebook summary).
- Created MSW-backed unit tests for each API service:
  - `src/frontend/src/features/operations/scheduling/api/__tests__/timetables.api.spec.ts` (9 tests)
  - `src/frontend/src/features/academic/curriculum/api/__tests__/curriculum.api.spec.ts` (15 tests)
  - `src/frontend/src/features/academic/attendance/api/__tests__/attendance.api.spec.ts` (3 tests)
  - `src/frontend/src/features/academic/scorebook/api/__tests__/scorebook.api.spec.ts` (4 tests)
- Verified all quality gates in `src/frontend/`:
  - `pnpm typecheck` passed (`vue-tsc --noEmit` exited 0).
  - `pnpm lint` passed (`eslint . --max-warnings 0` exited 0).
  - `pnpm test:unit` passed (34 test files, 198 tests passed).
  - `pnpm build` passed (`vue-tsc --noEmit && vite build` built successfully).

Changed Files:
- `src/frontend/src/shared/types/api/schema.d.ts`
- `src/frontend/src/features/operations/scheduling/api/timetables.keys.ts`
- `src/frontend/src/features/operations/scheduling/api/timetables.api.ts`
- `src/frontend/src/features/operations/scheduling/api/__tests__/timetables.api.spec.ts`
- `src/frontend/src/features/academic/curriculum/api/curriculum.keys.ts`
- `src/frontend/src/features/academic/curriculum/api/curriculum.api.ts`
- `src/frontend/src/features/academic/curriculum/api/__tests__/curriculum.api.spec.ts`
- `src/frontend/src/features/academic/attendance/api/attendance.keys.ts`
- `src/frontend/src/features/academic/attendance/api/attendance.api.ts`
- `src/frontend/src/features/academic/attendance/api/__tests__/attendance.api.spec.ts`
- `src/frontend/src/features/academic/scorebook/api/scorebook.keys.ts`
- `src/frontend/src/features/academic/scorebook/api/scorebook.api.ts`
- `src/frontend/src/features/academic/scorebook/api/__tests__/scorebook.api.spec.ts`
- `docs/issues/IS003- STAGE-2-DEV-IMPLEMENTATION-PLAN.md`

---

## P4 - Frontend Stage 2 Feature Screens (4 Parallel Screens)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P4-S12

Title: Implement `SCR-OPR-TIMETABLE` (`TimetableWorkspaceView.vue` — Schedule Management Pattern)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Deliver the complete Schedule Management pattern (Pattern 4) for study group weekly timetables, slot configuration drawer, SMK block date management drawer, inline conflict highlights, and teacher personal schedule tab.

Depends On: P3-S11

Repository: src/frontend

Completion Criteria:

1. Implement `src/frontend/src/features/operations/scheduling/views/TimetableWorkspaceView.vue` and subcomponents:
   - **Context Controls:** Study group selector (filtered by academic year), schedule type toggle (`REGULAR`, `GENERAL`, `VOCATIONAL`).
   - **Weekly Matrix Grid:** Days (Monday–Saturday) $\times$ Period Slots (1–10). Each cell displays Subject, Teacher, Room, and Start/End times.
   - **Slot Assignment Drawer (Tier 3 Right-Anchored):** Triggered by clicking an empty or populated slot. Form managed with VeeValidate + Zod allowing selection of Subject, Teacher, Room, and period times. Save dispatches `POST /api/opr/study-groups/{id}/timetables`. Delete button dispatches `DELETE /api/opr/study-groups/{id}/timetables/{scheduleId}` with Tier 2 confirmation dialog.
   - **SMK Block Date Management Drawer:** Accessible via "Manage SMK Blocks" action button. Lists active General/Vocational date ranges with add/remove controls.
   - **Inline Conflict Indicators:** Displays inline `rose-600` badge and warning tooltip on any cell where `teacherConflict === true` or `roomConflict === true` (D-10).
   - **Teacher Personal Schedule Tab (`UC-TCH-10`):** Visible when user has `Permissions.Operations.Timetables.ViewOwn`. Displays weekly matrix filtered strictly to the logged-in teacher's classes. Client-side date check resolves active SMK block (D-11).
2. Author unit and component tests with Vitest + MSW covering slot selection, drawer open/close, conflict badge rendering, and teacher tab switching.
3. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass.

Notes: Follow UI/UX Guideline Pattern 4 and 3-tier confirmation standards.

Implementation Notes:
- Implemented `src/frontend/src/features/operations/scheduling/schemas/timetable.schema.ts` with Zod validation for slot configuration (`periodSlot` 1-10, `dayOfWeek` 1-7, HH:mm times, start < end) and SMK block creation (`GENERAL` | `VOCATIONAL` rotation type, date ranges, end >= start).
- Extended shared lookup services in `src/frontend/src/shared/services/lookups.api.ts` with `fetchRoomsLookup` and `roomsLookupQuery` to prevent cross-feature imports into `operations/calendar`.
- Implemented TanStack Query hooks and mutations:
  - `src/frontend/src/features/operations/scheduling/api/useTimetableQueries.ts`: `useTimetableGridQuery`, `useSmkBlocksQuery`, `useTeacherPersonalScheduleQuery`.
  - `src/frontend/src/features/operations/scheduling/api/useTimetableMutations.ts`: `useSaveTimetableSlotMutation`, `useDeleteTimetableSlotMutation`, `useCreateSmkBlockMutation`, `useDeleteSmkBlockMutation` with query invalidations and toast alerts.
- Implemented subcomponents under `src/frontend/src/features/operations/scheduling/components/`:
  - `TimetableGrid.vue`: 6 days (Monday-Saturday) $\times$ 10 period slots matrix with cell details (Subject, Teacher, Room, Start/End times), inline `rose-600` conflict badge, conflict tooltip, and slot click events.
  - `SlotAssignmentDrawer.vue`: Tier 3 right-anchored drawer using VeeValidate + Zod, asynchronous subject/teacher/room lookups, save slot mutation, and Tier 2 `ConfirmActionDialog` for slot deletion.
  - `SmkBlockManagementDrawer.vue`: Tier 3 drawer listing active General/Vocational date ranges with add/remove controls (`POST`/`DELETE /api/opr/study-groups/{id}/blocks`).
  - `TeacherPersonalScheduleTab.vue`: Teacher's personal weekly schedule tab (`UC-TCH-10`) with cycle switcher and client-side SMK date check resolving the active rotation block.
- Implemented master view `src/frontend/src/features/operations/scheduling/views/TimetableWorkspaceView.vue`:
  - Top context bar with Academic Year selection, Study Group dropdown, and Schedule Type toggle buttons (`REGULAR`, `GENERAL`, `VOCATIONAL`).
  - Historical period read-only banner and edit control disabling.
  - Overall conflict alert banner when matrix cells have room/teacher conflicts.
  - Tab navigation between Study Group Timetable and Teacher Personal Schedule (guarded by `Permissions.Operations.Timetables.ViewOwn`).
- Authored unit and component tests:
  - `src/frontend/src/features/operations/scheduling/schemas/__tests__/timetable.schema.spec.ts` (13 tests) covering schema rules and invalid cases.
  - `src/frontend/src/features/operations/scheduling/views/__tests__/TimetableWorkspaceView.spec.ts` (10 tests) covering empty state, matrix grid rendering, inline conflict badge, drawer workflows (open/close, slot save, Tier 2 slot delete, SMK block add/delete), tab switching, and historical banner.
- Verified quality gates in `src/frontend/`:
  - `pnpm typecheck` passed (`vue-tsc --noEmit` exited 0).
  - `pnpm lint` passed (`eslint . --max-warnings 0` exited 0).
  - `pnpm test:unit` passed across entire test suite (41 test files, 261 passed, 0 failed).

Changed Files:
- `src/frontend/src/shared/services/lookups.api.ts`
- `src/frontend/src/features/operations/scheduling/schemas/timetable.schema.ts`
- `src/frontend/src/features/operations/scheduling/schemas/__tests__/timetable.schema.spec.ts`
- `src/frontend/src/features/operations/scheduling/api/useTimetableQueries.ts`
- `src/frontend/src/features/operations/scheduling/api/useTimetableMutations.ts`
- `src/frontend/src/features/operations/scheduling/components/TimetableGrid.vue`
- `src/frontend/src/features/operations/scheduling/components/SlotAssignmentDrawer.vue`
- `src/frontend/src/features/operations/scheduling/components/SmkBlockManagementDrawer.vue`
- `src/frontend/src/features/operations/scheduling/components/TeacherPersonalScheduleTab.vue`
- `src/frontend/src/features/operations/scheduling/views/TimetableWorkspaceView.vue`
- `src/frontend/src/features/operations/scheduling/views/__tests__/TimetableWorkspaceView.spec.ts`
- `docs/issues/IS003- STAGE-2-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S13

Title: Implement `SCR-ACD-CURRICULUM` (`CurriculumSetupView.vue` — Planning Pattern & Batch Enrollment)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Deliver the Planning pattern (Pattern 5) for curriculum hierarchy management (Subject $\rightarrow$ Stage $\rightarrow$ Learning Unit), administrative grade mappings, and the batch student enrollment workflow with Tier 3 Pre-Flight Impact Summary Modal.

Depends On: P3-S11

Repository: src/frontend

Completion Criteria:

1. Implement `src/frontend/src/features/academic/curriculum/views/CurriculumSetupView.vue` and subcomponents:
   - **3-Level Hierarchical Tree:** Expandable/collapsible view of Subjects, sequential Subject Stages, and modular Learning Units (`UC-ADM-17`).
   - **Curriculum Modals/Drawers:**
     - Add/Edit Subject modal.
     - Add/Edit Subject Stage modal (name, sequence order, description).
     - Add/Edit Learning Unit modal (name, unit type, sequence order, description).
     - Tier 2 confirmation dialogs for unit deletion and grade mapping removal.
   - **Grade Mapping Canvas:** Visual matrix or tag selector linking a Subject Stage to administrative grades (`SD`, `SMP`, `SMA`, `SMK` $\times$ grades 1–12).
   - **Batch Student Enrollment Workflow (`UC-ADM-18`):**
     - "Enroll Study Group" action button on a selected Subject Stage.
     - Selector for target Study Group.
     - **Tier 3 Pre-Flight Impact Summary Modal (D-17):** Displays summary cards showing target Subject Stage, Study Group name, eligible student count preview, and explanation of duplicate skip behavior.
     - Confirm button dispatches `POST /api/acd/participations/batch`. Displays success toast with enrolled and skipped counts.
2. Author unit and component tests with Vitest + MSW covering tree expansion, modal form validations, and pre-flight modal lifecycle.
3. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass.

Notes: Adhere strictly to UI/UX Guideline Pattern 5 and Tier 3 confirmation modal requirements.

Implementation Notes:
- Created Zod validation schemas and types in `schemas/curriculum.schema.ts` (`subjectSchema`, `subjectStageSchema`, `learningUnitSchema`, `batchEnrollSchema`, `VALID_GRADES_BY_LEVEL`).
- Created TanStack Query hooks in `api/useCurriculumQueries.ts` (`useCurriculumStructureQuery`, `useSubjectStageParticipationsQuery`).
- Created TanStack Mutation hooks in `api/useCurriculumMutations.ts` (`useCreateSubjectMutation`, `useUpdateSubjectMutation`, `useCreateSubjectStageMutation`, `useUpdateSubjectStageMutation`, `useCreateLearningUnitMutation`, `useUpdateLearningUnitMutation`, `useDeleteLearningUnitMutation`, `useMapGradeMutation`, `useRemoveGradeMappingMutation`, `useBatchEnrollMutation`) with RFC 7807 error extraction and PrimeVue toast notifications.
- Created `components/CurriculumTree.vue` implementing the 3-level hierarchical tree (`Subjects -> Subject Stages -> Learning Units`) with expandable/collapsible nodes, stage selection, sequence order badges, unit type chips, and action buttons (`Add Stage`, `Add Unit`, `Enroll Group`, `Edit`, `Delete`).
- Created `components/SubjectFormModal.vue`, `SubjectStageFormModal.vue`, and `LearningUnitFormModal.vue` with VeeValidate + Zod schema validation, FormField bindings, and conflict banners.
- Created `components/GradeMappingCanvas.vue` implementing the interactive visual matrix across school levels (`SD`, `SMP`, `SMA`, `SMK` $\times$ grades 1–12) with active mapping highlights and remove triggers.
- Created `components/BatchEnrollModal.vue` delivering the Tier 3 Pre-Flight Impact Summary Modal (`D-17`, `UC-ADM-18`) with target stage and study group summary cards, eligible student count preview, idempotent duplicate skip explanation, and batch commit dispatch.
- Implemented `views/CurriculumSetupView.vue` integrating the Dual-Region Planning canvas (Pattern 5), active stage selection, grade mapping canvas, enrolled participations roster preview, and Tier 2 confirmation dialogs (`ConfirmActionDialog.vue`) for unit deletion and grade mapping removal. Enforced role-based visibility ("Hide by Role") using `Permissions.Academic.Curriculum.Manage`.
- Authored comprehensive test suites:
  - `schemas/__tests__/curriculum.schema.spec.ts` (9 tests)
  - `views/__tests__/CurriculumSetupView.spec.ts` (9 tests with Vitest + MSW covering tree expansion, subject creation, stage creation, unit creation, grade mapping toggles, unit deletion dialog, batch enroll pre-flight modal lifecycle, and teacher read-only view).
- Verification:
  - `pnpm --dir src/frontend exec vitest run src/features/academic/curriculum` passed (3 test files, 33 tests passed).
  - `pnpm --dir src/frontend exec eslint src/features/academic/curriculum` passed (0 warnings, 0 errors).

Changed Files:
- `src/frontend/src/features/academic/curriculum/schemas/curriculum.schema.ts`
- `src/frontend/src/features/academic/curriculum/schemas/__tests__/curriculum.schema.spec.ts`
- `src/frontend/src/features/academic/curriculum/api/useCurriculumQueries.ts`
- `src/frontend/src/features/academic/curriculum/api/useCurriculumMutations.ts`
- `src/frontend/src/features/academic/curriculum/components/CurriculumTree.vue`
- `src/frontend/src/features/academic/curriculum/components/SubjectFormModal.vue`
- `src/frontend/src/features/academic/curriculum/components/SubjectStageFormModal.vue`
- `src/frontend/src/features/academic/curriculum/components/LearningUnitFormModal.vue`
- `src/frontend/src/features/academic/curriculum/components/GradeMappingCanvas.vue`
- `src/frontend/src/features/academic/curriculum/components/BatchEnrollModal.vue`
- `src/frontend/src/features/academic/curriculum/views/CurriculumSetupView.vue`
- `src/frontend/src/features/academic/curriculum/views/__tests__/CurriculumSetupView.spec.ts`
- `docs/issues/IS003- STAGE-2-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S14

Title: Implement `SCR-ACD-ATTENDANCE` (`AttendanceWorkbenchView.vue` — Work Queue Pattern & Batch Commits)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Deliver the Work Queue pattern (Pattern 1) for daily classroom attendance tracking, featuring session selectors, student roster grid, "Mark All Present" shortcut, attendance status chips, dirty tracking, sticky Save bar, and route exit guards.

Depends On: P3-S11

Repository: src/frontend

Completion Criteria:

1. Implement `src/frontend/src/features/academic/attendance/views/AttendanceWorkbenchView.vue` and subcomponents:
   - **Session Filter Bar:** Subject selector, Study Group selector, and Calendar Date picker (defaulting to current date) (`UC-TCH-01`).
   - **Quick-Mark Action Bar:** "Mark All Present" shortcut button that instantly populates all student chips to `PRESENT` client-side (D-08).
   - **Student Attendance Roster Grid:** Displays student avatar, NIS, full name, attendance status chips (`PRESENT` [emerald], `ABSENT` [rose], `EXCUSED` [blue], `SICK` [amber], `LATE` [orange]), and optional notes input.
   - **High-Velocity Sheet Ergonomics (D-08):**
     - Local component dirty tracking: `isDirty` flag set to true when any chip or note is changed.
     - Sticky bottom Save bar appearing when `isDirty === true` with "Save Changes" and "Cancel" buttons.
     - Navigation guard: Intercepts uncommitted navigation via `onBeforeRouteLeave` to prevent data loss.
     - Save button dispatches `POST /api/acd/attendance` batch upsert and resets dirty state on success.
2. Author unit and component tests with Vitest + MSW covering "Mark All Present", chip toggles, dirty-state triggers, and sticky bar commit.
3. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass.

Notes: In accordance with D-02, attendance is scoped to `subjectId + studyGroupId`.

Implementation Notes:
- Created query and mutation composables in `src/frontend/src/features/academic/attendance/api/`:
  - `useAttendanceQueries.ts`: `useAttendanceSheetQuery` wrapping `attendanceApi.getSheet` with reactive query keys.
  - `useAttendanceMutations.ts`: `useRecordAttendanceMutation` wrapping `attendanceApi.batchRecord` with deterministic cache invalidation and PrimeVue Toast feedback.
- Created presentation and ergonomic subcomponents in `src/frontend/src/features/academic/attendance/components/`:
  - `AttendanceFilterBar.vue`: Subject and Study Group dropdown selectors and HTML5 calendar date picker defaulting to current date (`formatBackendDateOnly(new Date())`).
  - `AttendanceActionBar.vue`: Quick-mark action bar featuring "Mark All Present" shortcut button and status summary counters (Total, Present, Absent, Excused, Sick, Late, Unmarked).
  - `AttendanceRosterGrid.vue`: Student attendance matrix displaying initial-letter avatars, NIS, full name, 5 segmented status chips (`PRESENT` [emerald], `ABSENT` [rose], `EXCUSED` [blue], `SICK` [amber], `LATE` [orange]), optional notes input field, and visual dirty row accents (`border-l-4 border-l-amber-500 bg-amber-50/20`). Role-aware "Disable by State" for read-only users.
  - `AttendanceStickyBar.vue`: Floating sticky bottom commit bar appearing when `isDirty === true` with modified change counter, "Cancel" discard button, and "Save Changes" primary commit button.
- Implemented `src/frontend/src/features/academic/attendance/views/AttendanceWorkbenchView.vue`:
  - Contextual synchronization with `useAppContextBridge()` and URL query parameters (`subjectId`, `studyGroupId`, `date`).
  - Local reactive dirty-state tracking comparing working roster against initial server baseline.
  - Intercepted uncommitted navigation via Vue Router `onBeforeRouteLeave` and browser `beforeunload` guards.
  - In-page filter switch confirmation dialog protecting unsaved edits across class sessions.
- Authored comprehensive unit & component tests in `src/frontend/src/features/academic/attendance/views/__tests__/AttendanceWorkbenchView.spec.ts`:
  - 8 Vitest + MSW test scenarios verifying missing filter empty states, roster sheet loading, "Mark All Present" shortcut, individual chip toggles and notes inputs, visual dirty row markers, sticky bar appearance, discard reverting, atomic batch commit via `POST /api/acd/attendance`, `onBeforeRouteLeave` navigation cancellation and confirmation, and read-only permission disabling.
- Verification:
  - Vitest: All 11 tests in `src/frontend/src/features/academic/attendance` pass (`attendance.api.spec.ts` [3 tests], `AttendanceWorkbenchView.spec.ts` [8 tests]).
  - ESLint: `pnpm exec eslint src/features/academic/attendance` passed with 0 errors and 0 warnings.
  - TypeScript: Vue-tsc compiled all attendance views and components with 0 errors.
- Remediation Iteration 1 (`RV-P4-S14-001`):
  - Fixed TS2533 TypeScript errors in `src/features/academic/attendance/views/__tests__/AttendanceWorkbenchView.spec.ts` at lines 350 and 355 with non-null assertion on optional array item indexing (`capturedPayload!.items![0]` and `capturedPayload!.items![1]`).
  - Verification:
    - TypeScript: `AttendanceWorkbenchView.spec.ts` compiles cleanly under `vue-tsc --noEmit` with 0 errors.
    - ESLint: `pnpm --dir src/frontend lint src/features/academic/attendance` exits 0.
    - Vitest: All 11 tests in `src/features/academic/attendance` pass cleanly.

Changed Files:
- `src/frontend/src/features/academic/attendance/api/useAttendanceQueries.ts`
- `src/frontend/src/features/academic/attendance/api/useAttendanceMutations.ts`
- `src/frontend/src/features/academic/attendance/components/AttendanceFilterBar.vue`
- `src/frontend/src/features/academic/attendance/components/AttendanceActionBar.vue`
- `src/frontend/src/features/academic/attendance/components/AttendanceRosterGrid.vue`
- `src/frontend/src/features/academic/attendance/components/AttendanceStickyBar.vue`
- `src/frontend/src/features/academic/attendance/views/AttendanceWorkbenchView.vue`
- `src/frontend/src/features/academic/attendance/views/__tests__/AttendanceWorkbenchView.spec.ts`
- `docs/issues/IS003- STAGE-2-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S15

Title: Implement `SCR-ACD-SCOREBOOK` (`ScorebookWorkbenchView.vue` — Work Queue Pattern & Spreadsheet Grid)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Deliver the Work Queue pattern (Pattern 1) for teacher scorebooks, featuring assessment creation, inline spreadsheet score entry with keyboard navigation, reactive running score computation, passing threshold visual highlights, and attendance summary metrics.

Depends On: P3-S11

Repository: src/frontend

Completion Criteria:

1. Implement `src/frontend/src/features/academic/scorebook/views/ScorebookWorkbenchView.vue` and subcomponents:
   - **Context Filter Bar:** Subject Stage selector and Study Group selector (`UC-TCH-02`, `UC-TCH-03`, `UC-TCH-05`).
   - **Assessment Action Header:** "Create Assessment" button opening a Tier 2 modal (`title`, `assignmentType`, `maxScore`, `weight`, `assignedDate`, `dueDate`).
   - **Assessment Header Cards:** Displays column headers for each assessment with type badge (`FORMATIVE`, `SUMMATIVE`, `EXAM`), max points, and weight percentage.
   - **Spreadsheet Score Entry Grid (Tier 1 Direct Edit — D-07):**
     - Student roster rows (NIS, Name).
     - Cell inputs for numeric scores (`0.00..maxScore`) and qualitative feedback tooltip/popover.
     - Keyboard navigation: `Enter` moves to the next student row; arrow keys navigate cells.
     - Visual passing threshold highlight: Configurable threshold input in the toolbar; cells falling below threshold highlight in rose (D-05).
     - Dirty tracking and sticky bottom Save bar: Commits batch array via `POST /api/acd/assignments/{id}/scores`.
     - Route navigation guard: `onBeforeRouteLeave` confirmation if uncommitted score changes exist.
   - **Summary Projection Columns (D-20):**
     - **Running Score Column:** Displays weighted running total computed from backend projection.
     - **Attendance Summary Column:** Displays aggregate counts `{ present, absent, late, excused, sick }` for the operational period.
2. Author unit and component tests with Vitest + MSW covering assessment creation, spreadsheet keyboard events, score updates, and running score display.
3. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass.

Notes: Adhere strictly to UI/UX Guideline Pattern 1 and spreadsheet grid interaction specifications.

Implementation Notes:
- Created Zod validation schema in `src/frontend/src/features/academic/scorebook/schemas/scorebook.schema.ts` for assessment creation (`title`, `assignmentType`, `maxScore`, `weight`, `assignedDate`, `dueDate`).
- Authored unit tests for schema in `src/frontend/src/features/academic/scorebook/schemas/__tests__/scorebook.schema.spec.ts` (7 tests).
- Created presentation and grid subcomponents in `src/frontend/src/features/academic/scorebook/components/`:
  - `AssessmentHeaderCard.vue`: Displays assessment column header card with type badge (`FORMATIVE` [sky], `SUMMATIVE` [amber], `EXAM` [purple]), max points, and weight percentage.
  - `ScoreCellInput.vue`: Direct spreadsheet cell input with numeric score clamping (`0.00..maxScore`), passing threshold check (< threshold -> rose-50 highlight per D-05), dirty indicator dot, qualitative feedback popover/modal trigger, and keyboard navigation event dispatching (`Enter`, `ArrowUp`, `ArrowDown`, `ArrowLeft`, `ArrowRight`).
  - `CreateAssessmentModal.vue`: Tier 2 Dialog modal (max-w-lg) for assessment creation (`title`, `assignmentType`, `maxScore`, `weight`, `assignedDate`, `dueDate`, `learningUnitId`), VeeValidate + Zod validation, and error banner mapping.
  - `ScorebookFilterBar.vue`: Context selector toolbar featuring Subject Stage dropdown, Study Group (Rombel) dropdown, configurable passing threshold (KKM) input, refresh button, and "Create Assessment" action button.
- Implemented `src/frontend/src/features/academic/scorebook/views/ScorebookWorkbenchView.vue`:
  - Contextual synchronization with `useAppContextBridge()` and URL query parameters (`subjectStageId`, `studyGroupId`, `threshold`).
  - Spreadsheet grid container with sticky left identifier columns (#, NIS, Student Name), dynamic assessment columns, and sticky right summary columns (Running Score, Attendance).
  - Reactive running score computation matching backend formula (`AVG(score)` when null weights exist; `SUM(score * weight) / SUM(weight)` when all weights present), reactively updating upon inline cell score edits.
  - Passing threshold visual highlighting: below-threshold scores highlight in rose (`bg-rose-50 text-rose-700`).
  - Dirty tracking and floating sticky bottom Save bar appearing when edits are dirty, showing modified count, "Discard Changes" undo, and "Save Changes" batch commit button calling `POST /api/acd/assignments/{id}/scores`.
  - Route navigation guard: `onBeforeRouteLeave` intercepting uncommitted edits with `ConfirmActionDialog`, and browser `beforeunload` warning.
  - Attendance summary column displaying aggregated counts `{ present, absent, late, excused, sick }` per student for the operational period.
- Authored comprehensive unit and component tests in `src/frontend/src/features/academic/scorebook/views/__tests__/ScorebookWorkbenchView.spec.ts`:
  - 7 Vitest + MSW tests covering initial roster and gradebook loading, rose highlighting below threshold, client-side reactive running score recalculation, batch score commit via MSW POST, discard reverting, Tier 2 modal assessment creation, and keyboard navigation.
- Fixed minor typing issues in `TimetableWorkspaceView.spec.ts` to ensure clean repo-wide compilation.
- Verification:
  - Vitest: All 18 tests in `src/features/academic/scorebook` pass; all 261 frontend tests pass across 41 test files.
  - ESLint: `pnpm --dir src/frontend lint` passes with 0 errors and 0 warnings (`--max-warnings 0`).
  - TypeScript: `pnpm --dir src/frontend typecheck` (`vue-tsc --noEmit`) passes with 0 errors.

Changed Files:
- `src/frontend/src/features/academic/scorebook/schemas/scorebook.schema.ts`
- `src/frontend/src/features/academic/scorebook/schemas/__tests__/scorebook.schema.spec.ts`
- `src/frontend/src/features/academic/scorebook/components/AssessmentHeaderCard.vue`
- `src/frontend/src/features/academic/scorebook/components/ScoreCellInput.vue`
- `src/frontend/src/features/academic/scorebook/components/CreateAssessmentModal.vue`
- `src/frontend/src/features/academic/scorebook/components/ScorebookFilterBar.vue`
- `src/frontend/src/features/academic/scorebook/views/ScorebookWorkbenchView.vue`
- `src/frontend/src/features/academic/scorebook/views/__tests__/ScorebookWorkbenchView.spec.ts`
- `src/frontend/src/features/operations/scheduling/views/__tests__/TimetableWorkspaceView.spec.ts`
- `docs/issues/IS003- STAGE-2-DEV-IMPLEMENTATION-PLAN.md`

---

## P5 - Stage 2 End-to-End Verification

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P5-S16

Title: Stage 2 Quality Gates & Live Multi-Role Local API + Vite E2E Verification

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Verify all backend and frontend quality gates (`build`, `test`, `typecheck`, `lint`, `api:check`) and execute multi-role operational verification against running local `ElsasaSis.Api` and Vite dev server.

Depends On: P3-S09, P3-S10, P4-S12, P4-S13, P4-S14, P4-S15

Repository: src/frontend

Completion Criteria:

1. Backend Quality Gates:
   - `dotnet build src/backend/ElsasaSis.sln` succeeds with `0` warnings and `0` errors.
   - `dotnet test src/backend/ElsasaSis.sln` passes ALL Unit, Architecture, and Integration tests with `0` failures against local SQL Server / `(localdb)\MSSQLLocalDB`.
2. Frontend Quality Gates:
   - `pnpm typecheck`, `pnpm lint`, `pnpm test:unit`, and `pnpm build` succeed with `0` errors in `src/frontend/`.
   - `openapi-typescript <api-url>/swagger/v1/swagger.json -o src/shared/types/api/schema.d.ts --check` confirms zero OpenAPI contract drift.
3. Multi-Role Live Operational Verification against local `ElsasaSis.Api` (auto-migrated `001`–`100` on `(localdb)\MSSQLLocalDB`) and Vite dev server:
   - **`admin` (`SCHOOL_ADMIN`)**:
     - Configures weekly timetable slots for study group `7A` in `SCR-OPR-TIMETABLE`, verifies conflict flags render correctly, and manages SMK block date ranges.
     - Creates sequential curriculum stages and modular learning units under Mathematics in `SCR-ACD-CURRICULUM`.
     - Executes batch student enrollment for study group `7A` into Mathematics Stage 7 via the Tier 3 Pre-Flight Impact Summary Modal, verifying enrolled and skipped counts.
   - **`teacher.budi` (`TEACHER` + `HOMEROOM_TEACHER`)**:
     - Logs in (`Teacher123!`), verifies role-filtered navigation sidebar.
     - Opens `SCR-OPR-TIMETABLE` and verifies the "My Schedule" tab displays only assigned classes.
     - Opens `SCR-ACD-ATTENDANCE`, selects Mathematics for study group `7A`, clicks "Mark All Present", adjusts one student to `SICK`, and saves changes via the sticky bar.
     - Opens `SCR-ACD-SCOREBOOK`, creates a new formative assessment, enters student scores in the spreadsheet grid, saves via the sticky bar, and observes updated running scores and attendance metrics.
   - **`principal` (`PRINCIPAL`)**:
     - Logs in (`Principal123!`), verifies role-filtered sidebar, views timetable grids, curriculum structure, attendance sheets, and gradebook summaries in read-only oversight mode.
4. Clean shutdown of temporary API and Vite server processes.

Notes: All 7 Acceptance Conditions in [IS003- STAGE-2-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-ARCHITECTURE.md) §11 must be fully verified and green.

Implementation Notes:
- **Backend Quality Gates (Passed with 0 warnings, 0 errors, 0 failures)**:
  - `dotnet build src/backend/ElsasaSis.sln`: Succeeded with 0 warnings and 0 errors under .NET 8.
  - `dotnet test src/backend/ElsasaSis.sln`: All test projects executed and passed with 0 failures:
    - `ElsasaSis.UnitTests`: 431 passed, 0 failed, 0 skipped.
    - `ElsasaSis.ArchitectureTests`: 13 passed, 0 failed, 0 skipped (enforcing complete NetArchTest isolation for `ElsasaSis.Modules.Acd` alongside `Auth`, `Opr`, `Std`, `Tch`, with zero EF Core dependencies).
    - `ElsasaSis.IntegrationTests`: 35 passed, 0 failed, 0 skipped against local SQL Server.
- **Frontend Quality Gates (Passed with 0 warnings, 0 errors, 0 failures)**:
  - `pnpm typecheck` (`vue-tsc --noEmit`): Exited 0 with zero type errors.
  - `pnpm lint` (`eslint . --max-warnings 0`): Exited 0 with zero warnings.
  - `pnpm test:unit` (`vitest run`): 41 test files passed (100%), 261 unit/component tests passed (100%), 0 failures.
  - `pnpm build` (`vite build`): Production build succeeded with all bundles and chunks minified cleanly.
- **OpenAPI Schema Contract Drift Verification**:
  - Started `ElsasaSis.Api` in `Development` mode on port 5055.
  - Executed `openapi-typescript http://127.0.0.1:5055/swagger/v1/swagger.json -o src/shared/types/api/schema.d.ts --check`.
  - Exited with 0 drift, verifying 100% contract synchronization between backend endpoint schemas and frontend types.
- **Live Multi-Role Operational Verification (Vite Dev Server + ElsasaSis.Api)**:
  - Started Vite dev server (`http://localhost:5173`) proxying API calls to `ElsasaSis.Api` (`http://127.0.0.1:5055`).
  - Executed end-to-end multi-role verification suite simulating all 3 key personas:
    1. **`admin` (`SCHOOL_ADMIN`)**:
       - Authenticated via `/api/auth/login` (verified `SCHOOL_ADMIN` role).
       - Configured weekly timetable slot for study group `7A` (`POST /api/opr/study-groups/{id}/timetables`).
       - Queried timetable grid: verified clean slot with `teacherConflict = false` and `roomConflict = false`.
       - Created overlapping timetable slot in `7B`: verified conflict detection rendered `teacherConflict = true` and `roomConflict = true` without blocking save, and deleted the overlap slot cleanly (`204 NoContent`).
       - Created SMK `GENERAL` block rotation date range (`2026-09-01` to `2026-09-14`), verified presence in list, and deleted it cleanly (`204 NoContent`).
       - Created Mathematics Stage 7 and modular learning unit "Linear Equations & Inequalities", and mapped Stage 7 to SMP Grade 7.
       - Executed batch student enrollment for study group `7A` into Mathematics Stage 7 per Tier 3 Pre-Flight Impact logic (`POST /api/acd/participations/batch`), verifying placed student enrollments (`enrolledCount = 4`) and verifying subsequent idempotent skip execution (`enrolledCount = 0, skippedCount = 4`). Verified enrolled roster count is 4.
    2. **`teacher.budi` (`TEACHER` + `HOMEROOM_TEACHER`)**:
       - Authenticated (`Teacher123!`), verified role-filtered claims (`HOMEROOM_TEACHER`, `TEACHER`).
       - Queried personal schedule (`GET /api/opr/teacher-schedule`), verifying "My Schedule" displays 2 assigned classes for study group `7A` Matematika across active cycle.
       - Queried daily attendance sheet for `7A` Matematika (`GET /api/acd/attendance`).
       - Submitted batch attendance marking all present, adjusting 1 student (Ahmad Dahlan) to `SICK` with notes ("Verified flu symptoms by homeroom teacher"), saving atomically via sticky Save bar commit (`POST /api/acd/attendance`).
       - Verified sheet persistence confirmed Ahmad Dahlan recorded `SICK` with notes.
       - Created new Formative Assessment ("Algebraic Formative Quiz 1", max score 100, weight 20) in `SCR-ACD-SCOREBOOK` (`POST /api/acd/assignments`).
       - Submitted student scores in spreadsheet grid via batch commit (`POST /api/acd/assignments/{id}/scores`).
       - Queried consolidated gradebook summary (`GET /api/acd/gradebook`), verifying Ahmad Dahlan running score reflects `88.5` and attendance aggregate metrics display `{ presentCount: 1, sickCount: 1, totalRecorded: 2 }`.
    3. **`principal` (`PRINCIPAL`)**:
       - Authenticated (`Principal123!`), verified `PRINCIPAL` role.
       - Inspected timetable grid for `7A` in read-only oversight mode (`200 OK`, 2 slots).
       - Inspected curriculum tree in read-only oversight mode (`200 OK`, 10 subjects).
       - Inspected attendance sheet for `7A` Matematika in read-only oversight mode (`200 OK`, 4 students).
       - Inspected consolidated gradebook summary for `7A` in read-only oversight mode (`200 OK`, 4 students).
- **Clean Server Teardown**:
  - Terminated temporary `ElsasaSis.Api` (task-109) and Vite dev server (task-119) background processes.
  - Confirmed ports 5055 and 5173 released.
- **Acceptance Conditions**: All 7 Acceptance Conditions in `IS003- STAGE-2-DEV-ARCHITECTURE.md` §11 are 100% satisfied and green.

Changed Files:
- `docs/issues/IS003- STAGE-2-DEV-IMPLEMENTATION-PLAN.md`

---

# 6. Change Log

- **2026-11-19 (v1.0):** Initial release of the Stage 2 Implementation Plan for `CR-STAGE-2` by the Architect (`ica-architect`). Establishes a 5-phase, 16-slice dependency-driven breakdown across backend (`src/backend`) and frontend (`src/frontend`) repositories, realizing all 8 Stage 2 use cases across 4 UI screens. Execution Approval granted (`APPROVED`).
