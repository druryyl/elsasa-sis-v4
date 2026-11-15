# Teaching Domain — Basic

## 1. Business Overview

The Teaching domain (Basic) is the authoritative source for who the school's teachers are and what they teach. It manages three core responsibilities:

1. **Teacher Profile** — the identity, personal details, and contact record of each teacher at the school.
2. **Teacher Assignment** — linking a teacher to a specific subject and study group for an academic period.
3. **Teaching Capability** — recording which subjects each teacher is qualified or able to teach.

Every other domain that references teachers depends on Teaching for teacher identity and assignment information. The domain begins when a teacher profile is created and continues through assignment to subjects and study groups each academic period.

**Out of scope:** student data, academic results, financial obligations, operational scheduling, student development, timetabling, attendance, payroll.

---

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Teacher | A person employed or engaged by the school to deliver instruction. |
| Teacher Profile | The school's official record of a teacher's identity, personal details, and contact information. |
| NIP | Nomor Induk Pegawai — a government-issued civil servant identification number, applicable only to PNS teachers. |
| NIK | Nomor Induk Kependudukan — a national citizen identification number. |
| Teacher Status | Whether a teacher is currently active or inactive at the school. |
| Subject | A distinct area of study offered by the school (e.g., Matematika, Bahasa Inggris). |
| Study Group | An academic-year-specific group of students within a grade (e.g., 7A). |
| Academic Year | A full annual school cycle, typically July to June (e.g., 2024/2025). |
| Semester | One half of an academic year. Indonesian schools use Semester 1 (odd) and Semester 2 (even). |
| Academic Period | A specific semester within a specific academic year (e.g., Semester 1 2024/2025). |
| Teacher Assignment | A record that a specific teacher teaches a specific subject in a specific study group for a specific academic period. |
| Teaching Capability | A record that a teacher is able to teach a particular subject. |

---

## 3. Business Capabilities

| Capability | Description |
|---|---|
| Maintain Teacher Profiles | Register, update, and deactivate teacher identity, personal details, and contact records. |
| Manage Teacher Assignments | Assign teachers to subjects and study groups for each academic period, and remove or replace those assignments. |
| Record Teaching Capabilities | Record and maintain the list of subjects each teacher is capable of teaching. |

---

## 4. Actors & Roles

| Actor | Responsibilities |
|---|---|
| School Administrator | Creates and maintains teacher profiles (identity, personal details, and contact information). Assigns teachers to subjects and study groups each academic period. Records and updates teaching capabilities. The primary operator of the Teaching domain. |
| Principal (Kepala Sekolah) | Has oversight of teacher assignments and may direct the School Administrator to create, change, or remove assignments. |

---

## 5. Domain Objects

### Teacher Profile

- **Purpose:** Represents the school's official record of a teacher.
- **Responsibility:** Holds the teacher's identity (full name, NIP/NIK), personal details (nickname, date of birth, gender, address), contact information (phone, email), and current status (active/inactive).
- **Relationships:** Referenced by Teacher Assignment and Teaching Capability.

### Teacher Assignment

- **Purpose:** Records that a teacher teaches a particular subject in a particular study group during a particular academic period.
- **Responsibility:** Links one teacher to one subject in one study group for one academic period. Serves as the authoritative record of who teaches what and where.
- **Relationships:** References a Teacher Profile. References a Subject and a Study Group (both defined outside this domain or accepted as reference data). Scoped to an Academic Period.

### Teaching Capability

- **Purpose:** Records that a teacher is able to teach a particular subject.
- **Responsibility:** Maintains a simple association between a teacher and a subject, independent of any specific study group or academic period.
- **Relationships:** References a Teacher Profile. References a Subject.

### Subject *(Reference Data)*

- **Purpose:** Identifies a distinct area of study.
- **Responsibility:** Provides the subject name used by assignments and capabilities. Managed outside the Teaching domain or treated as shared reference data.

### Study Group *(Reference Data)*

- **Purpose:** Identifies an academic-year-specific student group within a grade (e.g., 7A).
- **Responsibility:** Provides the study group identifier used by assignments. Managed in the Student domain (`std.study_groups`).

---

## 6. Aggregates

### Teacher (Aggregate Root)

- **Consistency Boundary:** A single teacher and all data directly belonging to that teacher.
- **Owned Entities:** Teacher Profile, Teaching Capabilities (the set of subjects this teacher can teach).
- **Business Responsibility:** Ensures the teacher's identity, personal details, contact information, status, and capability list are consistent. A teacher's capabilities are always managed in the context of that teacher.

### Teacher Assignment (Aggregate Root)

