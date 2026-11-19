---
Title: Stage 0 Walking Skeleton & Reference Slice — Implementation Plan
Code: CR-STAGE-0
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-18
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Deliver an executable, dependency-driven implementation plan for **CR-STAGE-0** (**Stage 0: Walking Skeleton & Reference Slice** of **ELSASA-SIS v4 Basic Level**), transforming the current pre-implementation greenfield workspace (`src/backend/` and `src/frontend/` empty) into the approved target technical realization.

Planning Mode: `FEATURE-PLANNING` (scoping `CR-STAGE-0` Stage 0 Walking Skeleton & Reference Slice over a pre-implementation greenfield codebase)

Referenced artifacts:

- ISSUE: [IS001- STAGE-0-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-ISSUE.md) (`CR-STAGE-0`)
- FEASIBILITY-ASSESSMENT: [IS001- STAGE-0-DEV-FEASIBILITY-ASSESMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-FEASIBILITY-ASSESMENT.md) (`Status: READY-FOR-PLANNING`)
- ARCHITECTURE: [IS001- STAGE-0-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-ARCHITECTURE.md) (`Version: 1.0`)
- Supporting Baseline Specifications: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md), [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md), [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md), [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md), [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)

Architecture Applicability: `ARCHITECTURE-REQUIRED` — Target architecture defined in [IS001- STAGE-0-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-ARCHITECTURE.md).

---

# 2. Planning Scope

Scope covers all Stage 0 deliverables across two strict repository boundaries (`src/backend` and `src/frontend`):

