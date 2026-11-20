---
Title: Stage 4 Consumer Portals & Executive Governance — Target Architecture
Code: CR-STAGE-4
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-20
---

# 1. Overview

This document defines the authoritative target architecture for **CR-STAGE-4** (**Stage 4: Consumer Portals & Executive Governance**) of **ELSASA-SIS v4 (Basic Level)**. It realizes the change request captured in [IS005- STAGE-4-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS005-%20STAGE-4-DEV-ISSUE.md) and consumes all decisions established in the Feasibility Assessment [IS005- STAGE-4-DEV-FEASIBILITY-ASSESSMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS005-%20STAGE-4-DEV-FEASIBILITY-ASSESSMENT.md).

Building upon the completed operational and instructional foundation delivered in Stages 0 through 3 (walking skeleton, identity, operational periods, master schedules, academic scorebooks, semester report cards, student pastoral dossiers, and tuition billing/cashier operations), Stage 4 delivers two external self-service consumer portals and two high-level institutional governance dashboards:

1. **Parent Family Portal (`portal-parent` — `SCR-PRT-PORTAL` at `/portal/parent`):** Responsive, mobile-first self-service portal empowering parents and guardians to switch among linked enrolled children, view and print finalized semester academic report cards (with internal grading drafts strictly hidden), inspect monthly fee billing statements and remaining advance deposit balances, download official payment receipts, and read school bulletins and academic calendars (`UC-PRT-01`, `UC-PRT-02`, `UC-PRT-03`, `UC-PRT-04`).
2. **Student Learner Portal (`portal-student` — `SCR-STD-PORTAL` at `/portal/student`):** Distraction-free, mobile-friendly self-service portal for enrolled learners to inspect their personal weekly study group timetable (automatically resolving SMK vocational block rotation cycles between General and Vocational periods), view today's class agenda, check active school calendars and holidays, and read student-targeted announcements (`UC-STD-01`, `UC-STD-02`, `UC-STD-03`).
3. **School-Wide Academic Progress Monitoring (`acd` — `SCR-ACD-MONITORING` at `/academic/monitoring`):** High-level institutional governance dashboard for school administrators and principals to track daily attendance taking completeness, assessment grading status across teachers and subjects, and semester report card finalization progress ahead of publishing deadlines, featuring exception alerts and progressive drill-downs (`UC-ADM-21`).
4. **Financial Receivables, Arrears Aging & Collection Governance (`fin` — `SCR-FIN-RECEIVABLES` at `/finance/receivables`):** Executive financial oversight dashboard providing visibility over total school receivables, aged tuition arrears (*tunggakan per rombel*), cash versus bank transfer collections across date windows, guardian advance deposit liabilities, and detailed student billing statements for auditing and leadership coordination without requiring cashier counter write permissions (`UC-ADM-26`, `UC-PRC-06`, `UC-FIN-09`).

Stage 4 realizes exactly **11 actor-driven use cases** across **4 UI screens** (2 dedicated consumer portals, 1 executive academic monitoring dashboard, and 1 financial oversight dashboard), introduces **1 new database table** (`auth.user_students`) and **1 test seed script**, extends **4 backend modules** (`Modules.Auth`, `Modules.Std`, `Modules.Opr`, `Modules.Acd`, with validation guards in `Modules.Fin`), and introduces a dedicated consumer frontend shell (`PortalLayout.vue`) with role-based post-login redirection.

---

# 2. Architectural Basis

## Business Context

Business concepts, domain invariants, actors, use cases, and screen ergonomics realized by Stage 4 are governed by upstream product specifications:

- **Domain Knowledge & Invariants:**
  - [student-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/student-domain-basic.md) (`Student`, `StudentPlacement`, `StudyGroup`, `Guardian`, `BR-STD-005`..`BR-STD-008`).
  - [operational-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/operational-domain-basic.md) (`AcademicYear`, `OperationalPeriod`, `CalendarEntry`, `Announcement`, `StudyGroupTimetable`, `StudyGroupSchedule`, `BR-OPR-007`..`BR-OPR-012`).
  - [academic-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/academic-domain-basic.md) (`ReportCard`, `ReportCardEntry`, `AttendanceRecord`, `Assignment`, `AssignmentScore`, `BR-RPCD-001`..`BR-RPCD-004`).
  - [finance-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/finance-domain-basic.md) (`StudentCharge`, `PaymentReceipt`, `PaymentAllocation`, `BR-FIN-006`..`BR-FIN-012`).
- **Actors & Roles:** [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md) (`PARENT`, `STUDENT`, `PRINCIPAL`, `SCHOOL_ADMIN`, `FINANCE_OFFICER`).
- **Use Case Taxonomy:** [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (11 use cases across `portal-parent`, `portal-student`, `acd`, and `fin`).
- **Screens & UI/UX Standards:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) and [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md) (§2.5 Navigation Taxonomy, §3 Screen Patterns, §5 Responsive Tiers, §6 Interaction Patterns, §8 Feedback & System States).

## Analysis Input

All architectural decisions (`GAP-001` through `GAP-013` and `OQ-001` through `OQ-010`) were formally evaluated and closed in the Feasibility Assessment:

- **Feasibility Assessment:** [IS005- STAGE-4-DEV-FEASIBILITY-ASSESSMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS005-%20STAGE-4-DEV-FEASIBILITY-ASSESSMENT.md) (`CR-STAGE-4`, `Status: READY-FOR-PLANNING`).
- **Base Architecture & Technical Standards:**
  - [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md) (Modular Monolith, Schemas, Use Case Mapping).
  - [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md) (Modular Monolith, VSA, Clean DDD, Dapper Pure Persistence, RFC 7807).
  - [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md) (Vue 3, TanStack Query, Pinia, VeeValidate + Zod, PrimeVue Aura).
  - [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md)
  - [finance-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/finance-tables-design.md)
  - [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md)
  - [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md)

```text
DOMAINS + ROADMAP + GUIDELINES (docs/architecture/)
                      +
        IS005 FEASIBILITY-ASSESSMENT
                      ↓
           CR-STAGE-4-ARCHITECTURE
```

---

# 3. Scope

## Included

Stage 4 realizes exactly **11 actor-driven use cases** across **4 UI screens** and supporting backend assemblies:

1. **Database Schema & Migrations (`ElsasaSis.Database`):**
   - Migration script `004_CreateAuthUserStudents.sql`: Create mapping table `auth.user_students` with primary key `(user_id, student_id)` and non-clustered index on `student_id`.
   - Migration script `102_SeedStage4Data.sql`: Deterministic seed fixtures for `Roles.Parent` (`parent.hendra` linked to 2 children) and `Roles.Student` (`student.ahmad`, `student.siti`).
2. **Identity, RBAC & Authorization (`ElsasaSis.Modules.Auth` & `ElsasaSis.Api`):**
   - Add compile-time permissions:
     - `Permissions.Academic.Monitoring.View = "Permissions.Academic.Monitoring.View"`
     - `Permissions.Students.Children.View = "Permissions.Students.Children.View"`
   - Populate `RolePermissionCatalog` for `Roles.Parent` and `Roles.Student`, granting compile-time access to calendar, announcements, linked children, finalized report cards, billing statements, and own timetables.
   - Register permission policies in `ElsasaSis.Api/Program.cs`.
