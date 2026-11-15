# Development Domain — Basic

## 1. Business Overview

The Development domain (Basic) manages the foundational non-academic records of student life. It is the authoritative source for behaviour observations, disciplinary actions, extracurricular participation, and non-academic achievements.

The domain begins when a teacher or staff member records a behaviour note, registers a disciplinary action, manages extracurricular activities, or logs a student achievement. It supports homeroom teachers and school leadership in understanding each student beyond academics.

**In scope:**

- Recording positive and negative student behaviour observations
- Recording code-of-conduct violations and sanctions
- Managing extracurricular activity offerings and student participation
- Recording non-academic student achievements

**Out of scope:**

- Academic results, grades, assessments
- Student identity, enrolment, demographic data
- Financial obligations, payments, billing
- Operational scheduling, timetabling, teaching assignments
- Counselling case management, wellbeing support, safeguarding
- Formal approval workflows, appeals, multi-step pipelines

---

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Student | A pupil enrolled at the school, referenced from the Student domain. The Development domain does not own student identity. |
| Behaviour Note | A dated record of a single observed student behaviour, classified as positive or negative. |
| Behaviour Type | The classification of a behaviour note: **positive** or **negative**. |
| Discipline Record | A dated record of a code-of-conduct violation committed by a student and the sanction imposed. |
| Violation Type | A category describing the nature of a code-of-conduct breach (e.g., tardiness, misconduct, uniform violation). |
| Sanction | The action taken in response to a violation (e.g., verbal warning, written warning, parent meeting, suspension). |
| Extracurricular Activity | A school-recognised non-academic activity offered to students outside the core curriculum. |
| Activity Status | Whether an extracurricular activity is currently offered: **active** or **inactive**. |
| Supervisor | The teacher or staff member responsible for overseeing an extracurricular activity. |
| Participation | The relationship between a student and an extracurricular activity in a given academic year. |
| Participation Status | The state of a student's involvement in an extracurricular activity: **active**, **completed**, or **withdrawn**. |
| Academic Year | The school year during which a participation takes place, referenced from the Academic domain. The Development domain does not own this concept. |
| Achievement | A recorded non-academic accomplishment of a student. |
| Achievement Type | A category describing the nature of the accomplishment (e.g., sports, arts, science competition, community service). |
| Achievement Level | The scope at which the achievement was attained: **school**, **city**, **province**, or **national**. |
| Recorder | The teacher or staff member who creates a behaviour note, discipline record, or achievement entry. |

---

## 3. Business Capabilities

| Capability | Description |
|---|---|
| Record Student Behaviour | Capture positive and negative behaviour observations for individual students. |
| Record Disciplinary Action | Capture code-of-conduct violations and the sanctions imposed on individual students. |
| Manage Extracurricular Offerings | Establish and maintain the catalogue of extracurricular activities the school offers, including their supervisors and active/inactive status. |
| Manage Student Participation | Record which students participate in which extracurricular activities each academic year, and track whether participation is active, completed, or withdrawn. |
| Record Non-Academic Achievement | Capture student accomplishments outside the academic curriculum, including the type, event, level, and date. |
| View Student Development Profile | Provide a consolidated view of a student's behaviour notes, discipline records, participations, and achievements to support holistic understanding. |

---

## 4. Actors & Roles

| Actor | Responsibilities |
|---|---|
| **Teacher** | Records behaviour notes for students they observe. Records discipline records for violations they witness or handle. Records non-academic achievements. May serve as supervisor of an extracurricular activity. |
| **Homeroom Teacher** | Has primary responsibility for monitoring the overall development profile of students in their study group. Records behaviour notes, discipline records, and achievements for their students. |
| **School Administrator** | Manages the catalogue of extracurricular activities (creation, status changes, supervisor assignment). Manages student participation records. May record discipline records and achievements. |
| **School Principal** | Reviews student development profiles for leadership oversight. May record discipline records for serious violations. |

---

## 5. Domain Objects

### Behaviour Note

A single dated observation of student behaviour. Each note captures who the student is, when the behaviour occurred, whether it was positive or negative, a brief description, and who recorded it. A behaviour note is an immutable factual record — once created, it represents a historical observation.

**Relationships:** Belongs to one student. Created by one recorder.

### Discipline Record

A single dated record of a code-of-conduct violation and the resulting sanction. Each record captures the student, date, violation type, description of the incident, the sanction applied, and who recorded it.

**Relationships:** Belongs to one student. Created by one recorder.

### Extracurricular Activity

A school-recognised non-academic activity available for student participation. Each activity has a name, brief description, assigned supervisor, and an active/inactive status.

**Relationships:** Has one supervisor. Has zero or more participations.

### Participation

The enrolment of a student in an extracurricular activity for a specific academic year. Each participation tracks the student, the activity joined, the academic year, and the participation status.

**Relationships:** Belongs to one student. Belongs to one extracurricular activity. Scoped to one academic year.

### Achievement

A recorded non-academic accomplishment of a student. Each achievement captures the student, achievement type, event or competition name, level, date, and optional notes.

**Relationships:** Belongs to one student. Created by one recorder.

---

## 6. Aggregates

### Behaviour Note (Aggregate Root)

- **Consistency boundary:** A single behaviour note and its data.
- **Owned entities:** None (flat structure).
- **Business responsibility:** Ensures every behaviour observation is attributed to a student, classified by type, dated, described, and attributed to a recorder.

### Discipline Record (Aggregate Root)

- **Consistency boundary:** A single discipline record and its data.
- **Owned entities:** None (flat structure).
- **Business responsibility:** Ensures every disciplinary action is attributed to a student, categorised by violation type, dated, described with a sanction, and attributed to a recorder.

