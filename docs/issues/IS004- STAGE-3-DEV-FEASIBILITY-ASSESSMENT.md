---
Title: Stage 3 Feasibility Assessment — Evaluation, Finance & Student Development
Code: CR-STAGE-3
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-20
Status: READY-FOR-PLANNING
---

# 1. Request Summary

**Feature being assessed:** IS004 — Stage 3: Implement Evaluation, Finance & Student Development  
(`acd` Report Cards, `dev` Pastoral/Discipline/Extracurricular, `fin` Fee Policies/Billing/Cashier)

**Referenced artifacts:**

- `docs/issues/IS004- STAGE-3-DEV-ISSUE.md`
- `docs/architecture/ELSASA-SIS-ARCHITECTURE.md`
- `docs/architecture/backend-architecture.md`
- `docs/architecture/frontend-architecture.md`
- `docs/architecture/ui-ux-guideline.md`
- `docs/architecture/academic-tables-design.md`
- `docs/architecture/development-tables-design.md`
- `docs/architecture/finance-tables-design.md`

## Objective

Determine whether Stage 3 is architecturally and operationally ready to proceed to Architecture Update → Planning, identify all structural gaps between the approved table designs and the full Stage 3 feature scope, and record every architectural decision so the agentic implementation team needs zero autonomous decision-making.

---

# 2. Current State

## Existing Behavior

Stages 0–2 are COMPLETED. The system provides:

- `ElsasaSis.Modules.Auth` — JWT auth, role-based authorization
- `ElsasaSis.Modules.Opr` — Academic years, operational periods, calendar, rooms, timetables, announcements
- `ElsasaSis.Modules.Std` — Admission candidates, student enrollment (NIS), profiles, guardians, study groups, placements
- `ElsasaSis.Modules.Tch` — Teacher directory, subject capabilities, teaching assignments
- `ElsasaSis.Modules.Acd` — Subjects, subject stages, learning units, academic participations, attendance records, assignments, assignment scores (report card tables `acd.report_cards` and `acd.report_card_entries` exist in the schema but are **not yet implemented** in the application layer)

**Frontend features present:** `auth/`, `operations/`, `student/`, `teaching/`, `academic/` (curriculum, attendance, scorebook submodules)

**Backend modules absent:** `ElsasaSis.Modules.Fin`, `ElsasaSis.Modules.Dev`

**Frontend features absent:** `finance/`, `development/`

## Existing Constraints

1. **Pure Persistence Layer:** No triggers, no stored procedures, no CHECK constraints, no DEFAULT constraints. All business rules enforced in the application layer.
2. **UUID v7 PKs:** All PKs are application-generated UUID v7. No DB-side generation.
3. **No cross-schema FK constraints:** Cross-module references are plain `UNIQUEIDENTIFIER` columns with application-layer referential integrity.
4. **Snapshot denormalization:** Cross-domain display attributes are snapshotted alongside FK IDs at write time.
5. **CQRS mandatory:** Write slices use Aggregate Roots + Repositories. Read slices use direct Dapper projections.
6. **No business logic in handlers:** All `if` statements governing state transitions live in Domain Aggregate Roots.
7. **Audit timestamps:** `created_at` and `updated_at` are `DATETIME2(3) NOT NULL`, UTC, supplied by the application. No DB DEFAULTs.
8. **No `created_by`/`updated_by` columns** in the schema (by design).

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | `acd.report_cards` table lacks an `override_reason` column required by UC-ADM-20 (administrative override audit). |
| GAP-002 | CRITICAL | `fin.student_charges` table lacks `cancellation_reason NVARCHAR(500)`, `cancelled_at DATETIME2(3)`, and `CANCELLED` status required by UC-FIN-05. |
| GAP-003 | CRITICAL | No `fin.receipt_sequences` table exists for concurrency-safe receipt number generation (KWT-YYYYMM-NNNN format) required by UC-FIN-06. |
| GAP-004 | CRITICAL | `dev.extracurricular_participations` table lacks `last_session_log NVARCHAR(1000)` and `last_session_date DATE` columns required by UC-TCH-09. |
| GAP-005 | CRITICAL | `std.student_placements` table lacks `is_academically_completed BIT NOT NULL` column required by the `ReportCardFinalizedDomainEvent` → `std` subscriber reaction. |
| GAP-006 | CRITICAL | `ElsasaSis.Modules.Fin` assembly does not exist. All 11 finance use cases (UC-FIN-01 through UC-FIN-11) are unimplemented. |
| GAP-007 | CRITICAL | `ElsasaSis.Modules.Dev` assembly does not exist. All 8 development use cases (UC-TCH-06 through UC-TCH-09, UC-ADM-22, UC-ADM-23, UC-ADM-24, UC-ADM-25, UC-PRC-03, UC-PRC-04, UC-HRM-04) are unimplemented. |
| GAP-008 | CRITICAL | 7 `acd`-module report card use cases (UC-ADM-19, UC-ADM-20, UC-TCH-04, UC-HRM-01, UC-HRM-02, UC-HRM-03, UC-PRC-02) are unimplemented despite `acd.*` tables existing. |
| GAP-009 | MAJOR | Frontend `src/features/finance/` directory does not exist. SCR-FIN-FEES-POLICY, SCR-FIN-BILLING, SCR-FIN-PAYMENTS, SCR-FIN-RECEIVABLES screens are absent. |
| GAP-010 | MAJOR | Frontend `src/features/development/` directory does not exist. SCR-DEV-PASTORAL, SCR-DEV-EXTRACURRICULAR screens are absent. |
| GAP-011 | MAJOR | Frontend `src/features/academic/report-cards/` submodule does not exist. SCR-ACD-REPORT-CARDS screen is absent. |
| GAP-012 | MAJOR | `StudentPlacedDomainEvent` subscriber in `Modules.Fin` does not exist. The event is published by `Modules.Std` but `Modules.Fin` has no registered handler to initialize default tuition charges. |
| GAP-013 | MAJOR | `CandidateEnrolledDomainEvent` subscriber in `Modules.Fin` does not exist. The event is published by `Modules.Std` but `Modules.Fin` has no registered handler to create the student's first SPP charges from matching ACTIVE charge types. |
| GAP-014 | MAJOR | `ReportCardFinalizedDomainEvent` subscriber in `Modules.Std` does not exist. The event is published by `Modules.Acd` but `Modules.Std` has no registered handler to update `is_academically_completed`. |
| GAP-015 | MINOR | `IFinanceModule` and `IDevelopmentModule` contract interfaces are not yet declared in `ElsasaSis.Contracts`. |
| GAP-016 | MINOR | `PaymentReceiptPostedDomainEvent` and `SanctionImposedDomainEvent` domain event records are not yet declared in `ElsasaSis.Contracts`. |

