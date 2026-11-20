# ISSUE

## Metadata

ID: CR-STAGE-3
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage 3: Evaluation, Finance & Student Development (Semester Report Cards, Pastoral Care & Discipline, Extracurriculars, Fee Policies, Billing & Cashier Counter)

## Source

Reported By: User
Reported Date: 2026-11-20

## Description

Implement Stage 3 ("Evaluation, Finance & Student Development") of the ELSASA-SIS v4 development roadmap.

This change request builds directly upon the operational master data established in Stage 1 and the instructional academic engine delivered in Stage 2 (master timetables, curriculum hierarchy, daily classroom attendance, and teacher scorebooks). It aims to activate the institution's core evaluative, student pastoral development, and frontline financial operational engines:

1. **Semester Report Cards & Academic Evaluation (`acd`):** End-of-semester collaborative evaluation workflows, batch generation of draft semester report cards from scorebook aggregates and attendance records, subject teacher evaluation remarks, homeroom teacher holistic remarks, administrative override/unlock, principal review, and formal finalization locking with printable official report card layouts.
2. **Pastoral Care, Student Discipline & Extracurricular Activities (`dev`):** Unified 360-degree holistic student development dossier tracking teacher behavior observation notes, classroom incidents, administrative disciplinary infractions, principal-level escalated disciplinary sanctions (`PARENT_MEETING`, `SUSPENSION`), student academic and non-academic achievement awards, extracurricular activities catalog management, and club attendance/activity logging.
3. **Fee Structures, Tuition Billing & Cashier Operations (`fin`):** Institutional fee policies and charge types (monthly tuition / SPP, registration fees, uniforms, learning materials), student discount and scholarship management, periodic batch student tuition charge generation, ad-hoc invoice adjustments, billing cancellation for unpaid erroneous charges, frontline cashier counter recording multi-method payments (`CASH`, `BANK_TRANSFER`), itemized charge allocation, advance deposit holding, student financial balance ledgers and account statements, guardian billing contact lookup, and daily cashier collection summaries.

The scope realizes 28 actor-driven use cases across 3 primary bounded contexts:
- **Academic Evaluation (`acd` — 7 UCs):** `UC-ADM-19`, `UC-ADM-20`, `UC-TCH-04`, `UC-HRM-01`, `UC-HRM-02`, `UC-HRM-03`, `UC-PRC-02`
- **Student Development & Pastoral (`dev` — 8 UCs):** `UC-TCH-06`, `UC-TCH-07`, `UC-ADM-22`, `UC-PRC-03`, `UC-TCH-08` / `UC-ADM-23`, `UC-ADM-25`, `UC-TCH-09`, `UC-ADM-24` / `UC-HRM-04` / `UC-PRC-04`
- **Finance & Cashier (`fin` — 10 UCs):** `UC-FIN-01`, `UC-FIN-02`, `UC-FIN-03`, `UC-FIN-04`, `UC-FIN-05`, `UC-FIN-06`, `UC-FIN-07`, `UC-FIN-08`, `UC-FIN-10`, `UC-FIN-11`

The stage delivers 6 interactive UI screens:
1. `SCR-ACD-REPORT-CARDS`: Collaborative report card workspace (draft generation, subject feedback, homeroom remarks, finalization locking, and official printable layout).
2. `SCR-DEV-PASTORAL`: Unified 360-degree student dossier (behavior timeline, disciplinary infractions, awards, and extracurricular logs).
3. `SCR-DEV-EXTRACURRICULAR`: Activity roster, student member enrollments, and attendance logger.
4. `SCR-FIN-FEES-POLICY`: Fee catalog, default amounts per grade level, and student discount assignment.
5. `SCR-FIN-BILLING`: Monthly batch tuition billing generator with preview, ad-hoc invoice creator, and charge cancellation.
6. `SCR-FIN-PAYMENTS`: Front-line cashier counter: payment receipt recording (`CASH`, `BANK_TRANSFER`), itemized charge allocation, advance deposit holding, and official receipt PDF printing.

## Desired Outcome

1. **Collaborative Semester Report Cards:**
   - Administrative personnel can trigger batch draft semester report card generation for all enrolled students in a study group for an active semester.
   - Subject teachers can input evaluation notes and qualitative feedback for their respective subjects.
   - Homeroom teachers can review draft report cards for their assigned study group and compose holistic developmental remarks.
   - Homeroom teachers can finalize and lock report cards (`FINALIZED`), preventing further edits unless an administrative override/unlock is granted.
   - School leadership (Principal) can review draft and finalized report cards across classes.
   - Standardized, printable official report card layouts can be viewed and printed.

2. **Pastoral Care, Disciplinary Governance & Student Development:**
   - Teachers and staff can record positive or constructive behavior observation notes on student records.
   - Classroom incidents and administrative disciplinary infractions can be formally logged with severity levels and corrective actions.
   - The school principal can issue escalated sanctions (such as parent conference summons or suspensions).
   - Staff can record academic and extracurricular achievement awards and institutional recognitions.
   - School administrators can manage the catalog of extracurricular activities (clubs, sports, arts) and enroll interested students.
   - Activity advisors can record attendance and session logs for extracurricular meetings.
   - Authorized roles (Homeroom Teacher, Principal, Admin) can access a comprehensive 360-degree student development dossier showing the full timeline of behavior, discipline, awards, and club activities.