- **Consistency Boundary:** A single assignment linking one teacher to one subject in one study group for one academic period.
- **Owned Entities:** The assignment record itself.
- **Business Responsibility:** Ensures each assignment is a valid, non-contradictory record of a teacher–subject–study-group–period combination. Each assignment is independently created and removed.

---

## 7. Business Rules

| ID | Rule |
|---|---|
| BR-TCH-001 | A Teacher Profile must have a full name and a date of birth. |
| BR-TCH-002 | A Teacher Profile must have at least one contact method: phone or email. |
| BR-TCH-003 | Only an active teacher may be assigned to a subject and study group. |
| BR-TCH-004 | A teacher may not be assigned to the same subject in the same study group for the same academic period more than once. |
| BR-TCH-005 | A teacher may only be assigned to subjects for which the teacher has a recorded Teaching Capability. |
| BR-TCH-006 | A Teaching Capability entry must not be duplicated — a teacher may not have the same subject listed more than once. |
| BR-TCH-007 | Deactivating a teacher (setting status to inactive) does not automatically remove existing assignments; assignments for the current or future academic periods must be explicitly reassigned or removed. |
| BR-TCH-008 | A Teacher Profile must have a gender (male or female). |

---

## 8. State Machines & Lifecycles

### Teacher Profile Lifecycle

```
[Active] ←→ [Inactive]
```

| State | Meaning |
|---|---|
| Active | The teacher is currently part of the school's teaching staff and eligible for assignment. |
| Inactive | The teacher is no longer actively teaching at the school. Retained for historical reference. |

**Transitions:**

| From | To | Trigger |
|---|---|---|
| *(new)* | Active | A new teacher profile is created. |
| Active | Inactive | The teacher leaves the school or is deactivated by the School Administrator. |
| Inactive | Active | A previously inactive teacher returns and is reactivated. |

### Teacher Assignment Lifecycle

```
[Assigned] → [Removed]
```

| State | Meaning |
|---|---|
| Assigned | The teacher is actively linked to a subject and study group for an academic period. |
| Removed | The assignment has been ended or deleted. |

**Transitions:**

| From | To | Trigger |
|---|---|---|
| *(new)* | Assigned | A teacher is assigned to a subject and study group for an academic period. |
| Assigned | Removed | The assignment is removed by the School Administrator. |

---

## 9. Domain Events

| Event | Description | Significance |
|---|---|---|
| Teacher Registered | A new teacher profile has been created. | Other domains may now reference this teacher. |
| Teacher Deactivated | A teacher's status has changed to inactive. | Dependent domains are notified that this teacher is no longer active; existing assignments should be reviewed. |
| Teacher Reactivated | A previously inactive teacher's status has changed back to active. | The teacher is again eligible for assignment. |
| Teacher Assigned | A teacher has been assigned to a subject in a study group for an academic period. | Scheduling, academic, and other dependent domains may use this assignment. |
| Teacher Assignment Removed | A teacher's assignment to a subject/study group/period has been removed. | Dependent domains are notified that this assignment no longer exists. |
| Teaching Capability Added | A subject has been added to a teacher's list of teaching capabilities. | The teacher is now eligible for assignment to this subject. |
| Teaching Capability Removed | A subject has been removed from a teacher's list of teaching capabilities. | The teacher is no longer considered capable of teaching this subject for future assignments. |

---

## 10. Business Workflows

### Register a New Teacher

1. School Administrator provides the teacher's full name, optional NIP/NIK, optional nickname, date of birth, gender, optional address, and phone and/or email.
2. A new Teacher Profile is created with status **Active**.
3. Event: **Teacher Registered**.

### Record Teaching Capabilities

1. School Administrator selects a teacher.
2. School Administrator adds one or more subjects to the teacher's capability list.
3. Each subject is recorded as a Teaching Capability for that teacher.
4. Event: **Teaching Capability Added** (per subject).

### Assign a Teacher to a Subject and Study Group

1. School Administrator selects an active teacher, a subject, a study group, and an academic period.
2. The teacher must have a Teaching Capability for the selected subject (BR-TCH-005).
3. The combination of teacher–subject–study-group–period must not already exist (BR-TCH-004).
4. A Teacher Assignment is created with state **Assigned**.
5. Event: **Teacher Assigned**.

### Remove a Teacher Assignment

1. School Administrator selects an existing assignment.
2. The assignment is removed.
3. Event: **Teacher Assignment Removed**.

### Deactivate a Teacher

1. School Administrator changes a teacher's status from Active to Inactive.
2. Any current or future assignments for this teacher remain in place but must be reviewed and handled explicitly (BR-TCH-007).
3. Event: **Teacher Deactivated**.