1. **Backend (`src/backend`):**
   - `.NET 8 LTS` (`net8.0` / C# 12) solution `ElsasaSis.sln` containing `ElsasaSis.SharedKernel`, `ElsasaSis.Contracts`, `ElsasaSis.Database`, `ElsasaSis.Modules.Auth`, `ElsasaSis.Modules.Opr`, `ElsasaSis.Api`, `ElsasaSis.UnitTests`, `ElsasaSis.ArchitectureTests`, and `ElsasaSis.IntegrationTests`.
   - DbUp migration runner (`001_CreateSchemas.sql` through `010_CreateOprAcademicYearTables.sql`) creating all 8 schemas, Stage 0 tables (`auth.*`, `outbox.messages`, `opr.academic_years`, `opr.operational_periods` with `status`), and idempotent `admin` (`SCHOOL_ADMIN`) seed.
   - Custom JWT Bearer + `HttpOnly` refresh cookie authentication (`POST /api/auth/login`, `/refresh`, `/logout`, `GET /api/auth/me`).
   - `UC-ADM-01` Academic Year & Operational Period lifecycle domain model (`BR-OPS-001..006`, `OQ-003`), Dapper persistence, `IOperationalContract`, domain events, and 6 Minimal API slices (`/api/opr/academic-years*`).
   - `ElsasaSis.Api` host pipeline (Serilog, `CorrelationIdMiddleware`, RFC 7807 `ValidationFilter<TRequest>` with `camelCase` field keys, global exception handler, CORS, Swagger/OpenAPI, `Development` auto-migration).
   - Automated NetArchTest, Unit, and `WebApplicationFactory` + `Respawn` integration test suites against local SQL Server / `MSSQLLocalDB` (no Docker).

2. **Frontend (`src/frontend`):**
   - Vue 3.5+ / TypeScript 5.6+ / Vite 6+ / Tailwind CSS v4 / PrimeVue v4 (`Aura` preset + PassThrough) / TanStack Query v5 / Pinia v2 / VeeValidate v4 + Zod / Axios (`withCredentials: true`, Vite `/api` dev proxy) / `openapi-typescript`.
   - Core shell (`AppLayout.vue` with Top Bar Academic Year & Semester Switcher, Historical Period Warning Banner, collapsible role-filtered sidebar; `AuthLayout.vue`; Pinia stores `useAuthStore`, `useAppContextStore`, `useUIStore`; Vue Router guards; shared primitives).
   - `SCR-AUTH-LOGIN` (`LoginView.vue` at `/auth/login` + `/login` redirect) and `SCR-OPR-CALENDAR` Part 1 (`CalendarWorkspaceView.vue` at `/operations/calendar` + `/` redirect).
   - Vitest + `@vue/test-utils` + MSW unit/component tests, `vue-tsc`, ESLint flat config boundary rules, and live Stage 0 smoke verification against running local API + Vite dev server.

3. **Excluded from Stage 0:**
   - Stages 1–4 modules (`Std`, `Tch`, `Acd`, `Fin`, `Dev`) assemblies/tables (beyond empty SQL schemas in `001_CreateSchemas.sql`), `SCR-OPR-CALENDAR` Part 2 (`opr.calendar_entries`, `UC-ADM-02`), Outbox background polling `BackgroundService`, multi-tenant `org_unit_id`, and Playwright E2E suite.

---

# 3. Dependencies

External/Environment prerequisites verified in the Feasibility Assessment:

- `.NET 8 SDK` (`8.0.428` installed; target `<TargetFramework>net8.0</TargetFramework>`)
- `Node.js` (`v22.22.3`) and `pnpm` (`11.9.0`)
- Local Microsoft SQL Server (`MSSQLSERVER` / `MSSQLLocalDB`) — Docker is not installed and must not be required.

For slice dependencies:

- `Depends On` declares implementation prerequisites.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation status `IMPLEMENTED` and verifiable repository output.
- Dependency satisfaction does not require review status `GO`.
- Each slice targets strictly one repository boundary (`src/backend` or `src/frontend`), never both.

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
| P1 | IN-PROGRESS | NOT-REVIEWED | 2/3 |
| P2 | NOT-STARTED | NOT-REVIEWED | 0/5 |
| P3 | NOT-STARTED | NOT-REVIEWED | 0/4 |
| P4 | COMPLETED | GO | 1/1 |

---

# 5. Phases

## P1 - Foundation & Scaffolding

Implementation Status: IN-PROGRESS
Review Status: NOT-REVIEWED

### P1-S01

Title: Initialize Backend Solution, SharedKernel, Contracts & Unit Test Harness

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Establish the `.NET 8` backend solution structure, project references, `ElsasaSis.SharedKernel`, `ElsasaSis.Contracts`, and `ElsasaSis.UnitTests` so all downstream backend slices can build and test in parallel without `.sln` or `.csproj` conflicts.

Depends On: None

Repository: src/backend

Completion Criteria:

1. `src/backend/Directory.Build.props` configures `net8.0`, C# 12 (`LangVersion` 12 / default `net8.0`), `Nullable` enabled, and `ImplicitUsings` enabled.
2. `src/backend/ElsasaSis.sln` exists and includes all 9 Stage 0 projects:
   - `src/backend/src/ElsasaSis.SharedKernel/ElsasaSis.SharedKernel.csproj`
   - `src/backend/src/ElsasaSis.Contracts/ElsasaSis.Contracts.csproj` (references `SharedKernel`)
   - `src/backend/src/ElsasaSis.Database/ElsasaSis.Database.csproj` (compilable CLI placeholder `Program.cs` + `dbup-sqlserver`, `Microsoft.Extensions.Identity.Core`, `Microsoft.Extensions.Configuration.*` packages)
   - `src/backend/src/Modules/ElsasaSis.Modules.Auth/ElsasaSis.Modules.Auth.csproj` (references `SharedKernel`, `Contracts`; packages `Dapper`, `FluentValidation`, `Microsoft.Extensions.Identity.Core`, `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`)
   - `src/backend/src/Modules/ElsasaSis.Modules.Opr/ElsasaSis.Modules.Opr.csproj` (references `SharedKernel`, `Contracts`; packages `Dapper`, `FluentValidation`, `MediatR`)
   - `src/backend/src/ElsasaSis.Api/ElsasaSis.Api.csproj` (compilable Web host placeholder `Program.cs` referencing `SharedKernel`, `Contracts`, `Database`, `Modules.Auth`, `Modules.Opr`; packages `Serilog.AspNetCore`, `Swashbuckle.AspNetCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`)
   - `src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` (references `SharedKernel`, `Contracts`, `Modules.Auth`, `Modules.Opr`)
   - `src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj` (package `NetArchTest.Rules`; references `SharedKernel`, `Contracts`, `Modules.Auth`, `Modules.Opr`, `Api`)
   - `src/backend/tests/ElsasaSis.IntegrationTests/ElsasaSis.IntegrationTests.csproj` (packages `Microsoft.AspNetCore.Mvc.Testing`, `Respawn`, `Microsoft.Data.SqlClient`, `Dapper`; references `Api`, `Database`, `SharedKernel`, `Contracts`)
3. `ElsasaSis.SharedKernel` implements: `Entity<TId>`, `AggregateRoot<TId>`, `DomainEventBase` (`MediatR.INotification`), `Result` / `Result<T>`, `DomainError` / `ErrorType`, `DomainErrorExtensions.ToProblemDetails()` (RFC 7807 `404`/`409`/`422`/`401`/`403` with `errorCode` and optional `errors`), `UuidV7.NewGuid()` (RFC 9562 UUID v7), `ISqlConnectionFactory`, `IDomainEventDispatcher`, `IEndpoint`, `ICurrentUser` (`UserId`, `Username`, `FullName`, `Roles`, `Permissions`, `TeacherId`, `StudentId`, `GuardianId` — omitting `org_unit_id`), `PagedQuery`, and `PaginatedResponse<T>`.
4. `ElsasaSis.Contracts` implements `IOperationalContract` (active academic year & active operational period lookup DTO/methods) and public domain event records `AcademicYearActivatedDomainEvent` and `OperationalPeriodActivatedDomainEvent`.
5. `ElsasaSis.UnitTests` contains passing unit tests verifying `UuidV7.NewGuid()` RFC 9562 version/variant bits and monotonicity, `Result<T>`, and `DomainError.ToProblemDetails()`.
6. `dotnet build src/backend/ElsasaSis.sln` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` succeed with exit code `0`.

Notes: Pre-registering all Stage 0 `.csproj` skeletons in `ElsasaSis.sln` during `P1-S01` eliminates `.sln`/`.csproj` merge collisions when `P1-S02`, `P2-S04`, and `P2-S05` execute concurrently.

Implementation Notes: Created `src/backend/Directory.Build.props`, `src/backend/ElsasaSis.sln` with all 9 Stage 0 `.csproj` skeletons, compilable `Program.cs` placeholders for `ElsasaSis.Database` and `ElsasaSis.Api`, `ElsasaSis.SharedKernel` primitives, `ElsasaSis.Contracts` (`IOperationalContract`, `AcademicYearActivatedDomainEvent`, `OperationalPeriodActivatedDomainEvent`), and 11 passing unit tests in `ElsasaSis.UnitTests`. Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors) and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` (11/11 passed).

---

### P1-S02

Title: Implement DbUp Migration Engine, Stage 0 SQL Scripts & Idempotent Admin Seeder

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement `ElsasaSis.Database` with embedded Pure Persistence SQL migration scripts for all 8 schemas and Stage 0 tables, an embeddable `DatabaseMigrator` service, idempotent `admin` (`SCHOOL_ADMIN`) seeding, and a standalone CLI runner.

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. Embedded SQL scripts exist under `src/backend/src/ElsasaSis.Database/Scripts/` (configured as `EmbeddedResource` in `.csproj`):
   - `001_CreateSchemas.sql`: idempotently creates schemas `auth`, `outbox`, `opr`, `std`, `tch`, `acd`, `fin`, `dev`.
   - `002_CreateAuthTables.sql`: creates `auth.users`, `auth.user_roles`, `auth.refresh_tokens`, `auth.user_actors` matching `ELSASA-SIS-ARCHITECTURE.md` §12.2 (intra-schema FKs to `auth.users(id)`, `UNIQUE(username)`).
   - `003_CreateOutboxTables.sql`: creates `outbox.messages` matching `ELSASA-SIS-ARCHITECTURE.md` §12.2.
   - `010_CreateOprAcademicYearTables.sql`: creates `opr.academic_years` and `opr.operational_periods` (including `status VARCHAR(20) NOT NULL`, `UQ_academic_years_name`, `UQ_operational_periods_year_name`, `IX_academic_years_status`, `IX_operational_periods_year_dates`, `IX_operational_periods_status`, zero DB FKs).
2. All SQL scripts strictly obey Pure Persistence rules: zero `DEFAULT` constraints, zero `CHECK` constraints, zero triggers, zero stored procedures, zero cross-schema foreign keys, `DATETIME2(3) NOT NULL` `created_at` and `updated_at`.
3. `DatabaseMigrator` ensures the target database exists (`EnsureDatabase.For.SqlDatabase`), executes DbUp scripts idempotently, and runs an idempotent `AdminSeeder` inserting the `admin` user (`SCHOOL_ADMIN` role) using ASP.NET Core PBKDF2 `PasswordHasher<object>` with password from `Seed:AdminPassword` (defaulting to `Admin123!` in `Development` only, failing fast in non-Development if unset when `admin` does not yet exist) without overwriting existing credentials on re-run.
4. `ElsasaSis.Database/Program.cs` runs `DatabaseMigrator` cleanly as a CLI tool (`dotnet run --project src/backend/src/ElsasaSis.Database`) reading connection string from configuration/args.
5. `dotnet build src/backend/ElsasaSis.sln` succeeds and executing `ElsasaSis.Database` against a local SQL Server / `MSSQLLocalDB` test database creates all 8 schemas, all 7 Stage 0 tables, and seeds `admin` idempotently across repeated runs.

Notes: Keep `ElsasaSis.Database` free of references to `Modules.*` or `ElsasaSis.Api` (`backend-architecture.md` §9.2).

Implementation Notes: Implemented embedded Pure Persistence SQL scripts (`001_CreateSchemas.sql`, `002_CreateAuthTables.sql`, `003_CreateOutboxTables.sql`, `010_CreateOprAcademicYearTables.sql`), `DatabaseMigrator` (DbUp `EnsureDatabase` + embedded script execution), `AdminSeeder` (PBKDF2 `PasswordHasher<object>`, `Seed:AdminPassword` with `Development`-only `Admin123!` fallback and non-Development fail-fast), and `ElsasaSis.Database/Program.cs` CLI runner. Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors), repeated idempotent runs against `(localdb)\MSSQLLocalDB` creating all 8 schemas, all 7 Stage 0 tables, and `admin` (`SCHOOL_ADMIN`), non-Development fail-fast when `Seed:AdminPassword` is unset, and `ElsasaSis.UnitTests` (11/11 passed).

