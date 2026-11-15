# Student Domain — Basic

## 1. Business Overview

The Student domain (Basic) is the authoritative source of truth about who the school's students are. It manages student identity, parent/guardian information, the admission of new student candidates, their enrollment as active students, and their administrative placement into school levels, grades, and study groups.

Every other domain depends on Student to know which students exist, who their guardians are, and where they are placed.

The domain begins when a candidate registers for admission and continues through acceptance, enrollment activation, and administrative placement. It does not manage academic results, financial obligations, teaching assignments, operational scheduling, or student development records.

**Scope — five responsibilities only:**

| # | Responsibility | Description |
|---|---|---|
| 1 | Student Identity | Official personal data of each enrolled student |
| 2 | Parent & Guardian Data | Information about people responsible for the student |
| 3 | Simple Admission | Registering new candidates and recording accept/reject decisions |
| 4 | Simple Enrollment | Activating an accepted candidate as an official student |
| 5 | Administrative Placement | Assigning a student to a school level, grade, and study group for an academic year |

---

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Student | A person officially enrolled in the school who holds an active student record. |
| Student Number (NIS) | A unique identifier assigned by the school to each enrolled student. |
| National Student Number (NISN) | A unique national identifier issued by the government for each student. |
| Candidate | A person who has registered for admission but has not yet been accepted or enrolled. |
| Parent | The biological father or mother of a student, recorded as a Guardian. |
| Guardian | A parent or non-parent guardian linked to a student or candidate who bears responsibility for them (relationship type: father, mother, or guardian). |
| Admission | The process of registering a candidate and deciding whether to accept or reject them. |
| Admission Year | The school year for which a candidate applies for admission. |
| Enrollment | The act of activating an accepted candidate as an official student of the school. |
| Enrollment Date | The date on which a candidate officially becomes an enrolled student. |
| Placement | The administrative assignment of a student to a specific academic year, school level, grade, and optionally a study group. |
| Academic Year | A defined yearly period during which the school operates (e.g., 2025/2026). |
| School Level | The tier of education offered by the school: SD (elementary), SMP (junior secondary), SMA (senior secondary), or SMK (vocational senior secondary). |
| Grade | A timeless year-level within a school level (e.g., Grade 1, Grade 7, Grade 10). |
| Study Group | An academic-year-specific subdivision within a grade to which students are assigned (e.g., 7A, 7B in 2026/2027). |

---

## 3. Business Capabilities

| Capability | Description |
|---|---|
| Maintain Student Identity | Record, update, and retrieve the official personal data of each student. |
| Maintain Parent & Guardian Data | Record and update information about each student's guardians. |
| Admit Candidates | Register new candidates, record admission decisions (accept or reject), and track admission status. |
| Enroll Students | Activate accepted candidates as official students by assigning a student number and enrollment date. |
| Place Students | Assign each student to an academic year, school level, grade, and study group. |

---

## 4. Actors & Roles

| Actor | Responsibilities |
|---|---|
| School Administrator | Registers candidates, records admission decisions, enrolls accepted candidates, maintains student identity and parent/guardian data, and assigns student placements. Has full authority over all Student domain operations. |
| School Principal | Makes or approves admission decisions (accept/reject). Has oversight of enrollment and placement data. |
| Parent / Guardian | Provides candidate and personal data during the admission process. Does not directly perform any domain operation. |

---

## 5. Domain Objects

### Student

The central entity of the domain. Represents a person officially enrolled in the school.

- **Purpose:** Serve as the single authoritative record of a student's identity.
- **Responsibility:** Hold the student's personal data, student number, and enrollment status.
- **Key data:** Full name, nickname, date of birth, gender, student number (NIS), national student number (NISN), contact information.
- **Relationships:** Linked to one or more Guardians. Subject of one Placement per academic year.

### Candidate

A person who has applied for admission but is not yet an enrolled student.

- **Purpose:** Represent a prospective student during the admission process.
- **Responsibility:** Hold candidate personal data and the admission decision.
- **Key data:** Full name, nickname, date of birth, gender, contact information, admission year, target school level, target grade, admission status.
- **Relationships:** Becomes a Student upon enrollment.

### Guardian

A parent or non-parent guardian linked to a student or candidate.

- **Purpose:** Record who bears parental or legal responsibility for a student or candidate.
- **Responsibility:** Hold identity and contact data of a guardian (father, mother, or guardian).
- **Key data:** Name, relationship type (father, mother, guardian), phone, email, address.
- **Relationships:** Linked to one or more Students or Candidates.

### Placement

The administrative assignment of a student to a position in the school structure for a given academic year.

- **Purpose:** Define where a student belongs in the school's organizational structure.
- **Responsibility:** Record the academic year, school level, grade, and study group for one student.
- **Key data:** Academic year, school level, grade, study group (optional).
- **Relationships:** Belongs to exactly one Student. Scoped to exactly one Academic Year. May reference one Study Group.

### Study Group

An academic-year-specific group of students within a grade (e.g., 7A, 7B in 2026/2027).

