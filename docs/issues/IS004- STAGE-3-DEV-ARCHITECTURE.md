---
Title: Stage 3 Evaluation, Finance & Student Development — Target Architecture
Code: CR-STAGE-3
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-20
---

# 1. Overview

This document defines the authoritative target architecture for **CR-STAGE-3** (**Stage 3: Evaluation, Finance & Student Development**) of **ELSASA-SIS v4 (Basic Level)**. It realizes the change request captured in [IS004- STAGE-3-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS004-%20STAGE-3-DEV-ISSUE.md) and consumes all decisions established in the Feasibility Assessment [IS004- STAGE-3-DEV-FEASIBILITY-ASSESSMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS004-%20STAGE-3-DEV-FEASIBILITY-ASSESSMENT.md).

Building upon the foundations of Stage 0 (architecture skeleton, authentication, audit), Stage 1 (operational periods, rooms, calendar, announcements, students with NIS, study groups, teacher profiles, and teaching assignments), and Stage 2 (master timetables, curriculum hierarchy, daily classroom attendance, and teacher scorebooks), Stage 3 activates three primary operational engines:

1. **Semester Report Cards & Academic Evaluation (`acd`):** Collaborative end-of-semester evaluation workflows, idempotent batch generation of draft semester report cards from scorebook aggregates and attendance metrics, subject teacher qualitative feedback, homeroom teacher holistic remarks, administrative override/unlock with audit reason, principal review, homeroom finalization locking, and browser-printable official report cards (`UC-ADM-19`, `UC-ADM-20`, `UC-TCH-04`, `UC-HRM-01`, `UC-HRM-02`, `UC-HRM-03`, `UC-PRC-02`).
2. **Pastoral Care, Student Discipline & Extracurricular Activities (`dev`):** Unified 360-degree holistic student development dossier tracking teacher behavior observation notes, classroom incidents, administrative disciplinary infractions, principal-level escalated disciplinary sanctions (`PARENT_MEETING`, `SUSPENSION`), student academic and non-academic achievement awards, extracurricular activities catalog management, and club attendance/session activity logging (`UC-TCH-06`, `UC-TCH-07`, `UC-ADM-22`, `UC-PRC-03`, `UC-TCH-08`, `UC-ADM-23`, `UC-ADM-24`, `UC-HRM-04`, `UC-PRC-04`, `UC-ADM-25`, `UC-TCH-09`).
3. **Fee Structures, Tuition Billing & Frontline Cashier Counter (`fin`):** Institutional fee policies and charge types (monthly tuition / SPP, uniforms, materials), student discount and scholarship management, periodic batch student tuition charge generation, ad-hoc invoice adjustments, billing cancellation for unpaid erroneous charges, frontline cashier counter recording multi-method payments (`CASH`, `BANK_TRANSFER`), itemized charge allocation, advance deposit holding, student financial balance ledgers, guardian billing contact lookup (`std`), and daily cashier collection summaries (`UC-FIN-01`, `UC-FIN-02`, `UC-FIN-03`, `UC-FIN-04`, `UC-FIN-05`, `UC-FIN-06`, `UC-FIN-07`, `UC-FIN-08`, `UC-FIN-09`, `UC-FIN-10`, `UC-FIN-11`, `UC-ADM-26`, `UC-PRC-06`).

Stage 3 realizes exactly **28 actor-driven use cases** across **7 frontend feature submodules** (6 primary interactive screens + receivables monitoring), introduces **2 new backend module assemblies** (`ElsasaSis.Modules.Fin`, `ElsasaSis.Modules.Dev`), expands `ElsasaSis.Modules.Acd` and `ElsasaSis.Modules.Std`, and introduces **7 database migration scripts** defining 11 relational tables and 4 schema extensions.

---

# 2. Architectural Basis

## Business Context

Business concepts, domain invariants, actors, use cases, and screen ergonomics realized by Stage 3 are governed by the upstream product-definition specifications:

- **Domain Knowledge & Invariants:**
  - [academic-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/academic-domain-basic.md) (`ReportCard`, `ReportCardEntry`, `BR-RPCD-001`..`BR-RPCD-004`, `EXT-ACD-001`..`EXT-ACD-003`).
  - [development-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/development-domain-basic.md) (`BehaviourNote`, `DisciplineRecord`, `ExtracurricularActivity`, `ExtracurricularParticipation`, `Achievement`, `BR-DEV-001`..`BR-DEV-014`).
  - [finance-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/finance-domain-basic.md) (`ChargeType`, `StudentDiscount`, `StudentCharge`, `PaymentReceipt`, `PaymentAllocation`, `BR-FIN-001`..`BR-FIN-012`).
  - [student-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/student-domain-basic.md) (`Student`, `StudentPlacement`, `StudyGroup`, `Guardian`).
  - [teaching-domain-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/domains/teaching-domain-basic.md) (`Teacher`, `TeacherAssignment`).
