# FEASIBILITY ASSESSMENT — IS003: Stage 2 (Timetables & Daily Academic Operations)

---
**Issue ID:** CR-STAGE-2  
**Assessment Date:** 2026-11-19  
**Assessed By:** Analyst (ica-analyst)  
**Status:** READY-FOR-PLANNING

---

## 1. Verdict

**FEASIBLE.** Stage 2 is fully implementable within the existing architecture. No architectural gaps, no missing tables, no unresolved business questions remain. All 20 open decisions have been resolved below and must be treated as binding constraints during implementation.

---

## 2. Scope Boundary (Explicit)

### In Scope (Stage 2)

| Use Case | Slice Name | Module | Screen |
|---|---|---|---|
| `UC-ADM-04` | ConfigureStudyGroupTimetables | `opr` | `SCR-OPR-TIMETABLE` |
| `UC-ADM-17` | ManageLearningStructure (subjects, stages, units, grade mappings) | `acd` | `SCR-ACD-CURRICULUM` |
| `UC-ADM-18` | EnrollStudentsInSubjectStages (batch) | `acd` | `SCR-ACD-CURRICULUM` |
| `UC-TCH-01` | RecordSubjectAttendance (batch upsert) | `acd` | `SCR-ACD-ATTENDANCE` |
| `UC-TCH-02` | CreateClassroomAssignment | `acd` | `SCR-ACD-SCOREBOOK` |
| `UC-TCH-03` | EnterStudentScores (batch upsert) | `acd` | `SCR-ACD-SCOREBOOK` |
| `UC-TCH-05` | GetSubjectGradebookSummary (scores + attendance) | `acd` | `SCR-ACD-SCOREBOOK` |
| `UC-TCH-10` | GetTeacherPersonalSchedule (weekly template) | `opr` | `SCR-OPR-TIMETABLE` |

### Explicitly Out of Scope (Stage 3+)

> [!IMPORTANT]
> The following are **NOT** Stage 2. Any implementation work touching these is out of scope and must be deferred.

- `SCR-ACD-REPORT-CARDS` and all report card UCs (`UC-ADM-19`, `UC-ADM-20`, `UC-TCH-04`, `UC-HRM-01`, `UC-HRM-02`, `UC-HRM-03`, `UC-PRC-02`)
- `StudentPlacedDomainEvent` subscriber in `acd` (auto-creating participations from placement)
- BR-ASGN-006 weight sum = 100% hard enforcement
- Algorithmic timetable conflict detection/blocking on write path
- `SCR-ACD-MONITORING` (academic progress monitoring)

---

## 3. Resolved Decisions (Binding)

### D-01 — Academic Participation Creation: Explicit Admin Action Only

**Decision:** `UC-ADM-18` (`POST /api/acd/participations/batch`) is the **sole** mechanism for creating `acd.academic_participations`. The `StudentPlacedDomainEvent` subscriber in `acd` is **NOT** implemented in Stage 2. No auto-enrollment from placement.

**Rationale:** Prevents ambiguity about which subject stages a student should be enrolled in. Admin controls this explicitly on `SCR-ACD-CURRICULUM`.

---

### D-02 — Attendance Granularity: Subject + Study Group (Not Stage)

**Decision:** Attendance is recorded at the **subject** level (`subject_id`), not stage level. The unique key `UQ_attendance_records_student_subject_date = UNIQUE(student_id, subject_id, attendance_date)` enforces one record per student per subject per day.

**Implementation:** Teacher selects `subjectId + studyGroupId` from their timetable. Handler loads students with `>= 1 ACTIVE participation` in that `subject_id` for that `study_group_id` within the current operational period (index `IX_academic_participations_subject_group` supports this). Stage is irrelevant to attendance.

---

### D-03 — Scorebook Context: Subject Stage + Study Group

**Decision:** Scorebook (`SCR-ACD-SCOREBOOK`) is scoped by `(subject_stage_id, study_group_id)`. Roster = students with `ACTIVE` participation in that exact `(subject_stage_id, study_group_id)` combination. Index `IX_academic_participations_stage_group` supports the lookup.

