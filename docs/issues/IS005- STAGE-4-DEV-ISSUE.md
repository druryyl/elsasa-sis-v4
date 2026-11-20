# ISSUE

## Metadata

ID: CR-STAGE-4
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage 4: Consumer Portals & Executive Governance (Parent Family Portal, Student Learner Portal, School-Wide Academic Monitoring, Financial Receivables & Arrears Aging Oversight)

## Source

Reported By: User
Reported Date: 2026-11-20

## Description

Implement Stage 4 ("Consumer Portals & Executive Governance") of the ELSASA-SIS v4 development roadmap.

This change request extends the verified operational and instructional cores delivered in Stages 0 through 3 (walking skeleton, master data backbones, academic timetables/scorebooks, semester report cards, student pastoral dossiers, and cashier operations). It aims to deliver dedicated self-service consumer interfaces for external stakeholders (parents and students) and executive governance oversight dashboards for school leadership:

1. **Parent Family Portal (`portal-parent`):** Responsive self-service portal empowering parents/guardians to select linked enrolled children, view and download finalized semester academic report cards (with drafts strictly hidden), inspect monthly tuition billing statements, view discounts applied, track remaining advance deposit balances, download official payment receipts, and read school announcements and academic calendars.
2. **Student Learner Portal (`portal-student`):** Distraction-free, mobile-friendly self-service portal for enrolled learners to inspect their personal weekly study group timetable (automatically resolving SMK vocational block rotations between General and Vocational cycles), view today's class agenda, check active school operating calendars and holidays, and read student-targeted announcements.
3. **School-Wide Academic Progress Monitoring (`acd`):** High-level institutional governance dashboard for school administrators and principals to track daily attendance taking completeness, assessment score entry status across subjects and teachers, and semester report card draft-to-finalization progress ahead of publishing deadlines, featuring exception alerts and progressive drill-downs.
4. **Financial Receivables, Arrears Aging & Collection Monitoring (`fin`):** Executive financial oversight dashboard providing visibility over total school receivables, aged tuition arrears (*tunggakan per rombel*), cash versus bank transfer collections across date windows, guardian advance deposit liabilities, and detailed student billing statements for auditing and leadership coordination without requiring cashier counter write permissions.

The scope realizes 11 actor-driven use cases across 4 functional domains / consumer contexts:
- **Parent Portal (`portal-parent` — 4 UCs):**
  - `UC-PRT-01` (`QUERY`): View & Download Child's Finalized Report Card
  - `UC-PRT-02` (`QUERY`): View Child's Monthly Billing Statements & Deposit Balance
  - `UC-PRT-03` (`QUERY`): View & Download Official Payment Receipts
  - `UC-PRT-04` (`QUERY`): View School Calendar & Parent Announcements
- **Student Portal (`portal-student` — 3 UCs):**
  - `UC-STD-01` (`QUERY`): View Personal Study Group Timetable
  - `UC-STD-02` (`QUERY`): View School Calendar
  - `UC-STD-03` (`QUERY`): View Student Announcements
- **Executive Governance & Monitoring (`acd` / `fin` — 4 UCs):**
  - `UC-ADM-21` (`QUERY`): Monitor School-Wide Attendance, Assessment & Report Card Progress
  - `UC-ADM-26` (`QUERY`): View Financial Receivables Summary (Read-Only)
  - `UC-PRC-06` (`QUERY`): Monitor School Financial Collection & Arrears Reports (Read-Only)
  - `UC-FIN-09` (`QUERY`): View Study Group Arrears & Period Collection Reports

The stage delivers 4 rich UI screens:
1. `SCR-PRT-PORTAL`: Responsive parent portal with multi-child selector, report card downloads, monthly billing/deposit statement viewer, receipt downloads, and school bulletins.
2. `SCR-STD-PORTAL`: Mobile-first student learner portal with daily schedule agenda, weekly timetable grid resolving SMK block rotations, school calendar, and announcements feed.
3. `SCR-ACD-MONITORING`: Executive academic monitoring dashboard with school-wide KPI metric tiles, operational exception alerts (missing attendance, overdue grades, unfinalized report cards), and progressive drill-down tables.
4. `SCR-FIN-RECEIVABLES`: Financial receivables and collection oversight dashboard with top KPI cards, study group arrears breakdown table, collection ledger, and student statement drawer.

## Desired Outcome