---

### P1-S03

Title: Scaffold Frontend Workspace, Design System, Shared UI Primitives & HTTP Core

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Initialize `src/frontend/` with Vue 3.5+, TypeScript 5.6+, Vite 6+, Tailwind CSS v4, PrimeVue v4 (`Aura`), ESLint flat config, Vitest + MSW test setup, shared types/formatters, `http.ts` Axios client with single-flight refresh hook, and shared UI primitives.

Depends On: None

Repository: src/frontend

Completion Criteria:

1. `src/frontend/package.json` and `pnpm-lock.yaml` install cleanly (`pnpm install`) with Vue 3.5+, TypeScript 5.6+, Vite 6+, Tailwind CSS v4 (`@tailwindcss/vite` or PostCSS), PrimeVue v4 (`Aura` theme), `lucide-vue-next`, `vue-router`, `pinia`, `@tanstack/vue-query`, `axios`, `vee-validate`, `@vee-validate/zod`, `zod`, `date-fns`, `openapi-typescript`, `vitest`, `@vue/test-utils`, `msw`, `eslint`, `prettier`, and `vue-tsc`.
2. `vite.config.ts` configures `@` alias (`src/`), Vite `/api` dev proxy (`VITE_API_PROXY_TARGET` defaulting to `http://localhost:5000`), and Vitest config; `eslint.config.js` enforces architectural import boundaries (`shared/` cannot import `features/` or `app/`; features cannot cross-import).
3. `src/frontend/src/shared/styles/main.css` uses `@import "tailwindcss";`, `@layer primevue, custom;`, and `@theme` tokens (`brand-primary-900`, `brand-accent-600`, etc.); `src/frontend/src/app/config/primevue.ts` configures PrimeVue `Aura` preset and PassThrough defaults.
4. `src/frontend/src/shared/` implements:
   - `types/common.ts` (`ApiError`, `PaginatedResponse<T>`, `PagedQuery`, `ProblemDetailsPayload`)
   - `utils/dateUtils.ts` (`date-fns` helpers for `dd MMM yyyy`, `dd MMM yyyy, HH:mm`, `yyyy-MM-dd`)
   - `services/http.ts` (`apiClient` with `withCredentials: true`, `X-Correlation-ID`, `configureHttpClient()`, single-flight `401` refresh queue, RFC 7807 `ApiError` mapper)
   - `components/feedback/` (`AppErrorBoundary.vue`, `StatusPill.vue`, `ConfirmActionDialog.vue`, `EmptyState.vue`)
   - `components/forms/FormField.vue`
   - `components/tables/AppDataTable.vue`
   - `views/ForbiddenView.vue` and `views/NotFoundView.vue`
5. Vitest unit tests verify `dateUtils.ts`, `ApiError` RFC 7807 parsing, and `http.ts` single-flight `401` refresh behavior using MSW.
6. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass in `src/frontend/`.

Notes: Strictly no `localStorage`/`sessionStorage` token persistence and no `v-html` in business views.

---

## P2 - Domain Modules, API Host & Frontend Shell

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P2-S04

Title: Implement `ElsasaSis.Modules.Auth` (JWT, Refresh Cookie Lifecycle & `/api/auth/*` Slices)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement the Auth module (`auth` schema persistence, compile-time `Role -> Permissions[]` catalog, PBKDF2 verification, JWT issuance, SHA-256 refresh token rotation with `HttpOnly` cookie, and `/api/auth/login`, `/refresh`, `/logout`, `/me` endpoints).

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. `ElsasaSis.Modules.Auth` implements:
   - `Permissions.cs` (covering `Permissions.Operations.Calendar.View`, `Permissions.Operations.Calendar.Manage`, etc.) and compile-time `RolePermissionCatalog` mapping `SCHOOL_ADMIN` (and the 7-role taxonomy) to `Permissions.*` constants.
   - `Domain/` entities/value objects, `AuthErrors` (`InvalidCredentials`, `InactiveUser`, `InvalidRefreshToken`, `Unauthorized`), and `IAuthRepository`.
   - `Infrastructure/` Dapper repository targeting `auth.users`, `auth.user_roles`, `auth.refresh_tokens`, `auth.user_actors`; `JwtTokenService` emitting `sub`, `preferred_username`, `name`, `roles`, `permissions`, and optional `teacherId`, `studentId`, `guardianId` claims (never `org_unit_id`); `PasswordHasher<object>` verification; SHA-256 refresh token generation/hashing and `HttpOnly`, `SameSite=Strict` (configurable) cookie writer scoped to `/api/auth`.
   - `Features/Auth/` vertical slices (`Login`, `Refresh`, `Logout`, `GetMe`), each with Endpoint (`.Produces<T>()` / `.ProducesProblem(...)`, `ValidationFilter`), Request/Response DTOs, FluentValidation validator, and Handler.
   - `DependencyInjection.cs` and `AuthAssemblyMarker.cs`.