**Rationale:** `acd.assignments` is scoped to `subject_stage_id`. Keeping scorebook and assignments at the same granularity is consistent.

---

### D-04 — Teacher Assessment Authorization: Via Existing Teaching Assignment

**Decision:** No changes to `tch.teacher_assignments` schema. Teacher authorization to create/enter assessments for a subject stage is implicitly validated: handler checks that a `tch.teacher_assignment` exists for `(teacher_id, study_group_id, subject_id)` via `ITeachingModule` contract. No `subject_stage_id` column added to `tch`.

---

### D-05 — Running Score: Pure Read Slice Projection (No Stored Aggregate)

**Decision:** Weighted running score is computed **entirely in SQL** inside the `GetSubjectGradebookSummaryHandler`. Formula:
- If all assessments have `weight IS NOT NULL`: `SUM(score * weight) / NULLIF(SUM(weight), 0)`
- If any `weight IS NULL`: simple average `AVG(score)`

No computed column on `acd.academic_participations`. No separate cache table. Pure Read Slice.

**Passing threshold:** UI-side filter parameter only, not persisted. Teacher inputs threshold value in the UI; the client highlights failing cells.

---

### D-06 — `study_group_id` on `acd.assignments`: Nullable in Schema, Required in Practice

**Decision:** Column stays `NULL`-able per table design. In Stage 2, the `CreateClassroomAssignment` handler **requires** `study_group_id` in the request and validates it. When present, only students from that group can receive scores for that assignment. No schema change.

---

### D-07 — Enter Student Scores: Batch Upsert (One Call Per Assignment)

**Decision:** `POST /api/acd/assignments/{id}/scores` accepts a **batch array** of `{ studentId, score }` pairs. Semantics: **upsert** — insert if no score exists, update if it does. Database unique constraint `UQ_assignment_scores_assignment_student` is the idempotency guard.

**Handler validates per student:** student has `ACTIVE` participation in the assignment's `subject_stage_id`.

**UI pattern:** Tier 1 inline spreadsheet grid + dirty-cell tracking + sticky "Save Scores" bar (batch commit, not auto-save).

---

### D-08 — Record Attendance: Batch Upsert (One Call Per Class Session)

**Decision:** `POST /api/acd/attendance` accepts a **batch array** of `{ studentId, status }` for a specific `(subjectId, studyGroupId, attendanceDate)`. Semantics: **upsert** per `UQ_attendance_records_student_subject_date`.

**UI pattern:** "Mark All Present" populates all chips to `PRESENT` client-side → teacher adjusts exceptions → single commit. Same Tier 1 dirty-state + sticky Save bar pattern as scorebook.

---

### D-09 — Timetable Editor UI: Weekly Grid + Tier 3 Drawer Per Slot

**Decision:**
- `SCR-OPR-TIMETABLE` = Schedule Management pattern (Pattern 4 from ui-ux-guideline)
- Admin selects: `operational_period_id` + `study_group_id` + `schedule_type` (REGULAR / GENERAL / VOCATIONAL)
- Weekly grid displayed (days × time slots)
- Clicking a slot opens a **Tier 3 right-anchored drawer** to assign `subject_id + teacher_id + room_id`
- Save = `POST /api/opr/study-groups/{id}/timetables` for **one slot** at a time
- SMK block date ranges managed in a **separate drawer** that creates entries in `opr.study_group_timetables`
- Delete slot = `DELETE /api/opr/study-groups/{id}/timetables/{scheduleId}`

---

### D-10 — Timetable Conflict Detection: Read-Side Only (No Write Blocking)

**Decision:** Conflict detection is **read-side only**. The timetable grid query returns per-slot conflict flags:
- `teacherConflict: true` if `teacher_id` already has another schedule at same `(period, day, start_time, schedule_type)`
- `roomConflict: true` if `room_id` already has another schedule at same slot

Conflicts displayed inline as `rose-600` cell highlights per ui-ux-guideline Section 6.1 (Pattern 4).

