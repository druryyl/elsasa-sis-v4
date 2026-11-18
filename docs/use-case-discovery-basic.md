# Elsasa SIS (Basic) — Actor-Driven Use-Case Discovery

**Document Type:** Cross-Domain Functional & Architectural Specification  
**Scope:** Basic Level (6 Bounded Contexts: `opr`, `std`, `tch`, `acd`, `fin`, `dev`)  
**Source Inputs:**  
* 6 Domain Definitions & Pure Persistence Database Designs (`opr`, `std`, `tch`, `acd`, `fin`, `dev`)  
* `actors-and-roles-basic.md` (5 Internal Core Actors + 2 External Consumer Actors)  

**Development Roadmap Alignment:**  
1. ~~Domain Definition~~ *(Completed)*  
2. ~~Persistence Design~~ *(Completed)*  
3. ~~Actor Discovery~~ *(Completed — `actors-and-roles-basic.md`)*  
4. **Use-Case Discovery** *(This Document)*  
5. **Screen Inventory** *(Next Step — Based on Use Cases & Actors)*  
6. **Development** *(Vertical Slices & UI)*

---

## 1. Executive Summary & Use-Case Taxonomy

This document enumerates all **68 Use Cases** required for **Elsasa SIS (Basic)**, organized strictly from the perspective of **Actor Goals** rather than raw database CRUD tables.

### Classification Conventions
* **Use Case ID Format:** `UC-<ACTOR_PREFIX>-<NN>`
  * `ADM` — `SCHOOL_ADMIN` (School Administrator / Tata Usaha)
  * `FIN` — `FINANCE_OFFICER` (Finance Officer / Bendahara)
  * `TCH` — `TEACHER` (Teacher / Guru Mata Pelajaran & Pembina Ekskul)
  * `HRM` — `HOMEROOM_TEACHER` (Homeroom Teacher / Wali Kelas)
  * `PRC` — `PRINCIPAL` (Principal / Kepala Sekolah)
  * `PRT` — `PARENT` (Parent / Guardian — Read-Only Portal)
  * `STD` — `STUDENT` (Student — Read-Only Portal)
* **Operation Type:**
  * **`COMMAND`**: Mutates domain state, enforces business invariants, and maps to one or more Vertical Slice Command Handlers.
  * **`QUERY`**: Reads data or aggregated projections without mutating state, mapping to Vertical Slice Query Handlers.

### Summary Matrix

| Actor Code | Actor Name | Access Profile | Command UCs | Query UCs | Total UCs | Primary Bounded Contexts |
| :--- | :--- | :--- | :---: | :---: | :---: | :--- |
| `SCHOOL_ADMIN` | School Administrator | Internal (Write) | 22 | 5 | **27** | `opr`, `std`, `tch`, `acd`, `dev`, `fin` (read) |
| `FINANCE_OFFICER` | Finance Officer | Internal (Write) | 6 | 5 | **11** | `fin`, `std` (read), `opr` (read) |
| `TEACHER` | Teacher | Internal (Write / Scoped) | 8 | 3 | **11** | `acd`, `dev`, `opr` |
| `HOMEROOM_TEACHER` | Homeroom Teacher | Internal (Write / Group-Scoped) | 2 | 3 | **5** | `acd`, `dev`, `std` (read) |
| `PRINCIPAL` | Principal | Internal (Governance & Oversight) | 2 | 5 | **7** | `std`, `acd`, `dev`, `tch` (read), `fin` (read), `opr` (read) |
| `PARENT` | Parent / Guardian | External (Read-Only) | 0 | 4 | **4** | `acd`, `fin`, `opr` |
| `STUDENT` | Student | External (Read-Only) | 0 | 3 | **3** | `opr` |
| **TOTAL** | | | **40** | **28** | **68** | |

---

## 2. `SCHOOL_ADMIN` — School Administrator *(Tata Usaha / Admin Sekolah)*

**Actor Goal:** Establish and maintain the school's operational setup, student admissions/enrollments/placements, teacher profiles/assignments, curriculum structure, and extracurricular activities.

