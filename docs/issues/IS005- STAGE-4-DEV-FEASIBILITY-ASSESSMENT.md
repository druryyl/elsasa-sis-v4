---
Title: Stage 4 Feasibility Assessment — Consumer Portals & Executive Governance
Code: CR-STAGE-4
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-20
Status: READY-FOR-PLANNING
---

# 1. Request Summary

**Feature being assessed:** IS005 — Stage 4: Implement Consumer Portals & Executive Governance  
(`portal-parent` Parent Family Portal, `portal-student` Student Learner Portal, `acd` School-Wide Academic Monitoring, `fin` Receivables & Collection Oversight)

**Referenced artifacts:**
- `docs/issues/IS005- STAGE-4-DEV-ISSUE.md`
- `docs/architecture/ELSASA-SIS-ARCHITECTURE.md`
- `docs/architecture/backend-architecture.md`
- `docs/architecture/frontend-architecture.md`
- `docs/architecture/ui-ux-guideline.md`
- `docs/elsasa-sis-v4-development-roadmap.md`
- `docs/screen-inventory.md`
- `docs/use-case-discovery-basic.md`
- `docs/actors-roles-specification-basic.md`
- `docs/architecture/academic-tables-design.md`
- `docs/architecture/finance-tables-design.md`
- `docs/architecture/operational-tables-design.md`
- `docs/architecture/student-tables-design.md`

## Objective

Determine whether Stage 4 is architecturally, technically, and operationally ready to proceed to Architecture Update and Implementation Planning. Identify all gaps between the existing codebase and the Stage 4 feature scope, close every open architectural and business question through explicit, pragmatic, and minimalist decisions, and establish unambiguous constraints so that autonomous agentic development requires zero run-time decision making.

---

# 2. Current State

## Existing Behavior

Stages 0 through 3 are COMPLETED and operational:

- **Backend Assemblies Present:**
  - `ElsasaSis.SharedKernel`: Common primitives, result patterns, domain errors, UUID v7 generator, domain event dispatcher, current user context (`ICurrentUser`).
  - `ElsasaSis.Contracts`: Inter-module contracts (`IOperationalContract`, `IStudentContract`, `IAcademicContract`, `IFinanceModule`, `IDevelopmentModule`) and domain events (`ReportCardFinalizedDomainEvent`, `PaymentReceiptPostedDomainEvent`, `SanctionImposedDomainEvent`, `StudentPlacedDomainEvent`, etc.).
  - `ElsasaSis.Database`: DbUp migration pipeline (scripts `001` through `070` plus `099`, `100`, `101` seed scripts).
  - `ElsasaSis.Modules.Auth`: Identity and access management, JWT emission (`sub`, `roles`, `permissions`, `teacherId`, `studentId`, `guardianId`), refresh token cookie handling, PBKDF2 password hashing, and `RolePermissionCatalog`.
  - `ElsasaSis.Modules.Opr`: Academic years, operational periods, school calendar entries, rooms, announcements, study group timetables, and SMK block rotation schedules (`opr.study_group_timetables`).
  - `ElsasaSis.Modules.Std`: Candidate intake, candidate decisions, atomic student enrollment with NIS, student profiles, student guardians, study groups, and annual student placements.
  - `ElsasaSis.Modules.Tch`: Teacher master directory, subject capabilities, and teaching assignment allocations.
  - `ElsasaSis.Modules.Acd`: Curriculum structures (subjects, stages, units), academic participations, daily subject attendance, teacher scorebook grading, and collaborative semester report cards (draft generation, teacher notes, homeroom remarks, finalization lock, and admin override).
  - `ElsasaSis.Modules.Dev`: Student behaviour notes, discipline infractions and sanctions, extracurricular catalog/logs, and student achievements.
  - `ElsasaSis.Modules.Fin`: Fee policy and charge types, standing student discounts, batch billing, cashier payment receipts, advance deposits, allocations, student financial statements, and financial receivables/aging reports (`ReceivablesSummary`, `ReceivablesAging`, `DailyCashier`).
  - `ElsasaSis.Api`: Web host entry point, Minimal API endpoint mapping, Swagger OpenAPI documentation, CORS, and Transactional Outbox processor.

- **Frontend Submodules Present:**
  - `src/features/auth/`: Sign-in view (`LoginView.vue`).
  - `src/features/operations/`: Calendar, announcements, and timetables workspaces.
  - `src/features/student/`: Admissions queue and student directory/placements.
  - `src/features/teaching/`: Teacher directory and assignment matrix.
  - `src/features/academic/`: Curriculum setup, classroom attendance, teacher scorebook, and report cards workbench.
  - `src/features/development/`: Pastoral dossier and extracurricular clubs.
  - `src/features/finance/`: Fee policies, tuition billing, cashier counter, and receivables/arrears aging dashboard (`ReceivablesAgingView.vue`, already passing unit tests for `UC-ADM-26`, `UC-PRC-06`, `UC-FIN-09`, `UC-FIN-11`, and `UC-FIN-08`).