1. **Parent Family Self-Service Portal (`SCR-PRT-PORTAL`):**
   - Parents/guardians authenticate with their portal credentials and are presented only with information corresponding to their legally linked children (multi-child switcher).
   - Parents can inspect and download finalized semester report card PDFs; draft or in-progress report cards are strictly inaccessible.
   - Parents can view monthly fee obligations, discounts applied, payments made, outstanding balances, and remaining advance deposit balances.
   - Parents can view historical payment transactions and download official payment receipt PDFs with itemized charge allocation breakdowns.
   - Parents can consult the active school calendar and read published announcements targeted to parents or the whole school.

2. **Student Learner Portal (`SCR-STD-PORTAL`):**
   - Enrolled students authenticate and access a personalized, distraction-free learner dashboard.
   - The weekly schedule displays subject, teacher, and room allocations, accurately reflecting SMK vocational block rotation cycles (`GENERAL` vs `VOCATIONAL`) based on the current date.
   - A "Today's Agenda" view provides rapid mobile-first visibility into remaining classes for the active day.
   - Students can view official school calendar dates (holidays, exams, closures) and student-targeted bulletins.

3. **School-Wide Academic Progress Monitoring (`SCR-ACD-MONITORING`):**
   - School administrators and principals have real-time visibility into daily attendance completion rates across all study groups.
   - Institutional grading completeness is tracked, surfacing subjects and teachers with overdue assignment or exam scores.
   - Semester report card readiness is tracked across grades, identifying study groups with pending homeroom or subject remarks ahead of deadlines.
   - Administrators can drill down progressively from School Level to Study Group and individual Subject Scorebooks.

4. **Financial Receivables, Arrears Aging & Collection Governance (`SCR-FIN-RECEIVABLES`):**
   - Finance officers, principals, and administrators can monitor total receivables, overdue arrears, daily/periodic collection totals, and advance deposit liabilities without modifying ledger transactions.
   - Study group fee arrears (*laporan tunggakan per rombel*) provide actionable delinquent student counts and overdue totals for homeroom teacher follow-up.
   - The collection transaction ledger provides chronological multi-channel audit logs (`CASH`, `BANK_TRANSFER`).
   - Authorized personnel can drill into any student's itemized monthly billing statement drawer to resolve balance inquiries.

## Current Situation

Stages 0 ("Walking Skeleton & Reference Slice"), 1 ("Operational Setup & Master Data Foundation"), 2 ("Timetables & Daily Academic Operations"), and 3 ("Evaluation, Finance & Student Development") establish the backend domain entities, business rules, database schemas (`opr`, `std`, `tch`, `acd`, `dev`, `fin`), and operational administrative workbenches. Internal staff can create timetables, take attendance, grade assignments, draft report cards, track student dossiers, generate charges, and record cashier payments.

However, the application currently lacks:
- Dedicated, consumer-facing self-service portals tailored for mobile and desktop consumption by Parents (`/portal/parent`) and Students (`/portal/student`).
- Strict consumer data isolation filters ensuring parent accounts can access only their legally linked children and student accounts can access only their own study group schedules.
- Executive monitoring dashboards for academic health and submission compliance (`SCR-ACD-MONITORING`).
- High-level financial receivables, arrears aging breakdown, and collection reconciliation dashboards for leadership governance (`SCR-FIN-RECEIVABLES`).
- Specialized read-only aggregation queries and backend API endpoints backing the consumer portals and executive monitoring views.

## Evidence

- Development Roadmap: [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md) (Section 3: Stage 4 Consumer Portals & Executive Governance)
- System Architecture: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
- Backend Architecture: [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
- Frontend Architecture: [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
- UI/UX Guidelines: [ui-ux-guideline.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ui-ux-guideline.md)
- Screen Inventory: [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) (`SCR-PRT-PORTAL`, `SCR-STD-PORTAL`, `SCR-ACD-MONITORING`, `SCR-FIN-RECEIVABLES`)
- Use Case Discovery: [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (`UC-PRT-01..04`, `UC-STD-01..03`, `UC-ADM-21`, `UC-ADM-26`, `UC-PRC-06`, `UC-FIN-09`)
- Academic Table Design: [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md)
- Finance Table Design: [finance-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/finance-tables-design.md)
- Operational Table Design: [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md)
- Student Table Design: [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md)
- Roles & Actors Specification: [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md) (`PARENT`, `STUDENT`, `PRINCIPAL`, `SCHOOL_ADMIN`, `FINANCE_OFFICER`)

## Notes

- Intake captured in accordance with the ELSASA-SIS v4 development roadmap for Stage 4.
- Solution-neutral intake: captures the requested system capabilities, stakeholder access boundaries, and operational oversight requirements without prescribing technical implementation details, architectural partitioning, or slice breakdown, which will be formally evaluated during Discovery and Feasibility Assessment by the Analyst (`ica-analyst`).
- Next workflow stage: Discovery and Feasibility Assessment by `ica-analyst`.