2. `ElsasaSis.UnitTests/Auth/` contains passing unit tests for `RolePermissionCatalog`, refresh token hashing, and JWT claim generation.
3. `dotnet build src/backend/ElsasaSis.sln` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` pass with zero cross-module references.

Notes: `Modules.Auth` must only reference `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`.

Implementation Notes: Implemented `ElsasaSis.Modules.Auth` (`Permissions.cs`, `RolePermissionCatalog.cs`, `Domain/` entities/value objects/errors/repository contract, `Infrastructure/` Dapper `AuthRepository`, `JwtTokenService`, `RefreshTokenService`, `RefreshTokenCookieService`, `JwtOptions`, `Features/Auth/` vertical slices for `Login`, `Refresh`, `Logout`, `GetMe` with `ValidationFilter<LoginRequest>`, `DependencyInjection.cs`, and `AuthAssemblyMarker.cs`) and unit tests in `ElsasaSis.UnitTests/Auth/`. Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors) and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` (25/25 passed).

---

### P2-S05

Title: Implement `ElsasaSis.Modules.Opr` Domain Model, Persistence & `IOperationalContract`

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement the `AcademicYear` Aggregate Root, `OperationalPeriod` child entity, `OprErrors`, Dapper transactional `AcademicYearRepository`, `IOperationalContract` implementation, and full unit tests for `BR-OPS-001..006` and `OQ-003`.

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. `ElsasaSis.Modules.Opr/Domain/` contains zero references to `Infrastructure`, `Features`, `Dapper`, SQL, or ASP.NET HTTP types, and implements:
   - `AcademicYear` (Aggregate Root) & `OperationalPeriod` (owned entity), `AcademicYearStatus` (`INACTIVE | ACTIVE`), `OperationalPeriodStatus` (`INACTIVE | ACTIVE`).
   - `Create(...)`: enforces `BR-OPS-005` (`endDate > startDate`), `BR-OPS-002` (>= 1 period), `BR-OPS-006` (period `endDate > startDate`), `BR-OPS-003` (periods inside year bounds), `BR-OPS-004` (non-overlapping periods), unique period names within year; assigns `UuidV7.NewGuid()` and UTC timestamps; starts `INACTIVE`.
   - `Update(...)`: permitted for both `INACTIVE` and `ACTIVE` years; enforces `BR-OPS-002..006` & unique period names; preserves `ACTIVE` status on the year and any active period; returns `409 Conflict` (`OprErrors`) if attempting to remove the currently `ACTIVE` period.
   - `Activate(...)`: activates target year, activates earliest period by `start_date` if none active, raises `AcademicYearActivatedDomainEvent` and `OperationalPeriodActivatedDomainEvent`.
   - `ActivatePeriod(Guid periodId)`: requires parent year `ACTIVE` (`409 Conflict` otherwise), deactivates other periods, activates target period, raises `OperationalPeriodActivatedDomainEvent`.
   - `OprErrors` (`NotFound`, `DuplicateYearName`, `DuplicatePeriodName`, `InvalidDateRange`, `PeriodOutOfBounds`, `OverlappingPeriods`, `AtLeastOnePeriodRequired`, `CannotRemoveActivePeriod`, `YearNotActiveForPeriodActivation`).
   - `IAcademicYearRepository` (defined in `Domain/`).
2. `ElsasaSis.Modules.Opr/Infrastructure/` implements `AcademicYearRepository` (Dapper with explicit `SqlTransaction` handling single-active-year deactivation + activation and atomic `opr.operational_periods` updates, populating UTC `created_at` and `updated_at`) and `OperationalContract` (`IOperationalContract`).
3. `Permissions.cs` (`Permissions.Operations.Calendar.View`, `Permissions.Operations.Calendar.Manage`) and `OprAssemblyMarker.cs` exist.
4. `ElsasaSis.UnitTests/Opr/` thoroughly tests all `AcademicYear` / `OperationalPeriod` invariants (`BR-OPS-001..006`, `OQ-003` activation, update, active period removal rejection, and domain event raising).
5. `dotnet build src/backend/ElsasaSis.sln` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` pass.

Notes: Target only `Modules.Opr/Domain/`, `Modules.Opr/Infrastructure/`, `Permissions.cs`, `OprAssemblyMarker.cs`, and `UnitTests/Opr/` so `P2-S04` and `P2-S05` execute cleanly in parallel.

Implementation Notes: Implemented `ElsasaSis.Modules.Opr` (`Permissions.cs`, `OprAssemblyMarker.cs`, `Domain/` with `AcademicYear`, `OperationalPeriod`, `OperationalPeriodInput`, `AcademicYearStatus`, `OperationalPeriodStatus`, `OprErrors`, and `IAcademicYearRepository`, `Infrastructure/Persistence/AcademicYearRepository.cs` using Dapper + `SqlTransaction`, `Infrastructure/Contracts/OperationalContract.cs` implementing `IOperationalContract`) and unit tests in `ElsasaSis.UnitTests/Opr/AcademicYearTests.cs`. Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors) and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` (40/40 passed).

---

### P2-S06

Title: Implement `ElsasaSis.Modules.Opr` `UC-ADM-01` Vertical Slices & DI Registration

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement all 6 `UC-ADM-01` vertical slices (`GetAcademicYears`, `GetAcademicYearById`, `CreateAcademicYear`, `UpdateAcademicYear`, `ActivateAcademicYear`, `ActivateOperationalPeriod`) and `DependencyInjection.cs` in `ElsasaSis.Modules.Opr`.

Depends On: P2-S05

Repository: src/backend

Completion Criteria:

1. Read slices under `Features/AcademicYears/` query `opr.academic_years` and `opr.operational_periods` directly via `ISqlConnectionFactory` + Dapper without Aggregate Roots:
   - `GetAcademicYears` (`GET /api/opr/academic-years`): binds `PagedQuery` and returns `PaginatedResponse<AcademicYearDto>` with each item's nested `periods` (`OperationalPeriodDto[]`).
   - `GetAcademicYearById` (`GET /api/opr/academic-years/{id}`): returns `AcademicYearDto` with nested `periods`, or `404 NotFound`.
2. Write slices (`CreateAcademicYear` `POST`, `UpdateAcademicYear` `PUT`, `ActivateAcademicYear` `POST .../activate`, `ActivateOperationalPeriod` `POST .../periods/{periodId}/activate`) contain zero SQL and zero business-rule `if` logic:
   - `FluentValidation` validators check input format only (required fields, max lengths, non-empty period list structure).
   - Write handlers orchestrate Repository fetch/save -> Domain method -> post-commit `IDomainEventDispatcher.DispatchAsync(...)` -> thin response DTO.