**Write path does NOT block saves.** Admin resolves conflicts manually. This is explicitly aligned with "no algorithmic conflict solver" in architecture scope §3.2.

---

### D-11 — Teacher Schedule Endpoint: Weekly Template (Not Date-Specific)

**Decision:** `GET /api/opr/teacher-schedule` returns the **weekly template** for the current/selected `operational_period_id`. It returns all schedule entries where `teacher_id = @currentTeacherId`, grouped by `schedule_type`. It is NOT date-resolved.

**SMK block resolution:** Client fetches `GET /api/opr/study-group-timetables?periodId=` separately to determine which block is active on a given date. Date-specific resolution is client-side.

---

### D-12 — ElsasaSis.Modules.Acd: New C# Project in Stage 2

**Decision:** `ElsasaSis.Modules.Acd` is a **brand new C# class library project** created in Stage 2 at `src/backend/src/Modules/ElsasaSis.Modules.Acd/`. It follows the standard module structure:

```
ElsasaSis.Modules.Acd/
├── Domain/
│   ├── Model/           # Subject, SubjectStage, AcademicParticipation, AttendanceRecord, Assignment
│   ├── Events/
│   ├── Errors/
│   └── I*Repository.cs  # Repository interfaces
├── Infrastructure/
│   └── Persistence/     # Dapper repository implementations
├── Features/
│   ├── LearningStructure/
│   ├── Participations/
│   ├── Attendance/
│   └── Scorebook/
├── DependencyInjection.cs
├── Permissions.cs
└── AcademicModuleAssemblyMarker.cs
```

The existing `ElsasaSis.Modules.Opr` gets new timetable slices added within its existing project structure under `Features/Timetables/`.

---

### D-13 — ManageLearningStructure: Multiple Endpoint Slices Under One Conceptual Area

**Decision:** `UC-ADM-17` is implemented as multiple vertical slices under `Features/LearningStructure/`:

| Slice | Endpoint |
|---|---|
| `CreateSubject` | `POST /api/acd/subjects` |
| `UpdateSubject` | `PUT /api/acd/subjects/{id}` |
| `CreateSubjectStage` | `POST /api/acd/subjects/{id}/stages` |
| `UpdateSubjectStage` | `PUT /api/acd/subjects/{id}/stages/{stageId}` |
| `CreateLearningUnit` | `POST /api/acd/subjects/{id}/stages/{stageId}/units` |
| `UpdateLearningUnit` | `PUT /api/acd/subjects/{id}/stages/{stageId}/units/{unitId}` |
| `MapSubjectStageGrade` | `POST /api/acd/subjects/{id}/stages/{stageId}/grade-mappings` |
| `RemoveSubjectStageGrade` | `DELETE /api/acd/subjects/{id}/stages/{stageId}/grade-mappings/{mappingId}` |
| `GetLearningStructure` | `GET /api/acd/subjects` (hierarchical read) |

All endpoints: `SCHOOL_ADMIN` permission only.

---

### D-14 — Cross-Module Contracts for Modules.Acd

**Decision:**

| Contract | Direction | Purpose |
|---|---|---|
| `IOperationalContract` (existing) | `Acd` → `Opr` | Verify `operational_period_id` is valid before mutations |
| `IStudentLookupContract` (existing) | `Acd` → `Std` | Verify student exists + snapshot `student_name`, `student_nis` |
| `ITeachingModule` (existing) | `Acd` → `Tch` | Verify teacher assignment for authorization |
| `IAcademicModule` (new stub) | Future consumers → `Acd` | Interface declared in `Contracts` assembly; minimal surface; consumed in Stage 3+ |

Read slices in `Modules.Acd` may use **cross-schema Dapper JOINs** (permitted per architecture §11.2).

---

### D-15 — Database Migration: 040_CreateAcdTables.sql is a New Stage 2 Migration

**Decision:** The `acd` schema exists (created in Stage 0 via `001_CreateSchemas.sql`). The 10 `acd` tables do not exist yet. Stage 2 creates migration script `040_CreateAcdTables.sql` in `ElsasaSis.Database/Scripts/` as the first implementation task.