- **Actors & Roles:** [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md) (`SCHOOL_ADMIN`, `FINANCE_OFFICER`, `TEACHER`, `HOMEROOM_TEACHER`, `PRINCIPAL`).
- **Use Case Taxonomy:** [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (28 use cases across `acd`, `dev`, `fin`, and `std`).
- **Screens & UI/UX Standards:** [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) and [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md) (§2.5 Navigation Taxonomy, §3 Screen Patterns, §6 Interaction Patterns, §8 Feedback & System States).

## Analysis Input

All architectural decisions (`GAP-001` through `GAP-016` and `OQ-001` through `OQ-032`) were formally evaluated and closed in the Feasibility Assessment:

- **Feasibility Assessment:** [IS004- STAGE-3-DEV-FEASIBILITY-ASSESSMENT.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS004-%20STAGE-3-DEV-FEASIBILITY-ASSESSMENT.md) (`CR-STAGE-3`, `Status: READY-FOR-PLANNING`).
- **Base Architecture & Technical Standards:**
  - [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md) (Modular Monolith, Schemas, Use Case Mapping).
  - [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md) (Modular Monolith, VSA, Clean DDD, Dapper Pure Persistence, RFC 7807).
  - [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md) (Vue 3, TanStack Query, Pinia, VeeValidate + Zod, PrimeVue Aura).
  - [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md) (§3.9 `report_cards`, §3.10 `report_card_entries`).
  - [development-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/development-tables-design.md) (§3.1 `behaviour_notes`, §3.2 `discipline_records`, §3.3 `extracurricular_activities`, §3.4 `extracurricular_participations`, §3.5 `achievements`).
  - [finance-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/finance-tables-design.md) (§3.1 `charge_types`, §3.2 `student_discounts`, §3.3 `student_charges`, §3.4 `payment_receipts`, §3.5 `payment_allocations`).

```text
DOMAINS + ROADMAP + GUIDELINES (docs/architecture/)
                      +
       IS004 FEASIBILITY-ASSESSMENT
                      ↓
           CR-STAGE-3-ARCHITECTURE
```

---

# 3. Scope

## Included

Stage 3 realizes exactly **28 actor-driven use cases** across **7 frontend feature submodules** and supporting backend assemblies:

1. **Two New Backend Module Assemblies:**
   - `ElsasaSis.Modules.Fin`: New C# class library at `src/backend/src/Modules/ElsasaSis.Modules.Fin/` (`Domain/`, `Infrastructure/`, `Features/`, `Subscribers/`).
     - Domain Aggregates: `ChargeType`, `StudentDiscount`, `StudentCharge`, `PaymentReceipt` (with child `PaymentAllocation`).
     - Vertical Slices: Fee policies, discount management, batch billing, ad-hoc charges, charge cancellation, payment receipt recording & allocation, advance deposit allocation, student statement query, receivables summary query, receivables aging query, daily cashier summary query.
     - Event Subscribers: `StudentPlacedEventHandler`, `CandidateEnrolledEventHandler`.
   - `ElsasaSis.Modules.Dev`: New C# class library at `src/backend/src/Modules/ElsasaSis.Modules.Dev/` (`Domain/`, `Infrastructure/`, `Features/`).
     - Domain Aggregates: `BehaviourNote`, `DisciplineRecord`, `ExtracurricularActivity` (with child `ExtracurricularParticipation`), `Achievement`.
     - Vertical Slices: Behaviour notes, classroom discipline, administrative discipline, principal escalated discipline, student achievements, extracurricular catalog management, extracurricular membership enrollment, extracurricular session logging, 360-degree student development dossier query, school-wide development monitoring query.
2. **Extensions to Existing Backend Modules:**
   - `ElsasaSis.Modules.Acd`: Implement report card vertical slices under `Features/ReportCards/`:
     - `GenerateDraftReportCards` (`POST /api/acd/report-cards/generate-batch` — `UC-ADM-19`).
     - `OverrideReportCardStatus` (`POST /api/acd/report-cards/{id}/override` — `UC-ADM-20`).
     - `ProvideSubjectReportCardNotes` (`POST /api/acd/report-cards/{id}/subject-notes` — `UC-TCH-04`).
     - `GetHomeroomReportCards` (`GET /api/acd/report-cards/homeroom` — `UC-HRM-01`).
     - `WriteHomeroomRemarks` (`POST /api/acd/report-cards/{id}/homeroom-remarks` — `UC-HRM-02`).
     - `FinalizeHomeroomReportCards` (`POST /api/acd/report-cards/{id}/finalize` — `UC-HRM-03`).
     - `ReviewAllReportCards` (`GET /api/acd/report-cards/overview` — `UC-PRC-02`).
     - `GetReportCardById` (`GET /api/acd/report-cards/{id}` — Printable DTO).
     - Domain Aggregate Root: `ReportCard` (with child `ReportCardEntry`).
   - `ElsasaSis.Modules.Std`:
     - Read Slice: `LookupStudentBillingContacts` (`GET /api/std/billing-lookup` — `UC-FIN-10`).
     - Event Subscriber: `ReportCardFinalizedEventHandler` (sets `is_academically_completed = 1` on `std.student_placements`).
     - Contract Implementation: Expand `IStudentLookupContract` to add `GetActiveStudentsByScopeAsync(...)`.
   - `ElsasaSis.Contracts`:
     - Interfaces: `IFinanceModule`, `IDevelopmentModule`.
     - Domain Event Records: `PaymentReceiptPostedDomainEvent`, `SanctionImposedDomainEvent`.
     - Expand `IStudentLookupContract`: `Task<IReadOnlyList<StudentPlacementScopeDto>> GetActiveStudentsByScopeAsync(...)`.
3. **Database Migrations (`ElsasaSis.Database`):**
   - `032_AlterAcdReportCardsAddOverrideReason.sql`: Add `override_reason NVARCHAR(500) NULL` and `general_remarks NVARCHAR(1000) NULL` to `acd.report_cards`.
   - `050_CreateFinTables.sql`: Create `fin.charge_types`, `fin.student_discounts`, `fin.student_charges`, `fin.payment_receipts`, `fin.payment_allocations`.
   - `051_CreateFinReceiptSequences.sql`: Create `fin.receipt_sequences` (`month CHAR(7) PK`, `next_seq INT`).
   - `052_AlterFinStudentChargesAddCancellation.sql`: Add `cancellation_reason NVARCHAR(500) NULL`, `cancelled_at DATETIME2(3) NULL` to `fin.student_charges`.
   - `060_CreateDevTables.sql`: Create `dev.behaviour_notes`, `dev.discipline_records`, `dev.extracurricular_activities`, `dev.extracurricular_participations`, `dev.achievements`.
   - `061_AlterDevExtracurricularParticipationsAddSessionLog.sql`: Add `last_session_log NVARCHAR(1000) NULL`, `last_session_date DATE NULL` to `dev.extracurricular_participations`.
   - `070_AlterStdStudentPlacementsAddAcademicCompletion.sql`: Add `is_academically_completed BIT NOT NULL` to `std.student_placements`.
   - `101_SeedStage3Data.sql`: Deterministic test data for local development and QA testing.
4. **Frontend Workspaces (7 Feature Submodules):**
   - `src/features/academic/report-cards/` (`SCR-ACD-REPORT-CARDS` at `/academic/report-cards`): Collaborative report card workspace with study group selector, student card list, subject grade table, teacher note editor, homeroom narrative remark editor, finalization locking, administrative override dialog, and browser-printable view (`ReportCardPrintView.vue`).
   - `src/features/development/pastoral/` (`SCR-DEV-PASTORAL` at `/development/pastoral`): 360-degree student development dossier, timeline of behaviour notes, disciplinary infractions with sanction tiers, and achievements.
   - `src/features/development/extracurricular/` (`SCR-DEV-EXTRACURRICULAR` at `/development/extracurricular`): Activity catalog, supervisor assignment, student enrollments, and session log recorder.
   - `src/features/finance/fee-policy/` (`SCR-FIN-FEES-POLICY` at `/finance/fee-policy`): Fee catalog, recurring frequencies (`MONTHLY`, `SEMESTER`, `YEARLY`, `ONE_TIME`), grade-level standard amounts, and student standing discount/scholarship assignments.
   - `src/features/finance/billing/` (`SCR-FIN-BILLING` at `/finance/billing`): Monthly batch tuition billing generator with Tier 3 pre-flight preview, ad-hoc individual invoice creator, and unpaid charge cancellation modal.
   - `src/features/finance/payments/` (`SCR-FIN-PAYMENTS` at `/finance/payments`): Frontline cashier counter, guardian contact search, payment receipt recording (`CASH`, `BANK_TRANSFER`), multi-charge allocation, advance deposit holding/application, student ledger statement, and browser-printable receipt (`PaymentReceiptPrintView.vue`).
   - `src/features/finance/receivables/` (`SCR-FIN-RECEIVABLES` at `/finance/receivables`): Receivables summary, aged arrears breakdown, and daily collection reports for finance officers and school leadership.
5. **Authorization & Security:**
   - Compile-time permissions added in `ElsasaSis.Modules.Auth`:
     - Report Cards: `Permissions.Academic.ReportCards.View`, `Permissions.Academic.ReportCards.Generate`, `Permissions.Academic.ReportCards.ProvideNotes`, `Permissions.Academic.ReportCards.Remarks`, `Permissions.Academic.ReportCards.Finalize`, `Permissions.Academic.ReportCards.Override`.
     - Development: `Permissions.Development.Behaviour.Record`, `Permissions.Development.Discipline.Record`, `Permissions.Development.Discipline.Escalate`, `Permissions.Development.Achievements.Record`, `Permissions.Development.Extracurricular.Manage`, `Permissions.Development.Extracurricular.Log`, `Permissions.Development.Dossier.View`.
     - Finance: `Permissions.Finance.FeePolicy.Manage`, `Permissions.Finance.Discounts.Manage`, `Permissions.Finance.Billing.Generate`, `Permissions.Finance.Billing.ManageAdHoc`, `Permissions.Finance.Billing.Cancel`, `Permissions.Finance.Payments.Record`, `Permissions.Finance.Deposits.Allocate`, `Permissions.Finance.Statements.View`, `Permissions.Finance.Reports.View`.

## Excluded

The following capabilities belong to Intermediate/Enterprise levels or future stages and are strictly excluded from Stage 3:

- **Automated Online Payment Gateway:** No Midtrans, Xendit, or DOKU integration. Frontline manual cashier counter (`CASH`, `BANK_TRANSFER` receipt posting) only.
- **Automated Dunning Engine & Direct Debit:** No SMS/WhatsApp automated notification pipelines or automated bank debit schedulers.
- **Cross-Student Credit Transfers:** Excess deposits cannot be transferred between different student accounts.
- **Formal Counseling & Safeguarding Case Management:** No psychiatric/counseling workflow pipelines or sealed psychological records (`dev.counseling_cases` is excluded from Basic).
- **Automated PDF Engine:** No backend headless Chrome/Puppeteer/wkhtmltopdf dependencies. Printable report cards and payment receipts render via clean frontend views using standard browser `window.print()` and `@media print` CSS.
- **Background Scheduler Daemon:** Batch billing and report card generation run synchronously within standard HTTP request budgets for Basic level.

---

# 4. Technical Decisions

This section realizes all 16 GAP decisions and 32 Open Question resolutions as concrete technical specifications:

### 4.1 Module Assembly & Clean DDD Boundary (`GAP-006`, `GAP-007`, `GAP-008`, `GAP-015`)
- Create `ElsasaSis.Modules.Fin` and `ElsasaSis.Modules.Dev` targeting `net8.0` in `src/backend/src/Modules/`.
- Maintain strict 3-folder anatomy inside each module:
  - `Domain/`: Pure Aggregate Roots, Entities, Repository Interfaces, Domain Events, Domain Errors. Zero external dependencies.
  - `Infrastructure/Persistence/`: Dapper implementations of repositories, database connection factory, SQL queries.
  - `Features/`: Vertical Slices organized by feature area (`FeePolicy/`, `Billing/`, `Payments/`, `Receivables/` in `Fin`; `Behaviour/`, `Discipline/`, `Extracurricular/`, `Achievements/`, `Dossier/` in `Dev`).
- Strict module reference discipline: Modules reference strictly `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`. NetArchTest asserts zero cross-module references among `Auth`, `Opr`, `Std`, `Tch`, `Acd`, `Fin`, and `Dev`.
- Register `FinModuleAssembly` and `DevModuleAssembly` in `ElsasaSis.ArchitectureTests` and verify boundary rules.

### 4.2 Report Card Evaluative Engine (`GAP-001`, `OQ-001`–`OQ-006`, `OQ-017`, `OQ-023`, `OQ-025`)
- **Arithmetic Final Grade Calculation (`OQ-001`):** `GenerateDraftReportCards` computes the final grade for each student per subject stage in the target semester as the simple arithmetic average of all assignment scores:
  $$\text{Final Grade} = \text{ROUND}(\text{AVG}(\text{score}), 2)$$
  If no assignment scores exist for that student in that subject stage, `final_grade = 0.00`.
- **Letter Grade (`OQ-002`):** `letter_grade` column on `acd.report_card_entries` remains `NULL` on draft generation. Manual grade conversion scales are deferred.
- **Idempotent Batch Generation (`OQ-003`):** `POST /api/acd/report-cards/generate-batch` accepts `{ studyGroupId: Guid, operationalPeriodId: Guid }`. It queries placed students in the study group, skips any student who already has a `DRAFT` or `FINALIZED` report card for that operational period, inserts draft headers and entry lines for the remaining students, and returns `{ generatedCount: int, skippedCount: int }`.
- **Subject Teacher Remarks (`OQ-004`):** `POST /api/acd/report-cards/{id}/subject-notes` accepts `{ subjectStageId: Guid, teacherNote: string }`. Handler validates:
  1. Target report card is in `DRAFT` status.
  2. Caller has a valid teaching assignment in `tch.teacher_assignments` for that subject stage and study group in the active period.
  3. Updates `teacher_notes`, `teacher_id`, `teacher_name` on the matching `acd.report_card_entries` row. Overwrites previous note.
- **Homeroom General Remarks (`UC-HRM-02`):** `POST /api/acd/report-cards/{id}/homeroom-remarks` accepts `{ generalRemarks: string }`. Handler validates `DRAFT` status and updates `general_remarks` on `acd.report_cards`.
- **Homeroom Finalization (`OQ-005`):** `POST /api/acd/report-cards/{id}/finalize` finalizes one card at a time. Transitions status `DRAFT -> FINALIZED`, sets `finalized_at = DateTime.UtcNow`, and raises `ReportCardFinalizedDomainEvent`.
- **Administrative Override/Unlock (`GAP-001`, `OQ-006`, `OQ-017`):** `POST /api/acd/report-cards/{id}/override` accepts `{ overrideReason: string }`. Enforces `overrideReason` is non-empty. Transitions `FINALIZED -> DRAFT`, stores `override_reason` on `acd.report_cards`, and clears `finalized_at`.
- **Attendance Summary Projection (`OQ-025`):** `acd.report_cards` does not store attendance columns. The Read Slice query for report cards aggregates counts directly from `acd.attendance_records` (`PRESENT`, `ABSENT`, `SICK`, `LATE`, `EXCUSED`) for that student and operational period.
- **Browser-Based Print (`OQ-023`):** `GET /api/acd/report-cards/{id}` returns a complete composite DTO. The frontend renders `ReportCardPrintView.vue` utilizing print-tailored CSS (`@page { size: A4 portrait; margin: 12mm; }`).

### 4.3 Pastoral Care, Discipline & Extracurriculars (`GAP-004`, `OQ-007`–`OQ-010`, `OQ-018`, `OQ-021`, `OQ-022`, `OQ-026`)
- **Behaviour Observations (`UC-TCH-06`, `OQ-021`):** `POST /api/dev/behaviour` accepts `{ studentId: Guid, academicYearId: Guid?, behaviourType: "POSITIVE" | "NEGATIVE", observedDate: DateOnly, description: string }`. Caller identity (`recorder_id = ICurrentUser.UserId`, `recorder_name = ICurrentUser.FullName`) is populated from JWT without cross-module lookups.
- **Discipline Route Separation by Authority (`OQ-007`, `OQ-026`):**
  - `POST /api/dev/discipline`: Authorized for `TEACHER` and `SCHOOL_ADMIN`. Sanctions restricted to `VERBAL_WARNING` and `WRITTEN_WARNING`. `violation_type` is free-text `VARCHAR(50)` validated for non-empty string.
  - `POST /api/dev/discipline/escalated`: Authorized strictly for `PRINCIPAL`. Sanctions restricted to `PARENT_MEETING` and `SUSPENSION`. Raises `SanctionImposedDomainEvent`.
- **Achievements Endpoint Unification (`OQ-008`):** `POST /api/dev/achievements` is shared by `TEACHER` and `SCHOOL_ADMIN`. Captures `event_name`, `achievement_type` (`SPORTS`, `ARTS`, `SCIENCE`, `COMMUNITY`, `OTHER`), `recognition_level` (`SCHOOL`, `CITY`, `PROVINCE`, `NATIONAL`), and date.
- **360-Degree Student Dossier Query (`OQ-010`):** `GET /api/dev/students/{studentId}/dossier` executes a single optimized Read Slice issuing 4 sub-queries within the `dev` schema (`behaviour_notes`, `discipline_records`, `extracurricular_participations`, `achievements`), returning a unified chronological profile.
- **Extracurricular Catalog & Supervisor Validation (`OQ-022`):** `POST /api/dev/extracurriculars` accepts `{ name: string, description: string?, supervisorId: Guid }`. Handler validates supervisor exists via `ITeacherLookupContract.TeacherExistsAsync(supervisorId)`.
- **Extracurricular Session Logging (`GAP-004`, `OQ-009`, `OQ-018`):** `POST /api/dev/extracurriculars/{activityId}/participants/{participationId}/log` accepts `{ sessionDate: DateOnly, log: string }`. Overwrites `last_session_log` and `last_session_date` on `dev.extracurricular_participations`. Authorized strictly for the assigned activity supervisor.

### 4.4 Fee Policies, Billing & Cashier Counter (`GAP-002`, `GAP-003`, `OQ-011`–`OQ-016`, `OQ-019`, `OQ-020`, `OQ-024`)
- **Charge Type Lifecycle (`OQ-011`):** `DRAFT` charge types can be updated freely. `POST /api/fin/charge-types/{id}/activate` activates the charge type. Once `ACTIVE`, updates via `PUT /api/fin/charge-types/{id}` return `409 Conflict`.
- **Student Discounts (`UC-FIN-02`):** `POST /api/fin/discounts` creates standing discounts (`FIXED_AMOUNT` or `PERCENTAGE`) scoped to student, charge type, and academic year.
- **Batch Tuition Billing Generation (`OQ-012`):** `POST /api/fin/charges/generate-batch` accepts `{ academicYearId: Guid, billingMonth: "YYYY-MM", schoolLevel: string?, grade: string?, studyGroupId: Guid? }`.
  1. Resolves active students via `IStudentLookupContract.GetActiveStudentsByScopeAsync(...)`.
  2. Queries active `MONTHLY` charge types matching the cohort.
  3. Applies student standing discounts from `fin.student_discounts`.
  4. Generates `fin.student_charges` rows.
  5. Skips duplicates idempotently via filtered unique index `UQ_student_charges_monthly`.
  6. Returns `{ generatedCount: int, skippedCount: int }`.
- **Ad-Hoc Manual Charges (`UC-FIN-04`):** `POST /api/fin/charges/adhoc` creates individual charges (uniforms, lost books, graduation) with custom quantity and description.
- **Charge Cancellation Audit (`GAP-002`, `OQ-020`):** `POST /api/fin/charges/{id}/cancel` accepts `{ cancellationReason: string }`. Allowed only if `paid_amount = 0`. Sets `cancellation_reason = reason`, `cancelled_at = DateTime.UtcNow`. Status derives to `CANCELLED` (terminal state).
- **Concurrency-Safe Receipt Generation (`GAP-003`, `OQ-013`, `OQ-019`):** Table `fin.receipt_sequences` stores `month CHAR(7)` and `next_seq INT`. Receipt number format: `KWT-YYYYMM-NNNN`. Atomic increment inside the payment transaction:
  ```sql
  UPDATE fin.receipt_sequences
     SET next_seq = next_seq + 1
  OUTPUT inserted.next_seq
   WHERE month = @currentMonth;
  ```
  If `@@ROWCOUNT = 0`, inserts `(month, 2)` and uses sequence `1`.
- **Atomic Payment Posting & Allocation (`OQ-014`):** `POST /api/fin/payments` accepts payment payload with itemized charge allocations. Executed within a single `DbTransaction`:
  1. Increment sequence counter and format receipt number.
  2. Insert `fin.payment_receipts` with `total_amount`, `allocated_amount`, and `unallocated_amount`.
  3. For each allocation line: insert `fin.payment_allocations`, update `paid_amount` and `outstanding_amount` on `fin.student_charges`, updating charge status (`UNPAID -> PARTIALLY_PAID -> PAID`).
  4. Raises `PaymentReceiptPostedDomainEvent`.
- **Advance Deposit Allocation (`UC-FIN-07`):** `POST /api/fin/deposits/allocate` applies existing unallocated deposit balances from prior receipts to new charges.
- **Guardian Billing Contact Lookup (`OQ-015`):** Owned by `Modules.Std`. `GET /api/std/billing-lookup?search=` returns student profiles with primary guardian names, phone numbers, and WhatsApp contact links. Accessible to `FINANCE_OFFICER`.
- **Daily Cashier Collection Summary (`OQ-016`):** `GET /api/fin/reports/daily-cashier?date=YYYY-MM-DD` computes totals grouped by payment method (`CASH`, `BANK_TRANSFER`) and returns the itemized receipt register directly from `fin.payment_receipts`.
- **Browser-Based Receipt Print (`OQ-024`):** `GET /api/fin/payments/{receiptId}` returns the complete receipt and allocation breakdown for `PaymentReceiptPrintView.vue` with `@media print`.

### 4.5 Reactive Cross-Module Domain Event Subscribers (`GAP-012`, `GAP-013`, `GAP-014`, `OQ-027`–`OQ-029`)
- `StudentPlacedDomainEvent` $\rightarrow$ `Modules.Fin/Subscribers/StudentPlacedEventHandler.cs`: In-process, awaited handler. Resolves student scope via `IStudentLookupContract`, queries active `MONTHLY` charge types, and generates current-month charges. Skips silently if no active charge types exist.
- `CandidateEnrolledDomainEvent` $\rightarrow$ `Modules.Fin/Subscribers/CandidateEnrolledEventHandler.cs`: In-process, awaited handler. Generates initial tuition charges for the newly enrolled student.
- `ReportCardFinalizedDomainEvent` $\rightarrow$ `Modules.Std/Subscribers/ReportCardFinalizedEventHandler.cs`: In-process, awaited handler. Sets `is_academically_completed = 1` on the matching `std.student_placements` row (`GAP-005`).

---

# 5. Component Responsibilities

| Component | Assembly / Path | Responsibility & Ownership |
|---|---|---|
| **`BND-MOD-FIN`** | `ElsasaSis.Modules.Fin` | Owns `ChargeType`, `StudentDiscount`, `StudentCharge`, and `PaymentReceipt` Aggregate Roots, repositories, and vertical slices for `UC-FIN-01` through `UC-FIN-09` and `UC-FIN-11`. Implements `IFinanceModule`. |
| **`BND-MOD-DEV`** | `ElsasaSis.Modules.Dev` | Owns `BehaviourNote`, `DisciplineRecord`, `ExtracurricularActivity`, and `Achievement` Aggregate Roots, repositories, and vertical slices for `UC-TCH-06`..`09`, `UC-ADM-22`..`25`, `UC-PRC-03`..`04`, `UC-HRM-04`. Implements `IDevelopmentModule`. |
| **`BND-MOD-ACD`** | `ElsasaSis.Modules.Acd` | Owns `ReportCard` Aggregate Root, repository, and vertical slices for `UC-ADM-19`, `UC-ADM-20`, `UC-TCH-04`, `UC-HRM-01`..`03`, `UC-PRC-02`. |
| **`BND-MOD-STD`** | `ElsasaSis.Modules.Std` | Owns `LookupStudentBillingContacts` Read Slice (`UC-FIN-10`), `ReportCardFinalizedEventHandler` (`GAP-014`), and expands `IStudentLookupContract.GetActiveStudentsByScopeAsync`. |
| **`BND-CONTRACTS`** | `ElsasaSis.Contracts` | Owns cross-module interfaces (`IFinanceModule`, `IDevelopmentModule`, `IStudentLookupContract`) and event records (`PaymentReceiptPostedDomainEvent`, `SanctionImposedDomainEvent`, `ReportCardFinalizedDomainEvent`). |
| **`BND-DATABASE`** | `ElsasaSis.Database` | Owns Stage 3 migration scripts (`032`, `050`, `051`, `052`, `060`, `061`, `070`, `101`) and automated DbUp execution. |
| **`BND-API-HOST`** | `ElsasaSis.Api` | Registers `Modules.Fin` and `Modules.Dev` DI containers, maps Minimal API endpoints, configures authorization policies, and exposes Swagger documentation. |
| **`ElsasaSis.ArchitectureTests`** | Tests | Enforces NetArchTest boundary isolation across `Auth`, `Opr`, `Std`, `Tch`, `Acd`, `Fin`, and `Dev`, verifying zero cross-module project references. |
| **`BND-FE-ACD-RPCD`** | `features/academic/report-cards/` | Owns `SCR-ACD-REPORT-CARDS` (`ReportCardsWorkbenchView.vue`): Collaborative report card review, subject feedback, homeroom remarks, finalize/unlock, and `ReportCardPrintView.vue`. |
| **`BND-FE-DEV-PAST`** | `features/development/pastoral/` | Owns `SCR-DEV-PASTORAL` (`PastoralDossierView.vue`): 360-degree student timeline, behaviour note composer, discipline logger, principal escalation modal, and award logger. |
| **`BND-FE-DEV-CLUB`** | `features/development/extracurricular/` | Owns `SCR-DEV-EXTRACURRICULAR` (`ExtracurricularWorkbenchView.vue`): Club catalog, supervisor assignment, student enrollments, and session log recorder. |
| **`BND-FE-FIN-FEE`** | `features/finance/fee-policy/` | Owns `SCR-FIN-FEES-POLICY` (`FeesPolicyView.vue`): Fee structure definitions, standard grade-level amounts, charge type activation, and student discount allocations. |
| **`BND-FE-FIN-BILL`** | `features/finance/billing/` | Owns `SCR-FIN-BILLING` (`BillingWorkbenchView.vue`): Monthly batch tuition billing generator with Pre-Flight Modal, ad-hoc invoice creator, and charge cancellation dialog. |
| **`BND-FE-FIN-PAY`** | `features/finance/payments/` | Owns `SCR-FIN-PAYMENTS` (`PaymentsWorkbenchView.vue`): Cashier workbench, guardian lookup, multi-charge allocation, advance deposit holding/application, student ledger statement, and `PaymentReceiptPrintView.vue`. |
| **`BND-FE-FIN-RECV`** | `features/finance/receivables/` | Owns `SCR-FIN-RECEIVABLES` (`ReceivablesAgingView.vue`): Tuition receivables summary, aged arrears table, student drill-down statement drawer, and daily cashier summary report. |

---

# 6. Integration Design

| Source | Target | Transport | Purpose |
|---|---|---|---|
| Frontend (`features/academic/report-cards`) | `BND-API-HOST` (`/api/acd/report-cards/*`) | HTTP REST (JSON) | Batch generate report cards, submit subject notes, write homeroom remarks, finalize, and override. |
| Frontend (`features/development/pastoral`) | `BND-API-HOST` (`/api/dev/*`) | HTTP REST (JSON) | Submit behaviour notes, discipline incidents, principal escalations, achievements, and query student 360 dossier. |
| Frontend (`features/development/extracurricular`) | `BND-API-HOST` (`/api/dev/extracurriculars/*`) | HTTP REST (JSON) | Manage club catalog, enroll participants, and record session logs. |
| Frontend (`features/finance/*`) | `BND-API-HOST` (`/api/fin/*`) | HTTP REST (JSON) | Configure fee policies, discounts, batch billing, cashier payments, deposit allocation, statements, and reports. |
| Frontend (`features/finance/payments`) | `BND-API-HOST` (`/api/std/billing-lookup`) | HTTP REST (JSON) | Search student billing profiles and guardian WhatsApp/phone contact info. |
| `BND-MOD-ACD` (Batch Generate) | `acd.assignment_scores`, `acd.attendance_records` | SQL Query (Dapper) | Compute arithmetic mean final grades and aggregate attendance metric counts. |
| `BND-MOD-ACD` (Teacher Auth) | `tch.teacher_assignments` | SQL Query (Dapper) | Verify teacher assignment before allowing subject note submission. |
| `BND-MOD-FIN` (Batch Billing) | `IStudentLookupContract` | In-Process Interface | Fetch active student cohort records for batch tuition charge generation. |
| `BND-MOD-DEV` (Club Supervisor) | `ITeacherLookupContract` | In-Process Interface | Verify teacher existence before assigning club supervisor. |
| `StudentPlacedDomainEvent` | `BND-MOD-FIN` (Subscriber) | In-Process Event | Auto-generate first monthly tuition charge for placed student. |
| `CandidateEnrolledDomainEvent` | `BND-MOD-FIN` (Subscriber) | In-Process Event | Auto-generate first monthly tuition charge for enrolled candidate. |
| `ReportCardFinalizedDomainEvent` | `BND-MOD-STD` (Subscriber) | In-Process Event | Update `std.student_placements.is_academically_completed = 1`. |
| `PaymentReceiptPostedDomainEvent` | `outbox.messages` | In-Process Event | Queue payment event for resilient dispatch. |

---

# 7. Data Ownership

| Schema / Table | Owning Module | Persistence Responsibility |
|---|---|---|
| `acd.report_cards` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `IReportCardRepository`. |
| `acd.report_card_entries` | `ElsasaSis.Modules.Acd` | Exclusive write ownership via `IReportCardRepository` (owned child). |
| `dev.behaviour_notes` | `ElsasaSis.Modules.Dev` | Exclusive write ownership via `IBehaviourNoteRepository`. |
| `dev.discipline_records` | `ElsasaSis.Modules.Dev` | Exclusive write ownership via `IDisciplineRecordRepository`. |
| `dev.extracurricular_activities` | `ElsasaSis.Modules.Dev` | Exclusive write ownership via `IExtracurricularActivityRepository`. |
| `dev.extracurricular_participations` | `ElsasaSis.Modules.Dev` | Exclusive write ownership via `IExtracurricularActivityRepository` (owned child). |
| `dev.achievements` | `ElsasaSis.Modules.Dev` | Exclusive write ownership via `IAchievementRepository`. |
| `fin.charge_types` | `ElsasaSis.Modules.Fin` | Exclusive write ownership via `IChargeTypeRepository`. |
| `fin.student_discounts` | `ElsasaSis.Modules.Fin` | Exclusive write ownership via `IStudentDiscountRepository`. |
| `fin.student_charges` | `ElsasaSis.Modules.Fin` | Exclusive write ownership via `IStudentChargeRepository`. |
| `fin.payment_receipts` | `ElsasaSis.Modules.Fin` | Exclusive write ownership via `IPaymentReceiptRepository`. |
| `fin.payment_allocations` | `ElsasaSis.Modules.Fin` | Exclusive write ownership via `IPaymentReceiptRepository` (owned child). |
| `fin.receipt_sequences` | `ElsasaSis.Modules.Fin` | Exclusive write ownership via payment receipt command handler. |
| `std.student_placements` | `ElsasaSis.Modules.Std` | Exclusive write ownership via `IStudentPlacementRepository` (updated by event subscriber). |

---

# 8. Database Design

Detailed schemas follow Pure Persistence Layer discipline: UUID v7 primary keys, UTC application-supplied timestamps, zero database foreign keys, zero triggers/defaults/checks, and snapshot denormalization for cross-domain display attributes.

## New Tables

| Table | Schema | Script | Purpose & Key Columns |
|---|---|---|---|
| `fin.charge_types` | `fin` | `050` | Institutional fee definitions. PK: `id`, `academic_year_id`, `academic_year_name`, `name`, `frequency` (`MONTHLY`, `SEMESTER`, `YEARLY`, `ONE_TIME`), `amount` (DECIMAL(18,2)), `school_level` (NULL), `grade` (NULL), `study_group_id` (NULL), `study_group_name` (NULL), `status` (`DRAFT`, `ACTIVE`). |
| `fin.student_discounts` | `fin` | `050` | Standing student fee discounts. PK: `id`, `academic_year_id`, `academic_year_name`, `charge_type_id`, `charge_type_name`, `student_id`, `student_name`, `student_nis`, `discount_type` (`FIXED_AMOUNT`, `PERCENTAGE`), `discount_value` (DECIMAL(18,2)), `reason`, `is_active` (BIT). UQ: `(student_id, charge_type_id, academic_year_id)`. |
| `fin.student_charges` | `fin` | `050` | Individual student payment obligations. PK: `id`, `academic_year_id`, `academic_year_name`, `operational_period_id` (NULL), `operational_period_name` (NULL), `billing_month` (CHAR(7) NULL), `charge_type_id`, `charge_type_name`, `charge_frequency`, `student_id`, `student_name`, `student_nis`, `school_level`, `grade`, `study_group_id` (NULL), `study_group_name` (NULL), `description` (NULL), `unit_amount` (DECIMAL(18,2)), `quantity` (INT), `gross_amount` (DECIMAL(18,2)), `discount_amount` (DECIMAL(18,2)), `discount_reason` (NULL), `net_amount` (DECIMAL(18,2)), `paid_amount` (DECIMAL(18,2)), `outstanding_amount` (DECIMAL(18,2)), `status` (`UNPAID`, `PARTIALLY_PAID`, `PAID`, `CANCELLED`), `due_date` (DATE NULL). Filtered UQ indexes for `YEARLY`, `SEMESTER`, and `MONTHLY`. |
| `fin.payment_receipts` | `fin` | `050` | Frontline payment records. PK: `id`, `receipt_number` (NVARCHAR(30) UQ), `academic_year_id`, `academic_year_name`, `student_id`, `student_name`, `student_nis`, `study_group_id` (NULL), `study_group_name` (NULL), `payment_date` (DATE), `payment_method` (`CASH`, `BANK_TRANSFER`), `bank_reference` (NULL), `total_amount` (DECIMAL(18,2)), `allocated_amount` (DECIMAL(18,2)), `unallocated_amount` (DECIMAL(18,2)), `received_by_id`, `received_by_name`, `notes` (NULL). |
| `fin.payment_allocations` | `fin` | `050` | Itemized receipt-to-charge links. PK: `id`, `payment_receipt_id`, `receipt_number`, `student_charge_id`, `charge_type_name`, `billing_month` (NULL), `student_id`, `student_name`, `student_nis`, `allocated_amount` (DECIMAL(18,2)), `allocation_date` (DATE). |
| `fin.receipt_sequences` | `fin` | `051` | Concurrency-safe monthly receipt counters. PK: `month` (`CHAR(7)`), `next_seq` (`INT`). |
| `dev.behaviour_notes` | `dev` | `060` | Teacher behavior observation notes. PK: `id`, `student_id`, `student_name`, `student_nis`, `academic_year_id` (NULL), `academic_year_name` (NULL), `behaviour_type` (`POSITIVE`, `NEGATIVE`), `observed_date` (DATE), `description` (NVARCHAR(1000)), `recorder_id`, `recorder_name`. |
| `dev.discipline_records` | `dev` | `060` | Disciplinary infractions and sanctions. PK: `id`, `student_id`, `student_name`, `student_nis`, `academic_year_id` (NULL), `academic_year_name` (NULL), `incident_date` (DATE), `violation_type` (VARCHAR(50)), `description` (NVARCHAR(1000)), `sanction` (`VERBAL_WARNING`, `WRITTEN_WARNING`, `PARENT_MEETING`, `SUSPENSION`), `recorder_id`, `recorder_name`. |
| `dev.extracurricular_activities` | `dev` | `060` | Extracurricular activity catalog. PK: `id`, `name` (NVARCHAR(100) UQ), `description` (NULL), `supervisor_id`, `supervisor_name`, `status` (`ACTIVE`, `INACTIVE`). |
| `dev.extracurricular_participations` | `dev` | `060` | Student club enrollments. PK: `id`, `activity_id`, `activity_name`, `student_id`, `student_name`, `student_nis`, `academic_year_id`, `academic_year_name`, `status` (`ACTIVE`, `COMPLETED`, `WITHDRAWN`), `joined_date` (DATE), `completed_at` (NULL), `withdrawn_at` (NULL). UQ: `(student_id, activity_id, academic_year_id)`. |
| `dev.achievements` | `dev` | `060` | Student awards and honors. PK: `id`, `student_id`, `student_name`, `student_nis`, `academic_year_id` (NULL), `academic_year_name` (NULL), `achievement_type` (VARCHAR(30)), `event_name` (NVARCHAR(200)), `recognition_level` (`SCHOOL`, `CITY`, `PROVINCE`, `NATIONAL`), `achievement_date` (DATE), `description` (NULL), `recorder_id`, `recorder_name`. |

## Modified Tables

| Table | Schema | Script | Modification & Purpose |
|---|---|---|---|
| `acd.report_cards` | `acd` | `032` | ADD `override_reason NVARCHAR(500) NULL` (for `UC-ADM-20` unlock audit), ADD `general_remarks NVARCHAR(1000) NULL` (for `UC-HRM-02` homeroom narrative). |
| `fin.student_charges` | `fin` | `052` | ADD `cancellation_reason NVARCHAR(500) NULL`, `cancelled_at DATETIME2(3) NULL` (for `UC-FIN-05` audit and status derivation). |
| `dev.extracurricular_participations` | `dev` | `061` | ADD `last_session_log NVARCHAR(1000) NULL`, `last_session_date DATE NULL` (for `UC-TCH-09` session logging). |
| `std.student_placements` | `std` | `070` | ADD `is_academically_completed BIT NOT NULL` (updated to `1` by `ReportCardFinalizedDomainEvent` subscriber). Migration script backfills existing rows with `0`. |

## Relationships

All inter-table relationships are logical and managed in the Application Layer:
- `acd.report_card_entries.report_card_id` $\rightarrow$ `acd.report_cards.id`
- `acd.report_card_entries.subject_stage_id` $\rightarrow$ `acd.subject_stages.id`
- `dev.extracurricular_participations.activity_id` $\rightarrow$ `dev.extracurricular_activities.id`
- `dev.extracurricular_participations.student_id` $\rightarrow$ `std.students.id`
- `dev.extracurricular_activities.supervisor_id` $\rightarrow$ `tch.teachers.id`
- `dev.behaviour_notes.student_id` $\rightarrow$ `std.students.id`
- `dev.discipline_records.student_id` $\rightarrow$ `std.students.id`
- `dev.achievements.student_id` $\rightarrow$ `std.students.id`
- `fin.student_discounts.charge_type_id` $\rightarrow$ `fin.charge_types.id`
- `fin.student_discounts.student_id` $\rightarrow$ `std.students.id`
- `fin.student_charges.charge_type_id` $\rightarrow$ `fin.charge_types.id`
- `fin.student_charges.student_id` $\rightarrow$ `std.students.id`
- `fin.payment_receipts.student_id` $\rightarrow$ `std.students.id`
- `fin.payment_allocations.payment_receipt_id` $\rightarrow$ `fin.payment_receipts.id`
- `fin.payment_allocations.student_charge_id` $\rightarrow$ `fin.student_charges.id`

## Migration Considerations

Scripts are authored as idempotent T-SQL executed via DbUp in `src/backend/src/ElsasaSis.Database/Scripts/`:
1. `032_AlterAcdReportCardsAddOverrideReason.sql`: Schema patch for `acd.report_cards` (`override_reason`, `general_remarks`).
2. `050_CreateFinTables.sql`: Tables `fin.charge_types`, `fin.student_discounts`, `fin.student_charges`, `fin.payment_receipts`, `fin.payment_allocations` with unique and filtered indexes.
3. `051_CreateFinReceiptSequences.sql`: Table `fin.receipt_sequences`.
4. `052_AlterFinStudentChargesAddCancellation.sql`: Add cancellation columns to `fin.student_charges`.
5. `060_CreateDevTables.sql`: Tables `dev.behaviour_notes`, `dev.discipline_records`, `dev.extracurricular_activities`, `dev.extracurricular_participations`, `dev.achievements`.
6. `061_AlterDevExtracurricularParticipationsAddSessionLog.sql`: Add session log columns to `dev.extracurricular_participations`.
7. `070_AlterStdStudentPlacementsAddAcademicCompletion.sql`: Add `is_academically_completed` to `std.student_placements` with idempotent column check and backfill.
8. `101_SeedStage3Data.sql`: Deterministic seed data:
   - Report card entries with subject teacher notes and draft/finalized states for study group `7A`.
   - Fee structures: SPP Bulanan (SMP: Rp 350.000, SMA: Rp 450.000), Uang Gedung, and Materi Pembelajaran.
   - Sample student discounts (sibling discount 20%, scholarship fixed Rp 100.000).
   - Generated monthly student charges with sample paid, partially paid, and unpaid statuses.
   - Payment receipts with cash and bank transfer allocations and deposit balances.
   - Sample behaviour notes, disciplinary records (verbal warning, suspension), and achievements (Olimpiade Sains, Futsal).
   - Extracurricular clubs (Pramuka, Futsal, English Club) with assigned teacher supervisors and active student members.

---

# 9. Cross-Cutting Concerns

- **Role & Permission Governance:**
  - Expanded compile-time permissions in `ElsasaSis.Modules.Auth`:
    - `SCHOOL_ADMIN`: Full administrative control across report cards, extracurricular catalog, fee structures, discounts, billing batch generation, and student dossiers.
    - `FINANCE_OFFICER`: Full control over fee policies, discounts, billing batches, manual charges, charge cancellations, cashier payment counter, deposit allocations, and financial reports.
    - `TEACHER`: Submit report card subject notes for assigned classes; record student behaviour notes; record classroom discipline (`VERBAL_WARNING`, `WRITTEN_WARNING`); record achievements; record club session logs for supervised clubs; view student development dossiers for taught students.
    - `HOMEROOM_TEACHER`: Review study group report cards; write holistic general remarks; finalize study group report cards; view full student development dossiers for homeroom students.
    - `PRINCIPAL`: Review draft and finalized report cards across the school; record escalated disciplinary sanctions (`PARENT_MEETING`, `SUSPENSION`); monitor school-wide student development; view receivables summaries and cashier collection reports.
- **Pure Persistence Layer SQL Discipline:**
  - Zero database foreign keys, stored procedures, triggers, default constraints, or check constraints.
  - All primary keys must use `UuidV7.NewGuid()`.
  - All `created_at` and `updated_at` timestamps supplied in UTC by C# code (`DateTime.UtcNow`).
- **Concurrency & Idempotency:**
  - Receipt sequence counter increment uses atomic `UPDATE … OUTPUT inserted.next_seq` inside the payment transaction.
  - Report card batch generation and monthly tuition billing generation filter existing records before insert to prevent duplicate charges or cards upon re-run.
- **Error Handling (RFC 7807):**
  - Validation failures (e.g. empty override reason, invalid payment amount, negative discount value) return `422 Unprocessable Entity` with camelCase property paths.
  - Domain conflicts (e.g. attempting to cancel a charge with `paid_amount > 0`, updating an `ACTIVE` charge type, modifying a `FINALIZED` report card) return `409 Conflict`.
- **UI Ergonomics & 3-Tier Confirmation:**
  - Direct Actions (Tier 1): Entering search terms, selecting filters, switching tabs.
  - Tier 2 Confirmation Dialog: Cancelling an unpaid charge, activating a charge type, unlocking a finalized report card, recording an escalated sanction.
  - Tier 3 Pre-Flight Impact Summary Modal: Triggering batch draft report card generation (`UC-ADM-19`) and triggering batch monthly tuition billing (`UC-FIN-03`), displaying student cohort size and charge totals before commit.

---

# 10. Implementation Constraints

1. **Strict Assembly Isolation (NetArchTest):**
   - `ElsasaSis.Modules.Fin` and `ElsasaSis.Modules.Dev` must have zero project references to each other or to `Auth`, `Opr`, `Std`, `Tch`, or `Acd`.
   - References are restricted strictly to `ElsasaSis.SharedKernel` and `ElsasaSis.Contracts`.
2. **Pure Persistence Layer SQL Discipline:**
   - Zero database foreign keys, stored procedures, triggers, default constraints, or check constraints.
   - All primary keys must use `UuidV7.NewGuid()`.
   - All `created_at` and `updated_at` timestamps must be supplied in UTC by C# code.
3. **No Entity Framework Core:**
   - Dapper only via `ISqlConnectionFactory`.
4. **Mandatory CQRS Handler Discipline:**
   - Write Slices must load an Aggregate Root, mutate via domain methods, save via Repository, and dispatch domain events. Write handlers must not depend on `ISqlConnectionFactory` or Dapper directly.
   - Read Slices must execute direct Dapper SQL projections into DTOs; never instantiate Aggregate Roots on read.
5. **No Server-Side PDF Libraries:**
   - Printable report cards and payment receipts must be rendered in Vue with CSS `@media print` print stylesheets. No backend PDF generation libraries.
6. **Frontend State & Typing Discipline:**
   - Server state managed strictly via TanStack Query hooks; client context via Pinia; form state via VeeValidate + Zod.
   - Generated TypeScript contracts (`openapi-typescript`) must drive all API DTO consumption.

---

# 11. Acceptance Conditions

Stage 3 implementation is architecturally complete when all of the following conditions hold:

1. **Solution & Architecture Verification:**
   - `src/backend/ElsasaSis.sln` builds cleanly under `.NET 8` (`net8.0`).
   - `ElsasaSis.ArchitectureTests` passes all NetArchTest rules confirming boundary isolation for `ElsasaSis.Modules.Fin` and `ElsasaSis.Modules.Dev` alongside existing modules, with zero EF Core usage.
2. **Database Migrations & Seeding:**
   - DbUp executes migrations `032`, `050`, `051`, `052`, `060`, `061`, `070`, and `101` successfully on SQL Server.
   - Seed data provides functional report cards, fee structures, discounts, charges, receipts, pastoral logs, and extracurricular activities.
3. **Academic Evaluation (`acd`) Verification:**
   - `UC-ADM-19`: Administrator can batch-generate draft semester report cards for a study group; arithmetic final grades are computed correctly; existing cards are skipped gracefully.
   - `UC-TCH-04`: Subject teacher can submit feedback remarks on draft report card entries for their assigned subject stage.
   - `UC-HRM-01`, `UC-HRM-02`: Homeroom teacher can review draft cards and record holistic general remarks.
   - `UC-HRM-03`: Homeroom teacher can finalize study group report cards (`FINALIZED`); finalized cards disallow further note edits.
   - `UC-ADM-20`: Administrator can override a finalized report card with a mandatory reason, unlocking it back to `DRAFT`.
   - `UC-PRC-02`: Principal can review all draft and finalized report cards across study groups.
   - Official printable report card layout renders cleanly and supports browser printing.
4. **Pastoral Care & Student Development (`dev`) Verification:**
   - `UC-TCH-06`: Teacher can record positive and negative behaviour observation notes on student profiles.
   - `UC-TCH-07`, `UC-ADM-22`: Teacher and Administrator can record disciplinary incidents with verbal or written warnings.
   - `UC-PRC-03`: Principal can issue escalated sanctions (`PARENT_MEETING`, `SUSPENSION`).
   - `UC-TCH-08`, `UC-ADM-23`: Teacher and Administrator can record student achievement awards.
   - `UC-ADM-24`, `UC-HRM-04`, `UC-PRC-04`: Authorized roles can view the unified 360-degree student development dossier.
   - `UC-ADM-25`: Administrator can create extracurricular clubs and enroll student participants.
   - `UC-TCH-09`: Extracurricular supervisor can record meeting activity logs for participants.
5. **Fee Structures, Tuition Billing & Frontline Cashier (`fin`) Verification:**
   - `UC-FIN-01`: Finance officer can define fee charge types and activate them; active charge types reject updates.
   - `UC-FIN-02`: Finance officer can grant and revoke student standing discounts.
   - `UC-FIN-03`: Finance officer can execute batch tuition charge generation with Pre-Flight Modal preview; discounts are auto-applied; duplicate charges are skipped idempotently.
   - `UC-FIN-04`: Finance officer can issue ad-hoc manual charges.
   - `UC-FIN-05`: Finance officer can cancel unpaid erroneous charges with a mandatory reason; charges with payments reject cancellation.
   - `UC-FIN-06`: Cashier can record multi-method payment receipts (`CASH`, `BANK_TRANSFER`), generate sequential receipt numbers (`KWT-YYYYMM-NNNN`), allocate funds across open charges, and hold excess amounts as deposit.
   - `UC-FIN-07`: Cashier can allocate accumulated advance deposit balances to newly due charges.
   - `UC-FIN-08`: Cashier and finance staff can view real-time student ledger statements and print official receipts.
   - `UC-FIN-09`, `UC-ADM-26`, `UC-PRC-06`: Finance staff and leadership can view receivables summaries and aged arrears reports.
   - `UC-FIN-10`: Cashier can look up student billing profiles and guardian contact numbers.
   - `UC-FIN-11`: Cashier and finance officer can generate daily cashier collection summaries reconciled by payment method.
6. **Reactive Event Verification:**
   - Placing a student in a study group (`StudentPlacedDomainEvent`) triggers `Modules.Fin` to initialize current-month tuition charges.
   - Enrolling an accepted candidate (`CandidateEnrolledDomainEvent`) triggers `Modules.Fin` to initialize initial tuition charges.
   - Finalizing a report card (`ReportCardFinalizedDomainEvent`) triggers `Modules.Std` to update `is_academically_completed = 1` on the student placement.
7. **Frontend Quality & Shell Integration:**
   - Frontend passes `pnpm typecheck`, `pnpm lint`, and unit tests.
   - Sidebar navigation in `AppLayout.vue` integrates:
     - `Academic & Curriculum` $\rightarrow$ `Semester Report Cards` (`SCR-ACD-REPORT-CARDS`)
     - `Student Development` $\rightarrow$ `Pastoral & Discipline` (`SCR-DEV-PASTORAL`) and `Extracurricular Clubs` (`SCR-DEV-EXTRACURRICULAR`)
     - `Finance & Billing` $\rightarrow$ `Fee Policies & Discounts` (`SCR-FIN-FEES-POLICY`), `Tuition Billing` (`SCR-FIN-BILLING`), `Cashier Counter` (`SCR-FIN-PAYMENTS`), and `Receivables & Arrears` (`SCR-FIN-RECEIVABLES`)
   - Role-based visibility and 3-tier confirmation workflows are properly enforced across all views.