3. Every endpoint implements `IEndpoint`, sets `.WithTags("Operations - Academic Years")`, `.RequireAuthorization(...)` (`Permissions.Operations.Calendar.View` / `.Manage`), `.AddEndpointFilter<ValidationFilter<T>>()` where applicable, and explicit `.Produces<T>()` / `.ProducesProblem(...)` OpenAPI metadata.
4. `DependencyInjection.cs` registers handlers, validators, `IAcademicYearRepository`, and `IOperationalContract`.
5. `dotnet build src/backend/ElsasaSis.sln` passes.

Notes: Ensure `PUT /api/opr/academic-years/{id}` maps unique constraint violations (if any race occurs) cleanly or checks uniqueness cleanly without bypassing Domain invariants.

Implementation Notes: Implemented all 6 `UC-ADM-01` vertical slices under `ElsasaSis.Modules.Opr/Features/AcademicYears/` (`GetAcademicYears`, `GetAcademicYearById`, `CreateAcademicYear`, `UpdateAcademicYear`, `ActivateAcademicYear`, `ActivateOperationalPeriod`), shared `AcademicYearDto`/`OperationalPeriodDto`/`ValidationFilter<T>`, and `DependencyInjection.cs` (`AddOprModule`). Verified `dotnet build src/backend/ElsasaSis.sln` (0 warnings, 0 errors) and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` (40/40 passed).

---

### P2-S07

Title: Implement Frontend App Shell, Pinia Stores, Router Guards & Layouts (`BND-FE-CORE`)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Build the Vue 3 SPA application layer (`src/frontend/src/app/`): Pinia stores (`useAuthStore`, `useAppContextStore`, `useUIStore`), HTTP client hook wiring, `AuthLayout.vue`, `AppLayout.vue` (Top Bar switcher shell, Historical Period Warning Banner, collapsible role-filtered sidebar), Vue Router with auth/permission guards, and `main.ts` bootstrap.

Depends On: P1-S03

Repository: src/frontend

Completion Criteria:

1. `src/frontend/src/app/stores/` implements:
   - `auth.store.ts`: in-memory `accessToken`, `user`, `roles`, `permissions`, `isAuthenticated`, `hasPermission()`, `hasAnyRole()`, `setSession()`, `clearSession()` (never `localStorage`/`sessionStorage`).
   - `appContext.store.ts`: `selectedAcademicYearId`, `selectedPeriodId`, `activeAcademicYearId`, `activePeriodId`, `isHistoricalPeriod`, `setActiveContext()`, `selectContext()` (no `org_unit_id`).
   - `ui.store.ts`: `isSidebarCollapsed`, `toggleSidebar()`, breadcrumbs state.
2. `src/frontend/src/app/providers/http.provider.ts` wires `configureHttpClient()` to `useAuthStore` (access token + refresh callback + session expiration redirect) and `useAppContextStore` (`X-Active-Period-ID` when set, no `X-Organization-Unit-ID`).
3. `src/frontend/src/app/layouts/`:
   - `AuthLayout.vue`: centered card shell on `slate-50` without sidebar or operational chrome.
   - `AppLayout.vue`: `56px` (`h-14`) dark Top Bar (`brand-primary-900`) with sidebar toggle, brand title, Academic Year & Semester Switcher slot/control, role badge + user profile menu with logout; persistent high-contrast amber Historical Period Warning Banner when `appContextStore.isHistoricalPeriod` is true; collapsible role-filtered sidebar (`Operations & Comms > School Calendar & Academic Cycle` gated by `Permissions.Operations.Calendar.View` or `.Manage`); `<router-view>`.
4. `src/frontend/src/app/router/` defines routes (`/auth/login`, `/login` -> redirect `/auth/login`, `/operations/calendar`, `/` -> redirect `/operations/calendar`, `/403`, catch-all `404`) and `beforeEach` guard enforcing `requiresAuth` and `meta.permission`.
5. `src/frontend/src/main.ts` and `App.vue` register Pinia, VueQueryPlugin, PrimeVue, ToastService, global `AppErrorBoundary`, and perform initial session hydration hook before mounting.
6. Vitest tests verify `useAuthStore`, `useAppContextStore` (`isHistoricalPeriod`), and router guard redirects (`/operations/calendar` unauthenticated -> `/auth/login`, missing permission -> `/403`).
7. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass in `src/frontend/`.

Notes: Placeholder view lazy imports for `LoginView.vue` and `CalendarWorkspaceView.vue` can point to minimal stub views created in this slice (if needed for `vue-tsc`) or replaced in `P3-S11`/`P3-S12`—keep stub views minimal so `P3-S11` and `P3-S12` own the real views cleanly.

Implementation Notes: Implemented `src/frontend/src/app/stores/` (`auth.store.ts`, `appContext.store.ts`, `ui.store.ts`), `src/frontend/src/app/providers/http.provider.ts`, `src/frontend/src/app/layouts/` (`AuthLayout.vue`, `AppLayout.vue`), `src/frontend/src/app/router/` (`routes.ts`, `index.ts`), `src/frontend/src/App.vue`, `src/frontend/src/main.ts`, minimal stub views (`LoginView.vue`, `CalendarWorkspaceView.vue`), and Vitest specs (`auth.store.spec.ts`, `appContext.store.spec.ts`, `router.guard.spec.ts`). Verified `pnpm typecheck`, `pnpm lint` (0 warnings), and `pnpm test:unit` (6 test files, 21 tests passed).

---

### P2-S08

Title: Implement `ElsasaSis.Api` Host, Middleware, ValidationFilter, Swagger & Startup Auto-Migration

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Wire the complete ASP.NET Core Minimal API host in `ElsasaSis.Api`, uniting `SharedKernel`, `Database`, `Modules.Auth`, and `Modules.Opr` with Serilog, `CorrelationIdMiddleware`, RFC 7807 `ValidationFilter<T>`, global exception handler, CORS, JWT Bearer + permission policies, Swagger/OpenAPI, and `Development` auto-migration.

Depends On: P1-S02, P2-S04, P2-S06

Repository: src/backend

Completion Criteria:

1. `ElsasaSis.Api` implements:
   - `CorrelationIdMiddleware`: reads or generates `X-Correlation-ID`, attaches to Serilog `LogContext`, and sets `X-Correlation-ID` response header.
   - Global exception handler mapping unhandled exceptions to RFC 7807 `500` without leaking stack traces outside `Development`.
   - `ValidationFilter<TRequest>`: executes FluentValidation and maps failures to `422 Unprocessable Entity` `ProblemDetails` with `errorCode: "Request.ValidationFailed"` and `camelCase` keys in `errors`.
   - `HttpCurrentUser`: resolves `ICurrentUser` from `HttpContext.User` claims (`sub`, `preferred_username`, `name`, `roles`, `permissions`, `teacherId`, `studentId`, `guardianId`).
   - `DomainEventDispatcher`: dispatches `DomainEventBase` instances via MediatR `IPublisher`.
   - `SqlConnectionFactory`: `ISqlConnectionFactory` creating `SqlConnection` from `ConnectionStrings:DefaultConnection`.
2. `Program.cs` configures Serilog, CORS (`WithOrigins(...)` + `AllowCredentials()`), JWT Bearer authentication (`Jwt:SigningKey`, `Jwt:Issuer`, `Jwt:Audience`), permission authorization policies for all `Permissions.*` constants, MediatR scanning `Modules.Opr` and `Modules.Auth`, Swagger/OpenAPI with JWT Bearer scheme (`/swagger/v1/swagger.json`), `IEndpoint` auto-discovery across modules, and `DatabaseMigrator` invocation on startup when `IHostEnvironment.IsDevelopment()`.
3. `Program.cs` exposes `public partial class Program;` for `WebApplicationFactory<Program>`.
4. `appsettings.json` and `appsettings.Development.json` configure local SQL Server / `MSSQLLocalDB` connection string, dev JWT settings, CORS origin `http://localhost:5173`, and `Seed:AdminPassword` dev fallback.
5. `dotnet build src/backend/ElsasaSis.sln` passes and running `ElsasaSis.Api` in `Development` auto-migrates the database, seeds `admin`, and serves `/swagger/v1/swagger.json` cleanly.

