# Elsasa SIS (Basic) — Actors & Roles Specification

**Document Type:** Cross-Domain Architectural Specification  
**Scope:** Basic Level (6 Bounded Contexts: `opr`, `std`, `acd`, `fin`, `tch`, `dev`)  
**Design Principles:** Pragmatic (aligned with Indonesian K-12 school operations) & Minimalist (zero role explosion, multi-role RBAC support, strict financial segregation of duties)  
**Development Roadmap Alignment:**  
1. ~~Domain Definition~~ *(Completed)*  
2. ~~Persistence Design~~ *(Completed)*  
3. **Actor Discovery** *(This Document)*  
4. **Use-Case Discovery** *(Next Step)*  
5. **Screen Inventory** *(Based on Use Cases & Actors)*  
6. **Development** *(Vertical Slices & UI)*

---

## 1. Executive Summary & Design Philosophy

To prevent role explosion during initial system delivery while maintaining real-world operational control across Indonesian K-12 schools (SD/SMP/SMA/SMK), all raw actors identified across the six domain specifications are consolidated into **5 Core Internal Actors (Write-Access)** and **2 Consumer Actors (Read-Only Portal)**.

### Key Architectural Decisions
1. **Pragmatic Role Consolidation:**
   * *Curriculum Coordinator* (`opr`) is merged into **School Administrator** so administrative staff do not need to switch accounts when configuring Academic Years, Teacher Assignments, and Study Group Schedules.
   * *Extracurricular Supervisor* (`dev`) is not a separate system role; any **Teacher** referenced via `supervisor_id` on `dev.extracurricular_activities` manages their activity using their standard teacher account.
2. **Strict Financial Segregation of Duties:**
   * **Finance Officer** is strictly separated from **School Administrator** so only authorized treasury staff can create charge types, issue charges, record payment receipts, and allocate funds.
3. **Composable Multi-Role RBAC (`Teacher` + `HomeroomTeacher`):**
   * Rather than creating separate user accounts, `HomeroomTeacher` is modeled as an additive RBAC role assigned to a `Teacher` user who is designated as a study group's homeroom teacher (`homeroom_teacher_id`).
4. **Phased Consumer Portal Rollout:**
   * Because **Parent / Guardian** and **Student** have zero write commands in the Basic specification, the system can launch Phase 1 with only the **5 Internal Actors** (distributing report cards and receipts as PDF/printouts), then enable self-service portals for Consumers without changing any domain write use cases.

---

## 2. Actor Catalog

### 2.1 Core Internal Actors (Operational / Write Access)

| Role Code | Display Name | Consolidated From | Primary Persona & Scope |
| :--- | :--- | :--- | :--- |
| `SCHOOL_ADMIN` | **School Administrator** *(Tata Usaha / Admin Sekolah)* | • School Administrator (`opr`, `std`, `acd`, `tch`, `dev`)<br>• Curriculum Coordinator (`opr`) | **Primary Master Data & Operational Operator.** Manages school temporal setup, rooms, timetables, student admissions/enrollments/placements, curriculum structure, teacher profiles/assignments, and extracurricular catalogs. Has read-only visibility into financial receivables. |
| `FINANCE_OFFICER` | **Finance Officer** *(Bendahara / Staf Keuangan)* | • Finance Officer (`fin`) | **Treasury & Billing Authority.** Holds exclusive write authority over fee structures (`fin.charge_types`), student discounts, billing generation, payment/deposit receipts, and fund allocations. |
| `TEACHER` | **Teacher** *(Guru Mata Pelajaran / Pembina Ekskul)* | • Teacher (`acd`, `opr`, `dev`)<br>• Supervisor (`dev`) | **Daily Instructional & Pastoral Operator.** Records daily subject attendance, creates assignments/exams, enters student scores, writes subject notes on report cards, logs behaviour/discipline/achievement records, and authors student/parent announcements. |
| `HOMEROOM_TEACHER` | **Homeroom Teacher** *(Wali Kelas)* | • Homeroom Teacher (`acd`, `dev`) | **Study Group Pastoral & Report Card Finalizer.** Additive role for teachers assigned to a study group. Reviews holistic student development profiles, adds general remarks to report cards, and transitions report cards from `DRAFT` to `FINALIZED`. |
| `PRINCIPAL` | **Principal** *(Kepala Sekolah)* | • Principal / School Principal (`std`, `acd`, `tch`, `dev`) | **Executive Oversight & Governance.** Approves/rejects student admission candidates, reviews report cards, monitors school-wide academic/teaching/financial summaries, and records major disciplinary sanctions. |

### 2.2 External Consumer Actors (Read-Only Access)

