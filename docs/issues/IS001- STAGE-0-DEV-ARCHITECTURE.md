---
Title: Stage 0 Walking Skeleton & Reference Slice — Target Architecture
Code: CR-STAGE-0
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-18
---

# 1. Overview

This architecture defines the target technical realization for **CR-STAGE-0** (**Stage 0: Walking Skeleton & Reference Slice**) of **ELSASA-SIS v4 (Basic Level)**, realizing the change request captured in [IS001- STAGE-0-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-ISSUE.md).

It establishes the monorepo foundation (`src/backend/` and `src/frontend/`), SQL Server schema initialization and DbUp migration runner, custom JWT + refresh-cookie authentication (`SCR-AUTH-LOGIN`), architectural boundary enforcement (NetArchTest), the Vue 3 SPA shell, and the first end-to-end vertical slice for Academic Year & Operational Period lifecycle management (`UC-ADM-01` on `SCR-OPR-CALENDAR` Part 1).

---

# 2. Architectural Basis

## Business Context

Business concepts, actors, use cases, and UI screens realized by Stage 0 are defined in the upstream product and domain artifacts:

- **Domain Knowledge & Invariants:** [operational-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/operational-domain-basic.md) (`AcademicYear`, `OperationalPeriod`, `BR-OPS-001`..`BR-OPS-006`) and [operational-domain-priorities.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/priorities/operational-domain-priorities.md).
- **Actors & Roles:** [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md) (`SCHOOL_ADMIN`, plus the 7-role RBAC taxonomy).
- **Use Case Scope:** [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (`UC-ADM-01`) and [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md) (Section 3, Stage 0).
- **Screens & Ergonomics:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) (`SCR-AUTH-LOGIN`, `SCR-OPR-CALENDAR` Part 1) and [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md).

## Analysis Input

All gaps (`GAP-001`..`GAP-008`) and open questions (`OQ-001`..`OQ-006`) were closed in the upstream Feasibility Assessment:

- **Feasibility Assessment:** [IS001- STAGE-0-DEV-FEASIBILITY-ASSESMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-FEASIBILITY-ASSESMENT.md) (`CR-STAGE-0`).
- **System Baseline & Reference Specifications:** [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md), [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md), [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md), and [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md).

```text
DOMAIN + SPECS + BASELINE ARCHITECTURE
                  +
    IS001 FEASIBILITY-ASSESSMENT
                  ↓
      CR-STAGE-0-ARCHITECTURE
```

---

# 3. Scope

## Included

1. **Monorepo Solution & Workspace Scaffold:**
   - Backend solution at `src/backend/ElsasaSis.sln` containing `ElsasaSis.SharedKernel`, `ElsasaSis.Contracts`, `ElsasaSis.Database`, `ElsasaSis.Modules.Auth`, `ElsasaSis.Modules.Opr`, `ElsasaSis.Api`, `ElsasaSis.ArchitectureTests`, `ElsasaSis.UnitTests`, and `ElsasaSis.IntegrationTests`.
   - Frontend SPA at `src/frontend/` (`src/app/`, `src/shared/`, `src/features/auth/`, `src/features/operations/calendar/`).
2. **Database Initialization & Migrations (`ElsasaSis.Database`):**
   - DbUp migration runner executable via CLI and embedded in `ElsasaSis.Api` (`Development` auto-run).
   - Initialization of all 8 schemas (`auth`, `outbox`, `opr`, `std`, `tch`, `acd`, `fin`, `dev`) and creation of Stage 0 tables (`auth.*`, `outbox.messages`, `opr.academic_years`, `opr.operational_periods`).
   - Idempotent seeding of the initial `admin` user (`SCHOOL_ADMIN` role).
3. **Authentication & Session Pipeline (`ElsasaSis.Modules.Auth` & `BND-FE-AUTH`):**
   - Endpoints: `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me`.
   - Custom JWT Bearer issuance + `HttpOnly` refresh token cookie lifecycle with hashed persistence in `auth.refresh_tokens`.
   - `SCR-AUTH-LOGIN` (`LoginView.vue`) at `/auth/login` (with `/login` redirect alias).
