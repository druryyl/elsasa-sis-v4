---
Title: Stage 2 Timetables & Daily Academic Operations — Target Architecture
Code: CR-STAGE-2
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-19
---

# 1. Overview

This document defines the authoritative target architecture for **CR-STAGE-2** (**Stage 2: Timetables & Daily Academic Operations**) of **ELSASA-SIS v4 (Basic Level)**, realizing the change request captured in [IS003- STAGE-2-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-ISSUE.md) and resolving all decisions established in the Feasibility Assessment [IS003- STAGE-2-DEV-FEASIBILITY.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-FEASIBILITY.md).

Building directly upon the foundational infrastructure of Stage 0 (monorepo, authentication, audit, academic years) and the institutional master data established in Stage 1 (rooms, calendars, announcements, students with NIS, study groups, student placements, teacher profiles, and teaching assignments), Stage 2 activates the core daily instructional operations across two bounded contexts:

1. **School Operational Setup (`opr`):** Master weekly scheduling for study groups, period configuration, classroom/facility allocation, teacher assignment linking, SMK vocational block rotation templates (General/Vocational alternate-week cycles), and personalized teacher schedule views (`UC-ADM-04`, `UC-TCH-10`).
2. **Academic Structure & Operations (`acd`):** Formal subject curriculum hierarchy (subjects, sequential stages, learning units, and grade mapping scales) (`UC-ADM-17`), explicit batch academic enrollment of placed students into subject stages (`UC-ADM-18`), daily classroom attendance tracking with bulk shortcuts (`UC-TCH-01`), classroom assessment creation and batch score entry (`UC-TCH-02`, `UC-TCH-03`), and consolidated gradebook summaries with running score calculations and attendance metrics (`UC-TCH-05`).

Stage 2 realizes exactly **8 actor-driven use cases** across **4 interactive UI screens** (`SCR-OPR-TIMETABLE`, `SCR-ACD-CURRICULUM`, `SCR-ACD-ATTENDANCE`, and `SCR-ACD-SCOREBOOK`), introducing 1 new backend module assembly (`ElsasaSis.Modules.Acd`), expanding `ElsasaSis.Modules.Opr`, and introducing 3 new database migrations defining 11 relational tables.

---

# 2. Architectural Basis

## Business Context

Business concepts, domain invariants, actors, use cases, and screen ergonomics realized by Stage 2 are governed by the upstream product-definition specifications:

- **Domain Knowledge & Invariants:**
  - [operational-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/operational-domain-basic.md) (`StudyGroupSchedule`, `StudyGroupTimetable`, `BR-OPS-004`..`BR-OPS-006`).
  - [academic-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/academic-domain-basic.md) (`Subject`, `SubjectStage`, `LearningUnit`, `AcademicParticipation`, `AttendanceRecord`, `Assignment`, `AssignmentScore`, `BR-LSTR-001`..`BR-LSTR-005`, `BR-APRT-001`..`BR-APRT-003`, `BR-ATTN-001`..`BR-ATTN-003`, `BR-ASGN-001`..`BR-ASGN-006`).
  - [student-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/student-domain-basic.md) (`StudyGroup`, `StudentPlacement`).
  - [teaching-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/teaching-domain-basic.md) (`Teacher`, `TeacherAssignment`).
- **Actors & Roles:** [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md) (`SCHOOL_ADMIN`, `TEACHER`, `HOMEROOM_TEACHER`, `PRINCIPAL`).
- **Use Case Taxonomy:** [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (Use cases `UC-ADM-04`, `UC-ADM-17`, `UC-ADM-18`, `UC-TCH-01`, `UC-TCH-02`, `UC-TCH-03`, `UC-TCH-05`, `UC-TCH-10`).
- **Screens & UI/UX Standards:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) and [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md) (§2.5 Navigation Taxonomy, §3 Screen Patterns, §6 Interaction Patterns, §8 Feedback & System States).

## Analysis Input

All architectural decisions (`D-01` through `D-20`) were formally evaluated and closed in the Feasibility Assessment:

- **Feasibility Assessment:** [IS003- STAGE-2-DEV-FEASIBILITY.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-FEASIBILITY.md) (`CR-STAGE-2`).
- **Base Architecture & Technical Standards:**
  - [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md) (Modular Monolith, Schemas, Screen Inventory).
  - [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md) (Modular Monolith, VSA, Clean DDD, Dapper Pure Persistence, RFC 7807).
  - [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md) (Vue 3, TanStack Query, Pinia, VeeValidate + Zod, PrimeVue Aura).
  - [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md) (§3.5 `study_group_schedules`, §3.6 `study_group_timetables`).
  - [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md) (§3.1 `subjects`, §3.2 `subject_stages`, §3.3 `learning_units`, §3.4 `subject_stage_grades`, §3.5 `academic_participations`, §3.6 `attendance_records`, §3.7 `assignments`, §3.8 `assignment_scores`, §3.9 `report_cards`, §3.10 `report_card_entries`).

```text
DOMAINS + ROADMAP + GUIDELINES (docs/architecture/)
                      +
         IS003 FEASIBILITY-ASSESSMENT
                      ↓
           CR-STAGE-2-ARCHITECTURE
```

---

# 3. Scope

## Included

Stage 2 realizes exactly **8 actor-driven use cases** across **4 UI screens** and supporting backend assemblies:

1. **New Backend Module Assembly (`ElsasaSis.Modules.Acd`):**
   - New C# class library at `src/backend/src/Modules/ElsasaSis.Modules.Acd/` (`Domain/`, `Infrastructure/`, `Features/`).
   - Domain Aggregate Roots and Entities: `Subject`, `SubjectStage` (with child `LearningUnit` and `SubjectStageGrade`), `AcademicParticipation`, `AttendanceRecord`, `Assignment` (with child `AssignmentScore`).
   - Vertical slices covering:
     - Learning Structure (`ManageLearningStructure` — `UC-ADM-17`): Subjects, stages, learning units, and administrative grade mappings.
     - Academic Participations (`EnrollStudentsInSubjectStages` — `UC-ADM-18`): Explicit batch enrollment.
     - Daily Attendance (`RecordSubjectAttendance`, `GetSubjectAttendanceSheet` — `UC-TCH-01`).
     - Assessments & Scorebook (`CreateClassroomAssignment`, `EnterStudentScores`, `GetSubjectGradebookSummary` — `UC-TCH-02`, `UC-TCH-03`, `UC-TCH-05`).
   - Integration into `src/backend/ElsasaSis.sln`, `ElsasaSis.Api`, and `ElsasaSis.ArchitectureTests`.
2. **Expansion of `ElsasaSis.Modules.Opr`:**
   - Domain Aggregate Roots and Entities: `StudyGroupSchedule`, `StudyGroupTimetable`.
   - Vertical slices covering:
     - Timetable Scheduling (`ConfigureStudyGroupTimetables` — `UC-ADM-04`): Slot configuration, removal, and SMK block date mapping.
     - Timetable Queries: Study group weekly schedule query with read-side conflict detection flags, and teacher personal weekly schedule (`GetTeacherPersonalSchedule` — `UC-TCH-10`).
3. **Database Migrations (`ElsasaSis.Database`):**
   - `013_CreateOprTimetableTables.sql`: `opr.study_group_schedules` and `opr.study_group_timetables`.
   - `031_CreateAcdTables.sql`: All remaining 9 `acd` tables (`acd.subject_stages`, `acd.learning_units`, `acd.subject_stage_grades`, `acd.academic_participations`, `acd.attendance_records`, `acd.assignments`, `acd.assignment_scores`, `acd.report_cards`, `acd.report_card_entries`).
   - `100_SeedStage2Data.sql`: Deterministic Stage 2 seed data (curriculum structure, timetable slots, student participations, sample attendance, and sample scorebook assignments).
4. **Cross-Module Contracts & Interface Support:**
   - `ElsasaSis.Contracts`: Maintain `IAcademicContract` and ensure cross-module query contracts (`IOperationalContract`, `IStudentContract`, `ITeachingContract`) support write-path validation.
5. **Frontend Screens & Workspaces (4 Screens):**
   - `SCR-OPR-TIMETABLE` (`TimetableWorkspaceView.vue` at `/operations/timetables`):
     - Schedule Management pattern (Pattern 4): Weekly matrix (days $\times$ period slots), study group picker, schedule type filter (`REGULAR`, `GENERAL`, `VOCATIONAL`), Tier 3 right-anchored slot assignment drawer, SMK block date range management drawer, inline `rose-600` conflict indicator highlights, and personalized teacher schedule tab (`UC-ADM-04`, `UC-TCH-10`).
   - `SCR-ACD-CURRICULUM` (`CurriculumSetupView.vue` at `/academic/curriculum`):
     - Planning pattern (Pattern 5): Hierarchical 3-level tree (`Subject` $\rightarrow$ `SubjectStage` $\rightarrow$ `LearningUnit`), administrative grade mapping canvas (`(school_level, grade)`), and explicit batch student enrollment workflow with Tier 3 Pre-Flight Impact Summary Modal (`UC-ADM-17`, `UC-ADM-18`).
   - `SCR-ACD-ATTENDANCE` (`AttendanceWorkbenchView.vue` at `/academic/attendance`):
     - Work Queue pattern (Pattern 1): Subject + Study Group session selector, daily calendar date picker, student roster grid, quick "Mark All Present" shortcut, attendance status chips (`PRESENT`, `ABSENT`, `EXCUSED`, `SICK`, `LATE`), dirty-state tracking, sticky Save bar, and route exit guards (`UC-TCH-01`).
   - `SCR-ACD-SCOREBOOK` (`ScorebookWorkbenchView.vue` at `/academic/scorebook`):
     - Work Queue pattern (Pattern 1): Subject Stage + Study Group selector, assessment header cards (`FORMATIVE`, `SUMMATIVE`, `EXAM`), Tier 2 assignment creation modal, Tier 1 inline spreadsheet grid for scores and feedback, keyboard navigation (`Enter`, arrow keys), visual passing threshold highlight, running score computation, and attendance summary columns (`UC-TCH-02`, `UC-TCH-03`, `UC-TCH-05`).
6. **Authorization & Security:**
   - Compile-time permissions added in `ElsasaSis.Modules.Auth`:
     - Operations: `Permissions.Operations.Timetables.View`, `Permissions.Operations.Timetables.Manage`, `Permissions.Operations.Timetables.ViewOwn`.
     - Academic: `Permissions.Academic.Curriculum.View`, `Permissions.Academic.Curriculum.Manage`, `Permissions.Academic.Attendance.View`, `Permissions.Academic.Attendance.Record`, `Permissions.Academic.Scorebook.View`, `Permissions.Academic.Scorebook.Manage`.
   - Dynamic navigation menu filtering in `AppLayout.vue` and route guards enforcing role permissions (`SCHOOL_ADMIN`, `TEACHER`, `HOMEROOM_TEACHER`, `PRINCIPAL`).
   - Personal schedule scoping: `teacher_id = CurrentUser.TeacherId` on `UC-TCH-10`.

## Excluded

The following features and components are strictly excluded from Stage 2 and deferred to Stage 3 or later:

- **Report Cards (`SCR-ACD-REPORT-CARDS`):** All report card generation, review, entry editing, finalization, and PDF generation use cases (`UC-ADM-19`, `UC-ADM-20`, `UC-TCH-04`, `UC-HRM-01`, `UC-HRM-02`, `UC-HRM-03`, `UC-PRC-02`) are deferred to Stage 3. The tables `acd.report_cards` and `acd.report_card_entries` are migrated for DDL completeness only.
- **Academic Progress Monitoring (`SCR-ACD-MONITORING`):** School-wide executive monitoring dashboards (`UC-ADM-21`, `UC-PRC-03`) are deferred to Stage 3.
- **Automated Placement Enrollment Subscriber:** `StudentPlacedDomainEvent` will not trigger auto-enrollment into `acd.academic_participations` in Stage 2 (D-01: explicit admin action only).
- **Algorithmic Timetable Conflict Blocking:** The write path will not block timetable slot saves when teacher or room conflicts exist. Conflicts are detected and flagged on read only (D-10).
- **Enforcement of 100% Assessment Weight Sum (`BR-ASGN-006`):** Handlers validate individual assessment weights (`0.00` to `100.00`), but cross-assessment sum validation is deferred to Stage 3 report card compilation (D-18).
- **Finance and Pastoral Domains:** Deferred to Stages 3 and 4 (`fin.*`, `dev.*`).

---

# 4. Technical Decisions

This section realizes the 20 approved analysis decisions (`D-01` through `D-20`) as concrete architectural specifications:

### 4.1 Module Assembly & Clean DDD Boundary (`D-12`, `D-14`)
- Create `ElsasaSis.Modules.Acd` targeting `net8.0` in `src/backend/src/Modules/ElsasaSis.Modules.Acd/`.
- Maintain strict 3-folder anatomy:
  - `Domain/`: Aggregate Roots (`Subject`, `SubjectStage`, `AcademicParticipation`, `AttendanceRecord`, `Assignment`), Entities (`LearningUnit`, `SubjectStageGrade`, `AssignmentScore`), Repository Interfaces, Domain Events, Domain Errors. Zero external dependencies.
  - `Infrastructure/Persistence/`: Dapper implementations of repositories, SQL queries.
  - `Features/`: Vertical Slices organized by feature area (`LearningStructure/`, `Participations/`, `Attendance/`, `Scorebook/`).
- Module references strictly `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`. NetArchTest asserts zero references between `Modules.Acd` and `Auth`, `Opr`, `Std`, `Tch`, `Api`, or `Database`.
- Update `ElsasaSis.ArchitectureTests`: Register `AcdModuleAssembly` in `AssemblyArchitectureContext` and verify boundary rules.

### 4.2 Timetable Scheduling, Read-Side Conflicts & SMK Block Cycles (`D-09`, `D-10`, `D-11`)
- Timetable models added to `ElsasaSis.Modules.Opr`:
  - `StudyGroupSchedule`: Scoped to `(operational_period_id, study_group_id, schedule_type, day_of_week, period_slot)`.
  - `StudyGroupTimetable`: Maps `(operational_period_id, study_group_id)` to `GENERAL` or `VOCATIONAL` schedule type for a specific date range (`start_date` to `end_date`).
- **Read-Side Conflict Flags:** The query handler for `GetStudyGroupTimetable` evaluates timetable entries in SQL and computes boolean conflict flags:
  - `teacherConflict = TRUE` if the assigned `teacher_id` is scheduled in another study group for the same operational period, day, time slot, and schedule type.
  - `roomConflict = TRUE` if the assigned `room_id` is scheduled in another study group for the same operational period, day, time slot, and schedule type.
  - Saves are **not blocked** on conflict (D-10). Conflicts render as `rose-600` cell highlights with explanatory tooltips.
- **Teacher Personal Schedule:** `GET /api/opr/teacher-schedule` returns the weekly template for the authenticated teacher (`ICurrentUser.TeacherId`). Resolving which SMK block is active on a given calendar day is calculated client-side by matching the current date against `study_group_timetables` date ranges (D-11).

### 4.3 Curriculum Hierarchy & Multi-Slice Endpoints (`D-13`)
- `UC-ADM-17` is decomposed into granular, decoupled vertical slices under `ElsasaSis.Modules.Acd/Features/LearningStructure/`:
  - `CreateSubject` (`POST /api/acd/subjects`) & `UpdateSubject` (`PUT /api/acd/subjects/{id}`)
  - `CreateSubjectStage` (`POST /api/acd/subjects/{id}/stages`) & `UpdateSubjectStage` (`PUT /api/acd/subjects/{id}/stages/{stageId}`)
  - `CreateLearningUnit` (`POST /api/acd/subjects/{id}/stages/{stageId}/units`) & `UpdateLearningUnit` (`PUT /api/acd/subjects/{id}/stages/{stageId}/units/{unitId}`)
  - `MapSubjectStageGrade` (`POST /api/acd/subjects/{id}/stages/{stageId}/grade-mappings`) & `RemoveSubjectStageGrade` (`DELETE /api/acd/subjects/{id}/stages/{stageId}/grade-mappings/{mappingId}`)
  - `GetLearningStructure` (`GET /api/acd/subjects`): Hierarchical query returning subjects with nested stages, learning units, and grade mappings.
- Authorized exclusively for `SCHOOL_ADMIN` (`Permissions.Academic.Curriculum.Manage`).

### 4.4 Explicit Batch Academic Participation Creation (`D-01`, `D-17`)
- `POST /api/acd/participations/batch` is the **sole** mechanism for creating student subject stage enrollments in Stage 2.
- Request payload: `{ operationalPeriodId: Guid, subjectStageId: Guid, studyGroupId: Guid }`.
- Execution pipeline:
  1. Validate `operationalPeriodId` is active via `IOperationalContract`.
  2. Validate `subjectStageId` exists via `ISubjectStageRepository`.
  3. Query `std.student_placements` for all students with active placement in `studyGroupId` for the current academic year (cross-schema Dapper read).
  4. Perform bulk insert into `acd.academic_participations` with display snapshots (`student_name`, `student_nis`, `study_group_name`, `subject_name`, `subject_stage_name`).
  5. Idempotent skip: Silently skip students already enrolled per `UQ_academic_participations_student_stage_period`.
  6. Return `{ enrolledCount: int, skippedCount: int }`.