| Role Code | Display Name | Domain Mapping | Primary Persona & Scope |
| :--- | :--- | :--- | :--- |
| `PARENT` | **Parent / Guardian** *(Orang Tua / Wali)* | • Parent / Guardian (`opr`, `acd`, `fin`) | **Family Information Consumer.** Views published school announcements, school calendar, finalized student report cards (`FINALIZED`), monthly billing statements, and payment receipts. Zero write access. |
| `STUDENT` | **Student** *(Siswa)* | • Student (`opr`) | **Learner Information Consumer.** Views study group timetables, school calendar, and published announcements targeted at students. Zero write access. |

---

## 3. Cross-Domain Permission Matrix

Legend:
* **CRUD / Write** — Full command execution authority for aggregate workflows in that domain.
* **Write (Scoped)** — Command execution restricted to assigned classes/subjects or specific actions.
* **Read** — Query access to views, lists, and reports.
* **—** — No direct domain access required in Basic v1.

| Domain & Schema | `SCHOOL_ADMIN` | `FINANCE_OFFICER` | `TEACHER` | `HOMEROOM_TEACHER` | `PRINCIPAL` | `PARENT` | `STUDENT` |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Operational (`opr`)**<br>*Years, Periods, Calendar, Rooms, Timetables, Announcements* | **CRUD / Write** | Read | Read + Write *(Announcements)* | Read | Read | Read *(Published / Calendar)* | Read *(Timetable / Calendar / Announcements)* |
| **Student (`std`)**<br>*Candidates, Guardians, Students, Study Groups, Placements* | **CRUD / Write** | Read *(Student Lookup)* | Read *(Rosters)* | Read *(Own Study Group)* | Read + Write *(Admission Decision)* | — | — |
| **Academic (`acd`)**<br>*Subjects, Stages, Units, Participation, Attendance, Scores, Report Cards* | **CRUD / Write**<br>*(Structure, Participation, Generate Draft)* | — | **Write (Scoped)**<br>*(Attendance, Assignments, Scores, Subject Notes)* | **Write (Scoped)**<br>*(General Remarks, Finalize Report Card)* | Read *(Oversight & Report Cards)* | Read *(Finalized Report Cards)* | — |
| **Finance (`fin`)**<br>*Charge Types, Discounts, Charges, Receipts, Allocations* | Read *(Receivables Summary)* | **CRUD / Write** | — | — | Read *(Financial Reports)* | Read *(Own Child Bills & Receipts)* | — |
| **Teaching (`tch`)**<br>*Teacher Profiles, Capabilities, Teacher Assignments* | **CRUD / Write** | — | Read *(Own Profile & Assignments)* | Read *(Own Profile & Assignments)* | Read *(Oversight)* | — | — |
| **Development (`dev`)**<br>*Behaviour, Discipline, Extracurriculars, Achievements* | **CRUD / Write**<br>*(Extracurriculars, Discipline, Achievements)* | — | **Write**<br>*(Behaviour, Discipline, Achievements)* | Read *(Holistic Profile)* + **Write** | Read *(Holistic Profile)* + **Write** *(Discipline)* | — | — |

---

## 4. Detailed Responsibilities & Domain Mapping

### 4.1 `SCHOOL_ADMIN` (School Administrator)
* **Operational (`opr`):**
  * Create and activate `opr.academic_years` and define `opr.operational_periods` (Semester 1 & 2).
  * Maintain `opr.calendar_entries` (`SCHOOL_DAY`, `HOLIDAY`, `CLOSURE`) and `opr.rooms`.
  * Create and maintain `opr.study_group_schedules` (`REGULAR`, `GENERAL`, `VOCATIONAL`) and SMK block rotations (`opr.study_group_timetables`).
  * Create, edit, and publish `opr.announcements` to any `target_audience`.
* **Student (`std`):**
  * Register `std.candidates` and `std.candidate_guardians`.
  * Record admission decisions (`ACCEPTED` / `REJECTED`) on behalf of leadership and execute atomic enrollment (`ENROLLED` $\rightarrow$ `std.students` + `std.student_guardians`).
  * Create annual `std.study_groups` and assign `std.student_placements`.
* **Academic (`acd`):**
  * Define `acd.subjects`, `acd.subject_stages`, `acd.learning_units`, and `acd.subject_stage_grades`.
  * Establish `acd.academic_participations` (bulk by grade mapping or individual).
  * Trigger batch or individual generation of `DRAFT` `acd.report_cards`.
* **Teaching (`tch`):**
  * Register and deactivate `tch.teachers`, maintain `tch.teaching_capabilities`, and manage `tch.teacher_assignments`.