---

# 4. Open Questions

*All open questions have been resolved via the /grill-me interview session on 2026-11-20. No OQs remain blocking.*

| ID | Question | Status |
|---|---|---|
| OQ-001 | Report card `final_grade` computation method | CLOSED |
| OQ-002 | `letter_grade` generation during batch | CLOSED |
| OQ-003 | Idempotency of batch report card generation | CLOSED |
| OQ-004 | Subject teacher note submission pattern | CLOSED |
| OQ-005 | Homeroom teacher finalization granularity | CLOSED |
| OQ-006 | Admin override behavior on FINALIZED cards | CLOSED |
| OQ-007 | Discipline endpoint separation by route vs. role | CLOSED |
| OQ-008 | Achievement recording endpoint unification | CLOSED |
| OQ-009 | UC-TCH-09 session log table gap resolution | CLOSED |
| OQ-010 | 360-degree dossier composite DTO structure | CLOSED |
| OQ-011 | ACTIVE charge type mutability | CLOSED |
| OQ-012 | Batch billing student cohort resolution | CLOSED |
| OQ-013 | Receipt number format and generation strategy | CLOSED |
| OQ-014 | Receipt creation + allocation atomicity | CLOSED |
| OQ-015 | UC-FIN-10 guardian lookup module ownership | CLOSED |
| OQ-016 | Daily cashier summary derivation strategy | CLOSED |
| OQ-017 | Override audit reason storage location | CLOSED |
| OQ-018 | UC-TCH-09 exact schema change | CLOSED |
| OQ-019 | Receipt number concurrency strategy | CLOSED |
| OQ-020 | Charge cancellation status representation | CLOSED |
| OQ-021 | `dev.*` recorder_id identity source | CLOSED |
| OQ-022 | Extracurricular supervisor identity source | CLOSED |
| OQ-023 | Printable report card rendering approach | CLOSED |
| OQ-024 | Printable cashier receipt rendering approach | CLOSED |
| OQ-025 | Attendance summary in report card | CLOSED |
| OQ-026 | `violation_type` enum vs free-text | CLOSED |
| OQ-027 | StudentPlacedDomainEvent → fin reaction | CLOSED |
| OQ-028 | CandidateEnrolledDomainEvent → fin reaction | CLOSED |
| OQ-029 | ReportCardFinalizedDomainEvent → std reaction | CLOSED |
| OQ-030 | Frontend feature directory structure for fin + dev | CLOSED |
| OQ-031 | Report cards frontend placement | CLOSED |
| OQ-032 | Migration script numbering strategy | CLOSED |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | DbUp migration scripts are idempotent. `ALTER TABLE` scripts use `IF NOT EXISTS (SELECT 1 FROM sys.columns ...)` guards. |
| ASM-002 | The school operates a single-tenant deployment (no `org_unit_id` scoping required). |
| ASM-003 | `IStudentLookupContract` already exists in `ElsasaSis.Contracts` from Stage 1 and is usable by `Modules.Fin` and `Modules.Dev`. |
| ASM-004 | `ITeacherLookupContract` already exists in `ElsasaSis.Contracts` from Stage 1 and is usable by `Modules.Dev`. |
| ASM-005 | `IOperationalContract` already exists in `ElsasaSis.Contracts` from Stage 1 and is usable by `Modules.Fin` and `Modules.Dev`. |
| ASM-006 | `StudentPlacedDomainEvent` and `CandidateEnrolledDomainEvent` are already declared in `ElsasaSis.Contracts` and published by `Modules.Std`. |
| ASM-007 | The active academic year and operational period are resolved at handler time via `IOperationalContract`, not passed as JWT claims. |
| ASM-008 | `auth.users.display_name` is accessible to handlers via `ICurrentUser.FullName` for recording snapshots in `dev.*` recorder fields. |
| ASM-009 | The `acd.report_cards` and `acd.report_card_entries` tables were created by Stage 2 migration scripts and are present in the database. |
| ASM-010 | Violation types are free-text categories — no backend enum validation beyond non-whitespace. The frontend may suggest common values. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Receipt number sequential counter race condition if multiple cashiers post payments simultaneously. | Duplicate receipt numbers → DB constraint violation → failed transactions. | Mitigated by `fin.receipt_sequences` table with atomic `UPDATE … SET next_seq = next_seq + 1 OUTPUT inserted.next_seq WHERE month = @month` inside the payment transaction. |
| RISK-002 | `CandidateEnrolledDomainEvent` → fin subscriber auto-generates charges before any ACTIVE charge type exists for the student's level. | Zero charges generated silently. | Handler must verify ACTIVE charge types exist before generating. If none exist, skip silently and log. Finance officers run UC-FIN-03 for manual batch. |
| RISK-003 | Batch report card generation (UC-ADM-19) for a large study group could be a long-running DB operation. | HTTP timeout on large batches (100+ students). | Accept as a Basic-level constraint. Batch is executed synchronously. No background job needed for Basic. |
| RISK-004 | `std.student_placements.is_academically_completed` migration applies to an existing table that may contain rows from Stages 1–2. | Existing rows need a default value for the new column. | The ALTER TABLE script adds the column as `BIT NOT NULL` with the application supplying `0` for existing rows via a one-time UPDATE in the migration script (acceptable as a migration-only DEFAULT). |
| RISK-005 | `fin.student_charges` gaining a `CANCELLED` status changes the status enum. | Existing UNPAID/PARTIALLY_PAID/PAID logic must remain unaffected. | `CANCELLED` is a terminal state only reachable from `UNPAID`. The status derivation logic is updated to: CANCELLED if `cancelled_at IS NOT NULL`, else derived from amounts. |