- UI requires a **Tier 3 Pre-Flight Impact Summary Modal** before dispatching the batch enrollment command.

### 4.5 Daily Attendance Architecture: Subject Scope & Batch Upsert (`D-02`, `D-08`)
- Attendance is recorded at the **Subject** level (`subject_id`), not the Stage level, matching the timetable session structure.
- Invariant guard: Database unique constraint `UQ_attendance_records_student_subject_date` enforces at most one attendance record per student per subject per calendar day.
- `POST /api/acd/attendance` accepts a batch payload:
  ```json
  {
    "subjectId": "UUID",
    "studyGroupId": "UUID",
    "attendanceDate": "YYYY-MM-DD",
    "items": [
      { "studentId": "UUID", "status": "PRESENT", "notes": null }
    ]
  }
  ```
- Semantics: **Upsert** in a single database transaction.
- Read Slice `GetSubjectAttendanceSheet` returns the roster of students enrolled in that subject and study group alongside any existing attendance records for the target date.
- UI: Work Queue pattern with "Mark All Present" button, dirty-cell tracking, sticky Save bar, and `onBeforeRouteLeave` confirmation.

### 4.6 Classroom Assessment & Scorebook Architecture (`D-03`, `D-04`, `D-06`, `D-07`)
- Scorebook is scoped to `(subject_stage_id, study_group_id, operational_period_id)`.
- Roster: All students with `ACTIVE` participation in that `(subject_stage_id, study_group_id)`.
- `acd.assignments.study_group_id` is nullable in schema but **required in the API request** for Stage 2 (`D-06`).
- **Authorization Check (`D-04`):** When a teacher creates an assignment or enters scores, the handler verifies that the teacher has a valid teaching assignment in `tch.teacher_assignments` for `(teacher_id, study_group_id, subject_id, operational_period_id)`.
- `POST /api/acd/assignments/{id}/scores` accepts a batch array of `{ studentId: Guid, score: decimal, feedback: string? }`.
- Semantics: **Upsert** per `UQ_assignment_scores_assignment_student`.
- UI: Spreadsheet grid with dirty-cell tracking, sticky Save bar, and keyboard navigation (`Enter`, arrow keys).

### 4.7 Consolidated Gradebook Projection: SQL-Only Weighted Computation (`D-05`, `D-18`, `D-20`)
- `GET /api/acd/gradebook?subjectStageId=&studyGroupId=&operationalPeriodId=` executes a single optimized Dapper query.
- **Running Score Formula (`D-05`, `D-18`):**
  - If all assessments for the stage and group have `weight IS NOT NULL`:
    $$\text{Running Score} = \frac{\sum (\text{score} \times \text{weight})}{\sum \text{weight}}$$
  - If any assessment has `weight IS NULL`:
    $$\text{Running Score} = \text{AVG}(\text{score})$$
  - Handled cleanly in SQL via conditional aggregation without stored computed columns or cache tables.
- **Attendance Metrics (`D-20`):** Single query aggregates attendance counts (`presentCount`, `absentCount`, `lateCount`, `excusedCount`, `sickCount`) for the current operational period alongside assignment scores.
- **Passing Threshold:** Pure client-side filter/highlighting parameter. The backend returns raw numbers; the frontend highlights failing scores in rose.

### 4.8 Database Migration Strategy & Table Numbering (`D-15`, `D-19`)
- `013_CreateOprTimetableTables.sql`: Creates `opr.study_group_schedules` and `opr.study_group_timetables`.
- `031_CreateAcdTables.sql`: Creates all remaining 9 `acd` tables (`subject_stages`, `learning_units`, `subject_stage_grades`, `academic_participations`, `attendance_records`, `assignments`, `assignment_scores`, `report_cards`, `report_card_entries`).
- `100_SeedStage2Data.sql`: Deterministic Stage 2 seed data for local development and QA testing.
- All tables adhere strictly to Pure Persistence Layer standards: UUID v7 primary keys, UTC application-supplied timestamps, zero database foreign keys, zero triggers/defaults/checks, and application-level snapshot denormalization.

### 4.9 Frontend Structure & Navigation Integration (`D-16`)
- Submodules structured under `src/frontend/src/features/`:
  - `features/operations/scheduling/` (`SCR-OPR-TIMETABLE`, route `/operations/timetables`)
  - `features/academic/curriculum/` (`SCR-ACD-CURRICULUM`, route `/academic/curriculum`)
  - `features/academic/attendance/` (`SCR-ACD-ATTENDANCE`, route `/academic/attendance`)
  - `features/academic/scorebook/` (`SCR-ACD-SCOREBOOK`, route `/academic/scorebook`)
- Navigation integration in `AppLayout.vue`:
  - Group **Operations & Comms**: Add "Timetables & Scheduling".
  - Group **Academic & Curriculum**: Add "Curriculum & Learning Structure".
  - Group **Faculty & Teaching**: Add "Classroom Attendance" and "Teacher Scorebook".
- Role-based visibility: "Hide by Role, Disable by State" applied across all views.

---

# 5. Component Responsibilities