3. **Institutional Fee Policies, Billing & Frontline Cashier Counter:**
   - Finance officers can configure institutional fee structures (e.g., standard monthly SPP by grade level, registration, facilities) and charge types.
   - Financial personnel can grant and revoke student-specific discounts and scholarships (e.g., sibling discounts, merit-based waivers).
   - Finance officers can execute periodic batch billing to generate monthly tuition charges across student cohorts with a pre-generation preview.
   - Financial staff can issue ad-hoc manual charges (e.g., replacement ID cards, graduation fees) and cancel unpaid erroneous charges with documented audit reasons.
   - Frontline cashier staff can record incoming student payments across payment channels (`CASH`, `BANK_TRANSFER`), allocate payments across open charges, and hold excess amounts as advance deposit balances.
   - Cashiers can apply accumulated advance deposit credits toward newly due charges.
   - Cashiers and finance staff can view real-time student ledger statements, look up guardian billing contacts, generate official printable payment receipts, and reconcile daily collection summaries.

## Current Situation

Stage 0 ("Walking Skeleton & Reference Slice"), Stage 1 ("Operational Setup & Master Data Foundation"), and Stage 2 ("Timetables & Daily Academic Operations") have all been completed and verified (`IS001- STAGE-0-DEV-IMPLEMENTATION-PLAN.md`, `IS002- STAGE-1-DEV-IMPLEMENTATION-PLAN.md`, and `IS003- STAGE-2-DEV-IMPLEMENTATION-PLAN.md` are marked `COMPLETED`). The application provides working authentication, navigation, operational calendars, rooms, announcements, student admission and enrollment with NIS, annual study groups (rombel), student class placements, teacher directories, subject capabilities, teaching allocations, master timetables with SMK rotation templates, subject curriculum structure, student subject enrollment, daily classroom attendance, and teacher scorebook grading.

However, the application currently lacks:
- Semester report card batch generation, multi-role remark workflows, finalization locking, administrative unlocking, and printable report card layouts.
- Pastoral care logging, student behavior observation timelines, and classroom incident tracking.
- Disciplinary infraction logging, principal-level disciplinary sanction escalations, and student achievement award records.
- Extracurricular activity catalog management, student club enrollments, and extracurricular attendance recording.
- Holistic 360-degree student development dossier views.
- Institutional fee structure configuration, grade-level fee policies, and student discount/scholarship management.
- Periodic batch tuition billing generation, ad-hoc student charge creation, and charge cancellation workflows.
- Frontline cashier payment receipt processing, multi-channel payment capture, itemized charge allocation, advance deposit holding, and deposit allocation.
- Student financial balance ledgers, account statements, printable official cashier receipts, and daily cashier collection reconciliation summaries.
- The corresponding backend domain models/modules (`ElsasaSis.Modules.Dev`, `ElsasaSis.Modules.Fin`, and report card extensions in `ElsasaSis.Modules.Acd`) and frontend screens (`SCR-ACD-REPORT-CARDS`, `SCR-DEV-PASTORAL`, `SCR-DEV-EXTRACURRICULAR`, `SCR-FIN-FEES-POLICY`, `SCR-FIN-BILLING`, and `SCR-FIN-PAYMENTS`).

## Evidence

- Development Roadmap: [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md) (Section 3: Stage 3 Evaluation, Finance & Student Development)
- Stage 0 Implementation Plan (Completed): [IS001- STAGE-0-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-IMPLEMENTATION-PLAN.md)
- Stage 1 Implementation Plan (Completed): [IS002- STAGE-1-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-IMPLEMENTATION-PLAN.md)
- Stage 2 Implementation Plan (Completed): [IS003- STAGE-2-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS003-%20STAGE-2-DEV-IMPLEMENTATION-PLAN.md)
- System Architecture: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
- Backend Architecture: [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
- Frontend Architecture: [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
- UI/UX Guidelines: [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)
- Screen Inventory: [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) (`SCR-ACD-REPORT-CARDS`, `SCR-DEV-PASTORAL`, `SCR-DEV-EXTRACURRICULAR`, `SCR-FIN-FEES-POLICY`, `SCR-FIN-BILLING`, `SCR-FIN-PAYMENTS`)
- Use Case Discovery: [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (Use Cases `UC-ADM-19..20`, `UC-ADM-22..25`, `UC-TCH-04`, `UC-TCH-06..09`, `UC-HRM-01..04`, `UC-PRC-02..04`, `UC-FIN-01..08`, `UC-FIN-10..11`)
- Academic Table Design: [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md) (`acd.report_cards`, `acd.report_card_entries`)
- Development Table Design: [development-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/development-tables-design.md) (`dev.behaviour_notes`, `dev.disciplinary_records`, `dev.student_awards`, `dev.extracurricular_activities`, `dev.extracurricular_participations`, `dev.extracurricular_sessions`, `dev.extracurricular_attendances`)
- Finance Table Design: [finance-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/finance-tables-design.md) (`fin.fee_structures`, `fin.student_discounts`, `fin.student_charges`, `fin.payment_receipts`, `fin.payment_allocations`, `fin.student_advance_deposits`, `fin.deposit_allocations`, `fin.student_ledger_entries`)
- Roles & Actors Specification: [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md)

## Notes

- Intake captured in accordance with the ELSASA-SIS v4 development roadmap for Stage 3.
- Solution-neutral intake: captures the requested system capabilities and user-facing operational outcomes without prescribing technical implementation specifics, architectural boundaries, or slice sequencing, which will be formally evaluated during Discovery and Feasibility Assessment by the Analyst (`ica-analyst`).