### 2.1 Operational Setup & Scheduling (`opr`)

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-ADM-01` | `COMMAND` | **Manage Academic Years & Operational Periods** | Create/update Academic Years and their constituent Operational Periods (Semester 1 & 2), and activate one Academic Year as the current operating cycle (deactivating any prior active year). | `opr.academic_years`<br>`opr.operational_periods` | `BR-OPS-001`..`006` |
| `UC-ADM-02` | `COMMAND` | **Manage School Calendar Entries** | Classify dates within an Academic Year as `SCHOOL_DAY`, `HOLIDAY`, or `CLOSURE` with optional descriptions. | `opr.calendar_entries` | `BR-OPS-007`, `BR-OPS-008` |
| `UC-ADM-03` | `COMMAND` | **Manage Room Master Data** | Register and update physical school rooms (`CLASSROOM`, `LABORATORY`, `HALL`, `OTHER`) with unique names and positive capacities. | `opr.rooms` | `BR-OPS-010`, `BR-OPS-011` |
| `UC-ADM-04` | `COMMAND` | **Configure Study Group Timetables & Block Rotations** | Define weekly schedule slots (`REGULAR`, or SMK `GENERAL`/`VOCATIONAL` templates) per Operational Period and Study Group, and plot date-window block rotations for SMK study groups. | `opr.study_group_schedules`<br>`opr.study_group_timetables` | `BR-OPS-009`, `EXT-OPS-001`..`004` |
| `UC-ADM-05` | `COMMAND` | **Author & Publish School Announcements** | Compose draft announcements and publish them (`DRAFT` $\rightarrow$ `PUBLISHED`) to target audiences (`ALL`, `TEACHERS`, `STUDENTS`, `PARENTS`). | `opr.announcements` | `BR-OPS-012`, `BR-OPS-013` |

### 2.2 Student Admissions, Master Data & Placement (`std`)

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-ADM-06` | `COMMAND` | **Register Admission Candidate** | Record prospective student applications for an Admission Year along with at least one pre-enrollment parent/guardian (`REGISTERED`). | `std.candidates`<br>`std.candidate_guardians` | `BR-STU-007` |
| `UC-ADM-07` | `COMMAND` | **Record Candidate Admission Decision** | Record leadership admission decisions transitioning a candidate from `REGISTERED` to `ACCEPTED` or `REJECTED`. | `std.candidates` | `BR-STU-009` |
| `UC-ADM-08` | `COMMAND` | **Enroll Accepted Candidate** | Atomically activate an `ACCEPTED` candidate into an official `ACTIVE` student with a unique NIS, enrollment date, and copied guardian records (`ENROLLED`). | `std.candidates`<br>`std.students`<br>`std.student_guardians` | `BR-STU-001`, `BR-STU-003`, `BR-STU-005`, `BR-STU-006`, `EXT-STU-003` |
| `UC-ADM-09` | `COMMAND` | **Maintain Student Profile & Guardians** | Update student personal details (including optional unique NISN) and add, update, or remove student guardians while preserving at least one guardian on record. | `std.students`<br>`std.student_guardians` | `BR-STU-002`, `BR-STU-003`, `BR-STU-007`, `BR-STU-010` |
| `UC-ADM-10` | `COMMAND` | **Manage Annual Study Groups (Rombel)** | Create and update annual study groups (e.g., 7A, 10 TKJ 1) scoped to an Academic Year, School Level (`SD`/`SMP`/`SMA`/`SMK`), and Grade. | `std.study_groups` | `EXT-STU-002` |
| `UC-ADM-11` | `COMMAND` | **Assign Student Placement** | Place an enrolled student into a School Level, Grade, and optional Study Group for an Academic Year (at most one placement per student per year). | `std.student_placements` | `BR-STU-004`, `BR-STU-008`, `EXT-STU-001` |
| `UC-ADM-12` | `QUERY` | **Browse Student Directory, Candidate Queue & Study Group Rosters** | Search enrolled students, review admission candidates by status, and view/print study group member rosters. | `std.students`<br>`std.candidates`<br>`std.study_groups`<br>`std.student_placements` | — |