Notes: Verify `/swagger/v1/swagger.json` includes complete request/response schemas for all `/api/auth/*` and `/api/opr/academic-years*` endpoints.

Implementation Notes: Implemented `ElsasaSis.Api` host components (`Middleware/CorrelationIdMiddleware.cs`, `Middleware/GlobalExceptionHandler.cs`, `Filters/ValidationFilter.cs`, `Security/HttpCurrentUser.cs`, `Infrastructure/DomainEventDispatcher.cs`, `Infrastructure/SqlConnectionFactory.cs`, `Extensions/EndpointExtensions.cs`, `appsettings.json`, `appsettings.Development.json`, and `Program.cs` with `public partial class Program;`). Verified `dotnet build src/backend/ElsasaSis.sln --no-restore` (0 warnings, 0 errors), `dotnet test src/backend/ElsasaSis.sln --no-build` (40/40 unit tests passed), and Development startup against `(localdb)\MSSQLLocalDB` (`ElsasaSis_Dev`) auto-migrating, seeding `admin`, serving `/health` with `X-Correlation-ID`, serving `/swagger/v1/swagger.json` with all `/api/auth/*` and `/api/opr/academic-years*` paths, and returning `200 OK` on `POST /api/auth/login`.

---

## P3 - Quality Verification Suites & Frontend Feature Slices

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P3-S09

Title: Implement `ElsasaSis.ArchitectureTests` (NetArchTest Boundary & Layer Enforcement)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement automated NetArchTest rules in `ElsasaSis.ArchitectureTests` enforcing modular monolith assembly isolation, Clean Architecture layer boundaries, CQRS handler discipline, and absence of EF Core.

Depends On: P2-S08

Repository: src/backend

Completion Criteria:

1. `ElsasaSis.ArchitectureTests` implements passing xUnit + NetArchTest tests verifying:
   - `ElsasaSis.Modules.Auth` does not depend on `ElsasaSis.Modules.Opr`, `ElsasaSis.Api`, or `ElsasaSis.Database`.
   - `ElsasaSis.Modules.Opr` does not depend on `ElsasaSis.Modules.Auth`, `ElsasaSis.Api`, or `ElsasaSis.Database`.
   - `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts` do not depend on any module, `Api`, or `Database`.
   - Within `Modules.Auth` and `Modules.Opr`, types in `..Domain..` namespaces do not depend on `..Infrastructure..`, `..Features..`, `Dapper`, `System.Data`, `Microsoft.Data.SqlClient`, or `Microsoft.AspNetCore.Http`.
   - Write slice handlers (command handlers in `Features/`) do not depend on `ISqlConnectionFactory` or `Dapper`.
   - No assembly references `Microsoft.EntityFrameworkCore`.
2. `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj` passes with `0` failures.

Notes: Runs completely in-memory with zero database or Docker dependency.

Implementation Notes: Implemented NetArchTest + xUnit boundary and layer enforcement tests in `src/backend/tests/ElsasaSis.ArchitectureTests/` (`AssemblyArchitectureContext.cs`, `ModuleBoundaryTests.cs`, `LayerAndCqrsArchitectureTests.cs`) covering cross-module isolation, `SharedKernel`/`Contracts` isolation, `Domain` layer purity (`Modules.Auth` and `Modules.Opr`), Write slice handler CQRS discipline (no `ISqlConnectionFactory` or `Dapper`), and absence of `Microsoft.EntityFrameworkCore` across all application assemblies. Verified `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj` passed (Failed: 0, Passed: 7, Skipped: 0, Total: 7).

---

### P3-S10

Title: Implement `ElsasaSis.IntegrationTests` (`WebApplicationFactory` + `Respawn` on Local SQL Server)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement end-to-end HTTP + SQL integration tests in `ElsasaSis.IntegrationTests` using `WebApplicationFactory<Program>` and `Respawn` backed by local SQL Server / `MSSQLLocalDB` without Docker.

Depends On: P2-S08

Repository: src/backend

Completion Criteria:

1. `ElsasaSis.IntegrationTests` defines a collection fixture connecting to a configurable local SQL Server / `MSSQLLocalDB` test database (`IntegrationTests:ConnectionString` / env override), running `DatabaseMigrator` once per test session and using `Respawn` to reset mutable data cleanly between tests (re-seeding `admin` when testing login).
2. Auth integration tests verify:
   - `POST /api/auth/login` with seeded `admin` returns `200` with JWT (containing `sub`, `preferred_username`, `name`, `roles`, `permissions`) and `Set-Cookie` `HttpOnly` refresh token; bad credentials return `401`.
   - `POST /api/auth/refresh` with refresh cookie issues new JWT and rotates refresh token in `auth.refresh_tokens` (old token rejected).
   - `POST /api/auth/logout` revokes refresh token and clears cookie.
   - `GET /api/auth/me` returns `200` with authenticated profile, `401` when anonymous.