* **Development (`dev`):**
  * Create and toggle status (`ACTIVE` / `INACTIVE`) of `dev.extracurricular_activities` and assign `supervisor_id`.
  * Enroll, complete, or withdraw students in `dev.extracurricular_participations`.
* **Finance (`fin`):**
  * Read-only access to student receivable positions (`outstanding_amount`) for administrative coordination.

### 4.2 `FINANCE_OFFICER` (Finance Officer)
* **Finance (`fin`):**
  * Define and activate `fin.charge_types` (`MONTHLY`, `SEMESTER`, `YEARLY`, `ONE_TIME`).
  * Configure standing `fin.student_discounts` (`FIXED_AMOUNT` or `PERCENTAGE`).
  * Generate individual or bulk `fin.student_charges` per `billing_month`, `operational_period_id`, or `academic_year_id`.
  * Record incoming money in `fin.payment_receipts` (`CASH` / `BANK_TRANSFER`) and allocate funds or deposit balances via `fin.payment_allocations`.
  * Query student billing statements, study group arrears (*tunggakan*), deposit balances, and period collection reports.
* **Operational & Student (`opr`, `std`):**
  * Read-only lookup of active academic years, periods, students, and study groups.

### 4.3 `TEACHER` (Teacher)
* **Academic (`acd`):**
  * Record daily `acd.attendance_records` (`PRESENT`, `ABSENT`, `EXCUSED`, `SICK`, `LATE`) for subjects they teach.
  * Create `acd.assignments` (`ASSIGNMENT`, `QUIZ`, `MIDTERM`, `FINAL`) and enter `acd.assignment_scores`.
  * Add `teacher_note` on `acd.report_card_entries` while the parent report card is `DRAFT`.
* **Development (`dev`):**
  * Record `dev.behaviour_notes` (`POSITIVE` / `NEGATIVE`), `dev.discipline_records`, and `dev.achievements`.
  * View activity rosters for `dev.extracurricular_activities` where they are assigned as `supervisor_id`.
* **Operational (`opr`):**
  * View teaching schedules (`opr.study_group_schedules`) and school calendar.
  * Author and publish `opr.announcements` targeted to `STUDENTS` or `PARENTS`.

### 4.4 `HOMEROOM_TEACHER` (Homeroom Teacher)
* *Inherits all `TEACHER` capabilities, plus study-group-scoped governance:*
* **Academic (`acd`):**
  * Review `DRAFT` `acd.report_cards` for students in their assigned study group (`homeroom_teacher_id`).
  * Write `general_remarks` on `acd.report_cards`.
  * Confirm and lock report cards (`DRAFT` $\rightarrow$ `FINALIZED`), transitioning related `acd.academic_participations` to `COMPLETED`.
* **Development (`dev`):**
  * Query consolidated **Student Development Profiles** (combining behaviour notes, discipline records, extracurricular participations, and achievements) for students in their study group.

### 4.5 `PRINCIPAL` (Principal)
* **Student (`std`):**
  * Review `std.candidates` and approve/reject admission applications (`REGISTERED` $\rightarrow$ `ACCEPTED` | `REJECTED`).
* **Academic (`acd`):**
  * Review `DRAFT` and `FINALIZED` `acd.report_cards` across all study groups.
* **Development (`dev`):**
  * Review school-wide and student-level development profiles; record `dev.discipline_records` for severe violations (e.g., `PARENT_MEETING`, `SUSPENSION`).
* **Teaching & Finance (`tch`, `fin`):**
  * Read-only oversight of `tch.teacher_assignments` and financial collection/arrears summaries.

---

## 5. Guide for Next Step: Use-Case Discovery

This actor catalog serves as the direct input for **Step 4: Use-Case Discovery** across all 6 bounded contexts:

1. **Actor-Goal Mapping:**
   * For every domain (`opr`, `std`, `tch`, `acd`, `fin`, `dev`), enumerate concrete **Command Use Cases** (state-mutating actions) and **Query Use Cases** (read/reporting goals) driven strictly by the actors listed in Section 2.
2. **Scope & Ownership Boundaries:**
   * Distinguish between **School-Wide Use Cases** (`SCHOOL_ADMIN`, `FINANCE_OFFICER`, `PRINCIPAL`) and **Scoped Use Cases** (`TEACHER` scoped to assigned subjects/activities; `HOMEROOM_TEACHER` scoped to assigned study group; `PARENT`/`STUDENT` scoped to linked student/self).
3. **Downstream Traceability to Screen Inventory (Step 5):**
   * Each discovered use case will map directly to:
     * A user interface screen, modal, or action widget in **Step 5 (Screen Inventory)**.
     * A self-contained Vertical Slice handler in **Step 6 (Development)**.