### 2.3 Teacher Profiles & Teaching Assignments (`tch`)

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-ADM-13` | `COMMAND` | **Manage Teacher Profiles** | Register new teachers (with unique optional NIP/NIK and at least one contact method), update profiles, or toggle status (`ACTIVE` $\leftrightarrow$ `INACTIVE`). | `tch.teachers` | `BR-TCH-001`, `BR-TCH-002`, `BR-TCH-007`, `BR-TCH-008`, `EXT-TCH-001`, `EXT-TCH-002` |
| `UC-ADM-14` | `COMMAND` | **Maintain Teacher Subject Capabilities** | Add or remove subjects that a teacher is qualified to teach. | `tch.teaching_capabilities` | `BR-TCH-006` |
| `UC-ADM-15` | `COMMAND` | **Assign & Remove Teacher Teaching Assignments** | Assign an `ACTIVE` teacher with matching subject capability to a Subject and Study Group for an Operational Period (`ASSIGNED`), or remove the assignment (`REMOVED`). | `tch.teacher_assignments` | `BR-TCH-003`, `BR-TCH-004`, `BR-TCH-005` |
| `UC-ADM-16` | `QUERY` | **View Teacher Directory & Assignment Matrix** | Inspect teacher profiles, subject capability lists, and teaching assignment allocations per study group or semester. | `tch.teachers`<br>`tch.teaching_capabilities`<br>`tch.teacher_assignments` | — |

### 2.4 Curriculum Structure & Academic Reporting (`acd`)

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-ADM-17` | `COMMAND` | **Manage Learning Structure (Subjects, Stages, Units & Grade Mappings)** | Define Subjects, ordered Subject Stages, ordered Learning Units (`MODULE`, `PACE`, `LESSON`, `PRESENTATION`, `BOOK`, `OTHER`), and optional Stage-to-Administrative-Grade mappings. | `acd.subjects`<br>`acd.subject_stages`<br>`acd.learning_units`<br>`acd.subject_stage_grades` | `BR-LSTR-001`..`005` |
| `UC-ADM-18` | `COMMAND` | **Establish Student Academic Participations** | Link students to Subject Stages for an Operational Period (`ACTIVE`), either via bulk cohort Grade mappings or individual proficiency placement. | `acd.academic_participations` | `BR-APRT-001`..`003` |
| `UC-ADM-19` | `COMMAND` | **Generate Draft Semester Report Cards** | Trigger batch or individual computation of weighted/simple average final grades and letter grades to generate `DRAFT` report cards for an Operational Period. | `acd.report_cards`<br>`acd.report_card_entries` | `BR-RPCD-001`..`004` |
| `UC-ADM-20` | `COMMAND` | **Finalize Report Cards (Administrative Override)** | Confirm and lock a `DRAFT` report card to `FINALIZED` when administrative completion is needed outside homeroom teacher workflow. | `acd.report_cards`<br>`acd.academic_participations` | `EXT-ACD-001` |
| `UC-ADM-21` | `QUERY` | **Monitor School-Wide Attendance, Assessment & Report Card Progress** | Inspect attendance summaries, grade entry completeness, and report card draft/finalization status across study groups. | `acd.attendance_records`<br>`acd.assignments`<br>`acd.report_cards` | — |

