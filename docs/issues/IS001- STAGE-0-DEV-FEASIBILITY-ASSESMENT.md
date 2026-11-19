---
Title: Stage 0 Walking Skeleton & Reference Slice — Feasibility Assessment
Code: CR-STAGE-0
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-18
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feasibility assessment for **CR-STAGE-0** ([IS001- STAGE-0-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-ISSUE.md)): implementing **Stage 0 ("Walking Skeleton & Reference Slice")** of the ELSASA-SIS v4 Basic Level roadmap.

Stage 0 establishes the foundational backend and frontend monorepo scaffold, database schema/migration runner (DbUp), custom JWT authentication (`SCR-AUTH-LOGIN`), architectural boundary enforcement (NetArchTest), and the first end-to-end vertical slice for Academic Year & Operational Period management (`UC-ADM-01` across `SCR-OPR-CALENDAR` Part 1).

Referenced upstream artifacts:

- ISSUE: [IS001- STAGE-0-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-ISSUE.md) (`CR-STAGE-0`)
- DOMAIN: [operational-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/operational-domain-basic.md), [operational-domain-priorities.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/priorities/operational-domain-priorities.md)
- ARCHITECTURE: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md), [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md), [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md), [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md), [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)
- PRODUCT SPECS: [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md), [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md), [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md), [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md)

## Objective

Identify all implementation gaps, cross-document contradictions, and unresolved business/architectural questions for `CR-STAGE-0`, and close every decision so downstream `ica-architect` updates, `IMPLEMENTATION-PLAN` creation, and autonomous `ica-developer` slice execution require zero guesswork.

---

# 2. Current State

## Existing Behavior

- **Codebase State:** True greenfield. Only `docs/`, `src/backend/` (empty), and `src/frontend/` (empty) exist in the workspace. No `.sln`, `.csproj`, `package.json`, SQL migration scripts, vertical slices, or tests exist yet.
- **Local Toolchain Observed:**
  - .NET SDKs installed: `5.0.416`, `6.0.428`, `8.0.204`, `8.0.428`, `9.0.313`, `9.0.321`, `10.0.100-rc.1.25451.107`, `10.0.401`.
  - Node.js `v22.22.3` and `pnpm` `11.9.0` installed.
  - Microsoft SQL Server (`MSSQLSERVER` service running), `MSSQLLocalDB` (`SqlLocalDB.exe`), and `SQLCMD.EXE` installed locally.
  - `docker` CLI is **not** installed on the host.
- **Upstream Documentation State:** Approved specifications exist for Basic domains, Pure Persistence table designs, 5 internal + 2 consumer roles, 68 use cases, 19 operational screens (+ login), UI/UX guidelines, backend/frontend reference architectures, target greenfield architecture (`ELSASA-SIS-ARCHITECTURE.md`), and the 5-stage roadmap (`elsasa-sis-v4-development-roadmap.md`).

## Existing Constraints

