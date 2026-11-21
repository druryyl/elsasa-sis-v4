---
Title: Stage 4 Consumer Portals & Executive Governance — Test Package
Code: CR-STAGE-4
Artifact: TEST-PACKAGE
Version: 1.0
Date: 2026-11-21
Status: READY-FOR-EXECUTION
Target Plan: docs/issues/IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md
Target Feature: docs/issues/IS005- STAGE-4-DEV-ISSUE.md
Target Architecture: docs/issues/IS005- STAGE-4-DEV-ARCHITECTURE.md
Audience: Human Tester / Quality Assurance / Reviewer / Stakeholder
---

# 1. Overview & Purpose

This **TEST-PACKAGE** provides human-executable, tester-agnostic manual verification instructions for **CR-STAGE-4** (**Stage 4: Consumer Portals & Executive Governance** of **ELSASA-SIS v4**). 

The underlying **IMPLEMENTATION-PLAN** ([IS005- STAGE-4-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS005-%20STAGE-4-DEV-IMPLEMENTATION-PLAN.md)) is **COMPLETED**, with all 14 slices across 5 phases marked `IMPLEMENTED` and reviewed as `GO`.

The purpose of this package is to guide a human tester through step-by-step validation of the live, running system as a cohesive whole. Human readability and observable behavior take precedence over internal architecture jargon.

### Scope of Validation
1. **Parent Family Self-Service Portal (`SCR-PRT-PORTAL` at `/portal/parent`):**
   - Multi-child switching (`Ahmad Dahlan` and `Siti Nurhaliza`).
   - Finalized academic report cards viewing and browser print preview (draft report cards strictly hidden).
   - Monthly tuition billing statements, charge breakdowns, discounts, and advance deposit balance.
   - Official payment receipts inspection and browser print preview.
   - Active school calendar and parent-targeted bulletins.
2. **Student Learner Self-Service Portal (`SCR-STD-PORTAL` at `/portal/student`):**
   - Student identification header, NIS, and study group banner.
   - SMK vocational block rotation cycle calculation ("Siklus Kejuruan" vs "Siklus Umum").
   - Today's Class Agenda timeline and full weekly timetable grid.
   - Academic calendar entries and student-targeted bulletins.
3. **School-Wide Academic Progress Monitoring Dashboard (`SCR-ACD-MONITORING` at `/academic/monitoring`):**
   - Real-time school-wide attendance taking completeness KPI.
   - Assessment grading submission completeness KPI and overdue teacher alerts.
   - Semester report card finalization readiness KPI.
   - Operational exception banners and progressive study group drill-downs.
4. **Financial Receivables, Arrears Aging & Collection Governance (`SCR-FIN-RECEIVABLES` at `/finance/receivables`):**
   - Total outstanding school receivables and aged tuition arrears summary cards.
   - Study group arrears breakdown table (*Laporan Tunggakan per Rombel*).
   - Period collection transaction ledger (Cash vs Bank Transfer).
   - Read-only student billing statement inspection drawer (zero write actions).
5. **Cross-Role Boundary Enforcement, Data Isolation & Responsiveness:**
   - Strict parent child-ownership boundaries (preventing access to unauthorized children).
   - Portal shell (`PortalLayout.vue`) vs Administrative shell (`AppLayout.vue`) routing guards.
   - Audience filtering for school announcements.
   - Mobile touch targets ($\ge 44\text{px}$) and responsive layout on mobile screens ($360\text{px}$ to $390\text{px}$).

---

# 2. Environment & Test Prerequisites

Before beginning test execution, ensure the local environment is operational:

### 2.1 Services & URLs
- **Backend API:** `http://localhost:52931` (or `https://localhost:52930`)
- **Frontend Web Application:** `http://localhost:5173`
- **Supported Browsers:** Google Chrome, Microsoft Edge, Mozilla Firefox, or Safari (Chromium-based browser recommended for print preview verification).
- **Mobile Emulation:** Google Chrome DevTools Device Mode (Preset: *iPhone 12/14/15 Pro* $390 \times 844$ or *Samsung Galaxy S20* $360 \times 800$).

### 2.2 Starting the Environment
1. **Database:** Ensure local Microsoft SQL Server is running and migrations/seeds (up to `102_SeedStage4Data.sql`) are applied:
   ```powershell
   # From repository root
   cd d:\Project.Private\elsasa-sis-v4\src\backend
   dotnet run --project src/ElsasaSis.Api
   ```
2. **Frontend:** In a separate terminal, launch the development web server:
   ```powershell
   cd d:\Project.Private\elsasa-sis-v4\src\frontend
   pnpm dev
   ```
3. Open `http://localhost:5173` in your web browser.

---

# 3. Test Personas & Test Data

The application includes pre-configured deterministic seed accounts. Use the following credentials during test execution:

| Persona Role | Username | Password | Display Name | Associated Data Context |
|---|---|---|---|---|
| **Parent** | `parent.hendra` | `Parent123!` | Hendra Dahlan | Linked to 2 children: **Ahmad Dahlan** (X-TKJ-A) & **Siti Nurhaliza** (X-TKJ-B) |
| **Student (Child 1)** | `student.ahmad` | `Student123!` | Ahmad Dahlan | NIS: `202607001`, Class: `X-TKJ-A`, Major: `Teknik Komputer dan Jaringan` |
| **Student (Child 2)** | `student.siti` | `Student123!` | Siti Nurhaliza | NIS: `202607002`, Class: `X-TKJ-B`, Major: `Teknik Komputer dan Jaringan` |
| **Principal** | `principal` | `Principal123!` | Dr. H. Mulyadi, M.Pd. | Executive governance oversight; access to academic monitoring and financial oversight |
| **School Administrator** | `admin` | `Admin123!` | System Administrator | Full institutional access across master data, operations, academic monitoring |
| **Finance Officer** | `finance.officer` | `Finance123!` | Siti Rahma | Finance operational workbench, receivables governance, cash/bank transactions |
| **Teacher / Homeroom**| `teacher.budi` | `Teacher123!` | Budi Santoso, S.Kom. | Homeroom teacher for X-TKJ-A; subject teacher for Pemrograman Dasar |

---

# 4. Test Execution Matrix & Traceability

| Test Case ID | Test Category | Target Screen / Context | Use Case ID | Primary Persona |
|---|---|---|---|---|
| **TC-STG4-HP-01** | Happy Path | `/portal/parent` | `UC-PRT-01` | `parent.hendra` |
| **TC-STG4-HP-02** | Happy Path | `/portal/parent` | `UC-PRT-02`, `UC-PRT-03` | `parent.hendra` |
| **TC-STG4-HP-03** | Happy Path | `/portal/parent` | `UC-PRT-04` | `parent.hendra` |
| **TC-STG4-HP-04** | Happy Path | `/portal/student` | `UC-STD-01` | `student.ahmad` |
| **TC-STG4-HP-05** | Happy Path | `/portal/student` | `UC-STD-02`, `UC-STD-03` | `student.ahmad` |
| **TC-STG4-HP-06** | Happy Path | `/academic/monitoring` | `UC-ADM-21` | `admin` / `principal` |
| **TC-STG4-HP-07** | Happy Path | `/finance/receivables` | `UC-ADM-26`, `UC-PRC-06` | `principal` / `finance.officer` |
| **TC-STG4-HP-08** | Happy Path | `/finance/receivables` | `UC-FIN-09` | `finance.officer` / `admin` |
| **TC-STG4-BR-01** | Business Rules | `/portal/parent` | `UC-PRT-01` | `parent.hendra` |
| **TC-STG4-BR-02** | Business Rules | `/portal/parent` | `UC-PRT-01` | `parent.hendra` |
| **TC-STG4-BR-03** | Business Rules | `/portal/student` | `UC-STD-01` | `student.ahmad` / `student.siti` |
| **TC-STG4-BR-04** | Business Rules | `/finance/receivables` | `UC-ADM-26` | `principal` / `admin` |
| **TC-STG4-VR-01** | Validation Rules | `/login` $\rightarrow$ Portal | Cross-Portal | All Personas |
| **TC-STG4-VR-02** | Validation Rules | Announcements | `UC-PRT-04`, `UC-STD-03` | `parent.hendra` vs `student.ahmad` |
| **TC-STG4-EH-01** | Error Handling | Direct URL Navigation | Security / RBAC | `parent.hendra` / `student.ahmad` |
| **TC-STG4-EH-02** | Error Handling | Direct API Infiltration | Data Boundary | `parent.hendra` |
| **TC-STG4-EH-03** | Error Handling | Invalid Credentials | Authentication | Non-existent User |
| **TC-STG4-RG-01** | Regression | Mobile Viewport ($390\text{px}$) | `SCR-PRT-PORTAL` | `parent.hendra` |
| **TC-STG4-RG-02** | Regression | Mobile Viewport ($360\text{px}$) | `SCR-STD-PORTAL` | `student.ahmad` |
| **TC-STG4-RG-03** | Regression | Desktop Admin Navigation | `AppLayout.vue` | `admin` / `principal` |

---

# 5. Detailed Test Cases

## Group 1: Happy Path Scenarios