### 2.5 Extracurriculars & Cross-Domain Oversight (`dev`, `fin`, `opr`)

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-ADM-22` | `COMMAND` | **Manage Extracurricular Activity Catalog** | Create extracurricular activities, assign a supervisor (`tch.teachers`), update details, and toggle status (`ACTIVE` $\leftrightarrow$ `INACTIVE`). | `dev.extracurricular_activities` | `BR-DEV-007`, `BR-DEV-008` |
| `UC-ADM-23` | `COMMAND` | **Manage Student Extracurricular Participations** | Enroll students into `ACTIVE` extracurricular activities for an Academic Year and transition participations to terminal `COMPLETED` or `WITHDRAWN` states. | `dev.extracurricular_participations` | `BR-DEV-009`, `BR-DEV-010`, `EXT-DEV-002` |
| `UC-ADM-24` | `COMMAND` | **Record Administrative Discipline or Achievement Entry** | Record student code-of-conduct violations/sanctions or non-academic achievements reported directly to the school office. | `dev.discipline_records`<br>`dev.achievements` | `BR-DEV-004`..`006`, `BR-DEV-011`..`014` |
| `UC-ADM-25` | `QUERY` | **View Extracurricular Activity Rosters** | Inspect enrolled students and participation statuses per extracurricular activity and Academic Year. | `dev.extracurricular_activities`<br>`dev.extracurricular_participations` | — |
| `UC-ADM-26` | `QUERY` | **View Financial Receivables Summary *(Read-Only)*** | Check student outstanding balances and study group arrears summaries for administrative coordination (no write capability). | `fin.student_charges`<br>`fin.payment_receipts` | `BR-FIN-012` |
| `UC-ADM-27` | `QUERY` | **View School Calendar & Master Timetables** | View school calendar entries, room schedules, and published announcements across the school. | `opr.calendar_entries`<br>`opr.study_group_schedules`<br>`opr.announcements` | — |

---

## 3. `FINANCE_OFFICER` — Finance Officer *(Bendahara / Staf Keuangan)*

**Actor Goal:** Manage the school's fee catalog, student waivers/discounts, billing cycles, incoming payments and advance guardian deposits, fund allocations, and financial reporting (`fin`).

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-FIN-01` | `COMMAND` | **Manage Charge Types (Fee Structure)** | Create, update, and activate (`DRAFT` $\rightarrow$ `ACTIVE`) fee definitions with frequency (`MONTHLY`, `SEMESTER`, `YEARLY`, `ONE_TIME`), unit amount, and scope (School Level, Grade, or Study Group). | `fin.charge_types` | `BR-FIN-001`, `BR-FIN-002` |
| `UC-FIN-02` | `COMMAND` | **Manage Standing Student Discounts** | Create, update, or toggle standing per-student waivers/discounts (`FIXED_AMOUNT` or `PERCENTAGE`) for a specific Charge Type and Academic Year. | `fin.student_discounts` | `BR-FIN-006` |
| `UC-FIN-03` | `COMMAND` | **Generate Bulk Student Charges** | Generate charges at once for all students in a Study Group, Grade, or School Level for a `billing_month`, Operational Period, or Academic Year, automatically applying standing discounts and skipping duplicates. | `fin.student_charges`<br>`fin.student_discounts` | `BR-FIN-003`..`005`, `BR-FIN-007` |
| `UC-FIN-04` | `COMMAND` | **Create Individual Student Charge** | Issue a specific charge to an individual student, supporting variable quantities (e.g., multiple PACE workbooks) and inline manual discounts. | `fin.student_charges` | `BR-FIN-003`..`005`, `BR-FIN-007` |
| `UC-FIN-05` | `COMMAND` | **Record Payment Receipt or Guardian Deposit** | Record incoming `CASH` or `BANK_TRANSFER` money from a payer with a unique receipt number—either for immediate settlement or as an advance multi-month guardian deposit. | `fin.payment_receipts` | `BR-FIN-008`, `BR-FIN-009` |
| `UC-FIN-06` | `COMMAND` | **Allocate Payment or Deposit Balance to Charges** | Apply funds from a new receipt or existing unallocated deposit balance (`unallocated_amount > 0`) to one or more unpaid/partially paid charges (`PARTIALLY_PAID` / `PAID`). | `fin.payment_receipts`<br>`fin.payment_allocations`<br>`fin.student_charges` | `BR-FIN-009`..`011`, `EXT-FIN-001` |
| `UC-FIN-07` | `QUERY` | **View & Print Official Payment Receipt** | Retrieve receipt details, itemized charge allocations, and remaining unallocated deposit balance for printing or PDF export. | `fin.payment_receipts`<br>`fin.payment_allocations` | — |
| `UC-FIN-08` | `QUERY` | **View Student Monthly Billing Statement & Deposit Position** | Inspect a student's itemized charges grouped by `billing_month`, net payable amounts, paid amounts, total outstanding balance, and remaining deposit balance. | `fin.student_charges`<br>`fin.payment_receipts` | `BR-FIN-012` |
| `UC-FIN-09` | `QUERY` | **View Study Group Arrears & Period Collection Reports** | Query students with unpaid/partially paid balances per study group (*tunggakan*) and summarize cash/bank receipts collected over a date range or period. | `fin.student_charges`<br>`fin.payment_receipts` | `BR-FIN-012` |
| `UC-FIN-10` | `QUERY` | **Lookup Students & Guardians *(Read-Only)*** | Search enrolled students, NIS, study group placements, and guardian names/contacts to identify payers and billing targets. | `std.students`<br>`std.student_guardians`<br>`std.student_placements` | — |
| `UC-FIN-11` | `QUERY` | **View School Calendar & Announcements *(Read-Only)*** | View active Academic Year, Operational Periods, school operating days, and internal announcements. | `opr.academic_years`<br>`opr.calendar_entries`<br>`opr.announcements` | — |

---

## 4. `TEACHER` — Teacher *(Guru Mata Pelajaran / Pembina Ekskul)*