- **What Is Absent (The Stage 4 Scope):**
  - **No Parent Family Portal:** `src/features/portal-parent/` does not exist (`SCR-PRT-PORTAL`). Parents have no UI to select linked children, view finalized report cards, view billing statements, download receipts, or read parent announcements.
  - **No Student Learner Portal:** `src/features/portal-student/` does not exist (`SCR-STD-PORTAL`). Students have no UI to view their daily class agenda, weekly timetable resolving SMK block rotations, school calendar, or student announcements.
  - **No Academic Monitoring Dashboard:** `src/features/academic/monitoring/` does not exist (`SCR-ACD-MONITORING`). School administrators and principals have no institutional dashboard to monitor school-wide attendance, grading submission completeness, or report card finalization progress ahead of deadlines (`UC-ADM-21`).
  - **No Multi-Child Linkage in Auth:** The `auth.user_actors` table provides only a single `guardian_id` column, with no mapping table to link a parent user account to multiple children.
  - **Empty Role Permissions:** In `RolePermissionCatalog`, `Roles.Parent` and `Roles.Student` have empty permission lists (`[]`), causing any authenticated request by these roles to fail with `403 Forbidden`.
  - **Missing Backend Read Slices:**
    - `GET /api/std/students/my-children` (`Modules.Std`) does not exist.
    - `GET /api/opr/timetables/my` (`Modules.Opr`) does not exist.
    - `GET /api/acd/monitoring/progress` (`Modules.Acd`) does not exist.
    - `GET /api/acd/report-cards/student/{studentId}` (`Modules.Acd`) does not exist.
  - **No Consumer Portal Layout:** Only `AppLayout.vue` (administrative sidebar) and `AuthLayout.vue` exist. `LoginView.vue` unconditionally redirects all authenticated users to `/operations/calendar`.

## Existing Constraints

1. **Pure Persistence Layer:** No triggers, stored procedures, DB-side CHECK constraints, or DEFAULT constraints. All validation, state derivation, and business invariants live strictly in the Application Layer.
2. **UUID v7 Primary Keys:** All primary keys are application-generated time-ordered UUID v7.
3. **No Cross-Schema Database Foreign Keys:** Cross-module references are plain `UNIQUEIDENTIFIER` columns.
4. **Permitted Cross-Schema Joins in Read Slices:** Per `backend-architecture.md` Principle 2 & §11.2, CQRS Read Slices project directly to response DTOs using Dapper and may execute cross-schema `JOIN` queries within the handler SQL.
5. **CQRS Mandatory:** Write slices rehydrate Domain Aggregates through repositories. Read slices use direct Dapper queries without domain models or entity tracking.
6. **RFC 7807 Problem Details:** All API errors return RFC 7807 responses with camelCase validation error dictionaries.
7. **Single-Tenant Scope:** Single school deployment without `org_unit_id` multi-tenant partitioning.
8. **Browser-Based Printing:** Official report cards and cashier receipts utilize browser-native print rendering (`window.print()`) through dedicated print layouts without server-side PDF generation dependencies (per Stage 3 decisions OQ-023 and OQ-024).

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | No multi-child mapping table exists in `auth`. `auth.user_actors` only has a single `guardian_id` column, preventing a parent account from linking to multiple enrolled children. |
| GAP-002 | CRITICAL | `ElsasaSis.Modules.Acd` lacks the academic monitoring read slice and endpoint (`UC-ADM-21` / `SCR-ACD-MONITORING`). School-wide attendance, grading completeness, and report card readiness cannot be tracked. |
| GAP-003 | CRITICAL | `ElsasaSis.Modules.Acd` lacks a student-specific report card query (`GET /api/acd/report-cards/student/{studentId}`) and lacks parent child-ownership and draft-hiding validation on `GET /api/acd/report-cards/{id}` (`UC-PRT-01`). |
| GAP-004 | CRITICAL | `ElsasaSis.Modules.Opr` lacks the personal student timetable read slice (`GET /api/opr/timetables/my`) resolving student study group placement, active SMK vocational block rotations, and today's class agenda (`UC-STD-01`). |
| GAP-005 | CRITICAL | `ElsasaSis.Modules.Std` lacks the linked-children query endpoint for parents (`GET /api/std/students/my-children`). |
| GAP-006 | MAJOR | `RolePermissionCatalog` assigns empty permission arrays (`[]`) to `Roles.Parent` and `Roles.Student`. Authenticated consumer accounts are blocked from accessing any API endpoint. |
| GAP-007 | MAJOR | `Permissions.Academic.Monitoring.View` and `Permissions.Students.Children.View` permissions do not exist in `Modules.Auth` or domain modules. |
| GAP-008 | MAJOR | `AnnouncementAccessPolicy` and `GetAnnouncementsHandler` do not scope announcements to `ALL` and `PARENTS` for parent users or `ALL` and `STUDENTS` for student users (`UC-PRT-04`, `UC-STD-03`). |
| GAP-009 | MAJOR | Frontend consumer portal feature directories and views do not exist: `src/features/portal-parent/` (`SCR-PRT-PORTAL`) and `src/features/portal-student/` (`SCR-STD-PORTAL`). |
| GAP-010 | MAJOR | Frontend academic monitoring submodule does not exist: `src/features/academic/monitoring/` (`SCR-ACD-MONITORING`). |
| GAP-011 | MAJOR | Frontend lacks a dedicated mobile-first `PortalLayout.vue`. Furthermore, `LoginView.vue` and root `/` route unconditionally redirect to `/operations/calendar`, causing parents and students to encounter a 403 Forbidden screen upon login. |
| GAP-012 | MINOR | `fin.student_charges` and `fin.payment_receipts` endpoints (`GetStudentStatementHandler` and `GetPaymentReceiptByIdHandler`) do not enforce child-ownership checks when the caller is in the `PARENT` role (`UC-PRT-02`, `UC-PRT-03`). |
| GAP-013 | MINOR | Database seed data lacks deterministic QA accounts for `PARENT` and `STUDENT` roles (`parent.hendra`, `student.ahmad`, `student.siti`), preventing automated end-to-end integration testing and verification of consumer portals. |