The script must follow the Pure Persistence Layer DDL from `academic-tables-design.md` Section 5 exactly:
- `PRIMARY KEY CLUSTERED (id)` only — no FK constraints
- `UNIQUE` constraints only where documented
- Non-unique indexes per the design document
- No `DEFAULT`, `CHECK`, triggers, or stored procedures

---

### D-16 — Frontend Feature Directory Mapping

**Decision:**

| Screen | Feature Directory | View File |
|---|---|---|
| `SCR-OPR-TIMETABLE` | `features/operations/scheduling/` | `views/TimetableWorkspaceView.vue` |
| `SCR-ACD-CURRICULUM` | `features/academic/curriculum/` | `views/CurriculumSetupView.vue` |
| `SCR-ACD-ATTENDANCE` | `features/academic/attendance/` | `views/AttendanceWorkbenchView.vue` |
| `SCR-ACD-SCOREBOOK` | `features/academic/scorebook/` | `views/ScorebookWorkbenchView.vue` |

Each follows standard submodule anatomy: `api/`, `components/`, `composables/`, `schemas/`, `types/`, `views/`, `__tests__/`.

---

### D-17 — UC-ADM-18 Batch Enrollment Request Shape & Semantics

**Decision:** `POST /api/acd/participations/batch`

**Request:** `{ operationalPeriodId: UUID, subjectStageId: UUID, studyGroupId: UUID }`

**Handler logic:**
1. Verify `operational_period_id` via `IOperationalContract`
2. Verify `subject_stage_id` exists in `acd.subject_stages`
3. Cross-schema read: fetch all students with `ACTIVE` placement in `study_group_id` for the academic year
4. Bulk-insert `acd.academic_participations` rows, populate all snapshot columns
5. Skip silently any student already enrolled (`UQ_academic_participations_student_stage_period`)
6. Return `{ enrolled: N, skipped: M }`

**UI:** Tier 3 Pre-Flight Impact Summary Modal before execution (shows preview count of students to be enrolled).

---

### D-18 — BR-ASGN-006 Weight Sum Validation: Deferred to Stage 3

**Decision:** The domain rule "weights must sum to 100" is **NOT enforced** in Stage 2 write slices. Weight per assignment is validated to be `0.00 ≤ weight ≤ 100.00` if provided, but no cross-assignment sum check.

**Running score computation in Stage 2:**
- If ALL assessments for the stage+group+period have `weight IS NOT NULL`: `SUM(score * weight) / NULLIF(SUM(weight), 0)`
- Otherwise: `AVG(score)` across all scored assessments

**BR-ASGN-006 strict enforcement is a Stage 3 prerequisite** (required before report card final grade computation).

---

### D-19 — Report Cards: Strictly Out of Scope

**Decision:** `SCR-ACD-REPORT-CARDS` and all associated UCs (`UC-ADM-19`, `UC-ADM-20`, `UC-TCH-04`, `UC-HRM-01`, `UC-HRM-02`, `UC-HRM-03`, `UC-PRC-02`) are **Stage 3**. The `acd.report_cards` and `acd.report_card_entries` tables are created by the `040_CreateAcdTables.sql` migration (for schema completeness) but no C# slices targeting them are built in Stage 2.

---

### D-20 — Gradebook Summary: Scores + Attendance in One Read Slice

**Decision:** `GET /api/acd/gradebook` (`UC-TCH-05`) returns a single DTO per student containing:
- Per-assignment scores (`assignmentId`, `score`, `weight`)
- Computed running score (weighted or simple average per D-18)
- Attendance count per status for the current operational period: `{ present, absent, late, excused, sick }`

All computed in a **single Dapper query** joining `acd.academic_participations`, `acd.assignment_scores`, `acd.assignments`, and `acd.attendance_records`. No separate API call for attendance summary.

---

## 4. Architectural Compliance Checklist

> [!NOTE]
> Every decision above has been verified against the full architecture documentation. The following confirms no violations.