**Actor Goal:** Deliver daily classroom instruction, record subject attendance, create and grade assignments/exams, write subject notes on report cards, log student behaviour/discipline/achievements, and publish student/parent announcements.

### 4.1 Classroom Instruction & Assessment (`acd`)

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-TCH-01` | `COMMAND` | **Record Daily Subject Attendance** | Record or update student presence (`PRESENT`, `ABSENT`, `EXCUSED`, `SICK`, `LATE`) per Subject and date for students with active participation in that Subject. | `acd.attendance_records` | `BR-ATTN-001`..`003` |
| `UC-TCH-02` | `COMMAND` | **Create & Manage Assignments / Exams** | Define assessed works (`ASSIGNMENT`, `QUIZ`, `MIDTERM`, `FINAL`) within a Subject Stage (optionally linked to a Learning Unit) with optional percentage weights. | `acd.assignments` | `BR-ASGN-001`, `BR-ASGN-002`, `BR-ASGN-006` |
| `UC-TCH-03` | `COMMAND` | **Enter & Update Student Scores** | Record or correct numeric scores (`0.00–100.00`) for students holding `ACTIVE` participation in the assignment's Subject Stage. | `acd.assignment_scores` | `BR-ASGN-003`..`005` |
| `UC-TCH-04` | `COMMAND` | **Write Subject Stage Note on Draft Report Card** | Add or update `teacher_note` on a student's `DRAFT` report card entry for a Subject Stage taught by the teacher. | `acd.report_card_entries` | `EXT-ACD-001` |
| `UC-TCH-05` | `QUERY` | **View Subject Roster, Attendance Log & Scorebook** | View enrolled participants for assigned Subject Stages, historical attendance sheets, and assessment scorebooks. | `acd.academic_participations`<br>`acd.attendance_records`<br>`acd.assignments`<br>`acd.assignment_scores` | — |

### 4.2 Student Character & Extracurricular Supervision (`dev`)

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-TCH-06` | `COMMAND` | **Record Student Behaviour Note** | Log a dated `POSITIVE` or `NEGATIVE` behaviour observation for an enrolled student. | `dev.behaviour_notes` | `BR-DEV-001`..`003` |
| `UC-TCH-07` | `COMMAND` | **Record Student Discipline Violation & Sanction** | Record a code-of-conduct breach with violation type, incident description, and sanction (`VERBAL_WARNING`, `WRITTEN_WARNING`, `PARENT_MEETING`, `SUSPENSION`). | `dev.discipline_records` | `BR-DEV-004`..`006`, `BR-DEV-014` |
| `UC-TCH-08` | `COMMAND` | **Record Student Non-Academic Achievement** | Log a student's competition or non-academic accomplishment with category, event name, level (`SCHOOL`, `CITY`, `PROVINCE`, `NATIONAL`), and date. | `dev.achievements` | `BR-DEV-011`..`013` |
| `UC-TCH-09` | `QUERY` | **View Supervised Extracurricular Roster** | *(For teachers assigned as `supervisor_id`)* View active and historical student participants in the extracurricular activities they supervise. | `dev.extracurricular_activities`<br>`dev.extracurricular_participations` | — |