### Extracurricular Activity (Aggregate Root)

- **Consistency boundary:** A single extracurricular activity, including all participations belonging to it.
- **Owned entities:** Participation.
- **Business responsibility:** Ensures the activity has a name, description, supervisor, and valid status. Governs creation and status transitions of participations within the activity.

### Achievement (Aggregate Root)

- **Consistency boundary:** A single achievement record and its data.
- **Owned entities:** None (flat structure).
- **Business responsibility:** Ensures every achievement is attributed to a student, categorised by type, associated with an event name and level, dated, and optionally annotated.

---

## 7. Business Rules

| ID | Rule |
|---|---|
| BR-DEV-001 | Every behaviour note must be classified as exactly one behaviour type: positive or negative. |
| BR-DEV-002 | Every behaviour note must reference a valid enrolled student. |
| BR-DEV-003 | Every behaviour note must have a date, a brief description, and a recorder. |
| BR-DEV-004 | Every discipline record must specify a violation type, a description, and a sanction. |
| BR-DEV-005 | Every discipline record must reference a valid enrolled student. |
| BR-DEV-006 | Every discipline record must have a date and a recorder. |
| BR-DEV-007 | An extracurricular activity must have a unique name within the school. |
| BR-DEV-008 | An extracurricular activity must have an assigned supervisor. |
| BR-DEV-009 | A student may participate in the same extracurricular activity only once per academic year. |
| BR-DEV-010 | A participation can only be created for an extracurricular activity whose status is active. |
| BR-DEV-011 | Every achievement must reference a valid enrolled student. |
| BR-DEV-012 | Every achievement must specify an achievement type, event or competition name, level, and date. |
| BR-DEV-013 | Achievement level must be one of: school, city, province, or national. |
| BR-DEV-014 | A sanction must be one of the recognised sanction types: verbal warning, written warning, parent meeting, or suspension. |

---

## 8. State Machines & Lifecycles

### Extracurricular Activity Status

```
[Active] ──deactivate──▸ [Inactive]
[Inactive] ──reactivate──▸ [Active]
```

| State | Meaning |
|---|---|
| Active | The activity is currently offered and open for new participations. |
| Inactive | The activity is no longer offered. No new participations may be created, but existing participation records are retained. |

**Transitions:**

| From | To | Trigger |
|---|---|---|
| Active | Inactive | School administrator deactivates the activity. |
| Inactive | Active | School administrator reactivates the activity. |

### Participation Status

```
[Active] ──complete──▸ [Completed]
[Active] ──withdraw──▸ [Withdrawn]
```

| State | Meaning |
|---|---|
| Active | The student is currently participating in the activity for the academic year. |
| Completed | The student has finished the activity for the academic year. |
| Withdrawn | The student has left the activity before completion. |

**Transitions:**

| From | To | Trigger |
|---|---|---|
| Active | Completed | The participation period ends or the student finishes the activity. |
| Active | Withdrawn | The student leaves the activity before completion. |

> Completed and Withdrawn are terminal states. A student who wishes to rejoin must create a new participation in a subsequent academic year.

---

## 9. Domain Events

| Event | Description | Trigger |
|---|---|---|
| Behaviour Note Recorded | A new behaviour observation has been captured for a student. | A recorder creates a behaviour note. |
| Discipline Record Recorded | A new code-of-conduct violation and sanction has been captured for a student. | A recorder creates a discipline record. |
| Extracurricular Activity Created | A new extracurricular activity has been established at the school. | A school administrator creates an activity. |
| Extracurricular Activity Deactivated | An extracurricular activity is no longer offered. | A school administrator deactivates an active activity. |
| Extracurricular Activity Reactivated | A previously inactive extracurricular activity is offered again. | A school administrator reactivates an inactive activity. |
| Participation Started | A student has joined an extracurricular activity for an academic year. | A participation record is created with status active. |
| Participation Completed | A student has finished an extracurricular activity for the academic year. | A participation transitions from active to completed. |
| Participation Withdrawn | A student has left an extracurricular activity before completion. | A participation transitions from active to withdrawn. |
| Achievement Recorded | A new non-academic accomplishment has been captured for a student. | A recorder creates an achievement. |

---

## 10. Business Workflows

### Record a Behaviour Note

1. A teacher observes a notable student behaviour (positive or negative).
2. The teacher identifies the student and classifies the behaviour type.
3. The teacher provides the date, a brief description, and submits the note.
4. The behaviour note is recorded and attributed to the teacher as recorder.

### Record a Discipline Record

1. A teacher or school principal identifies a code-of-conduct violation by a student.
2. The recorder identifies the student, selects the violation type, and describes the incident.
3. The recorder determines and specifies the appropriate sanction.
4. The discipline record is recorded with the date and attributed to the recorder.

### Manage Extracurricular Activities

1. A school administrator establishes a new extracurricular activity with a name, description, and assigned supervisor.
2. The activity is created with active status.
3. When the school no longer offers the activity, the administrator deactivates it.
4. A previously deactivated activity may be reactivated when the school resumes offering it.

### Enrol a Student in an Extracurricular Activity

1. A school administrator identifies the student and the desired extracurricular activity.
2. The activity must be active and the student must not already have a participation in the same activity for the current academic year.
3. A participation record is created with active status for the current academic year.

### Complete or Withdraw a Participation

1. When the activity period ends or the student finishes, the participation is marked as completed.
2. If the student leaves the activity early, the participation is marked as withdrawn.

### Record a Non-Academic Achievement

1. A teacher identifies a student who has attained a non-academic accomplishment.
2. The teacher specifies the achievement type, event or competition name, level, and date.
3. Optional notes may be added for additional context.
4. The achievement is recorded and attributed to the teacher as recorder.