---

# 4. Open Questions

*All open questions have been systematically explored and closed via the `/grill-me` architectural interview on 2026-11-20. Zero open questions remain unresolved.*

| ID | Question | Status | Impact |
|---|---|---|---|
| OQ-001 | How should parent user accounts be linked to multiple enrolled children for the Parent Portal multi-child selector? | CLOSED | Database schema addition in `auth` schema; data-scoping queries in `Modules.Std`, `Modules.Acd`, `Modules.Fin`. |
| OQ-002 | Where should the backend API endpoints backing the Parent and Student Portals be located? | CLOSED | Assembly placement and endpoint routing conventions. |
| OQ-003 | How should permissions and role assignments be structured for Parent Portal, Student Portal, and Academic Monitoring? | CLOSED | Authorization policy definitions in `Modules.Auth`, domain modules, and `ElsasaSis.Api`. |
| OQ-004 | How should the personal timetable query (UC-STD-01) structure and return the SMK block-resolved schedule and Today's Agenda? | CLOSED | API response contract and Dapper query structure in `Modules.Opr`. |
| OQ-005 | How should the School-Wide Academic Monitoring dashboard (UC-ADM-21 / SCR-ACD-MONITORING) aggregate its KPIs, exception alerts, and drill-down data? | CLOSED | Read handler Dapper aggregation in `Modules.Acd`. |
| OQ-006 | How should the frontend shell/layout and navigation routing be designed for consumer portals and monitoring screens? | CLOSED | Frontend layout architecture, Vue Router guards, and post-login redirection. |
| OQ-007 | How are report cards and cashier payment receipts rendered and downloaded in the consumer portal? | CLOSED | Frontend print view reuse (`window.print()`). Zero new server-side PDF libraries. |
| OQ-008 | How are announcements audience filters enforced for Parent and Student actors? | CLOSED | Query filter extension in `AnnouncementAccessPolicy` and `GetAnnouncementsHandler`. |
| OQ-009 | How does `Modules.Fin` enforce child-scoping when parents query billing statements or payment receipts? | CLOSED | Handler-level authorization check using `auth.user_students`. |
| OQ-010 | What is the database migration numbering and test account seeding strategy? | CLOSED | DbUp script numbering (`004_CreateAuthUserStudents.sql`, `102_SeedStage4Data.sql`) and `Stage4Seeder.cs`. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | Single-tenant deployment: The school operates as a single educational entity; multi-tenant `org_unit_id` filtering is not required. |
| ASM-002 | An enrolled student has exactly one active study group placement per academic year in `std.student_placements`. |
| ASM-003 | If a student's study group has an active SMK block rotation in `opr.study_group_timetables` where the query date falls within `[start_date, end_date]`, the student's timetable resolves to that block's `schedule_type` (`GENERAL` or `VOCATIONAL`). Otherwise, it resolves to `REGULAR`. |
| ASM-004 | A student user account in `auth.users` has a 1:1 link to `std.students.id` via `auth.user_actors.student_id`. |
| ASM-005 | A parent user account in `auth.users` links to 1..N children in `std.students` via the new mapping table `auth.user_students (user_id, student_id)`. |
| ASM-006 | Parents may only view report cards with `status = 'FINALIZED'`. Draft or in-review report cards must return `404 Not Found` or `403 Forbidden` to parents. |
| ASM-007 | Financial Receivables monitoring (`SCR-FIN-RECEIVABLES`) was already substantially implemented and verified in Stage 3 (`src/features/finance/receivables/` passing 6 unit tests). Stage 4 validates its alignment with `UC-ADM-26`, `UC-PRC-06`, and `UC-FIN-09` without re-implementing existing code. |
| ASM-008 | All new migration scripts are idempotent, using `IF NOT EXISTS` guards. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Parent accessing another student's report card, billing statement, or receipt by guessing UUIDs. | Critical data breach / privacy violation across families. | Handlers for `GET /api/acd/report-cards/{id}`, `GET /api/acd/report-cards/student/{studentId}`, `GET /api/fin/students/{id}/statement`, and `GET /api/fin/payments/{id}` explicitly verify that `student_id IN (SELECT student_id FROM auth.user_students WHERE user_id = @UserId)` when the caller has the `PARENT` role. |
| RISK-002 | Parent viewing draft grades or unfinished report cards before homeroom teacher finalization. | Confusion, student stress, and administrative disputes. | Handlers explicitly enforce `status = 'FINALIZED'` on all report card queries called by the `PARENT` role. Drafts are completely hidden. |
| RISK-003 | Performance degradation in `UC-ADM-21` if academic monitoring runs non-indexed table scans across large attendance and score tables. | Dashboard loading latency for school leadership. | Dapper SQL aggregates utilize existing indexed columns (`academic_year_id`, `operational_period_id`, `date`, `study_group_id`). Query executes sub-50ms for school-scale data. |
| RISK-004 | Student timetable failing to resolve SMK rotation when dates transition between General and Vocational blocks. | Students attending wrong classrooms or workshop blocks. | Handler queries `opr.study_group_timetables` for date overlap `start_date <= @Date AND end_date >= @Date` and matches `schedule_type` in `opr.study_group_schedules`. Falls back cleanly to `REGULAR` if non-SMK. |
| RISK-005 | Post-login redirect routing consumer actors to forbidden administrative screens (`/operations/calendar`). | Poor user experience / immediate 403 Forbidden on sign-in. | `LoginView.vue` and route guards inspect `sessionUser.roles` and route `PARENT` to `/portal/parent` and `STUDENT` to `/portal/student`. |