3. `UC-ADM-01` integration tests verify:
   - `POST /api/opr/academic-years` persists year + periods (`201`), and invalid payloads (e.g. `endDate <= startDate`, overlapping periods, out-of-bounds periods) return RFC 7807 `422` with `camelCase` field keys.
   - `GET /api/opr/academic-years` returns `PaginatedResponse<AcademicYearDto>` with nested `periods`.
   - `GET /api/opr/academic-years/{id}` returns `200` or `404`.
   - `PUT /api/opr/academic-years/{id}` updates `INACTIVE` and `ACTIVE` years, preserves `ACTIVE` statuses, and returns `409` if removing the currently `ACTIVE` period.
   - `POST /api/opr/academic-years/{id}/activate` deactivates any prior active year/period, activates the target year and its earliest period (`start_date`), and persists SQL state.
   - `POST /api/opr/academic-years/{id}/periods/{periodId}/activate` activates the target period when parent year is `ACTIVE`, and returns `409` when parent year is `INACTIVE`.
   - Unauthenticated calls return `401`.
4. `dotnet test src/backend/ElsasaSis.sln` passes `UnitTests`, `ArchitectureTests`, and `IntegrationTests` with `0` failures and zero Docker dependency.

Notes: Ensure `IntegrationTests` uses a dedicated test database name (e.g., `ElsasaSis_Test`) so it never collides with the local dev database.

Implementation Notes: Implemented `ElsasaSis.IntegrationTests` (`Fixtures/IntegrationTestWebApplicationFactory.cs`, `Fixtures/IntegrationTestCollection.cs`, `Fixtures/IntegrationTestBase.cs`, `Auth/AuthIntegrationTests.cs`, `Opr/AcademicYearsIntegrationTests.cs`) using `WebApplicationFactory<Program>` and `Respawn` against local `(localdb)\MSSQLLocalDB` (`ElsasaSis_Test`, configurable via `IntegrationTests:ConnectionString` / `INTEGRATION_TESTS_CONNECTION_STRING`) with zero Docker dependency. Verified `dotnet test src/backend/ElsasaSis.sln` passes `ElsasaSis.ArchitectureTests` (7/7), `ElsasaSis.UnitTests` (40/40), and `ElsasaSis.IntegrationTests` (12/12) with 0 failures.

---

### P3-S11

Title: Generate OpenAPI Contract Types & Implement `BND-FE-AUTH` (`SCR-AUTH-LOGIN`)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Generate TypeScript API contract definitions from `ElsasaSis.Api` OpenAPI schema and implement the authentication feature slice (`SCR-AUTH-LOGIN` / `LoginView.vue`), live `useAuthStore` API bindings, and Vitest + MSW tests.

Depends On: P2-S07, P2-S08

Repository: src/frontend

Completion Criteria:

1. `pnpm api:generate` generates `src/frontend/src/shared/types/api/schema.d.ts` from `ElsasaSis.Api` `/swagger/v1/swagger.json`, and `package.json` includes `api:generate` and `api:check` scripts.
2. `src/frontend/src/features/auth/` implements:
   - `api/auth.api.ts` (login, refresh, logout, me using types from `schema.d.ts` via `apiClient`)
   - `types/auth.types.ts` (`JwtClaims`, `UserSession` matching backend claims without `org_unit_id`)
   - `schemas/login.schema.ts` (Zod schema for `username`, `password`)
   - `views/LoginView.vue` (`SCR-AUTH-LOGIN` using VeeValidate + Zod, `FormField`, PrimeVue `InputText`/`Password`/`Button`, `401` inline error banner, redirect to `route.query.redirect || '/operations/calendar'`).
3. `useAuthStore` in `app/stores/auth.store.ts` integrates live `auth.api.ts` calls (`login`, `refreshSession`, `logout`, silent startup hydration).
4. Vitest + MSW tests (`login.schema.spec.ts`, `LoginView.spec.ts`) verify form validation, successful login flow, and `401` error rendering.
5. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass in `src/frontend/`.

Notes: Ensure `schema.d.ts` is generated cleanly before `P3-S12` begins so `P3-S12` can import typed `AcademicYearDto` / `OperationalPeriodDto` schemas directly.

Implementation Notes: Added `api:generate` and `api:check` scripts to `src/frontend/package.json` and generated `src/frontend/src/shared/types/api/schema.d.ts` from `ElsasaSis.Api` `/swagger/v1/swagger.json` (verified with `--check` and stopped temporary API process). Implemented `src/frontend/src/features/auth/` (`types/auth.types.ts`, `api/auth.api.ts`, `schemas/login.schema.ts`, `views/LoginView.vue`), added `src/frontend/src/shared/services/authSession.ts` + `http.provider.ts` wiring to preserve ESLint `features/` -> `app/` import boundary, updated `src/frontend/src/app/stores/auth.store.ts` to integrate live `authApi` calls (`login`, `refreshSession`, `hydrateSession`, `logout`), and added Vitest + MSW specs (`login.schema.spec.ts`, `LoginView.spec.ts`). Verified `pnpm typecheck`, `pnpm lint` (0 warnings), and `pnpm test:unit` (8 test files, 28 tests passed).

---

### P3-S12

Title: Implement `BND-FE-OPR` Part 1 (`SCR-OPR-CALENDAR` Academic Cycle & Top-Bar Switcher)

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Implement the `SCR-OPR-CALENDAR` Part 1 feature slice (`CalendarWorkspaceView.vue`, Academic Year/Period create/edit drawer, activation confirmation workflows, TanStack Query hooks) and wire `AppLayout.vue` Top Bar Academic Year & Semester Switcher to live `/api/opr/academic-years` data.

Depends On: P3-S11

Repository: src/frontend

Completion Criteria:

1. `src/frontend/src/features/operations/calendar/` implements:
   - `api/academicYears.api.ts`, `api/academicYears.keys.ts`, `api/useAcademicYearQueries.ts`, `api/useAcademicYearMutations.ts` (strictly typed against `schema.d.ts`, deterministic query invalidation and PrimeVue `Toast` success/error notifications).
   - `schemas/academicYear.schema.ts` (Zod schema for `name`, `startDate`, `endDate`, and `periods` array).
   - `components/AcademicYearFormDrawer.vue` (Tier 3 right-anchored PrimeVue `Drawer` for creating/editing an Academic Year and its Operational Periods; VeeValidate + Zod; maps backend `422` `camelCase` field errors via `setErrors()` and `409` domain conflicts via inline alert banner).
   - `views/CalendarWorkspaceView.vue` (`SCR-OPR-CALENDAR` Part 1 wrapped in `AppErrorBoundary`; displays paginated Academic Years + nested Operational Periods with `StatusPill`, `AppDataTable` / cards, `EmptyState`; supports Create, Edit, Activate Academic Year, and Activate Operational Period via `ConfirmActionDialog` with disabled-state tooltips when parent year is inactive or year/period is already active).