1. **Modular Monolith & VSA + DDD Isolation:** Module assemblies must never reference one another directly (enforced by NetArchTest). No EF Core; Dapper only. Minimal API endpoints invoke slice handlers directly; MediatR is restricted to post-commit domain events.
2. **Pure Persistence Layer:** `operational-tables-design.md` prohibits database triggers, stored procedures, `CHECK` constraints, `DEFAULT` constraints, and foreign key constraints; PKs are application-generated UUID v7 values.
3. **Single-School Basic Scope:** ELSASA-SIS Basic targets a single K-12 school (no multi-tenant organization units).
4. **RFC 7807 Error Contract:** All validation and domain errors must map to `ProblemDetails` with `camelCase` field error keys (`422`), `409 Conflict`, `404 NotFound`, `401 Unauthorized`, `403 Forbidden`.
5. **Single Repository Boundary per Slice:** Each implementation slice targets strictly Backend or Frontend, never both.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| `GAP-001` | CRITICAL | **Empty Greenfield Codebase:** `src/backend` and `src/frontend` contain no solution, projects, DbUp runner, auth pipeline, `UC-ADM-01` vertical slices, or Vue 3 SPA shell/screens (`SCR-AUTH-LOGIN`, `SCR-OPR-CALENDAR` Pt.1). |
| `GAP-002` | CRITICAL | **Monorepo Folder Path Conflict:** `ELSASA-SIS-ARCHITECTURE.md` §5.1 and `elsasa-sis-v4-development-roadmap.md` §3 place projects under root `backend/` and `frontend/`, whereas `backend-architecture.md` §9.1, `frontend-architecture.md` §4, `IS001`, and existing directories use `src/backend/` and `src/frontend/`. |
| `GAP-003` | CRITICAL | **Missing `status` Column on `opr.operational_periods`:** `elsasa-sis-v4-development-roadmap.md` §3 requires `SCR-OPR-CALENDAR` Pt.1 to "set active operational semester", `ui-ux-guideline.md` §3.2 displays `Semester 1 [ACTIVE]`, and `ELSASA-SIS-ARCHITECTURE.md` §10.1–10.2 checks active `operational_period_id` / `PeriodActivatedEvent`, but `operational-tables-design.md` §3.2 defines no `status` column on `opr.operational_periods`. |
| `GAP-004` | MAJOR | **Audit Column & DB Default Conflict:** `operational-tables-design.md` §1.3/§3 defines `created_at` and `updated_at` (`DATETIME2(3) NOT NULL`, app-supplied, no DB `DEFAULT`s, no `created_by`/`updated_by`), whereas `ELSASA-SIS-ARCHITECTURE.md` §13.3 specifies `DEFAULT SYSUTCDATETIME()`, `created_by`, `updated_by`, and nullable `updated_at`. |
| `GAP-005` | MAJOR | **Unresolved Auth Contract & Claims Mismatch:** `frontend-architecture.md` §18.1 marks the identity provider contract as pending; `frontend-architecture.md` §13.2 specifies `HttpOnly` refresh cookie + in-memory access token while `roadmap.md` §3 mentions "session storage"; `backend-architecture.md` §9.1 & `frontend-architecture.md` §13.1 expect `preferred_username`, `permissions`, `org_unit_id` while `ELSASA-SIS-ARCHITECTURE.md` §13.1 specifies `sub`, `name`, `roles`, `teacherId`, `studentId`, `guardianId`; and `ELSASA-SIS-ARCHITECTURE.md` §11.1 lists `auth.role_permissions` while §12.2 omits it and lists `auth.user_actors`. |
| `GAP-006` | MAJOR | **Frontend Stack, Directory & Route Mismatches:** `ELSASA-SIS-ARCHITECTURE.md` §14 lists Tailwind CSS `3.4+` and §5.1/§9 uses `features/operational/pages/CalendarPage.vue` at `/login` and `/operational/calendar`, whereas `frontend-architecture.md` §2/§4/§12.1 and `ui-ux-guideline.md` §3.1 mandate Tailwind CSS `v4` + PrimeVue `v4` (`Aura`), `features/operations/calendar/views/...View.vue`, and `/auth/login`, `/operations/calendar`. |
| `GAP-007` | MAJOR | **Testcontainers vs Host Environment:** `backend-architecture.md` §11.1 and `ELSASA-SIS-ARCHITECTURE.md` §5.1 reference Docker Testcontainers for `IntegrationTests`, but Docker is not installed on this Windows host (while local SQL Server `MSSQLSERVER` and `MSSQLLocalDB` are running). |
| `GAP-008` | MINOR | **Template Leftovers & Naming Drift:** `ELSASA-SIS-ARCHITECTURE.md` §11.1 names `outbox.outbox_messages` while §12.2 names `outbox.messages`; `backend-architecture.md` §9.1 & `frontend-architecture.md` §13.1 include `org_unit_id` / `X-Organization-Unit-ID` despite ELSASA-SIS Basic being single-school; and `frontend-architecture.md` §12.1 redirects `/` to `/dashboard`, which does not exist in Stage 0. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| `OQ-001` | Which .NET target framework should Stage 0 use (`net8.0` per docs with a custom UUID v7 generator vs `net10.0` installed locally for native `Guid.CreateVersion7()`)? | Affects all `.csproj` files, CI/SDK alignment, and `SharedKernel` ID generation. |
| `OQ-002` | Which schemas and tables should Stage 0 DbUp scripts create (`001`–`060` all tables vs Stage 0 tables only), and how should DbUp execute (CLI only vs auto-run on API startup)? | Determines DbUp script inventory (`ElsasaSis.Database`) and local dev startup flow. |
| `OQ-003` | For `UC-ADM-01`, what are the exact lifecycle rules for activating/deactivating an Academic Year and Operational Periods, and can `ACTIVE` years/periods still be edited via `PUT`? | Governs `AcademicYear` Aggregate Root invariants, API endpoints, and UI button/state rules. |
| `OQ-004` | How should the seeded `admin` (`SCHOOL_ADMIN`) account, password hashing, and role-to-permission mappings be implemented in `Modules.Auth`? | Blocks deterministic implementation of `ElsasaSis.Database` seeding and `Modules.Auth` login/authorization. |
| `OQ-005` | Should Stage 0 scaffold empty `.csproj` stubs for all 7 modules or only `Auth` & `Opr`, how is `GET /api/opr/academic-years` shaped (`PaginatedResponse<T>` vs plain array), and is the Outbox background worker needed in Stage 0? | Governs `ElsasaSis.sln` scope, OpenAPI contract for `SCR-OPR-CALENDAR`, and `ElsasaSis.Api` hosted services. |
| `OQ-006` | Does Stage 0 `AppLayout` include the top-bar Active Year/Semester switcher and role-filtered sidebar, and where does `/` redirect before Stage 1 Dashboard exists? | Defines `BND-FE-CORE` shell deliverables and `router` defaults for Stage 0. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| `ASM-001` | Local development and Stage 0 integration verification run on Windows using the locally running SQL Server (`MSSQLSERVER` / `MSSQLLocalDB`), .NET 8 SDK (`8.0.428`), Node.js `v22`, and `pnpm` `11.9`. |
| `ASM-002` | Only one school instance is served per deployment in Basic v1.0 (no multi-campus or multi-tenant `OrganizationUnit` partitioning). |
| `ASM-003` | Stage 0 delivers `SCR-OPR-CALENDAR` **Part 1 (Academic Cycle tab)** only; `opr.calendar_entries` (holidays/closures) and `UC-ADM-02` remain in Stage 1. |
| `ASM-004` | Frontend development uses Vite's `/api` dev proxy to `ElsasaSis.Api` so `HttpOnly` refresh cookies operate same-origin without cross-origin cookie friction during local dev. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| `RISK-001` | `operational-tables-design.md` lacks `status` on `opr.operational_periods`, which would force implementers to guess how "active semester" works or break Stage 0 exit criteria. | Schema rework in Stage 1+ across `std`, `tch`, `acd`, `fin`. | Closed via `GAP-003`: add `status VARCHAR(20) NOT NULL` (`INACTIVE \| ACTIVE`) to `opr.operational_periods` and update table design / architecture before planning. |
| `RISK-002` | Audit column drift between `operational-tables-design.md` (`created_at`, `updated_at` only) and `ELSASA-SIS-ARCHITECTURE.md` §13.3 (`created_by`, `updated_by`, `DEFAULT SYSUTCDATETIME()`) could cause Dapper SQL mismatches across stages. | Broken repositories and inconsistent migrations. | Closed via `GAP-004`: standardize on Pure Persistence columns (`created_at`, `updated_at` app-supplied, no DB defaults). |
| `RISK-003` | Docker unavailability on the host would cause Testcontainers-based integration tests to fail during `dotnet test`. | Blocked Stage 0 verification gate. | Closed via `GAP-007`: back `ElsasaSis.IntegrationTests` with local SQL Server / LocalDB + Respawn (no Docker required). |
| `RISK-004` | Frontend route/folder drift between `ELSASA-SIS-ARCHITECTURE.md` and `frontend-architecture.md` / `ui-ux-guideline.md` could break cross-stage links. | Rework of router guards and menu links in Stages 1–4. | Closed via `GAP-006`: standardize on `frontend-architecture.md` + `ui-ux-guideline.md` conventions (`views/`, `/auth/login` + `/login` alias, `/operations/calendar`). |