### TC-STG4-HP-01: Parent Portal — Multi-Child Selection & Academic Report Card Inspection
- **Objective:** Verify that a parent with multiple enrolled children can log in, view each child via the switcher, inspect finalized semester report cards, and open the print layout.
- **Preconditions:** Backend and frontend running; seed data loaded; active operational period is Semester 1 (2026/2027).
- **Test Data:** User `parent.hendra` / `Parent123!`.
- **Steps:**
  1. Open browser and navigate to `http://localhost:5173/login`.
  2. Enter Username `parent.hendra` and Password `Parent123!`. Click **Masuk** (Login).
  3. Observe post-login redirection.
  4. Verify the top header displays "ELSASA SIS", parent name "Hendra Dahlan", role badge "Orang Tua / Wali", and a Logout button.
  5. Locate the **Pilih Anak** (Child Switcher) tab bar at the top of the portal. Verify two children are listed: **Ahmad Dahlan** (`X-TKJ-A`) and **Siti Nurhaliza** (`X-TKJ-B`).
  6. Click on the tab for **Ahmad Dahlan**.
  7. In the sub-tabs below, ensure **Rapor Akademik** (Academic Report Cards) is selected.
  8. Verify the list displays the finalized report card for Semester 1 (2026/2027).
  9. Click on the report card row to view subject grades, knowledge/skill scores, homeroom teacher notes (*Catatan Wali Kelas*), and attendance summary (Sakit, Izin, Alpa).
  10. Click the **Cetak Rapor** (Print Report Card) button.
  11. Cancel or close the browser print preview dialog.
  12. Click on the **Siti Nurhaliza** tab in the child switcher.
- **Expected Result:**
  - Login immediately redirects to `/portal/parent` under the streamlined `PortalLayout.vue` (no administrative left sidebar).
  - Child switcher clearly displays both Ahmad and Siti with their respective classes.
  - Ahmad's finalized report card is visible with complete academic marks, predicates, and remarks.
  - Clicking "Cetak Rapor" triggers browser print preview formatted for A4 official report card layout.
  - Switching to Siti Nurhaliza updates the view to Siti's academic records seamlessly without page reload errors.

---

### TC-STG4-HP-02: Parent Portal — Monthly Tuition Statement & Official Payment Receipts
- **Objective:** Verify that a parent can inspect their child's monthly fee obligations, discounts applied, deposit balances, and download official payment receipts.
- **Preconditions:** Logged in as `parent.hendra` on `/portal/parent`.
- **Test Data:** Student `Ahmad Dahlan` (Child 1).
- **Steps:**
  1. Ensure **Ahmad Dahlan** is selected in the child switcher.
  2. Click the **Keuangan & SPP** (Fees & Billing) sub-tab.
  3. Observe the summary metric cards:
     - Total Tagihan (Total Billed)
     - Total Terbayar (Total Paid)
     - Sisa Tagihan (Outstanding Balance)
     - Saldo Deposit (Advance Deposit Balance)
  4. Review the monthly charges table (e.g. SPP Bulan Juli 2026, SPP Bulan Agustus 2026).
  5. Check status badges: verify that fully paid months show green `Lunas` (Paid), partially paid show `Sebagian`, and unpaid show `Belum Bayar`.
  6. Scroll down to the **Riwayat Pembayaran & Kuitansi** (Payment History & Receipts) section.
  7. Verify historical receipts issued by the school cashier (e.g., Receipt Number `RCP-...`, Date, Payment Method `TUNAI` / `TRANSFER`, Total Amount).
  8. Click the **Cetak Kuitansi** (Print Receipt) button on a receipt row.
  9. Verify the receipt preview modal or print window opens.
- **Expected Result:**
  - The financial summary clearly displays accurate billing totals and deposit balances.
  - Monthly fee breakdown displays fee name, base amount, discounts applied, amount paid, and remaining balance.
  - Receipt details show itemized allocations matching the cashier transaction.
  - Browser print layout for receipt opens cleanly with official school header, receipt number, date, cashier name, and breakdown.

---

### TC-STG4-HP-03: Parent Portal — School Calendar & Bulletins
- **Objective:** Verify that a parent can view published school bulletins and the academic operational calendar.
- **Preconditions:** Logged in as `parent.hendra` on `/portal/parent`.
- **Test Data:** None.
- **Steps:**
  1. Click the **Info Sekolah** (School Info) sub-tab on the Parent Portal.
  2. Inspect the **Pengumuman Sekolah** (School Bulletins) section.
  3. Verify that announcements targeted to `PARENTS` or `ALL` are visible (e.g., "Jadwal Pertemuan Orang Tua Murid", "Informasi Libur Semester").
  4. Inspect the **Kalender Akademik** (Academic Calendar) section.
  5. Verify that operational dates, exams, and holidays for the current semester are listed with their event type tags (e.g. `HOLIDAY`, `EXAM`, `ACTIVITY`).
- **Expected Result:**
  - Announcements list shows title, publication date, author/office, and announcement body.
  - No announcements targeted exclusively to `STUDENTS` or internal staff are displayed.
  - Calendar displays chronological upcoming events with clear color-coded badges.

---