---

# 7. Recommendations

## Option A — Full Stage 3 Delivery (Recommended)

Implement all 28 use cases across `acd` (report cards), `dev`, and `fin` as defined in IS004, with the gap resolutions recorded in Section 8.

### Advantages

- Delivers the complete evaluative, pastoral, and financial operational engine in one stage.
- All architectural decisions are closed — zero ambiguity for agentic implementation.
- Aligns with the development roadmap.

### Disadvantages

- Largest stage in terms of new assembly count (2 new modules: `Fin`, `Dev`) + 7 new migration scripts + 6 new frontend screens.

---

# 8. Gap Closure

## GAP-001

### Decision

Add migration script `041_AlterAcdReportCardsAddOverrideReason.sql` that executes:

```sql
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'acd.report_cards') AND name = N'override_reason')
BEGIN
    ALTER TABLE acd.report_cards ADD override_reason NVARCHAR(500) NULL;
END;
```

The `ReportCard` Aggregate Root gains an `OverrideReason` property (nullable string). The `OverrideReportCardStatus` Write Slice sets `override_reason` and transitions status back to `DRAFT`. The override reason is mandatory (non-blank) when calling the override endpoint.

### Rationale

No separate audit table is required. The override reason on the record itself provides sufficient operational audit for Basic level.

### Impact

One new migration script. One new column in `acd.report_cards`. No change to existing read slices (nullable column).

### Architecture Impact

Minor schema extension to `acd.report_cards`. The `ReportCard` Aggregate Root requires a new `Unlock(string overrideReason)` domain method.

### Resolved By

User decision — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-002

### Decision

Add migration script `052_AlterFinStudentChargesAddCancellation.sql` that executes:

```sql
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'fin.student_charges') AND name = N'cancellation_reason')
BEGIN
    ALTER TABLE fin.student_charges ADD cancellation_reason NVARCHAR(500) NULL;
    ALTER TABLE fin.student_charges ADD cancelled_at DATETIME2(3) NULL;
END;
```

The `status` field's valid values become: `UNPAID`, `PARTIALLY_PAID`, `PAID`, `CANCELLED`.  
Cancellation is restricted to `UNPAID` charges only (`paid_amount = 0`).  
`cancellation_reason` is mandatory (non-blank) on the cancellation write.  
The `CANCELLED` status is a terminal state — no further transitions allowed.

**Status derivation rule (updated):**
- If `cancelled_at IS NOT NULL` → `CANCELLED`
- Else if `paid_amount = 0` → `UNPAID`
- Else if `outstanding_amount > 0` → `PARTIALLY_PAID`
- Else → `PAID`

### Rationale

Explicit cancellation state preserves audit history. Soft-delete via discount manipulation would corrupt financial reporting.

### Impact

One new migration script. Two new nullable columns on `fin.student_charges`. The `StudentCharge` Aggregate Root gains a `Cancel(string reason)` domain method.

### Architecture Impact

Minor schema extension. `CANCELLED` added to status enum throughout `Modules.Fin`.

### Resolved By

User decision — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-003

### Decision

Add migration script `051_CreateFinReceiptSequences.sql`:

```sql
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'fin.receipt_sequences') AND type = N'U')
BEGIN
    CREATE TABLE fin.receipt_sequences (
        month     CHAR(7)  NOT NULL,
        next_seq  INT      NOT NULL,
        CONSTRAINT PK_receipt_sequences PRIMARY KEY CLUSTERED (month)
    );
END;
```

The `RecordPaymentReceiptAndAllocate` handler atomically obtains the next sequence within the same `DbTransaction` as the INSERT:

```sql
-- Try to increment existing month row:
UPDATE fin.receipt_sequences
   SET next_seq = next_seq + 1
OUTPUT inserted.next_seq
 WHERE month = @currentMonth;

-- If @@ROWCOUNT = 0 (first receipt this month), insert with seq = 1:
-- INSERT INTO fin.receipt_sequences (month, next_seq) VALUES (@currentMonth, 2);
-- And use sequence value 1 for this receipt.
```

Receipt number format: `KWT-YYYYMM-NNNN` (e.g., `KWT-202607-0001`), where `NNNN` is 4-digit zero-padded.

### Rationale

Atomic `UPDATE … OUTPUT` within the same transaction prevents duplicate receipt numbers under concurrent load.

### Impact

One new migration script. One new table in `fin` schema. Owned exclusively by `Modules.Fin`.

### Architecture Impact

New table in `fin` schema. No cross-module impact.

### Resolved By

User decision — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-004

### Decision

Add migration script `061_AlterDevExtracurricularParticipationsAddSessionLog.sql`:

```sql
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dev.extracurricular_participations') AND name = N'last_session_log')
BEGIN
    ALTER TABLE dev.extracurricular_participations ADD last_session_log  NVARCHAR(1000) NULL;
    ALTER TABLE dev.extracurricular_participations ADD last_session_date DATE           NULL;
END;
```

UC-TCH-09 (`RecordExtracurricularLog`) maps to `POST /api/dev/extracurriculars/{activityId}/participants/{participationId}/log` with body `{ "sessionDate": "YYYY-MM-DD", "log": "..." }`.

The Write Slice overwrites `last_session_log` and `last_session_date` on the targeted participation row. Only the TEACHER role (supervisor of the activity) may call this endpoint.

### Rationale

No separate sessions table is required at Basic level. A simple last-session note per participation is sufficient.

### Impact

One new migration script. Two new nullable columns on `dev.extracurricular_participations`. `ExtracurricularParticipation` Aggregate Root gains `UpdateSessionLog(string log, DateOnly sessionDate)` domain method.

### Architecture Impact

Minor schema extension.

### Resolved By

User decision — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-005

### Decision

Add migration script `070_AlterStdStudentPlacementsAddAcademicCompletion.sql`:

```sql
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'std.student_placements') AND name = N'is_academically_completed')
BEGIN
    ALTER TABLE std.student_placements ADD is_academically_completed BIT NOT NULL;
    UPDATE std.student_placements SET is_academically_completed = 0;
END;
```

The `ReportCardFinalizedDomainEvent` triggers a `Modules.Std` subscriber that sets `is_academically_completed = 1` for the student's placement row matching `student_id` + `academic_year_id`.

### Rationale

Enables future promotion/graduation workflows. Must be in `Modules.Std` ownership per boundary rules.