---

# 7. Recommendations

All major gaps and open questions were evaluated through structured stakeholder decisioning (`/grill-me` interview on 2026-11-18). The following options were analyzed and resolved in Section 8:

## Option A (Selected Direction): Align Architecture Docs to Detailed Reference Specs + Explicit Period Status

Resolve all cross-document conflicts in favor of the specialized, detailed specifications (`backend-architecture.md`, `frontend-architecture.md`, `ui-ux-guideline.md`, `operational-tables-design.md`) while keeping `net8.0` and adding the single missing `status` column on `opr.operational_periods`.

### Advantages

- Zero rework in existing `src/backend` and `src/frontend` folder roots.
- Preserves 100% Pure Persistence alignment across all 6 `*-tables-design.md` specs.
- Fully closes the pending auth contract (`HttpOnly` cookie + in-memory JWT + `permissions` + `actorId` claims).
- Runs `dotnet test` (NetArchTest + unit + LocalDB/SQL Server integration tests) immediately without Docker.

### Disadvantages

- Requires minor reconciliation updates to `ELSASA-SIS-ARCHITECTURE.md` and `operational-tables-design.md` (specifically `opr.operational_periods.status`, `src/` paths, audit columns, and frontend route/stack alignment) by `ica-architect` prior to planning.

## Option B (Rejected): Follow `ELSASA-SIS-ARCHITECTURE.md` Literally & Defer Period Status