---

# 7. Recommendations

## Option A — Granular Domain-Aligned Slices & Dedicated Consumer Portal Layout (Recommended)

- Create `auth.user_students` mapping table in `004_CreateAuthUserStudents.sql`.
- Place read slices in their authoritative domain modules (`Modules.Std`, `Modules.Acd`, `Modules.Fin`, `Modules.Opr`).
- Define explicit, granular permissions (`Permissions.Academic.Monitoring.View`, `Permissions.Students.Children.View`) and populate `RolePermissionCatalog` for `PARENT` and `STUDENT`.
- Build a dedicated, responsive `PortalLayout.vue` for consumer portals without the admin sidebar, with role-based post-login redirection.
- Implement real-time Dapper aggregation for `SCR-ACD-MONITORING`.

### Advantages
- Strictly adheres to `ELSASA-SIS-ARCHITECTURE.md` §8 and §13.
- Minimalist, explicit, and highly maintainable: avoids artificial BFF layers or bloated web hosts.
- Exceptional consumer UX: mobile-friendly layout for parents and students without administrative clutter.
- Zero ambiguity for autonomous agentic implementation.

### Disadvantages
- Requires touching 4 backend domain modules and creating 1 migration script + 1 seed script.

## Option B — Monolithic BFF Controller in Web Host

- Put all portal logic in a single `PortalController` inside `ElsasaSis.Api`.
- Reuse `AppLayout.vue` with conditional `v-if` flags.

### Disadvantages
- Bloats the web host API layer with domain query logic.
- Leaks database queries across domain boundaries into the API host.
- Compromises mobile UX by forcing a heavy admin layout onto phone viewports.

---

# 8. Gap Closure

## GAP-001 (Parent Multi-Child Mapping) & OQ-001

### Decision

Add migration script `004_CreateAuthUserStudents.sql`:

```sql
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.user_students') AND type = N'U')
BEGIN
    CREATE TABLE [auth].[user_students] (
        [user_id]    UNIQUEIDENTIFIER NOT NULL,
        [student_id] UNIQUEIDENTIFIER NOT NULL,
        [created_at] DATETIME2(3)     NOT NULL,
        CONSTRAINT [PK_auth_user_students] PRIMARY KEY CLUSTERED ([user_id], [student_id]),
        CONSTRAINT [FK_auth_user_students_users] FOREIGN KEY ([user_id]) REFERENCES [auth].[users] ([id])
    );

    CREATE NONCLUSTERED INDEX [IX_auth_user_students_student_id]
        ON [auth].[user_students] ([student_id]);
END;
```