| Component | Assembly / Path | Responsibility & Ownership |
|---|---|---|
| **`BND-MOD-ACD`** | `ElsasaSis.Modules.Acd` | Owns `Subject`, `SubjectStage` (with `LearningUnit`, `SubjectStageGrade`), `AcademicParticipation`, `AttendanceRecord`, and `Assignment` (with `AssignmentScore`) Aggregate Roots, repositories, and vertical slices for `UC-ADM-17`, `UC-ADM-18`, `UC-TCH-01`, `UC-TCH-02`, `UC-TCH-03`, `UC-TCH-05`. |
| **`BND-MOD-OPR`** | `ElsasaSis.Modules.Opr` | Owns `StudyGroupSchedule` and `StudyGroupTimetable` Aggregate Roots, repositories, read-side conflict detection projection, and vertical slices for `UC-ADM-04`, `UC-TCH-10`. |
| **`BND-CONTRACTS`** | `ElsasaSis.Contracts` | Owns cross-module interfaces (`IAcademicContract`, `IOperationalContract`, `IStudentContract`, `ITeachingContract`) and shared DTOs. |
| **`BND-DATABASE`** | `ElsasaSis.Database` | Owns Stage 2 migration scripts (`013_CreateOprTimetableTables.sql`, `031_CreateAcdTables.sql`, `100_SeedStage2Data.sql`) and automated DbUp execution. |
| **`BND-API-HOST`** | `ElsasaSis.Api` | Registers `Modules.Acd` DI container, maps Minimal API endpoints, configures authorization policies, and exposes Swagger documentation. |
| **`ElsasaSis.ArchitectureTests`** | Tests | Enforces NetArchTest boundary isolation across `Auth`, `Opr`, `Std`, `Tch`, and `Acd`, verifying zero inter-module assembly references and EF Core prohibition. |
| **`BND-FE-OPR-SCHED`** | `features/operations/scheduling/` | Owns `SCR-OPR-TIMETABLE` (`TimetableWorkspaceView.vue`): Weekly timetable grid, Tier 3 slot assignment drawer, SMK block management drawer, conflict badges, and teacher personal schedule tab. |
| **`BND-FE-ACD-CURR`** | `features/academic/curriculum/` | Owns `SCR-ACD-CURRICULUM` (`CurriculumSetupView.vue`): 3-level learning structure tree, grade mapping canvas, and batch participation enrollment with Pre-Flight Modal. |
| **`BND-FE-ACD-ATTN`** | `features/academic/attendance/` | Owns `SCR-ACD-ATTENDANCE` (`AttendanceWorkbenchView.vue`): Subject class session selector, date picker, attendance grid with quick-mark chips, dirty-state guard, and sticky Save bar. |
| **`BND-FE-ACD-SCBK`** | `features/academic/scorebook/` | Owns `SCR-ACD-SCOREBOOK` (`ScorebookWorkbenchView.vue`): Assessment creation drawer/modal, spreadsheet score entry grid, keyboard navigation, running grade calculation, and attendance metrics summary. |
| **`BND-FE-CORE`** | `src/frontend/src/app/` | Owns Vue Router route registrations, permission guards, role-filtered sidebar groups in `AppLayout.vue`, and global context synchronization. |

---

# 6. Integration Design

| Source | Target | Transport | Purpose |
|---|---|---|---|
| `BND-FE-OPR-SCHED` | `BND-API-HOST` (`/api/opr/*`) | HTTP REST (JSON) | Manage timetable slots (`/study-groups/{id}/timetables`), SMK blocks (`/study-groups/{id}/blocks`), and fetch teacher personal schedule (`/teacher-schedule`). |
| `BND-FE-ACD-CURR` | `BND-API-HOST` (`/api/acd/*`) | HTTP REST (JSON) | Manage subjects, stages, learning units, grade mappings (`/subjects/*`), and batch enroll placed students (`/participations/batch`). |
| `BND-FE-ACD-ATTN` | `BND-API-HOST` (`/api/acd/*`) | HTTP REST (JSON) | Fetch daily subject roster and attendance (`GET /attendance`), and batch upsert attendance records (`POST /attendance`). |
| `BND-FE-ACD-SCBK` | `BND-API-HOST` (`/api/acd/*`) | HTTP REST (JSON) | Create assignments (`POST /assignments`), batch upsert scores (`POST /assignments/{id}/scores`), and fetch consolidated gradebook (`GET /gradebook`). |
| `BND-MOD-ACD` (Batch Enroll) | `std.student_placements` | SQL Query (Dapper) | Fetch placed students in study group for batch participation creation. |
| `BND-MOD-ACD` (Auth Check) | `tch.teacher_assignments` | SQL Query (Dapper) | Validate teacher is assigned to subject + study group before allowing assessment creation or score entry. |
| `BND-MOD-OPR` (Conflict Check) | `opr.study_group_schedules` | SQL Query (Dapper) | Evaluate teacher and room overlapping time slots for read-side conflict flags. |
| Read Slices (`Dapper`) | SQL Server (`opr`, `acd`, `std`, `tch`) | Direct SQL (`SELECT`) | Direct cross-schema read projections for gradebook summary, attendance rosters, and timetable grids. |

---

# 7. Data Ownership

| Schema / Table | Owning Module | Persistence Responsibility |
|---|---|---|
| `opr.study_group_schedules` | `ElsasaSis.Modules.Opr` | Exclusive write ownership via `IStudyGroupScheduleRepository`. |
| `opr.study_group_timetables` | `ElsasaSis.Modules.Opr` | Exclusive write ownership via `IStudyGroupTimetableRepository`. |
| `acd.subjects` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `ISubjectRepository` (seeded in Stage 1, managed in Stage 2). |
| `acd.subject_stages` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `ISubjectStageRepository`. |
| `acd.learning_units` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `ISubjectStageRepository` (owned child). |
| `acd.subject_stage_grades` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `ISubjectStageRepository` (owned mapping). |
| `acd.academic_participations` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `IAcademicParticipationRepository`. |
| `acd.attendance_records` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `IAttendanceRecordRepository`. |
| `acd.assignments` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `IAssignmentRepository`. |
| `acd.assignment_scores` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `IAssignmentRepository` (owned child). |
| `acd.report_cards` | `ElsasaSis.Modules.Acd` | Schema created in Stage 2; C# write ownership deferred to Stage 3. |
| `acd.report_card_entries` | `ElsasaSis.Modules.Acd` | Schema created in Stage 2; C# write ownership deferred to Stage 3. |

---

# 8. Database Design

Detailed table schemas follow Pure Persistence Layer standards: UUID v7 primary keys, application-supplied UTC `created_at` and `updated_at`, zero foreign key constraints, zero triggers, zero database defaults, and denormalized snapshots for cross-domain display strings.