### Impact

One new migration script. One new column on `std.student_placements`. New domain event subscriber in `Modules.Std/Subscribers/`.

### Architecture Impact

Schema extension to `std.student_placements`. New subscriber class.

### Resolved By

User decision — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-006, GAP-007, GAP-008

### Decision

Create `ElsasaSis.Modules.Fin` and `ElsasaSis.Modules.Dev` assemblies. Implement all 28 Stage 3 use cases per the architecture's use case mapping (Section 8 of `ELSASA-SIS-ARCHITECTURE.md`). Implement 7 report card slices inside `Modules.Acd/Features/ReportCards/`.

### Rationale

Required to fulfill IS004 scope.

### Impact

2 new .NET projects. 7 new acd features. All registered in solution and DI.

### Architecture Impact

New bounded context assemblies. ArchitectureTests must add isolation assertions.

### Resolved By

IS004 scope

### Resolved Date

2026-11-20

---

## GAP-009, GAP-010, GAP-011

### Decision

Create frontend feature submodules:

```
src/features/academic/report-cards/
src/features/finance/fee-policy/
src/features/finance/billing/
src/features/finance/payments/
src/features/finance/receivables/
src/features/development/pastoral/
src/features/development/extracurricular/
```

Each follows the standard `api/ components/ schemas/ types/ views/` structure. Vue Router entries and sidebar navigation entries added with role-based `v-if` guards.

### Rationale

Standard application of the existing feature-oriented modular frontend structure.

### Impact

7 new feature submodule directories. Vue Router and sidebar nav updates.

### Architecture Impact

No structural change.

### Resolved By

IS004 scope

### Resolved Date

2026-11-20

---

## GAP-012, GAP-013, GAP-014

### Decision

Add domain event subscribers:

- `Modules.Fin/Subscribers/StudentPlacedEventHandler.cs` — On `StudentPlacedDomainEvent`: resolve student scope via `IStudentLookupContract`; query ACTIVE charge types; generate current-month `fin.student_charges` for each matching MONTHLY charge type (skip if already exists). Skip silently if no ACTIVE charge types found.
- `Modules.Fin/Subscribers/CandidateEnrolledEventHandler.cs` — On `CandidateEnrolledDomainEvent`: same logic as above.
- `Modules.Std/Subscribers/ReportCardFinalizedEventHandler.cs` — On `ReportCardFinalizedDomainEvent`: set `is_academically_completed = 1` on the matching `std.student_placements` row.

All three are **in-process, awaited** (UI-visible side effects).

### Rationale

Immediate visibility of financial and academic completion state is required by the UI.

### Impact

3 new subscriber classes. No schema change beyond GAP-005.

### Architecture Impact

New in-process domain event handlers.

### Resolved By

User decision — 2026-11-20

### Resolved Date

2026-11-20

---

## GAP-015

### Decision

Declare in `ElsasaSis.Contracts`:
- `IFinanceModule` — exposing `Task<StudentFinancialSummary?> GetStudentFinancialSummaryAsync(Guid studentId, CancellationToken ct)`.
- `IDevelopmentModule` — exposing `Task<bool> HasActiveDisciplineRecordAsync(Guid studentId, CancellationToken ct)` as a placeholder.

Initial implementations may be stubs. Stubs allow immediate DI registration.

### Rationale

Contracts must be declared before assembly scan wiring, following the existing contract pattern.

### Impact

Two new interface declarations. Two new stub implementations.

### Architecture Impact

Minimal. Follows `IOperationalContract` / `IStudentLookupContract` pattern.

### Resolved By

IS004 scope

### Resolved Date

2026-11-20

---

## GAP-016

### Decision

Declare in `ElsasaSis.Contracts/Events/`:
- `PaymentReceiptPostedDomainEvent(Guid ReceiptId, Guid StudentId, decimal TotalAmount, string ReceiptNumber, DateOnly PaymentDate)`
- `SanctionImposedDomainEvent(Guid DisciplineRecordId, Guid StudentId, string Sanction, DateOnly IncidentDate)`

Both are raised post-commit and dispatched via `IDomainEventDispatcher`. `PaymentReceiptPostedDomainEvent` routes to the Outbox for resilient delivery.

### Rationale

Event records must be in Contracts (not in the raising module) to allow cross-module consumption.

### Impact

Two new record declarations.

### Architecture Impact

Minimal. Follows existing event declaration pattern.

### Resolved By

IS004 scope

### Resolved Date

2026-11-20

---

## OQ-001

### Decision

Simple arithmetic average of all `acd.assignment_scores.score` for the student's `subject_stage_id` + `operational_period_id`. No weighting. If no scores exist, `final_grade = 0.00`. Rounded to `DECIMAL(5,2)`.