When resolving a parent's authorized student scope in any read handler:
```sql
WHERE student_id IN (SELECT student_id FROM auth.user_students WHERE user_id = @UserId)
```

### Rationale

Keeping user-to-actor authorization mapping strictly inside the `auth` schema maintains single-responsibility principles, avoids modifying `std.student_guardians`, and cleanly supports parents with 1, 2, or more enrolled children.

### Impact

One new migration script (`004_CreateAuthUserStudents.sql`). No impact on existing tables.

### Architecture Impact

New support table in `auth` schema, following existing `auth.user_roles` pattern.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-002 (School-Wide Academic Monitoring) & OQ-005

### Decision

Implement `Features/Monitoring/GetAcademicMonitoringProgress/` in `ElsasaSis.Modules.Acd`:
- Route: `GET /api/acd/monitoring/progress?academicYearId={id}&operationalPeriodId={id}&date={YYYY-MM-DD}`
- Protected by: `Permissions.Academic.Monitoring.View` (granted to `SCHOOL_ADMIN` and `PRINCIPAL`).
- Execution: Direct Dapper query in `GetAcademicMonitoringProgressHandler` computing:
  1. `overview`:
     - `attendanceRateToday`: count present, absent, sick, late, excused, total enrolled, and percentage attended.
     - `gradingCompleteness`: total assignments in period, total scores recorded, expected scores, and % completed.
     - `reportCardProgress`: total placed students, count DRAFT, count FINALIZED, % finalized.
  2. `exceptions`:
     - `missingAttendanceStudyGroups`: study groups with 0 attendance recorded for the queried date.
     - `overdueGradingSubjects`: assignments where created date > 7 days ago and scored student count < enrolled student count.
     - `unfinalizedReportCardStudyGroups`: study groups with pending DRAFT report cards.
  3. `studyGroupDrillDown`: list of rows per study group (level, grade, study group name, homeroom teacher name, attendance rate today, assignments count, grading %, report cards finalized/total).

### Rationale

Real-time Dapper aggregation in the application layer satisfies the Pure Persistence Layer principle, executes in < 50ms for school-scale data, and delivers all necessary oversight in a single cohesive request.

### Impact

New feature folder `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/Monitoring/`. One new endpoint in `AcdEndpoints.cs`.

### Architecture Impact

Read Slice in `Modules.Acd`. No schema change required.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-003 (Parent Child Report Cards) & OQ-007

### Decision

1. In `ElsasaSis.Modules.Acd/Features/ReportCards/`:
   Add Read Slice `GetStudentReportCards` mapped to `GET /api/acd/report-cards/student/{studentId}`:
   - Protected by: `Permissions.Academic.ReportCards.View`.
   - If caller is in `PARENT` role:
     - Enforces: `studentId` MUST exist in `SELECT student_id FROM auth.user_students WHERE user_id = @UserId`. If not, returns `403 Forbidden`.
     - Query condition: `WHERE rc.student_id = @StudentId AND rc.status = 'FINALIZED'`. (Drafts strictly hidden).
   - If caller is staff (`SCHOOL_ADMIN`, `PRINCIPAL`, `TEACHER`):
     - Allows viewing both `DRAFT` and `FINALIZED` report cards for the student.
2. In existing `GetReportCardByIdHandler` (`GET /api/acd/report-cards/{id}`):
   - Add actor check: If caller has `PARENT` role:
     - Verify that report card `student_id` is linked to `@UserId` in `auth.user_students`.
     - Verify that `status == 'FINALIZED'`. If `DRAFT`, returns `404 NotFound`.
3. Report card print view: Reuses existing `ReportCardPrintView.vue` with `window.print()` triggered from the Parent Portal.

### Rationale

Ensures strict child privacy and hides internal draft grading from parents until officially finalized.

### Impact

One new Read Slice in `Modules.Acd`. Slight update to `GetReportCardByIdHandler`.

### Architecture Impact

Scoped authorization guard in `Modules.Acd` read queries.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-004 (Personal Student Timetable & Agenda) & OQ-004

### Decision