## New Tables

| Table | Schema | Script | Purpose & Key Columns |
|---|---|---|---|
| `opr.study_group_schedules` | `opr` | `013` | Period-scoped study group weekly timetable entries. PK: `id`, `operational_period_id`, `study_group_id`, `study_group_name`, `schedule_type` (`REGULAR` \| `GENERAL` \| `VOCATIONAL`), `day_of_week` (INT 1-7), `period_slot` (INT), `start_time` (TIME(0)), `end_time` (TIME(0)), `subject_id`, `subject_name`, `teacher_id`, `teacher_name`, `room_id` (NULL), `room_name` (NULL). UQ: `(operational_period_id, study_group_id, schedule_type, day_of_week, period_slot)`. |
| `opr.study_group_timetables` | `opr` | `013` | SMK block rotation date range mappings. PK: `id`, `operational_period_id`, `study_group_id`, `study_group_name`, `schedule_type` (`GENERAL` \| `VOCATIONAL`), `start_date` (DATE), `end_date` (DATE). UQ: `(operational_period_id, study_group_id, start_date)`. |
| `acd.subject_stages` | `acd` | `031` | Curriculum stages/levels within a subject. PK: `id`, `subject_id`, `subject_name`, `name`, `sequence_order` (INT), `description` (NULL). UQ: `(subject_id, name)`, UQ: `(subject_id, sequence_order)`. |
| `acd.learning_units` | `acd` | `031` | Modular units within a subject stage. PK: `id`, `subject_stage_id`, `name`, `sequence_order` (INT), `unit_type` (`MODULE` \| `PACE` \| `LESSON` \| `PRESENTATION` \| `BOOK` \| `OTHER`), `description` (NULL). UQ: `(subject_stage_id, name)`, UQ: `(subject_stage_id, sequence_order)`. |
| `acd.subject_stage_grades` | `acd` | `031` | Mapping between subject stages and administrative grades. PK: `id`, `subject_stage_id`, `school_level` (`SD` \| `SMP` \| `SMA` \| `SMK`), `grade` (`1`..`12`). UQ: `(subject_stage_id, school_level, grade)`. |
| `acd.academic_participations` | `acd` | `031` | Student subject stage enrollment for a semester. PK: `id`, `student_id`, `student_name`, `student_nis`, `subject_stage_id`, `subject_stage_name`, `subject_id`, `subject_name`, `operational_period_id`, `operational_period_name`, `academic_year_id`, `academic_year_name`, `study_group_id`, `study_group_name`, `status` (`ACTIVE` \| `COMPLETED`). UQ: `(student_id, subject_stage_id, operational_period_id)`. |
| `acd.attendance_records` | `acd` | `031` | Daily student subject attendance. PK: `id`, `student_id`, `student_name`, `student_nis`, `subject_id`, `subject_name`, `study_group_id`, `study_group_name`, `operational_period_id`, `operational_period_name`, `attendance_date` (DATE), `status` (`PRESENT` \| `ABSENT` \| `EXCUSED` \| `SICK` \| `LATE`), `notes` (NULL), `recorded_by_teacher_id`, `recorded_by_teacher_name`. UQ: `(student_id, subject_id, attendance_date)`. |
| `acd.assignments` | `acd` | `031` | Graded classroom assignments/exams. PK: `id`, `subject_stage_id`, `subject_stage_name`, `subject_id`, `subject_name`, `learning_unit_id` (NULL), `learning_unit_name` (NULL), `operational_period_id`, `operational_period_name`, `study_group_id` (NULL in DDL, required in Stage 2), `study_group_name` (NULL), `title`, `assignment_type` (`FORMATIVE` \| `SUMMATIVE` \| `EXAM`), `max_score` (DECIMAL(5,2)), `weight` (DECIMAL(5,2) NULL), `assigned_date` (DATE), `due_date` (DATE NULL), `teacher_id`, `teacher_name`. |
| `acd.assignment_scores` | `acd` | `031` | Student numeric scores for assignments. PK: `id`, `assignment_id`, `student_id`, `student_name`, `student_nis`, `score` (DECIMAL(5,2)), `feedback` (NULL), `graded_at` (DATETIME2(3)). UQ: `(assignment_id, student_id)`. |
| `acd.report_cards` | `acd` | `031` | Semester report card headers (schema migrated, C# deferred to Stage 3). PK: `id`, `student_id`, `student_name`, `student_nis`, `student_nisn`, `academic_year_id`, `academic_year_name`, `operational_period_id`, `operational_period_name`, `study_group_id`, `study_group_name`, `homeroom_teacher_id`, `homeroom_teacher_name`, `status` (`DRAFT` \| `FINALIZED`), `finalized_at` (NULL). UQ: `(student_id, operational_period_id)`. |
| `acd.report_card_entries` | `acd` | `031` | Report card subject stage grade lines (schema migrated, C# deferred to Stage 3). PK: `id`, `report_card_id`, `subject_id`, `subject_name`, `subject_code`, `subject_stage_id`, `subject_stage_name`, `teacher_id`, `teacher_name`, `numeric_score`, `letter_grade`, `teacher_notes`. UQ: `(report_card_id, subject_stage_id)`. |

## Modified Tables

None. Stage 0 and Stage 1 tables (`auth.*`, `opr.academic_years`, `opr.operational_periods`, `opr.calendar_entries`, `opr.rooms`, `opr.announcements`, `std.*`, `tch.*`, `acd.subjects`) remain unchanged.

## Relationships

All inter-table relationships are logical and managed in the Application Layer:
- `opr.study_group_schedules.operational_period_id` $\rightarrow$ `opr.operational_periods.id`
- `opr.study_group_schedules.study_group_id` $\rightarrow$ `std.study_groups.id`
- `opr.study_group_schedules.subject_id` $\rightarrow$ `acd.subjects.id`
- `opr.study_group_schedules.teacher_id` $\rightarrow$ `tch.teachers.id`
- `opr.study_group_schedules.room_id` $\rightarrow$ `opr.rooms.id`
- `opr.study_group_timetables.study_group_id` $\rightarrow$ `std.study_groups.id`
- `acd.subject_stages.subject_id` $\rightarrow$ `acd.subjects.id`
- `acd.learning_units.subject_stage_id` $\rightarrow$ `acd.subject_stages.id`
- `acd.subject_stage_grades.subject_stage_id` $\rightarrow$ `acd.subject_stages.id`
- `acd.academic_participations.student_id` $\rightarrow$ `std.students.id`
- `acd.academic_participations.subject_stage_id` $\rightarrow$ `acd.subject_stages.id`
- `acd.academic_participations.operational_period_id` $\rightarrow$ `opr.operational_periods.id`
- `acd.attendance_records.student_id` $\rightarrow$ `std.students.id`
- `acd.attendance_records.subject_id` $\rightarrow$ `acd.subjects.id`
- `acd.attendance_records.recorded_by_teacher_id` $\rightarrow$ `tch.teachers.id`
- `acd.assignments.subject_stage_id` $\rightarrow$ `acd.subject_stages.id`
- `acd.assignments.learning_unit_id` $\rightarrow$ `acd.learning_units.id`
- `acd.assignments.study_group_id` $\rightarrow$ `std.study_groups.id`
- `acd.assignments.teacher_id` $\rightarrow$ `tch.teachers.id`
- `acd.assignment_scores.assignment_id` $\rightarrow$ `acd.assignments.id`
- `acd.assignment_scores.student_id` $\rightarrow$ `std.students.id`

## Migration Considerations

Scripts are authored as idempotent T-SQL executed via DbUp in `src/backend/src/ElsasaSis.Database/Scripts/`:
1. `013_CreateOprTimetableTables.sql`: Tables `opr.study_group_schedules`, `opr.study_group_timetables` with unique constraints and non-clustered performance indexes.
2. `031_CreateAcdTables.sql`: Tables `acd.subject_stages`, `acd.learning_units`, `acd.subject_stage_grades`, `acd.academic_participations`, `acd.attendance_records`, `acd.assignments`, `acd.assignment_scores`, `acd.report_cards`, `acd.report_card_entries`.
3. `100_SeedStage2Data.sql`: Deterministic test data ensuring a complete runnable instructional workflow:
   - Sets up curriculum stages and learning units under seeded subjects (e.g., Mathematics Stage 7, English Stage 7).
   - Maps stages to grade `SMP` `7`.
   - Creates weekly timetable entries for study group `7A` with teacher Budi Santoso (`MTK`), linking room `Classroom 7A`.
   - Batch enrolls placed students of `7A` into Mathematics Stage 7 and English Stage 7 (`academic_participations`).
   - Seeds sample attendance records for `7A`.
   - Seeds sample formative and summative assignments with student scores.

---

# 9. Cross-Cutting Concerns

- **Role & Permission Governance:**
  - Expanded permissions in `ElsasaSis.Modules.Auth`:
    - Timetables: `Permissions.Operations.Timetables.View`, `Permissions.Operations.Timetables.Manage`, `Permissions.Operations.Timetables.ViewOwn`.
    - Curriculum: `Permissions.Academic.Curriculum.View`, `Permissions.Academic.Curriculum.Manage`.
    - Attendance: `Permissions.Academic.Attendance.View`, `Permissions.Academic.Attendance.Record`.
    - Scorebook: `Permissions.Academic.Scorebook.View`, `Permissions.Academic.Scorebook.Manage`.
  - Role catalog mapping:
    - `SCHOOL_ADMIN`: Possesses all permissions.
    - `TEACHER`: `Permissions.Operations.Timetables.ViewOwn`, `Permissions.Academic.Curriculum.View`, `Permissions.Academic.Attendance.View`, `Permissions.Academic.Attendance.Record`, `Permissions.Academic.Scorebook.View`, `Permissions.Academic.Scorebook.Manage`.
    - `HOMEROOM_TEACHER`: Inherits Teacher permissions plus homeroom-scoped read access.
    - `PRINCIPAL`: Read-only operational oversight across all timetable, curriculum, attendance, and scorebook views.
- **Data Scoping & Actor Validation:**
  - Teacher personal schedule (`UC-TCH-10`) extracts `TeacherId` claim from `ICurrentUser` and strictly filters `WHERE teacher_id = @TeacherId`.
  - Assessment creation and score entry handlers verify that the current teacher is authorized to teach that subject in that study group via `tch.teacher_assignments` lookup.
- **High-Velocity Batch Commits & Route Guards:**
  - `SCR-ACD-ATTENDANCE` and `SCR-ACD-SCOREBOOK` implement explicit batch commits (D-07, D-08).
  - Dirty-state tracking via local component state.
  - Sticky bottom action bar (`Save Changes` / `Cancel`) appears when `isDirty === true`.
  - Route navigation intercepted by `onBeforeRouteLeave` to guard against accidental data loss.
- **Concurrency & Idempotency:**
  - Batch score entry and batch attendance recording utilize SQL upsert semantics guarded by unique indexes (`UQ_assignment_scores_assignment_student`, `UQ_attendance_records_student_subject_date`).
  - Concurrent writes for the same student/session overwrite deterministically without duplicating rows.
- **Error Handling (RFC 7807):**
  - Input validation failures (e.g., negative score, score exceeding max points, invalid date format) return `422 Unprocessable Entity` with camelCase property paths.
  - Domain conflicts (e.g., duplicate stage sequence order, duplicate subject name, student not actively enrolled) return `409 Conflict`.
- **UI Ergonomics & 3-Tier Confirmation:**
  - Direct Actions (Tier 1): Toggling attendance status chips, editing cell scores, switching timetable days.
  - Tier 2 Confirmation Dialog: Deleting a timetable slot, deleting a learning unit, removing a grade mapping.
  - Tier 3 Pre-Flight Impact Summary Modal: Executing batch academic participation enrollment (`UC-ADM-18`), displaying target student counts before commit.

---

# 10. Implementation Constraints

1. **Strict Assembly Isolation (NetArchTest):**
   - `ElsasaSis.Modules.Acd` must have zero project references to `ElsasaSis.Modules.Auth`, `ElsasaSis.Modules.Opr`, `ElsasaSis.Modules.Std`, or `ElsasaSis.Modules.Tch`.
   - References are restricted strictly to `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`.
2. **Pure Persistence Layer SQL Discipline:**
   - Zero database foreign keys, stored procedures, triggers, default constraints, or check constraints.
   - All primary keys must use `UuidV7.NewGuid()`.
   - All `created_at` and `updated_at` timestamps must be supplied in UTC by C# code (`DateTime.UtcNow`).
3. **No Entity Framework Core:**
   - Dapper only via `ISqlConnectionFactory`.
4. **Mandatory CQRS Handler Discipline:**
   - Write Slices must load an Aggregate Root, mutate via domain methods, save via Repository, and dispatch domain events. Write handlers must not depend on `ISqlConnectionFactory` or Dapper directly.
   - Read Slices must execute direct Dapper SQL projections into DTOs; never instantiate Aggregate Roots on read.
5. **Strict 4-Screen Inventory:**
   - No orphan screens or additional top-level routes. All 8 use cases must live in the 4 assigned screens:
     - `SCR-OPR-TIMETABLE` (`views/TimetableWorkspaceView.vue`)
     - `SCR-ACD-CURRICULUM` (`views/CurriculumSetupView.vue`)
     - `SCR-ACD-ATTENDANCE` (`views/AttendanceWorkbenchView.vue`)
     - `SCR-ACD-SCOREBOOK` (`views/ScorebookWorkbenchView.vue`)
6. **Frontend State & Typing Discipline:**
   - Server state managed strictly via TanStack Query hooks; client context via Pinia; form state via VeeValidate + Zod.
   - Generated TypeScript contracts (`openapi-typescript`) must drive all API DTO consumption.

---

# 11. Acceptance Conditions

Stage 2 implementation is architecturally complete when all of the following conditions hold:

1. **Solution & Architecture Verification:**
   - `src/backend/ElsasaSis.sln` builds cleanly under `.NET 8` (`net8.0`).
   - `ElsasaSis.ArchitectureTests` passes all NetArchTest rules confirming boundary isolation for `ElsasaSis.Modules.Acd` alongside `Auth`, `Opr`, `Std`, and `Tch`, with zero EF Core usage.
2. **Database Migrations & Seeding:**
   - DbUp executes migrations `013_CreateOprTimetableTables.sql`, `031_CreateAcdTables.sql`, and `100_SeedStage2Data.sql` successfully on SQL Server.
   - Seed data provides functional study group timetables, curriculum hierarchy, active academic participations, sample attendance records, and sample assignments with scores.
3. **Timetables & Scheduling (`opr`) Verification:**
   - `UC-ADM-04`: Administrator can configure study group timetable slots (day, period, subject, teacher, room) for `REGULAR`, `GENERAL`, and `VOCATIONAL` schedule types; slot can be deleted.
   - `UC-ADM-04`: SMK block date ranges can be added and listed for study groups in `opr.study_group_timetables`.
   - Conflict detection: Conflicting teacher or room allocations render visual conflict indicators in the timetable editor without blocking saves.
   - `UC-TCH-10`: Logged-in teacher (`teacher.budi`) can view their personalized weekly schedule containing only their assigned classes.
4. **Curriculum Structure & Academic Participations (`acd`) Verification:**
   - `UC-ADM-17`: Administrator can create and update subjects, sequential subject stages, modular learning units, and map stages to administrative grades (`(school_level, grade)`).
   - `UC-ADM-18`: Administrator can batch-enroll placed students of a study group into a subject stage (`academic_participations`) via the Pre-Flight Modal, successfully populating snapshots and skipping duplicate enrollments gracefully.
5. **Daily Attendance (`acd`) Verification:**
   - `UC-TCH-01`: Teacher can open attendance sheet for an assigned subject and study group, view the enrolled student roster, mark attendance with quick shortcuts ("Mark All Present"), adjust statuses (`PRESENT`, `ABSENT`, `EXCUSED`, `SICK`, `LATE`), and batch-save the records atomically.
   - Database unique constraint `UQ_attendance_records_student_subject_date` prevents duplicate daily attendance rows.
6. **Assessments & Scorebook (`acd`) Verification:**
   - `UC-TCH-02`: Teacher can create assignments (`FORMATIVE`, `SUMMATIVE`, `EXAM`) scoped to a subject stage and study group, with title, max points, and optional weight.
   - `UC-TCH-03`: Teacher can enter and update numeric scores (`0.00`–`100.00`) and qualitative feedback in a responsive spreadsheet grid with dirty-cell tracking and batch save.
   - `UC-TCH-05`: Scorebook view displays the consolidated gradebook showing student roster, assignment scores, weighted running total, visual passing threshold indicator, and attendance counts summary.
7. **Frontend Quality & Shell Integration:**
   - Frontend passes `pnpm typecheck`, `pnpm lint`, and unit tests.
   - Sidebar navigation in `AppLayout.vue` displays menu items according to user roles:
     - `Operations & Comms` $\rightarrow$ `Timetables & Scheduling`
     - `Academic & Curriculum` $\rightarrow$ `Curriculum & Learning Structure`
     - `Faculty & Teaching` $\rightarrow$ `Classroom Attendance` & `Teacher Scorebook`
   - High-velocity sheets (`Attendance`, `Scorebook`) enforce dirty tracking, sticky Save bars, and `onBeforeRouteLeave` navigation guards.