### Rationale

Simplest correct implementation for Basic level.

### Impact

`GenerateDraftReportCards` handler computes `AVG(score)` per student per subject stage via Dapper.

---

## OQ-002

### Decision

`letter_grade` stays `NULL` on DRAFT report card entries. Never auto-computed. Optional manual entry deferred to future.

### Rationale

Schools use custom grading scales; no hardcoded mapping.

### Impact

No grade conversion logic. `letter_grade` column remains nullable and unpopulated by batch generation.

---

## OQ-003

### Decision

`GenerateDraftReportCards` is idempotent: students with an existing DRAFT or FINALIZED card for the target `operational_period_id` are skipped. Returns `{ generatedCount, skippedCount }`.

### Rationale

Prevents duplicates. Safe to re-run.

### Impact

Batch handler filters existing records before inserting.

---

## OQ-004

### Decision

`ProvideSubjectReportCardNotes` (UC-TCH-04): `POST /api/acd/report-cards/{reportCardId}/subject-notes` with body `{ "subjectStageId": "...", "teacherNote": "..." }`.

Handler validates: (1) card is DRAFT; (2) caller has teaching assignment for the subject stage in that study group; (3) updates `teacher_note`, `teacher_id`, `teacher_name` on the matching `acd.report_card_entries` row. Overwrites previous note. Only allowed while `DRAFT`.

### Rationale

Simple targeted update. Teachers refine notes before homeroom finalization.

### Impact

One Write Slice in `Modules.Acd`.

---

## OQ-005

### Decision

`FinalizeHomeroomReportCards` (UC-HRM-03): `POST /api/acd/report-cards/{id}/finalize`. Finalizes one card at a time. No bulk endpoint.

### Rationale

Minimalist implementation. Per-card quality review.

### Impact

One Write Slice. `ReportCard.Finalize()` domain method. Raises `ReportCardFinalizedDomainEvent`.

---

## OQ-006

### Decision

`OverrideReportCardStatus` (UC-ADM-20): `POST /api/acd/report-cards/{id}/override` with body `{ "overrideReason": "..." }`. Transitions `FINALIZED → DRAFT`. Sets `override_reason` (required non-blank). Homeroom teacher can then re-edit and re-finalize.

### Rationale

Standard unlock pattern. Override reason auditable on the record.

### Impact

One Write Slice. `ReportCard.Unlock(string reason)` domain method. `override_reason` column (GAP-001).

---

## OQ-007

### Decision

- `POST /api/dev/discipline` — `TEACHER` and `SCHOOL_ADMIN`. Allowed sanctions: `VERBAL_WARNING`, `WRITTEN_WARNING`. Authorization: role check + domain method validates sanction ∈ {VERBAL_WARNING, WRITTEN_WARNING}.
- `POST /api/dev/discipline/escalated` — `PRINCIPAL` only. Allowed sanctions: `PARENT_MEETING`, `SUSPENSION`. Authorization: role check + domain method validates sanction ∈ {PARENT_MEETING, SUSPENSION}.

Both write to `dev.discipline_records`.

### Rationale

Segregation of authority. Clear route-level separation.

### Impact

Two Write Slices sharing one `DisciplineRecord` Aggregate Root.

---

## OQ-008

### Decision

`POST /api/dev/achievements` — shared by `TEACHER` and `SCHOOL_ADMIN`. Single endpoint. `recorder_id` = `ICurrentUser.UserId`, `recorder_name` = `ICurrentUser.FullName`.

### Rationale

Both roles record the same entity. No business distinction requires separate routes.

### Impact

One Write Slice in `Modules.Dev`.

---

## OQ-009

### Decision

UC-TCH-09: `POST /api/dev/extracurriculars/{activityId}/participants/{participationId}/log` with body `{ "sessionDate": "YYYY-MM-DD", "log": "..." }`. Overwrites `last_session_log` and `last_session_date`. Authorization: `TEACHER` who is supervisor of the activity. See GAP-004.

### Impact

One Write Slice. Schema change in GAP-004.

---

## OQ-010

### Decision

`GET /api/dev/students/{studentId}/dossier` — single Read Slice returning composite DTO:

```json
{
  "studentId": "...", "studentName": "...", "studentNis": "...",
  "behaviourNotes": [...],
  "disciplineRecords": [...],
  "extracurricularParticipations": [...],
  "achievements": [...]
}
```

4 sub-queries within one handler (all `dev.*`, no cross-schema joins needed). Each sub-collection sorted descending by date.

Authorization data scoping: TEACHER → students they teach; HOMEROOM_TEACHER → their study group students; SCHOOL_ADMIN + PRINCIPAL → all.

### Impact

One Read Slice in `Modules.Dev`.