| Concern | Status |
|---|---|
| No cross-schema WRITE operations | ✅ All writes stay within owning schema |
| No hard DB foreign keys across schemas | ✅ All cross-schema refs = raw GUIDs |
| No business logic in DB (no CHECK, DEFAULT, triggers) | ✅ All invariants enforced in Application Layer |
| UUID v7 for all new PKs | ✅ |
| `created_at` / `updated_at` = UTC, app-supplied | ✅ |
| No cross-module assembly references | ✅ Modules.Acd only references Shared + Contracts |
| Write slices use Aggregate Root pattern | ✅ Per backend-architecture.md §6.1 |
| Read slices use direct Dapper projection | ✅ Per backend-architecture.md §6.2 |
| FluentValidation = input format only | ✅ Business rules stay in Domain |
| Domain events dispatched after commit | ✅ |
| Frontend: no cross-feature imports | ✅ Cross-domain = named Vue Router links only |
| Frontend: server state in TanStack Query, client state in Pinia | ✅ |
| UI screens match one of 5 authoritative screen patterns | ✅ Timetable=Schedule Mgmt, Curriculum=Planning, Attendance+Scorebook=Work Queue |
| "Hide by Role, Disable by State" enforced | ✅ Teacher sees only their classes; SCHOOL_ADMIN sees curriculum |
| Batch commit with dirty-state guard on Attendance + Scorebook | ✅ Per ui-ux-guideline §6.2 |
| Tier 3 Pre-Flight Summary for UC-ADM-18 | ✅ Per ui-ux-guideline §8.5 |

---

## 5. New Contracts to Declare (Stage 2)

### Backend (ElsasaSis.Contracts)

**`IAcademicModule`** — stub interface, minimal surface for Stage 3 consumption:
```csharp
// Contracts/Academic/IAcademicModule.cs
public interface IAcademicModule
{
    // Stage 3+ will add: HasActiveParticipation, GetStudentGradeSummary, etc.
}
```

### Frontend (src/shared/types/)

New OpenAPI-generated types from new `acd` endpoints added to `shared/types/api/`.

New query key factories:
- `academicKeys` (curriculum, participations, attendance, gradebook)
- `timetableKeys` (schedules, blocks, teacher-schedule)

---

## 6. Implementation Sequence (Suggested)

> [!TIP]
> This is a suggested dependency order for the implementation plan. The architect may revise slice ordering.

1. **Database Migration** — `040_CreateAcdTables.sql` (10 acd tables)
2. **Opr Module Expansion** — Timetable slices in `ElsasaSis.Modules.Opr`:
   - `ConfigureStudyGroupTimetables` (UC-ADM-04)
   - `GetTeacherPersonalSchedule` (UC-TCH-10)
   - Supporting query slices (GetStudyGroupTimetable for admin grid + conflict flags)
3. **Acd Module Bootstrap** — Project creation, DI wiring, `IAcademicModule` stub
4. **Learning Structure** — `ManageLearningStructure` slices (UC-ADM-17): Subject → Stage → Unit → Grade Mapping
5. **Academic Participations** — `EnrollStudentsInSubjectStages` (UC-ADM-18)
6. **Attendance** — `RecordSubjectAttendance` + supporting read slices (UC-TCH-01)
7. **Scorebook** — `CreateClassroomAssignment` + `EnterStudentScores` + `GetSubjectGradebookSummary` (UC-TCH-02, 03, 05)
8. **Frontend — SCR-OPR-TIMETABLE** (depends on backend steps 2 above)
9. **Frontend — SCR-ACD-CURRICULUM** (depends on steps 4+5)
10. **Frontend — SCR-ACD-ATTENDANCE** (depends on step 6)
11. **Frontend — SCR-ACD-SCOREBOOK** (depends on step 7)

---

## 7. Open Questions: None

All architectural and business questions have been resolved.

---

## 8. Planning Readiness

**Status:** READY-FOR-PLANNING  
**Granted By:** Architect (ica-architect)  
**Granted Date:** 2026-11-19