### TC-STG4-HP-04: Student Portal — Today's Agenda & Weekly Timetable (SMK Block Cycles)
- **Objective:** Verify that an enrolled student can log in, view their class identity, inspect today's class agenda, and review their weekly schedule reflecting SMK vocational block rotation cycles.
- **Preconditions:** User logged out from any previous session.
- **Test Data:** User `student.ahmad` / `Student123!`.
- **Steps:**
  1. Navigate to `http://localhost:5173/login`.
  2. Enter Username `student.ahmad` and Password `Student123!`. Click **Masuk**.
  3. Observe redirection to `/portal/student` under `PortalLayout.vue`.
  4. Inspect the top learner banner: verify Student Full Name "Ahmad Dahlan", NIS "202607001", Study Group "X-TKJ-A", and Major "Teknik Komputer dan Jaringan".
  5. Locate the **Siklus Blok SMK** badge. Verify it displays either **Siklus Kejuruan (Vocational Cycle)** or **Siklus Umum (General Cycle)** according to the active calendar date.
  6. Look at the **Agenda Hari Ini** (Today's Agenda) timeline card:
     - Verify class entries for the current day of the week, showing Subject Name, Teacher Name, Class Time (Start - End), and Assigned Room (e.g. `Lab Jaringan 1`).
     - If today is Sunday or a non-school day, verify an empty state message "Tidak ada jadwal kegiatan belajar mengajar hari ini" is displayed.
  7. Toggle or scroll to the **Jadwal Mingguan** (Weekly Timetable) view.
  8. Inspect the Monday–Friday schedule grid for Class `X-TKJ-A`.
- **Expected Result:**
  - Login redirects directly to `/portal/student` without access to staff navigation.
  - Header accurately displays Ahmad's student master data and active SMK cycle.
  - Today's classes list start/end periods, subject names, room assignments, and instructors.
  - Weekly schedule displays all scheduled teaching blocks without overlap or missing data.

---

### TC-STG4-HP-05: Student Portal — Bulletins & Academic Calendar
- **Objective:** Verify that a student can read student-targeted bulletins and check school calendar dates.
- **Preconditions:** Logged in as `student.ahmad` on `/portal/student`.
- **Test Data:** User `student.ahmad`.
- **Steps:**
  1. On the Student Portal view, scroll to the **Pengumuman & Informasi** (Bulletins) section.
  2. Verify bulletins targeted to `STUDENTS` or `ALL` are visible (e.g., "Tata Tertib Praktikum Bengkel TKJ", "Jadwal Penilaian Akhir Semester").
  3. Scroll to the **Kalender Akademik** section.
  4. Verify school dates, holidays, and examination periods are displayed in chronological order.
- **Expected Result:**
  - Student sees relevant school announcements.
  - Announcements targeted exclusively to `PARENTS` (such as parent-teacher conferences or tuition notices) are absent.
  - Calendar shows accurate school dates and events.

---

### TC-STG4-HP-06: Executive Academic Progress Monitoring Dashboard
- **Objective:** Verify that school leaders and administrators can monitor school-wide attendance taking, grading progress, and report card readiness across all study groups.
- **Preconditions:** Logged out from student portal.
- **Test Data:** User `admin` / `Admin123!` (or `principal` / `Principal123!`).
- **Steps:**
  1. Navigate to `http://localhost:5173/login`.
  2. Enter Username `admin` and Password `Admin123!`. Click **Masuk**.
  3. Verify redirection to `/operations/calendar` under `AppLayout.vue` with full left navigation sidebar.
  4. In the left sidebar, expand **Akademik & Kurikulum** (Academic & Curriculum) and click **Monitoring Progres Akademik** (Academic Monitoring).
  5. URL navigates to `/academic/monitoring`.
  6. Inspect the top KPI summary tiles:
     - **Kehadiran Hari Ini** (Today's Attendance Rate %): e.g. percentage of study groups where attendance has been submitted today.
     - **Kelengkapan Nilai** (Grading Completeness %): percentage of completed assessment scores across subjects.
     - **Kesiapan Rapor** (Report Card Finalization %): percentage of semester report cards finalized vs drafted.
  7. Inspect the **Peringatan Pengecualian Operasional** (Operational Exception Alerts) banner section:
     - Check if study groups with missing attendance or overdue grading are flagged with warning chips.
  8. In the **Progres per Rombel** (Study Group Breakdown) table:
     - Verify list of study groups (e.g. `X-TKJ-A`, `X-TKJ-B`, `XI-TKJ-A`).
     - Check status columns: Wali Kelas, Kehadiran %, Status Nilai, Status Rapor.
  9. Click on a study group row to expand or drill down into subject-level grading details.
- **Expected Result:**
  - Dashboard loads real-time aggregated metrics across all active study groups.
  - KPI percentage cards are calculated accurately from underlying attendance, scorebook, and report card records.
  - Exception banners highlight bottlenecks (e.g., homeroom remarks pending or unsubmitted attendance).
  - Progressive drill-down reveals individual subject scorebook progress without page crash.

---

### TC-STG4-HP-07: Financial Receivables, Arrears Aging & Collection Monitoring
- **Objective:** Verify that executive staff can monitor school-wide fee receivables, study group arrears, and historical collection cash flows without requiring cashier counter write privileges.
- **Preconditions:** Logged in as `principal` / `Principal123!` or `finance.officer` / `Finance123!`.
- **Test Data:** None.
- **Steps:**
  1. From the left navigation menu, expand **Keuangan** (Finance) and click **Piutang & Rekapitulasi** (Receivables & Arrears Aging) at `/finance/receivables`.
  2. Inspect the top KPI metric tiles:
     - **Total Piutang Berjalan** (Total School Receivables)
     - **Tunggakan Jatuh Tempo** (Overdue Tuition Arrears)
     - **Penerimaan Periode Ini** (Total Collections in Current Period)
     - **Liabilitas Saldo Deposit** (Total Advance Deposit Liability)
  3. Check the **Tunggakan per Rombongan Belajar** (Arrears by Study Group) summary table:
     - Verify columns: Nama Rombel, Tingkat, Jumlah Siswa Menunggak, Total Tunggakan (IDR).
  4. Click the **Buku Kas Penerimaan** (Collection Ledger) tab:
     - Verify chronological transaction entries listing Receipt #, Date, Student Name, Payment Method (`TUNAI` / `TRANSFER BANK`), Amount, and Cashier Name.
  5. Confirm that this screen is purely for governance: there are NO buttons to add manual payments, edit ledger entries, or void receipts (counter operations remain on `/finance/payments`).
- **Expected Result:**
  - Governance dashboard displays comprehensive school financial posture.
  - Monetary figures are formatted in Indonesian Rupiah (`Rp X.XXX.XXX`).
  - Arrears by study group pinpoint cohorts requiring administrative attention.
  - Collection ledger reflects actual cashier transactions without providing write/edit actions.

---

### TC-STG4-HP-08: Financial Receivables — Student Billing Statement Inspection Drawer
- **Objective:** Verify that an executive or finance officer can inspect an individual student's itemized statement from the arrears monitoring view.
- **Preconditions:** On `/finance/receivables` logged in as `finance.officer` or `principal`.
- **Test Data:** Student `Ahmad Dahlan` or any student with active billing records.
- **Steps:**
  1. In the Arrears / Student List table on `/finance/receivables`, locate student `Ahmad Dahlan`.
  2. Click the **Lihat Rekening Tagihan** (View Statement) action button on the row.
  3. Observe a side drawer or dialog opening.
  4. Inspect the itemized monthly ledger for Ahmad Dahlan:
     - Fee items (SPP, DSP, etc.), billing period, charged amount, discounts, payment allocations, remaining balance.
     - Advance deposit account balance and history.
  5. Click the close button to dismiss the drawer.
- **Expected Result:**
  - Drawer smoothly opens with the student's complete ledger history.
  - Shows clear distinction between paid and unpaid obligations.
  - Closes cleanly without losing the parent table's scroll position or active filters.

---

## Group 2: Business Rules & Data Isolation Scenarios

### TC-STG4-BR-01: Parent Portal — Strict Child Ownership Isolation
- **Objective:** Verify that a parent cannot view information for students who are not legally linked to their account in `auth.user_students`.
- **Preconditions:** Logged in as `parent.hendra`. Hendra is linked only to Ahmad Dahlan (`01A1F11A-7010-7010-8000-000000000010`) and Siti Nurhaliza (`01A1F11A-7011-7011-8000-000000000011`).
- **Test Data:** Third student in seed data (e.g. Student C: Budi Utomo or Rizky Pratama in X-TKJ-A).
- **Steps:**
  1. Open the Child Switcher tab on `/portal/parent`.
  2. Verify that ONLY Ahmad Dahlan and Siti Nurhaliza appear as selectable tabs.
  3. Open browser developer console (F12) $\rightarrow$ Network Tab.
  4. Refresh the page and examine the response of `GET /api/std/students/my-children`.
  5. Inspect the JSON array: verify exactly 2 student objects are returned.
- **Expected Result:**
  - Only Ahmad and Siti are exposed to Hendra.
  - No other students in the school appear in the UI or in the API response.

---

### TC-STG4-BR-02: Parent Portal — Draft Academic Report Cards Strictly Hidden
- **Objective:** Verify that draft or in-progress report cards are hidden from parents until officially finalized by the homeroom teacher / principal.
- **Preconditions:** Ensure at least one report card in the database for the active period is in `DRAFT` status, or only finalized cards exist.
- **Test Data:** User `parent.hendra`.
- **Steps:**
  1. Log in as `parent.hendra` and select **Ahmad Dahlan**.
  2. Navigate to the **Rapor Akademik** tab.
  3. Inspect all visible report card cards/rows.
  4. Note the status badge of each report card.
- **Expected Result:**
  - Every visible report card has status `FINALIZED` (or `PUBLISHED`).
  - No report card with status `DRAFT`, `IN_REVIEW`, or `CANCELLED` is visible in the UI.
  - The API endpoint `GET /api/acd/report-cards/student/{studentId}` returns only finalized records for parent users.

---

### TC-STG4-BR-03: Student Portal — Automatic SMK Vocational Block Cycle Resolution
- **Objective:** Verify that the student portal correctly calculates and displays the active SMK cycle (`GENERAL` vs `VOCATIONAL`) based on the operational period schedule.
- **Preconditions:** Logged in as `student.ahmad` (X-TKJ-A) and subsequently `student.siti` (X-TKJ-B).
- **Test Data:** Active operational schedule cycle definitions.
- **Steps:**
  1. Log in as `student.ahmad` at `http://localhost:5173/portal/student`.
  2. Inspect the **Siklus SMK** badge at the top of the schedule.
  3. Note whether the badge displays `Siklus Kejuruan` or `Siklus Umum`.
  4. Verify that the displayed timetable classes match that cycle (e.g., if Vocational Cycle, vocational subjects like *Administrasi Server*, *Praktikum Komputer* are scheduled; if General Cycle, subjects like *Bahasa Indonesia*, *Matematika* are scheduled).
  5. Log out and log in as `student.siti` (X-TKJ-B).
  6. Verify Siti's schedule reflects the corresponding cycle for Class X-TKJ-B (which may alternate if parallel classes rotate cycles).
- **Expected Result:**
  - The SMK cycle badge is clearly highlighted.
  - Timetable schedule accurately reflects the subjects assigned to the current block cycle without displaying inactive cycle periods.

---

### TC-STG4-BR-04: Executive Receivables Governance — Pure Read-Only Discipline
- **Objective:** Verify that the executive receivables screen (`SCR-FIN-RECEIVABLES`) strictly serves governance and audit purposes without exposing financial write operations.
- **Preconditions:** Logged in as `principal` at `/finance/receivables`.
- **Test Data:** None.
- **Steps:**
  1. Inspect the `/finance/receivables` dashboard interface.
  2. Search for any buttons labeled "Catat Pembayaran" (Record Payment), "Batalkan Tagihan" (Cancel Charge), "Hapus Transaksi" (Delete Transaction), or "Koreksi Saldo" (Adjust Balance).
  3. Open the student billing statement drawer for any student.
  4. Inspect all drawer actions.
- **Expected Result:**
  - No payment entry forms or mutation buttons exist on this dashboard.
  - The drawer only provides "Tutup" (Close) or "Cetak Rekening" (Print Statement).
  - Frontline cashier collection remains strictly confined to `/finance/payments` with role enforcement.

---

## Group 3: Validation Rules & Boundary Scenarios

### TC-STG4-VR-01: Post-Login Role-Based Redirection
- **Objective:** Verify that every user persona is redirected to their appropriate workspace immediately upon authenticating at `/login`.
- **Preconditions:** Clean browser session (no existing auth tokens).
- **Test Data:** All 4 role types (`PARENT`, `STUDENT`, `PRINCIPAL`, `SCHOOL_ADMIN`).
- **Steps:**
  1. Log in as `parent.hendra` $\rightarrow$ verify destination URL.
  2. Log out.
  3. Log in as `student.ahmad` $\rightarrow$ verify destination URL.
  4. Log out.
  5. Log in as `principal` $\rightarrow$ verify destination URL.
  6. Log out.
  7. Log in as `admin` $\rightarrow$ verify destination URL.
- **Expected Result:**
  - `parent.hendra` redirects directly to `/portal/parent` under `PortalLayout.vue`.
  - `student.ahmad` redirects directly to `/portal/student` under `PortalLayout.vue`.
  - `principal` redirects to `/operations/calendar` under `AppLayout.vue`.
  - `admin` redirects to `/operations/calendar` under `AppLayout.vue`.

---

### TC-STG4-VR-02: School Bulletin Audience Scoping
- **Objective:** Verify that announcements published for specific audiences are strictly filtered by audience tag (`ALL`, `PARENTS`, `STUDENTS`).
- **Preconditions:** Database contains announcements with varying target audiences:
  - Announcement A: Audience = `ALL`
  - Announcement B: Audience = `PARENTS`
  - Announcement C: Audience = `STUDENTS`
- **Test Data:** `parent.hendra` and `student.ahmad`.
- **Steps:**
  1. Log in as `parent.hendra` and navigate to **Info Sekolah** $\rightarrow$ **Pengumuman**.
  2. Check visibility of Announcements A, B, and C.
  3. Log out.
  4. Log in as `student.ahmad` and scroll to **Pengumuman & Informasi**.
  5. Check visibility of Announcements A, B, and C.
- **Expected Result:**
  - `parent.hendra` sees Announcement A (`ALL`) and Announcement B (`PARENTS`). Announcement C (`STUDENTS`) is NOT visible.
  - `student.ahmad` sees Announcement A (`ALL`) and Announcement C (`STUDENTS`). Announcement B (`PARENTS`) is NOT visible.

---

## Group 4: Error Handling & Security Scenarios

### TC-STG4-EH-01: Cross-Portal Route Infiltration Prevention
- **Objective:** Verify that consumer accounts (parents and students) cannot access internal administrative and academic screens by typing URLs directly.
- **Preconditions:** Logged in as `parent.hendra` or `student.ahmad`.
- **Test Data:** Protected staff routes:
  - `/academic/monitoring`
  - `/finance/receivables`
  - `/finance/payments`
  - `/operations/calendar`
  - `/students/records`
- **Steps:**
  1. Log in as `parent.hendra`.
  2. Manually enter `http://localhost:5173/academic/monitoring` into the browser address bar and press Enter.
  3. Observe application behavior.
  4. Manually enter `http://localhost:5173/finance/receivables` into the address bar and press Enter.
  5. Observe application behavior.
  6. Log out and repeat steps 1–5 as `student.ahmad`.
- **Expected Result:**
  - Direct navigation is blocked by the frontend route guard.
  - The application displays an access denied page / `403 Forbidden` notification, or immediately redirects back to `/portal/parent` or `/portal/student`.
  - The administrative sidebar is never rendered for consumer accounts.

---

### TC-STG4-EH-02: Backend API Direct Infiltration & Child Ownership Guard
- **Objective:** Verify that the backend API blocks a parent attempting to access another student's report card or statement via direct HTTP request.
- **Preconditions:** Authenticated session as `parent.hendra` (retrieve JWT bearer token from browser LocalStorage / Network tab).
- **Test Data:** An unlinked student ID (e.g. `01A1F11A-7012-7012-8000-000000000012`).
- **Steps:**
  1. Open browser developer console or API testing tool (Postman / curl / Fetch).
  2. Send a `GET` request to:
     `http://localhost:5173/api/fin/students/01A1F11A-7012-7012-8000-000000000012/statement`
     with `Authorization: Bearer <Parent_Token>`.
  3. Observe HTTP status code and response body.
  4. Send a `GET` request to:
     `http://localhost:5173/api/acd/report-cards/student/01A1F11A-7012-7012-8000-000000000012`
     with `Authorization: Bearer <Parent_Token>`.
  5. Observe HTTP status code and response body.
- **Expected Result:**
  - Both requests return HTTP `403 Forbidden` (or empty array / RFC 7807 problem details: "You do not have permission to view data for this student").
  - Unauthorized financial or academic student data is never leaked across parent accounts.

---

### TC-STG4-EH-03: Authentication Failures & Invalid Credentials
- **Objective:** Verify that invalid credentials display user-friendly error messages without crashing or exposing stack traces.
- **Preconditions:** Clean `/login` page.
- **Test Data:** Username `parent.hendra` with password `WrongPassword!`.
- **Steps:**
  1. Navigate to `http://localhost:5173/login`.
  2. Enter Username `parent.hendra` and Password `WrongPassword!`. Click **Masuk**.
  3. Observe UI feedback.
  4. Attempt login with non-existent user `unknown.user` / `Test1234!`.
- **Expected Result:**
  - An inline error alert or toast appears: "Username atau password salah" (Invalid username or password).
  - Form remains intact allowing correction.
  - No server stack traces or unhandled promise exceptions appear in the console.

---

## Group 5: Regression & Cross-Device Ergonomics Scenarios

### TC-STG4-RG-01: Parent Portal — Mobile Viewport Audit ($390\text{px} \times 844\text{px}$)
- **Objective:** Verify that the Parent Portal is fully usable on modern smartphones without horizontal scrollbars or cramped elements.
- **Preconditions:** Chrome DevTools open in Device Emulation mode (Device: **iPhone 14 Pro** / $390\text{px} \times 844\text{px}$, 100% zoom).
- **Test Data:** User `parent.hendra`.
- **Steps:**
  1. Log in as `parent.hendra` on the emulated mobile viewport.
  2. Inspect the top header: branding, user name, and logout button should fit without wrapping awkwardly.
  3. Inspect the **Pilih Anak** child switcher:
     - Verify tabs can be scrolled horizontally with swipe/drag.
     - Verify touch targets for both child tabs have height $\ge 44\text{px}$.
  4. Switch between the 3 sub-tabs: **Rapor Akademik**, **Keuangan & SPP**, and **Info Sekolah**.
  5. Inspect the monthly fees card: verify amount figures, status badges, and receipt buttons wrap neatly without horizontal page overflow.
  6. Click "Cetak Kuitansi" or "Cetak Rapor" to ensure dialogs are responsive.
- **Expected Result:**
  - Zero unwanted horizontal body scroll (`overflow-x` is clean).
  - All interactive buttons and tabs have $\ge 44\text{px}$ touch targets.
  - Font sizes are easily readable on mobile displays.

---

### TC-STG4-RG-02: Student Portal — Small Mobile Viewport Audit ($360\text{px} \times 800\text{px}$)
- **Objective:** Verify that the Student Portal renders properly on compact entry-level Android devices.
- **Preconditions:** Chrome DevTools open in Device Emulation mode (Device: **Samsung Galaxy S20** / $360\text{px} \times 800\text{px}$).
- **Test Data:** User `student.ahmad`.
- **Steps:**
  1. Log in as `student.ahmad` on the emulated $360\text{px}$ viewport.
  2. Inspect the student identity header: verify Name, NIS, Class, and SMK Cycle badge do not clip or overlap.
  3. Review the **Agenda Hari Ini** timeline card:
     - Verify time indicators, subject names, and room tags are clearly legible.
  4. Switch to the **Jadwal Mingguan** (Weekly Timetable) view:
     - Verify schedule days can be viewed or toggled cleanly on mobile.
  5. Check Announcements and Calendar feeds.
- **Expected Result:**
  - Clean vertical stacking with no clipping or broken cards.
  - Today's agenda provides immediate, distraction-free clarity for a student on the move.
  - Page scrolls smoothly.

---

### TC-STG4-RG-03: Desktop Staff Navigation Integrity (Regression Verification)
- **Objective:** Verify that the introduction of Stage 4 consumer portals and layouts has not regressed existing operational screens in `AppLayout.vue`.
- **Preconditions:** Logged in as `admin` on a desktop viewport ($1280 \times 800$ or higher).
- **Test Data:** User `admin` / `Admin123!`.
- **Steps:**
  1. Log in as `admin`.
  2. Verify the left navigation sidebar contains all expected modules:
     - Dashboard / Kalender Operasional (`/operations/calendar`)
     - Data Induk Siswa (`/students/records`)
     - Pembelajaran & Penilaian (`/academic/scorebooks`, `/academic/report-cards`)
     - Monitoring Progres Akademik (`/academic/monitoring`)
     - Keuangan (`/finance/fees`, `/finance/payments`, `/finance/receivables`)
     - Perkembangan Siswa (`/development/counseling`, `/development/extracurricular`)
  3. Click through each major section to confirm views load without white-screen or script errors.
- **Expected Result:**
  - Full administrative menu tree is functional.
  - Seamless navigation across operational workbenches without regression.

---

# 6. Defect Logging & Failure Recording Guidelines

When performing manual testing, record the outcome of each test case. If any step fails or behaves unexpectedly:

1. **Do NOT modify code or attempt fixes.** (Testing records findings; fixing is handled in the SDLC remediation loop).
2. **Assign Status:** Mark the test case as **`FAIL`**.
3. **Capture Verbatim Evidence:**
   - Exact URL where failure occurred.
   - User account and persona used.
   - Steps taken immediately prior to the failure.
   - Observed behavior vs Expected result.
   - Browser console logs (errors/warnings) and Network tab details (HTTP status, request URL, response payload).
   - Screenshot or screen recording if visual/layout issue.
4. **Handoff:** The FAIL record serves as formal defect intake for the Issuer (`ica-issuer`) to generate a `BUG` ISSUE artifact.

---

# 7. Test Gate & Sign-off Criteria

To grant **TEST PASSED** for **CR-STAGE-4**:

- [ ] Every Happy Path test case (**TC-STG4-HP-01** through **TC-STG4-HP-08**) is marked **`PASS`**.
- [ ] Every Business Rules & Isolation test case (**TC-STG4-BR-01** through **TC-STG4-BR-04**) is marked **`PASS`**.
- [ ] Every Validation Rules test case (**TC-STG4-VR-01** through **TC-STG4-VR-02**) is marked **`PASS`**.
- [ ] Every Error Handling & Security test case (**TC-STG4-EH-01** through **TC-STG4-EH-03**) is marked **`PASS`**.
- [ ] Every Regression & Mobile Ergonomics test case (**TC-STG4-RG-01** through **TC-STG4-RG-03**) is marked **`PASS`**.
- [ ] Any identified defects (`FAIL`) have been formally tracked, resolved via approved BUG plans, and retested as **`PASS`**.

Once all test cases are executed and verified, record the results in `docs/issues/IS005- STAGE-4-DEV-TEST-EXECUTION.md` and declare the **TEST PASSED** gate.