---

## OQ-011

### Decision

`DRAFT` charge type: freely editable. `ACTIVE` charge type: locked — no edits. `PUT /api/fin/charge-types/{id}` returns `409 Conflict` if status is `ACTIVE`.

### Impact

`ChargeType.Activate()` is a one-way transition enforced by the domain method.

---

## OQ-012

### Decision

`GeneratePeriodicCharges` (UC-FIN-03):
1. Calls `IStudentLookupContract.GetActiveStudentsByScope(schoolLevel, grade, studyGroupId, academicYearId)`.
2. For each student, generates one `fin.student_charges` row for the target billing month/period.
3. Auto-applies active `fin.student_discounts`.
4. Skips duplicates (idempotent via unique indexes).
5. Returns `{ generated, skipped }`.

Requires `GetActiveStudentsByScope` method on `IStudentLookupContract` (verify/add as needed).

### Impact

One Write Slice. May require a new method on the existing `IStudentLookupContract`.

---

## OQ-013

### Decision

Format: `KWT-YYYYMM-NNNN`. Generated via `fin.receipt_sequences` (GAP-003). Resets per calendar month.

### Impact

`RecordPaymentReceiptAndAllocate` handler atomically obtains sequence before inserting.

---

## OQ-014

### Decision

`RecordPaymentReceiptAndAllocate` (UC-FIN-06): single atomic `DbTransaction` covering:
1. Sequence increment (`fin.receipt_sequences`).
2. INSERT `fin.payment_receipts`.
3. For each allocation in payload: INSERT `fin.payment_allocations` + UPDATE `fin.student_charges`.

One Write Slice. Full transactional rollback on any failure.

### Impact

One Write Slice in `Modules.Fin`.

---

## OQ-015

### Decision

`LookupStudentBillingContacts` (UC-FIN-10) — owned by `Modules.Std`. `GET /api/std/billing-lookup?search=...`. Returns student + guardian contact summary. Accessible to `FINANCE_OFFICER` role. One Read Slice in `Modules.Std`.

### Impact

One Read Slice added to `Modules.Std`. No schema change.

---

## OQ-016

### Decision

`GetDailyCashierCollectionSummary` (UC-FIN-11): `GET /api/fin/reports/daily-cashier?date=YYYY-MM-DD`. Derived from `fin.payment_receipts WHERE payment_date = @date`. Returns totals by payment method + receipt list. No persistent summary table.

### Impact

One Read Slice in `Modules.Fin`.

---

## OQ-017

### Decision

Override reason stored as `override_reason NVARCHAR(500) NULL` on `acd.report_cards`. See GAP-001.

---

## OQ-018

### Decision

Two columns on `dev.extracurricular_participations`: `last_session_log NVARCHAR(1000) NULL`, `last_session_date DATE NULL`. See GAP-004.

---

## OQ-019

### Decision

`fin.receipt_sequences` table with atomic `UPDATE … SET next_seq = next_seq + 1 OUTPUT inserted.next_seq WHERE month = @month`. See GAP-003.

---

## OQ-020

### Decision

`CANCELLED` added as terminal status to `fin.student_charges` with `cancellation_reason` and `cancelled_at` columns. Only reachable from `UNPAID`. See GAP-002.

---

## OQ-021

### Decision

`recorder_id` = `ICurrentUser.UserId` (`auth.users.id`). `recorder_name` = `ICurrentUser.FullName` (`auth.users.display_name`). No cross-module lookup for recorder identity.

### Impact

No contract call needed. `ICurrentUser` provides data directly.

---

## OQ-022

### Decision

`supervisor_id` = `tch.teachers.id`. Validated via `ITeacherLookupContract.TeacherExistsAsync(supervisorId)`. `supervisor_name` = snapshot from teacher profile.

### Impact

Requires `ITeacherLookupContract.TeacherExistsAsync` (verify existence from Stage 1).

---

## OQ-023

### Decision

Browser-based print via `PrintLayout.vue` and `window.print()`. No server-side PDF. Backend provides data DTO; frontend renders `ReportCardPrintView.vue`.

### Impact

No new backend library. Frontend implements print view.

---

## OQ-024

### Decision

Browser-based print via `PrintLayout.vue`. Backend `GET /api/fin/payments/{receiptId}` returns receipt DTO. Frontend renders `PaymentReceiptPrintView.vue`.

### Impact

No new backend library.

---

## OQ-025

### Decision

`acd.report_cards` and `acd.report_card_entries` do NOT store attendance summary data. The report card Read Slice joins `acd.attendance_records` (permitted cross-schema read) to compute present/absent/sick/late/excused counts per subject per period for the DTO. No schema change.

### Impact

Report card Read Slice includes attendance aggregate sub-query.