- **Purpose:** Serve as the school's authoritative annual student group identity (Rombongan Belajar) within an academic year, whose count and naming are defined per year rather than maintained as timeless master data.
- **Responsibility:** Hold the academic year, school level, grade, and study group name.
- **Key data:** Academic year, school level, grade, name.
- **Relationships:** Scoped to one Academic Year. Referenced by Placement records and by external domains (Operational scheduling, Academic, Teaching, Finance).

---

## 6. Aggregates

### Student Aggregate

- **Aggregate Root:** Student
- **Consistency Boundary:** A single student's identity, enrollment status, guardians, and placements.
- **Owned Entities:** Guardian associations, Placement records.
- **Business Responsibility:** Guarantee that a student's identity data is consistent, that the student number is unique, and that guardians and placements are always accessed and modified through the student.

### Candidate Aggregate

- **Aggregate Root:** Candidate
- **Consistency Boundary:** A single candidate's application data and admission status.
- **Owned Entities:** Guardian associations (pre-enrollment).
- **Business Responsibility:** Guarantee that a candidate's admission status transitions are valid and that candidate data is consistent throughout the admission process.

### Study Group Aggregate

- **Aggregate Root:** Study Group
- **Consistency Boundary:** A single study group's academic year, school level, grade, and name.
- **Owned Entities:** None.
- **Business Responsibility:** Guarantee that each study group belongs to a valid academic year, school level, and grade, and that study group names are unique within an academic year.

---

## 7. Business Rules

| ID | Rule |
|---|---|
| BR-STU-001 | Each student must have a unique school-assigned student number (NIS) within the school. |
| BR-STU-002 | Each student must have a full name and a date of birth. |
| BR-STU-003 | A student must have at least one guardian (parent or non-parent guardian) on record. |
| BR-STU-004 | A student may have at most one placement per academic year. |
| BR-STU-005 | A candidate may only be enrolled if their admission status is Accepted. |
| BR-STU-006 | A candidate's admission status may not change once the candidate has been enrolled. |
| BR-STU-007 | The relationship type of a guardian must be one of: father, mother, or guardian. |
| BR-STU-008 | A placement must reference a valid academic year, school level, and grade. The study group is optional. |
| BR-STU-009 | A candidate may only transition from Registered to Accepted or Rejected. No other status transitions are permitted. |
| BR-STU-010 | If a national student number (NISN) is provided, it must be unique across all students. |

---

## 8. State Machines & Lifecycles

### Candidate Admission Lifecycle

```
[Registered] ──── accept ────► [Accepted] ──── enroll ────► [Enrolled]
      │
      └──── reject ────► [Rejected]
```

| State | Meaning |
|---|---|
| Registered | The candidate has submitted an application and is awaiting a decision. |
| Accepted | The candidate has been approved for enrollment. |
| Rejected | The candidate has been denied admission. This is a terminal state. |
| Enrolled | The candidate has been activated as an official student. This is a terminal state within the admission lifecycle; the student record continues in the Student lifecycle. |

| Transition | From | To | Trigger |
|---|---|---|---|
| Accept | Registered | Accepted | School decides to admit the candidate. |
| Reject | Registered | Rejected | School decides to deny the candidate. |
| Enroll | Accepted | Enrolled | School activates the candidate as an official student. |

### Student Status Lifecycle

```
[Active]
```

At the Basic level, an enrolled student is always Active. There are no further status transitions (withdrawal, graduation, suspension, and transfer are not in scope).

---

## 9. Domain Events

| Event | Description | Triggered When |
|---|---|---|
| Candidate Registered | A new candidate has been recorded in the admission process. | A candidate application is created. |
| Candidate Accepted | A candidate has been approved for admission. | Admission decision is set to Accepted. |
| Candidate Rejected | A candidate has been denied admission. | Admission decision is set to Rejected. |
| Student Enrolled | An accepted candidate has been activated as an official student with a student number. | An accepted candidate is enrolled. |
| Student Placed | A student has been assigned to an academic year, school level, grade, and study group. | A placement is recorded for a student. |
| Student Identity Updated | A student's personal data has been modified. | Student identity data is changed. |
| Guardian Added | A parent or guardian has been linked to a student. | A new guardian record is associated with a student. |

---

## 10. Business Workflows

### Admit and Enroll a New Student

1. A candidate's data is recorded along with parent/guardian information, target school level, target grade, and admission year. The candidate is now **Registered**.
2. The school evaluates the candidate and records a decision: **Accepted** or **Rejected**.
3. If accepted, the school enrolls the candidate: a student number (NIS) is assigned, an enrollment date is recorded, and the candidate becomes an **Active Student**.
4. The school assigns a placement for the student — academic year, school level, grade, and optionally a study group.

### Update Student Identity

1. The school identifies the student whose data needs to change.
2. The student's personal data (name, contact, date of birth, etc.) is updated.
3. The updated record remains the authoritative source of truth for that student.

### Manage Guardians

1. The school identifies the student.
2. A guardian (father, mother, or non-parent guardian) is added, updated, or removed — subject to the constraint that at least one guardian must remain on record (BR-STU-003).

### Place a Student for an Academic Year

1. The school identifies the student and the target academic year.
2. The school assigns the student to a school level, grade, and optionally a study group for that academic year.
3. If the student already has a placement for that academic year, the existing placement is updated (BR-STU-004).