4. **Reference Vertical Slice (`ElsasaSis.Modules.Opr` & `BND-FE-OPR` Part 1):**
   - `UC-ADM-01` endpoints: `GET /api/opr/academic-years` (`PaginatedResponse<AcademicYearDto>` with nested `periods`), `GET /api/opr/academic-years/{id}`, `POST /api/opr/academic-years`, `PUT /api/opr/academic-years/{id}`, `POST /api/opr/academic-years/{id}/activate`, `POST /api/opr/academic-years/{id}/periods/{periodId}/activate`.
   - Domain events `AcademicYearActivatedDomainEvent` and `OperationalPeriodActivatedDomainEvent` dispatched post-commit via MediatR.
   - `SCR-OPR-CALENDAR` Part 1 (`CalendarWorkspaceView.vue`) at `/operations/calendar` (default `/` redirect target in Stage 0).
5. **Frontend Core Shell & Primitives (`BND-FE-CORE`):**
   - `AppLayout.vue` (Top Bar with Academic Year & Semester Switcher, Historical Period Warning Banner, collapsible role-filtered sidebar), `AuthLayout.vue`, Pinia stores (`useAuthStore`, `useAppContextStore`, `useUIStore`), Axios client with single-flight refresh (`http.ts` + `http.provider.ts`), `AppErrorBoundary.vue`, `StatusPill.vue`, `ConfirmActionDialog.vue`, `EmptyState.vue`, and `AppDataTable.vue`.
6. **Automated Quality & Verification Suites:**
   - NetArchTest boundary/layer tests (`ElsasaSis.ArchitectureTests`), domain/UUID v7 unit tests (`ElsasaSis.UnitTests`), `WebApplicationFactory` + `Respawn` integration tests against local SQL Server / `MSSQLLocalDB` (`ElsasaSis.IntegrationTests`), and frontend Vitest / `vue-tsc` / ESLint checks.

## Excluded

- Module assemblies and tables for Stages 1–4 (`Std`, `Tch`, `Acd`, `Fin`, `Dev`), except creating their empty SQL schemas in `001_CreateSchemas.sql`.
- `SCR-OPR-CALENDAR` Part 2 (`opr.calendar_entries`, `UC-ADM-02`) and all other `opr` slices/screens (`opr.rooms`, `opr.study_group_schedules`, `opr.study_group_timetables`, `opr.announcements`).
- Outbox background polling `BackgroundService` (deferred to Stage 1+ when durable cross-module async subscribers are introduced).
- Multi-tenant organization unit partitioning (`org_unit_id`, `X-Organization-Unit-ID`).
- Playwright E2E test suite (deferred to Stage 1+; Stage 0 E2E is verified against the running local API + Vite dev server).

---

# 4. Technical Decisions

Technical realizations of the approved `CR-STAGE-0` decisions:

1. **Monorepo Layout & Target Framework (`GAP-002`, `OQ-001`):**
   - Place the backend solution at `src/backend/ElsasaSis.sln` (application projects under `src/backend/src/`, tests under `src/backend/tests/`) and the SPA at `src/frontend/`.
   - Target `.NET 8 LTS` (`net8.0` / C# 12) across all backend `.csproj` files.
   - Implement a deterministic RFC 9562 **UUID v7** generator (`UuidV7.NewGuid()`) in `ElsasaSis.SharedKernel` for application-generated primary keys.
2. **Modular Monolith, VSA & CQRS Realization (`GAP-001`, `OQ-005`):**
   - Stage 0 instantiates only `ElsasaSis.Modules.Auth` and `ElsasaSis.Modules.Opr` alongside `SharedKernel`, `Contracts`, `Database`, and `Api`.
   - Every vertical slice under `Features/<Submodule>/<SliceName>/` follows the self-contained slice anatomy (`Endpoint`, `Request`, `Response`, `Validator`, `Handler`) with Minimal API endpoints invoking slice handlers directly.
   - `GET /api/opr/academic-years` binds `PagedQuery` and returns `PaginatedResponse<AcademicYearDto>` with each item's nested `periods` (`OperationalPeriodDto[]`) included.
   - `IDomainEventDispatcher` dispatches domain events via in-process MediatR awaited after repository transaction commit.
3. **Custom JWT + Refresh Cookie Authentication (`GAP-005`, `GAP-008`, `OQ-004`):**
   - `ElsasaSis.Modules.Auth` issues short-lived HS256/RS256 JWT Bearer access tokens and opaque cryptographically random refresh tokens stored hashed (SHA-256) in `auth.refresh_tokens` and delivered via an `HttpOnly`, `SameSite=Strict` (configurable for cross-origin) cookie scoped to `/api/auth`.
   - JWT claims emit `sub`, `preferred_username`, `name`, `roles`, `permissions`, and optional actor IDs (`teacherId`, `studentId`, `guardianId`) from `auth.user_actors` — omitting `org_unit_id`.
   - Role-to-permission resolution (`Role -> Permissions[]`) is maintained as a compile-time catalog in `ElsasaSis.Modules.Auth` and materialized into the `permissions` JWT claim on login and refresh.
   - Seeded `admin` (`SCHOOL_ADMIN`) password is hashed using ASP.NET Core's PBKDF2 `PasswordHasher<object>` (`Microsoft.Extensions.Identity.Core`), sourced from `Seed:AdminPassword` (defaulting to `Admin123!` in `Development` only).
4. **Academic Year & Operational Period Lifecycle Realization (`GAP-003`, `OQ-003`):**
   - `opr.operational_periods` carries explicit `status VARCHAR(20) NOT NULL` (`'INACTIVE' | 'ACTIVE'`), enforcing at most one system-wide `ACTIVE` Operational Period inside the `ACTIVE` Academic Year.
   - `AcademicYear` Aggregate Root enforces `BR-OPS-001..006` and exposes:
     - Year activation (`POST /api/opr/academic-years/{id}/activate`): deactivates any previously `ACTIVE` year and its `ACTIVE` period, activates the target year, activates its chronologically earliest period (by `start_date`) if none is active, and raises `AcademicYearActivatedDomainEvent` + `OperationalPeriodActivatedDomainEvent`.
     - Period activation (`POST /api/opr/academic-years/{id}/periods/{periodId}/activate`): requires the parent year to be `ACTIVE` (`409 Conflict` otherwise), deactivates any other active period, activates the target period, and raises `OperationalPeriodActivatedDomainEvent`.
     - Update (`PUT /api/opr/academic-years/{id}`): permitted for both `INACTIVE` and `ACTIVE` years; preserves `ACTIVE` status of the year and active period; rejects removing the currently `ACTIVE` period (`409 Conflict`). No standalone year-deactivation endpoint is exposed.
5. **Pure Persistence & Phased DbUp Execution (`GAP-004`, `GAP-008`, `OQ-002`):**
   - All tables use application-supplied UTC `created_at DATETIME2(3) NOT NULL` and `updated_at DATETIME2(3) NOT NULL` with zero database `DEFAULT`s, triggers, stored procedures, or `CHECK` constraints.
   - Outbox table is standardized to `outbox.messages`.
   - `ElsasaSis.Database` executes `001_CreateSchemas.sql`, `002_CreateAuthTables.sql`, `003_CreateOutboxTables.sql`, `010_CreateOprAcademicYearTables.sql`, and the idempotent `admin` seed via CLI and automatically on `ElsasaSis.Api` startup when `IHostEnvironment.IsDevelopment()`.
6. **Frontend Stack, Shell & Route Realization (`GAP-006`, `OQ-006`):**
   - Vue 3.5+, TypeScript 5.6+, Vite 6+, Tailwind CSS v4 (`@import "tailwindcss"` + `@theme` with `@layer primevue, custom;`), PrimeVue v4 (`Aura` preset + PassThrough), TanStack Query v5, Pinia v2, VeeValidate v4 + Zod, Axios (`withCredentials: true` + Vite `/api` dev proxy), and `openapi-typescript`.
   - Access token resides strictly in-memory inside `useAuthStore`; `main.ts` / router bootstrap performs silent session hydration via `/api/auth/refresh` on initial page load.
   - Routes: `/auth/login` (`LoginView.vue`, with `/login` redirect), `/operations/calendar` (`CalendarWorkspaceView.vue`, with `/` redirect in Stage 0), and `/403` / catch-all 404 views.
   - `AppLayout.vue` renders the persistent Top Bar with the global Academic Year & Semester Switcher (`useAppContextStore` backed by `GET /api/opr/academic-years`), Historical Period Read-Only Banner (when selected period is not `ACTIVE`), and role-filtered sidebar (`Operations & Comms > School Calendar & Academic Cycle` for `SCHOOL_ADMIN`).
7. **Docker-Free Local Integration Verification (`GAP-007`):**
   - `ElsasaSis.IntegrationTests` runs `WebApplicationFactory<Program>` + `Respawn` against a configurable local SQL Server / `MSSQLLocalDB` connection string (running DbUp migrations once per collection fixture), eliminating any Docker dependency for `dotnet test`.

---

# 5. Component Responsibilities

| Component | Responsibility |
|---|---|
| `BND-SHARED-KERNEL` (`ElsasaSis.SharedKernel`) | Owns `Entity<TId>`, `AggregateRoot<TId>`, `DomainEventBase`, `Result` / `Result<T>`, `DomainError` / `ErrorType`, `ToProblemDetails()`, `UuidV7`, `ISqlConnectionFactory`, `IDomainEventDispatcher`, `IEndpoint`, `ICurrentUser`, `PagedQuery`, and `PaginatedResponse<T>`. |
| `BND-CONTRACTS` (`ElsasaSis.Contracts`) | Owns cross-module contract interfaces (`IOperationalContract` for active year/period queries) and public cross-module domain event records (`AcademicYearActivatedDomainEvent`, `OperationalPeriodActivatedDomainEvent`). |
| `BND-DATABASE` (`ElsasaSis.Database`) | Owns DbUp embedded SQL scripts (`001`..`010`), standalone CLI migration/seeding entrypoint, embeddable `DatabaseMigrator` service, and idempotent `SCHOOL_ADMIN` (`admin`) seeder using `PasswordHasher<object>`. |
| `BND-MOD-AUTH` (`ElsasaSis.Modules.Auth`) | Owns `auth` schema persistence, JWT signing/validation configuration, opaque refresh token hashing & rotation (`auth.refresh_tokens`), `Role -> Permissions[]` catalog, `ICurrentUser` claims population, and `/api/auth/*` endpoints (`login`, `refresh`, `logout`, `me`). |
| `BND-MOD-OPR` (`ElsasaSis.Modules.Opr`) | Owns `AcademicYear` aggregate root, `OperationalPeriod` child entity, `OprErrors`, `IAcademicYearRepository` (Dapper), `IOperationalContract` implementation, and `UC-ADM-01` vertical slices (`GetAcademicYears`, `GetAcademicYearById`, `CreateAcademicYear`, `UpdateAcademicYear`, `ActivateAcademicYear`, `ActivateOperationalPeriod`). |
| `BND-API-HOST` (`ElsasaSis.Api`) | Owns ASP.NET Core host configuration, Serilog logging, `CorrelationIdMiddleware`, global exception handler, CORS (`AllowCredentials`), JWT Bearer & permission policy authorization, `ValidationFilter<T>` (`camelCase` RFC 7807 errors), Swagger/OpenAPI generation, endpoint auto-discovery, and `Development` auto-migration startup hook. |
| `ElsasaSis.ArchitectureTests` | Enforces compile-time module isolation (no cross-module references), intra-module Clean Architecture direction (`Domain` free of `Infrastructure`/`Features`/`Dapper`/`System.Data`), and CQRS discipline (Write handlers free of `ISqlConnectionFactory`) via NetArchTest. |
| `ElsasaSis.UnitTests` | Verifies `AcademicYear` & `OperationalPeriod` domain invariants (`BR-OPS-001`..`006`, activation & update rules), `DomainError`, and `UuidV7` monotonicity/format. |
| `ElsasaSis.IntegrationTests` | Verifies end-to-end HTTP endpoints, RFC 7807 validation/conflict responses, JWT/cookie refresh lifecycle, and SQL persistence using `WebApplicationFactory` + `Respawn` + local SQL Server/`MSSQLLocalDB`. |
| `BND-FE-CORE` (`src/frontend/src/app/`, `src/shared/`) | Owns Vite/Tailwind v4/PrimeVue v4 bootstrap, Vue Router & permission/auth guards, `AppLayout.vue`, `AuthLayout.vue`, Pinia stores (`useAuthStore`, `useAppContextStore`, `useUIStore`), Axios client & single-flight refresh (`http.ts`, `http.provider.ts`), RFC 7807 `ApiError` mapper, OpenAPI generated types (`openapi-typescript`), and shared primitives (`AppErrorBoundary.vue`, `StatusPill.vue`, `ConfirmActionDialog.vue`, `EmptyState.vue`, `AppDataTable.vue`). |
| `BND-FE-AUTH` (`src/frontend/src/features/auth/`) | Owns `LoginView.vue`, login Zod schema + VeeValidate form, and auth TanStack/Pinia API bindings (`/api/auth/*`). |
| `BND-FE-OPR` (`src/frontend/src/features/operations/calendar/`) | Owns `CalendarWorkspaceView.vue` (`SCR-OPR-CALENDAR` Part 1), Academic Year create/edit drawer/dialog (`VeeValidate` + `Zod` + backend `422` field error mapping), activation confirmation dialogs, and TanStack Query hooks for `/api/opr/academic-years*`. |

---

# 6. Integration Design

| Source | Target | Purpose |
|---|---|---|
| `BND-FE-CORE` / `BND-FE-AUTH` | `BND-API-HOST` (`/api/auth/*` via Vite `/api` proxy) | Authenticate user, receive in-memory JWT access token + `HttpOnly` refresh cookie, perform single-flight silent refresh on `401`/startup, fetch `/api/auth/me`, and revoke refresh token on logout. |
| `BND-FE-OPR` / `useAppContextStore` | `BND-API-HOST` (`/api/opr/academic-years*`) | Query paginated Academic Years with nested Operational Periods for Top Bar switcher and `SCR-OPR-CALENDAR` Part 1; execute create, update, year activation, and period activation mutations. |
| `BND-API-HOST` | `BND-DATABASE` | Invoke `DatabaseMigrator` automatically at startup in `Development` environment to ensure schemas, Stage 0 tables, and `admin` seed exist. |
| `BND-API-HOST` | `BND-MOD-AUTH`, `BND-MOD-OPR` | Scan and register module DI services, validators, Dapper repositories, MediatR event handlers, and `IEndpoint` routes; resolve `ICurrentUser` from `HttpContext.User` claims. |
| `BND-MOD-OPR` (Handlers) | `BND-SHARED-KERNEL` (`IDomainEventDispatcher`) | Publish `AcademicYearActivatedDomainEvent` and `OperationalPeriodActivatedDomainEvent` via MediatR after the repository transaction commits. |
| `BND-MOD-OPR` (`OperationalContract`) | `BND-CONTRACTS` (`IOperationalContract`) | Expose read-only active Academic Year and active Operational Period resolution for Stage 1+ module prerequisite checks. |

---

# 7. Data Ownership

| Data | Owner |
|---|---|
| `auth.users`, `auth.user_roles`, `auth.refresh_tokens`, `auth.user_actors` | `ElsasaSis.Modules.Auth` (exclusive write and schema ownership) |
| `Role -> Permissions[]` authorization catalog | `ElsasaSis.Modules.Auth` (compile-time code catalog) |
| `opr.academic_years`, `opr.operational_periods` | `ElsasaSis.Modules.Opr` (exclusive write and schema ownership via `IAcademicYearRepository`) |
| `outbox.messages` | `ElsasaSis.Api` / shared infrastructure (Schema initialized in Stage 0; durable polling activated in Stage 1+) |
| `std.*`, `tch.*`, `acd.*`, `fin.*`, `dev.*` schemas | Reserved empty schemas created in Stage 0 (`001_CreateSchemas.sql`); tables deferred to respective Stage 1–3 module owners |
| Client Auth Session (Access Token & User Profile) | Frontend `useAuthStore` (in-memory only; never `localStorage`/`sessionStorage`) |
| Client Active Temporal Context (Selected Year & Period) | Frontend `useAppContextStore` (synced with server `ACTIVE` state from `/api/opr/academic-years`) |

---

# 8. Database Design

Detailed column definitions, SQL Server data types, and indexes are maintained in [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md) (§3.1–3.2) and [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md) (§12.2).

## New Tables

| Table | Purpose |
|---|---|
| `auth.users` | Stores local user credentials (`username`, PBKDF2 `password_hash`, `display_name`, `email`, `is_active`). |
| `auth.user_roles` | Stores multi-role RBAC assignments per user (`SCHOOL_ADMIN`, etc.). |
| `auth.refresh_tokens` | Stores SHA-256 hashed refresh tokens, expiration, and revocation timestamps for secure session persistence. |
| `auth.user_actors` | Maps a user account to optional domain actor identifiers (`teacher_id`, `student_id`, `guardian_id`). |
| `outbox.messages` | Stores transactional outbox messages for resilient async cross-module delivery (table provisioned in Stage 0; worker deferred to Stage 1+). |
| `opr.academic_years` | Stores Academic Year aggregate root boundaries (`name`, `start_date`, `end_date`, `status`). |

## Modified Tables

| Table | Change |
|---|---|
| `opr.operational_periods` | Added `status VARCHAR(20) NOT NULL` (`'INACTIVE' \| 'ACTIVE'`) and nonclustered index `IX_operational_periods_status` to support explicit semester activation (`GAP-003`) in [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md). |

## Relationships

- **Intra-Schema `auth` Foreign Keys:** `auth.user_roles.user_id`, `auth.refresh_tokens.user_id`, and `auth.user_actors.user_id` reference `auth.users(id)`.
- **Pure Persistence `opr` Logical Ownership:** `opr.operational_periods.academic_year_id` logically references `opr.academic_years(id)` without a database `FOREIGN KEY` constraint; integrity and cascade persistence are enforced inside `AcademicYear` and `AcademicYearRepository` within a single SQL transaction.
- **Uniqueness Guarantees:** Database `UNIQUE` constraints enforce `auth.users(username)`, `opr.academic_years(name)`, and `opr.operational_periods(academic_year_id, name)`. Single-active-year and single-active-period invariants are enforced in the Application/Domain layer.

## Migration Considerations

- **Phased Script Execution (`ElsasaSis.Database`):**
  1. `001_CreateSchemas.sql`: Idempotently creates all 8 schemas (`auth`, `outbox`, `opr`, `std`, `tch`, `acd`, `fin`, `dev`).
  2. `002_CreateAuthTables.sql`: Creates `auth.users`, `auth.user_roles`, `auth.refresh_tokens`, `auth.user_actors`.
  3. `003_CreateOutboxTables.sql`: Creates `outbox.messages`.
  4. `010_CreateOprAcademicYearTables.sql`: Creates `opr.academic_years` and `opr.operational_periods` (including `status`).
  5. Idempotent post-migration seed ensures the `admin` user (`SCHOOL_ADMIN`) exists without overwriting existing credentials on subsequent runs.
- **Pure Persistence Discipline:** Zero `DEFAULT` constraints, `CHECK` constraints, triggers, or stored procedures. All `id` (UUID v7), `created_at` (UTC), and `updated_at` (UTC) values are supplied by C# code.
- **Environment Rollout:** `ElsasaSis.Api` auto-runs `ElsasaSis.Database` migrations on startup in `Development`; non-development environments execute the standalone `ElsasaSis.Database` CLI runner prior to API rollout.

---

# 9. Cross-Cutting Concerns

- **Security & Authorization:**
  - `ElsasaSis.Modules.Auth` issues JWTs signed with symmetric/asymmetric key from configuration (`Jwt:SigningKey`, `Jwt:Issuer`, `Jwt:Audience`).
  - Refresh tokens use `HttpOnly`, `SameSite=Strict` cookies (`Secure` outside local HTTP dev) with rotation and server-side revocation on `/api/auth/logout` and `/api/auth/refresh`.
  - Endpoints enforce `.RequireAuthorization(...)` with granular `Permissions.*` policies; frontend router guards enforce `meta.requiresAuth` and `meta.permission`, while backend policies remain authoritative.
- **Audit & Observability:**
  - Every table persists application-supplied UTC `created_at` and `updated_at` (`DATETIME2(3)`).
  - `CorrelationIdMiddleware` reads or generates `X-Correlation-ID`, enriches Serilog log scopes, and echoes the header on every HTTP response.
- **Error Handling (RFC 7807):**
  - `ValidationFilter<TRequest>` normalizes FluentValidation property keys to `camelCase` (`422 Unprocessable Entity`, `errorCode: "Request.ValidationFailed"`).
  - Domain errors map via `DomainErrorExtensions.ToProblemDetails()` (`404`, `409`, `422`, `401`, `403`) with `errorCode` and optional `camelCase` `errors` dictionary.
  - Unhandled exceptions map to RFC 7807 `500` without leaking internal stack details outside `Development`.
- **Concurrency & Transaction Integrity:**
  - `AcademicYearRepository` executes multi-row mutations (such as deactivating the previously `ACTIVE` year/period and activating the target year/period, or replacing child `opr.operational_periods` rows on `PUT`) inside an explicit `SqlTransaction` (`BeginTransactionAsync` / `CommitAsync`).
  - Frontend Axios interceptor implements single-flight refresh token locking so concurrent `401` responses share one `/api/auth/refresh` call.

---

# 10. Implementation Constraints

1. **No Entity Framework Core:** All SQL queries and commands must use Dapper (`IDbConnection` via `ISqlConnectionFactory`).
2. **Strict Vertical Slice & Clean Architecture Isolation:** `Domain/` must never reference `Infrastructure/`, `Features/`, Dapper, SQL, or HTTP types; Write slice handlers must never contain SQL or business-rule `if` decisions; Read slice handlers project directly via Dapper without Aggregate Roots.
3. **No MediatR for Command/Query Dispatch:** Minimal API endpoints must inject and invoke slice handlers directly; MediatR is restricted to post-commit domain event dispatch.
4. **No Cross-Module Assembly References:** `ElsasaSis.Modules.Auth` and `ElsasaSis.Modules.Opr` may reference only `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`.
5. **Pure Persistence SQL Rules:** Zero triggers, stored procedures, `CHECK` constraints, `DEFAULT` constraints, or cross-schema foreign keys. Primary keys must use `UuidV7.NewGuid()`; `created_at` and `updated_at` must be populated in UTC by application code.
6. **OpenAPI Metadata Completeness:** Every Minimal API endpoint must declare `.Produces<T>()` / `.ProducesProblem(...)` (or `TypedResults`) so `/swagger/v1/swagger.json` accurately drives frontend `openapi-typescript` generation.
7. **Frontend Layer & Security Rules:** Zero cross-feature imports; zero `localStorage`/`sessionStorage` token storage; zero `v-html` in business views; all API calls go through TanStack Query hooks (`api/`) wrapping `http.ts`; all forms use VeeValidate + Zod mapping backend `422` field errors.
8. **Single Repository Boundary per Slice:** Each slice in the Stage 0 `IMPLEMENTATION-PLAN` must target strictly Backend or strictly Frontend, never both.
9. **Local Test Execution Without Docker:** `dotnet test` must pass `ElsasaSis.ArchitectureTests`, `ElsasaSis.UnitTests`, and `ElsasaSis.IntegrationTests` using local SQL Server / `MSSQLLocalDB` without requiring Docker.

---

# 11. Acceptance Conditions

Stage 0 implementation is architecturally complete when all of the following conditions hold:

1. `src/backend/ElsasaSis.sln` builds cleanly under `.NET 8` (`net8.0`) and `dotnet test` passes `ElsasaSis.ArchitectureTests` (NetArchTest boundary & layer isolation), `ElsasaSis.UnitTests`, and `ElsasaSis.IntegrationTests` (`WebApplicationFactory` + `Respawn` on local SQL Server / `MSSQLLocalDB`).
2. `ElsasaSis.Database` initializes all 8 schemas, creates all Stage 0 tables (`auth.*`, `outbox.messages`, `opr.academic_years`, `opr.operational_periods` with `status`), and idempotently seeds the `admin` (`SCHOOL_ADMIN`) account.
3. Custom JWT + `HttpOnly` refresh cookie authentication (`POST /api/auth/login`, `/refresh`, `/logout`, `GET /api/auth/me`) works end-to-end with `sub`, `preferred_username`, `name`, `roles`, and `permissions` claims.
4. All `UC-ADM-01` endpoints (`GET` list/detail, `POST`, `PUT`, `POST .../activate`, `POST .../periods/{periodId}/activate`) enforce `BR-OPS-001..006`, single-active-year/single-active-period invariants, and RFC 7807 error payloads (`422` with `camelCase` field keys, `409`, `404`).
5. `src/frontend/` passes `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit`, and compiles against OpenAPI-generated types (`openapi-typescript`).
6. End-to-end verification against the running local API + Vite dev proxy confirms: signing in via `/auth/login` (`/login`), navigating to `/operations/calendar`, creating an Academic Year with Operational Periods, activating the Academic Year and toggling active Operational Periods, observing the Top Bar switcher update, and receiving `422` field validation feedback on invalid inputs.