---

## OQ-026

### Decision

`violation_type` is free-text `VARCHAR(50) NOT NULL`. Validated only for non-whitespace by FluentValidation. No backend enum. Frontend may suggest common values.

### Impact

No `CHECK` constraint. FluentValidation: `RuleFor(x => x.ViolationType).NotEmpty()`.

---

## OQ-027

### Decision

`StudentPlacedEventHandler` in `Modules.Fin` generates current-month MONTHLY charges. In-process, awaited. See GAP-012.

---

## OQ-028

### Decision

`CandidateEnrolledEventHandler` in `Modules.Fin` generates first-month MONTHLY charges. In-process, awaited. See GAP-013.

---

## OQ-029

### Decision

`ReportCardFinalizedEventHandler` in `Modules.Std` sets `is_academically_completed = 1`. In-process, awaited. See GAP-014.

---

## OQ-030

### Decision

```
src/features/finance/fee-policy/
src/features/finance/billing/
src/features/finance/payments/
src/features/finance/receivables/
src/features/development/pastoral/
src/features/development/extracurricular/
```

Each: `api/ components/ schemas/ types/ views/`.

---

## OQ-031

### Decision

```
src/features/academic/report-cards/
```

Existing `academic/attendance/`, `academic/scorebook/`, `academic/curriculum/` are untouched.

---

## OQ-032

### Decision

Stage 3 migration scripts in execution order:

| Script | Action |
|---|---|
| `041_AlterAcdReportCardsAddOverrideReason.sql` | ADD `override_reason NVARCHAR(500) NULL` to `acd.report_cards` |
| `050_CreateFinTables.sql` | CREATE `fin.charge_types`, `fin.student_discounts`, `fin.student_charges`, `fin.payment_receipts`, `fin.payment_allocations` |
| `051_CreateFinReceiptSequences.sql` | CREATE `fin.receipt_sequences` |
| `052_AlterFinStudentChargesAddCancellation.sql` | ADD `cancellation_reason`, `cancelled_at` to `fin.student_charges` |
| `060_CreateDevTables.sql` | CREATE `dev.behaviour_notes`, `dev.discipline_records`, `dev.extracurricular_activities`, `dev.extracurricular_participations`, `dev.achievements` |
| `061_AlterDevExtracurricularParticipationsAddSessionLog.sql` | ADD `last_session_log`, `last_session_date` to `dev.extracurricular_participations` |
| `070_AlterStdStudentPlacementsAddAcademicCompletion.sql` | ADD `is_academically_completed BIT NOT NULL` to `std.student_placements` |

All scripts are idempotent (`IF NOT EXISTS` guards).

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Stage 3 introduces:
- 2 new module assemblies (`Modules.Fin`, `Modules.Dev`)
- 7 new database migration scripts
- 4 schema modifications on existing tables + 1 new support table (`fin.receipt_sequences`)
- 3 new domain event subscribers across existing modules
- 1 new contract method on `IStudentLookupContract`
- 2 new contract interfaces + 2 new domain event records
- 7 new frontend feature submodules
- 28 new use case implementations across 3 bounded contexts

Formal architecture documentation is required before planning proceeds.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved (GAP-001 through GAP-016)
- [x] All required decisions recorded (OQ-001 through OQ-032)
- [x] All blocking open questions resolved (zero open OQs)
- [x] Architecture can be finalized or updated

## Status

**READY-FOR-PLANNING** — Granted by Architect on 2026-11-20.

## Notes

All 32 open questions and 16 gaps have been fully resolved with explicit decisions, rationale, and implementation guidance. Zero ambiguities remain for the agentic implementation team. The Architect must review and grant READY-FOR-PLANNING before the Architecture Update and Implementation Planning steps begin.

READY-FOR-PLANNING means that feasibility is sufficiently resolved for the Architecture skill to finalize or update the target architecture. It does not mean that architecture is complete or that the architecture step may be skipped. Planning begins only after the required architecture work is complete.

---

# 11. References

**Artifacts:**

- [IS004- STAGE-3-DEV-ISSUE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS004-%20STAGE-3-DEV-ISSUE.md)
- [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
- [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
- [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
- [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)
- [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md)
- [development-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/development-tables-design.md)
- [finance-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/finance-tables-design.md)

**Codebase locations:**

- `src/backend/src/Modules/` — existing module assemblies
- `src/backend/src/ElsasaSis.Database/Scripts/` — migration scripts
- `src/frontend/src/features/` — frontend feature directories

**Referenced documents:**

- `docs/actors-roles-specification-basic.md`
- `docs/use-case-discovery-basic.md`
- `docs/screen-inventory.md`
- `docs/elsasa-sis-v4-development-roadmap.md`