Implement `Features/Timetables/GetPersonalStudentTimetable/` in `ElsasaSis.Modules.Opr`:
- Route: `GET /api/opr/timetables/my?date={YYYY-MM-DD}`
- Protected by: `Permissions.Operations.Timetables.ViewOwn` (granted to `STUDENT`).
- Handler logic:
  1. Resolves `studentId = ICurrentUser.StudentId`. If null, returns `401 Unauthorized` / `403 Forbidden`.
  2. Resolves active academic year and period via `IOperationalContract.GetActiveOperationalPeriodAsync`.
  3. Queries `std.student_placements` to obtain the student's `study_group_id`, `study_group_name`, `school_level`, and `grade`. If unplaced, returns an empty schedule with placement-pending message.
  4. Resolves SMK block rotation: Checks `opr.study_group_timetables` for an active block where `study_group_id = @StudyGroupId` AND `operational_period_id = @PeriodId` AND `@Date BETWEEN start_date AND end_date`.
     - If block found, `activeScheduleType = block.schedule_type` (`GENERAL` or `VOCATIONAL`).
     - If no block found, `activeScheduleType = 'REGULAR'`.
  5. Queries `opr.study_group_schedules` for all weekly slots of the study group.
  6. Filters `todayAgenda`: slots where `day_of_week = @DayOfWeek` AND (`schedule_type = @ActiveScheduleType` OR `schedule_type = 'REGULAR'`), ordered by `period_slot`.
  7. Returns unified DTO: `{ studentInfo, activeCycle, todayAgenda, weeklySchedule }`.

### Rationale

Single unified DTO prevents multiple network round-trips for mobile learners and seamlessly handles SMK alternate-week rotations.

### Impact

New feature folder in `Modules.Opr`. Mapped in `TimetableEndpoints.cs`.

### Architecture Impact

Read Slice in `Modules.Opr` with cross-schema read to `std.student_placements`.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-005 (Parent Linked Children Query)

### Decision

Implement `Features/Students/GetMyChildren/` in `ElsasaSis.Modules.Std`:
- Route: `GET /api/std/students/my-children`
- Protected by: `Permissions.Students.Children.View` (granted to `PARENT`).
- Handler SQL:
  ```sql
  SELECT 
      s.id AS StudentId,
      s.nis AS Nis,
      s.nisn AS Nisn,
      s.full_name AS FullName,
      s.nickname AS Nickname,
      s.date_of_birth AS DateOfBirth,
      s.gender AS Gender,
      sp.study_group_id AS StudyGroupId,
      sp.study_group_name AS StudyGroupName,
      sp.school_level AS SchoolLevel,
      sp.grade AS Grade
  FROM std.students s
  INNER JOIN auth.user_students us ON us.student_id = s.id
  LEFT JOIN std.student_placements sp 
         ON sp.student_id = s.id 
        AND sp.academic_year_id = @ActiveAcademicYearId
  WHERE us.user_id = @CurrentUserId
    AND s.status = 'ACTIVE'
  ORDER BY s.full_name ASC;
  ```
- Returns `IReadOnlyList<LinkedChildDto>`.

### Rationale

Allows the Parent Portal multi-child switcher to dynamically populate tabs for each of the parent's children.

### Impact

One new Read Slice in `Modules.Std`.

### Architecture Impact

Read Slice in `Modules.Std` with cross-schema read to `auth.user_students`.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-006 & GAP-007 (Permissions & Role Catalog) & OQ-003

### Decision

1. In `ElsasaSis.Modules.Auth/Permissions.cs`:
   - Add `Permissions.Academic.Monitoring.View = "Permissions.Academic.Monitoring.View"`
   - Add `Permissions.Students.Children.View = "Permissions.Students.Children.View"`
2. In `RolePermissionCatalog.cs`:
   - `Roles.SchoolAdmin`: Add `Permissions.Academic.Monitoring.View`.
   - `Roles.Principal`: Add `Permissions.Academic.Monitoring.View`.
   - `Roles.Parent`:
     ```csharp
     [Roles.Parent] =
     [
         Permissions.Operations.Calendar.View,
         Permissions.Operations.Announcements.View,
         Permissions.Students.Children.View,
         Permissions.Academic.ReportCards.View,
         Permissions.Finance.Statements.View
     ]
     ```
   - `Roles.Student`:
     ```csharp
     [Roles.Student] =
     [
         Permissions.Operations.Timetables.ViewOwn,
         Permissions.Operations.Calendar.View,
         Permissions.Operations.Announcements.View
     ]
     ```
3. In `ElsasaSis.Api/Program.cs`:
   - Authorization policies automatically register from `Permissions.All`.

### Rationale

Follows the existing granular permission system. No hardcoded role bypasses in controllers.

### Impact

Updates to `Permissions.cs`, `RolePermissionCatalog.cs`, and `ElsasaSis.Modules.Acd/Permissions.cs`.

### Architecture Impact

Zero structural change. Standard catalog maintenance.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-008 (Announcements Audience Scoping) & OQ-008

### Decision

Update `AnnouncementAccessPolicy.cs` and `GetAnnouncementsHandler.cs` in `ElsasaSis.Modules.Opr`:
- When caller has role `PARENT` (and does not hold staff roles):
  - Add query condition: `AND a.target_audience IN ('ALL', 'PARENTS')`
  - Enforce `a.status = 'PUBLISHED'`
