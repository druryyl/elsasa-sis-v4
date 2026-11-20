---
Title: Stage 3 Evaluation, Finance & Student Development — Implementation Plan
Code: CR-STAGE-3
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-20
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Deliver an executable, dependency-driven implementation plan for **CR-STAGE-3** (**Stage 3: Evaluation, Finance & Student Development** of **ELSASA-SIS v4 Basic Level**), transforming the completed Stage 2 codebase (`src/backend/` and `src/frontend/`) into the approved Stage 3 target technical realization across **28 actor-driven use cases** and **7 frontend feature submodules**.

Planning Mode: `FEATURE-PLANNING` (scoping `CR-STAGE-3` Stage 3 Evaluation, Finance & Student Development over the completed Stage 0, Stage 1, and Stage 2 baseline).

Referenced artifacts:

- ISSUE: [IS004- STAGE-3-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS004-%20STAGE-3-DEV-ISSUE.md) (`CR-STAGE-3`)
- FEASIBILITY-ASSESSMENT: [IS004- STAGE-3-DEV-FEASIBILITY-ASSESSMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS004-%20STAGE-3-DEV-FEASIBILITY-ASSESSMENT.md) (`Status: READY-FOR-PLANNING`; Decisions `GAP-001` through `GAP-016` and `OQ-001` through `OQ-032` binding and closed)
- ARCHITECTURE: [IS004- STAGE-3-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS004-%20STAGE-3-DEV-ARCHITECTURE.md) (`Version: 1.0`)
- Supporting Baseline Specifications:
  - [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
  - [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
  - [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
  - [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)
  - [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md)
  - [development-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/development-tables-design.md)
  - [finance-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/finance-tables-design.md)
  - [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md)
  - [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md)
  - [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md)

Architecture Applicability: `ARCHITECTURE-REQUIRED` — Target architecture defined in [IS004- STAGE-3-DEV-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS004-%20STAGE-3-DEV-ARCHITECTURE.md).

---

# 2. Planning Scope

Scope covers all Stage 3 deliverables across two strict repository boundaries (`src/backend` and `src/frontend`):

1. **Backend (`src/backend`):**
   - Two new `.NET 8 LTS` (`net8.0`) module assemblies:
     - `ElsasaSis.Modules.Fin`: Pure DDD Domain, Dapper Infrastructure, and Vertical Slices for Fee Policies (`UC-FIN-01`), Discounts (`UC-FIN-02`), Batch Billing (`UC-FIN-03`), Ad-Hoc Charges (`UC-FIN-04`), Charge Cancellation (`UC-FIN-05`), Frontline Cashier Counter & Multi-Method Payments (`UC-FIN-06`), Deposit Allocation (`UC-FIN-07`), Student Ledger Statements (`UC-FIN-08`), Receivables & Arrears Reports (`UC-FIN-09`, `UC-ADM-26`, `UC-PRC-06`), and Daily Cashier Summary (`UC-FIN-11`).
     - `ElsasaSis.Modules.Dev`: Pure DDD Domain, Dapper Infrastructure, and Vertical Slices for Behaviour Notes (`UC-TCH-06`), Classroom Discipline (`UC-TCH-07`, `UC-ADM-22`), Principal Escalated Discipline (`UC-PRC-03`), Student Achievements (`UC-TCH-08`, `UC-ADM-23`), Unified 360° Student Development Dossier (`UC-ADM-24`, `UC-HRM-04`, `UC-PRC-04`), Extracurricular Catalog & Participant Enrollment (`UC-ADM-25`), and Extracurricular Session Activity Logging (`UC-TCH-09`).
   - Extensions to `ElsasaSis.Modules.Acd`:
     - Vertical Slices for Batch Draft Report Card Generation (`UC-ADM-19`), Administrative Override/Unlock (`UC-ADM-20`), Subject Teacher Feedback Notes (`UC-TCH-04`), Homeroom Teacher Study Group Review (`UC-HRM-01`), Homeroom Holistic General Remarks (`UC-HRM-02`), Homeroom Finalization Locking (`UC-HRM-03`), Principal Review (`UC-PRC-02`), and Composite Printable Report Card DTO projection with attendance metric aggregates.
   - Extensions to `ElsasaSis.Modules.Std`:
     - Read Slice for Student Billing Contacts Lookup with Guardian Phone & WhatsApp direct links (`UC-FIN-10`).
     - Event Subscriber: `ReportCardFinalizedEventHandler` setting `is_academically_completed = 1` on `std.student_placements`.
     - Contract Implementation: Expand `IStudentContract` with `GetActiveStudentsByScopeAsync(...)`.
   - Extensions to `ElsasaSis.Contracts`:
     - Cross-module contracts: `IFinanceModule`, `IDevelopmentModule`.
     - Domain event records: `PaymentReceiptPostedDomainEvent`, `SanctionImposedDomainEvent`, `ReportCardFinalizedDomainEvent`.
   - Extensions to `ElsasaSis.Modules.Auth`:
     - Compile-time permissions in `Permissions.cs` and role mappings in `RolePermissionCatalog.cs` across Academic Report Cards, Student Development, and Finance.
   - Database Migrations (`ElsasaSis.Database`):
     - 7 idempotent DbUp scripts (`032`, `050`, `051`, `052`, `060`, `061`, `070`) and 1 deterministic test fixture seed script (`101_SeedStage3Data.sql`), plus `Stage3Seeder.cs` for integration test resets.
   - Test Suites:
     - Architecture isolation tests in `ElsasaSis.ArchitectureTests` enforcing zero cross-module references across all 7 module assemblies and pure persistence layer rules.
     - Comprehensive unit tests in `ElsasaSis.UnitTests` for domain aggregates, calculations, and validation rules.
     - Integration tests in `ElsasaSis.IntegrationTests` verifying end-to-end HTTP APIs against SQL Server.

2. **Frontend (`src/frontend`):**
   - Expanded `AppLayout.vue` role-filtered sidebar navigation:
     - `Academic & Curriculum` $\rightarrow$ `Semester Report Cards` (`/academic/report-cards`).
     - `Student Development` $\rightarrow$ `Pastoral & Discipline` (`/development/pastoral`), `Extracurricular Clubs` (`/development/extracurricular`).
     - `Finance & Billing` $\rightarrow$ `Fee Policies & Discounts` (`/finance/fee-policy`), `Tuition Billing` (`/finance/billing`), `Cashier Counter` (`/finance/payments`), `Receivables & Arrears` (`/finance/receivables`).
   - Route definitions in `routes.ts` with permission-driven guards and page titles.
   - Regenerated OpenAPI TypeScript schema (`schema.d.ts`) and TanStack Query key factories.
   - All 7 Stage 3 frontend feature submodules realizing all 28 use cases:
     1. `SCR-ACD-REPORT-CARDS` (`src/features/academic/report-cards/`): Collaborative report card review, subject teacher feedback, homeroom narrative remarks, finalize action, administrative override dialog with mandatory reason, and browser-printable view (`ReportCardPrintView.vue` with `@media print`).
     2. `SCR-DEV-PASTORAL` (`src/features/development/pastoral/`): 360° student development dossier, behaviour note composer, classroom discipline incident logger, principal escalated sanction modal (`PARENT_MEETING`, `SUSPENSION`), and achievement award logger.
     3. `SCR-DEV-EXTRACURRICULAR` (`src/features/development/extracurricular/`): Club catalog, supervisor assignment, student enrollments, and meeting session log recorder.
     4. `SCR-FIN-FEES-POLICY` (`src/features/finance/fee-policy/`): Fee catalog, recurring frequencies (`MONTHLY`, `SEMESTER`, `YEARLY`, `ONE_TIME`), grade-level standard amounts, charge type activation confirmation (Tier 2), and student discount / scholarship assignments.
     5. `SCR-FIN-BILLING` (`src/features/finance/billing/`): Monthly batch tuition billing generator with Tier 3 Pre-Flight Impact Summary Modal, ad-hoc invoice creator, and unpaid charge cancellation modal with mandatory audit reason.
     6. `SCR-FIN-PAYMENTS` (`src/features/finance/payments/`): Frontline cashier counter, guardian contact search with WhatsApp link, multi-charge allocation, advance deposit holding and application, student ledger statement, and browser-printable receipt (`PaymentReceiptPrintView.vue` with `@media print`).
     7. `SCR-FIN-RECEIVABLES` (`src/features/finance/receivables/`): Tuition receivables summary KPI cards, aged arrears breakdown table (Current, 30, 60, 90+ days), student drill-down statement drawer, and daily cashier collection summary report.
   - Vitest component tests, `pnpm typecheck`, `pnpm lint`, and multi-role live system verification (`admin`, `finance.officer`, `teacher.budi`, `homeroom.teacher`, `principal`).

3. **Strictly Excluded from Stage 3:**
   - Online payment gateway integration (Midtrans, Xendit, DOKU) — manual cashier counter only.
   - Automated dunning or direct debit engines — manual follow-up via guardian billing lookup only.
   - Cross-student credit or deposit balance transfers.
   - Formal psychological counseling case management (`dev.counseling_cases`).
   - Server-side PDF generation engines (Puppeteer, wkhtmltopdf) — clean browser printing with CSS `@media print` only.
   - Background daemon schedulers — synchronous HTTP batch requests within normal budgets for Basic level.

---

# 3. Dependencies

External/Environment prerequisites verified in the Feasibility Assessment:

- `.NET 8 SDK` (`net8.0`)
- `Node.js` (`v22+`) and `pnpm`
- Local Microsoft SQL Server (`MSSQLSERVER` / `(localdb)\MSSQLLocalDB`)
- Stage 2 baseline prerequisite: [IS003- STAGE-2-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-IMPLEMENTATION-PLAN.md) (`COMPLETED`).

For slice dependencies:

- `Depends On` declares implementation prerequisites.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation status `IMPLEMENTED` and verifiable repository output.
- Dependency satisfaction does not require review status `GO`.
- Dependencies must represent real implementation prerequisites.
- Each slice targets strictly one repository boundary (`src/backend` or `src/frontend`), never both.

### Parallel Execution Wave Summary *(Informational Only — `Depends On` Is Authoritative)*

```text
Wave 1 (Scaffolding & Shell):
  P1-S01 (src/backend)  ||  P1-S03 (src/frontend)

Wave 2 (Database Migrations):
  P1-S02 (src/backend, depends on P1-S01)

Wave 3 (Domain Models & Repositories across 3 Bounded Contexts):
  P2-S04 (Acd Report Cards)  ||  P3-S07 (Dev Domain)  ||  P4-S10 (Fin Domain)  ||  P2-S06 (Std Extensions)
  *(all src/backend, strictly disjoint module assemblies and schemas)*

Wave 4 (Vertical Slices across 3 Bounded Contexts):
  P2-S05 (Acd Slices)  ||  P3-S08 (Dev Pastoral Slices)  ||  P3-S09 (Dev Club Slices)  ||  P4-S11 (Fin Policy Slices)

Wave 5 (Billing & Cashier Engines):
  P4-S12 (Fin Billing Engine, depends on P4-S10, P4-S11, P2-S06)
  ↓
  P4-S13 (Fin Cashier Counter, depends on P4-S10, P4-S12)
  ↓
  P4-S14 (Fin Reports & Receivables, depends on P4-S10, P4-S13)

Wave 6 (Verification, Tests & OpenAPI Synchronization):
  P5-S15 (Unit & Arch Tests)  →  P5-S16 (Integration Tests)  →  P5-S17 (OpenAPI & TypeScript Client Sync)

Wave 7 (Frontend Feature Screens in Parallel):
  P6-S18 || P6-S19 || P6-S20 || P6-S21 || P6-S22 || P6-S23 || P6-S24
  *(all 7 screens in src/frontend, strictly disjoint feature folders)*

Wave 8 (Live Multi-Role Verification):
  P7-S25 (src/frontend, live multi-role verification against running monorepo stack)
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
| P1 - Foundation, Scaffolding, Migrations & Shell Setup | IMPLEMENTED | GO | 3/3 |
| P2 - Academic Report Cards & Student Module Integration | IMPLEMENTED | GO | 3/3 |
| P3 - Student Development & Extracurriculars Backend | IMPLEMENTED | GO | 3/3 |
| P4 - Fee Policies, Billing & Cashier Counter Backend | IMPLEMENTED | GO | 5/5 |
| P5 - Architecture Verification, Testing & OpenAPI Sync | IMPLEMENTED | GO | 3/3 |
| P6 - Frontend Feature Screens | IMPLEMENTED | GO | 7/7 |
| P7 - Live Verification & End-to-End System Validation | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Foundation, Scaffolding, Migrations & Shell Setup

Implementation Status: IMPLEMENTED  
Review Status: GO

### P1-S01

Title: Scaffold `ElsasaSis.Modules.Fin` and `ElsasaSis.Modules.Dev` Projects, Cross-Module Contracts, Domain Events, Auth Permissions & Role Catalog

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Create the new `ElsasaSis.Modules.Fin` and `ElsasaSis.Modules.Dev` C# module projects, register them in `ElsasaSis.sln`, declare Stage 3 cross-module contracts and domain events in `ElsasaSis.Contracts`, expand `ElsasaSis.Modules.Auth` permissions and role mappings, and update project references across `Api`, `UnitTests`, `ArchitectureTests`, and `IntegrationTests` with zero cross-module references.

Depends On: None

Repository: src/backend

Completion Criteria:

1. Create `src/backend/src/Modules/ElsasaSis.Modules.Fin/ElsasaSis.Modules.Fin.csproj` and `src/backend/src/Modules/ElsasaSis.Modules.Dev/ElsasaSis.Modules.Dev.csproj`:
   - Target `net8.0`, nullable enable, implicit usings.
   - Framework reference `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
   - Package references: `Dapper 2.1.35`, `FluentValidation 11.11.0`, `MediatR 12.4.1`.
   - Project references: strictly `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`.
   - Register both projects in `src/backend/ElsasaSis.sln` under the `Modules` solution folder.
2. Add module skeleton files in each new module:
   - `FinAssemblyMarker.cs` and `DevAssemblyMarker.cs`.
   - `Permissions.cs` in each module defining local permission constants and composite policies.
   - `DependencyInjection.cs` exposing `public static IServiceCollection AddFinModule(this IServiceCollection services)` and `public static IServiceCollection AddDevModule(this IServiceCollection services)`.
   - `Features/ValidationFilter.cs` in both modules.
3. Expand `ElsasaSis.Contracts`:
   - `Finance/IFinanceModule.cs` interface.
   - `Finance/Events/PaymentReceiptPostedDomainEvent.cs` (`Guid ReceiptId, string ReceiptNumber, Guid StudentId, decimal TotalAmount, DateTime PaymentDate`).
   - `Development/IDevelopmentModule.cs` interface.
   - `Development/Events/SanctionImposedDomainEvent.cs` (`Guid DisciplineRecordId, Guid StudentId, string Sanction, DateTime IncidentDate`).
   - `Academic/Events/ReportCardFinalizedDomainEvent.cs` (`Guid ReportCardId, Guid StudentId, Guid OperationalPeriodId, DateTime FinalizedAt`).
   - `Student/IStudentContract.cs`: Add method `Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(Guid? studyGroupId, string? schoolLevel, string? grade, CancellationToken cancellationToken = default)` and define `StudentPlacementScopeDto(Guid StudentId, string StudentName, string StudentNis, Guid StudyGroupId, string StudyGroupName, string SchoolLevel, string Grade)`.
4. Expand `src/backend/src/Modules/ElsasaSis.Modules.Auth/Permissions.cs`:
   - Academic Report Cards: `Permissions.Academic.ReportCards.View`, `Permissions.Academic.ReportCards.Generate`, `Permissions.Academic.ReportCards.ProvideNotes`, `Permissions.Academic.ReportCards.Remarks`, `Permissions.Academic.ReportCards.Finalize`, `Permissions.Academic.ReportCards.Override`.
   - Development: `Permissions.Development.Behaviour.Record`, `Permissions.Development.Discipline.Record`, `Permissions.Development.Discipline.Escalate`, `Permissions.Development.Achievements.Record`, `Permissions.Development.Extracurricular.Manage`, `Permissions.Development.Extracurricular.Log`, `Permissions.Development.Dossier.View`.
   - Finance: `Permissions.Finance.FeePolicy.Manage`, `Permissions.Finance.Discounts.Manage`, `Permissions.Finance.Billing.Generate`, `Permissions.Finance.Billing.ManageAdHoc`, `Permissions.Finance.Billing.Cancel`, `Permissions.Finance.Payments.Record`, `Permissions.Finance.Deposits.Allocate`, `Permissions.Finance.Statements.View`, `Permissions.Finance.Reports.View`.
   - Add composite authorization policies in `ElsasaSis.Modules.Auth/Policies.cs`:
     - `Policies.Academic.ReportCards.ViewOrManage`
     - `Policies.Development.Pastoral.RecordOrView`
     - `Policies.Finance.Cashier.Manage`
     - `Policies.Finance.Reports.View`
5. Expand `src/backend/src/Modules/ElsasaSis.Modules.Auth/RolePermissionCatalog.cs`:
   - `SCHOOL_ADMIN`: Possesses all Report Cards, Extracurricular Catalog, Fee Policies, Discounts, Billing, and Dossier permissions.
   - `FINANCE_OFFICER`: Possesses all Fee Policies, Discounts, Billing, Payments, Deposits, Statements, and Reports permissions.
   - `TEACHER`: Possesses `ReportCards.ProvideNotes`, `Development.Behaviour.Record`, `Development.Discipline.Record`, `Development.Achievements.Record`, `Development.Extracurricular.Log`, `Development.Dossier.View`.
   - `HOMEROOM_TEACHER`: Inherits teacher permissions plus `ReportCards.Remarks`, `ReportCards.Finalize`, and homeroom-scoped dossier access.
   - `PRINCIPAL`: Possesses `ReportCards.View`, `Development.Discipline.Escalate`, `Development.Dossier.View`, `Finance.Reports.View`.
6. Update project references:
   - Add references to `ElsasaSis.Modules.Fin` and `ElsasaSis.Modules.Dev` in `ElsasaSis.Api.csproj`, `ElsasaSis.UnitTests.csproj`, `ElsasaSis.ArchitectureTests.csproj`, and `ElsasaSis.IntegrationTests.csproj`.
   - Assert zero inter-module project references among `{Auth, Opr, Std, Tch, Acd, Fin, Dev}`.
7. Update `src/backend/src/ElsasaSis.Api/Program.cs`:
   - Register MediatR services from `typeof(FinAssemblyMarker).Assembly` and `typeof(DevAssemblyMarker).Assembly`.
   - Call `builder.Services.AddFinModule()` and `builder.Services.AddDevModule()`.
   - Add `typeof(FinAssemblyMarker).Assembly` and `typeof(DevAssemblyMarker).Assembly` to `app.MapDiscoveredEndpoints(...)`.
   - Register Stage 3 composite authorization policies.
8. Update `src/backend/tests/ElsasaSis.UnitTests/Auth/RolePermissionCatalogTests.cs` to verify all Stage 3 role-permission mappings.
9. `dotnet build src/backend/ElsasaSis.sln` and `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` succeed with exit code `0`.

Notes: Preserve all existing Stage 0, Stage 1, and Stage 2 tests. Ensure policy names match string-for-string across `Modules.Auth` and module permission classes.

Implementation Notes:
- Created `ElsasaSis.Modules.Fin` and `ElsasaSis.Modules.Dev` projects targeting `net8.0` with `Dapper`, `FluentValidation`, `MediatR`, and strictly referencing `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`. Registered both under `Modules` solution folder in `ElsasaSis.sln`.
- Added skeleton module files: `FinAssemblyMarker.cs`, `DevAssemblyMarker.cs`, `DependencyInjection.cs` (`AddFinModule`, `AddDevModule`), `Permissions.cs`, `Features/ValidationFilter.cs`, and stub contracts/errors in each module.
- Expanded `ElsasaSis.Contracts`:
  - `Finance/IFinanceModule.cs` and `Finance/Events/PaymentReceiptPostedDomainEvent.cs`
  - `Development/IDevelopmentModule.cs` and `Development/Events/SanctionImposedDomainEvent.cs`
  - `Academic/Events/ReportCardFinalizedDomainEvent.cs`
  - `Student/IStudentContract.cs`: added `GetActiveStudentsByScopeAsync` and `StudentPlacementScopeDto`. Implemented query in `StudentContract.cs` (`Modules.Std`).
- Expanded `ElsasaSis.Modules.Auth/Permissions.cs` with 22 new Stage 3 permissions across Academic Report Cards, Student Development, and Finance, plus Stage 3 composite policies. Updated local `Permissions.cs` in `Modules.Acd`, `Modules.Dev`, and `Modules.Fin`.
- Expanded `ElsasaSis.Modules.Auth/RolePermissionCatalog.cs` with role mappings for `SCHOOL_ADMIN`, `FINANCE_OFFICER`, `TEACHER`, `HOMEROOM_TEACHER`, and `PRINCIPAL`.
- Updated project references in `ElsasaSis.Api.csproj`, `ElsasaSis.UnitTests.csproj`, `ElsasaSis.ArchitectureTests.csproj`, and `ElsasaSis.IntegrationTests.csproj`.
- Updated `ElsasaSis.Api/Program.cs` to register MediatR handlers, module DI services, discovered endpoints, and composite policies for `Fin` and `Dev`.
- Updated `ElsasaSis.ArchitectureTests`: added `Fin` and `Dev` assemblies to `AssemblyArchitectureContext`, `ModuleBoundaryTests` (enforcing zero inter-module references across all 7 modules), and `LayerAndCqrsArchitectureTests`.
- Updated `RolePermissionCatalogTests.cs` and mock student contracts in unit tests.
- Verification: `dotnet build src/backend/ElsasaSis.sln` succeeded (0 warnings, 0 errors); 431 Unit Tests passed; 17 Architecture Tests passed.

Changed Files:
- `src/backend/ElsasaSis.sln`
- `src/backend/src/ElsasaSis.Api/ElsasaSis.Api.csproj`
- `src/backend/src/ElsasaSis.Api/Program.cs`
- `src/backend/src/ElsasaSis.Contracts/Academic/Events/ReportCardFinalizedDomainEvent.cs`
- `src/backend/src/ElsasaSis.Contracts/Development/Events/SanctionImposedDomainEvent.cs`
- `src/backend/src/ElsasaSis.Contracts/Development/IDevelopmentModule.cs`
- `src/backend/src/ElsasaSis.Contracts/Finance/Events/PaymentReceiptPostedDomainEvent.cs`
- `src/backend/src/ElsasaSis.Contracts/Finance/IFinanceModule.cs`
- `src/backend/src/ElsasaSis.Contracts/Student/IStudentContract.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Permissions.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Auth/Permissions.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Auth/RolePermissionCatalog.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/DependencyInjection.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/DevAssemblyMarker.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/DevErrors.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/ElsasaSis.Modules.Dev.csproj`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/ValidationFilter.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Infrastructure/Contracts/DevelopmentModuleContract.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Permissions.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/DependencyInjection.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/FinErrors.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/ElsasaSis.Modules.Fin.csproj`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/ValidationFilter.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/FinAssemblyMarker.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Contracts/FinanceModuleContract.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Permissions.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Infrastructure/Contracts/StudentContract.cs`
- `src/backend/tests/ElsasaSis.ArchitectureTests/AssemblyArchitectureContext.cs`
- `src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj`
- `src/backend/tests/ElsasaSis.ArchitectureTests/LayerAndCqrsArchitectureTests.cs`
- `src/backend/tests/ElsasaSis.ArchitectureTests/ModuleBoundaryTests.cs`
- `src/backend/tests/ElsasaSis.IntegrationTests/ElsasaSis.IntegrationTests.csproj`
- `src/backend/tests/ElsasaSis.UnitTests/Acd/AcademicParticipationFeatureTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Acd/AttendanceFeatureTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Acd/ScorebookFeatureTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Auth/RolePermissionCatalogTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj`
- `src/backend/tests/ElsasaSis.UnitTests/Opr/TimetableFeaturesTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P1-S02

Title: Implement Stage 3 DbUp Migration Scripts (`032`, `050`, `051`, `052`, `060`, `061`, `070`, `101`), Seed Data & `Stage3Seeder`

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Author all 8 Stage 3 embedded Pure Persistence SQL scripts in `ElsasaSis.Database/Scripts/` covering 11 new tables, 4 schema modifications, deterministic verification fixtures, and `Stage3Seeder.cs` for integration test resets.

Depends On: P1-S01

Repository: src/backend

Completion Criteria:

1. Embedded SQL scripts exist under `src/backend/src/ElsasaSis.Database/Scripts/`, matching `IS004- STAGE-3-DEV-ARCHITECTURE.md` §8 and authoritative table designs with zero DB FKs, zero `DEFAULT` constraints, zero `CHECK` constraints, zero triggers, zero stored procedures, UUID v7 clustered PKs, and `DATETIME2(3) NOT NULL` UTC timestamps:
   - `032_AlterAcdReportCardsAddOverrideReason.sql`:
     - Add `override_reason NVARCHAR(500) NULL` and `general_remarks NVARCHAR(1000) NULL` to `acd.report_cards`.
   - `050_CreateFinTables.sql`:
     - `fin.charge_types`: PK `id`, `academic_year_id`, `academic_year_name`, `name`, `frequency` (`MONTHLY`, `SEMESTER`, `YEARLY`, `ONE_TIME`), `amount` (DECIMAL(18,2)), `school_level` (NULL), `grade` (NULL), `study_group_id` (NULL), `study_group_name` (NULL), `status` (`DRAFT`, `ACTIVE`), `created_at`, `updated_at`. Index `IX_charge_types_year_status`.
     - `fin.student_discounts`: PK `id`, `academic_year_id`, `academic_year_name`, `charge_type_id`, `charge_type_name`, `student_id`, `student_name`, `student_nis`, `discount_type` (`FIXED_AMOUNT`, `PERCENTAGE`), `discount_value` (DECIMAL(18,2)), `reason`, `is_active` (BIT), `created_at`, `updated_at`. Unique constraint `UQ_student_discounts_student_charge_year` on `(student_id, charge_type_id, academic_year_id)`.
     - `fin.student_charges`: PK `id`, `academic_year_id`, `academic_year_name`, `operational_period_id` (NULL), `operational_period_name` (NULL), `billing_month` (CHAR(7) NULL), `charge_type_id`, `charge_type_name`, `charge_frequency`, `student_id`, `student_name`, `student_nis`, `school_level`, `grade`, `study_group_id` (NULL), `study_group_name` (NULL), `description` (NULL), `unit_amount` (DECIMAL(18,2)), `quantity` (INT), `gross_amount` (DECIMAL(18,2)), `discount_amount` (DECIMAL(18,2)), `discount_reason` (NULL), `net_amount` (DECIMAL(18,2)), `paid_amount` (DECIMAL(18,2)), `outstanding_amount` (DECIMAL(18,2)), `status` (`UNPAID`, `PARTIALLY_PAID`, `PAID`, `CANCELLED`), `due_date` (DATE NULL), `cancellation_reason` (NULL), `cancelled_at` (NULL), `created_at`, `updated_at`. Filtered unique index `UQ_student_charges_monthly` on `(student_id, charge_type_id, billing_month)` WHERE `billing_month IS NOT NULL AND status <> 'CANCELLED'`. Filtered unique index for `YEARLY` and `SEMESTER`. Indexes `IX_student_charges_student_status`, `IX_student_charges_period_group`.
     - `fin.payment_receipts`: PK `id`, `receipt_number` (NVARCHAR(30) NOT NULL), `academic_year_id`, `academic_year_name`, `student_id`, `student_name`, `student_nis`, `study_group_id` (NULL), `study_group_name` (NULL), `payment_date` (DATE), `payment_method` (`CASH`, `BANK_TRANSFER`), `bank_reference` (NULL), `total_amount` (DECIMAL(18,2)), `allocated_amount` (DECIMAL(18,2)), `unallocated_amount` (DECIMAL(18,2)), `received_by_id`, `received_by_name`, `notes` (NULL), `created_at`, `updated_at`. Unique constraint `UQ_payment_receipts_receipt_number`. Index `IX_payment_receipts_student_date`.
     - `fin.payment_allocations`: PK `id`, `payment_receipt_id`, `receipt_number`, `student_charge_id`, `charge_type_name`, `billing_month` (NULL), `student_id`, `student_name`, `student_nis`, `allocated_amount` (DECIMAL(18,2)), `allocation_date` (DATE), `created_at`, `updated_at`. Index `IX_payment_allocations_receipt`, Index `IX_payment_allocations_charge`.
   - `051_CreateFinReceiptSequences.sql`:
     - `fin.receipt_sequences`: PK `month` (`CHAR(7)`), `next_seq` (`INT NOT NULL`).
   - `052_AlterFinStudentChargesAddCancellation.sql`:
     - Ensures `cancellation_reason NVARCHAR(500) NULL` and `cancelled_at DATETIME2(3) NULL` are present on `fin.student_charges` (idempotent guard).
   - `060_CreateDevTables.sql`:
     - `dev.behaviour_notes`: PK `id`, `student_id`, `student_name`, `student_nis`, `academic_year_id` (NULL), `academic_year_name` (NULL), `behaviour_type` (`POSITIVE`, `NEGATIVE`), `observed_date` (DATE), `description` (NVARCHAR(1000)), `recorder_id`, `recorder_name`, `created_at`, `updated_at`. Index `IX_behaviour_notes_student_date`.
     - `dev.discipline_records`: PK `id`, `student_id`, `student_name`, `student_nis`, `academic_year_id` (NULL), `academic_year_name` (NULL), `incident_date` (DATE), `violation_type` (VARCHAR(50)), `description` (NVARCHAR(1000)), `sanction` (`VERBAL_WARNING`, `WRITTEN_WARNING`, `PARENT_MEETING`, `SUSPENSION`), `recorder_id`, `recorder_name`, `created_at`, `updated_at`. Index `IX_discipline_records_student_date`.
     - `dev.extracurricular_activities`: PK `id`, `name` (NVARCHAR(100) NOT NULL), `description` (NVARCHAR(500) NULL), `supervisor_id`, `supervisor_name`, `status` (`ACTIVE`, `INACTIVE`), `created_at`, `updated_at`. Unique constraint `UQ_extracurricular_activities_name`.
     - `dev.extracurricular_participations`: PK `id`, `activity_id`, `activity_name`, `student_id`, `student_name`, `student_nis`, `academic_year_id`, `academic_year_name`, `status` (`ACTIVE`, `COMPLETED`, `WITHDRAWN`), `joined_date` (DATE), `completed_at` (NULL), `withdrawn_at` (NULL), `last_session_log` (NVARCHAR(1000) NULL), `last_session_date` (DATE NULL), `created_at`, `updated_at`. Unique constraint `UQ_extracurricular_participations_student_activity_year` on `(student_id, activity_id, academic_year_id)`.
     - `dev.achievements`: PK `id`, `student_id`, `student_name`, `student_nis`, `academic_year_id` (NULL), `academic_year_name` (NULL), `achievement_type` (VARCHAR(30)), `event_name` (NVARCHAR(200)), `recognition_level` (`SCHOOL`, `CITY`, `PROVINCE`, `NATIONAL`), `achievement_date` (DATE), `description` (NVARCHAR(500) NULL), `recorder_id`, `recorder_name`, `created_at`, `updated_at`. Index `IX_achievements_student_date`.
   - `061_AlterDevExtracurricularParticipationsAddSessionLog.sql`:
     - Ensures `last_session_log` and `last_session_date` columns exist on `dev.extracurricular_participations` (idempotent guard).
   - `070_AlterStdStudentPlacementsAddAcademicCompletion.sql`:
     - Add `is_academically_completed BIT NOT NULL DEFAULT 0` (or backfilled) to `std.student_placements`.
   - `101_SeedStage3Data.sql`: Idempotently seeds:
     - Fee structures: SPP Bulanan SMP (Rp 350.000, ACTIVE), SPP Bulanan SMA (Rp 450.000, ACTIVE), Uniforms (Rp 250.000, ACTIVE).
     - Standing discounts: Sibling discount 20% on SPP for seeded student; Scholarship fixed Rp 100.000.
     - Student charges for study group 7A students for active month with paid, partially paid, and unpaid statuses.
     - Cashier payment receipts with allocations and unallocated deposit holding.
     - Initial sequence in `fin.receipt_sequences` for current month.
     - Sample behaviour notes (positive and negative).
     - Sample discipline records (verbal warning, suspension).
     - Extracurricular clubs: Pramuka, Futsal, English Club with assigned teacher supervisors and active student members.
     - Sample student achievements (Olimpiade Sains, Futsal).
     - Draft and finalized report cards with subject teacher notes for study group 7A.
2. Implement `src/backend/src/ElsasaSis.Database/Stage3Seeder.cs` exposing idempotent `SeedStage3FixturesAsync(string connectionString, CancellationToken cancellationToken = default)` for integration test resets on demand.
3. Running `ElsasaSis.Database` twice idempotently against local SQL Server succeeds with exit code `0`, creating all 11 Stage 3 tables and seeding operational fixtures without error.

Notes: All tables must follow Pure Persistence rules strictly: application UUID v7 PKs, UTC `DATETIME2(3)` timestamps, zero foreign keys.

Implementation Notes:
- Authored all 8 embedded Pure Persistence SQL scripts in `src/backend/src/ElsasaSis.Database/Scripts/`:
  - `032_AlterAcdReportCardsAddOverrideReason.sql`: Idempotently adds `override_reason NVARCHAR(500) NULL` and `general_remarks NVARCHAR(1000) NULL` to `acd.report_cards`.
  - `050_CreateFinTables.sql`: Creates `fin.charge_types`, `fin.student_discounts`, `fin.student_charges`, `fin.payment_receipts`, and `fin.payment_allocations` with clustered PKs on `id`, UTC `DATETIME2(3)` timestamps, zero foreign keys, filtered unique indexes for monthly/yearly/semester charges (`UQ_student_charges_monthly`, `UQ_student_charges_yearly`, `UQ_student_charges_semester`), unique constraint on `receipt_number`, and performance nonclustered indexes.
  - `051_CreateFinReceiptSequences.sql`: Creates `fin.receipt_sequences` with PK on `month` (`CHAR(7)`) and `next_seq INT`.
  - `052_AlterFinStudentChargesAddCancellation.sql`: Idempotently ensures `cancellation_reason NVARCHAR(500) NULL` and `cancelled_at DATETIME2(3) NULL` on `fin.student_charges`.
  - `060_CreateDevTables.sql`: Creates `dev.behaviour_notes`, `dev.discipline_records`, `dev.extracurricular_activities`, `dev.extracurricular_participations`, and `dev.achievements` with clustered PKs on `id`, UTC `DATETIME2(3)` timestamps, zero foreign keys, unique constraint on activity name, and unique constraint on student-activity-year.
  - `061_AlterDevExtracurricularParticipationsAddSessionLog.sql`: Idempotently ensures `last_session_log NVARCHAR(1000) NULL` and `last_session_date DATE NULL` on `dev.extracurricular_participations`.
  - `070_AlterStdStudentPlacementsAddAcademicCompletion.sql`: Idempotently adds `is_academically_completed BIT NOT NULL` with `DEFAULT 0` constraint on `std.student_placements` for compatibility with earlier seed scripts.
  - `101_SeedStage3Data.sql`: Idempotently seeds deterministic fixtures for Stage 3:
    - Dedicated finance officer user (`finance.officer` with role `FINANCE_OFFICER`).
    - Active fee structures: SPP Bulanan SMP (Rp 350.000, ACTIVE), SPP Bulanan SMA (Rp 450.000, ACTIVE), Seragam Sekolah (Rp 250.000, ACTIVE).
    - Standing discounts: Sibling discount 20% on SPP for Siti Nurhaliza; Scholarship fixed Rp 100.000 for Ahmad Dahlan.
    - Monthly charges for 7A students for month `2026-07`: Ahmad SPP (PAID), Siti SPP (PARTIALLY_PAID), Ahmad Uniforms (UNPAID).
    - Payment receipts with itemized allocations and unallocated deposit holding (Siti: Rp 50.000 deposit holding).
    - Concurrency counter in `fin.receipt_sequences` for `2026-07` (next_seq = 3).
    - Behaviour notes (positive for Ahmad Dahlan, negative for Siti Nurhaliza).
    - Discipline records (verbal warning for Ahmad Dahlan, suspension for Siti Nurhaliza).
    - Extracurricular activities (Pramuka, Futsal, English Club) with assigned supervisor Budi Santoso and active student participations with session logs.
    - Student achievements (Ahmad: OSN Matematika, Siti: Kejuaraan Futsal Daerah).
    - Draft and finalized report cards with subject teacher notes for MTK & BING for study group 7A, plus `is_academically_completed = 1` for Ahmad Dahlan.
- Implemented `Stage3Seeder.cs` exposing public deterministic constant IDs and idempotent `SeedStage3FixturesAsync(string connectionString, CancellationToken cancellationToken = default)` executing prerequisite `Stage2Seeder` and `101_SeedStage3Data.sql` inside a transaction.
- Updated `IntegrationTestWebApplicationFactory` and `IntegrationTestBase` with `SeedStage3FixturesAsync` helper methods.
- Authored integration test `Stage3DatabaseAndSeederIntegrationTests.cs` verifying 11 tables, 4 column modifications, seed idempotency, and data assertions.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded (0 warnings, 0 errors).
  - Executed `ElsasaSis.Database` twice idempotently against local SQL Server; both runs completed with exit code 0.
  - All 431 Unit Tests passed.
  - All 17 Architecture Tests passed.
  - All 36 Integration Tests passed (including `Stage3DatabaseAndSeederIntegrationTests`).

Changed Files:
- `src/backend/src/ElsasaSis.Database/Scripts/032_AlterAcdReportCardsAddOverrideReason.sql`
- `src/backend/src/ElsasaSis.Database/Scripts/050_CreateFinTables.sql`
- `src/backend/src/ElsasaSis.Database/Scripts/051_CreateFinReceiptSequences.sql`
- `src/backend/src/ElsasaSis.Database/Scripts/052_AlterFinStudentChargesAddCancellation.sql`
- `src/backend/src/ElsasaSis.Database/Scripts/060_CreateDevTables.sql`
- `src/backend/src/ElsasaSis.Database/Scripts/061_AlterDevExtracurricularParticipationsAddSessionLog.sql`
- `src/backend/src/ElsasaSis.Database/Scripts/070_AlterStdStudentPlacementsAddAcademicCompletion.sql`
- `src/backend/src/ElsasaSis.Database/Scripts/101_SeedStage3Data.sql`
- `src/backend/src/ElsasaSis.Database/Stage3Seeder.cs`
- `src/backend/tests/ElsasaSis.IntegrationTests/Fixtures/IntegrationTestBase.cs`
- `src/backend/tests/ElsasaSis.IntegrationTests/Fixtures/IntegrationTestWebApplicationFactory.cs`
- `src/backend/tests/ElsasaSis.IntegrationTests/Stage3DatabaseAndSeederIntegrationTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P1-S03

Title: Expand Frontend Shell Navigation (`AppLayout.vue`), Stage 3 Route Definitions & Stub Views

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Expand `AppLayout.vue` role-filtered navigation with all Stage 3 sections and items, configure lazy-loaded route definitions in `routes.ts`, and create stub views for the 7 new screens to establish shell navigation before screen development.

Depends On: None

Repository: src/frontend

Completion Criteria:

1. Update `src/frontend/src/app/layouts/AppLayout.vue`:
   - Import Lucide icons: `HeartPulse`, `Award`, `Coins`, `Receipt`, `FileSpreadsheet`, `Wallet`, `CalendarClock`, `GraduationCap`.
   - In `Academic & Curriculum` nav group, add:
     - `Semester Report Cards` (`to: { name: 'academic.report-cards' }`, icon: `GraduationCap`, permissions: `['Permissions.Academic.ReportCards.View', 'Permissions.Academic.ReportCards.Generate', 'Permissions.Academic.ReportCards.ProvideNotes', 'Permissions.Academic.ReportCards.Remarks', 'Permissions.Academic.ReportCards.Finalize']`).
   - Add new nav group `Student Development`:
     - `Pastoral & Discipline` (`to: { name: 'development.pastoral' }`, icon: `HeartPulse`, permissions: `['Permissions.Development.Behaviour.Record', 'Permissions.Development.Discipline.Record', 'Permissions.Development.Discipline.Escalate', 'Permissions.Development.Achievements.Record', 'Permissions.Development.Dossier.View']`).
     - `Extracurricular Clubs` (`to: { name: 'development.extracurricular' }`, icon: `Award`, permissions: `['Permissions.Development.Extracurricular.Manage', 'Permissions.Development.Extracurricular.Log']`).
   - Add new nav group `Finance & Billing`:
     - `Fee Policies & Discounts` (`to: { name: 'finance.fee-policy' }`, icon: `Coins`, permissions: `['Permissions.Finance.FeePolicy.Manage', 'Permissions.Finance.Discounts.Manage']`).
     - `Tuition Billing` (`to: { name: 'finance.billing' }`, icon: `FileSpreadsheet`, permissions: `['Permissions.Finance.Billing.Generate', 'Permissions.Finance.Billing.ManageAdHoc', 'Permissions.Finance.Billing.Cancel']`).
     - `Cashier Counter` (`to: { name: 'finance.payments' }`, icon: `Wallet`, permissions: `['Permissions.Finance.Payments.Record', 'Permissions.Finance.Deposits.Allocate', 'Permissions.Finance.Statements.View']`).
     - `Receivables & Arrears` (`to: { name: 'finance.receivables' }`, icon: `Receipt`, permissions: `['Permissions.Finance.Reports.View']`).
2. Update `src/frontend/src/app/router/routes.ts`:
   - Register route `/academic/report-cards` (`name: 'academic.report-cards'`).
   - Register routes under `/development`:
     - `pastoral` (`name: 'development.pastoral'`)
     - `extracurricular` (`name: 'development.extracurricular'`)
   - Register routes under `/finance`:
     - `fee-policy` (`name: 'finance.fee-policy'`)
     - `billing` (`name: 'finance.billing'`)
     - `payments` (`name: 'finance.payments'`)
     - `receivables` (`name: 'finance.receivables'`)
3. Create stub views returning simple container templates:
   - `src/features/academic/report-cards/views/ReportCardsWorkbenchView.vue`
   - `src/features/development/pastoral/views/PastoralDossierView.vue`
   - `src/features/development/extracurricular/views/ExtracurricularWorkbenchView.vue`
   - `src/features/finance/fee-policy/views/FeesPolicyView.vue`
   - `src/features/finance/billing/views/BillingWorkbenchView.vue`
   - `src/features/finance/payments/views/PaymentsWorkbenchView.vue`
   - `src/features/finance/receivables/views/ReceivablesAgingView.vue`
4. Update unit tests in `src/frontend/src/app/router/__tests__/router.guard.spec.ts` to assert that Stage 3 routes enforce authentication and role permissions correctly.
5. `pnpm build` and `pnpm test:unit` succeed with exit code `0`.

Implementation Notes:
- Expanded `AppLayout.vue` sidebar navigation with `Semester Report Cards` under `Academic & Curriculum`, new nav group `Student Development` (`Pastoral & Discipline`, `Extracurricular Clubs`), and new nav group `Finance & Billing` (`Fee Policies & Discounts`, `Tuition Billing`, `Cashier Counter`, `Receivables & Arrears`).
- Added Lucide icons (`Award`, `Coins`, `HeartPulse`, `Receipt`, `Wallet`) to `AppLayout.vue`.
- Registered lazy-loaded routes in `routes.ts` with required permissions and titles for all 7 Stage 3 screens.
- Created all 7 stub views in their respective feature directories.
- Expanded `router.guard.spec.ts` with unauthenticated redirect tests and permission access/forbidden matrix tests covering all 7 Stage 3 routes.
- Verified with `pnpm test:unit` (41 test suites, 263 tests passed), `pnpm build` (vue-tsc + vite build completed cleanly), and `pnpm lint` (0 errors/warnings).

Changed Files:
- `src/frontend/src/app/layouts/AppLayout.vue`
- `src/frontend/src/app/router/routes.ts`
- `src/frontend/src/app/router/__tests__/router.guard.spec.ts`
- `src/frontend/src/features/academic/report-cards/views/ReportCardsWorkbenchView.vue`
- `src/features/development/pastoral/views/PastoralDossierView.vue`
- `src/features/development/extracurricular/views/ExtracurricularWorkbenchView.vue`
- `src/frontend/src/features/finance/fee-policy/views/FeesPolicyView.vue`
- `src/frontend/src/features/finance/billing/views/BillingWorkbenchView.vue`
- `src/frontend/src/features/finance/payments/views/PaymentsWorkbenchView.vue`
- `src/frontend/src/features/finance/receivables/views/ReceivablesAgingView.vue`

---

## P2 - Academic Report Cards & Student Module Integration

Implementation Status: IMPLEMENTED  
Review Status: GO

### P2-S04

Title: Implement Academic Report Cards Domain Model, Errors & Repository (`ReportCard`, `ReportCardEntry`, `IReportCardRepository`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Create the pure Domain aggregate root `ReportCard` (with child entity `ReportCardEntry`), domain error definitions, and Dapper repository implementation inside `ElsasaSis.Modules.Acd`.

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Create Domain Entities in `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/ReportCards/`:
   - `ReportCard` Aggregate Root:
     - Properties: `Id`, `StudentId`, `StudentName`, `StudentNis`, `StudentNisn`, `AcademicYearId`, `AcademicYearName`, `OperationalPeriodId`, `OperationalPeriodName`, `StudyGroupId`, `StudyGroupName`, `HomeroomTeacherId`, `HomeroomTeacherName`, `Status` (`DRAFT`, `FINALIZED`), `OverrideReason`, `GeneralRemarks`, `FinalizedAt`, `CreatedAt`, `UpdatedAt`.
     - Private constructor, factory method `CreateDraft(...)`.
     - Business methods:
       - `Finalize(DateTime utcNow)`: Enforces `Status == DRAFT`, sets `FINALIZED`, sets `FinalizedAt`. Raises `ReportCardFinalizedDomainEvent`.
       - `OverrideToDraft(string reason, DateTime utcNow)`: Enforces `Status == FINALIZED`, requires non-empty reason, clears `FinalizedAt`, sets `OverrideReason = reason`, transitions to `DRAFT`.
       - `UpdateHomeroomRemarks(string remarks, DateTime utcNow)`: Enforces `Status == DRAFT`.
   - `ReportCardEntry` Entity:
     - Properties: `Id`, `ReportCardId`, `SubjectId`, `SubjectName`, `SubjectCode`, `SubjectStageId`, `SubjectStageName`, `TeacherId`, `TeacherName`, `NumericScore`, `LetterGrade`, `TeacherNotes`, `CreatedAt`, `UpdatedAt`.
     - Business method `UpdateTeacherNotes(Guid teacherId, string teacherName, string notes, DateTime utcNow)`: Enforces non-empty notes and updates teacher signature snapshot.
2. Create `AcdErrors.ReportCards` error definitions in `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/AcdErrors.cs`:
   - `ReportCardNotFound`, `AlreadyFinalized`, `NotFinalized`, `EmptyOverrideReason`, `TeacherNotAssignedToSubject`.
3. Create Repository Interface and Dapper implementation:
   - Interface `IReportCardRepository` in `Domain/ReportCards/IReportCardRepository.cs`:
     - `Task<ReportCard?> GetByIdAsync(Guid id, CancellationToken ct = default)`
     - `Task<ReportCard?> GetByStudentAndPeriodAsync(Guid studentId, Guid periodId, CancellationToken ct = default)`
     - `Task<IReadOnlyList<ReportCardEntry>> GetEntriesByReportCardIdAsync(Guid reportCardId, CancellationToken ct = default)`
     - `Task InsertAsync(ReportCard reportCard, IEnumerable<ReportCardEntry> entries, CancellationToken ct = default)`
     - `Task UpdateAsync(ReportCard reportCard, CancellationToken ct = default)`
     - `Task UpdateEntryNotesAsync(ReportCardEntry entry, CancellationToken ct = default)`
   - Implementation in `Infrastructure/Persistence/ReportCardRepository.cs` using `ISqlConnectionFactory` and Dapper with parameterized SQL queries. Register in `DependencyInjection.cs`.
4. Unit tests in `ElsasaSis.UnitTests/Academic/ReportCardDomainTests.cs` verifying state transitions and invariant enforcements pass.

Implementation Notes:
- Created domain model in `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/ReportCards/`:
  - `ReportCardStatus`: Constants `Draft = "DRAFT"` and `Finalized = "FINALIZED"`.
  - `ReportCard` Aggregate Root: Extends `AggregateRoot<Guid>`, encapsulates state properties, invariants, `CreateDraft(...)`, `Finalize(...)` raising `ReportCardFinalizedDomainEvent`, `OverrideToDraft(...)` enforcing mandatory reason, `UpdateHomeroomRemarks(...)` enforcing DRAFT status, and `Rehydrate(...)`.
  - `ReportCardEntry` Entity: Extends `Entity<Guid>`, encapsulates entry properties, `Create(...)`, `UpdateTeacherNotes(...)` enforcing non-empty notes, and `Rehydrate(...)`.
- Created `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/AcdErrors.cs` with `AcdErrors.ReportCards` containing `ReportCardNotFound`, `AlreadyFinalized`, `NotFinalized`, `EmptyOverrideReason`, `TeacherNotAssignedToSubject`, `EmptyTeacherNotes`, `EntryNotFound`, `StudentIdRequired`, `AcademicYearRequired`, and `OperationalPeriodRequired`.
- Created `IReportCardRepository` interface in `Domain/ReportCards/IReportCardRepository.cs` declaring all required query and persistence contracts.
- Implemented `ReportCardRepository` in `Infrastructure/Persistence/ReportCardRepository.cs` using `ISqlConnectionFactory` and Dapper with parameterized SQL queries and transactional batch entry inserts.
- Registered `IReportCardRepository` in `src/backend/src/Modules/ElsasaSis.Modules.Acd/DependencyInjection.cs`.
- Authored comprehensive unit tests in `src/backend/tests/ElsasaSis.UnitTests/Academic/ReportCardDomainTests.cs` verifying draft creation, parameter validation, finalization state transition and domain event raising, finalization idempotency conflicts, override unlocking with audit reasons, homeroom remarks updates, subject teacher notes updates, and rehydration.
- Verification: `dotnet build src/backend/ElsasaSis.sln` succeeded with 0 warnings and 0 errors; all 22 tests in `ReportCardDomainTests` passed; all 489 unit tests in `ElsasaSis.UnitTests` passed.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/AcdErrors.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/ReportCards/ReportCardStatus.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/ReportCards/ReportCard.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/ReportCards/ReportCardEntry.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/ReportCards/IReportCardRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Infrastructure/Persistence/ReportCardRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/DependencyInjection.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Academic/ReportCardDomainTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P2-S05

Title: Implement Academic Report Cards Vertical Slices (`Features/ReportCards/`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement all 8 report card vertical slices (batch generation with arithmetic mean calculation, subject teacher notes, homeroom remarks, homeroom finalization, administrative override, homeroom study group query, principal overview, and composite printable DTO projection) inside `ElsasaSis.Modules.Acd`.

Depends On: P2-S04

Repository: src/backend

Completion Criteria:

1. Implement Write Slices in `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/`:
   - `GenerateDraftReportCards` (`POST /api/acd/report-cards/generate-batch` — `UC-ADM-19`):
     - Handler queries students in target study group.
     - Computes arithmetic final grade per subject stage from `acd.assignment_scores`: $\text{ROUND}(\text{AVG}(\text{score}), 2)$ (or `0.00` if no scores exist).
     - Checks existing `acd.report_cards` for that student and period; skips existing cards idempotently (`OQ-003`).
     - Inserts new draft cards and entries. Returns `{ generatedCount, skippedCount }`.
   - `ProvideSubjectReportCardNotes` (`POST /api/acd/report-cards/{id}/subject-notes` — `UC-TCH-04`):
     - Validates caller has active teaching assignment in `tch.teacher_assignments` for that study group and subject stage.
     - Updates `teacher_notes`, `teacher_id`, `teacher_name` on the entry row. Rejects with 409 if card is `FINALIZED`.
   - `WriteHomeroomRemarks` (`POST /api/acd/report-cards/{id}/homeroom-remarks` — `UC-HRM-02`):
     - Validates card is in `DRAFT` status and caller is the assigned homeroom teacher or admin. Updates `general_remarks`.
   - `FinalizeHomeroomReportCards` (`POST /api/acd/report-cards/{id}/finalize` — `UC-HRM-03`):
     - Transitions card to `FINALIZED`, records `finalized_at`, dispatches `ReportCardFinalizedDomainEvent`.
   - `OverrideReportCardStatus` (`POST /api/acd/report-cards/{id}/override` — `UC-ADM-20`):
     - Authorized strictly for `SCHOOL_ADMIN`.
     - Validates non-empty `overrideReason`. Unlocks card back to `DRAFT`, records `override_reason`, clears `finalized_at`.
2. Implement Read Slices:
   - `GetHomeroomReportCards` (`GET /api/acd/report-cards/homeroom?studyGroupId=&operationalPeriodId=` — `UC-HRM-01`):
     - Direct Dapper projection returning student cards with finalization status and completion indicators.
   - `ReviewAllReportCards` (`GET /api/acd/report-cards/overview?operationalPeriodId=&studyGroupId=` — `UC-PRC-02`):
     - Principal overview across all classes with grade averages and lock statuses.
   - `GetReportCardById` (`GET /api/acd/report-cards/{id}`):
     - Composite DTO projection returning header, subject entries, homeroom remarks, and attendance metric counts (`PRESENT`, `ABSENT`, `SICK`, `LATE`, `EXCUSED`) aggregated directly from `acd.attendance_records` (`OQ-025`).
3. Map Minimal API endpoints in `ReportCardsEndpoints.cs` implementing `IEndpoint` discovery.
4. `dotnet build src/backend/ElsasaSis.sln` succeeds with exit code `0`.

Implementation Notes:
- Expanded `IReportCardRepository` and `ReportCardRepository`:
  - Added `InsertBatchAsync` for atomic transactional batch report card and entry persistence.
  - Added `GetExistingCardStudentIdsAsync` for idempotent pre-generation duplicate detection.
  - Added `GetStudentNisnsAsync` for snapshotting NISN attributes from `std.students`.
  - Added `ComputeDraftCardEntriesAsync` calculating arithmetic final grade per subject stage from `acd.assignment_scores` ($\text{ROUND}(\text{AVG}(\text{score}), 2)$ with `0.00` fallback) and active teaching assignments from `tch.teacher_assignments`.
  - Added `HasTeacherAssignmentAsync` to verify active teacher assignments for subject note submissions.
- Expanded `AcdErrors.ReportCards` with `StudyGroupNotFound`, `OperationalPeriodNotFound`, and `NotAssignedHomeroomTeacher`.
- Implemented 5 Write Slices in `Features/ReportCards/`:
  - `GenerateDraftReportCards`: Validates operational period and study group, queries placed cohort, skips existing cards, computes arithmetic grades, snapshots entries, and batch inserts draft cards.
  - `ProvideSubjectReportCardNotes`: Enforces `DRAFT` status, validates active teaching assignment in `tch.teacher_assignments`, and updates teacher notes snapshot.
  - `WriteHomeroomRemarks`: Enforces `DRAFT` status and authorized homeroom teacher / admin identity, updating `general_remarks`.
  - `FinalizeHomeroomReportCards`: Enforces `DRAFT` status and homeroom teacher / admin authorization, locks card to `FINALIZED`, records `finalized_at`, and dispatches `ReportCardFinalizedDomainEvent`.
  - `OverrideReportCardStatus`: Authorized strictly for `SCHOOL_ADMIN`, enforces non-empty override reason, resets card to `DRAFT`, records `override_reason`, and clears `finalized_at`.
- Implemented 3 Read Slices with direct Dapper SQL projections:
  - `GetHomeroomReportCards` (`GET /api/acd/report-cards/homeroom`): Direct projection computing completion indicators and averages.
  - `GetReportCardsOverviewHandler` (`GET /api/acd/report-cards/overview`): Principal overview computing school-wide grade averages, draft/finalized counters, and study group breakdowns.
  - `GetReportCardById` (`GET /api/acd/report-cards/{id}`): Composite projection combining header metadata, subject entries, homeroom remarks, and attendance metric counts (`PRESENT`, `ABSENT`, `SICK`, `LATE`, `EXCUSED`) from `acd.attendance_records`.
- Implemented `ReportCardsEndpoints` registering all 8 Minimal API endpoints under `/api/acd/report-cards` conforming to `IEndpoint` discovery with permissions, validation filters, and RFC 7807 problem details responses.
- Authored comprehensive test suite in `ElsasaSis.UnitTests/Academic/ReportCardsFeatureTests.cs` verifying validators, handlers, CQRS dependency discipline, and DI registration.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded (0 warnings, 0 errors).
  - All 46 report card unit tests passed; all 243 `Acd`/`Academic` unit tests passed; all 578 solution unit tests passed.
  - All 17 architecture tests passed (verifying zero CQRS write-handler dependencies on `ISqlConnectionFactory`/Dapper).
  - All 36 integration tests passed.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/AcdErrors.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Domain/ReportCards/IReportCardRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/FinalizeHomeroomReportCards/FinalizeHomeroomReportCardsHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GenerateDraftReportCards/GenerateDraftReportCardsHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GenerateDraftReportCards/GenerateDraftReportCardsRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GenerateDraftReportCards/GenerateDraftReportCardsResponse.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GenerateDraftReportCards/GenerateDraftReportCardsValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetHomeroomReportCards/GetHomeroomReportCardsHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetHomeroomReportCards/HomeroomReportCardDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetReportCardById/GetReportCardByIdHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/GetReportCardById/ReportCardDetailDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/OverrideReportCardStatus/OverrideReportCardStatusHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/OverrideReportCardStatus/OverrideReportCardStatusRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/OverrideReportCardStatus/OverrideReportCardStatusValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/ProvideSubjectReportCardNotes/ProvideSubjectNotesRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/ProvideSubjectReportCardNotes/ProvideSubjectNotesValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/ProvideSubjectReportCardNotes/ProvideSubjectReportCardNotesHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/ReportCardsEndpoints.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/ReviewAllReportCards/GetReportCardsOverviewHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/ReviewAllReportCards/ReportCardsOverviewDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/WriteHomeroomRemarks/WriteHomeroomRemarksHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/WriteHomeroomRemarks/WriteHomeroomRemarksRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Features/ReportCards/WriteHomeroomRemarks/WriteHomeroomRemarksValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Acd/Infrastructure/Persistence/ReportCardRepository.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Academic/ReportCardsFeatureTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P2-S06

Title: Implement Student Module Extensions — Billing Contacts Lookup, Placement Completion Subscriber & Scope Query

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Expand `ElsasaSis.Modules.Std` with the guardian billing contacts lookup Read Slice (`UC-FIN-10`), the `ReportCardFinalizedEventHandler` subscriber, and implement `GetActiveStudentsByScopeAsync` on `StudentContract`.

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Implement Read Slice `LookupStudentBillingContacts` in `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/BillingLookup/` (`GET /api/std/billing-lookup?search=` — `UC-FIN-10`):
   - Direct Dapper query joining `std.students`, `std.guardians`, and `std.student_placements`.
   - Returns student ID, NIS, Full Name, Grade, Study Group, Primary Guardian Name, Phone Number, Relationship, and preformatted WhatsApp direct URL (`https://wa.me/...`).
   - Accessible to `FINANCE_OFFICER` and `SCHOOL_ADMIN`.
2. Implement Event Subscriber `ReportCardFinalizedEventHandler` in `src/backend/src/Modules/ElsasaSis.Modules.Std/Subscribers/`:
   - Implements `INotificationHandler<ReportCardFinalizedDomainEvent>`.
   - Executes parameterized UPDATE: `UPDATE std.student_placements SET is_academically_completed = 1, updated_at = @utcNow WHERE student_id = @studentId AND operational_period_id = @operationalPeriodId`.
3. Implement `GetActiveStudentsByScopeAsync` in `src/backend/src/Modules/ElsasaSis.Modules.Std/Infrastructure/Contracts/StudentContract.cs`:
   - Queries active placed students matching study group, school level, or grade.
   - Returns `IReadOnlyList<StudentPlacementScopeDto>`.
4. Unit tests in `ElsasaSis.UnitTests/Student/StudentExtensionsTests.cs` pass.

Implementation Notes:
- Implemented Read Slice `LookupStudentBillingContacts` in `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/BillingLookup/`:
  - `StudentBillingContactDto.cs`: Student ID, NIS, Full Name, School Level, Grade, Study Group ID & Name, Primary Guardian Name, Phone Number, Relationship, and preformatted WhatsApp URL.
  - `LookupStudentBillingContactsQuery.cs`: Search query record (`string? Search`).
  - `LookupStudentBillingContactsHandler.cs` (`GetStudentBillingContactsHandler`): Direct Dapper projection joining `std.students`, `std.student_placements` (latest placement per student), and `std.student_guardians` (primary guardian prioritising Father > Mother > Guardian). Includes `FormatWhatsAppUrl` converting Indonesian numbers (e.g. `08...`, `+62...`, `62...`) to `https://wa.me/62...`.
  - `LookupStudentBillingContactsEndpoint.cs`: Minimal API `GET /api/std/billing-lookup?search=` authorized under `Policies.Students.BillingLookup` (`FINANCE_OFFICER` and `SCHOOL_ADMIN`).
- Implemented Event Subscriber `ReportCardFinalizedEventHandler` in `src/backend/src/Modules/ElsasaSis.Modules.Std/Subscribers/`:
  - Implements `INotificationHandler<ReportCardFinalizedDomainEvent>`.
  - Delegated academic completion persistence via `IStudentPlacementRepository.MarkAcademicallyCompletedAsync` to ensure clean DDD architecture with zero direct DB/Dapper dependencies in handlers.
  - Executes parameterized UPDATE on `std.student_placements` setting `is_academically_completed = 1` and `updated_at = @UtcNow` matching student and academic year resolved from operational period (or direct match fallback).
- Updated `StudentContract.cs`:
  - Implemented `GetActiveStudentsByScopeAsync` using CTE deduplication to query active placed students matching optional `studyGroupId`, `schoolLevel`, and `grade` returning `IReadOnlyList<StudentPlacementScopeDto>`.
- Configured compile-time authorization policy `Policies.Students.BillingLookup` across `Modules.Std/Permissions.cs`, `Modules.Auth/Permissions.cs`, and `ElsasaSis.Api/Program.cs`.
- Authored unit test suite in `src/backend/tests/ElsasaSis.UnitTests/Student/StudentExtensionsTests.cs` covering WhatsApp URL formatting variations, Dapper query mapping and search parameters, event subscriber placement completion updates, and `StudentContract.GetActiveStudentsByScopeAsync` scope filtering.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded (0 errors).
  - All 529 Unit Tests in `ElsasaSis.UnitTests` passed.
  - All 17 Architecture Tests in `ElsasaSis.ArchitectureTests` passed.
  - All 36 Integration Tests in `ElsasaSis.IntegrationTests` passed.

Changed Files:
- `src/backend/src/ElsasaSis.Api/Program.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Auth/Permissions.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Domain/IStudentPlacementRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/BillingLookup/LookupStudentBillingContactsEndpoint.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/BillingLookup/LookupStudentBillingContactsHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/BillingLookup/LookupStudentBillingContactsQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Features/BillingLookup/StudentBillingContactDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Infrastructure/Contracts/StudentContract.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Infrastructure/Persistence/StudentPlacementRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Permissions.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Std/Subscribers/ReportCardFinalizedEventHandler.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Std/StudentFeaturesTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Student/StudentExtensionsTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

## P3 - Student Development & Extracurriculars Backend

Implementation Status: IMPLEMENTED  
Review Status: GO

### P3-S07

Title: Implement Development Domain Models, Errors & Repositories (`BehaviourNote`, `DisciplineRecord`, `ExtracurricularActivity`, `Achievement`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Create pure Domain aggregate roots, domain errors, and Dapper repositories for behaviour notes, discipline records, extracurricular activities (with child participations), and achievements in `ElsasaSis.Modules.Dev`.

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Create Domain Entities in `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/`:
   - `BehaviourNote` Aggregate Root:
     - Properties: `Id`, `StudentId`, `StudentName`, `StudentNis`, `AcademicYearId`, `AcademicYearName`, `BehaviourType` (`POSITIVE`, `NEGATIVE`), `ObservedDate`, `Description`, `RecorderId`, `RecorderName`, `CreatedAt`, `UpdatedAt`.
     - Factory method `Create(...)` enforcing valid behaviour type and non-empty description.
   - `DisciplineRecord` Aggregate Root:
     - Properties: `Id`, `StudentId`, `StudentName`, `StudentNis`, `AcademicYearId`, `AcademicYearName`, `IncidentDate`, `ViolationType`, `Description`, `Sanction` (`VERBAL_WARNING`, `WRITTEN_WARNING`, `PARENT_MEETING`, `SUSPENSION`), `RecorderId`, `RecorderName`, `CreatedAt`, `UpdatedAt`.
     - Factory method `CreateIncident(...)` enforcing sanctions allowed for role:
       - Teacher/Admin: `VERBAL_WARNING`, `WRITTEN_WARNING`.
       - Principal: `PARENT_MEETING`, `SUSPENSION`. Raises `SanctionImposedDomainEvent`.
   - `ExtracurricularActivity` Aggregate Root (with child `ExtracurricularParticipation`):
     - Properties: `Id`, `Name`, `Description`, `SupervisorId`, `SupervisorName`, `Status` (`ACTIVE`, `INACTIVE`), `CreatedAt`, `UpdatedAt`.
     - Child entity `ExtracurricularParticipation`: `Id`, `ActivityId`, `StudentId`, `Status` (`ACTIVE`, `COMPLETED`, `WITHDRAWN`), `JoinedDate`, `LastSessionLog`, `LastSessionDate`.
     - Methods:
       - `EnrollStudent(Guid studentId, ...)`: Prevents duplicate active enrollment.
       - `WithdrawStudent(Guid participationId, DateTime utcNow)`: Transitions status to `WITHDRAWN`.
       - `RecordSessionLog(Guid participationId, Guid supervisorId, DateOnly sessionDate, string log, DateTime utcNow)`: Verifies caller is assigned supervisor, updates `last_session_log` and `last_session_date`.
   - `Achievement` Aggregate Root:
     - Properties: `Id`, `StudentId`, `StudentName`, `StudentNis`, `AcademicYearId`, `AcademicYearName`, `AchievementType` (`SPORTS`, `ARTS`, `SCIENCE`, `COMMUNITY`, `OTHER`), `EventName`, `RecognitionLevel` (`SCHOOL`, `CITY`, `PROVINCE`, `NATIONAL`), `AchievementDate`, `Description`, `RecorderId`, `RecorderName`, `CreatedAt`, `UpdatedAt`.
     - Factory method `Create(...)` enforcing valid recognition level and achievement type.
   - `DevErrors.cs`: Domain error codes.
2. Create Repositories in `src/backend/src/Modules/ElsasaSis.Modules.Dev/Infrastructure/Persistence/`:
   - `IBehaviourNoteRepository`, `IDisciplineRecordRepository`, `IExtracurricularActivityRepository`, `IAchievementRepository`.
   - Dapper implementations with parameterized queries. Register in `DependencyInjection.cs`.
3. Unit tests in `ElsasaSis.UnitTests/Development/DevelopmentDomainTests.cs` pass.

Implementation Notes:
- Created domain constants and value sets in `ElsasaSis.Modules.Dev.Domain`: `BehaviourType` (`POSITIVE`, `NEGATIVE`), `DisciplineSanction` (`VERBAL_WARNING`, `WRITTEN_WARNING`, `PARENT_MEETING`, `SUSPENSION`), `ExtracurricularActivityStatus` (`ACTIVE`, `INACTIVE`), `ExtracurricularParticipationStatus` (`ACTIVE`, `COMPLETED`, `WITHDRAWN`), `AchievementType` (`SPORTS`, `ARTS`, `SCIENCE`, `COMMUNITY`, `OTHER`), and `RecognitionLevel` (`SCHOOL`, `CITY`, `PROVINCE`, `NATIONAL`).
- Expanded `DevErrors.cs` with descriptive domain error definitions covering validation, conflict, not-found, and role authorization errors for behaviour notes, discipline records, extracurricular activities, and achievements.
- Created Aggregate Roots and Entities in `ElsasaSis.Modules.Dev.Domain`:
  - `BehaviourNote` Aggregate Root: private constructor, `Create(...)` factory with invariant enforcement (type, non-empty description <= 1000 chars, student and recorder details), and `Rehydrate(...)`.
  - `DisciplineRecord` Aggregate Root: role-aware `CreateIncident(...)` enforcing sanctions allowed for teacher/admin vs. principal (`PARENT_MEETING`, `SUSPENSION` raises `SanctionImposedDomainEvent`), length invariants (violation type <= 50, description <= 1000), and `Rehydrate(...)`.
  - `ExtracurricularActivity` Aggregate Root and `ExtracurricularParticipation` Entity: `Create(...)`, `UpdateDetails(...)`, `EnrollStudent(...)` preventing duplicate active enrollment within same academic year and enforcing active status, `WithdrawStudent(...)` with terminal status guard, `RecordSessionLog(...)` enforcing assigned supervisor identity and active participation status, and `Rehydrate(...)`.
  - `Achievement` Aggregate Root: `Create(...)` enforcing valid types, levels, event name <= 200, description <= 500, and `Rehydrate(...)`.
- Created repository interfaces in `Domain/`: `IBehaviourNoteRepository`, `IDisciplineRecordRepository`, `IExtracurricularActivityRepository`, and `IAchievementRepository`.
- Created Dapper repository implementations in `Infrastructure/Persistence/`: `BehaviourNoteRepository`, `DisciplineRecordRepository`, `ExtracurricularActivityRepository`, and `AchievementRepository` with parameterized SQL queries and UTC datetime handling.
- Registered all four repositories as scoped dependencies in `DependencyInjection.cs`.
- Authored comprehensive unit tests in `ElsasaSis.UnitTests/Development/DevelopmentDomainTests.cs` (20 tests covering all aggregates, state transitions, validation failures, role sanctions, event raising, and rehydration).
- Verified with `dotnet build src/backend/ElsasaSis.sln` (0 errors), `dotnet test src/backend/tests/ElsasaSis.UnitTests` (489 passed, 0 failed), and architecture tests (`ModuleBoundaryTests` 9/9 passed).

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/Achievement.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/AchievementType.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/BehaviourNote.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/BehaviourType.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/DevErrors.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/DisciplineRecord.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/DisciplineSanction.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/ExtracurricularActivity.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/ExtracurricularActivityStatus.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/ExtracurricularParticipation.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/ExtracurricularParticipationStatus.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/IAchievementRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/IBehaviourNoteRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/IDisciplineRecordRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/IExtracurricularActivityRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/RecognitionLevel.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Infrastructure/Persistence/AchievementRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Infrastructure/Persistence/BehaviourNoteRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Infrastructure/Persistence/DisciplineRecordRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Infrastructure/Persistence/ExtracurricularActivityRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/DependencyInjection.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Development/DevelopmentDomainTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P3-S08

Title: Implement Pastoral Care, Discipline, Achievements & 360° Dossier Vertical Slices

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement vertical slices for behaviour notes, classroom discipline, principal escalated discipline, achievements, and the unified 360° student development dossier query in `ElsasaSis.Modules.Dev`.

Depends On: P3-S07

Repository: src/backend

Completion Criteria:

1. Implement Vertical Slices in `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/`:
   - `RecordBehaviourNote` (`POST /api/dev/behaviour` — `UC-TCH-06`):
     - Populates recorder identity (`RecorderId = ICurrentUser.UserId`, `RecorderName = ICurrentUser.FullName`) from JWT.
   - `RecordDisciplineIncident` (`POST /api/dev/discipline` — `UC-TCH-07`, `UC-ADM-22`):
     - Accessible to `TEACHER` and `SCHOOL_ADMIN`. Sanctions restricted to `VERBAL_WARNING` and `WRITTEN_WARNING`.
   - `RecordEscalatedSanction` (`POST /api/dev/discipline/escalated` — `UC-PRC-03`):
     - Accessible strictly to `PRINCIPAL`. Sanctions restricted to `PARENT_MEETING` and `SUSPENSION`. Raises `SanctionImposedDomainEvent`.
   - `RecordAchievement` (`POST /api/dev/achievements` — `UC-TCH-08`, `UC-ADM-23`):
     - Unified endpoint for teacher and admin achievement logging.
   - `GetStudentDossier` (`GET /api/dev/students/{studentId}/dossier` — `UC-ADM-24`, `UC-HRM-04`, `UC-PRC-04`):
     - Optimized Read Slice executing 4 sub-queries inside `dev` schema: behaviour notes, discipline records, extracurricular participations, and achievements.
     - Combines results into a single chronological student development profile DTO.
   - `GetDevelopmentOverview` (`GET /api/dev/overview`):
     - Principal and administrator dashboard summary of recent incidents, awards, and activity participation metrics.
2. Map endpoints in `DevelopmentEndpoints.cs` implementing `IEndpoint` discovery.
3. `dotnet build src/backend/ElsasaSis.sln` succeeds with exit code `0`.

Implementation Notes:
- Implemented vertical slices for behaviour, discipline, achievements, student dossier, and overview in `ElsasaSis.Modules.Dev.Features`:
  - `Features/Behaviour`: `BehaviourNoteDto`, `RecordBehaviourNoteRequest`, `RecordBehaviourNoteValidator` (FluentValidation for student details, type, date, description <= 1000), and `RecordBehaviourNoteHandler` populating recorder from `ICurrentUser` and inserting via `IBehaviourNoteRepository`.
  - `Features/Discipline`: `DisciplineRecordDto`, `RecordDisciplineIncidentRequest`, `RecordDisciplineIncidentValidator` (enforcing teacher-allowed sanctions `VERBAL_WARNING`, `WRITTEN_WARNING`), `RecordDisciplineIncidentHandler`, `RecordEscalatedSanctionRequest`, `RecordEscalatedSanctionValidator` (enforcing principal-allowed sanctions `PARENT_MEETING`, `SUSPENSION`), and `RecordEscalatedSanctionHandler` saving escalated incident and dispatching `SanctionImposedDomainEvent` via `IDomainEventDispatcher`.
  - `Features/Achievements`: `AchievementDto`, `RecordAchievementRequest`, `RecordAchievementValidator` (enforcing valid types and recognition levels), and `RecordAchievementHandler` saving achievement via `IAchievementRepository`.
  - `Features/Dossier`: `ExtracurricularParticipationDto`, `DossierTimelineItemDto`, `StudentDossierDto`, and CQRS Read Slice `GetStudentDossierHandler` issuing 4 direct Dapper queries against `dev.behaviour_notes`, `dev.discipline_records`, `dev.extracurricular_participations`, and `dev.achievements` and projecting into a unified chronologically sorted timeline.
  - `Features/Overview`: `DevelopmentOverviewDto` and CQRS Read Slice `GetDevelopmentOverviewHandler` executing metric counts and fetching top 5 recent behaviour notes, discipline incidents, and achievements.
- Mapped all Minimal API endpoints in `Features/DevelopmentEndpoints.cs` implementing `IEndpoint` discovery with compile-time permission authorization and `ValidationFilter` filters.
- Authored comprehensive unit tests in `ElsasaSis.UnitTests/Development/DevelopmentFeatureTests.cs` (15 tests covering validators, handlers, role restrictions, domain event dispatching, and DI registration).
- Verification: `dotnet build src/backend/ElsasaSis.sln` succeeded with 0 warnings and 0 errors; all 77 unit tests in `ElsasaSis.UnitTests` for Development passed; all 17 architecture boundary tests in `ElsasaSis.ArchitectureTests` passed.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Behaviour/BehaviourNoteDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Behaviour/RecordBehaviourNoteRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Behaviour/RecordBehaviourNoteValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Behaviour/RecordBehaviourNoteHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Discipline/DisciplineRecordDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Discipline/RecordDisciplineIncidentRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Discipline/RecordDisciplineIncidentValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Discipline/RecordDisciplineIncidentHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Discipline/RecordEscalatedSanctionRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Discipline/RecordEscalatedSanctionValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Discipline/RecordEscalatedSanctionHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Achievements/AchievementDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Achievements/RecordAchievementRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Achievements/RecordAchievementValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Achievements/RecordAchievementHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Dossier/ExtracurricularParticipationDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Dossier/DossierTimelineItemDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Dossier/StudentDossierDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Dossier/GetStudentDossierHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Overview/DevelopmentOverviewDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Overview/GetDevelopmentOverviewHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/DevelopmentEndpoints.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Development/DevelopmentFeatureTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P3-S09

Title: Implement Extracurricular Activities Management & Session Logging Vertical Slices

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement vertical slices for extracurricular club catalog management, participant enrollment, and meeting session logging in `ElsasaSis.Modules.Dev`.

Depends On: P3-S07

Repository: src/backend

Completion Criteria:

1. Implement Vertical Slices in `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/`:
   - `CreateExtracurricularActivity` (`POST /api/dev/extracurriculars` — `UC-ADM-25`):
     - Validates supervisor exists via `ITeachingContract.GetTeacherByIdAsync(supervisorId)`.
     - Stores activity with status `ACTIVE`.
   - `GetExtracurricularActivities` (`GET /api/dev/extracurriculars`):
     - Returns club catalog with supervisor names and active participant counts.
   - `UpdateExtracurricularActivity` (`PUT /api/dev/extracurriculars/{id}`):
     - Allows updating name, description, status, and supervisor.
   - `EnrollExtracurricularParticipant` (`POST /api/dev/extracurriculars/{activityId}/participants` — `UC-ADM-25`):
     - Enrolls student; rejects with 409 Conflict if student already actively enrolled in the activity for that academic year.
   - `GetExtracurricularParticipants` (`GET /api/dev/extracurriculars/{activityId}/participants`):
     - Lists enrolled students, join dates, session logs, and statuses.
   - `WithdrawExtracurricularParticipant` (`POST /api/dev/extracurriculars/{activityId}/participants/{participationId}/withdraw`):
     - Marks participation as `WITHDRAWN`.
   - `RecordExtracurricularSessionLog` (`POST /api/dev/extracurriculars/{activityId}/participants/{participationId}/log` — `UC-TCH-09`):
     - Verifies caller is the assigned club supervisor.
     - Updates `last_session_log` and `last_session_date` on target participation.
2. Map endpoints in `ExtracurricularEndpoints.cs` implementing `IEndpoint` discovery.
3. `dotnet build src/backend/ElsasaSis.sln` succeeds with exit code `0`.

Implementation Notes:
- Implemented Extracurricular DTOs in `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/`:
  - `ExtracurricularActivityDto`: includes activity metadata and `ActiveParticipantCount`.
  - `ExtracurricularParticipationDto`: includes participation lifecycle details, student and academic year snapshots, and session log information.
- Implemented Vertical Slices and RFC 7807 FluentValidation validators:
  - `CreateExtracurricularActivity`: checks duplicate name, verifies supervisor existence via `ITeachingContract.GetTeacherByIdAsync`, creates activity with `ACTIVE` status.
  - `GetExtracurricularActivities`: lists activities with active member counts and supervisor details, filterable by status and supervisor.
  - `UpdateExtracurricularActivity`: updates name, description, status (`ACTIVE`/`INACTIVE`), and validates supervisor existence on change.
  - `EnrollExtracurricularParticipant`: enrolls placed student, rejects duplicate active enrollments per academic year (409 Conflict), resolves student snapshot and academic year snapshot via contracts if omitted.
  - `GetExtracurricularParticipants`: lists club enrollments by activity ID and optional academic year ID.
  - `WithdrawExtracurricularParticipant`: transitions participation to terminal state `WITHDRAWN` with timestamp.
  - `CompleteExtracurricularParticipant`: transitions participation to terminal state `COMPLETED` with timestamp.
  - `RecordExtracurricularSessionLog`: enforces caller identity matches assigned club supervisor (`ICurrentUser.TeacherId` / `ICurrentUser.UserId`), verifies active participation status, and updates `last_session_log` and `last_session_date`.
- Created `ExtracurricularEndpoints` implementing `IEndpoint` with discovery registration, permission guards (`Permissions.Development.Extracurricular.Manage` for admin/catalog actions, `Permissions.Development.Extracurricular.Log` for supervisor session logging, composite check for read operations), and endpoint validation filters.
- Authored 20 unit tests in `src/backend/tests/ElsasaSis.UnitTests/Development/ExtracurricularFeatureTests.cs` verifying all validators and handlers for positive, duplicate, unauthorized, and edge cases.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded (0 warnings, 0 errors).
  - All 17 architecture tests in `ElsasaSis.ArchitectureTests` passed.
  - All 578 unit tests in `ElsasaSis.UnitTests` passed.
  - All 36 integration tests in `ElsasaSis.IntegrationTests` passed.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Domain/DevErrors.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/ExtracurricularActivityDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/ExtracurricularParticipationDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/CreateExtracurricularActivity/CreateExtracurricularActivityRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/CreateExtracurricularActivity/CreateExtracurricularActivityValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/CreateExtracurricularActivity/CreateExtracurricularActivityHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/GetExtracurricularActivities/GetExtracurricularActivitiesQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/GetExtracurricularActivities/GetExtracurricularActivitiesHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/UpdateExtracurricularActivity/UpdateExtracurricularActivityRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/UpdateExtracurricularActivity/UpdateExtracurricularActivityValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/UpdateExtracurricularActivity/UpdateExtracurricularActivityHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/EnrollExtracurricularParticipant/EnrollExtracurricularParticipantRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/EnrollExtracurricularParticipant/EnrollExtracurricularParticipantValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/EnrollExtracurricularParticipant/EnrollExtracurricularParticipantHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/GetExtracurricularParticipants/GetExtracurricularParticipantsQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/GetExtracurricularParticipants/GetExtracurricularParticipantsHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/WithdrawExtracurricularParticipant/WithdrawExtracurricularParticipantHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/CompleteExtracurricularParticipant/CompleteExtracurricularParticipantHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/RecordExtracurricularSessionLog/RecordExtracurricularSessionLogRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/RecordExtracurricularSessionLog/RecordExtracurricularSessionLogValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/RecordExtracurricularSessionLog/RecordExtracurricularSessionLogHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Extracurricular/ExtracurricularEndpoints.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Development/ExtracurricularFeatureTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

## P4 - Fee Policies, Billing & Cashier Counter Backend

Implementation Status: IMPLEMENTED  
Review Status: GO

### P4-S10

Title: Implement Finance Domain Models, Errors, Receipt Sequence Counter & Repositories

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Create pure Domain aggregate roots (`ChargeType`, `StudentDiscount`, `StudentCharge`, `PaymentReceipt`), receipt sequence counter logic, domain errors, and Dapper repositories in `ElsasaSis.Modules.Fin`.

Depends On: P1-S01, P1-S02

Repository: src/backend

Completion Criteria:

1. Create Domain Entities in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/`:
   - `ChargeType` Aggregate Root:
     - Properties: `Id`, `AcademicYearId`, `AcademicYearName`, `Name`, `Frequency` (`MONTHLY`, `SEMESTER`, `YEARLY`, `ONE_TIME`), `Amount`, `SchoolLevel`, `Grade`, `StudyGroupId`, `StudyGroupName`, `Status` (`DRAFT`, `ACTIVE`), `CreatedAt`, `UpdatedAt`.
     - Methods: `Activate()`, `Update(...)` (enforces status == `DRAFT`; rejects if `ACTIVE`).
   - `StudentDiscount` Aggregate Root:
     - Properties: `Id`, `AcademicYearId`, `AcademicYearName`, `ChargeTypeId`, `ChargeTypeName`, `StudentId`, `StudentName`, `StudentNis`, `DiscountType` (`FIXED_AMOUNT`, `PERCENTAGE`), `DiscountValue`, `Reason`, `IsActive`, `CreatedAt`, `UpdatedAt`.
     - Methods: `Revoke()`, `ComputeDiscount(decimal grossAmount)`.
   - `StudentCharge` Aggregate Root:
     - Properties: `Id`, `AcademicYearId`, `AcademicYearName`, `OperationalPeriodId`, `OperationalPeriodName`, `BillingMonth`, `ChargeTypeId`, `ChargeTypeName`, `ChargeFrequency`, `StudentId`, `StudentName`, `StudentNis`, `SchoolLevel`, `Grade`, `StudyGroupId`, `StudyGroupName`, `Description`, `UnitAmount`, `Quantity`, `GrossAmount`, `DiscountAmount`, `DiscountReason`, `NetAmount`, `PaidAmount`, `OutstandingAmount`, `Status` (`UNPAID`, `PARTIALLY_PAID`, `PAID`, `CANCELLED`), `DueDate`, `CancellationReason`, `CancelledAt`, `CreatedAt`, `UpdatedAt`.
     - Methods:
       - `ApplyPayment(decimal amount)`: Increases `PaidAmount`, recalculates `OutstandingAmount`, updates status (`UNPAID` $\rightarrow$ `PARTIALLY_PAID` $\rightarrow$ `PAID`).
       - `Cancel(string reason, DateTime utcNow)`: Enforces `PaidAmount == 0`, non-empty reason, sets status `CANCELLED`.
   - `PaymentReceipt` Aggregate Root (with child `PaymentAllocation`):
     - Properties: `Id`, `ReceiptNumber` (`KWT-YYYYMM-NNNN`), `AcademicYearId`, `AcademicYearName`, `StudentId`, `StudentName`, `StudentNis`, `PaymentDate`, `PaymentMethod` (`CASH`, `BANK_TRANSFER`), `BankReference`, `TotalAmount`, `AllocatedAmount`, `UnallocatedAmount`, `ReceivedById`, `ReceivedByName`, `Notes`, `CreatedAt`, `UpdatedAt`.
     - Methods: `AllocateCharge(Guid chargeId, decimal amount, ...)`: Enforces allocated sum $\le$ total amount; calculates `UnallocatedAmount` surplus.
   - `FinErrors.cs`: Domain error codes.
2. Implement Concurrency-Safe Receipt Sequence Generator:
   - In `Infrastructure/Persistence/ReceiptSequenceGenerator.cs`:
     - Executes atomic SQL update on `fin.receipt_sequences`:
       ```sql
       UPDATE fin.receipt_sequences
          SET next_seq = next_seq + 1
       OUTPUT inserted.next_seq
        WHERE month = @month;
       ```
     - If row does not exist, inserts `(month, 2)` and returns sequence `1`.
     - Formats receipt number as `KWT-YYYYMM-NNNN` (e.g. `KWT-202611-0001`).
3. Create Repositories in `Infrastructure/Persistence/`:
   - `IChargeTypeRepository`, `IStudentDiscountRepository`, `IStudentChargeRepository`, `IPaymentReceiptRepository`.
   - Dapper implementations with parameterized queries. Register in `DependencyInjection.cs`.
4. Unit tests in `ElsasaSis.UnitTests/Finance/FinanceDomainTests.cs` pass.

Implementation Notes:
- Created pure Domain Aggregate Roots and Entities in `ElsasaSis.Modules.Fin.Domain.Model`:
  - `ChargeType`: aggregate root enforcing draft update rules, activation locking, validation rules, and rehydration.
  - `StudentDiscount`: aggregate root with percentage and fixed amount discount computation algorithms, revocation guard, and rehydration.
  - `StudentCharge`: aggregate root calculating gross, discount, net, paid, and outstanding balances; supports partial and full payments with status derivation; supports cancellation with non-empty reason only when unpaid.
  - `PaymentReceipt` and `PaymentAllocation`: receipt aggregate root with child allocations; validates total vs. allocated amounts, tracks unallocated deposit balances, raises `PaymentReceiptPostedDomainEvent`, and rehydrates child allocations.
  - `FinanceConstants`: constant classes for frequencies, statuses, discount types, and payment methods.
- Expanded `FinErrors.cs` with typed error codes for charge types, discounts, charges, receipts, and deposits.
- Created Domain repository interfaces in `ElsasaSis.Modules.Fin.Domain`: `IChargeTypeRepository`, `IStudentDiscountRepository`, `IStudentChargeRepository`, `IPaymentReceiptRepository`, `IReceiptSequenceGenerator` (pure interfaces, zero `System.Data` dependencies).
- Implemented Dapper repositories and concurrency-safe sequence counter in `ElsasaSis.Modules.Fin.Infrastructure.Persistence`:
  - `ReceiptSequenceGenerator`: atomic SQL increment and safe fallback insertion on `fin.receipt_sequences`, producing formatted receipt number `KWT-YYYYMM-NNNN`.
  - `ChargeTypeRepository`: parameterized queries with multi-scope lookup and unique name checks.
  - `StudentDiscountRepository`: parameterized queries supporting student active discount resolution and unique constraints.
  - `StudentChargeRepository`: parameterized queries, batch insertion, and unpaid/partially paid charge retrieval.
  - `PaymentReceiptRepository`: multi-grid queries loading receipt aggregate root alongside child `fin.payment_allocations`.
- Registered all repositories and `ReceiptSequenceGenerator` in `DependencyInjection.cs`.
- Authored comprehensive domain unit tests in `ElsasaSis.UnitTests/Finance/FinanceDomainTests.cs` covering all aggregate roots, domain events, discount math, payments, cancellations, and validations.
- Verification: all 25 Finance domain tests passed; entire test suite (529 tests) passed; NetArchTest architecture boundary and layer tests passed.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/DependencyInjection.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/FinErrors.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/IChargeTypeRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/IPaymentReceiptRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/IReceiptSequenceGenerator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/IStudentChargeRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/IStudentDiscountRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/Model/ChargeType.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/Model/FinanceConstants.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/Model/PaymentAllocation.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/Model/PaymentReceipt.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/Model/StudentCharge.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/Model/StudentDiscount.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Persistence/ChargeTypeRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Persistence/PaymentReceiptRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Persistence/ReceiptSequenceGenerator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Persistence/StudentChargeRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Persistence/StudentDiscountRepository.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Finance/FinanceDomainTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Std/StudentFeaturesTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S11

Title: Implement Fee Policies & Student Discounts Vertical Slices (`Features/FeePolicy/`, `Features/Discounts/`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement vertical slices for fee charge type catalog management, activation locking, and student standing discount / scholarship assignments in `ElsasaSis.Modules.Fin`.

Depends On: P4-S10

Repository: src/backend

Completion Criteria:

1. Implement Vertical Slices in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/`:
   - `CreateChargeType` (`POST /api/fin/charge-types` — `UC-FIN-01`): Creates fee with status `DRAFT`.
   - `GetChargeTypes` (`GET /api/fin/charge-types`): Lists charge types filtered by academic year, frequency, and status.
   - `UpdateChargeType` (`PUT /api/fin/charge-types/{id}`): Updates fee definition; returns 409 Conflict if fee is already `ACTIVE` (`OQ-011`).
   - `ActivateChargeType` (`POST /api/fin/charge-types/{id}/activate` — `UC-FIN-01`): Transitions status `DRAFT -> ACTIVE`.
2. Implement Vertical Slices in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Discounts/`:
   - `CreateStudentDiscount` (`POST /api/fin/discounts` — `UC-FIN-02`): Assigns standing fixed amount or percentage discount to student for specified charge type and academic year.
   - `GetStudentDiscounts` (`GET /api/fin/discounts`): Lists student discounts with filters.
   - `RevokeStudentDiscount` (`POST /api/fin/discounts/{id}/revoke` — `UC-FIN-02`): Deactivates discount (`is_active = 0`).
3. Map endpoints in `FeePolicyEndpoints.cs` implementing `IEndpoint` discovery.
4. `dotnet build src/backend/ElsasaSis.sln` succeeds with exit code `0`.

Implementation Notes:
- Implemented Fee Policy vertical slices in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/`:
  - `ChargeTypeDto`: API response projection from domain `ChargeType` aggregate.
  - `CreateChargeType`: Request, FluentValidation validator, and handler creating charge type in `DRAFT` status; resolves academic year from active year if omitted; resolves study group name via `IStudentContract`; prevents duplicate names in the same academic year.
  - `GetChargeTypes`: Query and handler returning charge types filtered optionally by `academicYearId`, `frequency`, and `status`.
  - `UpdateChargeType`: Request, FluentValidation validator, and handler updating fee definition; enforces draft status and rejects with 409 Conflict (`FinErrors.ChargeTypeCannotBeModified`) if status is `ACTIVE` (`OQ-011`); enforces unique name within academic year.
  - `ActivateChargeType`: Handler transitioning charge type status from `DRAFT` to `ACTIVE`.
- Implemented Student Discounts vertical slices in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Discounts/`:
  - `StudentDiscountDto`: API response projection from domain `StudentDiscount` aggregate.
  - `CreateStudentDiscount`: Request, FluentValidation validator, and handler creating standing discounts; validates charge type exists; resolves student details via `IStudentContract.GetActiveStudentsByScopeAsync(...)`; enforces unique active discount per student, charge type, and academic year.
  - `GetStudentDiscounts`: Query and handler returning discounts filtered optionally by `academicYearId`, `studentId`, `chargeTypeId`, and `isActive`.
  - `RevokeStudentDiscount`: Handler deactivating discount (`is_active = 0`); rejects already revoked discounts with conflict.
- Created `FeePolicyEndpoints` in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/FeePolicyEndpoints.cs` implementing `IEndpoint`, registering route groups `/api/fin/charge-types` and `/api/fin/discounts` with required permissions (`Permissions.Finance.FeePolicy.Manage`, `Permissions.Finance.Discounts.Manage`), request validation filters, and OpenAPI specifications.
- Updated `IChargeTypeRepository` and `ChargeTypeRepository` to support optional academic year filtering in `GetByAcademicYearAsync`.
- Added `GetDiscountsAsync` with multi-parameter filtering to `IStudentDiscountRepository` and `StudentDiscountRepository`.
- Added comprehensive feature unit tests in `src/backend/tests/ElsasaSis.UnitTests/Finance/FeePolicyFeatureTests.cs` (16 test methods) testing validators and handler workflows.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded with 0 warnings and 0 errors.
  - `dotnet test src/backend/tests/ElsasaSis.UnitTests/ElsasaSis.UnitTests.csproj` passed (617 passed, 0 failed).
  - `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests/ElsasaSis.ArchitectureTests.csproj` passed (17 passed, 0 failed).

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/IChargeTypeRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/IStudentDiscountRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Persistence/ChargeTypeRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Persistence/StudentDiscountRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/ChargeTypeDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/CreateChargeType/CreateChargeTypeRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/CreateChargeType/CreateChargeTypeValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/CreateChargeType/CreateChargeTypeHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/GetChargeTypes/GetChargeTypesQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/GetChargeTypes/GetChargeTypesHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/UpdateChargeType/UpdateChargeTypeRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/UpdateChargeType/UpdateChargeTypeValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/UpdateChargeType/UpdateChargeTypeHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/ActivateChargeType/ActivateChargeTypeHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/FeePolicy/FeePolicyEndpoints.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Discounts/StudentDiscountDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Discounts/CreateStudentDiscount/CreateStudentDiscountRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Discounts/CreateStudentDiscount/CreateStudentDiscountValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Discounts/CreateStudentDiscount/CreateStudentDiscountHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Discounts/GetStudentDiscounts/GetStudentDiscountsQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Discounts/GetStudentDiscounts/GetStudentDiscountsHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Discounts/RevokeStudentDiscount/RevokeStudentDiscountHandler.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Finance/FeePolicyFeatureTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S12

Title: Implement Tuition Billing Engine, Ad-Hoc Charges, Cancellation & Reactive Event Subscribers

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement vertical slices for monthly batch tuition billing generation with Pre-Flight preview support, ad-hoc charge creation, charge cancellation audit, and reactive event subscribers (`StudentPlacedDomainEvent`, `CandidateEnrolledDomainEvent`) in `ElsasaSis.Modules.Fin`.

Depends On: P4-S10, P4-S11, P2-S06

Repository: src/backend

Completion Criteria:

1. Implement Vertical Slices in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/`:
   - `GetBatchBillingPreview` (`POST /api/fin/charges/batch-preview`):
     - Resolves target students via `IStudentContract.GetActiveStudentsByScopeAsync(...)`.
     - Queries matching active `MONTHLY` charge types from `fin.charge_types`.
     - Applies standing discounts from `fin.student_discounts`.
     - Computes prospective student count, gross total, discount total, and net total without writing to the database. Supports frontend Tier 3 Pre-Flight Modal.
   - `GenerateBatchTuitionCharges` (`POST /api/fin/charges/generate-batch` — `UC-FIN-03`):
     - Executes batch generation, applies discounts, skips duplicate student charges idempotently via filtered unique index, and inserts `fin.student_charges`. Returns `{ generatedCount, skippedCount }`.
   - `CreateAdHocCharge` (`POST /api/fin/charges/adhoc` — `UC-FIN-04`):
     - Creates individual student charge with custom quantity, description, and unit amount.
   - `CancelStudentCharge` (`POST /api/fin/charges/{id}/cancel` — `UC-FIN-05`):
     - Validates `paid_amount == 0`. Requires non-empty cancellation reason. Sets `status = 'CANCELLED'`, `cancellation_reason`, and `cancelled_at`. Rejects with 409 Conflict if payments have been applied.
2. Implement Event Subscribers in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Subscribers/`:
   - `StudentPlacedEventHandler`: Reacts to `StudentPlacedDomainEvent`, resolves active monthly charge types, and initializes current-month tuition charge for placed student.
   - `CandidateEnrolledEventHandler`: Reacts to `CandidateEnrolledDomainEvent`, initializes initial tuition charges.
3. Map endpoints in `BillingEndpoints.cs` implementing `IEndpoint` discovery.
4. `dotnet build src/backend/ElsasaSis.sln` succeeds with exit code `0`.

Implementation Notes:
- Added `CandidateEnrolledDomainEvent` domain event record to `ElsasaSis.Contracts/Student/Events/`.
- Expanded `IStudentChargeRepository` and `StudentChargeRepository` with `GetChargesAsync` for filtering charges by academic year, billing month, school level, grade, study group, student, and status.
- Implemented `StudentChargeDto` mapping domain aggregate properties to API representations.
- Implemented Vertical Slices in `ElsasaSis.Modules.Fin/Features/Billing/`:
  - `GetBatchBillingPreview`: Request, Response (`StudentCount`, `ChargeCount`, `SkippedCount`, `TotalGrossAmount`, `TotalDiscountAmount`, `TotalNetAmount`, `Items`), FluentValidation validator (`YYYY-MM`), and handler computing prospective impact for Tier 3 Pre-Flight Modal without database mutations.
  - `GenerateBatchTuitionCharges`: Request, Response (`GeneratedCount`, `SkippedCount`), FluentValidation validator, and handler creating `StudentCharge` records, applying standing discounts, and skipping duplicates idempotently.
  - `CreateAdHocCharge`: Request, FluentValidation validator, and handler validating student, charge type, optional discounts, and creating custom charges.
  - `CancelStudentCharge`: Request, FluentValidation validator, and handler verifying zero payments applied, recording audit reason and UTC timestamp, and updating charge status to `CANCELLED` (or returning 409 Conflict if payments applied).
  - `GetStudentCharges`: Query and handler for charge list querying.
- Implemented Reactive Event Subscribers in `ElsasaSis.Modules.Fin/Subscribers/`:
  - `StudentPlacedEventHandler`: Handles `StudentPlacedDomainEvent`, resolves student scope via `IStudentContract`, queries active monthly charge types, applies discounts, and initializes current-month tuition charge.
  - `CandidateEnrolledEventHandler`: Handles both `CandidateEnrolledDomainEvent` and `StudentEnrolledDomainEvent`, generating initial tuition charges if student placement is already available.
- Mapped all endpoints in `BillingEndpoints.cs` implementing `IEndpoint` with permissions `Permissions.Finance.Billing.Generate`, `ManageAdHoc`, and `Cancel`.
- Authored comprehensive unit tests in `src/backend/tests/ElsasaSis.UnitTests/Finance/BillingFeatureTests.cs` (13 tests) covering validators, batch preview calculations, idempotent batch generation, ad-hoc charge creation, charge cancellation rules (including 409 Conflict on paid charges), and both reactive event subscribers.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded (0 warnings, 0 errors).
  - All 638 Unit Tests passed.
  - All 17 Architecture Tests passed.

Changed Files:
- `src/backend/src/ElsasaSis.Contracts/Student/Events/CandidateEnrolledDomainEvent.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/IStudentChargeRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Persistence/StudentChargeRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/StudentChargeDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/BillingEndpoints.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GetBatchBillingPreview/BatchBillingPreviewRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GetBatchBillingPreview/BatchBillingPreviewResponse.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GetBatchBillingPreview/BatchBillingPreviewValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GetBatchBillingPreview/GetBatchBillingPreviewHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GenerateBatchTuitionCharges/GenerateBatchTuitionChargesRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GenerateBatchTuitionCharges/GenerateBatchTuitionChargesResponse.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GenerateBatchTuitionCharges/GenerateBatchTuitionChargesValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GenerateBatchTuitionCharges/GenerateBatchTuitionChargesHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/CreateAdHocCharge/CreateAdHocChargeRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/CreateAdHocCharge/CreateAdHocChargeValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/CreateAdHocCharge/CreateAdHocChargeHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/CancelStudentCharge/CancelStudentChargeRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/CancelStudentCharge/CancelStudentChargeValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/CancelStudentCharge/CancelStudentChargeHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GetStudentCharges/GetStudentChargesQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Billing/GetStudentCharges/GetStudentChargesHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Subscribers/StudentPlacedEventHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Subscribers/CandidateEnrolledEventHandler.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Finance/BillingFeatureTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S13

Title: Implement Cashier Counter, Payment Recording, Receipt Generation & Deposit Allocation Slices

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement vertical slices for frontline cashier payment recording with concurrency-safe receipt numbering (`KWT-YYYYMM-NNNN`), multi-charge allocation, excess deposit holding, advance deposit application, and student ledger statements in `ElsasaSis.Modules.Fin`.

Depends On: P4-S10, P4-S12

Repository: src/backend

Completion Criteria:

1. Implement Vertical Slices in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/`:
   - `RecordPaymentReceipt` (`POST /api/fin/payments` — `UC-FIN-06`):
     - Executed inside a single `DbTransaction`:
       1. Increment receipt sequence and generate receipt number `KWT-YYYYMM-NNNN`.
       2. Insert `fin.payment_receipts` with total, allocated, and unallocated amounts.
       3. For each allocation line: insert `fin.payment_allocations`, update `paid_amount` and status on `fin.student_charges`.
       4. Hold surplus amount as `unallocated_amount` on the receipt.
       5. Dispatch `PaymentReceiptPostedDomainEvent`.
   - `AllocateAdvanceDeposit` (`POST /api/fin/deposits/allocate` — `UC-FIN-07`):
     - Applies accumulated `unallocated_amount` from previous receipts to newly outstanding charges.
   - `GetStudentStatement` (`GET /api/fin/students/{studentId}/statement` — `UC-FIN-08`):
     - Detailed financial ledger returning all student charges, payments, allocated receipts, and net open balance.
   - `GetPaymentReceiptById` (`GET /api/fin/payments/{receiptId}`):
     - Composite DTO projection returning receipt header, itemized charge allocations, student information, and cashier signature for browser printing (`OQ-024`).
2. Map endpoints in `PaymentsEndpoints.cs` implementing `IEndpoint` discovery.
3. `dotnet build src/backend/ElsasaSis.sln` succeeds with exit code `0`.

Implementation Notes:
- Expanded `IPaymentReceiptRepository` and `PaymentReceiptRepository` with transaction-scoped persistence methods:
  - `SavePaymentReceiptTransactionAsync`: Inserts payment receipt, its itemized allocations, and updates all affected student charges inside a single `DbTransaction`.
  - `SaveDepositAllocationTransactionAsync`: Updates receipt allocations, deducts unallocated deposit holding, and updates all newly settled student charges inside a single `DbTransaction`.
- Implemented Payment DTOs in `ElsasaSis.Modules.Fin.Features.Payments`:
  - `PaymentAllocationDto`: Mapping receipt-to-charge allocation details.
  - `PaymentReceiptDto`: Complete payment receipt snapshot with itemized allocations.
  - `PaymentReceiptDetailDto`: Composite browser-printable receipt DTO (`OQ-024`) with cashier and allocation metadata.
  - `StudentStatementDto`: Real-time financial ledger statement (`UC-FIN-08`) aggregating total billed, total paid, total outstanding, available unallocated deposit balance, charges list, and receipt collection register.
- Implemented Vertical Slices in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/`:
  - `RecordPaymentReceipt` (`POST /api/fin/payments` — `UC-FIN-06`):
    - Validates request and cashier identity from JWT `ICurrentUser`.
    - Atomically increments receipt sequence and formats concurrency-safe receipt number `KWT-YYYYMM-NNNN` using `IReceiptSequenceGenerator`.
    - Handles exact payment, partial multi-charge settlement, and surplus advance deposit holding in `unallocated_amount`.
    - Persists receipt, allocations, and charge status transitions (`UNPAID -> PARTIALLY_PAID -> PAID`) in a single `DbTransaction`.
    - Raises and dispatches `PaymentReceiptPostedDomainEvent`.
  - `AllocateAdvanceDeposit` (`POST /api/fin/deposits/allocate` — `UC-FIN-07`):
    - Validates sufficient `unallocated_amount` on source receipt and student ownership.
    - Atomically allocates deposit funds across open student charges and records new `PaymentAllocation` lines in a single `DbTransaction`.
  - `GetStudentStatement` (`GET /api/fin/students/{studentId}/statement` — `UC-FIN-08`):
    - Direct Dapper projection across charges, receipts, and allocations with real-time financial balance computation.
  - `GetPaymentReceiptById` (`GET /api/fin/payments/{receiptId}` — `OQ-024`):
    - Direct Dapper projection for browser-tailored printable receipt view.
- Mapped Minimal API endpoints in `PaymentsEndpoints.cs` implementing `IEndpoint` discovery with permissions `Permissions.Finance.Payments.Record`, `Deposits.Allocate`, and `Statements.View`.
- Authored comprehensive unit tests in `src/backend/tests/ElsasaSis.UnitTests/Finance/PaymentsFeatureTests.cs` (16 tests) covering validators, exact payment, multi-charge allocation, excess deposit holding, pure deposit recording, deposit reallocation to open charges, student mismatches, and domain event dispatch.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded (0 warnings, 0 errors).
  - All 659 Unit Tests passed (0 failed).
  - All 17 Architecture Tests passed (0 failed).

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Domain/IPaymentReceiptRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Infrastructure/Persistence/PaymentReceiptRepository.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/PaymentAllocationDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/PaymentReceiptDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/PaymentReceiptDetailDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/StudentStatementDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/PaymentsEndpoints.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/RecordPaymentReceipt/PaymentAllocationItemRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/RecordPaymentReceipt/RecordPaymentReceiptRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/RecordPaymentReceipt/RecordPaymentReceiptValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/RecordPaymentReceipt/RecordPaymentReceiptHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/AllocateAdvanceDeposit/AllocateAdvanceDepositRequest.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/AllocateAdvanceDeposit/AllocateAdvanceDepositValidator.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/AllocateAdvanceDeposit/AllocateAdvanceDepositHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/GetStudentStatement/GetStudentStatementQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/GetStudentStatement/GetStudentStatementHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/GetPaymentReceiptById/GetPaymentReceiptByIdQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Payments/GetPaymentReceiptById/GetPaymentReceiptByIdHandler.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Finance/PaymentsFeatureTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P4-S14

Title: Implement Receivables Aging & Daily Cashier Summary Report Slices

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement Read Slices for receivables summary KPI metrics, aged arrears breakdown, and the daily cashier collection summary register in `ElsasaSis.Modules.Fin`.

Depends On: P4-S10, P4-S13

Repository: src/backend

Completion Criteria:

1. Implement Read Slices in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/`:
   - `GetReceivablesSummary` (`GET /api/fin/reports/receivables-summary` — `UC-FIN-09`, `UC-ADM-26`, `UC-PRC-06`):
     - Direct Dapper projection aggregating total charges, total collected, and total outstanding grouped by school level and academic year.
   - `GetReceivablesAging` (`GET /api/fin/reports/receivables-aging`):
     - Categorizes unpaid/partially paid charges into aging buckets: Current, 1–30 Days, 31–60 Days, 61–90 Days, and 90+ Days based on `due_date`.
     - Supports filtering by study group, grade, and academic year.
   - `GetDailyCashierSummary` (`GET /api/fin/reports/daily-cashier?date=YYYY-MM-DD` — `UC-FIN-11`):
     - Returns total collections for specified date grouped by payment method (`CASH`, `BANK_TRANSFER`), total deposit held, and itemized receipt register.
2. Map endpoints in `ReportsEndpoints.cs` implementing `IEndpoint` discovery.
3. `dotnet build src/backend/ElsasaSis.sln` succeeds with exit code `0`.

Implementation Notes:
- Implemented CQRS Read Slices for financial reports in `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/`:
  - `ReceivablesSummary`:
    - `ReceivablesSummaryDto` and `ReceivablesSummaryByLevelDto`: Aggregating total billed (`net_amount`), total collected (`paid_amount`), total outstanding, total student count, and charge count with breakdowns grouped by school level and academic year.
    - `GetReceivablesSummaryQuery` and `GetReceivablesSummaryHandler`: Direct multi-query Dapper projection against `fin.student_charges` excluding cancelled charges, with optional `academicYearId` filter.
  - `ReceivablesAging`:
    - `ReceivablesAgingDto`, `ReceivablesAgingSummaryDto`, and `ReceivablesAgingStudentItemDto`: Categorizes open receivables into aging buckets (`Current`, `1–30 Days`, `31–60 Days`, `61–90 Days`, `90+ Days`) and computes `ArrearsOver30Days`.
    - `GetReceivablesAgingQuery` and `GetReceivablesAgingHandler`: Direct multi-query Dapper projection against `fin.student_charges` for `status IN ('UNPAID', 'PARTIALLY_PAID')`, supporting filtering by academic year, school level, grade, study group, and as-of date (defaulting to current date).
  - `DailyCashier`:
    - `DailyCashierSummaryDto`, `PaymentMethodSummaryDto`, and `DailyCashierReceiptItemDto`: Daily collection register detailing cash collections, bank transfer collections, gross collections, advance deposits held (`unallocated_amount`), allocated amounts, receipt counts, and itemized receipts.
    - `GetDailyCashierSummaryQuery` and `GetDailyCashierSummaryHandler`: Direct Dapper query against `fin.payment_receipts` filtered by `payment_date`.
- Created `ReportsEndpoints.cs` implementing `IEndpoint` discovery, registering Minimal API endpoints under `/api/fin/reports`:
  - `GET /api/fin/reports/receivables-summary` (authorized with `Permissions.Finance.Reports.View`)
  - `GET /api/fin/reports/receivables-aging` (authorized with `Permissions.Finance.Reports.View`)
  - `GET /api/fin/reports/daily-cashier` (authorized with `Permissions.Finance.Reports.View`)
- Authored comprehensive feature unit tests in `src/backend/tests/ElsasaSis.UnitTests/Finance/ReportsFeatureTests.cs` (7 test methods) verifying DI handler registration, endpoint discovery contract, query defaults, and DTO calculation logic.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded (0 warnings, 0 errors).
  - All 667 Unit Tests passed.
  - All 17 Architecture Tests passed.
  - All 36 Integration Tests passed.

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/DailyCashier/DailyCashierSummaryDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/DailyCashier/GetDailyCashierSummaryHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/DailyCashier/GetDailyCashierSummaryQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/ReceivablesAging/GetReceivablesAgingHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/ReceivablesAging/GetReceivablesAgingQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/ReceivablesAging/ReceivablesAgingDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/ReceivablesSummary/GetReceivablesSummaryHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/ReceivablesSummary/GetReceivablesSummaryQuery.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/ReceivablesSummary/ReceivablesSummaryDto.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Fin/Features/Reports/ReportsEndpoints.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Finance/ReportsFeatureTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

## P5 - Architecture Verification, Testing & OpenAPI Sync

Implementation Status: IMPLEMENTED  
Review Status: GO

### P5-S15

Title: Update NetArchTest Boundary Rules, CQRS Constraints & Author Domain Unit Tests

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Update `ElsasaSis.ArchitectureTests` to enforce strict boundary isolation across all 7 module assemblies and author comprehensive domain unit tests in `ElsasaSis.UnitTests`.

Depends On: P2-S05, P3-S08, P3-S09, P4-S13, P4-S14

Repository: src/backend

Completion Criteria:

1. Update `src/backend/tests/ElsasaSis.ArchitectureTests/AssemblyArchitectureContext.cs`:
   - Add `FinModuleAssembly` (`typeof(FinAssemblyMarker).Assembly`) and `DevModuleAssembly` (`typeof(DevAssemblyMarker).Assembly`).
   - Include both assemblies in `AllApplicationAssemblies`.
2. Update `src/backend/tests/ElsasaSis.ArchitectureTests/ModuleBoundaryTests.cs`:
   - Add isolation tests for `FinModule` and `DevModule` asserting zero cross-module references to each other and to `{Auth, Opr, Std, Tch, Acd, Api, Database}`.
   - Assert all modules depend strictly on `SharedKernel` and `Contracts`.
   - Update `LayerAndCqrsArchitectureTests.cs` asserting zero EF Core references across all module assemblies.
3. Expand `ElsasaSis.UnitTests`:
   - `Academic/ReportCardDomainTests.cs`: Draft creation, arithmetic final grade averaging, homeroom remarks, finalization locking, administrative override audit reason.
   - `Development/DisciplineRecordTests.cs`: Role-sanction restrictions (teacher warning vs. principal suspension).
   - `Development/ExtracurricularActivityTests.cs`: Supervisor session logging validation.
   - `Finance/ChargeTypeTests.cs`: Status immutability once `ACTIVE`.
   - `Finance/StudentChargeTests.cs`: Cancellation rejected if `paid_amount > 0`.
   - `Finance/PaymentReceiptTests.cs`: Allocation math, deposit surplus holding, sequence formatting.
4. `dotnet test tests/ElsasaSis.ArchitectureTests/` and `dotnet test tests/ElsasaSis.UnitTests/` pass with exit code `0`.

Implementation Notes:
- Verified `AssemblyArchitectureContext.cs` includes `FinModuleAssembly` (`typeof(FinAssemblyMarker).Assembly`) and `DevModuleAssembly` (`typeof(DevAssemblyMarker).Assembly`) inside `AllApplicationAssemblies`.
- Updated `src/backend/tests/ElsasaSis.ArchitectureTests/ModuleBoundaryTests.cs`:
  - Verified isolation assertions for `FinModule` and `DevModule` enforcing zero cross-module dependencies to each other and `{Auth, Opr, Std, Tch, Acd, Api, Database}`.
  - Added `AllModules_ShouldOnlyReference_SharedKernel_And_Contracts_AmongInternalAssemblies` verifying that all 7 module assemblies depend strictly on `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts` with zero forbidden internal references.
- Updated `src/backend/tests/ElsasaSis.ArchitectureTests/LayerAndCqrsArchitectureTests.cs`:
  - Added `AllModuleAssemblies_ShouldNotReference_EntityFrameworkCore` asserting zero EF Core dependencies across all 7 module assemblies.
- Expanded `ElsasaSis.UnitTests`:
  - `Academic/ReportCardDomainTests.cs`: Added unit tests for arithmetic final grade averaging calculation (`ROUND(AVG(score), 2)`), precision rounding, letter grade mapping, and draft entry generation with calculated averages.
  - `Development/DisciplineRecordTests.cs`: Authored dedicated unit test suite (14 test methods) verifying role-sanction restrictions (`TEACHER` classroom warnings vs. `PRINCIPAL` escalated suspension/parent meeting with `SanctionImposedDomainEvent` dispatch, `SCHOOL_ADMIN` boundaries, violation type and length validations, and rehydration).
  - `Development/ExtracurricularActivityTests.cs`: Authored dedicated unit test suite (16 test methods) verifying supervisor session logging validation (assigned supervisor permission check, non-supervisor rejection, status validation for withdrawn/completed enrollments, length/empty validations, duplicate enrollments guard, and rehydration).
  - `Finance/ChargeTypeTests.cs`: Authored dedicated unit test suite (10 test methods) verifying status immutability once `ACTIVE` (`Fin.ChargeType.CannotBeModified` conflict), draft updates, frequency/amount validations, and rehydration.
  - `Finance/StudentChargeTests.cs`: Authored dedicated unit test suite (12 test methods) verifying cancellation rejection when `paid_amount > 0` (`Fin.StudentCharge.CannotCancelPaidCharge`), cancellation audit reason requirements, gross/net/discount arithmetic, payment application status transitions (`UNPAID -> PARTIALLY_PAID -> PAID`), and rehydration.
  - `Finance/PaymentReceiptTests.cs`: Authored dedicated unit test suite (8 test methods) verifying allocation math, surplus advance deposit holding in `unallocated_amount`, multi-charge allocations, receipt sequence regex format verification (`KWT-YYYYMM-NNNN`), exceeding allocation rejection, and rehydration.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded (0 warnings, 0 errors).
  - `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests` passed (19 tests passed, 0 failed).
  - `dotnet test src/backend/tests/ElsasaSis.UnitTests` passed (739 tests passed, 0 failed).

Changed Files:
- `src/backend/tests/ElsasaSis.ArchitectureTests/LayerAndCqrsArchitectureTests.cs`
- `src/backend/tests/ElsasaSis.ArchitectureTests/ModuleBoundaryTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Academic/ReportCardDomainTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Development/DisciplineRecordTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Development/ExtracurricularActivityTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Finance/ChargeTypeTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Finance/StudentChargeTests.cs`
- `src/backend/tests/ElsasaSis.UnitTests/Finance/PaymentReceiptTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P5-S16

Title: Implement Comprehensive Backend Integration Tests for Stage 3 API Endpoints

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Author and verify end-to-end HTTP integration tests for all Stage 3 endpoints in `ElsasaSis.IntegrationTests` using `WebApplicationFactory` and `Stage3Seeder`.

Depends On: P5-S15

Repository: src/backend

Completion Criteria:

1. Implement integration test suites in `src/backend/tests/ElsasaSis.IntegrationTests/`:
   - `Stage3AcdReportCardTests.cs`:
     - Batch draft report card generation (`POST /api/acd/report-cards/generate-batch`) verifies arithmetic mean final grade calculation and idempotent re-run.
     - Subject notes submission (`POST /api/acd/report-cards/{id}/subject-notes`) enforces teaching assignment check.
     - Homeroom remarks and finalization (`POST /api/acd/report-cards/{id}/finalize`).
     - Administrative override (`POST /api/acd/report-cards/{id}/override`) with mandatory reason unlocks card back to `DRAFT`.
   - `Stage3DevPastoralTests.cs`:
     - Behaviour notes recording (`POST /api/dev/behaviour`).
     - Discipline recording with warning (`POST /api/dev/discipline`).
     - Principal escalated sanction (`POST /api/dev/discipline/escalated`).
     - Extracurricular activity creation, enrollment, and supervisor session logging (`POST /api/dev/extracurriculars/.../log`).
     - 360° student development dossier query (`GET /api/dev/students/{id}/dossier`).
   - `Stage3FinBillingAndPaymentsTests.cs`:
     - Charge type creation, update, and activation lock.
     - Student standing discount creation.
     - Batch billing generation with discount application and duplicate skip.
     - Ad-hoc charge creation and charge cancellation with audit reason.
     - Cashier payment receipt posting with atomic sequence counter (`KWT-YYYYMM-NNNN`), multi-charge allocation, and deposit holding.
     - Advance deposit allocation (`POST /api/fin/deposits/allocate`).
     - Student statement and daily cashier summary queries.
2. `dotnet test tests/ElsasaSis.IntegrationTests/` passes against local SQL Server with exit code `0`.

Implementation Notes:
- Added `DefaultFinancePassword = "Teacher123!"` constant to `Stage3Seeder.cs` for integration test credential clarity.
- Implemented `src/backend/tests/ElsasaSis.IntegrationTests/Stage3AcdReportCardTests.cs` (4 test methods):
  - `BatchDraftReportCardGeneration_ComputesArithmeticMeanFinalGrade_AndIsIdempotent`: Cleans pre-existing cards, issues batch draft generation verifying arithmetic average final grades from scorebook assignments (`GeneratedCount = 2, SkippedCount = 0`), and asserts idempotent skipping on subsequent invocation (`GeneratedCount = 0, SkippedCount = 2`).
  - `SubjectNotesSubmission_EnforcesTeachingAssignmentCheck`: Verifies assigned subject teacher submits notes successfully (200 OK), while unassigned teacher receives 409 Conflict (`AcdErrors.ReportCards.TeacherNotAssignedToSubject`).
  - `HomeroomRemarks_And_Finalization_TransitionsToFinalized`: Verifies homeroom teacher submits general remarks, finalizes report card (`DRAFT -> FINALIZED` with `finalized_at`), and asserts modification rejection on finalized card.
  - `AdministrativeOverride_UnlocksCardBackToDraft_WithMandatoryReason`: Verifies empty override reason rejection (422 UnprocessableEntity) and non-empty override unlocks card back to `DRAFT` while clearing `finalized_at` and recording audit reason.
- Implemented `src/backend/tests/ElsasaSis.IntegrationTests/Stage3DevPastoralTests.cs` (5 test methods):
  - `BehaviourNotes_Recording_Succeeds`: Records positive behaviour observation and verifies populated DTO response and persistence.
  - `Discipline_ClassroomWarning_Succeeds_AndRejectsEscalatedSanctions`: Verifies teacher issues classroom verbal warning (201 Created) and rejects illegal escalation attempts (`SUSPENSION`) with 422 UnprocessableEntity.
  - `Principal_EscalatedSanction_Succeeds_AndRejectsUnauthorizedCallers`: Verifies principal issues escalated suspension (201 Created) while non-principal callers are rejected with 403 Forbidden.
  - `Extracurricular_FullLifecycle_Create_Enroll_AndSupervisorSessionLogging`: Admin creates club and enrolls student; assigned teacher supervisor records session meeting log (200 OK) updating `last_session_log` and `last_session_date`.
  - `StudentDevelopmentDossier_ReturnsComposite360View`: Verifies unified 360-degree timeline dossier query aggregating behaviour notes, discipline incidents, active clubs, achievements, and timeline items.
- Implemented `src/backend/tests/ElsasaSis.IntegrationTests/Stage3FinBillingAndPaymentsTests.cs` (7 test methods):
  - `ChargeType_Lifecycle_CreateDraft_Update_ActivateLock_AndRejectSubsequentUpdates`: Draft creation (201 Created), draft amount update (200 OK), Tier 2 activation lock (200 OK), and subsequent update rejection (409 Conflict).
  - `StudentStandingDiscount_Creation_Succeeds`: Registers percentage-based student discount against fee structure.
  - `BatchBillingGeneration_AppliesDiscounts_AndSkipsDuplicatesIdempotently`: Generates monthly cohort billing, applies standing student discounts, and skips duplicate charges idempotently on repeat execution.
  - `AdHocCharge_Creation_AndCancellationWithAuditReason`: Creates ad-hoc charge (201 Created), rejects empty cancellation reason (422), and cancels charge with mandatory audit reason (200 OK).
  - `CashierPaymentPosting_SequenceCounter_MultiAllocation_AndExcessDepositHolding`: Frontier cashier payment posting with atomic receipt numbering (`KWT-YYYYMM-NNNN`), multi-charge settlement, and surplus advance deposit holding in `unallocated_amount`.
  - `AdvanceDeposit_Allocation_AppliesHeldFundsToOpenCharge`: Reallocates advance deposit surplus from previous receipt to settle newly created student charge.
  - `FinancialReports_StudentStatement_DailyCashier_AndReceivablesQueries`: Validates student ledger statement (`UC-FIN-08`), daily cashier collection summary register (`UC-FIN-11`), receivables summary KPIs (`UC-FIN-09`), and receivables aged arrears breakdown.
- Verification:
  - `dotnet build src/backend/ElsasaSis.sln` succeeded (0 warnings, 0 errors).
  - `dotnet test src/backend/tests/ElsasaSis.IntegrationTests` passed (52 passed, 0 failed).
  - `dotnet test src/backend/tests/ElsasaSis.UnitTests` passed (739 passed, 0 failed).
  - `dotnet test src/backend/tests/ElsasaSis.ArchitectureTests` passed (19 passed, 0 failed).

Changed Files:
- `src/backend/src/ElsasaSis.Database/Stage3Seeder.cs`
- `src/backend/tests/ElsasaSis.IntegrationTests/Stage3AcdReportCardTests.cs`
- `src/backend/tests/ElsasaSis.IntegrationTests/Stage3DevPastoralTests.cs`
- `src/backend/tests/ElsasaSis.IntegrationTests/Stage3FinBillingAndPaymentsTests.cs`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P5-S17

Title: Regenerate OpenAPI Specification, TypeScript Schema (`schema.d.ts`) & Define Query Key Factories

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Generate the updated OpenAPI contract from the running backend API, synchronize frontend TypeScript contracts (`schema.d.ts`), and establish TanStack Query key factories for all Stage 3 feature areas.

Depends On: P5-S16

Repository: src/frontend

Completion Criteria:

1. Export updated OpenAPI specification from backend API and execute `pnpm openapi:generate` in `src/frontend`:
   - `src/frontend/src/api/schema.d.ts` is updated with all Stage 3 endpoint paths, DTO schemas, and request/response contracts.
2. Create TanStack Query key factories in `src/frontend/src/features/`:
   - `src/features/academic/report-cards/api/keys.ts` (`reportCardKeys`)
   - `src/features/development/pastoral/api/keys.ts` (`pastoralKeys`)
   - `src/features/development/extracurricular/api/keys.ts` (`extracurricularKeys`)
   - `src/features/finance/fee-policy/api/keys.ts` (`feePolicyKeys`)
   - `src/features/finance/billing/api/keys.ts` (`billingKeys`)
   - `src/features/finance/payments/api/keys.ts` (`paymentKeys`)
   - `src/features/finance/receivables/api/keys.ts` (`receivableKeys`)
3. `pnpm typecheck` in `src/frontend` compiles cleanly with exit code `0`.

Implementation Notes:
- Resolved Swashbuckle schemaId duplicate conflict between `ElsasaSis.Modules.Dev.Features.Extracurricular.ExtracurricularParticipationDto` and redundant `Dossier/ExtracurricularParticipationDto` by consolidating onto the canonical Extracurricular DTO.
- Exported complete OpenAPI JSON specification from test host via `SwaggerExportIntegrationTests.cs` to `src/frontend/swagger.json`.
- Synchronized frontend TypeScript contracts using `openapi-typescript` updating `src/frontend/src/shared/types/api/schema.d.ts` (5,032 additions) and created `src/frontend/src/api/schema.d.ts` re-export module.
- Established TanStack Query key factories across all 7 Stage 3 feature areas:
  - `src/frontend/src/features/academic/report-cards/api/keys.ts` (`reportCardKeys`)
  - `src/frontend/src/features/development/pastoral/api/keys.ts` (`pastoralKeys`)
  - `src/frontend/src/features/development/extracurricular/api/keys.ts` (`extracurricularKeys`)
  - `src/frontend/src/features/finance/fee-policy/api/keys.ts` (`feePolicyKeys`)
  - `src/frontend/src/features/finance/billing/api/keys.ts` (`billingKeys`)
  - `src/frontend/src/features/finance/payments/api/keys.ts` (`paymentKeys`)
  - `src/frontend/src/features/finance/receivables/api/keys.ts` (`receivableKeys`)
- Verification:
  - `pnpm typecheck` (`vue-tsc --noEmit`) succeeded with exit code 0.
  - `pnpm build` succeeded with exit code 0.
  - `pnpm test:unit` passed all 41 test files (263 unit tests passed).
  - `pnpm lint` passed with 0 errors and 0 warnings.
  - Backend `dotnet test` passed (739 unit tests, 19 architecture tests, 53 integration tests).

Changed Files:
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Dossier/GetStudentDossierHandler.cs`
- `src/backend/src/Modules/ElsasaSis.Modules.Dev/Features/Dossier/StudentDossierDto.cs`
- `src/backend/tests/ElsasaSis.IntegrationTests/SwaggerExportIntegrationTests.cs`
- `src/frontend/swagger.json`
- `src/frontend/src/api/schema.d.ts`
- `src/frontend/src/shared/types/api/schema.d.ts`
- `src/frontend/src/features/academic/report-cards/api/keys.ts`
- `src/frontend/src/features/development/pastoral/api/keys.ts`
- `src/frontend/src/features/development/extracurricular/api/keys.ts`
- `src/frontend/src/features/finance/fee-policy/api/keys.ts`
- `src/frontend/src/features/finance/billing/api/keys.ts`
- `src/frontend/src/features/finance/payments/api/keys.ts`
- `src/frontend/src/features/finance/receivables/api/keys.ts`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

## P6 - Frontend Feature Screens

Implementation Status: IMPLEMENTED  
Review Status: GO

### P6-S18

Title: Implement Academic Semester Report Cards Screen (`SCR-ACD-REPORT-CARDS`) & Browser Print View

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement the full collaborative report card workspace, study group review grid, subject feedback editor, homeroom narrative remark editor, finalization action, administrative unlock dialog, and printable report card view in `src/features/academic/report-cards/`.

Depends On: P1-S03, P5-S17

Repository: src/frontend

Completion Criteria:

1. Create components in `src/frontend/src/features/academic/report-cards/`:
   - `ReportCardsWorkbenchView.vue`:
     - Study group selector, academic period selector.
     - Student report card roster table with status chips (`DRAFT`, `FINALIZED`), grade averages, and action buttons.
     - Batch draft generator button triggering Pre-Flight summary modal (`UC-ADM-19`).
     - Subject teacher feedback panel allowing assigned teachers to enter feedback notes on their subject stage (`UC-TCH-04`).
     - Homeroom teacher narrative editor for holistic remarks (`UC-HRM-02`).
     - Homeroom finalization button with confirmation dialog (`UC-HRM-03`).
     - Administrative override dialog requesting non-empty reason to unlock finalized cards back to `DRAFT` (Tier 2 confirmation, `UC-ADM-20`).
   - `ReportCardPrintView.vue`:
     - Official A4 portrait report card layout (`@page { size: A4 portrait; margin: 12mm; }`).
     - Displays school header, student info (NIS, NISN, Name, Study Group), subject grade breakdown table, attendance metric counts, teacher notes, homeroom remarks, and signature blocks.
     - Triggers standard browser `window.print()` cleanly without layout clipping.
2. Author TanStack Query hooks in `api/useReportCardQueries.ts` and mutations in `api/useReportCardMutations.ts`.
3. Unit tests in `ReportCardsWorkbenchView.spec.ts` pass.
4. `pnpm typecheck` and `pnpm lint` succeed.

Implementation Notes:
- Implemented typed API client (`api/reportCards.api.ts`), TanStack Query hooks (`api/useReportCardQueries.ts`), and mutation hooks (`api/useReportCardMutations.ts`) wired to `reportCardKeys` and OpenAPI DTOs (`HomeroomReportCardDto`, `ReportCardsOverviewDto`, `ReportCardDetailDto`, `GenerateDraftReportCardsRequest`, `ProvideSubjectNotesRequest`, `WriteHomeroomRemarksRequest`, `OverrideReportCardStatusRequest`).
- Implemented `ReportCardsWorkbenchView.vue` (`SCR-ACD-REPORT-CARDS`) with Academic Period and Study Group selectors, school-wide overview mode for `Permissions.Academic.ReportCards.View` (`SCHOOL_ADMIN`, `PRINCIPAL`) and homeroom roster mode (`ViewOrManage` for `TEACHER`, `HOMEROOM_TEACHER`), status/search filtering, KPI summary cards, and split work-queue roster/detail layout.
- Implemented `BatchGenerateModal.vue` (`UC-ADM-19`) with Tier 3 Pre-Flight Impact Summary displaying target study group, academic period, enrolled cohort count, and estimated new vs skipped drafts.
- Implemented `ReportCardDetailPanel.vue` with inline subject teacher feedback editor (`UC-TCH-04`), homeroom narrative remarks editor (`UC-HRM-02`), Tier 2 finalization confirmation dialog (`UC-HRM-03`), Tier 2 administrative override/unlock dialog enforcing mandatory audit reason (`UC-ADM-20`), override audit notice banner, and finalized/historical period read-only guards.
- Implemented `ReportCardPrintView.vue` with A4 portrait `@media print` (`@page { size: A4 portrait; margin: 12mm; }`) print styling hiding shell chrome and preview controls cleanly, displaying school header, student metadata (NIS, NISN, Study Group, Homeroom Teacher), subject grade breakdown, attendance counts, homeroom remarks, signature blocks, and `window.print()` trigger.
- Authored 8 Vitest + MSW unit tests in `views/__tests__/ReportCardsWorkbenchView.spec.ts` covering Admin overview & batch draft generation, Subject Teacher feedback notes, Homeroom remarks & finalization, Admin override reason enforcement, Principal read-only overview, A4 print view `window.print()`, and historical period locking.
- Verification:
  - `pnpm --dir src/frontend test:unit src/features/academic/report-cards/views/__tests__/ReportCardsWorkbenchView.spec.ts`: 1 test file, 8 tests passed.
  - `pnpm --dir src/frontend exec eslint "src/features/academic/report-cards/**/*.{ts,vue}" --max-warnings 0`: passed (0 errors, 0 warnings).
  - `pnpm --dir src/frontend exec vite build`: passed (production bundle built cleanly).
  - `pnpm --dir src/frontend typecheck`: passed with 0 errors on `report-cards` prior to concurrent `P6-S21` edit in `ChargeTypeDrawer.vue`.

Changed Files:
- `src/frontend/src/features/academic/report-cards/api/reportCards.api.ts`
- `src/frontend/src/features/academic/report-cards/api/useReportCardQueries.ts`
- `src/frontend/src/features/academic/report-cards/api/useReportCardMutations.ts`
- `src/frontend/src/features/academic/report-cards/components/BatchGenerateModal.vue`
- `src/frontend/src/features/academic/report-cards/components/ReportCardDetailPanel.vue`
- `src/frontend/src/features/academic/report-cards/components/ReportCardPrintView.vue`
- `src/frontend/src/features/academic/report-cards/views/ReportCardsWorkbenchView.vue`
- `src/frontend/src/features/academic/report-cards/views/__tests__/ReportCardsWorkbenchView.spec.ts`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P6-S19

Title: Implement Pastoral Care & 360° Student Dossier Screen (`SCR-DEV-PASTORAL`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement the 360° student development dossier, behaviour note composer, classroom discipline incident logger, principal escalated sanction modal, and achievement award logger in `src/features/development/pastoral/`.

Depends On: P1-S03, P5-S17

Repository: src/frontend

Completion Criteria:

1. Create components in `src/frontend/src/features/development/pastoral/`:
   - `PastoralDossierView.vue`:
     - Student search bar with quick autocomplete.
     - Metric cards: Positive Observations, Negative Observations, Disciplinary Actions, Awards, Active Clubs.
     - 360° chronological timeline displaying all development events with category filter pills (All, Behaviour, Discipline, Awards, Clubs).
     - Action buttons triggering recording modals based on user role permissions.
   - `BehaviourNoteComposerModal.vue` (`UC-TCH-06`):
     - Form capturing behaviour type (`POSITIVE`, `NEGATIVE`), observation date, and description.
   - `DisciplineIncidentModal.vue` (`UC-TCH-07`, `UC-ADM-22`):
     - Form capturing incident date, violation type, description, and sanction (`VERBAL_WARNING`, `WRITTEN_WARNING`).
   - `EscalatedSanctionModal.vue` (`UC-PRC-03`):
     - Principal-only modal for issuing `PARENT_MEETING` or `SUSPENSION` with Tier 2 confirmation dialog.
   - `AchievementAwardModal.vue` (`UC-TCH-08`, `UC-ADM-23`):
     - Form capturing event name, achievement type, recognition level, date, and description.
2. Author TanStack Query hooks in `api/usePastoralQueries.ts` and mutations in `api/usePastoralMutations.ts`.
3. Unit tests in `PastoralDossierView.spec.ts` pass.
4. `pnpm typecheck` and `pnpm lint` succeed.

Implementation Notes:
- Implemented `pastoral.api.ts`, `usePastoralQueries.ts`, and `usePastoralMutations.ts` consuming generated OpenAPI contracts (`StudentDossierDto`, `DevelopmentOverviewDto`, `BehaviourNoteDto`, `DisciplineRecordDto`, `AchievementDto`) and `pastoralKeys` with cache invalidation and PrimeVue Toast feedback.
- Created Zod validation schemas and shared constants/error helpers in `schemas/pastoral.schema.ts` aligned with backend FluentValidation constraints.
- Implemented `PastoralDossierView.vue` (`SCR-DEV-PASTORAL`) with debounced student search autocomplete (`/api/std/students`), initial school-wide pastoral overview (`/api/dev/overview`), 5 metric cards, 360° chronological timeline with category filter pills (`All`, `Behaviour`, `Discipline`, `Awards`, `Clubs`), and permission-gated action buttons.
- Implemented `StudentContextBanner.vue`, `BehaviourNoteComposerModal.vue`, `DisciplineIncidentModal.vue`, `EscalatedSanctionModal.vue` (with `ConfirmActionDialog` Tier 2 confirmation before posting `PARENT_MEETING` or `SUSPENSION`), and `AchievementAwardModal.vue`.
- Added Vitest + MSW unit tests in `PastoralDossierView.spec.ts` covering overview rendering, student autocomplete search, metric cards, timeline category filtering, role-based button visibility, modal submissions, and Principal Tier 2 confirmation flow.
- Verification:
  - `pnpm typecheck` (`vue-tsc --noEmit`) passed with exit code 0.
  - `pnpm lint` (`eslint . --max-warnings 0`) passed with 0 errors and 0 warnings.
  - `pnpm test:unit src/features/development/pastoral/views/__tests__/PastoralDossierView.spec.ts` passed (4/4 tests).
  - `pnpm build` (`vue-tsc --noEmit && vite build`) succeeded with exit code 0.

Changed Files:
- `src/frontend/src/features/development/pastoral/api/pastoral.api.ts`
- `src/frontend/src/features/development/pastoral/api/usePastoralQueries.ts`
- `src/frontend/src/features/development/pastoral/api/usePastoralMutations.ts`
- `src/frontend/src/features/development/pastoral/schemas/pastoral.schema.ts`
- `src/frontend/src/features/development/pastoral/components/StudentContextBanner.vue`
- `src/frontend/src/features/development/pastoral/components/BehaviourNoteComposerModal.vue`
- `src/frontend/src/features/development/pastoral/components/DisciplineIncidentModal.vue`
- `src/frontend/src/features/development/pastoral/components/EscalatedSanctionModal.vue`
- `src/frontend/src/features/development/pastoral/components/AchievementAwardModal.vue`
- `src/frontend/src/features/development/pastoral/views/PastoralDossierView.vue`
- `src/frontend/src/features/development/pastoral/views/__tests__/PastoralDossierView.spec.ts`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P6-S20

Title: Implement Extracurricular Activities Workbench (`SCR-DEV-EXTRACURRICULAR`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement the extracurricular activity catalog, supervisor assignment, student enrollment modal, and supervisor session activity log recorder in `src/features/development/extracurricular/`.

Depends On: P1-S03, P5-S17

Repository: src/frontend

Completion Criteria:

1. Create components in `src/frontend/src/features/development/extracurricular/`:
   - `ExtracurricularWorkbenchView.vue`:
     - Activity catalog grid showing club cards, assigned supervisor, and active member count.
     - "New Club" button for administrators (`UC-ADM-25`).
     - Activity detail drawer showing enrolled students roster and recent session activity logs.
   - `ActivityEditorModal.vue`:
     - Form creating or editing club details with teacher supervisor selection dropdown.
   - `EnrollmentModal.vue`:
     - Student search and enrollment into selected activity (`UC-ADM-25`).
   - `SessionLogModal.vue` (`UC-TCH-09`):
     - Meeting activity log recorder with session date, meeting topics, and participant notes (accessible only to assigned supervisor).
2. Author TanStack Query hooks in `api/useExtracurricularQueries.ts` and mutations in `api/useExtracurricularMutations.ts`.
3. Unit tests in `ExtracurricularWorkbenchView.spec.ts` pass.
4. `pnpm typecheck` and `pnpm lint` succeed.

Implementation Notes:
- Implemented Extracurricular Activities Workbench (`SCR-DEV-EXTRACURRICULAR`) in `src/frontend/src/features/development/extracurricular/`:
  - API & Query Layer: Updated `api/keys.ts` (`extracurricularKeys` with `supervisorId` and `studentSearch` keys), authored `api/extracurricular.api.ts` consuming generated OpenAPI schema types (`ExtracurricularActivityDto`, `ExtracurricularParticipationDto`, `CreateExtracurricularActivityRequest`, `UpdateExtracurricularActivityRequest`, `EnrollExtracurricularParticipantRequest`, `RecordExtracurricularSessionLogRequest`), `api/useExtracurricularQueries.ts`, and `api/useExtracurricularMutations.ts` (create/update club, enroll/withdraw/complete participant, record session log with cache invalidation and PrimeVue Toast notifications).
  - Validation & Helpers: Authored Zod schemas (`schemas/extracurricular.schema.ts`) for club creation/editing, student enrollment, and session logs, plus shared form error mapping (`utils/formErrors.ts`) and supervisor identity matching (`utils/supervisorAuth.ts`).
  - UI Components:
    - `views/ExtracurricularWorkbenchView.vue`: KPI summary cards, search/status/`My Supervised Clubs` filters, responsive club catalog grid displaying supervisor and active participant counts, and admin-only `"New Club"` / `"Edit"` controls (`Permissions.Development.Extracurricular.Manage`).
    - `components/ActivityDetailDrawer.vue`: Slide-over detail drawer showing club overview, participant status filter, enrolled students roster `DataTable`, Tier 2 confirmation dialogs for withdrawing/completing active participants, recent session activity logs list, and supervisor-scoped session logging access banner.
    - `components/ActivityEditorModal.vue`: VeeValidate + Zod modal for creating/editing clubs with active teacher supervisor selection dropdown (`activeTeachersLookupQuery`) and status management.
    - `components/EnrollmentModal.vue`: Live active student search & selection modal (`UC-ADM-25`) disabling already-enrolled students and handling 409 Conflict / 422 validation errors.
    - `components/SessionLogModal.vue` (`UC-TCH-09`): Meeting activity log recorder accessible strictly to the assigned club supervisor (`Permissions.Development.Extracurricular.Log` + `isActivitySupervisor` match).
  - Tests: Authored 7 schema unit tests (`schemas/__tests__/extracurricular.schema.spec.ts`) and 7 MSW-backed component tests (`views/__tests__/ExtracurricularWorkbenchView.spec.ts`) covering catalog grid rendering, permission gating, filtering, detail drawer roster & recent logs, supervisor-only session logging, student enrollment, and club creation.
- Verification:
  - `npx vitest run src/features/development/extracurricular/`: 2 test files, 14 tests passed.
  - `npx eslint src/features/development/extracurricular/ --max-warnings 0`: passed with 0 errors and 0 warnings.
  - `npx vite build`: succeeded cleanly.
  - `pnpm typecheck` (`vue-tsc --noEmit`): 0 errors in `src/features/development/extracurricular/` (1 pre-existing/parallel error reported outside scope in `src/features/development/pastoral/schemas/pastoral.schema.ts(147,11)`).

Changed Files:
- `src/frontend/src/features/development/extracurricular/api/keys.ts`
- `src/frontend/src/features/development/extracurricular/api/extracurricular.api.ts`
- `src/frontend/src/features/development/extracurricular/api/useExtracurricularQueries.ts`
- `src/frontend/src/features/development/extracurricular/api/useExtracurricularMutations.ts`
- `src/frontend/src/features/development/extracurricular/schemas/extracurricular.schema.ts`
- `src/frontend/src/features/development/extracurricular/schemas/__tests__/extracurricular.schema.spec.ts`
- `src/frontend/src/features/development/extracurricular/utils/formErrors.ts`
- `src/frontend/src/features/development/extracurricular/utils/supervisorAuth.ts`
- `src/frontend/src/features/development/extracurricular/components/ActivityDetailDrawer.vue`
- `src/frontend/src/features/development/extracurricular/components/ActivityEditorModal.vue`
- `src/frontend/src/features/development/extracurricular/components/EnrollmentModal.vue`
- `src/frontend/src/features/development/extracurricular/components/SessionLogModal.vue`
- `src/frontend/src/features/development/extracurricular/views/ExtracurricularWorkbenchView.vue`
- `src/frontend/src/features/development/extracurricular/views/__tests__/ExtracurricularWorkbenchView.spec.ts`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P6-S21

Title: Implement Fee Policies & Standing Discounts Screen (`SCR-FIN-FEES-POLICY`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement the fee policy catalog, fee structure drawer, fee activation confirmation (Tier 2), and student standing discount / scholarship management in `src/features/finance/fee-policy/`.

Depends On: P1-S03, P5-S17

Repository: src/frontend

Completion Criteria:

1. Create components in `src/frontend/src/features/finance/fee-policy/`:
   - `FeesPolicyView.vue`:
     - Tabbed interface: "Fee Catalog" and "Student Discounts".
   - `FeeCatalogTab.vue` (`UC-FIN-01`):
     - Data table listing fee structures, recurrence frequencies, amounts, status badges (`DRAFT`, `ACTIVE`).
     - "New Fee Structure" button opening drawer.
     - "Activate" action with Tier 2 confirmation dialog warning that active fees cannot be modified.
   - `ChargeTypeDrawer.vue`:
     - Form creating or editing charge types with VeeValidate + Zod validation.
   - `StudentDiscountsTab.vue` (`UC-FIN-02`):
     - Data table of active standing discounts.
     - "Grant Discount" modal with student search, fee selection, discount type (`FIXED_AMOUNT`, `PERCENTAGE`), and value.
     - "Revoke" action with confirmation dialog.
2. Author TanStack Query hooks in `api/useFeePolicyQueries.ts` and mutations in `api/useFeePolicyMutations.ts`.
3. Unit tests in `FeesPolicyView.spec.ts` pass.
4. `pnpm typecheck` and `pnpm lint` succeed.

Implementation Notes:
- Implemented Fee Policies & Standing Discounts Screen (`SCR-FIN-FEES-POLICY`) in `src/frontend/src/features/finance/fee-policy/`:
  - API & Query Layer: Extended `api/keys.ts` (`feePolicyKeys` with `academicYearId`, `frequency`, `isActive`, and `studentSearch` keys), authored `api/feePolicy.api.ts` consuming generated OpenAPI schema types (`ChargeTypeDto`, `CreateChargeTypeRequest`, `UpdateChargeTypeRequest`, `StudentDiscountDto`, `CreateStudentDiscountRequest`, `StudentListItemDto`), `api/useFeePolicyQueries.ts` (`useChargeTypesQuery`, `useStudentDiscountsQuery`, `useDiscountStudentSearchQuery`), and `api/useFeePolicyMutations.ts` (create/update/activate charge type, grant/revoke student discount with cache invalidation and PrimeVue Toast notifications).
  - Validation & Utilities: Authored Zod schemas (`schemas/feePolicy.schema.ts`) validating fee structure fields (`MONTHLY`, `SEMESTER`, `YEARLY`, `ONE_TIME`, non-negative amount, max 100 chars) and student discounts (`PERCENTAGE` 0–100%, `FIXED_AMOUNT` > 0, mandatory reason <= 500 chars), plus `utils/formatters.ts` (IDR currency formatting, frequency labels, cohort scope display, discount value display) and `utils/formErrors.ts` (RFC 7807 `422` field error mapping and `409` conflict extraction).
  - UI Components:
    - `views/FeesPolicyView.vue`: Tabbed workbench (`Fee Catalog`, `Student Discounts`) with permission gating (`Permissions.Finance.FeePolicy.Manage`, `Permissions.Finance.Discounts.Manage`) and academic year selector defaulting to active/selected app context.
    - `components/FeeCatalogTab.vue` (`UC-FIN-01`): Summary KPI strip, search/frequency/status filters, fee catalog `DataTable` displaying recurrence frequency, IDR standard amount, cohort scope, and `DRAFT`/`ACTIVE` status pills, `"New Fee Structure"` action, `"Edit"` action for drafts (locked badge on `ACTIVE` fees), and `"Activate"` action with Tier 2 `ConfirmActionDialog` warning that active fees cannot be modified.
    - `components/ChargeTypeDrawer.vue`: Slide-over `Drawer` with VeeValidate + Zod form for creating/editing charge types (academic year, name, frequency, amount, optional school level, grade, and study group scope) and immutable active notice.
    - `components/StudentDiscountsTab.vue` (`UC-FIN-02`): Summary KPI strip, search & active/revoked status filters, standing discounts `DataTable`, `"Grant Discount"` button, and `"Revoke"` action with Tier 2 `ConfirmActionDialog`.
    - `components/GrantDiscountModal.vue` (`UC-FIN-02`): Modal with live student search (`/api/std/students`), fee structure selector, discount type (`PERCENTAGE`, `FIXED_AMOUNT`), discount value, reason/program input, and `409 Conflict` / `422` error banner handling.
  - Tests: Authored 5 schema unit tests (`schemas/__tests__/feePolicy.schema.spec.ts`) and 7 MSW-backed component tests (`views/__tests__/FeesPolicyView.spec.ts`) covering Fee Catalog rendering, create/edit fee drawer, Tier 2 activation warning, Student Discounts listing, grant discount modal + `409` conflict handling, discount revocation, and permission gating.
- Verification:
  - `npx vitest run src/features/finance/fee-policy/`: 2 test files, 12 tests passed.
  - `npx eslint src/features/finance/fee-policy/ --max-warnings 0`: passed with 0 errors and 0 warnings.
  - `npx prettier --check src/features/finance/fee-policy/`: all matched files formatted cleanly.
  - `pnpm typecheck` (`vue-tsc --noEmit`): 0 errors in `src/features/finance/fee-policy/` (1 unrelated parallel-slice error reported outside scope in `src/features/finance/payments/views/PaymentsWorkbenchView.vue(72,30)`).
  - `npx vite build`: production bundle built cleanly (`FeesPolicyView` chunk emitted).

Changed Files:
- `src/frontend/src/features/finance/fee-policy/api/keys.ts`
- `src/frontend/src/features/finance/fee-policy/api/feePolicy.api.ts`
- `src/frontend/src/features/finance/fee-policy/api/useFeePolicyQueries.ts`
- `src/frontend/src/features/finance/fee-policy/api/useFeePolicyMutations.ts`
- `src/frontend/src/features/finance/fee-policy/schemas/feePolicy.schema.ts`
- `src/frontend/src/features/finance/fee-policy/schemas/__tests__/feePolicy.schema.spec.ts`
- `src/frontend/src/features/finance/fee-policy/utils/formatters.ts`
- `src/frontend/src/features/finance/fee-policy/utils/formErrors.ts`
- `src/frontend/src/features/finance/fee-policy/components/ChargeTypeDrawer.vue`
- `src/frontend/src/features/finance/fee-policy/components/FeeCatalogTab.vue`
- `src/frontend/src/features/finance/fee-policy/components/GrantDiscountModal.vue`
- `src/frontend/src/features/finance/fee-policy/components/StudentDiscountsTab.vue`
- `src/frontend/src/features/finance/fee-policy/views/FeesPolicyView.vue`
- `src/frontend/src/features/finance/fee-policy/views/__tests__/FeesPolicyView.spec.ts`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P6-S22

Title: Implement Tuition Billing Workbench (`SCR-FIN-BILLING`) with Tier 3 Pre-Flight Modal

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement monthly batch tuition billing generation with Tier 3 Pre-Flight Impact Summary Modal, ad-hoc invoice creator, and unpaid charge cancellation modal with mandatory audit reason in `src/features/finance/billing/`.

Depends On: P1-S03, P5-S17

Repository: src/frontend

Completion Criteria:

1. Create components in `src/frontend/src/features/finance/billing/`:
   - `BillingWorkbenchView.vue`:
     - Filter toolbar: Academic Year, Billing Month, School Level, Grade, Status (`UNPAID`, `PARTIALLY_PAID`, `PAID`, `CANCELLED`).
     - Charges data table with amounts, discounts, net balance, and status chips.
     - "Generate Monthly Billing" button triggering batch workflow (`UC-FIN-03`).
     - "Create Ad-Hoc Charge" button (`UC-FIN-04`).
     - Row action: "Cancel Charge" (`UC-FIN-05`).
   - `BatchBillingModal.vue` & `PreFlightSummaryModal.vue` (`UC-FIN-03`):
     - Form selecting target month and cohort filters.
     - Fetches `/api/fin/charges/batch-preview` and presents Tier 3 Pre-Flight Modal showing student cohort size, total gross amount, total discounts, and prospective net total before commit.
   - `AdHocChargeModal.vue` (`UC-FIN-04`):
     - Form selecting student, charge type, description, unit price, quantity, and due date.
   - `ChargeCancellationModal.vue` (`UC-FIN-05`):
     - Tier 2 confirmation dialog requesting mandatory non-empty cancellation audit reason.
     - Disables cancellation if `paid_amount > 0` with an informative warning tooltip.
2. Author TanStack Query hooks in `api/useBillingQueries.ts` and mutations in `api/useBillingMutations.ts`.
3. Unit tests in `BillingWorkbenchView.spec.ts` pass.
4. `pnpm typecheck` and `pnpm lint` succeed.

Implementation Notes:
- Implemented Tuition Billing Workbench (`SCR-FIN-BILLING`) in `src/frontend/src/features/finance/billing/`:
  - API & Query Layer: Extended `api/keys.ts` (`billingKeys` with `billingMonth`, `chargeTypes`, and `studentSearch` keys), authored `api/billing.api.ts` consuming generated OpenAPI schema types (`StudentChargeDto`, `BatchBillingPreviewRequest`, `BatchBillingPreviewResponse`, `GenerateBatchTuitionChargesRequest`, `GenerateBatchTuitionChargesResponse`, `CreateAdHocChargeRequest`, `CancelStudentChargeRequest`, `ChargeTypeDto`, `StudentListItemDto`), `api/useBillingQueries.ts` (`useStudentChargesQuery`, `useActiveChargeTypesQuery`, `useBillingStudentSearchQuery`), and `api/useBillingMutations.ts` (batch preview, batch generation, ad-hoc charge creation, and charge cancellation with cache invalidation and PrimeVue Toast notifications).
  - Validation & Utilities: Authored Zod schemas (`schemas/billing.schema.ts`) aligned with backend FluentValidation rules for batch billing (`YYYY-MM` validation), ad-hoc charges (`unitAmount >= 0`, `quantity > 0`, `description <= 500`), and charge cancellation (`cancellationReason` required, max 500 chars), plus RFC 7807 field error extraction and `utils/formatters.ts` (IDR formatting, billing month labels, frequency labels, status pill intents).
  - UI Components:
    - `views/BillingWorkbenchView.vue`: Summary KPI strip (visible charges, net billed, collected payments, outstanding balance), filter toolbar (Academic Year, Billing Month, School Level, Grade, Status, Quick Search, Reset Filters), charges `AppDataTable` with gross/discount/net/paid/balance/due date/status chips, permission-gated actions (`Permissions.Finance.Billing.Generate`, `ManageAdHoc`, `Cancel`), and disabled "Cancel Charge" button with warning tooltip whenever `paidAmount > 0` or status is `PAID`/`CANCELLED`.
    - `components/BatchBillingModal.vue` & `components/PreFlightSummaryModal.vue` (`UC-FIN-03`): Cohort filter form calling `POST /api/fin/charges/batch-preview` and presenting the Tier 3 Pre-Flight Impact Summary Modal with student cohort size, prospective/skipped charge counts, gross total, discount total, net receivable total, and itemized line preview before committing `POST /api/fin/charges/generate-batch`.
    - `components/AdHocChargeModal.vue` (`UC-FIN-04`): Modal with placed student selector, active fee type selector (auto-populating unit price), description, unit price, quantity, optional billing month/due date, and live gross preview.
    - `components/ChargeCancellationModal.vue` (`UC-FIN-05`): Tier 2 audit confirmation dialog displaying charge summary, blocking cancellation when `paidAmount > 0`, and requiring a mandatory non-empty cancellation audit reason.
  - Tests: Authored 3 schema unit tests (`schemas/__tests__/billing.schema.spec.ts`) and 5 MSW-backed component tests (`views/__tests__/BillingWorkbenchView.spec.ts`) covering table/KPI rendering, `paidAmount > 0` cancellation disable + tooltip, unpaid charge cancellation with mandatory reason validation, monthly batch billing with Tier 3 Pre-Flight Modal, ad-hoc invoice creation, and permission gating.
- Verification:
  - `pnpm --dir src/frontend test:unit src/features/finance/billing/`: 2 test files, 8 tests passed.
  - `pnpm --dir src/frontend exec eslint src/features/finance/billing/ --max-warnings 0`: passed with 0 errors and 0 warnings.
  - `pnpm --dir src/frontend typecheck`: 0 errors in `src/features/finance/billing/` (1 unrelated parallel-slice error reported outside scope in `src/features/finance/payments/views/PaymentsWorkbenchView.vue(72,30)`).
  - `pnpm --dir src/frontend exec vite build`: production bundle built cleanly (`BillingWorkbenchView` chunk emitted).

Changed Files:
- `src/frontend/src/features/finance/billing/api/keys.ts`
- `src/frontend/src/features/finance/billing/api/billing.api.ts`
- `src/frontend/src/features/finance/billing/api/useBillingQueries.ts`
- `src/frontend/src/features/finance/billing/api/useBillingMutations.ts`
- `src/frontend/src/features/finance/billing/schemas/billing.schema.ts`
- `src/frontend/src/features/finance/billing/schemas/__tests__/billing.schema.spec.ts`
- `src/frontend/src/features/finance/billing/utils/formatters.ts`
- `src/frontend/src/features/finance/billing/components/PreFlightSummaryModal.vue`
- `src/frontend/src/features/finance/billing/components/BatchBillingModal.vue`
- `src/frontend/src/features/finance/billing/components/AdHocChargeModal.vue`
- `src/frontend/src/features/finance/billing/components/ChargeCancellationModal.vue`
- `src/frontend/src/features/finance/billing/views/BillingWorkbenchView.vue`
- `src/frontend/src/features/finance/billing/views/__tests__/BillingWorkbenchView.spec.ts`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P6-S23

Title: Implement Cashier Counter & Payment Recording Screen (`SCR-FIN-PAYMENTS`) & Browser Print Receipt

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement the frontline cashier counter, guardian contact search with WhatsApp link, multi-charge allocation, advance deposit holding and application, student ledger statement, and browser-printable payment receipt in `src/features/finance/payments/`.

Depends On: P1-S03, P5-S17

Repository: src/frontend

Completion Criteria:

1. Create components in `src/frontend/src/features/finance/payments/`:
   - `PaymentsWorkbenchView.vue`:
     - Live student search bar displaying student profile, grade, study group, and guardian WhatsApp direct link (`UC-FIN-10`).
     - Open charges ledger table showing outstanding amounts and due dates.
     - Historical payment receipts tab.
     - "Record Payment" button (`UC-FIN-06`).
     - "Apply Deposit" button (`UC-FIN-07`).
     - "View Statement" button opening ledger drawer (`UC-FIN-08`).
   - `RecordPaymentModal.vue` (`UC-FIN-06`):
     - Input for payment method (`CASH`, `BANK_TRANSFER`), bank reference, and payment amount.
     - Checkboxes to allocate payment across open charges.
     - Auto-calculates allocated sum and excess deposit amount held.
   - `DepositAllocationModal.vue` (`UC-FIN-07`):
     - Displays unallocated deposit balance and allows applying it to open charges.
   - `StudentStatementDrawer.vue` (`UC-FIN-08`):
     - Complete student financial history drawer with charge and payment breakdown.
   - `PaymentReceiptPrintView.vue`:
     - Official printable receipt layout (`KWT-YYYYMM-NNNN`) with school header, student info, itemized settled charges, remaining balance, and cashier signature with `@media print`.
2. Author TanStack Query hooks in `api/usePaymentQueries.ts` and mutations in `api/usePaymentMutations.ts`.
3. Unit tests in `PaymentsWorkbenchView.spec.ts` pass.
4. `pnpm typecheck` and `pnpm lint` succeed.

Implementation Notes:
- Implemented `src/frontend/src/features/finance/payments/api/payments.api.ts`, `api/usePaymentQueries.ts`, and `api/usePaymentMutations.ts` consuming generated OpenAPI contracts (`StudentBillingContactDto`, `StudentStatementDto`, `PaymentReceiptDto`, `PaymentReceiptDetailDto`, `RecordPaymentReceiptRequest`, `AllocateAdvanceDepositRequest`) and `paymentKeys` with cache invalidation and PrimeVue Toast feedback.
- Created `schemas/payments.schema.ts` (Zod validation schemas for payment receipt posting and advance deposit allocation, plus RFC 7807 error helpers) and `utils/formatters.ts` (IDR formatting, billing month formatting, payment method labels, charge status intents, and WhatsApp URL builder).
- Implemented `views/PaymentsWorkbenchView.vue` (`SCR-FIN-PAYMENTS`):
  - Permission-gated actions (`Permissions.Finance.Payments.Record`, `Permissions.Finance.Deposits.Allocate`, `Permissions.Finance.Statements.View`).
  - Debounced live student & guardian billing contact lookup (`/api/std/billing-lookup`, `UC-FIN-10`) displaying student profile, grade, study group, primary guardian contact, and direct WhatsApp links.
  - Financial KPI summary cards (`Total Billed`, `Total Paid`, `Outstanding Balance`, `Advance Deposit Balance`), Open Charges Ledger tab with quick `"Pay"` row action, and Historical Payment Receipts tab with `"Print Receipt"` action.
- Implemented `components/RecordPaymentModal.vue` (`UC-FIN-06`) supporting `CASH` and `BANK_TRANSFER` with bank reference, checkbox-based multi-charge allocation, auto-allocation helper, real-time allocated sum calculation, and excess advance deposit holding preview.
- Implemented `components/DepositAllocationModal.vue` (`UC-FIN-07`) listing source receipts holding unallocated deposits and allocating funds across open unpaid charges.
- Implemented `components/StudentStatementDrawer.vue` (`UC-FIN-08`) presenting complete student financial history (all charges including discounts/paid/cancelled, plus itemized receipts and allocations).
- Implemented `components/PaymentReceiptPrintView.vue` rendering an A4 portrait browser-printable official receipt (`KWT-YYYYMM-NNNN`) with school header, student metadata, itemized settled charges, deposit/balance summary, cashier signature blocks, and `@media print` CSS.
- Authored Vitest + MSW unit tests in `views/__tests__/PaymentsWorkbenchView.spec.ts` (5/5 tests passed covering permission gating, guardian billing lookup & WhatsApp links, multi-charge payment with advance deposit holding, deposit allocation, statement drawer, and browser print receipt).
- Verification:
  - `pnpm vitest run src/features/finance/payments/`: 1 test file, 5 tests passed.
  - `npx eslint src/features/finance/payments/ --max-warnings 0`: passed with 0 errors and 0 warnings.
  - `pnpm typecheck && pnpm build` (`vue-tsc --noEmit && vite build`): succeeded cleanly with exit code 0.

Changed Files:
- `src/frontend/src/features/finance/payments/api/payments.api.ts`
- `src/frontend/src/features/finance/payments/api/usePaymentQueries.ts`
- `src/frontend/src/features/finance/payments/api/usePaymentMutations.ts`
- `src/frontend/src/features/finance/payments/schemas/payments.schema.ts`
- `src/frontend/src/features/finance/payments/utils/formatters.ts`
- `src/frontend/src/features/finance/payments/components/RecordPaymentModal.vue`
- `src/frontend/src/features/finance/payments/components/DepositAllocationModal.vue`
- `src/frontend/src/features/finance/payments/components/StudentStatementDrawer.vue`
- `src/frontend/src/features/finance/payments/components/PaymentReceiptPrintView.vue`
- `src/frontend/src/features/finance/payments/views/PaymentsWorkbenchView.vue`
- `src/frontend/src/features/finance/payments/views/__tests__/PaymentsWorkbenchView.spec.ts`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

### P6-S24

Title: Implement Receivables Aging & Daily Cashier Reports Screen (`SCR-FIN-RECEIVABLES`)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Implement the tuition receivables summary KPI metrics, aged arrears table (Current, 30, 60, 90+ days), student drill-down statement drawer, and daily cashier collection summary report in `src/features/finance/receivables/`.

Depends On: P1-S03, P5-S17

Repository: src/frontend

Completion Criteria:

1. Create components in `src/frontend/src/features/finance/receivables/`:
   - `ReceivablesAgingView.vue`:
     - Tabbed report interface: "Receivables & Arrears" and "Daily Cashier Summary".
   - `ReceivablesTab.vue` (`UC-FIN-09`, `UC-ADM-26`, `UC-PRC-06`):
     - Metric cards: Total Billed, Total Collected, Total Outstanding, Arrears > 30 Days.
     - Aged arrears breakdown table categorized into Current, 1–30 Days, 31–60 Days, 61–90 Days, and 90+ Days.
     - Filter toolbar: Academic Year, School Level, Grade, Study Group.
     - Row action opening `StudentStatementDrawer` for student balance drill-down.
   - `DailyCashierTab.vue` (`UC-FIN-11`):
     - Date picker selector.
     - Summary KPI cards: Total Cash Collections, Total Bank Transfer Collections, Total Advance Deposits Held.
     - Itemized daily receipt register table with receipt number, student name, method, amount, and cashier name.
2. Author TanStack Query hooks in `api/useReceivableQueries.ts`.
3. Unit tests in `ReceivablesAgingView.spec.ts` pass.
4. `pnpm typecheck` and `pnpm lint` succeed.

Implementation Notes:
- Implemented Receivables Aging & Daily Cashier Reports (`SCR-FIN-RECEIVABLES`) in `src/frontend/src/features/finance/receivables/`:
  - API & Query Layer: Added `statement` query key to `api/keys.ts` (`receivableKeys`), authored `api/receivables.api.ts` consuming generated OpenAPI types (`ReceivablesSummaryDto`, `ReceivablesAgingDto`, `DailyCashierSummaryDto`, `StudentStatementDto`) for `/api/fin/reports/receivables-summary`, `/api/fin/reports/receivables-aging`, `/api/fin/reports/daily-cashier`, and `/api/fin/students/{studentId}/statement`, and authored TanStack Query hooks in `api/useReceivableQueries.ts` (`useReceivablesSummaryQuery`, `useReceivablesAgingQuery`, `useDailyCashierSummaryQuery`, `useStudentStatementQuery`).
  - Formatting & Filter Helpers: Authored `utils/formatters.ts` for IDR currency formatting, billing month labels, payment method labels, charge status pill intents, collection rate calculation, and school level/grade options.
  - UI Components:
    - `views/ReceivablesAgingView.vue`: Permission-guarded (`Permissions.Finance.Reports.View`) tabbed reporting workspace with `"Receivables & Arrears"` and `"Daily Cashier Summary"` tabs and context badge.
    - `components/ReceivablesTab.vue` (`UC-FIN-09`, `UC-ADM-26`, `UC-PRC-06`): Filter toolbar (Academic Year, School Level, Grade, Study Group, Aging As-Of Date, Student Search, Reset Filters), 4 KPI metric cards (`Total Billed`, `Total Collected`, `Total Outstanding`, `Arrears > 30 Days`), school level breakdown cards, aging bucket summary strip, and aged arrears `AppDataTable` (`Current`, `1–30 Days`, `31–60 Days`, `61–90 Days`, `90+ Days`, `Total Outstanding`) with student statement drill-down action (`Permissions.Finance.Statements.View`).
    - `components/StudentStatementDrawer.vue`: Slide-over student financial statement drawer displaying student & aging context, 4 KPI cards (`Total Billed`, `Total Paid`, `Outstanding`, `Deposit Held`), charges table, and payment receipts allocation breakdown.
    - `components/DailyCashierTab.vue` (`UC-FIN-11`): Collection date picker with Today & Refresh controls, receipt search filter, 3 KPI summary cards (`Total Cash Collections`, `Total Bank Transfer Collections`, `Total Advance Deposits Held`), reconciliation summary strip, and itemized daily receipt register `AppDataTable` (`Receipt Number`, `Student`, `Method`, `Amount`, `Allocated / Deposit`, `Cashier`).
  - Tests: Authored 6 MSW-backed Vitest component tests in `views/__tests__/ReceivablesAgingView.spec.ts` covering restricted access, KPI cards & aged arrears table, school level & search filtering, `StudentStatementDrawer` drill-down, Principal/Admin statement permission gating, and Daily Cashier Summary date switching & receipt search.
- Verification:
  - `pnpm --dir src/frontend test:unit src/features/finance/receivables/views/__tests__/ReceivablesAgingView.spec.ts`: 1 test file, 6 tests passed.
  - `pnpm --dir src/frontend exec eslint "src/features/finance/receivables/**/*.{ts,vue}" --max-warnings 0` and `pnpm --dir src/frontend lint`: passed with 0 errors and 0 warnings.
  - `pnpm --dir src/frontend typecheck` (`vue-tsc --noEmit`): passed with exit code 0.
  - `pnpm --dir src/frontend exec vite build`: succeeded cleanly.

Changed Files:
- `src/frontend/src/features/finance/receivables/api/keys.ts`
- `src/frontend/src/features/finance/receivables/api/receivables.api.ts`
- `src/frontend/src/features/finance/receivables/api/useReceivableQueries.ts`
- `src/frontend/src/features/finance/receivables/utils/formatters.ts`
- `src/frontend/src/features/finance/receivables/components/StudentStatementDrawer.vue`
- `src/frontend/src/features/finance/receivables/components/ReceivablesTab.vue`
- `src/frontend/src/features/finance/receivables/components/DailyCashierTab.vue`
- `src/frontend/src/features/finance/receivables/views/ReceivablesAgingView.vue`
- `src/frontend/src/features/finance/receivables/views/__tests__/ReceivablesAgingView.spec.ts`
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

## P7 - Live Verification & End-to-End System Validation

Implementation Status: IMPLEMENTED  
Review Status: GO

### P7-S25

Title: Live Multi-Role System Verification & E2E Validation Across All 28 Stage 3 Use Cases

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective: Execute full end-to-end multi-role live system verification across the monorepo stack, validating all 28 Stage 3 use cases, browser print workflows, and role authorization boundaries against local SQL Server.

Depends On: P6-S18, P6-S19, P6-S20, P6-S21, P6-S22, P6-S23, P6-S24

Repository: src/frontend

Completion Criteria:

1. Start local backend API and frontend dev server against seeded database (`Stage3Seeder`).
2. Execute live verification across all 5 operational roles:
   - **`admin` (`SCHOOL_ADMIN`):**
     - Fee Policy: Create fee structure and activate it; verify 409 Conflict on attempt to edit active fee (`UC-FIN-01`).
     - Discounts: Grant sibling discount to student (`UC-FIN-02`).
     - Billing: Execute batch tuition billing with Tier 3 Pre-Flight Modal preview and commit (`UC-FIN-03`).
     - Report Cards: Batch-generate draft report cards for study group 7A (`UC-ADM-19`).
     - Unlock Report Card: Override finalized card with mandatory audit reason (`UC-ADM-20`).
     - Extracurricular: Create club and enroll student participant (`UC-ADM-25`).
   - **`finance.officer` (`FINANCE_OFFICER`):**
     - Ad-Hoc Charge: Create individual manual invoice (`UC-FIN-04`).
     - Cancellation: Cancel unpaid charge with mandatory reason; verify cancellation rejected if payments exist (`UC-FIN-05`).
     - Cashier Counter: Look up student with guardian WhatsApp direct link (`UC-FIN-10`).
     - Cashier Payment: Record multi-method payment (`CASH`, `BANK_TRANSFER`) generating sequential `KWT-YYYYMM-NNNN` receipt, verify multi-charge allocation and deposit holding (`UC-FIN-06`).
     - Deposit Application: Apply held deposit balance to open charge (`UC-FIN-07`).
     - Reports: Verify student financial statement (`UC-FIN-08`), receivables aging breakdown (`UC-FIN-09`), and daily cashier collection summary reconciled by payment method (`UC-FIN-11`).
   - **`teacher.budi` (`TEACHER`):**
     - Report Cards: Submit subject feedback notes for assigned subject stage on draft card (`UC-TCH-04`).
     - Pastoral Care: Record positive and negative student behaviour observation notes (`UC-TCH-06`).
     - Discipline: Record classroom disciplinary incident with verbal/written warning (`UC-TCH-07`, `UC-ADM-22`).
     - Achievements: Record student achievement award (`UC-TCH-08`, `UC-ADM-23`).
     - Extracurricular: Log club meeting activity notes for supervised club (`UC-TCH-09`).
     - Dossier: View 360° student development dossier for taught students.
   - **`homeroom.teacher` (`HOMEROOM_TEACHER`):**
     - Report Cards: Review study group 7A report cards (`UC-HRM-01`).
     - Homeroom Remarks: Write holistic narrative general remarks (`UC-HRM-02`).
     - Finalize: Finalize study group report cards (`UC-HRM-03`); verify cards become read-only and trigger `ReportCardFinalizedDomainEvent` updating placement completion (`GAP-014`).
   - **`principal` (`PRINCIPAL`):**
     - Report Cards: Review draft and finalized cards across study groups (`UC-PRC-02`).
     - Discipline Escalation: Issue escalated sanctions (`PARENT_MEETING`, `SUSPENSION`) (`UC-PRC-03`).
     - Student Dossier: Monitor school-wide 360° student development timeline (`UC-PRC-04`).
     - Financial Oversight: Inspect tuition receivables and daily cashier summary (`UC-PRC-06`).
3. Verify Official Print Views:
   - Report card browser print view renders cleanly on A4 portrait without clipping.
   - Cashier payment receipt browser print view renders cleanly with receipt number, allocation breakdown, and cashier signature.
4. Verify Full CI Automation:
   - `dotnet test src/backend/ElsasaSis.sln` passes with zero errors.
   - `pnpm typecheck`, `pnpm lint`, and `pnpm test:unit` in `src/frontend` pass with zero errors.

Implementation Notes:
- **Database & Live Server Stack Startup**:
  - Executed `ElsasaSis.Database` in `Development` mode against local SQL Server (`Server=JUDE7;Database=ElsasaSis`), verifying idempotent DbUp migrations (`001`–`101`) and `Stage3Seeder` fixtures completed with exit code `0`.
  - Started `ElsasaSis.Api` (`Development` mode, `http://127.0.0.1:5055`) and Vite dev server (`http://localhost:5174`, `VITE_API_PROXY_TARGET=http://127.0.0.1:5055`).
  - Verified OpenAPI contract synchronization via `pnpm --dir src/frontend exec openapi-typescript http://127.0.0.1:5055/swagger/v1/swagger.json -o src/shared/types/api/schema.d.ts --check` (exit code `0`, zero drift).
- **Live Multi-Role E2E Verification Across All 28 Stage 3 Use Cases & Authorization Boundaries (via Vite Proxy)**:
  - **`admin` (`SCHOOL_ADMIN`)**:
    - `UC-FIN-01`: Created `DRAFT` charge type (`Uji Lab Komputer`), updated amount, activated (`POST /api/fin/charge-types/{id}/activate`), and verified `409 Conflict` when attempting to edit an `ACTIVE` charge type.
    - `UC-FIN-02`: Granted 25% standing discount to `Siti Nurhaliza`.
    - `UC-FIN-03`: Executed Tier 3 Pre-Flight billing preview (`POST /api/fin/charges/batch-preview` — 4 charges, net `300,000`), committed batch (`POST /api/fin/charges/generate-batch` — 4 generated), and verified idempotent re-run (`0` generated, `52` skipped).
    - `UC-ADM-19`: Executed batch draft report card generation for study group `7A` (`POST /api/acd/report-cards/generate-batch`).
    - `UC-ADM-20`: Verified empty override reason returns `422 UnprocessableEntity`; unlocked `FINALIZED` report card with mandatory audit reason (`FINALIZED -> DRAFT`).
    - `UC-ADM-25`: Created extracurricular club (`Klub Sains E2E`) with supervisor `Budi Santoso` and enrolled `Siti Nurhaliza`.
    - Boundary verified: `POST /api/dev/discipline/escalated` rejected with `403 Forbidden`.
  - **`finance.officer` (`FINANCE_OFFICER`)**:
    - `UC-FIN-04` & `UC-FIN-05`: Created ad-hoc charges, verified empty cancellation reason rejected (`422`), cancelled unpaid charge with mandatory audit reason (`200 OK`, `CANCELLED`), and verified cancelling a paid charge returns `409 Conflict`.
    - `UC-FIN-10`: Verified `/api/std/billing-lookup` returns student billing profiles with primary guardian contact and `https://wa.me/62...` WhatsApp link.
    - `UC-FIN-06`: Recorded `CASH` payment (`KWT-202611-0017`, multi-charge allocation `60,000` + `40,000` excess advance deposit) and `BANK_TRANSFER` payment (`KWT-202611-0018`, partial `20,000` with bank reference).
    - `UC-FIN-07`: Allocated `40,000` held advance deposit (`POST /api/fin/deposits/allocate`) to settle remaining open charge balance (`unallocatedAmount -> 0`).
    - `UC-FIN-08`, `UC-FIN-09`, `UC-FIN-11`: Verified student financial ledger statement, receivables summary & aged arrears breakdown, and daily cashier collection summary reconciled by `CASH` and `BANK_TRANSFER`.
    - Boundary verified: `finance.officer` rejected (`403 Forbidden`) on academic report cards and pastoral behaviour recording.
  - **`teacher.budi` (`TEACHER` & `HOMEROOM_TEACHER`)**:
    - `UC-TCH-04`: Submitted Mathematics subject feedback note on `Siti Nurhaliza`'s `DRAFT` report card.
    - `UC-TCH-06`: Recorded both `POSITIVE` and `NEGATIVE` student behaviour observation notes.
    - `UC-TCH-07` & `UC-ADM-22`: Recorded classroom `WRITTEN_WARNING` (`201 Created`), verified `SUSPENSION` rejected (`422`) on `/api/dev/discipline`, and verified `/api/dev/discipline/escalated` rejected (`403 Forbidden`).
    - `UC-TCH-08` & `UC-ADM-23`: Recorded `SCIENCE` province-level student achievement award (`201 Created`).
    - `UC-TCH-09`: Recorded extracurricular session meeting log as assigned supervisor (`200 OK`).
    - `UC-ADM-24` & `UC-HRM-04`: Verified 360° student development dossier (`GET /api/dev/students/{id}/dossier`) returned composite timeline (`31` chronological entries).
    - `UC-HRM-01`, `UC-HRM-02`, `UC-HRM-03`, `GAP-014`: Reviewed study group `7A` homeroom report cards, saved holistic remarks on `Ahmad Dahlan`'s `DRAFT` card, finalized card (`DRAFT -> FINALIZED`), verified read-only enforcement (`409 Conflict` on subsequent remark edit), and confirmed `std.student_placements.is_academically_completed = 1` in SQL Server via `sqlcmd`.
    - Boundary verified: `teacher.budi` rejected (`403 Forbidden`) on `POST /api/fin/charge-types`.
  - **`principal` (`PRINCIPAL`)**:
    - `UC-PRC-02`: Reviewed school-wide report cards overview and printable card detail.
    - `UC-PRC-03`: Issued escalated `PARENT_MEETING` disciplinary sanction (`201 Created`).
    - `UC-PRC-04`: Inspected school-wide development overview and 360° student dossier.
    - `UC-PRC-06` & `UC-ADM-26`: Inspected receivables summary, aged arrears, and daily cashier collection report.
    - Boundary verified: `principal` rejected (`403 Forbidden`) on report card finalization, behaviour recording, and charge-type creation.
- **Headless Chromium Browser E2E & `@media print` Verification**:
  - Executed real headless Chromium CDP verification across all 4 seeded accounts (`admin`, `finance.officer`, `teacher.budi`, `principal`) on `http://localhost:5174`:
    - Verified role-filtered sidebar navigation visibility per persona (e.g., `Cashier Counter` hidden from `admin`; `Academic`/`Pastoral` hidden from `finance.officer`; `Finance` hidden from `teacher.budi`; `Fee Policies`/`Tuition Billing` hidden from `principal`) and verified direct navigation by `principal` to `/finance/billing` redirects to `/403` (`Access Restricted`).
    - Verified all Stage 3 feature views render cleanly with zero browser console errors, zero uncaught page errors, and zero HTTP `5xx` responses.
    - Verified `PaymentReceiptPrintView.vue` under `@media print` emulation and `Page.printToPDF` (A4 portrait, `284,628` bytes): sidebar and `.no-print` toolbar hidden (`display: none`), receipt number `KWT-202611-0017` and cashier signature rendered cleanly with no horizontal clipping (`scrollWidth <= clientWidth`).
    - Verified `ReportCardPrintView.vue` under `@media print` emulation and `Page.printToPDF` (A4 portrait, `74,727` bytes): sidebar and `.no-print` toolbar hidden (`display: none`), `Ahmad Dahlan` official semester report card rendered cleanly with no horizontal clipping (`scrollWidth <= clientWidth`).
  - Cleanly terminated temporary `ElsasaSis.Api` and Vite dev server background processes and verified ports `5055` and `5174` released.
- **Full CI Automation Verification**:
  - `dotnet test src/backend/ElsasaSis.sln` passed with zero failures:
    - `ElsasaSis.UnitTests`: 739 passed, 0 failed, 0 skipped.
    - `ElsasaSis.ArchitectureTests`: 19 passed, 0 failed, 0 skipped.
    - `ElsasaSis.IntegrationTests`: 53 passed, 0 failed, 0 skipped.
  - `src/frontend` quality gates passed with zero errors:
    - `pnpm typecheck` (`vue-tsc --noEmit`): passed (exit code `0`).
    - `pnpm lint` (`eslint . --max-warnings 0`): passed (0 errors, 0 warnings).
    - `pnpm test:unit` (`vitest run`): 51 test files passed, 320 tests passed, 0 failed.
    - `pnpm build` (`vue-tsc --noEmit && vite build`): production bundle built cleanly (exit code `0`).

Changed Files:
- `docs/issues/IS004- STAGE-3-DEV-IMPLEMENTATION-PLAN.md`

---

# 6. Change Log

- 2026-11-20: Initial release of `CR-STAGE-3` Implementation Plan. Version `1.0`. Granted `Execution Approval: APPROVED` by Architect. Plan structure is finalized and immutable for autonomous execution by `ica-developer`.