2. `AppLayout.vue` Top Bar Academic Year & Semester Switcher displays live options from `GET /api/opr/academic-years`, syncs `useAppContextStore` automatically with the school's `ACTIVE` year & period, and triggers the amber Historical Period Read-Only Warning Banner when a non-active period is selected.
3. Zero cross-feature imports exist (verified by ESLint).
4. Vitest + MSW tests (`academicYear.schema.spec.ts`, `CalendarWorkspaceView.spec.ts`) verify rendering, drawer create/edit submission, `422` field error mapping, and activation flows.
5. `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` pass in `src/frontend/`.

Notes: Stage 0 delivers `SCR-OPR-CALENDAR` Part 1 (Academic Cycle) only; do not add Stage 1 `opr.calendar_entries` tabs/forms.

Implementation Notes: Implemented `src/frontend/src/features/operations/calendar/` (`api/academicYears.keys.ts`, `api/academicYears.api.ts`, `api/useAcademicYearQueries.ts`, `api/useAcademicYearMutations.ts`, `schemas/academicYear.schema.ts`, `components/AcademicYearFormDrawer.vue`, `views/CalendarWorkspaceView.vue`) strictly typed against `schema.d.ts` with VeeValidate + Zod validation (`BR-OPS-002..006`), backend `422` camelCase field error mapping via `setErrors()`, `409` inline conflict banner, PrimeVue Toast notifications, `AppErrorBoundary`, `AppDataTable`, `StatusPill`, `EmptyState`, and `ConfirmActionDialog` activation workflows. Wired `src/frontend/src/app/layouts/AppLayout.vue` Top Bar Academic Year & Semester Switcher to live `GET /api/opr/academic-years`, auto-syncing `useAppContextStore` with the `ACTIVE` year/period and triggering the amber Historical Period Read-Only Warning Banner when a non-active period is selected (zero ESLint boundary violations). Added Vitest + MSW specs (`academicYear.schema.spec.ts`, `CalendarWorkspaceView.spec.ts`, `AppLayout.spec.ts`). Verified `pnpm typecheck`, `pnpm lint` (0 warnings), and `pnpm test:unit` (11 test files, 39 tests passed).

---

## P4 - Stage 0 End-to-End Verification

Implementation Status: COMPLETED
Review Status: GO

### P4-S13

Title: Stage 0 Frontend Quality Gates & Live Local API + Vite E2E Smoke Verification

Implementation Status: IMPLEMENTED
Review Status: GO

Objective: Verify all frontend build/quality checks (`typecheck`, `lint`, `test:unit`, `build`, `api:check`) and execute the Stage 0 end-to-end verification scenario against the running local `ElsasaSis.Api` + Vite dev proxy.

Depends On: P3-S09, P3-S10, P3-S12

Repository: src/frontend

Completion Criteria:

1. `pnpm typecheck`, `pnpm lint`, `pnpm test:unit`, and `pnpm build` execute with `0` errors in `src/frontend/`, and `pnpm api:check` confirms zero drift between `schema.d.ts` and running `ElsasaSis.Api` `/swagger/v1/swagger.json`.
2. Against running local `ElsasaSis.Api` (with SQL Server / `MSSQLLocalDB` auto-migrated) and Vite dev server (`/api` proxy), end-to-end verification confirms:
   - Signing in at `/auth/login` (and verifying `/login` alias redirect) as seeded `admin` establishes an authenticated session and redirects to `/operations/calendar`.
   - Page reload preserves the authenticated session via silent `/api/auth/refresh` `HttpOnly` cookie without storing tokens in `localStorage`/`sessionStorage`.
   - Creating an Academic Year with 2 Operational Periods, editing it, activating the Academic Year, and activating/toggling Operational Periods succeeds and updates the `AppLayout` Top Bar switcher and Historical Period Warning Banner accurately.
   - Submitting invalid date ranges triggers RFC 7807 `422` validation feedback with `camelCase` field errors in the form.
3. All Stage 0 Acceptance Conditions (`IS001- STAGE-0-DEV-ARCHITECTURE.md` §11 items 1–6) are confirmed satisfied.

Notes: Executes verification against the already-completed backend (`P3-S09`, `P3-S10`); only frontend adjustments (if any integration defect is surfaced) occur inside this slice's repository boundary.

Implementation Notes: Executed all Stage 0 frontend quality gates in `src/frontend/` (`pnpm typecheck` exited 0, `pnpm lint` exited 0 with 0 warnings, `pnpm test:unit` passed 11/11 test files and 39/39 tests, `pnpm build` succeeded). Started `ElsasaSis.Api` in `Development` against `(localdb)\MSSQLLocalDB` (`ElsasaSis_Dev`, auto-migrated and seeded `admin`) on `http://localhost:5055` (Windows TCP port 5000 is held by PID 4 / excluded port range) and verified zero OpenAPI drift (`openapi-typescript http://localhost:5055/swagger/v1/swagger.json -o src/shared/types/api/schema.d.ts --check` exited 0). Started Vite dev server (`VITE_API_PROXY_TARGET=http://localhost:5055`) on `http://localhost:5174/` and executed live headless Chrome CDP E2E smoke verification through the Vite `/api` proxy: verified `/login` -> `/auth/login` redirect, `admin` login -> `/operations/calendar`, zero tokens in `localStorage`/`sessionStorage` with `HttpOnly` refresh cookie, page reload preserving session via `/api/auth/refresh`, creating an Academic Year with 2 Operational Periods, editing it, activating the Academic Year and toggling `Semester 2` active, Top Bar switcher updates + Historical Period Warning Banner on non-active period selection, form inline validation, and live RFC 7807 `422` `camelCase` `endDate` error from `/api/opr/academic-years`. Stopped both temporary server processes cleanly.

---

# 6. Change Log

- Initial `CR-STAGE-0` Stage 0 Implementation Plan created and approved (`v1.0`, 4 phases, 13 continuous slices `P1-S01`..`P4-S13`) based on [IS001- STAGE-0-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-ARCHITECTURE.md) and [IS001- STAGE-0-DEV-FEASIBILITY-ASSESMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-FEASIBILITY-ASSESMENT.md).