Move folders to root `backend/`/`frontend/`, add `created_by`/`updated_by` + DB defaults contradicting all 6 table designs, and derive active semester from dates.

### Advantages

- Requires fewer edits to `ELSASA-SIS-ARCHITECTURE.md`.

### Disadvantages

- Contradicts all 6 `*-tables-design.md` files, `backend-architecture.md`, `frontend-architecture.md`, and `ui-ux-guideline.md`.
- Cannot satisfy `roadmap.md` Stage 0 "set active operational semester" explicitly.

---

# 8. Gap Closure

## GAP-001

Status: CLOSED

### Decision

Implement the complete Stage 0 Walking Skeleton & Reference Slice (`BND-SHARED-KERNEL`, `BND-CONTRACTS`, `BND-DATABASE`, `BND-MOD-AUTH`, `BND-MOD-OPR`, `BND-API-HOST`, `ElsasaSis.ArchitectureTests`, `ElsasaSis.IntegrationTests`, `BND-FE-CORE`, `BND-FE-AUTH`, and `BND-FE-OPR` Part 1) from scratch in `src/backend/` and `src/frontend/`.

### Rationale

The repository is currently greenfield (`0` application code); Stage 0 establishes the reference vertical slice and automated guardrails required before parallelizing Stages 1–4.

### Impact

All Stage 0 backend and frontend boundaries will be created cleanly under an approved `IMPLEMENTATION-PLAN`.

### Architecture Impact

`ELSASA-SIS-ARCHITECTURE.md` Stage 0 scope confirmed; no structural boundary redesign required beyond the specific gap closures below.

### Resolved By

User (via `/grill-me` feasibility review)

### Resolved Date

2026-11-18

---

## GAP-002

Status: CLOSED

### Decision

Use the existing `src/backend/` (with `src/backend/tests/`) and `src/frontend/` directory layout, aligning with `backend-architecture.md` §9.1, `frontend-architecture.md` §4, and `IS001`.

### Rationale

`src/backend` and `src/frontend` already exist in the repository and match both detailed technical reference architectures (`backend-architecture.md` and `frontend-architecture.md`), isolating `node_modules` cleanly from .NET MSBuild globbing.

### Impact

`ElsasaSis.sln` lives at `src/backend/ElsasaSis.sln`, application projects at `src/backend/src/...`, backend tests at `src/backend/tests/...`, and Vue 3 SPA at `src/frontend/`.

### Architecture Impact

Update `ELSASA-SIS-ARCHITECTURE.md` §5.1 / §16 and `elsasa-sis-v4-development-roadmap.md` §3 path references to `src/backend/...` and `src/frontend/...`.

### Resolved By

User

### Resolved Date

2026-11-18

---

## GAP-003

Status: CLOSED

### Decision

Add explicit `status VARCHAR(20) NOT NULL` (`'INACTIVE' | 'ACTIVE'`) to `opr.operational_periods`, with an explicit period-activation action enforcing at most one `ACTIVE` Operational Period system-wide (inside the `ACTIVE` Academic Year).

### Rationale

Directly satisfies `roadmap.md` Stage 0 ("set active operational semester"), `ui-ux-guideline.md` §3.2 (`Semester 1 [ACTIVE]`), and `ELSASA-SIS-ARCHITECTURE.md` §10 (`IOperationalContract`, `PeriodActivatedEvent`), while supporting real-world Indonesian school transitions (where Semester 2 is activated manually after Semester 1 report cards finalize).

### Impact

`opr.operational_periods` DDL, `OperationalPeriod` domain entity, `AcademicYear` aggregate root methods, `ActivateOperationalPeriod` endpoint, and `SCR-OPR-CALENDAR` Part 1 UI all model explicit period `status`.

### Architecture Impact

Update `operational-tables-design.md` §3.2 (and §2 ERD / §1.3) to add `status VARCHAR(20) NOT NULL` (`'INACTIVE' | 'ACTIVE'`) and `IX_operational_periods_status` if needed, and update `ELSASA-SIS-ARCHITECTURE.md` §8.1 (`UC-ADM-01` endpoints).

### Resolved By

User

### Resolved Date

2026-11-18

---

## GAP-004

Status: CLOSED

### Decision

Follow `operational-tables-design.md` (Pure Persistence Layer): every table uses `created_at DATETIME2(3) NOT NULL` and `updated_at DATETIME2(3) NOT NULL` populated explicitly in UTC by the Application Layer, with zero database `DEFAULT` constraints and no `created_by` / `updated_by` columns.

### Rationale

