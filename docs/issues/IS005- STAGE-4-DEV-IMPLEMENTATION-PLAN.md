---
Title: Stage 4 Consumer Portals & Executive Governance — Implementation Plan
Code: CR-STAGE-4
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-20
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Deliver an executable, dependency-driven implementation plan for **CR-STAGE-4** (**Stage 4: Consumer Portals & Executive Governance** of **ELSASA-SIS v4 Basic Level**), transforming the completed Stage 3 codebase (`src/backend` and `src/frontend`) into the approved Stage 4 target technical realization across **11 actor-driven use cases** and **4 UI screens** (2 dedicated consumer self-service portals and 2 executive governance dashboards).

Planning Mode: `FEATURE-PLANNING` (scoping `CR-STAGE-4` Stage 4 Consumer Portals & Executive Governance over the completed Stage 0, Stage 1, Stage 2, and Stage 3 baseline).

Referenced artifacts:

- ISSUE: [IS005- STAGE-4-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS005-%20STAGE-4-DEV-ISSUE.md) (`CR-STAGE-4`)
- FEASIBILITY-ASSESSMENT: [IS005- STAGE-4-DEV-FEASIBILITY-ASSESSMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS005-%20STAGE-4-DEV-FEASIBILITY-ASSESSMENT.md) (`Status: READY-FOR-PLANNING`; Decisions `GAP-001` through `GAP-013` and `OQ-001` through `OQ-010` binding and closed)
- ARCHITECTURE: [IS005- STAGE-4-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS005-%20STAGE-4-DEV-ARCHITECTURE.md) (`Version: 1.0`)
- Supporting Baseline Specifications:
  - [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
  - [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
  - [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
  - [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)
  - [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md)
  - [finance-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/finance-tables-design.md)
  - [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md)
  - [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md)
  - [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md)
  - [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md)
  - [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md)

Architecture Applicability: `ARCHITECTURE-REQUIRED` — Target architecture defined in [IS005- STAGE-4-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS005-%20STAGE-4-DEV-ARCHITECTURE.md).

---

# 2. Planning Scope

Scope covers all Stage 4 deliverables across two strict repository boundaries (`src/backend` and `src/frontend`):

1. **Backend (`src/backend`):**
   - Database Migrations (`ElsasaSis.Database`):
     - Migration script `004_CreateAuthUserStudents.sql`: Create mapping table `auth.user_students` with clustered primary key `(user_id, student_id)` and non-clustered index on `student_id`.
     - Seed script `102_SeedStage4Data.sql`: Deterministic seed fixtures for `Roles.Parent` (`parent.hendra` linked to 2 children: Ahmad Dahlan and Siti Nurhaliza) and `Roles.Student` (`student.ahmad`, `student.siti`).
   - Identity, RBAC & Permissions (`ElsasaSis.Modules.Auth` & `ElsasaSis.Api`):
     - Define compile-time permissions `Permissions.Academic.Monitoring.View` and `Permissions.Students.Children.View`.
     - Update `RolePermissionCatalog` granting explicit compile-time permissions for `Roles.Parent`, `Roles.Student`, `Roles.Principal`, and `Roles.SchoolAdmin`.
     - Register authorization policies in `ElsasaSis.Api/Program.cs`.
   - CQRS Read Slices Across Domain Modules:
     - `ElsasaSis.Modules.Std`: Read Slice `GetMyChildren` (`GET /api/std/students/my-children` — `UC-PRT-01` multi-child switcher).
     - `ElsasaSis.Modules.Opr`: Read Slice `GetPersonalStudentTimetable` (`GET /api/opr/timetables/my` — `UC-STD-01` resolving SMK block rotations & today's agenda); audience filtering in `AnnouncementAccessPolicy.cs` and `GetAnnouncementsHandler.cs` enforcing `ALL` + `PARENTS` for parent users and `ALL` + `STUDENTS` for student users (`UC-PRT-04`, `UC-STD-03`).
     - `ElsasaSis.Modules.Acd`: Read Slice `GetAcademicMonitoringProgress` (`GET /api/acd/monitoring/progress` — `UC-ADM-21` school-wide attendance, grading completeness, and report card readiness); Read Slice `GetStudentReportCards` (`GET /api/acd/report-cards/student/{studentId}` — `UC-PRT-01`); scoped validation in `GetReportCardByIdHandler` (`GET /api/acd/report-cards/{id}`) enforcing child ownership and hiding unfinalized draft report cards for parents.
     - `ElsasaSis.Modules.Fin`: Child-ownership validation guards on `GetStudentStatementHandler` (`GET /api/fin/students/{studentId}/statement` — `UC-PRT-02`) and `GetPaymentReceiptByIdHandler` (`GET /api/fin/payments/{receiptId}` — `UC-PRT-03`); verification of existing receivables oversight queries (`UC-ADM-26`, `UC-PRC-06`, `UC-FIN-09`).
   - Architecture & Integration Tests:
     - NetArchTest boundary isolation checks in `ElsasaSis.ArchitectureTests` verifying zero forbidden module cross-references and CQRS read slice compliance.
     - Integration test suites in `ElsasaSis.IntegrationTests` verifying parent multi-child scoping, report card draft hiding, student timetable SMK block resolution, academic monitoring aggregation, and announcement audience filtering.

2. **Frontend (`src/frontend`):**
   - Shell, Layout & Redirection:
     - Create `PortalLayout.vue` (`src/app/layouts/PortalLayout.vue`): Mobile-first authenticated shell without administrative sidebar, featuring top branding, user name, role badge, and sign-out button.
     - Role-based post-login redirection in `LoginView.vue`, `routes.ts`, and `authNavigationGuard` in `src/app/router/index.ts` routing `PARENT` to `/portal/parent`, `STUDENT` to `/portal/student`, and staff to `/operations/calendar`.
     - Register `Academic Progress Monitoring` in `AppLayout.vue` sidebar under `Academic & Curriculum`.
   - Feature Submodules & Screens:
     - `SCR-PRT-PORTAL` (`src/features/portal-parent/` at `/portal/parent`): Parent Family Portal with multi-child tab selector and 3 feature tabs: Academic Report Cards with browser print trigger, Fees & Receipts with receipt print triggers, and School Calendar & Bulletins.
     - `SCR-STD-PORTAL` (`src/features/portal-student/` at `/portal/student`): Student Learner Portal with student header, active SMK cycle badge, Today's Agenda timeline vs Weekly Timetable grid toggle, and Calendar/Announcements feed.
     - `SCR-ACD-MONITORING` (`src/features/academic/monitoring/` at `/academic/monitoring`): Executive academic monitoring dashboard with school-wide KPI metric cards, operational exception alert banners, and progressive drill-down tables.
     - `SCR-FIN-RECEIVABLES` (`src/features/finance/receivables/` at `/finance/receivables`): Validate and verify existing Stage 3 receivables and arrears aging dashboard against `UC-ADM-26`, `UC-PRC-06`, and `UC-FIN-09` without duplicate implementation.
   - OpenAPI Synchronization:
     - Export updated OpenAPI spec and regenerate TypeScript API contracts in `src/shared/types/api/schema.d.ts`.
   - End-to-End Live Stack Multi-Role Validation:
     - Multi-role live walkthrough across `parent.hendra`, `student.ahmad`, and `admin`/`principal`, with responsive mobile viewport verification.

3. **Strictly Excluded from Stage 4:**
   - Third-party online payment gateways (Midtrans, Xendit, QRIS) — manual cashier counter operations only.
   - Automated WhatsApp / SMS notification push schedulers or dunning pipelines.
   - Consumer self-service write mutations (parents and students have strictly read-only inspection access).
   - Server-side headless PDF generation engines (Puppeteer, wkhtmltopdf) — clean browser printing with CSS `@media print` only.
   - Multi-tenant database partitioning (`org_unit_id`).

---

# 3. Dependencies

External/Environment prerequisites verified in the Feasibility Assessment:

- `.NET 8 SDK` (`net8.0`)
- `Node.js` (`v22+`) and `pnpm`
- Local Microsoft SQL Server (`MSSQLSERVER` / `(localdb)\MSSQLLocalDB`)
- Stage 3 baseline prerequisite: [IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS004-%20STAGE-3-DEV-IMPLEMENTATION-PLAN.md) (`COMPLETED`).

For slice dependencies:

- `Depends On` declares implementation prerequisites.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation status `IMPLEMENTED` and verifiable repository output.
- Dependency satisfaction does not require review status `GO`.
- Dependencies must represent real implementation prerequisites.
- Each slice targets strictly one repository boundary (`src/backend` or `src/frontend`), never both.

### Parallel Execution Wave Summary *(Informational Only — `Depends On` Is Authoritative)*

```text
Wave 1 (Database Migrations & Frontend Shell Setup):
  P1-S01 (src/backend: Migrations & Seeds)  ||  P1-S03 (src/frontend: PortalLayout & Redirection)

Wave 2 (Auth, RBAC & Permissions):
  P1-S02 (src/backend, depends on P1-S01)

Wave 3 (Concurrent Backend Read Slices across 4 Modules):
  P2-S04 (Std GetMyChildren)  ||  P2-S05 (Opr Timetable & Bulletins)  ||  P2-S06 (Acd Monitoring & ReportCards)  ||  P2-S07 (Fin Scoping)
  *(all src/backend, strictly disjoint module assemblies and schemas)*

Wave 4 (Backend Architecture & Integration Tests):
  P3-S08 (src/backend, depends on P2-S04, P2-S05, P2-S06, P2-S07)

Wave 5 (OpenAPI & TypeScript Client Synchronization):
  P3-S09 (src/frontend, depends on P3-S08)

Wave 6 (Concurrent Frontend Feature Screens across 4 Workspaces):
  P4-S10 (Parent Portal)  ||  P4-S11 (Student Portal)  ||  P4-S12 (Academic Monitoring)  ||  P4-S13 (Finance Receivables)
  *(all 4 screens in src/frontend, strictly disjoint feature folders, depending on P1-S03 and P3-S09)*

Wave 7 (Live Multi-Role Verification & Full System Validation):
  P5-S14 (src/frontend, depends on P4-S10, P4-S11, P4-S12, P4-S13)
```

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
| P1 - Foundation, Database Migrations, Auth & Shell Setup | IMPLEMENTED | GO | 3/3 |
| P2 - Backend Read Slices & Scoped Data Isolation | IMPLEMENTED | GO | 4/4 |
| P3 - Architecture Verification, Testing & OpenAPI Sync | IMPLEMENTED | GO | 2/2 |
| P4 - Frontend Consumer Portals & Executive Dashboards | IMPLEMENTED | GO | 4/4 |
| P5 - Live Multi-Role System Verification & End-to-End Validation | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Foundation, Database Migrations, Auth & Shell Setup

Implementation Status: IMPLEMENTED  
Review Status: GO

### P1-S01

Title: Database Migrations (`004_CreateAuthUserStudents.sql`) and Deterministic Test Fixtures Seed Script (`102_SeedStage4Data.sql`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Create and execute the idempotent DbUp migration script introducing the `auth.user_students` mapping table and the deterministic test fixture seed script provisioning parent (`parent.hendra`) and student (`student.ahmad`, `student.siti`) accounts linked to enrolled student master records.

Depends On: None

Repository: src/backend

Completion Criteria:

1. Create `src/backend/src/ElsasaSis.Database/Scripts/004_CreateAuthUserStudents.sql`:
   - Idempotent script using `IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.user_students') AND type = N'U')`.
   - Table definition:
     - `user_id UNIQUEIDENTIFIER NOT NULL`
     - `student_id UNIQUEIDENTIFIER NOT NULL`
     - `created_at DATETIME2(3) NOT NULL`
     - `CONSTRAINT PK_auth_user_students PRIMARY KEY CLUSTERED (user_id, student_id)`
     - `CONSTRAINT FK_auth_user_students_users FOREIGN KEY (user_id) REFERENCES auth.users (id)`
   - Non-clustered index:
     - `CREATE NONCLUSTERED INDEX IX_auth_user_students_student_id ON auth.user_students (student_id);`
   - Pure Persistence Layer discipline: Zero cross-schema foreign keys (logical UUID reference to `std.students.id`), zero triggers, zero database defaults.
2. Create `src/backend/src/ElsasaSis.Database/Scripts/102_SeedStage4Data.sql`:
   - Idempotent script checking `IF NOT EXISTS` on `username` in `auth.users`.
   - Parent Account:
     - User: `parent.hendra` (`Hendra Dahlan`, email `hendra.dahlan@elsasa.sch.id`, password `Parent123!` with ASP.NET Core Identity v3 PBKDF2 hash).
     - Role: assign `PARENT` role in `auth.user_roles`.
     - Linkage: insert 2 rows into `auth.user_students` mapping `parent.hendra`'s `user_id` to:
       - Ahmad Dahlan (`01A1F11A-7010-7010-8000-000000000010`)
       - Siti Nurhaliza (`01A1F11A-7011-7011-8000-000000000011`)
   - Student Account 1:
     - User: `student.ahmad` (`Ahmad Dahlan`, email `ahmad.dahlan@elsasa.sch.id`, password `Student123!` with PBKDF2 hash).
     - Role: assign `STUDENT` role in `auth.user_roles`.
     - Actor linkage: insert into `auth.user_actors` linking `student_id = '01A1F11A-7010-7010-8000-000000000010'`.
   - Student Account 2:
     - User: `student.siti` (`Siti Nurhaliza`, email `siti.nurhaliza@elsasa.sch.id`, password `Student123!` with PBKDF2 hash).
     - Role: assign `STUDENT` role in `auth.user_roles`.
     - Actor linkage: insert into `auth.user_actors` linking `student_id = '01A1F11A-7011-7011-8000-000000000011'`.
3. Verify migration execution:
   - Run DbUp migration pipeline (or integration test seeder) against SQL Server.
   - Query `auth.user_students` and verify parent multi-child rows exist and refer to valid students.

Implementation Notes:
- Created idempotent migration `004_CreateAuthUserStudents.sql` introducing `auth.user_students` with composite PK (`user_id`, `student_id`), foreign key referencing `auth.users(id)`, and non-clustered index `IX_auth_user_students_student_id`. Enforces Pure Persistence Layer discipline (zero cross-schema FKs, zero triggers, zero database defaults).
- Created deterministic seed script `102_SeedStage4Data.sql` provisioning parent account `parent.hendra` (linked to Ahmad Dahlan and Siti Nurhaliza in `auth.user_students` with role `PARENT`), and learner accounts `student.ahmad` and `student.siti` (linked to their respective student IDs in `auth.user_actors` with role `STUDENT`). Standard ASP.NET Core Identity v3 PBKDF2 hashes generated and verified for `Parent123!` and `Student123!`.
- Created C# seeder helper `Stage4Seeder.cs` and integrated `SeedStage4FixturesAsync` into `IntegrationTestWebApplicationFactory` and `IntegrationTestBase`.
- Created and passed end-to-end integration tests in `Stage4DatabaseAndSeederIntegrationTests.cs`, validating migration table structure, constraints, index, idempotent double execution, role assignments, student linkages, and successful password authentication.

Changed Files:
- `src/backend/src/ElsasaSis.Database/Scripts/004_CreateAuthUserStudents.sql` (created)
- `src/backend/src/ElsasaSis.Database/Scripts/102_SeedStage4Data.sql` (created)
- `src/backend/src/ElsasaSis.Database/Stage4Seeder.cs` (created)
- `src/backend/tests/ElsasaSis.IntegrationTests/Fixtures/IntegrationTestBase.cs` (modified)
- `src/backend/tests/ElsasaSis.IntegrationTests/Fixtures/IntegrationTestWebApplicationFactory.cs` (modified)
- `src/backend/tests/ElsasaSis.IntegrationTests/Stage4DatabaseAndSeederIntegrationTests.cs` (created)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

---

### P1-S02

Title: Identity, RBAC Permissions, Role Catalogs & API Policy Registrations

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Define Stage 4 compile-time permissions, populate `RolePermissionCatalog` for `Roles.Parent` and `Roles.Student`, grant monitoring permissions to `Roles.Principal` and `Roles.SchoolAdmin`, and register corresponding authorization policies in `ElsasaSis.Api`.

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. Update `src/backend/src/Modules/ElsasaSis.Modules.Auth/Permissions.cs`:
   - Under `Academic`: add `public static class Monitoring { public const string View = "Permissions.Academic.Monitoring.View"; }`.
   - Under `Students`: add `public static class Children { public const string View = "Permissions.Students.Children.View"; }`.
   - Add both permissions to `Permissions.All` collection.
2. Update local `Permissions.cs` in affected modules:
   - `src/backend/src/Modules/ElsasaSis.Modules.Acd/Permissions.cs`: add `Monitoring.View`.
   - `src/backend/src/Modules/ElsasaSis.Modules.Std/Permissions.cs`: add `Children.View`.
3. Update `src/backend/src/Modules/ElsasaSis.Modules.Auth/RolePermissionCatalog.cs`:
   - `Roles.SchoolAdmin`: add `Permissions.Academic.Monitoring.View`.
   - `Roles.Principal`: add `Permissions.Academic.Monitoring.View`.
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
4. Update `src/backend/src/ElsasaSis.Api/Program.cs`:
   - Register authorization policies for `Permissions.Academic.Monitoring.View` and `Permissions.Students.Children.View` using `.RequireAuthenticatedUser().RequireClaim("permissions", ...)`.
5. Update `src/backend/tests/ElsasaSis.UnitTests/Auth/RolePermissionCatalogTests.cs`:
   - Assert `PARENT` possesses exactly the 5 assigned permissions.
   - Assert `STUDENT` possesses exactly the 3 assigned permissions.
   - Assert `PRINCIPAL` and `SCHOOL_ADMIN` possess `Permissions.Academic.Monitoring.View`.
6. Run `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` and ensure all tests pass with exit code `0`.

Implementation Notes:
- Added compile-time permissions `Academic.Monitoring.View` and `Students.Children.View` to `Modules.Auth/Permissions.cs` and included both in `Permissions.All`.
- Added localized permission constants to `Modules.Acd/Permissions.cs` and `Modules.Std/Permissions.cs`.
- Populated `RolePermissionCatalog` for `Roles.Parent` (5 permissions), `Roles.Student` (3 permissions), and granted `Academic.Monitoring.View` to `Roles.SchoolAdmin` and `Roles.Principal`.
- Registered authorization policies for `Permissions.Academic.Monitoring.View` and `Permissions.Students.Children.View` with `.RequireAuthenticatedUser().RequireClaim("permissions", ...)` in `ElsasaSis.Api/Program.cs`.
- Updated unit tests in `RolePermissionCatalogTests.cs` to assert exact permission assignments for PARENT (5), STUDENT (3), and verification of Academic.Monitoring.View for PRINCIPAL and SCHOOL_ADMIN. All 740 unit tests and 54 integration tests pass with exit code 0.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Auth/Permissions.cs` (modified)
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Permissions.cs` (modified)
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Permissions.cs` (modified)
- `src/backend/src/Modules/ElsasaSis.Modules.Auth/RolePermissionCatalog.cs` (modified)
- `src/backend/src/ElsasaSis.Api/Program.cs` (modified)
- `src/backend/tests/ElsasaSis.UnitTests/Auth/RolePermissionCatalogTests.cs` (modified)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

Notes: Compile-time permission strings must match identically between `Modules.Auth` and Minimal API endpoint authorization policies.

---

### P1-S03

Title: Frontend Consumer Shell Scaffolding (`PortalLayout.vue`), Post-Login Role Redirection & Router Setup

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Create the distraction-free, mobile-first `PortalLayout.vue` consumer shell, configure role-based post-login redirection in `LoginView.vue`, update `authNavigationGuard` in `router/index.ts`, and register routes for `/portal/parent`, `/portal/student`, and `/academic/monitoring`.

Depends On: None

Repository: src/frontend

Completion Criteria:

1. Create `src/frontend/src/app/layouts/PortalLayout.vue`:
   - Dedicated authenticated shell for consumer personas (`PARENT` and `STUDENT`) without the administrative sidebar.
   - Header Bar:
     - Application brand logo / icon and title ("ELSASA-SIS").
     - Role Badge chip (displaying "Portal Orang Tua" for Parent role, "Portal Siswa" for Student role).
     - User Display Name and Sign-Out button (triggering `authStore.logout()` and redirecting to `/login`).
   - Content Container:
     - Centered layout (`max-w-5xl mx-auto px-4 py-6 sm:px-6 lg:px-8`).
     - Responsive viewport padding optimized for touch screens.
     - Slot `<router-view />`.
2. Update `src/frontend/src/app/router/routes.ts`:
   - Add route `/portal/parent`:
     - Name: `portal.parent`
     - Component: `() => import('@/features/portal-parent/views/ParentPortalView.vue')`
     - Meta: `requiresAuth: true`, `permission: 'Permissions.Students.Children.View'`, `layout: 'portal'`, `title: 'Portal Orang Tua'`
   - Add route `/portal/student`:
     - Name: `portal.student`
     - Component: `() => import('@/features/portal-student/views/StudentPortalView.vue')`
     - Meta: `requiresAuth: true`, `permission: 'Permissions.Operations.Timetables.ViewOwn'`, `layout: 'portal'`, `title: 'Portal Siswa'`
   - Add route `/academic/monitoring`:
     - Name: `academic.monitoring`
     - Component: `() => import('@/features/academic/monitoring/views/AcademicMonitoringView.vue')`
     - Meta: `requiresAuth: true`, `permission: 'Permissions.Academic.Monitoring.View'`, `layout: 'app'`, `title: 'Monitoring Progres Akademik'`
3. Update `src/frontend/src/app/layouts/AppLayout.vue`:
   - Add `Academic Progress Monitoring` navigation item under `Academic & Curriculum` nav group:
     - `label: 'Academic Progress Monitoring'`
     - `to: { name: 'academic.monitoring' }`
     - `icon: Activity` (or `BarChart3`)
     - `permissions: ['Permissions.Academic.Monitoring.View']`
4. Update `src/frontend/src/features/auth/views/LoginView.vue`:
   - Dynamic post-login redirection:
     - If authenticated user has role `PARENT` $\rightarrow$ redirect to `/portal/parent`.
     - If authenticated user has role `STUDENT` $\rightarrow$ redirect to `/portal/student`.
     - Otherwise (staff roles) $\rightarrow$ redirect to `/operations/calendar` (or query `redirect` param).
5. Update `src/frontend/src/app/router/index.ts` (`authNavigationGuard`):
   - When authenticated user visits a `guestOnly` route (e.g. `/login`), redirect `PARENT` to `/portal/parent`, `STUDENT` to `/portal/student`, and staff to `/operations/calendar`.
6. Create placeholder view stubs:
   - `src/frontend/src/features/portal-parent/views/ParentPortalView.vue`
   - `src/frontend/src/features/portal-student/views/StudentPortalView.vue`
   - `src/frontend/src/features/academic/monitoring/views/AcademicMonitoringView.vue`
7. Run `pnpm typecheck` and `pnpm test:unit` and verify zero errors.

Implementation Notes:
- Created `PortalLayout.vue` mobile-first authenticated shell featuring ELSASA-SIS logo/branding, role badge chip ("Portal Orang Tua" for PARENT, "Portal Siswa" for STUDENT), user display name, and Sign-Out button routing to `/login`.
- Updated `LoginView.vue` to inspect `sessionUser.roles` and redirect `PARENT` to `/portal/parent`, `STUDENT` to `/portal/student`, and staff to `/operations/calendar` (or relative query `redirect`).
- Updated `authNavigationGuard` in `src/app/router/index.ts` to redirect authenticated users visiting `guestOnly` routes (`/login` / `/auth/login`) to `/portal/parent` for `PARENT` and `/portal/student` for `STUDENT`.
- Registered routes `/portal/parent`, `/portal/student`, and `/academic/monitoring` with permission guards and layout metadata in `routes.ts`.
- Added `Academic Progress Monitoring` navigation item under `Academic & Curriculum` in `AppLayout.vue` gated by `Permissions.Academic.Monitoring.View`.
- Created placeholder view stubs in `portal-parent`, `portal-student`, and `academic/monitoring`.
- Added comprehensive unit tests in `PortalLayout.spec.ts`, updated `LoginView.spec.ts`, `router.guard.spec.ts`, and `AppLayout.spec.ts`. All 52 test files (330 tests) and `vue-tsc --noEmit` pass with 0 errors.

Changed Files:
- `src/frontend/src/app/layouts/PortalLayout.vue` (new)
- `src/frontend/src/app/layouts/__tests__/PortalLayout.spec.ts` (new)
- `src/frontend/src/features/portal-parent/views/ParentPortalView.vue` (new)
- `src/frontend/src/features/portal-student/views/StudentPortalView.vue` (new)
- `src/frontend/src/features/academic/monitoring/views/AcademicMonitoringView.vue` (new)
- `src/frontend/src/app/router/routes.ts`
- `src/frontend/src/app/router/index.ts`
- `src/frontend/src/app/layouts/AppLayout.vue`
- `src/frontend/src/features/auth/views/LoginView.vue`
- `src/frontend/src/app/router/__tests__/router.guard.spec.ts`
- `src/frontend/src/features/auth/views/__tests__/LoginView.spec.ts`
- `src/frontend/src/app/layouts/__tests__/AppLayout.spec.ts`
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md`

Notes: Ensure `App.vue` or layout resolution properly selects `PortalLayout.vue` when route meta contains `layout: 'portal'`.

---

## P2 - Backend Read Slices & Scoped Data Isolation

Implementation Status: IMPLEMENTED  
Review Status: GO

### P2-S04

Title: Parent Linked Children Query (`GetMyChildren`) Read Slice in `ElsasaSis.Modules.Std`

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement the CQRS Read Slice `GetMyChildren` (`GET /api/std/students/my-children`) allowing authenticated parents to retrieve their linked active children with enrollment and placement details for the multi-child switcher (`UC-PRT-01`).

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Create directory `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/Students/GetMyChildren/`:
   - `GetMyChildrenQuery.cs`: MediatR request record implementing `IRequest<Result<IReadOnlyList<LinkedChildDto>>>`.
   - `LinkedChildDto.cs`: Record defining:
     - `Guid StudentId`
     - `string Nis`
     - `string? Nisn`
     - `string FullName`
     - `string? Nickname`
     - `DateTime DateOfBirth`
     - `string Gender`
     - `Guid? StudyGroupId`
     - `string? StudyGroupName`
     - `string? SchoolLevel`
     - `string? Grade`
   - `GetMyChildrenHandler.cs`:
     - Injects `ICurrentUser currentUser`, `ISqlConnectionFactory connectionFactory`, and `IOperationalContract operationalContract`.
     - Resolves `currentUserId = currentUser.UserId`.
     - Resolves active academic year via `operationalContract.GetActiveOperationalPeriodAsync()`.
     - Executes direct Dapper query:
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
     - Returns `Result<IReadOnlyList<LinkedChildDto>>.Success(...)`.
   - `GetMyChildrenEndpoint.cs`:
     - Maps `GET /api/std/students/my-children` with `.RequireAuthorization("Permissions.Students.Children.View")`.
     - Produces `200 OK` with `IReadOnlyList<LinkedChildDto>` and `401 Unauthorized` / `403 Forbidden`.
2. Unit Tests in `src/backend/tests/ElsasaSis.UnitTests/Std/GetMyChildrenHandlerTests.cs`:
   - Test retrieving linked children returns expected student DTOs ordered by name.
   - Test parent with zero linked children returns empty list.
3. Run `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` and ensure all tests pass.

Implementation Notes:
- Created CQRS read slice `GetMyChildren` under `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/Students/GetMyChildren/`:
  - `LinkedChildDto.cs`: Record defining child identity, demographics, and active academic year study group placement (`StudyGroupId`, `StudyGroupName`, `SchoolLevel`, `Grade`).
  - `GetMyChildrenQuery.cs`: Request record implementing MediatR `IRequest<Result<IReadOnlyList<LinkedChildDto>>>`.
  - `GetMyChildrenHandler.cs`: Executes optimized Dapper query joining `std.students`, `auth.user_students`, and left joining `std.student_placements` scoped to active academic year (resolved via `IOperationalContract`) and authenticated parent user ID (`ICurrentUser.UserId`), returning active students ordered alphabetically.
  - `GetMyChildrenEndpoint.cs`: Minimal API endpoint mapped to `GET /api/std/students/my-children` protected by `Permissions.Students.Children.View` returning `200 OK` or `401`/`403`.
- Created comprehensive unit tests in `src/backend/tests/ElsasaSis.UnitTests/Std/GetMyChildrenHandlerTests.cs` verifying multi-child retrieval, alphabetical sorting, parameter bindings, empty child lists for unlinked parents, and MediatR interface dispatch. All 743 unit tests passed with exit code 0.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/Students/GetMyChildren/LinkedChildDto.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/Students/GetMyChildren/GetMyChildrenQuery.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/Students/GetMyChildren/GetMyChildrenHandler.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/Students/GetMyChildren/GetMyChildrenEndpoint.cs` (created)
- `src/backend/tests/ElsasaSis.UnitTests/Std/GetMyChildrenHandlerTests.cs` (created)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

Notes: Handlers must not instantiate domain aggregate roots or use write repositories. Query executes directly against Dapper connection.

---

### P2-S05

Title: Personal Student Timetable with SMK Block Rotation Resolution & Announcement Audience Scoping in `ElsasaSis.Modules.Opr`

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement `GetPersonalStudentTimetable` (`GET /api/opr/timetables/my`) resolving student study group, active SMK vocational block cycle (`GENERAL` vs `VOCATIONAL`), today's agenda, and weekly schedule (`UC-STD-01`), and update `GetAnnouncementsHandler` and `AnnouncementAccessPolicy` to scope bulletins by actor audience (`UC-PRT-04`, `UC-STD-03`).

Depends On: P1-S02

Repository: src/backend

Completion Criteria:

1. Create directory `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Timetables/GetPersonalStudentTimetable/`:
   - `GetPersonalStudentTimetableQuery.cs`: MediatR request `(DateOnly? Date)` returning `Result<StudentPersonalScheduleResponse>`.
   - Response DTOs in `StudentPersonalScheduleDtos.cs`:
     - `StudentPersonalScheduleResponse(StudentTimetableInfoDto StudentInfo, ActiveSmkCycleDto ActiveCycle, IReadOnlyList<ScheduleSlotDto> TodayAgenda, IReadOnlyList<ScheduleSlotDto> WeeklySchedule)`
     - `StudentTimetableInfoDto(Guid StudentId, string FullName, string Nis, Guid? StudyGroupId, string? StudyGroupName, string? SchoolLevel, string? Grade)`
     - `ActiveSmkCycleDto(string ScheduleType, bool IsSmkRotation, DateOnly? StartDate, DateOnly? EndDate)`
     - `ScheduleSlotDto(Guid Id, string DayOfWeek, int PeriodSlot, string StartTime, string EndTime, Guid SubjectId, string SubjectName, string SubjectCode, Guid TeacherId, string TeacherName, Guid? RoomId, string? RoomName, string ScheduleType)`
   - `GetPersonalStudentTimetableHandler.cs`:
     - Extracts `studentId = currentUser.StudentId`. If null/empty, returns `DomainErrors.Auth.Forbidden` / `Result.Failure`.
     - Resolves active period from `IOperationalContract.GetActiveOperationalPeriodAsync()`.
     - Queries `std.student_placements` for `student_id = @StudentId AND academic_year_id = @ActiveYearId` to obtain `study_group_id`, `study_group_name`, `school_level`, `grade`. If unplaced, returns empty timetable with placement pending info.
     - Resolves SMK block rotation cycle for `targetDate = query.Date ?? DateOnly.FromDateTime(DateTime.UtcNow)`:
       ```sql
       SELECT schedule_type, start_date, end_date
       FROM opr.study_group_timetables
       WHERE study_group_id = @StudyGroupId
         AND operational_period_id = @PeriodId
         AND @QueryDate BETWEEN start_date AND end_date;
       ```
       - If match found: `activeScheduleType = schedule_type`, `isSmkRotation = true`.
       - If no match: `activeScheduleType = 'REGULAR'`, `isSmkRotation = false`.
     - Queries `opr.study_group_schedules` for `study_group_id = @StudyGroupId AND operational_period_id = @PeriodId`.
     - Projects `TodayAgenda`: Filter slots where `day_of_week = @DayOfWeek` AND (`schedule_type = @ActiveScheduleType` OR `schedule_type = 'REGULAR'`), ordered by `period_slot ASC`.
     - Projects `WeeklySchedule`: All slots for the active cycle (`schedule_type = @ActiveScheduleType` OR `schedule_type = 'REGULAR'`), ordered by `day_of_week, period_slot`.
   - `GetPersonalStudentTimetableEndpoint.cs`:
     - Maps `GET /api/opr/timetables/my` with `.RequireAuthorization("Permissions.Operations.Timetables.ViewOwn")`.
2. Update Announcements Audience Scoping:
   - In `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Announcements/AnnouncementAccessPolicy.cs`:
     - Add helper methods `IsParentOnly(ICurrentUser currentUser)` and `IsStudentOnly(ICurrentUser currentUser)`.
     - Update `CanView`:
       - If caller is parent-only: returns true only if `status == "PUBLISHED"` and `targetAudience IN ('ALL', 'PARENTS')`.
       - If caller is student-only: returns true only if `status == "PUBLISHED"` and `targetAudience IN ('ALL', 'STUDENTS')`.
   - In `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Announcements/GetAnnouncements/GetAnnouncementsHandler.cs`:
     - If caller is parent-only: append conditions `a.status = 'PUBLISHED'` and `a.target_audience IN ('ALL', 'PARENTS')`.
     - If caller is student-only: append conditions `a.status = 'PUBLISHED'` and `a.target_audience IN ('ALL', 'STUDENTS')`.
3. Unit Tests in `src/backend/tests/ElsasaSis.UnitTests/Opr/`:
   - Test `GetPersonalStudentTimetableHandler` properly resolves Vocational vs General block cycles.
   - Test `AnnouncementAccessPolicyTests` asserting parent cannot view `TEACHERS` or `STUDENTS` bulletins, and student cannot view `PARENTS` or `TEACHERS` bulletins.
4. Run `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` and ensure all tests pass.

Implementation Notes:
- Created `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Timetables/GetPersonalStudentTimetable/` with `GetPersonalStudentTimetableQuery.cs`, `StudentPersonalScheduleDtos.cs`, `GetPersonalStudentTimetableHandler.cs`, and `GetPersonalStudentTimetableEndpoint.cs`.
- `GetPersonalStudentTimetableHandler` resolves the active operational period via `IOperationalContract.GetActiveOperationalPeriodAsync()`, validates authenticated `currentUser.StudentId`, queries `std.students` joined with `std.student_placements` for active study group assignment, resolves active SMK block rotation cycles (`GENERAL` vs `VOCATIONAL`) from `opr.study_group_timetables` via date range inclusion (`@QueryDate BETWEEN start_date AND end_date`), and projects `TodayAgenda` and `WeeklySchedule` from `opr.study_group_schedules` with subject codes from `acd.subjects`.
- Registered `GetPersonalStudentTimetableEndpoint` exposing `GET /api/opr/timetables/my` protected with `Permissions.Operations.Timetables.ViewOwn`.
- Registered `GetPersonalStudentTimetableHandler` in `ElsasaSis.Modules.Opr.DependencyInjection`.
- Updated `AnnouncementAccessPolicy.cs` with helper methods `IsParentOnly` and `IsStudentOnly`, updating `CanView` to scope parents to `ALL` and `PARENTS` bulletins (strictly `PUBLISHED`) and students to `ALL` and `STUDENTS` bulletins (strictly `PUBLISHED`).
- Updated `GetAnnouncementsHandler.cs` to apply `a.status = 'PUBLISHED'` and corresponding `a.target_audience IN ('ALL', 'PARENTS')` / `a.target_audience IN ('ALL', 'STUDENTS')` filters for parent and student callers.
- Created unit tests in `src/backend/tests/ElsasaSis.UnitTests/Opr/AnnouncementAccessPolicyTests.cs` and `src/backend/tests/ElsasaSis.UnitTests/Opr/GetPersonalStudentTimetableHandlerTests.cs`.
- Verified all 776 unit tests and 19 architecture tests pass cleanly.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Timetables/GetPersonalStudentTimetable/GetPersonalStudentTimetableQuery.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Timetables/GetPersonalStudentTimetable/StudentPersonalScheduleDtos.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Timetables/GetPersonalStudentTimetable/GetPersonalStudentTimetableHandler.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Timetables/GetPersonalStudentTimetable/GetPersonalStudentTimetableEndpoint.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Announcements/AnnouncementAccessPolicy.cs` (modified)
- `src/backend/src/Modules/ElsasaSis.Modules.Opr/Features/Announcements/GetAnnouncements/GetAnnouncementsHandler.cs` (modified)
- `src/backend/src/Modules/ElsasaSis.Modules.Opr/DependencyInjection.cs` (modified)
- `src/backend/tests/ElsasaSis.UnitTests/Opr/AnnouncementAccessPolicyTests.cs` (created)
- `src/backend/tests/ElsasaSis.UnitTests/Opr/GetPersonalStudentTimetableHandlerTests.cs` (created)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

Notes: SMK block rotation cycle resolves dynamically from `opr.study_group_timetables` using date range inclusion (`@QueryDate BETWEEN start_date AND end_date`).

---

### P2-S06

Title: Academic Monitoring Progress Engine & Parent Report Card Draft Isolation in `ElsasaSis.Modules.Acd`

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement `GetAcademicMonitoringProgress` (`GET /api/acd/monitoring/progress` — `UC-ADM-21`) aggregating school-wide attendance, grading completeness, and report card readiness with exception alerts, implement `GetStudentReportCards` (`GET /api/acd/report-cards/student/{studentId}` — `UC-PRT-01`), and update `GetReportCardByIdHandler` to enforce child ownership and draft hiding for parent callers.

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Create directory `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/Monitoring/GetAcademicMonitoringProgress/`:
   - `GetAcademicMonitoringProgressQuery.cs`: MediatR request `(Guid? AcademicYearId, Guid? OperationalPeriodId, DateOnly? Date)` returning `Result<AcademicMonitoringProgressDto>`.
   - DTOs in `AcademicMonitoringProgressDto.cs`:
     - `AcademicMonitoringOverviewDto`:
       - `AttendanceRateTodayPercent` (`decimal`)
       - `TotalEnrolledStudentsCount` (`int`)
       - `PresentCount`, `AbsentCount`, `SickCount`, `LateCount`, `ExcusedCount` (`int`)
       - `GradingCompletenessPercent` (`decimal`)
       - `TotalAssignmentsCount`, `ExpectedScoresCount`, `ActualScoresCount` (`int`)
       - `ReportCardsFinalizedPercent` (`decimal`)
       - `TotalPlacedStudentsCount`, `DraftReportCardsCount`, `FinalizedReportCardsCount` (`int`)
     - `AcademicMonitoringExceptionsDto`:
       - `MissingAttendanceStudyGroups`: `IReadOnlyList<MissingAttendanceAlertDto>` (`StudyGroupId`, `StudyGroupName`, `SchoolLevel`, `Grade`, `HomeroomTeacherName`)
       - `OverdueGradingAssignments`: `IReadOnlyList<OverdueGradingAlertDto>` (`AssignmentId`, `Title`, `SubjectName`, `TeacherName`, `StudyGroupName`, `DaysOverdue`, `MissingScoresCount`)
       - `PendingReportCards`: `IReadOnlyList<PendingReportCardAlertDto>` (`StudyGroupId`, `StudyGroupName`, `SchoolLevel`, `Grade`, `HomeroomTeacherName`, `DraftCount`, `TotalCount`)
     - `StudyGroupMonitoringRowDto`:
       - `StudyGroupId`, `StudyGroupName`, `SchoolLevel`, `Grade`, `HomeroomTeacherName`, `AttendanceTodayPercent`, `AssignmentsCount`, `GradingCompletenessPercent`, `ReportCardsFinalizedRatio`, `IsAttendanceMissingToday`, `HasOverdueGrading`, `HasPendingReportCards`
   - `GetAcademicMonitoringProgressHandler.cs`:
     - Resolves active academic year and operational period if not provided in query.
     - Performs aggregate SQL queries against `acd.attendance_records`, `acd.assignments`, `acd.assignment_scores`, `acd.report_cards`, and `std.student_placements`.
     - Returns composite `AcademicMonitoringProgressDto`.
   - `GetAcademicMonitoringProgressEndpoint.cs`:
     - Maps `GET /api/acd/monitoring/progress` with `.RequireAuthorization("Permissions.Academic.Monitoring.View")`.
2. Create directory `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetStudentReportCards/`:
   - `GetStudentReportCardsQuery.cs`: MediatR request `(Guid StudentId)` returning `Result<IReadOnlyList<StudentReportCardSummaryDto>>`.
   - `GetStudentReportCardsHandler.cs`:
     - Injects `ICurrentUser currentUser`, `ISqlConnectionFactory connectionFactory`.
     - If caller has `PARENT` role:
       - Validates `studentId` belongs to parent: `SELECT 1 FROM auth.user_students WHERE user_id = @UserId AND student_id = @StudentId`. If not found, returns `DomainErrors.Auth.Forbidden`.
       - Scoped query strictly enforces: `WHERE rc.student_id = @StudentId AND rc.status = 'FINALIZED'`.
     - If staff: queries all report cards (`DRAFT` and `FINALIZED`) for the student.
     - Projects `StudentReportCardSummaryDto` (`Id`, `StudentId`, `StudentName`, `AcademicYearName`, `OperationalPeriodName`, `StudyGroupName`, `Status`, `FinalizedAt`).
   - `GetStudentReportCardsEndpoint.cs`:
     - Maps `GET /api/acd/report-cards/student/{studentId}` with `.RequireAuthorization("Permissions.Academic.ReportCards.View")`.
3. Update `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetReportCardById/GetReportCardByIdHandler.cs`:
   - Injects `ICurrentUser currentUser`.
   - If caller has `PARENT` role:
     - Verifies report card `student_id` is linked to parent in `auth.user_students`. If not, returns `403 Forbidden`.
     - Verifies `status == 'FINALIZED'`. If in `DRAFT`, returns `404 Not Found` (`AcdErrors.ReportCardNotFound`) to prevent draft grade leakage.
4. Unit Tests in `src/backend/tests/ElsasaSis.UnitTests/Acd/`:
   - Test `GetAcademicMonitoringProgressHandler` computes KPI percentages and exception alerts accurately.
   - Test `GetStudentReportCardsHandler` filters out draft report cards when called by parent.
   - Test `GetReportCardByIdHandler` returns 404 for draft cards when called by parent.
5. Run `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` and ensure all tests pass.

Implementation Notes:
- Created `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/Monitoring/GetAcademicMonitoringProgress/` containing:
  - `GetAcademicMonitoringProgressQuery.cs`: MediatR request record with optional `AcademicYearId`, `OperationalPeriodId`, and `Date`.
  - `AcademicMonitoringProgressDto.cs`: Defines `AcademicMonitoringOverviewDto` (attendance %, grading %, report cards %, counts), `AcademicMonitoringExceptionsDto` (missing attendance, overdue grading > 7 days, pending report cards <= 14 days of period end), and `StudyGroupMonitoringRowDto` drill-down matrix.
  - `GetAcademicMonitoringProgressHandler.cs`: Implements Dapper-based cross-schema CQRS query aggregating attendance, grading, and report cards with exception detection and auto-resolution of active period and year.
  - `GetAcademicMonitoringProgressEndpoint.cs`: Maps `GET /api/acd/monitoring/progress` secured with `Permissions.Academic.Monitoring.View`.
- Created `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetStudentReportCards/` containing:
  - `StudentReportCardSummaryDto.cs`: Summary DTO for report card cards list.
  - `GetStudentReportCardsQuery.cs`: MediatR request record with `StudentId`.
  - `GetStudentReportCardsHandler.cs`: Injects `ISqlConnectionFactory` and `ICurrentUser`. For callers with role `PARENT`, validates parent child linkage in `auth.user_students` (returns 403 Forbidden if unlinked) and strictly filters `status = 'FINALIZED'` (draft hiding). For staff roles, returns all report cards.
  - `GetStudentReportCardsEndpoint.cs`: Maps `GET /api/acd/report-cards/student/{studentId:guid}` secured with `Permissions.Academic.ReportCards.View`.
- Updated `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetReportCardById/GetReportCardByIdHandler.cs`:
  - Injects `ICurrentUser currentUser`.
  - For `PARENT` callers, enforces child linkage in `auth.user_students` (403 Forbidden) and returns `404 Not Found` (`AcdErrors.ReportCards.ReportCardNotFound`) if the report card is not finalized (preventing draft grade and existence leakage).
- Added comprehensive unit tests in `src/backend/tests/ElsasaSis.UnitTests/Acd/AcademicMonitoringAndReportCardTests.cs` verifying KPI computation, missing attendance alert, overdue grading alert, pending report card alert, parent draft isolation, parent forbidden access, staff access, and DI registration.
- Ran `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` (all 784 tests pass) and `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj` (all 19 tests pass).

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/Monitoring/GetAcademicMonitoringProgress/AcademicMonitoringProgressDto.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/Monitoring/GetAcademicMonitoringProgress/GetAcademicMonitoringProgressQuery.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/Monitoring/GetAcademicMonitoringProgress/GetAcademicMonitoringProgressHandler.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/Monitoring/GetAcademicMonitoringProgress/GetAcademicMonitoringProgressEndpoint.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetStudentReportCards/StudentReportCardSummaryDto.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetStudentReportCards/GetStudentReportCardsQuery.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetStudentReportCards/GetStudentReportCardsHandler.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetStudentReportCards/GetStudentReportCardsEndpoint.cs` (created)
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetReportCardById/GetReportCardByIdHandler.cs` (modified)
- `src/backend/tests/ElsasaSis.UnitTests/Acd/AcademicMonitoringAndReportCardTests.cs` (created)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

Notes: Draft report cards must be completely invisible to parent accounts; attempting to access a draft card returns `404 Not Found` rather than 403 to prevent existence leakage.

---

### P2-S07

Title: Finance Child Scoping Validation Guards & Receivables Governance Verification in `ElsasaSis.Modules.Fin`

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Enforce child-ownership verification guards on `GetStudentStatementHandler` (`UC-PRT-02`) and `GetPaymentReceiptByIdHandler` (`UC-PRT-03`) for parent callers, and verify executive financial reports endpoints for leadership governance (`UC-ADM-26`, `UC-PRC-06`, `UC-FIN-09`).

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Update `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/GetStudentStatement/GetStudentStatementHandler.cs`:
   - Injects `ICurrentUser currentUser`.
   - If caller has `PARENT` role:
     - Check child ownership:
       ```sql
       IF NOT EXISTS (SELECT 1 FROM auth.user_students WHERE user_id = @UserId AND student_id = @StudentId)
       ```
     - If unauthorized, abort and return `Result<StudentStatementDto>.Failure(DomainErrors.Auth.Forbidden("You do not have permission to view financial statements for this student."))`.
2. Update `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/GetPaymentReceiptById/GetPaymentReceiptByIdHandler.cs`:
   - Injects `ICurrentUser currentUser`.
   - If caller has `PARENT` role:
     - Check receipt belongs to an authorized linked child:
       ```sql
       AND EXISTS (
           SELECT 1 FROM auth.user_students us
           WHERE us.user_id = @UserId
             AND us.student_id = r.student_id
       )
       ```
     - If unauthorized, return `DomainErrors.Auth.Forbidden`.
3. Governance Verification on Financial Reports:
   - Inspect existing Stage 3 endpoints in `ElsasaSis.Modules.Fin`:
     - `GET /api/fin/reports/receivables-summary` (`UC-ADM-26`)
     - `GET /api/fin/reports/receivables-aging` (`UC-FIN-09`, `UC-PRC-06`)
     - `GET /api/fin/reports/daily-cashier` (`UC-FIN-11`)
   - Confirm endpoints require `Permissions.Finance.Reports.View` and are accessible to `SCHOOL_ADMIN`, `PRINCIPAL`, and `FINANCE_OFFICER` roles without requiring cashier counter write permissions (`Permissions.Finance.Payments.Record`).
4. Unit Tests in `src/backend/tests/ElsasaSis.UnitTests/Fin/`:
   - Test `GetStudentStatementHandler` returns 403 Forbidden when parent queries an unlinked student.
   - Test `GetPaymentReceiptByIdHandler` returns 403 Forbidden when parent queries a receipt for an unlinked student.
5. Run `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` and ensure all tests pass.

Implementation Notes:
- Updated `GetStudentStatementHandler` to inject `ICurrentUser currentUser`. When caller has role `PARENT` (and not elevated staff), queries `auth.user_students` to enforce child ownership. If unlinked, returns RFC 7807 403 Forbidden (`FinErrors.Forbidden("You do not have permission to view financial statements for this student.")`).
- Updated `GetPaymentReceiptByIdHandler` to inject `ICurrentUser currentUser`. When caller has role `PARENT` (and not elevated staff), validates whether `receiptRow.StudentId` exists in `auth.user_students` for `@UserId`. If unlinked, returns 403 Forbidden (`FinErrors.Forbidden("You do not have permission to view this payment receipt.")`).
- Added `FinErrors.Forbidden` helper producing `DomainError.Forbidden("Auth.Forbidden", message)`.
- Verified executive financial report endpoints (`receivables-summary`, `receivables-aging`, `daily-cashier`) require `Permissions.Finance.Reports.View`, granted to `SCHOOL_ADMIN`, `PRINCIPAL`, and `FINANCE_OFFICER` roles without cashier write permissions (`Permissions.Finance.Payments.Record`).
- Created unit tests in `src/backend/tests/ElsasaSis.UnitTests/Fin/FinanceChildScopingTests.cs` verifying 403 Forbidden on unlinked student statements and receipts, 200 OK on linked children, staff bypass, 404 on missing receipts, and RBAC governance verification. All 776 unit tests and 54 integration tests pass.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/FinErrors.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/GetStudentStatement/GetStudentStatementHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/GetPaymentReceiptById/GetPaymentReceiptByIdHandler.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Fin/FinanceChildScopingTests.cs`
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md`

Notes: All child authorization checks must evaluate against `auth.user_students` using the authenticated parent's `UserId`.

---

## P3 - Architecture Verification, Testing & OpenAPI Sync

Implementation Status: IMPLEMENTED  
Review Status: GO

### P3-S08

Title: Architecture Boundary Tests, Integration Test Suites & Database Migration Verification

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Execute NetArchTest boundary isolation tests verifying zero forbidden module cross-references, implement end-to-end integration test suites verifying Stage 4 use cases, and verify clean database migration execution.

Depends On: P2-S04, P2-S05, P2-S06, P2-S07

Repository: src/backend

Completion Criteria:

1. Update `src/backend/tests/ElsasaSis.ArchitectureTests/`:
   - Run `ModuleBoundaryTests` asserting zero cross-module references among all 7 modules `{Auth, Opr, Std, Tch, Acd, Fin, Dev}`.
   - Run `LayerAndCqrsArchitectureTests` asserting all Stage 4 read slices conform to CQRS read slice rules (no write repository dependencies).
2. Create `src/backend/tests/ElsasaSis.IntegrationTests/Stage4ConsumerPortalsIntegrationTests.cs`:
   - Test Parent Portal Flow:
     - Authenticate as `parent.hendra` with `Parent123!`.
     - `GET /api/std/students/my-children` returns 2 linked children (Ahmad Dahlan and Siti Nurhaliza).
     - `GET /api/acd/report-cards/student/{studentId}` returns finalized report cards; draft cards are excluded.
     - `GET /api/acd/report-cards/{draftId}` returns 404 Not Found for parent.
     - `GET /api/fin/students/{studentId}/statement` returns fee ledger and advance deposit balance.
     - `GET /api/fin/payments/{receiptId}` returns receipt breakdown.
     - Querying an unlinked student ID on any endpoint returns 403 Forbidden.
     - `GET /api/opr/announcements` returns bulletins with target audience `ALL` and `PARENTS`, excluding `STUDENTS` and `TEACHERS`.
   - Test Student Portal Flow:
     - Authenticate as `student.ahmad` with `Student123!`.
     - `GET /api/opr/timetables/my` returns student info, active SMK cycle badge, Today's Agenda, and Weekly Timetable.
     - `GET /api/opr/announcements` returns bulletins with target audience `ALL` and `STUDENTS`, excluding `PARENTS` and `TEACHERS`.
3. Create `src/backend/tests/ElsasaSis.IntegrationTests/Stage4AcademicMonitoringIntegrationTests.cs`:
   - Authenticate as `admin` or `principal`.
   - `GET /api/acd/monitoring/progress` returns:
     - Overview KPI percentages (Attendance Rate Today %, Grading Completeness %, Report Cards Finalized %).
     - Operational exception alerts (Missing Attendance, Overdue Grading, Pending Report Cards).
     - Study group drill-down tabular matrix.
4. Create `src/backend/tests/ElsasaSis.IntegrationTests/Stage4DatabaseMigrationIntegrationTests.cs`:
   - Verify DbUp scripts `004_CreateAuthUserStudents.sql` and `102_SeedStage4Data.sql` execute cleanly and idempotently on a fresh or migrated test database.
5. Run all test suites:
   - `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj`
   - `dotnet test src/backend/tests/ElsasaSis.IntegrationTests/ElsasaSis.IntegrationTests.csproj`
   - All tests pass with exit code `0`.

Implementation Notes:
- Updated `LayerAndCqrsArchitectureTests.cs` with test `Stage4ReadSliceHandlers_ShouldNotDependOn_WriteRepositories` validating that all 8 Stage 4 read slice handlers (`GetMyChildrenHandler`, `GetPersonalStudentTimetableHandler`, `GetAnnouncementsHandler`, `GetAcademicMonitoringProgressHandler`, `GetStudentReportCardsHandler`, `GetReportCardByIdHandler`, `GetStudentStatementHandler`, `GetPaymentReceiptByIdHandler`) have zero dependencies on write repositories.
- Updated `GetPaymentReceiptByIdHandler.cs` to buffer allocations before running child authorization queries, preventing open DataReader collisions.
- Created `Stage4ConsumerPortalsIntegrationTests.cs` implementing end-to-end tests for parent and student portal workflows:
  - Parent Portal Flow: authenticated as `parent.hendra`, verified `GET /api/std/students/my-children` returns Ahmad Dahlan and Siti Nurhaliza; verified `GET /api/acd/report-cards/student/{studentId}` returns finalized report cards and excludes draft cards; verified `GET /api/acd/report-cards/{draftId}` returns 404 Not Found while finalized card returns 200 OK; verified `GET /api/fin/students/{studentId}/statement` and `GET /api/fin/payments/{receiptId}` return statements and receipt breakdowns; verified querying unlinked students returns 403 Forbidden across report card, statement, and payment receipt endpoints; verified parent announcement feed filters strictly to `ALL` and `PARENTS`.
  - Student Portal Flow: authenticated as `student.ahmad`, verified `GET /api/opr/timetables/my` returns student info, active cycle metadata, today's agenda, and weekly timetable; verified student announcement feed filters strictly to `ALL` and `STUDENTS`.
- Created `Stage4AcademicMonitoringIntegrationTests.cs` testing `GET /api/acd/monitoring/progress` as Admin and Principal: verified Overview KPIs (attendance rate, grading completeness, report cards finalized), operational exception alerts (missing attendance, overdue grading, pending report cards), and study group tabular matrix, with 401/403 guards for anonymous and parent accounts.
- Created `Stage4DatabaseMigrationIntegrationTests.cs` verifying clean, idempotent execution of DbUp migrations `004_CreateAuthUserStudents.sql` and `102_SeedStage4Data.sql`, checking schema, composite PK, indexes, role assignments, and student linkages.
- Ran test suites: all 20 Architecture tests passed, all 59 Integration tests passed, and all 784 Unit tests passed with exit code 0.

Changed Files:
- `src/backend/tests/ElsasaSis.ArchitectureTests/LayerAndCqrsArchitectureTests.cs` (modified)
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/GetPaymentReceiptById/GetPaymentReceiptByIdHandler.cs` (modified)
- `src/backend/tests/ElsasaSis.IntegrationTests/Stage4ConsumerPortalsIntegrationTests.cs` (created)
- `src/backend/tests/ElsasaSis.IntegrationTests/Stage4AcademicMonitoringIntegrationTests.cs` (created)
- `src/backend/tests/ElsasaSis.IntegrationTests/Stage4DatabaseMigrationIntegrationTests.cs` (created)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

Notes: Integration tests must use the established WebApplicationFactory and test authentication fixtures.

---

### P3-S09

Title: OpenAPI Schema Generation & TypeScript Contract Synchronization

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Export the updated OpenAPI specification from the running integration test factory and regenerate the TypeScript API schema definitions in `src/frontend`.

Depends On: P3-S08

Repository: src/frontend

Completion Criteria:

1. Run `SwaggerExportIntegrationTests` in `ElsasaSis.IntegrationTests`:
   - `dotnet test src/backend/tests/ElsasaSis.IntegrationTests/ElsasaSis.IntegrationTests.csproj --filter FullyQualifiedName~SwaggerExportIntegrationTests`
   - Verifies `src/frontend/swagger.json` is exported and contains all Stage 4 endpoints (`/api/std/students/my-children`, `/api/opr/timetables/my`, `/api/acd/monitoring/progress`, `/api/acd/report-cards/student/{studentId}`).
2. Regenerate TypeScript schema in `src/frontend`:
   - Run `pnpm api:generate` (or `npx openapi-typescript src/frontend/swagger.json -o src/shared/types/api/schema.d.ts`).
   - Verify `src/shared/types/api/schema.d.ts` reflects all new response DTOs:
     - `LinkedChildDto`
     - `StudentPersonalScheduleResponse`, `ActiveSmkCycleDto`, `ScheduleSlotDto`
     - `AcademicMonitoringProgressDto`, `AcademicMonitoringOverviewDto`, `AcademicMonitoringExceptionsDto`, `StudyGroupMonitoringRowDto`
     - `StudentReportCardSummaryDto`
3. Update `src/frontend/src/api/schema.d.ts` if needed to ensure clean re-exports.
4. Run `pnpm typecheck` to verify all existing and new TypeScript types compile with zero diagnostic errors.

Implementation Notes:
- Executed `SwaggerExportIntegrationTests` in `ElsasaSis.IntegrationTests` via WebApplicationFactory; successfully generated and exported updated `src/frontend/swagger.json` containing all Stage 4 endpoints (`/api/std/students/my-children`, `/api/opr/timetables/my`, `/api/acd/monitoring/progress`, and `/api/acd/report-cards/student/{studentId}`).
- Updated `src/frontend/package.json` scripts `api:generate` and `api:check` to target local `swagger.json`.
- Executed `pnpm api:generate` regenerating `src/frontend/src/shared/types/api/schema.d.ts` with all Stage 4 response DTOs (`LinkedChildDto`, `StudentPersonalScheduleResponse`, `ActiveSmkCycleDto`, `ScheduleSlotDto`, `AcademicMonitoringProgressDto`, `AcademicMonitoringOverviewDto`, `AcademicMonitoringExceptionsDto`, `StudyGroupMonitoringRowDto`, and `StudentReportCardSummaryDto`).
- Verified `src/frontend/src/api/schema.d.ts` clean re-export of `../shared/types/api/schema`.
- Ran `pnpm typecheck` (`vue-tsc --noEmit`) and `pnpm test:unit` (`vitest run`), verifying all existing and new TypeScript types compile with 0 diagnostic errors and all 52 test files (330 tests) pass.

Changed Files:
- `src/frontend/swagger.json`
- `src/frontend/package.json`
- `src/frontend/src/shared/types/api/schema.d.ts`
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md`

Notes: Generated schema types must drive all frontend TanStack Query hooks and API clients.

---

## P4 - Frontend Consumer Portals & Executive Dashboards

Implementation Status: IMPLEMENTED  
Review Status: GO

### P4-S10

Title: Parent Family Portal Feature (`SCR-PRT-PORTAL` at `/portal/parent`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Build the responsive, mobile-first Parent Family Portal (`src/features/portal-parent/`) under `PortalLayout.vue`, featuring a multi-child tab selector, academic report card viewer with browser print trigger, school fee statement viewer, receipt viewer with print trigger, and school bulletins (`UC-PRT-01`, `UC-PRT-02`, `UC-PRT-03`, `UC-PRT-04`).

Depends On: P1-S03, P3-S09

Repository: src/frontend

Completion Criteria:

1. Create directory `src/frontend/src/features/portal-parent/`:
   - `api/parentPortal.api.ts`: API clients calling:
     - `getMyChildren()`: `GET /api/std/students/my-children`
     - `getChildReportCards(studentId)`: `GET /api/acd/report-cards/student/${studentId}`
     - `getChildStatement(studentId)`: `GET /api/fin/students/${studentId}/statement`
     - `getParentAnnouncements()`: `GET /api/opr/announcements`
     - `getParentCalendar()`: `GET /api/opr/calendar/entries`
   - `api/useParentPortalQueries.ts`: TanStack Query hooks with key factories (`parentPortalKeys`).
   - `components/ChildSelectorTabs.vue`:
     - Horizontal pill/tab selector displaying each child's name, grade, and study group.
     - Smooth active state transition; updates `selectedChildId`.
   - `components/ReportCardsTab.vue` (`UC-PRT-01`):
     - Displays finalized semester report cards with overall performance, homeroom remarks, and letter grades table.
     - "Unduh Rapor PDF" button invoking `ReportCardPrintView.vue` with `window.print()` (using CSS `@media print`).
     - Empty state when no finalized report cards exist.
   - `components/BillingAndReceiptsTab.vue` (`UC-PRT-02`, `UC-PRT-03`):
     - Advance deposit balance card/banner.
     - Monthly tuition billing ledger table with status chips (`PAID`, `PARTIALLY_PAID`, `UNPAID`), gross amount, discounts applied, and outstanding balance.
     - Historical payment receipts table with "Cetak Kuitansi" button invoking `PaymentReceiptPrintView.vue` with `window.print()`.
   - `components/SchoolInfoTab.vue` (`UC-PRT-04`):
     - Feed of announcements targeted to parents with published dates.
     - Active school operational calendar dates and holidays.
   - `views/ParentPortalView.vue`:
     - Master portal view integrating child switcher, 3 feature tabs, pull-to-refresh, skeleton loaders, and error state alerts.
2. Unit / Component Tests:
   - Create `src/frontend/src/features/portal-parent/views/__tests__/ParentPortalView.spec.ts`:
     - Test switching active child switches report cards and billing queries.
     - Test print dialog triggers without throwing errors.
     - Test empty state rendering.
3. Run `pnpm test:unit` and `pnpm typecheck` and ensure all tests pass.

Notes: Report card and receipt printing must utilize browser-native `window.print()` via Vue print views; zero backend headless browser dependencies.

---

### P4-S11

Title: Student Learner Portal Feature (`SCR-STD-PORTAL` at `/portal/student`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Build the responsive, distraction-free Student Learner Portal (`src/features/portal-student/`) under `PortalLayout.vue`, featuring a student banner, active SMK block rotation cycle badge, Today's Agenda timeline, Weekly Timetable grid, and student announcements (`UC-STD-01`, `UC-STD-02`, `UC-STD-03`).

Depends On: P1-S03, P3-S09

Repository: src/frontend

Completion Criteria:

1. Create directory `src/frontend/src/features/portal-student/`:
   - `api/studentPortal.api.ts`: API clients calling:
     - `getPersonalTimetable(date?)`: `GET /api/opr/timetables/my`
     - `getStudentAnnouncements()`: `GET /api/opr/announcements`
     - `getStudentCalendar()`: `GET /api/opr/calendar/entries`
   - `api/useStudentPortalQueries.ts`: TanStack Query hooks with key factories (`studentPortalKeys`).
   - `components/StudentHeaderBanner.vue`:
     - Displays student NIS, full name, study group name, and school level.
   - `components/ActiveCycleBadge.vue`:
     - Prominent status badge indicating active SMK rotation: "Siklus Kejuruan (Vocational)" or "Siklus Umum (General)" with date validity range.
     - If not SMK rotation, displays "Jadwal Reguler".
   - `components/TodayAgendaTimeline.vue` (`UC-STD-01`):
     - Chronological list of today's classes: time slot, subject name, teacher name, and room.
     - Highlights current ongoing period slot based on client time.
   - `components/WeeklyTimetableGrid.vue` (`UC-STD-01`):
     - Responsive weekly period-by-day timetable grid for the active cycle.
   - `components/StudentAnnouncementsFeed.vue` (`UC-STD-02`, `UC-STD-03`):
     - Published announcements targeted to students and upcoming school calendar dates.
   - `views/StudentPortalView.vue`:
     - Master student portal view with toggle between "Agenda Hari Ini" and "Jadwal Mingguan", date picker for historical/future inspection, skeleton loaders, and pull-to-refresh.
2. Unit / Component Tests:
   - Create `src/frontend/src/features/portal-student/views/__tests__/StudentPortalView.spec.ts`:
     - Test toggle between Today's Agenda and Weekly Timetable.
     - Test SMK block rotation badge renders active cycle text.
     - Test empty schedule states.
3. Run `pnpm test:unit` and `pnpm typecheck` and ensure all tests pass.

Implementation Notes:
- Created `studentPortal.api.ts` wrapping endpoints `/api/opr/timetables/my`, `/api/opr/announcements` (scoped for students), and `/api/opr/calendar-entries` using OpenAPI schema types.
- Created `useStudentPortalQueries.ts` with `studentPortalKeys` key factory and TanStack Query hooks (`useStudentTimetableQuery`, `useStudentAnnouncementsQuery`, `useStudentCalendarQuery`).
- Created `StudentHeaderBanner.vue` displaying student full name, NIS, school level with grade, and study group rombel badge.
- Created `ActiveCycleBadge.vue` rendering prominent status badge indicating SMK rotation ("Siklus Kejuruan (Vocational)" or "Siklus Umum (General)") with date validity range, or "Jadwal Reguler" when not an SMK rotation.
- Created `TodayAgendaTimeline.vue` (`UC-STD-01`) presenting chronological class slots with period numbers, subject names, teacher names, room locations, and ongoing slot highlight ("Sedang Berlangsung") based on current time comparison, along with clean empty schedule state.
- Created `WeeklyTimetableGrid.vue` (`UC-STD-01`) with touch-friendly day pills (Senin-Sabtu) and daily slot breakdowns conforming to Tier 1 Mobile-First responsive standards ($360\text{px}+$ viewport, $\ge 44\text{px}$ touch targets).
- Created `StudentAnnouncementsFeed.vue` (`UC-STD-02`, `UC-STD-03`) rendering tabs for published student announcements and upcoming school calendar dates.
- Updated `StudentPortalView.vue` as master portal view under `PortalLayout.vue` featuring banner, active cycle badge, view toggle ("Agenda Hari Ini" vs "Jadwal Mingguan"), date picker for date inspection, refresh trigger, skeleton loading states, and error handling.
- Created unit and component tests in `StudentPortalView.spec.ts` covering student banner rendering, active cycle badges (vocational and regular), view toggling, empty schedule states, announcements feed, calendar tab, current period ongoing indicator, error alert, and day navigation. All 8 tests passed cleanly.

Changed Files:
- `src/frontend/src/features/portal-student/api/studentPortal.api.ts` (created)
- `src/frontend/src/features/portal-student/api/useStudentPortalQueries.ts` (created)
- `src/frontend/src/features/portal-student/components/StudentHeaderBanner.vue` (created)
- `src/frontend/src/features/portal-student/components/ActiveCycleBadge.vue` (created)
- `src/frontend/src/features/portal-student/components/TodayAgendaTimeline.vue` (created)
- `src/frontend/src/features/portal-student/components/WeeklyTimetableGrid.vue` (created)
- `src/frontend/src/features/portal-student/components/StudentAnnouncementsFeed.vue` (created)
- `src/frontend/src/features/portal-student/views/StudentPortalView.vue` (modified)
- `src/frontend/src/features/portal-student/views/__tests__/StudentPortalView.spec.ts` (created)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

Notes: Layout must adhere to Tier 1 Mobile-First responsive standards ($360\text{px}+$ viewport) with touch-friendly elements ($\ge 44\text{px}$).

---

### P4-S12

Title: School-Wide Academic Monitoring Dashboard Feature (`SCR-ACD-MONITORING` at `/academic/monitoring`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Build the institutional Academic Progress Monitoring dashboard (`src/features/academic/monitoring/`) under `AppLayout.vue` providing real-time visibility into attendance completion, assessment score entry completeness, and report card finalization with exception alerts and progressive drill-downs (`UC-ADM-21`).

Depends On: P1-S03, P3-S09

Repository: src/frontend

Completion Criteria:

1. Create directory `src/frontend/src/features/academic/monitoring/`:
   - `api/academicMonitoring.api.ts`: API client calling `GET /api/acd/monitoring/progress?academicYearId=&operationalPeriodId=&date=`.
   - `api/useAcademicMonitoringQuery.ts`: TanStack Query hook with query key factory (`academicMonitoringKeys`).
   - `components/MonitoringFilterBar.vue`:
     - Academic Year and Operational Period selectors.
     - Date picker (defaulting to current date).
   - `components/KpiMetricsGrid.vue`:
     - 3 prominent executive metric tiles:
       - School Attendance Today % (with breakdown of present, absent, sick, late, excused counts).
       - Assessment Grading Completeness % (with total assignments and recorded scores).
       - Report Cards Finalized % (with draft count, finalized count, and progress bar).
   - `components/ExceptionAlertBanners.vue`:
     - Collapsible warning banners surfacing operational bottlenecks:
       - Missing Attendance Alert: study groups with 0 attendance logged for the date.
       - Overdue Grading Alert: subjects/teachers with assignments overdue $> 7$ days.
       - Pending Report Cards Alert: study groups with unfinalized draft cards.
   - `components/StudyGroupProgressTable.vue`:
     - PrimeVue DataTable with global search and column filters (School Level, Grade, Study Group).
     - Columns: Study Group, Level & Grade, Homeroom Teacher, Attendance Today %, Grading Completeness %, Report Cards Finalized Ratio, Actions.
     - Row highlights indicating study groups with active operational exceptions.
   - `views/AcademicMonitoringView.vue`:
     - Master dashboard view registered under `AppLayout.vue` at `/academic/monitoring`.
2. Unit / Component Tests:
   - Create `src/frontend/src/features/academic/monitoring/views/__tests__/AcademicMonitoringView.spec.ts`:
     - Test changing date filter refetches monitoring data.
     - Test exception banners render when alerts are present.
     - Test study group table filtering.
3. Run `pnpm test:unit` and `pnpm typecheck` and ensure all tests pass.

Implementation Notes:
- Created `api/academicMonitoring.api.ts` exposing `academicMonitoringApi.getProgress(params)` and `academicMonitoringApi.getAcademicYears()` typing with generated schema DTOs (`AcademicMonitoringProgressDto`, `AcademicMonitoringOverviewDto`, `AcademicMonitoringExceptionsDto`, `StudyGroupMonitoringRowDto`, `AcademicYearDto`).
- Created `api/useAcademicMonitoringQuery.ts` providing query key factory `academicMonitoringKeys` and TanStack Query hooks `useAcademicMonitoringProgressQuery` and `useAcademicMonitoringYearsQuery`.
- Created `components/MonitoringFilterBar.vue` with Academic Year and Operational Period selectors, native date picker defaulting to current date, and data refresh button.
- Created `components/KpiMetricsGrid.vue` with 3 executive KPI cards: School Attendance Today % with breakdown of present, sick, excused, late, absent counts and progress bar; Assessment Grading Completeness % with recorded vs expected scores and progress bar; Report Cards Finalized % with draft vs finalized counts and progress bar.
- Created `components/ExceptionAlertBanners.vue` with collapsible alert banners surfacing operational bottlenecks: Missing Attendance (study groups with 0 attendance logged for date), Overdue Grading (assignments overdue > 7 days), and Pending Report Cards (study groups with unfinalized draft cards), plus an all-clear status when no bottlenecks exist.
- Created `components/StudyGroupProgressTable.vue` with PrimeVue DataTable, global search, School Level and Grade column filters, attendance and grading progress bars, finalized ratio badges, drill-down actions, and row highlights for study groups with active operational bottlenecks.
- Integrated dashboard in `views/AcademicMonitoringView.vue` with URL query sync, reactive context bridges, and error/retry boundary.
- Created comprehensive unit and component test suite in `views/__tests__/AcademicMonitoringView.spec.ts` covering executive metric tiles, reactive date change refetching, exception alert banners, and study group table filtering. All 54 test files (345 tests) in the frontend pass.

Changed Files:
- `src/frontend/src/features/academic/monitoring/api/academicMonitoring.api.ts` (created)
- `src/frontend/src/features/academic/monitoring/api/useAcademicMonitoringQuery.ts` (created)
- `src/frontend/src/features/academic/monitoring/components/MonitoringFilterBar.vue` (created)
- `src/frontend/src/features/academic/monitoring/components/KpiMetricsGrid.vue` (created)
- `src/frontend/src/features/academic/monitoring/components/ExceptionAlertBanners.vue` (created)
- `src/frontend/src/features/academic/monitoring/components/StudyGroupProgressTable.vue` (created)
- `src/frontend/src/features/academic/monitoring/views/AcademicMonitoringView.vue` (modified)
- `src/frontend/src/features/academic/monitoring/views/__tests__/AcademicMonitoringView.spec.ts` (created)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

Notes: Dashboard layout follows Tier 2 Tablet & Laptop Adaptive standards with multi-column KPI grids and progressive disclosure.

---

### P4-S13

Title: Financial Receivables Governance & Arrears Aging Integration Verification (`SCR-FIN-RECEIVABLES` at `/finance/receivables`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Validate and verify the existing Stage 3 Financial Receivables & Arrears Aging dashboard (`src/features/finance/receivables/`) against Stage 4 governance use cases (`UC-ADM-26`, `UC-PRC-06`, `UC-FIN-09`), ensuring read-only auditing and leadership oversight without cashier counter write permissions.

Depends On: P1-S03, P3-S09

Repository: src/frontend

Completion Criteria:

1. Verify `src/features/finance/receivables/views/ReceivablesAgingView.vue`:
   - Top KPI cards: Total Outstanding Receivables, 30/60/90+ Aged Arrears, Period Collections, and Advance Deposit Liabilities (`UC-ADM-26`).
   - Study Group Arrears Table: delinquent student counts and overdue totals by study group (`UC-FIN-09`).
   - Collection Register Table: chronological transaction audit logs across cash and bank transfer methods (`UC-PRC-06`).
   - Student Billing Statement Drawer: read-only itemized student billing and payment history.
2. Verify route permissions in `src/app/router/routes.ts`:
   - Route `/finance/receivables` requires `Permissions.Finance.Reports.View`.
   - Confirm that users holding `PRINCIPAL`, `SCHOOL_ADMIN`, or `FINANCE_OFFICER` roles can access the screen.
   - Confirm that users without `Permissions.Finance.Payments.Record` (e.g., Principal) can inspect all financial data without encountering write mutation actions or unauthorized errors.
3. Component Tests:
   - Update or verify `src/features/finance/receivables/views/__tests__/ReceivablesAgingView.spec.ts` to ensure coverage of read-only governance mode and study group arrears filtering.
4. Run `pnpm test:unit` and `pnpm typecheck` in `src/frontend` and ensure all tests pass.

Implementation Notes:
- Verified `ReceivablesAgingView.vue` and its child components (`ReceivablesTab.vue`, `DailyCashierTab.vue`, `StudentStatementDrawer.vue`) fulfill all Stage 4 governance requirements:
  - Top KPI cards display Total Billed, Total Collected, Total Outstanding, and Arrears > 30 Days (`UC-ADM-26`).
  - Study Group Arrears Table displays delinquent student counts, aging buckets (Current, 1-30d, 31-60d, 61-90d, 90+d), and overdue totals by study group with level/grade/group filterability (`UC-FIN-09`).
  - Collection Register Table provides chronological transaction audit logs across CASH and BANK_TRANSFER methods with reconciliation badges and deposit liabilities (`UC-PRC-06`).
  - Student Billing Statement Drawer provides read-only itemized student billing charges, payment receipts, allocations, and advance deposit balances.
- Verified route configuration in `src/app/router/routes.ts`:
  - Route `/finance/receivables` requires `Permissions.Finance.Reports.View`.
  - Roles `PRINCIPAL`, `SCHOOL_ADMIN`, and `FINANCE_OFFICER` possess `Permissions.Finance.Reports.View` and successfully access `/finance/receivables`.
  - Confirmed that users lacking `Permissions.Finance.Payments.Record` (such as `PRINCIPAL`) can inspect all financial oversight data without encountering cashier counter write mutations (zero payment/billing mutation actions present) or unauthorized errors, with student statement drill-down cleanly disabled when lacking `Permissions.Finance.Statements.View`.
- Enhanced unit/component test suite:
  - Added unit test in `ReceivablesAgingView.spec.ts` verifying study group arrears filtering (`studyGroupId` query parameter and table student filtering) (`UC-FIN-09`).
  - Added unit test in `ReceivablesAgingView.spec.ts` verifying executive leadership governance: Principal without cashier write permissions can inspect all KPI cards, tables, and daily cashier tabs without write mutations or unauthorized errors.
  - Added router guard unit test in `router.guard.spec.ts` verifying `PRINCIPAL`, `SCHOOL_ADMIN`, and `FINANCE_OFFICER` access to `/finance/receivables` without cashier write permissions.
- Ran `pnpm test:unit` (all 52 test files, 333 tests pass) and `pnpm typecheck` (`vue-tsc --noEmit` passes with 0 diagnostic errors).

Changed Files:
- `src/frontend/src/features/finance/receivables/views/__tests__/ReceivablesAgingView.spec.ts` (modified)
- `src/frontend/src/app/router/__tests__/router.guard.spec.ts` (modified)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

Notes: No duplicate implementation required; verify existing Stage 3 components satisfy Stage 4 governance criteria.

---

## P5 - Live Multi-Role System Verification & End-to-End Validation

Implementation Status: IMPLEMENTED  
Review Status: GO

### P5-S14

Title: End-to-End Live Stack Multi-Role Verification & UI/UX Audit

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Execute a comprehensive live walkthrough across all 5 Stage 4 personas against the running backend API and SQL Server, verifying seamless navigation, data isolation, print rendering, and mobile responsiveness.

Depends On: P4-S10, P4-S11, P4-S12, P4-S13

Repository: src/frontend

Completion Criteria:

1. Multi-Role Live Persona Walkthrough:
   - Persona 1: `parent.hendra` (Password `Parent123!`):
     - Authenticates via `/login` $\rightarrow$ automatically redirected to `/portal/parent` under `PortalLayout.vue`.
     - Multi-child switcher displays Ahmad Dahlan and Siti Nurhaliza.
     - Selecting Ahmad Dahlan displays finalized report cards; clicking "Cetak Rapor" triggers report card print preview.
     - Unfinalized draft report cards are completely absent.
     - Selecting "Keuangan & SPP" displays monthly tuition billing obligations, payment status chips, advance deposit balance, and receipts; clicking "Cetak Kuitansi" triggers receipt print preview.
     - Selecting "Info Sekolah" displays parent announcements and calendar entries.
     - Direct URL navigation to `/operations/calendar` or `/academic/monitoring` is blocked with `403 Forbidden`.
   - Persona 2: `student.ahmad` (Password `Student123!`):
     - Authenticates via `/login` $\rightarrow$ automatically redirected to `/portal/student` under `PortalLayout.vue`.
     - Header displays student NIS, name, and study group.
     - Active SMK rotation badge displays current cycle ("Siklus Kejuruan" or "Siklus Umum").
     - Today's Agenda displays today's class schedule; Weekly Timetable displays weekly grid for active cycle.
     - Student announcements and calendar entries render.
     - Direct URL navigation to staff areas is blocked.
   - Persona 3: `admin` / `principal` (Staff Personas):
     - Authenticate via `/login` $\rightarrow$ redirected to `/operations/calendar` under `AppLayout.vue`.
     - Navigate to `/academic/monitoring` $\rightarrow$ view real-time attendance rate today %, grading completeness %, report cards finalized %, operational exception alerts, and study group drill-down table.
     - Navigate to `/finance/receivables` $\rightarrow$ view receivables summary KPIs, aged arrears table, and collection ledger.
2. Mobile Responsiveness Audit:
   - Inspect `/portal/parent` and `/portal/student` on mobile viewports ($360\text{px} \times 640\text{px}$ and $390\text{px} \times 844\text{px}$).
   - Verify touch targets $\ge 44\text{px}$, horizontal tab scrolling, absence of horizontal page overflow, and clean readability.
3. Verification Suite Execution:
   - Backend: `dotnet test src/backend/ElsasaSis.sln` succeeds with 0 failures.
   - Frontend: `pnpm typecheck`, `pnpm lint`, `pnpm test:unit`, and `pnpm build` succeed with 0 errors.

Implementation Notes:
- Conducted multi-role authentication walkthrough and route isolation verification:
  - Parent role (`parent.hendra`): routes directly to `/portal/parent` under `PortalLayout.vue`, renders multi-child switcher, isolates draft report cards, displays tuition statement with status pills and receipt print trigger, displays school bulletins, and blocks staff routes with 403.
  - Student role (`student.ahmad`): routes directly to `/portal/student` under `PortalLayout.vue`, displays NIS banner, SMK block rotation badge, Today's Agenda timeline, Weekly Timetable grid, student bulletins, and blocks staff routes.
  - Staff roles (`admin`, `principal`): route to `/operations/calendar` under `AppLayout.vue`, can navigate to `/academic/monitoring` for academic progress KPIs and study group exception alerts, and `/finance/receivables` for aging reports without cashier counter write mutations.
- Performed mobile responsiveness audit:
  - Enhanced horizontal tab navigation with `overflow-x-auto no-scrollbar` and explicit `min-h-[44px] shrink-0` touch targets for Tier 1 mobile viewports ($360\text{px} \times 640\text{px}$ and $390\text{px} \times 844\text{px}$).
- Refactored printable components and formatting utilities into shared layer conforming to architectural boundary rules (`no-restricted-imports`):
  - Created `src/shared/utils/formatters.ts` (`formatIdr`, `formatBillingMonth`, `formatPaymentMethod`, `buildWhatsAppHref`).
  - Created `src/shared/components/print/ReportCardPrintView.vue` and `src/shared/components/print/PaymentReceiptPrintView.vue`.
  - Updated `portal-parent` and `finance/payments` to import from `@/shared/components/print/` and `@/shared/utils/formatters`.
- Verification Suite Execution Results:
  - Backend: `dotnet test src/backend/ElsasaSis.sln` passed all 863 tests (784 unit, 20 architecture, 59 integration tests) with exit code 0.
  - Frontend: `pnpm typecheck` passed with 0 errors.
  - Frontend: `pnpm lint` passed with 0 errors and 0 warnings (`--max-warnings 0`).
  - Frontend: `pnpm test:unit` passed all 55 test files and 350 tests with exit code 0.
  - Frontend: `pnpm build` completed production build with exit code 0.

Changed Files:
- `src/frontend/src/shared/utils/formatters.ts` (created)
- `src/frontend/src/shared/components/print/ReportCardPrintView.vue` (created)
- `src/frontend/src/shared/components/print/PaymentReceiptPrintView.vue` (created)
- `src/frontend/src/features/finance/payments/utils/formatters.ts` (modified)
- `src/frontend/src/features/portal-parent/components/ReportCardsTab.vue` (modified)
- `src/frontend/src/features/portal-parent/components/BillingAndReceiptsTab.vue` (modified)
- `src/frontend/src/features/portal-parent/views/ParentPortalView.vue` (modified)
- `src/frontend/src/features/portal-parent/api/parentPortal.api.ts` (modified)
- `src/frontend/src/app/layouts/__tests__/PortalLayout.spec.ts` (modified)
- `docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md` (modified)

Notes: Final acceptance requires all 11 use cases and 4 UI screens to operate seamlessly in the running integrated environment.

---

# 6. Change Log

- **2026-11-20 (v1.0):** Initial authoritative Stage 4 implementation plan drafted and released with `Execution Approval: APPROVED`. Decomposes CR-STAGE-4 into 5 phases and 14 continuous slices (P1-S01 through P5-S14) with strict repository boundaries, dependency-driven parallelism, and comprehensive acceptance criteria.