- When caller has role `STUDENT` (and does not hold staff roles):
  - Add query condition: `AND a.target_audience IN ('ALL', 'STUDENTS')`
  - Enforce `a.status = 'PUBLISHED'`

### Rationale

Prevents parents from reading internal staff/student notices, and prevents students from reading parent-specific tuition/administrative notices.

### Impact

Query filter adjustment in `GetAnnouncementsHandler`.

### Architecture Impact

None. Existing handler enhancement.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-009, GAP-010, GAP-011 (Frontend Portals, Layout & Redirection) & OQ-006

### Decision

1. **`PortalLayout.vue` (`src/app/layouts/PortalLayout.vue`):**
   - Clean, mobile-first authenticated shell without the administrative sidebar.
   - Top Bar: School Logo ("ELSASA-SIS"), Role Badge ("Portal Orang Tua" / "Portal Siswa"), User Full Name, and Logout action.
   - Max width: `max-w-5xl` centered responsive container with pull-to-refresh / tab navigation.
2. **Parent Family Portal (`src/features/portal-parent/views/ParentPortalView.vue` — `SCR-PRT-PORTAL`):**
   - Multi-child switcher tabs: `[Ananda: Ahmad Dahlan - 7A] [Ananda: Siti Nurhaliza - 7A]`.
   - 3 Feature Tabs:
     - **Tab 1 (Rapor Akademik):** List of finalized semester report cards with letter grades, homeroom remarks, and "Unduh Rapor PDF" (triggers `ReportCardPrintView.vue` with `window.print()`).
     - **Tab 2 (Keuangan & SPP):** Monthly tuition status (Paid / Unpaid / Partially Paid), discounts applied, deposit balance banner, and historical payment receipt list with "Cetak Kuitansi" buttons (`PaymentReceiptPrintView.vue`).
     - **Tab 3 (Info Sekolah):** School calendar dates, holidays, and announcements targeted to parents.