Maintains 100% consistency across all six approved `*-tables-design.md` documents (`operational`, `student`, `teaching`, `academic`, `finance`, `development`) and eliminates Pure Persistence violations.

### Impact

All DbUp migration scripts (`auth.*`, `outbox.messages`, `opr.*`) and Dapper repositories populate `created_at` and `updated_at` in C# using `DateTime.UtcNow` (or `TimeProvider.GetUtcNow().UtcDateTime`).

### Architecture Impact

Update `ELSASA-SIS-ARCHITECTURE.md` §13.3 (aligning audit column standard with Pure Persistence `*-tables-design.md`, retaining `X-Correlation-Id` and Serilog observability).

### Resolved By

User

### Resolved Date

2026-11-18

---

## GAP-005

Status: CLOSED

### Decision

Implement custom JWT + Refresh Token authentication in `ElsasaSis.Modules.Auth` exposing `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, and `GET /api/auth/me`:
1. Access token stored strictly in-memory inside Pinia `useAuthStore`; refresh token stored in an `HttpOnly`, `SameSite=Strict` (dev/same-origin via Vite `/api` proxy; configurable for split origin) cookie managed by `Modules.Auth` and persisted hashed in `auth.refresh_tokens`.
2. JWT claims emit `sub`, `preferred_username`, `name`, `roles`, `permissions`, and optional actor IDs (`teacherId`, `studentId`, `guardianId`) from `auth.user_actors` (omitting `org_unit_id`).

### Rationale

Resolves `frontend-architecture.md` §18.1 (Pending Auth Provider), prevents XSS access-token exfiltration while preserving page-reload sessions via silent `/api/auth/refresh`, and unifies `frontend-architecture.md` §13.1 (`permissions`) with `ELSASA-SIS-ARCHITECTURE.md` §13.1 (`roles` + `actorId` claims).

### Impact

`Modules.Auth` owns `auth.users`, `auth.user_roles`, `auth.refresh_tokens`, `auth.user_actors`; `ICurrentUser` in `SharedKernel` exposes `UserId`, `Username`, `FullName`, `Roles`, `Permissions`, `TeacherId`, `StudentId`, `GuardianId`; frontend `http.ts` uses `withCredentials: true` and single-flight refresh.

### Architecture Impact

Close pending item 1 in `frontend-architecture.md` §18.1; update `ELSASA-SIS-ARCHITECTURE.md` §13.1 JWT claim list to include `preferred_username` and `permissions`.

### Resolved By

User

### Resolved Date

2026-11-18

---

## GAP-006

Status: CLOSED

### Decision

Standardize the frontend on `frontend-architecture.md` and `ui-ux-guideline.md`:
1. Stack: Vue 3.5+, TypeScript 5.6+, Vite 6+, **Tailwind CSS v4** (`@import "tailwindcss"` + `@theme`), **PrimeVue v4** (`Aura` preset + PassThrough), TanStack Query v5, Pinia v2, VeeValidate v4 + Zod, Axios, `openapi-typescript`.
2. Feature folder & component naming: `views/` under `src/frontend/src/features/auth/` (`LoginView.vue`) and `src/frontend/src/features/operations/calendar/` (`CalendarWorkspaceView.vue`).
3. Routes: `/auth/login` (with `/login` redirecting to `/auth/login` so roadmap links work seamlessly) and `/operations/calendar` (with `/` redirecting to `/operations/calendar` for Stage 0).

### Rationale

Eliminates drift between the generic architectural overview and the authoritative `frontend-architecture.md` + `ui-ux-guideline.md` standards while honoring the Stage 0 verification route expectations.

### Impact

Frontend package manifests, Vite `/api` proxy, Tailwind v4 CSS layers (`@layer primevue, custom;`), and Vue Router routes follow one unambiguous convention.

### Architecture Impact

Update `ELSASA-SIS-ARCHITECTURE.md` §5.1, §9, §14, §16 to align with Tailwind CSS v4 + PrimeVue v4, `features/operations/calendar/views/`, and `/auth/login`, `/operations/calendar`.

### Resolved By

User

### Resolved Date

2026-11-18

---

## GAP-007

Status: CLOSED

### Decision

In Stage 0, `dotnet test` executes:
1. `ElsasaSis.ArchitectureTests` (NetArchTest boundary & layer enforcement),
2. Domain & Application unit tests (testing `AcademicYear` / `OperationalPeriod` invariants and UUID v7 generation), and
3. `ElsasaSis.IntegrationTests` (`WebApplicationFactory` + Respawn) targeting local SQL Server / `MSSQLLocalDB` via configurable test connection string — **without** requiring Docker Testcontainers.

On the frontend, automated checks cover `pnpm test:unit` (Vitest), `pnpm typecheck` (`vue-tsc`), and `pnpm lint` (ESLint flat config), with Playwright E2E deferred to Stage 1+ (Stage 0 end-to-end gate verified via running local API + Vite dev server).

### Rationale

Docker is not installed in the target environment whereas `MSSQLSERVER` and `MSSQLLocalDB` are running locally. Requiring Docker Testcontainers would block `dotnet test` execution.

### Impact

`ElsasaSis.IntegrationTests` uses `Respawn` against a dedicated test database on LocalDB/local SQL Server, running DbUp migrations once per test fixture.

### Architecture Impact

Update `backend-architecture.md` §11.1 / `ELSASA-SIS-ARCHITECTURE.md` §5.1 note for local execution: allow local SQL Server / LocalDB connection string in `IntegrationTests` instead of mandatory Docker Testcontainers.

### Resolved By

User

### Resolved Date

2026-11-18

---

## GAP-008

Status: CLOSED

### Decision

Standardize the outbox table name to `outbox.messages` (matching `ELSASA-SIS-ARCHITECTURE.md` §12.2) and strip multi-tenant `org_unit_id` / `X-Organization-Unit-ID` template artifacts from `ICurrentUser`, `IRequestContext`, JWT claims, and `useAppContextStore`.

### Rationale

Prevents confusion between `outbox.messages` and `outbox.outbox_messages`, and avoids dead multi-tenant headers/claims in a single-school Basic system.

### Impact

Cleaner `SharedKernel`, `Database`, `Modules.Auth`, and `useAppContextStore` implementations.

### Architecture Impact

Harmonize `ELSASA-SIS-ARCHITECTURE.md` §11.1 (`outbox.messages`) and note single-school omission of `org_unit_id` / `X-Organization-Unit-ID` relative to generic `backend-architecture.md` / `frontend-architecture.md` templates.

### Resolved By

User

### Resolved Date

2026-11-18

---

## OQ-001

Status: CLOSED

### Decision

Target **`.NET 8 LTS` (`net8.0` / C# 12)** across all backend projects as documented, and implement a deterministic RFC 9562 **UUID v7** generator (`UuidV7.NewGuid()` or `IGuidGenerator`) in `ElsasaSis.SharedKernel`.

### Rationale

`.NET 8.0.428` SDK is installed locally, matching `ELSASA-SIS-ARCHITECTURE.md` §14 and `backend-architecture.md`, while a small RFC 9562 helper in `SharedKernel` provides time-ordered UUID v7 primary keys without upgrading the target framework.

### Impact

All `.csproj` files specify `<TargetFramework>net8.0</TargetFramework>`; domain factories generate PKs via `SharedKernel` UUID v7 helper.

### Architecture Impact

None (confirms existing `.NET 8 LTS` target and clarifies UUID v7 realization in `SharedKernel`).

### Resolved By

User

### Resolved Date

2026-11-18

---

## OQ-002

Status: CLOSED

### Decision

1. Stage 0 DbUp migrations create **all 8 schemas** (`auth`, `outbox`, `opr`, `std`, `tch`, `acd`, `fin`, `dev`) in `001_CreateSchemas.sql`, and create **only Stage 0 tables** (`auth.users`, `auth.user_roles`, `auth.refresh_tokens`, `auth.user_actors`, `outbox.messages`, `opr.academic_years`, `opr.operational_periods`), deferring remaining `opr` and `std`/`tch`/`acd`/`fin`/`dev` tables to Stages 1–3.
2. `ElsasaSis.Database` exposes both a standalone CLI migration runner (`dotnet run --project src/backend/src/ElsasaSis.Database`) and an embeddable migration library invoked automatically on `ElsasaSis.Api` startup in `Development` environment.

### Rationale

Keeps Stage 0 clean and fast while satisfying the roadmap milestone ("Schemas Initialized: all 8 schemas") and supporting zero-friction local/test startup.

### Impact

`ElsasaSis.Database` contains `001_CreateSchemas.sql`, `002_CreateAuthTables.sql`, `003_CreateOutboxTables.sql`, `004_CreateOprStage0Tables.sql` (or `010_CreateOprAcademicYearTables.sql`), and idempotent admin seed.

### Architecture Impact

Clarifies `ELSASA-SIS-ARCHITECTURE.md` §12.3 phased script execution per stage rather than creating unused Stage 1–3 tables in Stage 0.

### Resolved By

User

### Resolved Date

2026-11-18

---

## OQ-003

Status: CLOSED

### Decision

Lock down `UC-ADM-01` Academic Year & Operational Period lifecycle and mutation rules as follows:
1. **Activate Academic Year (`POST /api/opr/academic-years/{id}/activate`):** Deactivates any previously `ACTIVE` Academic Year and its `ACTIVE` Operational Period, sets the target Academic Year to `ACTIVE`, and automatically activates its chronologically earliest Operational Period (by `start_date`) if none of its periods is active. Raises `AcademicYearActivatedDomainEvent` and `OperationalPeriodActivatedDomainEvent`.
2. **Activate Operational Period (`POST /api/opr/academic-years/{id}/periods/{periodId}/activate`):** Requires the parent Academic Year to already be `ACTIVE` (returns `409 Conflict` otherwise); deactivates any other active period and activates the target period. Raises `OperationalPeriodActivatedDomainEvent`.
3. **No Explicit Year Deactivation Endpoint:** An `ACTIVE` Academic Year transitions to `INACTIVE` only when another Academic Year is activated (preventing accidental zero-active-year states once initialized).
4. **Update Academic Year (`PUT /api/opr/academic-years/{id}`):** Allowed for both `INACTIVE` and `ACTIVE` years. Validates `BR-OPS-002..006` (at least 1 period, valid date ordering, periods within year bounds, non-overlapping periods, unique year name and unique period names within the year). Preserves `ACTIVE` status of the year and active period when updated; rejects removing the currently `ACTIVE` Operational Period (`409 Conflict`).

### Rationale

Eliminates every domain edge-case ambiguity so `AcademicYear` aggregate root invariants, endpoints, and UI guards can be implemented deterministically without agent invention.

### Impact

Defines exact endpoints, domain methods, `OprErrors` codes (`409`/`422`/`404`), and UI disable/tooltip rules on `SCR-OPR-CALENDAR` Part 1.

### Architecture Impact

Update `ELSASA-SIS-ARCHITECTURE.md` §8.1 (`UC-ADM-01` routes to include `GET /api/opr/academic-years`, `GET /api/opr/academic-years/{id}`, `POST /api/opr/academic-years`, `PUT /api/opr/academic-years/{id}`, `POST /api/opr/academic-years/{id}/activate`, `POST /api/opr/academic-years/{id}/periods/{periodId}/activate`) and §10.2 (`AcademicYearActivatedDomainEvent` / `PeriodActivatedEvent`).

### Resolved By

User

### Resolved Date

2026-11-18

---

## OQ-004

Status: CLOSED

### Decision

1. **Initial Seeded Admin:** Seed an `admin` user (`SCHOOL_ADMIN` role) idempotently during migration/seed using ASP.NET Core's PBKDF2 `PasswordHasher<object>` (`Microsoft.Extensions.Identity.Core`), reading password from configuration (`Seed:AdminPassword`, defaulting to `Admin123!` in `Development` only when unset).
2. **Role-to-Permission Mapping:** Maintain the `Role -> Permissions[]` catalog in code inside `ElsasaSis.Modules.Auth` (rather than adding an `auth.role_permissions` database table), emitting the resolved `permissions` claim array into JWTs at login/refresh.

### Rationale

Avoids hand-rolled hashing or hardcoded production secrets, removes an unnecessary `auth.role_permissions` table that was absent from §12.2, and ensures compile-time synchronization between `Permissions.*` constants and role grants.

### Impact

`auth` schema has exactly `auth.users`, `auth.user_roles`, `auth.refresh_tokens`, and `auth.user_actors`.

### Architecture Impact

Remove `auth.role_permissions` from `ELSASA-SIS-ARCHITECTURE.md` §11.1 (aligning with §12.2).

### Resolved By

User

### Resolved Date

2026-11-18

---

## OQ-005

Status: CLOSED

### Decision

1. **Module Assemblies:** Create only `ElsasaSis.Modules.Auth` and `ElsasaSis.Modules.Opr` in Stage 0 (alongside `SharedKernel`, `Contracts`, `Database`, `Api`, `ArchitectureTests`, `IntegrationTests`); defer `Std`, `Tch`, `Acd`, `Fin`, and `Dev` `.csproj` files to Stages 1–3.
2. **Academic Years List Contract:** `GET /api/opr/academic-years` uses `PagedQuery` and returns `PaginatedResponse<AcademicYearDto>` with each item's nested `periods` (`OperationalPeriodDto[]`) included.
3. **Outbox Scope:** Create `outbox.messages` table and implement `IDomainEventDispatcher` using in-process MediatR awaited post-commit; defer the Outbox background polling `BackgroundService` until Stage 1+ introduces durable cross-module async subscribers.

### Rationale

Prevents dead empty module projects and idle background polling loops in Stage 0 while establishing the standard `PaginatedResponse<T>` envelope and post-commit domain event pattern for all subsequent stages.

### Impact

`ElsasaSis.sln` remains minimal and 100% tested; frontend consumes one typed OpenAPI response for both the calendar table and the top-bar switcher.

### Architecture Impact

Clarifies Stage 0 module realization and `outbox` background worker phasing in `ELSASA-SIS-ARCHITECTURE.md`.

### Resolved By

User

### Resolved Date

2026-11-18

---

## OQ-006

Status: CLOSED

### Decision

Stage 0 `AppLayout.vue` implements the persistent Top Bar (with the global **Academic Year & Semester Switcher** backed by `useAppContextStore` + `GET /api/opr/academic-years`) and collapsible role-filtered Sidebar (`Operations & Comms > School Calendar & Academic Cycle` for `SCHOOL_ADMIN`), plus `AuthLayout.vue`. Default authenticated route `/` redirects to `/operations/calendar`.

### Rationale

Proves the complete global shell (`ui-ux-guideline.md` §3.2 & §4.2) end-to-end in Stage 0 so Stage 1+ features plug directly into `AppLayout` without shell rework.

### Impact

`BND-FE-CORE` delivers `AppLayout.vue`, `AuthLayout.vue`, `useAuthStore`, `useAppContextStore`, `useUIStore`, `http.ts` + `http.provider.ts`, `AppErrorBoundary.vue`, `StatusPill.vue`, `ConfirmActionDialog.vue`, `EmptyState.vue`, and `AppDataTable.vue`.

### Architecture Impact

None beyond `GAP-006` route alignment.

### Resolved By

User

### Resolved Date

2026-11-18

---

# 9. Architecture Applicability

> [!NOTE]
> Per `ics-feasibility-assessment`, the formal `Architecture Applicability` decision and `READY-FOR-PLANNING` gate are owned by `ica-architect`.

## Decision

`ARCHITECTURE-REQUIRED` — Confirmed by `ica-architect`. Target architecture created at [CR-STAGE-0-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/CR-STAGE-0-ARCHITECTURE.md), and baseline specifications reconciled in [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md) and [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md).

## Rationale

`ELSASA-SIS-ARCHITECTURE.md` v1.0 already defines the greenfield target architecture, but the closed Stage 0 decisions introduce concrete schema and specification updates that must be reconciled before `IMPLEMENTATION-PLAN` creation:
1. Add `status VARCHAR(20) NOT NULL` (`INACTIVE | ACTIVE`) to `opr.operational_periods` in `operational-tables-design.md` and record the `UC-ADM-01` activation/edit endpoints and lifecycle rules.
2. Harmonize `ELSASA-SIS-ARCHITECTURE.md` with the approved `src/backend/` + `src/frontend/` paths, Pure Persistence audit columns (`created_at`, `updated_at` without DB defaults or `created_by`/`updated_by`), JWT claims (`preferred_username`, `permissions`, `actorId`s, no `org_unit_id`), `outbox.messages` naming, no `auth.role_permissions` table, Tailwind v4 + PrimeVue v4 + `views/` routes (`/auth/login`, `/operations/calendar`), and LocalDB/SQL Server `IntegrationTests`.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved (`GAP-001`..`GAP-003` closed)
- [x] All required decisions recorded (`GAP-001`..`GAP-008`, `OQ-001`..`OQ-006` all `CLOSED` with rationale & impacts)
- [x] All blocking open questions resolved (`0` open questions remain)
- [ ] Architecture updated and finalized by `ica-architect` (`READY-FOR-PLANNING` gate pending Architect review)

## Status

READY-FOR-PLANNING

## Notes

From a business and architectural decision standpoint, **zero open questions remain**. Status remains `NOT-READY` strictly per SDLC governance (`ics-feasibility-assessment` / `ica-analyst`) because only `ica-architect` may grant `READY-FOR-PLANNING` after reconciling the minor architecture/table-design updates listed in Section 9 before creating the Stage 0 `IMPLEMENTATION-PLAN`.

---

# 11. References

Referenced artifacts:

- [IS001- STAGE-0-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-ISSUE.md) (`CR-STAGE-0`)
- [operational-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/operational-domain-basic.md)
- [operational-domain-priorities.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/priorities/operational-domain-priorities.md)
- [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
- [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md)
- [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
- [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
- [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)

Referenced codebase locations:

- `src/backend/` (currently empty)
- `src/frontend/` (currently empty)

Referenced documents:

- [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md)
- [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md)
- [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md)
- [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md)