### 4.3 Daily Schedule & Communication (`opr`)

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-TCH-10` | `COMMAND` | **Publish Announcement to Students or Parents** | Compose and publish announcements (`DRAFT` $\rightarrow$ `PUBLISHED`) targeted at `STUDENTS` or `PARENTS`. | `opr.announcements` | `BR-OPS-012`, `BR-OPS-013` |
| `UC-TCH-11` | `QUERY` | **View Personal Teaching Schedule & School Calendar** | View assigned weekly teaching timetable slots (resolving SMK `GENERAL`/`VOCATIONAL` block dates when applicable), rooms, and school calendar entries. | `opr.study_group_schedules`<br>`opr.study_group_timetables`<br>`opr.calendar_entries` | — |

---

## 5. `HOMEROOM_TEACHER` — Homeroom Teacher *(Wali Kelas)*

**Actor Goal:** Oversee holistic student well-being and academic completeness for their assigned Study Group, review semester report cards, write general remarks, and finalize report cards for distribution.  
*(Note: Inherits all `TEACHER` use cases `UC-TCH-01` through `UC-TCH-11` for subjects they teach.)*

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-HRM-01` | `QUERY` | **Review Draft Report Cards for Assigned Study Group** | Inspect `DRAFT` report cards for all students in the homeroom teacher's study group (`homeroom_teacher_id`), verifying completeness of final grades and subject teacher notes. | `acd.report_cards`<br>`acd.report_card_entries` | — |
| `UC-HRM-02` | `COMMAND` | **Write General Remarks on Draft Report Card** | Enter or edit overall homeroom narrative feedback (`general_remarks`) on a student's `DRAFT` report card. | `acd.report_cards` | `EXT-ACD-001` |
| `UC-HRM-03` | `COMMAND` | **Finalize Study Group Report Cards** | Confirm and lock student report cards (`DRAFT` $\rightarrow$ `FINALIZED`), setting `finalized_at` and transitioning corresponding `acd.academic_participations` to `COMPLETED`. | `acd.report_cards`<br>`acd.academic_participations` | `EXT-ACD-001` |
| `UC-HRM-04` | `QUERY` | **View Holistic Student Development Profile** | Inspect consolidated non-academic records (behaviour notes, discipline records, extracurricular participations, and achievements) for students in their study group. | `dev.behaviour_notes`<br>`dev.discipline_records`<br>`dev.extracurricular_participations`<br>`dev.achievements` | — |
| `UC-HRM-05` | `QUERY` | **View Study Group Roster & Guardian Contacts** | View enrolled student details and father/mother/guardian contact information for students in their study group. | `std.students`<br>`std.student_guardians`<br>`std.student_placements` | — |

---

## 6. `PRINCIPAL` — Principal *(Kepala Sekolah)*

**Actor Goal:** Exercise executive leadership over student admissions, serious disciplinary actions, academic quality assurance, teaching load oversight, and school financial health.

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-PRC-01` | `COMMAND` | **Approve or Reject Admission Candidates** | Review registered admission applicants (`REGISTERED`) and issue executive admission decisions (`ACCEPTED` or `REJECTED`). | `std.candidates` | `BR-STU-009` |
| `UC-PRC-02` | `QUERY` | **Review Draft & Finalized Student Report Cards** | Inspect semester report cards across all study groups prior to or following parent distribution. | `acd.report_cards`<br>`acd.report_card_entries` | — |
| `UC-PRC-03` | `COMMAND` | **Record Escalated Disciplinary Sanction** | Record severe code-of-conduct violations and leadership-level sanctions (such as `PARENT_MEETING` or `SUSPENSION`). | `dev.discipline_records` | `BR-DEV-004`..`006`, `BR-DEV-014` |
| `UC-PRC-04` | `QUERY` | **Monitor School-Wide Student Development, Discipline & Achievements** | View individual holistic development profiles and school-wide trends in behaviour, disciplinary sanctions, and student achievements. | `dev.behaviour_notes`<br>`dev.discipline_records`<br>`dev.extracurricular_participations`<br>`dev.achievements` | — |
| `UC-PRC-05` | `QUERY` | **Review Teacher Assignments & Workload Distribution** | Inspect teacher assignment allocations across subjects and study groups to direct staffing adjustments. | `tch.teachers`<br>`tch.teacher_assignments` | — |
| `UC-PRC-06` | `QUERY` | **Monitor School Financial Collection & Arrears Reports *(Read-Only)*** | Review school-wide receivable totals, arrears per school level/study group, and period collection summaries. | `fin.student_charges`<br>`fin.payment_receipts` | `BR-FIN-012` |
| `UC-PRC-07` | `QUERY` | **View School Calendar, Timetables & Announcements** | View school operating calendar, study group schedules, and published announcements. | `opr.calendar_entries`<br>`opr.study_group_schedules`<br>`opr.announcements` | — |

---

## 7. `PARENT` — Parent / Guardian *(Orang Tua / Wali Siswa)*

**Actor Goal:** Stay informed about their child's finalized academic results, monthly school fee obligations, official payment receipts, school calendar, and parent-targeted announcements (*Read-Only Portal*).

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-PRT-01` | `QUERY` | **View & Download Child's Finalized Report Card** | View finalized (`FINALIZED`) semester report cards for linked children, including subject stage grades, teacher notes, and homeroom general remarks. | `acd.report_cards`<br>`acd.report_card_entries` | Only `status = 'FINALIZED'` visible |
| `UC-PRT-02` | `QUERY` | **View Child's Monthly Billing Statements & Deposit Balance** | View itemized monthly charges (`billing_month`), discounts applied, paid/outstanding balances, and remaining advance deposit balance for linked children. | `fin.student_charges`<br>`fin.payment_receipts` | `BR-FIN-012` |
| `UC-PRT-03` | `QUERY` | **View & Download Official Payment Receipts** | View payment history and download official receipts showing itemized allocations and remaining deposit balances. | `fin.payment_receipts`<br>`fin.payment_allocations` | — |
| `UC-PRT-04` | `QUERY` | **View School Calendar & Parent Announcements** | View the active Academic Year school calendar and published announcements targeted at `ALL` or `PARENTS`. | `opr.calendar_entries`<br>`opr.announcements` | `status = 'PUBLISHED'`, `target_audience IN ('ALL', 'PARENTS')` |