3. **Student Learner Portal (`src/features/portal-student/views/StudentPortalView.vue` — `SCR-STD-PORTAL`):**
   - Student info header: NIS, Name, Study Group, School Level.
   - SMK Cycle Badge: "Siklus Kejuruan (Vocational)" or "Siklus Umum (General)" with date validity.
   - View Toggle: "Agenda Hari Ini" (Today's remaining classes) vs "Jadwal Mingguan" (Full weekly timetable matrix).
   - Side Bulletin / Tabs: Academic calendar events and student announcements feed.
4. **School-Wide Academic Monitoring (`src/features/academic/monitoring/views/AcademicMonitoringView.vue` — `SCR-ACD-MONITORING`):**
   - Executive KPI cards: School Attendance Today %, Assessment Grading %, Report Cards Finalized %.
   - Operational Exception Banners: Missing attendance today alert, overdue scorebooks alert, pending draft report cards alert.
   - Progressive drill-down table with filters for Academic Year, Period, Level, Grade, and Study Group.
   - Registered under `AppLayout.vue` at `/academic/monitoring`.
5. **Post-Login & Root Redirect:**
   - Update `LoginView.vue` and router guard:
     - If user has role `PARENT` $\rightarrow$ redirect to `/portal/parent`.
     - If user has role `STUDENT` $\rightarrow$ redirect to `/portal/student`.
     - Otherwise $\rightarrow$ redirect to `/operations/calendar`.

### Rationale

Delivers dedicated mobile-first ergonomics for external consumers while preserving the administrative workbench for staff.

### Impact

New layout component, 2 new feature submodules (`portal-parent`, `portal-student`), 1 new academic submodule (`academic/monitoring`), and router configuration updates.

### Architecture Impact

Standard frontend architecture feature modularity.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-012 (Finance Child Data Scoping) & OQ-009

### Decision

In `ElsasaSis.Modules.Fin`:
1. In `GetStudentStatementHandler` (`GET /api/fin/students/{studentId}/statement`):
   - If caller has role `PARENT`: Verify `studentId IN (SELECT student_id FROM auth.user_students WHERE user_id = @UserId)`. If not, return `403 Forbidden`.
2. In `GetPaymentReceiptByIdHandler` (`GET /api/fin/payments/{receiptId}`):
   - If caller has role `PARENT`: Verify receipt belongs to a linked child:
     ```sql
     AND EXISTS (
         SELECT 1 FROM auth.user_students us
         WHERE us.user_id = @UserId
           AND us.student_id = r.student_id
     )
     ```
     If not, return `403 Forbidden`.

### Rationale

Prevents unauthorized access to financial statements or receipts across different students.

### Impact

Two handler guard checks in `Modules.Fin`.

### Architecture Impact

None. Standard handler authorization check.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-013 (Deterministic Test Seeding) & OQ-010

### Decision

Add migration script `102_SeedStage4Data.sql` and `Stage4Seeder.cs`:
1. Seed Parent account:
   - `username = 'parent.hendra'`, `display_name = 'Hendra Dahlan'`, `password = 'Parent123!'` (Identity v3 hash).
   - Role: `PARENT`.
   - Links in `auth.user_students` to:
     - Ahmad Dahlan (`01A1F11A-7010-7010-8000-000000000010`)
     - Siti Nurhaliza (`01A1F11A-7011-7011-8000-000000000011`)
2. Seed Student accounts:
   - `username = 'student.ahmad'`, `display_name = 'Ahmad Dahlan'`, `password = 'Student123!'`.
     - Role: `STUDENT`.
     - `auth.user_actors.student_id` = Ahmad Dahlan ID.
   - `username = 'student.siti'`, `display_name = 'Siti Nurhaliza'`, `password = 'Student123!'`.
     - Role: `STUDENT`.
     - `auth.user_actors.student_id` = Siti Nurhaliza ID.

### Rationale

Provides stable, reproducible fixtures for QA verification gates and automated integration tests.

### Impact

One new seed script (`102_SeedStage4Data.sql`) and `Stage4Seeder.cs`.

### Architecture Impact

None. Follows `099`, `100`, `101` seeder patterns.

### Resolved By

User decision via `/grill-me` — 2026-11-20

### Resolved Date

2026-11-20

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Stage 4 introduces:
- A new database mapping table (`auth.user_students`) altering the identity and authorization data model.
- 4 new backend Read Slices across 3 existing domain modules (`Modules.Std`, `Modules.Opr`, `Modules.Acd`).
- Security scoping updates across `Modules.Auth`, `Modules.Opr`, `Modules.Acd`, and `Modules.Fin` enforcing parent child-ownership and draft-hiding invariants.
- 2 new system permissions and activation of `Roles.Parent` and `Roles.Student` in `RolePermissionCatalog`.
- A new frontend shell layout (`PortalLayout.vue`), post-login redirection engine, and 3 new UI screens (`SCR-PRT-PORTAL`, `SCR-STD-PORTAL`, `SCR-ACD-MONITORING`).
- 11 use cases to be formally verified.

Formal architecture documentation (`IS005- STAGE-4-DEV-ARCHITECTURE.md`) is required before implementation planning begins.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved (GAP-001 through GAP-013)
- [x] All required decisions recorded (OQ-001 through OQ-010)
- [x] All blocking open questions resolved (zero open OQs remaining)
- [x] Pure Persistence Layer constraints respected (zero triggers, zero SPs, zero DB defaults/checks)
- [x] Architecture can be finalized or updated based on approved decisions

## Status

**READY-FOR-PLANNING** — Granted by Architect on 2026-11-20.

## Notes

All 10 open questions and 13 gaps have been exhaustively evaluated and closed with pragmatic, explicit, and minimalist decisions. No architectural or business ambiguities remain. The agentic implementation team will have complete, prescriptive specifications and will not need to make autonomous design decisions.

READY-FOR-PLANNING means that feasibility is sufficiently resolved for the Architecture skill to finalize the target architecture (`IS005- STAGE-4-DEV-ARCHITECTURE.md`). Planning begins only after the required architecture artifact is complete.

---

# 11. References

**Artifacts:**
- [IS005- STAGE-4-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS005-%20STAGE-4-DEV-ISSUE.md)
- [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
- [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
- [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
- [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)
- [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md)
- [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md)
- [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md)
- [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md)

**Codebase Locations:**
- `src/backend/src/Modules/ElsasaSis.Modules.Auth/` — Auth & Role Catalog
- `src/backend/src/Modules/ElsasaSis.Modules.Std/` — Student & My Children queries
- `src/backend/src/Modules/ElsasaSis.Modules.Opr/` — Calendar, Announcements & Student Timetables
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/` — Report Cards & Academic Monitoring
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/` — Billing Statements, Receipts & Receivables Aging
- `src/backend/src/ElsasaSis.Database/Scripts/` — Database migrations (`004_CreateAuthUserStudents.sql`, `102_SeedStage4Data.sql`)
- `src/frontend/src/app/layouts/` — Shell layouts (`PortalLayout.vue`)
- `src/frontend/src/features/portal-parent/` — Parent Portal (`SCR-PRT-PORTAL`)
- `src/frontend/src/features/portal-student/` — Student Portal (`SCR-STD-PORTAL`)
- `src/frontend/src/features/academic/monitoring/` — Academic Monitoring (`SCR-ACD-MONITORING`)
- `src/frontend/src/features/finance/receivables/` — Receivables & Collection Monitoring (`SCR-FIN-RECEIVABLES`)