3. **Backend Read Slices Across Domain Modules:**
   - `ElsasaSis.Modules.Std`:
     - Read Slice `GetMyChildren` (`GET /api/std/students/my-children` — `UC-PRT-01` multi-child switcher).
   - `ElsasaSis.Modules.Opr`:
     - Read Slice `GetPersonalStudentTimetable` (`GET /api/opr/timetables/my` — `UC-STD-01` resolving SMK block rotations & today's agenda).
     - Audience scoping in `AnnouncementAccessPolicy.cs` and `GetAnnouncementsHandler.cs` enforcing `ALL` + `PARENTS` for parent users and `ALL` + `STUDENTS` for student users (`UC-PRT-04`, `UC-STD-03`).
   - `ElsasaSis.Modules.Acd`:
     - Read Slice `GetAcademicMonitoringProgress` (`GET /api/acd/monitoring/progress` — `UC-ADM-21` school-wide attendance, grading completeness, and report card readiness).
     - Read Slice `GetStudentReportCards` (`GET /api/acd/report-cards/student/{studentId}` — `UC-PRT-01`).
     - Scoped validation in `GetReportCardByIdHandler` (`GET /api/acd/report-cards/{id}`): Verify child-ownership for `PARENT` role and hide unfinalized draft cards (`UC-PRT-01`).
   - `ElsasaSis.Modules.Fin`:
     - Scoped validation in `GetStudentStatementHandler` (`GET /api/fin/students/{studentId}/statement`): Enforce child-ownership check for `PARENT` callers (`UC-PRT-02`).
     - Scoped validation in `GetPaymentReceiptByIdHandler` (`GET /api/fin/payments/{receiptId}`): Enforce child-ownership check for `PARENT` callers (`UC-PRT-03`).
4. **Frontend Consumer Shell Layout & Redirection (`src/app/` & `src/features/auth/`):**
   - Create `PortalLayout.vue`: Clean, responsive, mobile-first shell without the administrative sidebar, featuring top branding, user name, role badge, and sign-out action.
   - Dynamic post-login redirection in `LoginView.vue`, `routes.ts`, and `authNavigationGuard` routing `PARENT` to `/portal/parent`, `STUDENT` to `/portal/student`, and staff to `/operations/calendar`.
5. **Frontend Feature Workspaces (4 UI Screens):**
   - `src/features/portal-parent/` (`SCR-PRT-PORTAL` at `/portal/parent`): Parent Family Portal with multi-child tab selector and 3 content tabs (Academic Report Cards with browser print trigger, School Fees & Receipts with receipt print triggers, School Calendar & Bulletins).
   - `src/features/portal-student/` (`SCR-STD-PORTAL` at `/portal/student`): Student Learner Portal with student header, active SMK cycle badge, Today's Agenda timeline vs Weekly Timetable grid toggle, and Calendar/Announcements feed.
   - `src/features/academic/monitoring/` (`SCR-ACD-MONITORING` at `/academic/monitoring`): Executive academic monitoring dashboard with school-wide KPI metric cards, operational exception alert banners, and progressive drill-down tables.
   - `src/features/finance/receivables/` (`SCR-FIN-RECEIVABLES` at `/finance/receivables`): Validate and verify existing Stage 3 receivables and arrears aging dashboard against `UC-ADM-26`, `UC-PRC-06`, and `UC-FIN-09` without duplicate implementation.

## Excluded

The following capabilities belong to Enterprise tiers or future operational enhancements and are strictly excluded from Stage 4:

- **Third-Party Online Payment Gateways:** No automated Midtrans, Xendit, or QRIS payment gateway integrations. Parent portal displays billing status, outstanding balances, and official receipts issued by the frontline cashier counter.
- **Automated WhatsApp / SMS Notification Push Schedulers:** No automated dunning message pipelines or push notification engines.
- **Consumer Self-Service Write Mutations:** Parents and students have strictly read-only inspection access; they cannot modify grades, submit enrollment edits, or execute ad-hoc financial ledger adjustments.
- **Server-Side Headless PDF Generation Engines:** No backend Chromium/Puppeteer/wkhtmltopdf dependencies. Official report cards and receipts utilize browser-native print rendering (`window.print()`) via existing Vue print layouts.
- **Multi-Tenant Partitioning:** System operates as a single-school institution without `org_unit_id` database partitioning.

---

# 4. Technical Decisions

This section realizes all approved analysis decisions from the Feasibility Assessment (`GAP-001` through `GAP-013` and `OQ-001` through `OQ-010`) into concrete technical specifications.

### 4.1 Parent Multi-Child Identity & Scoping (`GAP-001`, `OQ-001`)

To cleanly support parent accounts linked to multiple enrolled children without mutating `std.student_guardians`, a new relationship mapping table `auth.user_students` is introduced in the `auth` schema:

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

**Cross-Module Authorization Guard Pattern:**
When an authenticated request from an actor with the `PARENT` role accesses any student-scoped data (in `Modules.Std`, `Modules.Acd`, or `Modules.Fin`), the read handler enforces child ownership via Dapper SQL:
```sql
WHERE student_id IN (SELECT student_id FROM auth.user_students WHERE user_id = @UserId)
```
If the requested `studentId` does not exist in the parent's authorized child set, the handler immediately aborts and returns an RFC 7807 `403 Forbidden` response (`DomainErrors.Auth.Forbidden("You do not have permission to view data for this student.")`).

### 4.2 RBAC Catalog & Permission Architecture (`GAP-006`, `GAP-007`, `OQ-003`)

1. **New Permissions Defined in `ElsasaSis.Modules.Auth/Permissions.cs`:**
   - `Permissions.Academic.Monitoring.View = "Permissions.Academic.Monitoring.View"`
   - `Permissions.Students.Children.View = "Permissions.Students.Children.View"`
   - Both are included in `Permissions.All` collection.
2. **`RolePermissionCatalog` Updates:**
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
3. **Endpoint Authorization:**
   Minimal API endpoints are mapped with `.RequireAuthorization(policy)` matching the compile-time permissions.

### 4.3 Parent Linked Children Query (`GAP-005`, `OQ-001`)

- **Component:** `ElsasaSis.Modules.Std/Features/Students/GetMyChildren/`
- **Endpoint:** `GET /api/std/students/my-children`
- **Protection:** `Permissions.Students.Children.View`
- **Execution:** Direct Dapper query joining `auth.user_students`, `std.students`, and `std.student_placements`:
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
- **Response Contract:** `Result<IReadOnlyList<LinkedChildDto>>`.

### 4.4 Personal Student Timetable & SMK Block Resolution (`GAP-004`, `OQ-004`)

- **Component:** `ElsasaSis.Modules.Opr/Features/Timetables/GetPersonalStudentTimetable/`
- **Endpoint:** `GET /api/opr/timetables/my?date={YYYY-MM-DD}`
- **Protection:** `Permissions.Operations.Timetables.ViewOwn`
- **Execution Flow:**
  1. Extract `studentId = ICurrentUser.StudentId`. If null, return `403 Forbidden`.
  2. Resolve active academic year and active operational period via `IOperationalContract.GetActiveOperationalPeriodAsync()`.
  3. Query `std.student_placements` for `student_id = @StudentId AND academic_year_id = @ActiveYearId` to obtain `study_group_id`, `study_group_name`, `school_level`, and `grade`. If unplaced, return an empty timetable with placement pending note.
  4. Query `opr.study_group_timetables` to resolve SMK vocational block rotation:
     ```sql
     SELECT schedule_type, start_date, end_date
     FROM opr.study_group_timetables
     WHERE study_group_id = @StudyGroupId
       AND operational_period_id = @PeriodId
       AND @QueryDate BETWEEN start_date AND end_date;
     ```
     - If an active block row exists: `activeScheduleType = schedule_type` (`GENERAL` or `VOCATIONAL`).
     - If no block row matches: `activeScheduleType = 'REGULAR'`.
  5. Query `opr.study_group_schedules` for all weekly slots matching `study_group_id = @StudyGroupId` and `operational_period_id = @PeriodId`.
  6. Compute `TodayAgenda`: Filter slots where `day_of_week = @TargetDayOfWeek` AND (`schedule_type = @ActiveScheduleType` OR `schedule_type = 'REGULAR'`), ordered by `period_slot ASC`.
  7. Compute `WeeklySchedule`: All slots for the active cycle, grouped by `day_of_week` (Monday through Friday/Saturday).
  8. Return composite DTO:
     ```json
     {
       "studentInfo": { "studentId": "...", "fullName": "...", "nis": "...", "studyGroupName": "...", "schoolLevel": "...", "grade": "..." },
       "activeCycle": { "scheduleType": "VOCATIONAL", "isSmkRotation": true, "startDate": "2026-11-17", "endDate": "2026-11-28" },
       "todayAgenda": [ ... ],
       "weeklySchedule": [ ... ]
     }
     ```

### 4.5 Parent Child Report Cards & Draft Isolation (`GAP-003`, `OQ-007`)

1. **New Read Slice in `ElsasaSis.Modules.Acd`:**
   - Component: `Features/ReportCards/GetStudentReportCards/`
   - Endpoint: `GET /api/acd/report-cards/student/{studentId}`
   - Protection: `Permissions.Academic.ReportCards.View`
   - Authorization & Draft Isolation:
     - If caller has `PARENT` role:
       - Validates that `studentId` exists in `SELECT student_id FROM auth.user_students WHERE user_id = @UserId`. If not, returns `403 Forbidden`.
       - Query applies: `WHERE rc.student_id = @StudentId AND rc.status = 'FINALIZED'`. (Drafts strictly hidden).
     - If caller has staff roles (`SCHOOL_ADMIN`, `PRINCIPAL`, `TEACHER`):
       - Allows viewing both `DRAFT` and `FINALIZED` report cards for the student.
2. **Update to Existing `GetReportCardByIdHandler` (`GET /api/acd/report-cards/{id}`):**
   - If caller has `PARENT` role:
     - Verifies report card `student_id` is linked to `@UserId` in `auth.user_students`.
     - Verifies `status == 'FINALIZED'`. If still in `DRAFT`, returns `404 NotFound` to prevent information leakage.
3. **Print Rendering:**
   Parent portal triggers `ReportCardPrintView.vue` with `window.print()` using CSS `@media print` print styling. Zero server-side PDF engine dependencies.

### 4.6 School-Wide Academic Progress Monitoring Engine (`GAP-002`, `OQ-005`)

- **Component:** `ElsasaSis.Modules.Acd/Features/Monitoring/GetAcademicMonitoringProgress/`
- **Endpoint:** `GET /api/acd/monitoring/progress?academicYearId={id}&operationalPeriodId={id}&date={YYYY-MM-DD}`
- **Protection:** `Permissions.Academic.Monitoring.View`
- **Execution:** Direct Dapper query in `GetAcademicMonitoringProgressHandler` computing 3 aggregate sections:
  1. **Overview KPIs:**
     - **Attendance Rate Today:** Count `PRESENT`, `ABSENT`, `SICK`, `LATE`, `EXCUSED`, total enrolled placed students, and percentage attended for the queried date from `acd.attendance_records`.
     - **Grading Completeness:** Count total assignments in operational period, expected scores (`total_assignments * enrolled_students`), actual recorded scores in `acd.assignment_scores`, and completion percentage.
     - **Report Card Progress:** Count total placed students, count `DRAFT` report cards, count `FINALIZED` report cards, and percentage finalized for the operational period from `acd.report_cards`.
  2. **Operational Exception Alerts:**
     - **Missing Attendance Today:** Study groups with 0 attendance records logged for `@Date`.
     - **Overdue Grading Subjects:** Assignments created $> 7$ days ago where `COUNT(scores) < COUNT(enrolled_students)`.
     - **Pending Report Cards:** Study groups with unfinalized `DRAFT` report cards within 14 days of period end.
  3. **Study Group Drill-Down Rows:**
     - Tabular matrix per study group: `Level`, `Grade`, `StudyGroupName`, `HomeroomTeacherName`, `AttendanceTodayRate`, `AssignmentsCount`, `GradingPercentage`, `ReportCardsFinalizedRatio`.

### 4.7 Finance Child Data Scoping (`GAP-012`, `OQ-009`)

In `ElsasaSis.Modules.Fin`:
1. **`GetStudentStatementHandler` (`GET /api/fin/students/{studentId}/statement`):**
   - When caller has `PARENT` role, verify child linkage:
     ```sql
     IF NOT EXISTS (SELECT 1 FROM auth.user_students WHERE user_id = @UserId AND student_id = @StudentId)
         RETURN 403 Forbidden;
     ```
2. **`GetPaymentReceiptByIdHandler` (`GET /api/fin/payments/{receiptId}`):**
   - When caller has `PARENT` role, verify receipt belongs to an authorized linked child:
     ```sql
     AND EXISTS (
         SELECT 1 FROM auth.user_students us
         WHERE us.user_id = @UserId
           AND us.student_id = r.student_id
     )
     ```
     If not matching, return `403 Forbidden`.
3. **Receipt Print Rendering:**
   Parent portal triggers `PaymentReceiptPrintView.vue` with `window.print()` using CSS `@media print` print styling.

### 4.8 Announcements Audience Scoping (`GAP-008`, `OQ-008`)

Update `AnnouncementAccessPolicy.cs` and `GetAnnouncementsHandler.cs` in `ElsasaSis.Modules.Opr`:
- When caller has role `PARENT` (and does not hold staff roles):
  - Query condition added: `AND a.target_audience IN ('ALL', 'PARENTS')`
  - Enforce `a.status = 'PUBLISHED'`
- When caller has role `STUDENT` (and does not hold staff roles):
  - Query condition added: `AND a.target_audience IN ('ALL', 'STUDENTS')`
  - Enforce `a.status = 'PUBLISHED'`

### 4.9 Frontend Shell Layout, Post-Login Redirection & Navigation (`GAP-009`, `GAP-010`, `GAP-011`, `OQ-006`)

1. **`PortalLayout.vue` (`src/app/layouts/PortalLayout.vue`):**
   - Dedicated mobile-first authenticated shell without the administrative sidebar.
   - Top Navigation Bar: Application Logo ("ELSASA-SIS"), Role Badge ("Portal Orang Tua" / "Portal Siswa"), User Display Name, and Sign-out button.
   - Centered container (`max-w-5xl mx-auto px-4 py-6`) optimized for tablet and mobile touch viewports.
2. **Post-Login & Route Guard Redirection Engine:**
   - Update `LoginView.vue`, `src/app/router/index.ts` (`authNavigationGuard`), and `routes.ts`:
     - If authenticated user has role `PARENT` $\rightarrow$ redirect to `/portal/parent`.
     - If authenticated user has role `STUDENT` $\rightarrow$ redirect to `/portal/student`.
     - Otherwise (staff roles) $\rightarrow$ redirect to `/operations/calendar`.
   - Prevent 403 Forbidden dead-ends on initial sign-in.
3. **Parent Family Portal (`src/features/portal-parent/views/ParentPortalView.vue` — `SCR-PRT-PORTAL`):**
   - Multi-child switcher: Horizontal pill/tab selector displaying each child's name, grade, and study group.
   - 3 Feature Tabs:
     - **Tab 1: Rapor Akademik (Academic Report Cards):** Displays finalized semester report cards with overall performance, homeroom teacher remarks, and "Unduh Rapor PDF" button (invoking `ReportCardPrintView.vue`).
     - **Tab 2: Keuangan & SPP (Fees & Billing):** Monthly tuition obligation status chips (`PAID`, `PARTIALLY_PAID`, `UNPAID`), discounts applied, advance deposit balance banner, and historical payment receipt table with "Cetak Kuitansi" buttons (`PaymentReceiptPrintView.vue`).
     - **Tab 3: Info Sekolah (Calendar & Bulletins):** Published parent announcements and active school calendar entries.
4. **Student Learner Portal (`src/features/portal-student/views/StudentPortalView.vue` — `SCR-STD-PORTAL`):**
   - Header Banner: Student NIS, Full Name, Study Group, and School Level.
   - Active SMK Rotation Badge: Displays whether the current week is "Siklus Kejuruan (Vocational)" or "Siklus Umum (General)" with date validity range.
   - Schedule View Switcher:
     - **Agenda Hari Ini:** Chronological card list of today's remaining classes (time slot, subject, teacher name, room).
     - **Jadwal Mingguan:** Clean responsive weekly period-by-day timetable grid.
   - Secondary Bulletin Feed: School calendar dates, holidays, and student announcements.
5. **School-Wide Academic Monitoring (`src/features/academic/monitoring/views/AcademicMonitoringView.vue` — `SCR-ACD-MONITORING`):**
   - Registered under `AppLayout.vue` at `/academic/monitoring`.
   - Top Filter Bar: Academic Year, Operational Period, and Date picker (defaults to today).
   - Executive Metric Cards: School Attendance Today %, Assessment Grading %, Report Cards Finalized %.
   - Operational Exception Alerts: Missing attendance alert banner, overdue grading alert banner, unfinalized report cards banner.
   - Drill-Down Data Table: Filterable by school level, grade, and study group.

### 4.10 Database Migrations & Test Account Seeding (`GAP-013`, `OQ-010`)

1. **Migration `004_CreateAuthUserStudents.sql`:**
   - Defines `auth.user_students` table and non-clustered index on `student_id`.
2. **Seed Migration `102_SeedStage4Data.sql`:**
   - Creates parent account `parent.hendra` (`Hendra Dahlan`, password `Parent123!`, PBKDF2 Identity v3 hash), assigned role `PARENT`, linked in `auth.user_students` to:
     - Ahmad Dahlan (`01A1F11A-7010-7010-8000-000000000010`)
     - Siti Nurhaliza (`01A1F11A-7011-7011-8000-000000000011`)
   - Creates student account `student.ahmad` (`Ahmad Dahlan`, password `Student123!`), assigned role `STUDENT`, linked in `auth.user_actors.student_id` to Ahmad Dahlan.
   - Creates student account `student.siti` (`Siti Nurhaliza`, password `Student123!`), assigned role `STUDENT`, linked in `auth.user_actors.student_id` to Siti Nurhaliza.

---

# 5. Component Responsibilities

| Component | Assembly / Path | Responsibility & Ownership |
|---|---|---|
| **`BND-MOD-AUTH`** | `ElsasaSis.Modules.Auth` | Owns `auth.user_students` table schema definition in migration `004`, `Permissions.cs` constants, `RolePermissionCatalog.cs` mappings for `PARENT` and `STUDENT`, and JWT claims emission. |
| **`BND-MOD-STD`** | `ElsasaSis.Modules.Std` | Owns `GetMyChildren` Read Slice (`GET /api/std/students/my-children` — `UC-PRT-01`). |
| **`BND-MOD-OPR`** | `ElsasaSis.Modules.Opr` | Owns `GetPersonalStudentTimetable` Read Slice (`GET /api/opr/timetables/my` — `UC-STD-01`), and audience filtering in `GetAnnouncementsHandler` and `AnnouncementAccessPolicy` (`UC-PRT-04`, `UC-STD-03`). |
| **`BND-MOD-ACD`** | `ElsasaSis.Modules.Acd` | Owns `GetAcademicMonitoringProgress` Read Slice (`GET /api/acd/monitoring/progress` — `UC-ADM-21`), `GetStudentReportCards` Read Slice (`GET /api/acd/report-cards/student/{studentId}` — `UC-PRT-01`), and child-linkage / draft-hiding guards on `GetReportCardByIdHandler`. |
| **`BND-MOD-FIN`** | `ElsasaSis.Modules.Fin` | Owns child-linkage guards on `GetStudentStatementHandler` (`UC-PRT-02`) and `GetPaymentReceiptByIdHandler` (`UC-PRT-03`). |
| **`BND-DATABASE`** | `ElsasaSis.Database` | Owns migration scripts `004_CreateAuthUserStudents.sql` and `102_SeedStage4Data.sql`. |
| **`BND-API-HOST`** | `ElsasaSis.Api` | Minimal API endpoint mapping, authorization policy registrations, and OpenAPI Swagger documentation. |
| **`ElsasaSis.ArchitectureTests`** | Tests | NetArchTest verification enforcing module boundary isolation with zero forbidden cross-module project references. |
| **`BND-FE-SHELL`** | `src/app/layouts/` & `src/app/router/` | Owns `PortalLayout.vue`, role-based post-login redirection in `LoginView.vue`, routes definition, and `authNavigationGuard`. |
| **`BND-FE-PRT`** | `src/features/portal-parent/` | Owns `SCR-PRT-PORTAL` (`ParentPortalView.vue`): Multi-child switcher, report card viewer, fee statement viewer, receipt viewer, and school bulletin. |
| **`BND-FE-STD`** | `src/features/portal-student/` | Owns `SCR-STD-PORTAL` (`StudentPortalView.vue`): Student learner portal, SMK rotation cycle badge, Today's Agenda, and Weekly Timetable grid. |
| **`BND-FE-ACD-MON`** | `src/features/academic/monitoring/` | Owns `SCR-ACD-MONITORING` (`AcademicMonitoringView.vue`): Institutional academic oversight, KPI metric tiles, exception alerts, and study group drill-down table. |
| **`BND-FE-FIN-RECV`** | `src/features/finance/receivables/` | Owns `SCR-FIN-RECEIVABLES` (`ReceivablesAgingView.vue`): Financial receivables summary, aged arrears table, and daily collection reports (verified from Stage 3). |

---

# 6. Integration Design

| Source | Target | Transport | Purpose |
|---|---|---|---|
| Frontend (`features/portal-parent`) | `BND-API-HOST` (`/api/std/students/my-children`) | HTTP REST (JSON) | Fetch list of authorized enrolled children for the logged-in parent actor. |
| Frontend (`features/portal-parent`) | `BND-API-HOST` (`/api/acd/report-cards/student/{studentId}`) | HTTP REST (JSON) | Fetch finalized report cards for the selected child. |
| Frontend (`features/portal-parent`) | `BND-API-HOST` (`/api/fin/students/{studentId}/statement`) | HTTP REST (JSON) | Fetch monthly tuition billing ledger and advance deposit balance for the selected child. |
| Frontend (`features/portal-parent`) | `BND-API-HOST` (`/api/fin/payments/{receiptId}`) | HTTP REST (JSON) | Fetch official cashier payment receipt detail and allocation breakdown for printing. |
| Frontend (`features/portal-parent`) | `BND-API-HOST` (`/api/opr/announcements`) | HTTP REST (JSON) | Fetch announcements filtered to `ALL` and `PARENTS`. |
| Frontend (`features/portal-parent`) | `BND-API-HOST` (`/api/opr/calendar/entries`) | HTTP REST (JSON) | Fetch active school operational calendar entries and holidays. |
| Frontend (`features/portal-student`) | `BND-API-HOST` (`/api/opr/timetables/my`) | HTTP REST (JSON) | Fetch personal weekly schedule and Today's Agenda resolving SMK block rotations. |
| Frontend (`features/portal-student`) | `BND-API-HOST` (`/api/opr/calendar/entries`) | HTTP REST (JSON) | Fetch school calendar entries and closures. |
| Frontend (`features/portal-student`) | `BND-API-HOST` (`/api/opr/announcements`) | HTTP REST (JSON) | Fetch announcements filtered to `ALL` and `STUDENTS`. |
| Frontend (`features/academic/monitoring`) | `BND-API-HOST` (`/api/acd/monitoring/progress`) | HTTP REST (JSON) | Fetch institutional attendance %, grading completeness %, report card progress %, and exceptions. |
| Frontend (`features/finance/receivables`) | `BND-API-HOST` (`/api/fin/reports/*`) | HTTP REST (JSON) | Fetch receivables aging summaries and daily cashier collection registers. |
| `BND-MOD-STD` (`GetMyChildren`) | `auth.user_students`, `std.students`, `std.student_placements` | SQL Query (Dapper) | Cross-schema query projecting child profiles and active placements. |
| `BND-MOD-OPR` (`GetPersonalStudentTimetable`) | `std.student_placements`, `opr.study_group_timetables`, `opr.study_group_schedules` | SQL Query (Dapper) | Cross-schema query resolving student study group and active SMK block rotation slots. |
| `BND-MOD-ACD` (`GetAcademicMonitoringProgress`) | `std.student_placements`, `acd.attendance_records`, `acd.assignments`, `acd.assignment_scores`, `acd.report_cards` | SQL Query (Dapper) | Cross-schema query aggregating institutional completion metrics and drill-down rows. |
| `BND-MOD-ACD` (`GetStudentReportCards`) | `auth.user_students`, `acd.report_cards` | SQL Query (Dapper) | Scoped query validating parent authorization and returning finalized report cards. |
| `BND-MOD-FIN` (`GetStudentStatement`, `GetPaymentReceiptById`) | `auth.user_students`, `fin.student_charges`, `fin.payment_receipts` | SQL Query (Dapper) | Scoped query validating parent child-ownership before projecting statements or receipts. |

---

# 7. Data Ownership

| Schema / Table | Owning Module | Persistence Responsibility |
|---|---|---|
| `auth.user_students` | `ElsasaSis.Modules.Auth` | Exclusive write ownership via auth repository or migration seed. Cross-schema read access permitted for CQRS read slices. |
| `auth.users`, `auth.user_roles`, `auth.user_actors` | `ElsasaSis.Modules.Auth` | Exclusive write ownership by `Modules.Auth`. |
| `std.students`, `std.student_placements` | `ElsasaSis.Modules.Std` | Exclusive write ownership by `Modules.Std`. |
| `opr.study_group_timetables`, `opr.study_group_schedules`, `opr.announcements` | `ElsasaSis.Modules.Opr` | Exclusive write ownership by `Modules.Opr`. |
| `acd.attendance_records`, `acd.assignments`, `acd.assignment_scores`, `acd.report_cards` | `ElsasaSis.Modules.Acd` | Exclusive write ownership by `Modules.Acd`. |
| `fin.student_charges`, `fin.payment_receipts`, `fin.payment_allocations` | `ElsasaSis.Modules.Fin` | Exclusive write ownership by `Modules.Fin`. |

---

# 8. Database Design

Detailed schemas adhere strictly to Pure Persistence Layer discipline: UUID v7 primary keys, UTC application-supplied timestamps, zero database foreign keys across schemas, and zero database triggers, defaults, or check constraints.

## New Tables

| Table | Schema | Script | Purpose & Key Columns |
|---|---|---|---|
| `auth.user_students` | `auth` | `004` | Maps parent user accounts to 1..N enrolled student records. PK: `(user_id, student_id)`, `created_at` (DATETIME2(3)). Clustered PK on `(user_id, student_id)`, non-clustered index on `student_id`. |

## Modified Tables

Zero existing tables modified. All read slices project directly from existing Stage 0 through Stage 3 relational tables.

## Relationships

Architecturally significant relationships introduced or utilized:
- `auth.user_students.user_id` $\rightarrow$ `auth.users.id` (FK in `auth` schema).
- `auth.user_students.student_id` $\rightarrow$ `std.students.id` (logical cross-schema UUID reference, no database FK).
- `opr.study_group_timetables.study_group_id` $\rightarrow$ `std.student_placements.study_group_id` (logical matching on active cycle).
- `acd.report_cards.student_id` $\rightarrow$ `auth.user_students.student_id` (logical child authorization check).
- `fin.student_charges.student_id` $\rightarrow$ `auth.user_students.student_id` (logical child authorization check).
- `fin.payment_receipts.student_id` $\rightarrow$ `auth.user_students.student_id` (logical child authorization check).

## Migration Considerations

1. **`004_CreateAuthUserStudents.sql`:**
   - Idempotent script using `IF NOT EXISTS` check.
   - Creates `auth.user_students` table and index `IX_auth_user_students_student_id`.
2. **`102_SeedStage4Data.sql`:**
   - Idempotent seed script checking `IF NOT EXISTS` on `username`:
     - Inserts parent `parent.hendra` with PBKDF2 hash for password `Parent123!`.
     - Assigns role `PARENT` in `auth.user_roles`.
     - Maps in `auth.user_students` to Ahmad Dahlan (`01A1F11A-7010-7010-8000-000000000010`) and Siti Nurhaliza (`01A1F11A-7011-7011-8000-000000000011`).
     - Inserts student `student.ahmad` with PBKDF2 hash for password `Student123!`, assigns role `STUDENT`, and links `auth.user_actors.student_id`.
     - Inserts student `student.siti` with PBKDF2 hash for password `Student123!`, assigns role `STUDENT`, and links `auth.user_actors.student_id`.

---

# 9. Cross-Cutting Concerns

- **Role-Based Access Control & Strict Data Isolation:**
  - Multi-child privacy: Parent callers can only retrieve student profiles, report cards, billing statements, and payment receipts matching their linked children in `auth.user_students`.
  - Academic Draft Hiding: `acd.report_cards` with `status = 'DRAFT'` are completely filtered out from parent queries. If a parent requests an unfinalized card directly by ID, the system returns an RFC 7807 `404 Not Found`.
  - Student Timetable Scoping: Student callers can only query the timetable belonging to their placed study group derived from their authenticated `student_id`.
  - Announcement Audience Scoping: Parents receive only `ALL` and `PARENTS` announcements; students receive only `ALL` and `STUDENTS` announcements.
- **Performance & Dapper Aggregation:**
  - Academic monitoring query (`GET /api/acd/monitoring/progress`) aggregates attendance, grading completeness, and report cards in a single round-trip. All joins execute against indexed columns (`operational_period_id`, `academic_year_id`, `attendance_date`, `study_group_id`), completing in $< 50\text{ ms}$ on SQL Server.
- **Pure Persistence Layer SQL Discipline:**
  - Zero database foreign keys across schemas, zero stored procedures, zero triggers, and zero default/check constraints.
  - All primary keys use `UuidV7.NewGuid()`. All timestamps supplied in UTC (`DateTime.UtcNow`).
- **Error Handling (RFC 7807):**
  - Unauthenticated requests return `401 Unauthorized`.
  - Cross-child access violations return `403 Forbidden` (`DomainErrors.Auth.Forbidden`).
  - Access to unfinalized report cards by parents returns `404 Not Found`.
- **UI Ergonomics & Responsive Shell Architecture:**
  - Consumer portals utilize `PortalLayout.vue` adhering to Tier 1 Mobile-First responsiveness (minimum $44\text{px} \times 44\text{px}$ touch targets, responsive horizontal child switcher tabs, pull-to-refresh ergonomics).
  - Executive monitoring dashboards utilize `AppLayout.vue` adhering to Tier 2 Tablet & Laptop Adaptive layout with multi-column KPI grids and progressive drill-down tables.

---

# 10. Implementation Constraints

1. **Strict Assembly Isolation (NetArchTest):**
   - No module assembly may reference another module assembly directly. Modules reference strictly `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`.
2. **Pure Persistence Layer SQL Discipline:**
   - Zero database foreign keys across schemas, stored procedures, triggers, default constraints, or check constraints.
   - All primary keys must use `UuidV7.NewGuid()`.
   - All timestamps must be supplied in UTC by C# code.
3. **CQRS Read Slice Discipline:**
   - All 4 new endpoints are Read Slices executing direct Dapper projections into response DTOs. Handlers must not instantiate Domain Aggregate Roots or call write repositories.
   - Read Slices may execute cross-schema `JOIN` queries within the handler SQL per `backend-architecture.md` §11.2.
4. **No Server-Side Headless PDF Libraries:**
   - Official report cards and receipts must be rendered in Vue with CSS `@media print` print stylesheets. No Puppeteer, Chromium, or wkhtmltopdf backend dependencies.
5. **Frontend State & Typing Discipline:**
   - Server state managed strictly via TanStack Query; client auth context via Pinia; form state via VeeValidate + Zod.
   - Generated TypeScript contracts (`openapi-typescript`) must drive all API DTO consumption.
6. **No 403 Forbidden on Initial Sign-In:**
   - `LoginView.vue` and `authNavigationGuard` must inspect the authenticated user's roles and route `PARENT` to `/portal/parent` and `STUDENT` to `/portal/student`, avoiding redirection to administrative paths.

---

# 11. Acceptance Conditions

Stage 4 implementation is architecturally complete when all of the following conditions hold:

1. **Solution & Architecture Verification:**
   - `src/backend/ElsasaSis.sln` builds cleanly under `.NET 8` (`net8.0`).
   - `ElsasaSis.ArchitectureTests` passes all NetArchTest boundary isolation rules with zero forbidden module cross-references.
2. **Database Migrations & Seeding:**
   - DbUp executes migration `004_CreateAuthUserStudents.sql` creating `auth.user_students` and `IX_auth_user_students_student_id`.
   - DbUp executes seed script `102_SeedStage4Data.sql` provisioning `parent.hendra` (linked to Ahmad Dahlan and Siti Nurhaliza), `student.ahmad`, and `student.siti`.
3. **Parent Family Portal (`portal-parent`) Verification:**
   - `UC-PRT-01`: Parent authenticates as `parent.hendra` and sees multi-child tabs for Ahmad Dahlan and Siti Nurhaliza. Selecting a child displays their finalized semester report cards with letter grades and homeroom remarks. Clicking "Unduh Rapor PDF" launches `ReportCardPrintView.vue` with `window.print()`. Draft report cards are strictly hidden (inaccessible and returning 404). Attempting to query an unlinked student returns 403 Forbidden.
   - `UC-PRT-02`: Parent can view the child's monthly tuition billing ledger, applied discounts, payment status chips, and advance deposit balance.
   - `UC-PRT-03`: Parent can view historical payment transactions and trigger the official printable receipt view (`PaymentReceiptPrintView.vue`). Attempting to view a receipt for an unlinked student returns 403 Forbidden.
   - `UC-PRT-04`: Parent can view active school calendar entries and published announcements targeted to `ALL` and `PARENTS`. Announcements targeted to `STAFF` or `STUDENTS` are hidden.
4. **Student Learner Portal (`portal-student`) Verification:**
   - `UC-STD-01`: Student authenticates as `student.ahmad` and is routed to `/portal/student`. The view displays student information, active SMK block cycle badge ("Siklus Kejuruan" or "Siklus Umum"), Today's Agenda list for the current day, and a Weekly Timetable grid accurately reflecting active block slots.
   - `UC-STD-02`: Student can inspect the official school calendar and holiday schedule.
   - `UC-STD-03`: Student can view published announcements targeted to `ALL` and `STUDENTS`. Announcements targeted to `PARENTS` or `STAFF` are hidden.
5. **School-Wide Academic Progress Monitoring (`acd`) Verification:**
   - `UC-ADM-21`: Administrator or Principal navigates to `/academic/monitoring` and views real-time attendance rate today %, assessment score entry completeness %, and report card finalization %. Operational exception alerts surface missing attendance, overdue grading, and unfinalized report cards. Drill-down table supports filtering by school level, grade, and study group.
6. **Financial Governance & Receivables Monitoring (`fin`) Verification:**
   - `UC-ADM-26`, `UC-PRC-06`, `UC-FIN-09`: Administrator, Principal, and Finance Officer can navigate to `/finance/receivables` and inspect receivables summaries, study group arrears breakdowns, collection registers, and student billing statement drawers.
7. **Frontend Shell & Redirection Quality:**
   - `pnpm typecheck`, `pnpm lint`, and unit tests pass with zero errors.
   - Authenticating as `parent.hendra` redirects automatically to `/portal/parent` under `PortalLayout.vue`.
   - Authenticating as `student.ahmad` redirects automatically to `/portal/student` under `PortalLayout.vue`.
   - Authenticating as `admin` redirects automatically to `/operations/calendar` under `AppLayout.vue`.
   - Consumer portals render responsively on mobile viewports ($360\text{px}+$) with touch-friendly navigation.