---

## 8. `STUDENT` — Student *(Siswa)*

**Actor Goal:** Access their daily/weekly class timetable, school operating calendar, and student-targeted announcements (*Read-Only Portal*).

| UC ID | Type | Use Case Name | Goal & Behavioral Description | Primary Tables | Key Business Rules |
| :--- | :---: | :--- | :--- | :--- | :--- |
| `UC-STD-01` | `QUERY` | **View Personal Study Group Timetable** | View the weekly class schedule for their assigned Study Group in the active Operational Period (automatically resolving SMK `GENERAL`/`VOCATIONAL` rotation blocks by date). | `opr.study_group_schedules`<br>`opr.study_group_timetables` | — |
| `UC-STD-02` | `QUERY` | **View School Calendar** | View school days, holidays, and closures for the active Academic Year. | `opr.calendar_entries` | — |
| `UC-STD-03` | `QUERY` | **View Student Announcements** | Read published announcements targeted at `ALL` or `STUDENTS`. | `opr.announcements` | `status = 'PUBLISHED'`, `target_audience IN ('ALL', 'STUDENTS')` |

---

## 9. Traceability Guide for Next Step: Screen Inventory (Step 5)

When translating these **68 Use Cases** into **Step 5: Screen Inventory**, apply the following consolidation rules to keep the UI clean and pragmatic:

1. **Multi-Actor Shared Screens with Role-Based Action Controls:**
   * **Student Directory & Detail Screen:** Serves `UC-ADM-09`, `UC-ADM-11`, `UC-ADM-12` (full write for `SCHOOL_ADMIN`), `UC-FIN-10` (read-only lookup for `FINANCE_OFFICER`), and `UC-HRM-05` (scoped read-only for `HOMEROOM_TEACHER`).
   * **Admissions Queue Screen:** Serves `UC-ADM-06`, `UC-ADM-07`, `UC-ADM-08`, `UC-ADM-12` (`SCHOOL_ADMIN`) and `UC-PRC-01` (`PRINCIPAL` decision action).
   * **Report Card Workspace Screen:** Serves `UC-ADM-19`, `UC-ADM-20` (generate & override for `SCHOOL_ADMIN`), `UC-TCH-04` (subject notes for `TEACHER`), `UC-HRM-01`..`03` (review, general remarks, finalize for `HOMEROOM_TEACHER`), and `UC-PRC-02` (oversight for `PRINCIPAL`).
   * **Student Development Profile & Logs Screen:** Serves `UC-TCH-06`..`08` (`TEACHER`), `UC-HRM-04` (`HOMEROOM_TEACHER`), `UC-PRC-03`..`04` (`PRINCIPAL`), and `UC-ADM-24` (`SCHOOL_ADMIN`).
   * **Financial Receivables & Statements Screen:** Serves `UC-FIN-08`..`09` (`FINANCE_OFFICER`), `UC-ADM-26` (`SCHOOL_ADMIN` read-only), and `UC-PRC-06` (`PRINCIPAL` read-only).
2. **Phase-1 Print/PDF Alternative for Consumer Portal (`PARENT` & `STUDENT`):**
   * All 7 consumer read-only use cases (`UC-PRT-01`..`04` and `UC-STD-01`..`03`) map directly to printable/PDF views already needed by internal staff (**Report Card Print View**, **Monthly Billing Statement Print View**, **Official Receipt Print View**, and **Study Group Timetable Print View**), allowing internal staff to operate immediately even before external parent/student portal logins are enabled.