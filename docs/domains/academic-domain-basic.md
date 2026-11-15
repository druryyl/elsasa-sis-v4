# Academic Domain — Basic

## 1. Business Overview

The Academic domain (Basic) manages the foundational learning and assessment activities of an Indonesian K-12 school (SD/SMP/SMA/SMK). It uses a curriculum-agnostic, three-level learning structure (`Subject → Subject Stage → Learning Unit`) decoupled from administrative Grades so schools can support both standard grade-aligned cohorts and individualized progression (such as ACE levels).

It is the authoritative source for:

- The school's curriculum structure (`Subject`, `Subject Stage`, and `Learning Unit`) and its separate mapping to administrative Grades.
- Which students participate in which `Subject Stage` for a semester (`Operational Period`).
- Daily student attendance per `Subject`.
- Assignment and exam scores per `Subject Stage` (optionally linked to a `Learning Unit`).
- Periodic report cards.

The domain begins when the school defines its subjects, stages, and learning units, and students are linked to their subject stages. It continues through daily attendance recording and assignment/exam scoring, and culminates in the generation of report cards per semester.

**Boundary:** This domain does not manage student identity, student placement into administrative grades or study groups, teaching staff assignments, financial obligations, operational scheduling, or student development. It receives student, operational period, study group, and teacher information from outside its boundary and produces report cards as its primary output.

**Primary consumers:** Teachers (daily workflow), Parents (report cards), School Administration (curriculum and academic oversight).

---

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Subject | A broad area of learning offered by the school (e.g., Mathematics, Science, Islamic Studies). |
| Subject Stage | A grouping of learning content within a Subject representing a specific educational phase, level, or progression (e.g., Mathematics Level 7, Matematika Fase D), independent of administrative Grade. |
| Learning Unit | A specific learning package or instructional unit within a Subject Stage (e.g., a Module, PACE, Lesson, Presentation, or Book). |
| Unit Type | The instructional format of a Learning Unit: Module, PACE, Lesson, Presentation, Book, or Other. |
| School Level | The administrative tier offered by the school: SD, SMP, SMA, or SMK. |
| Grade | A timeless administrative year-level within a school level (e.g., Grade 7, Grade 10). |
| Stage Grade Mapping | A separate many-to-many association linking a Subject Stage to zero or more Administrative Grades (`School Level` + `Grade`). |
| Study Group | An academic-year-specific group of students organized by the school (e.g., 7A, 7B). Study group composition is managed in the Student domain. |
| Academic Year | A one-year period representing the school's annual learning cycle. |
| Operational Period (Semester) | One half of an Academic Year (Semester 1, Semester 2); the scheduling, participation, and reporting cycle for report cards. |
| Academic Participation | The link between a Student and a Subject Stage for a given Operational Period (Semester), indicating the student is undertaking that stage. |
| Attendance | A record of a student's presence status for a specific Subject session on a specific date. |
| Attendance Status | The classification of a student's presence: Present, Absent, Excused, Sick, or Late. |
| Assignment | A piece of assessed work given by a teacher within a Subject Stage, optionally tied to a specific Learning Unit (e.g., homework, project, PACE test, quiz). |
| Exam | A formal assessment event within a Subject Stage, optionally tied to a Learning Unit (e.g., midterm exam, final exam). |
| Assessment Type | The category of an assessed work: Assignment, Quiz, Midterm, or Final. |
| Score | A numeric value (0–100) representing a student's achievement on a single Assignment or Exam. |
| Weight | An optional proportion assigned to an Assignment or Exam indicating its relative importance in calculating the final grade for a Subject Stage. |
| Final Grade | The computed result for a student in a Subject Stage for a semester, derived from the weighted or simple average of all scores. |
| Letter Grade | An optional alphabetic classification (A, B, C, D, E) mapped from the Final Grade. |
| Report Card | A formal document listing a student's final grades across all Subject Stages participated in for one semester, optionally including general remarks and teacher notes. |
| Final Grade Entry | A single Subject Stage result line within a Report Card, carrying both Subject and Subject Stage information. |
| Teacher Note | An optional free-text remark by a teacher about a student's performance in a Subject Stage on the report card. |
| Student | A learner enrolled at the school. Identity and enrollment are managed outside this domain; Academic receives student references. |
| Teacher | A person responsible for teaching a subject, recording attendance, scoring assignments, and contributing to report cards. |

---

## 3. Business Capabilities

| # | Capability | Description |
|---|---|---|
| 1 | **Maintain Learning Structure** | Define and maintain Subjects, their Subject Stages (progressions/levels), Learning Units within each stage, and optional separate mappings to Administrative Grades (`School Level` + `Grade`). |
| 2 | **Manage Academic Participation** | Establish and maintain the association between students and the Subject Stages they undertake for a given Academic Year and Operational Period (Semester). |
| 3 | **Record Student Attendance** | Capture student presence status for each Subject session on each school day. |
| 4 | **Record Assignments & Scores** | Define assignments and exams within a Subject Stage (optionally linked to a Learning Unit) and record individual student scores. |
| 5 | **Generate Report Cards** | Compute final grades per Subject Stage per student per semester and produce a report card document. |

---

## 4. Actors & Roles

| Actor | Responsibilities |
|---|---|
| **Teacher** | Defines assignments and exams for their Subject Stages (and optional Learning Units). Records student scores. Records daily attendance for their Subjects. Provides optional teacher notes for report cards. |
| **Homeroom Teacher** | Reviews and finalizes report cards for students in their study group. May add general remarks to report cards. |
| **School Administrator** | Defines and maintains Subjects, Subject Stages, Learning Units, and Administrative Grade mappings. Manages academic participation links (from grade mappings or individually). Triggers report card generation. |
| **Principal** | Oversees academic activities. May review report cards before distribution. |
| **Parent** | Receives and reviews the student's report card. No write authority within this domain. |

---

## 5. Domain Objects

### Subject

- **Purpose:** Represents a broad area of learning offered by the school (e.g., Mathematics, Science, Islamic Studies).
- **Responsibility:** Holds the identity of the Subject (unique name, optional unique code). Exists independently of curriculum progression levels and administrative Grades.
- **Relationships:** Has zero or more Subject Stages. Referenced by Attendance Records and Academic Participations.

### Subject Stage

- **Purpose:** Represents a grouping of learning content within a Subject, capturing a curriculum phase, level, or progression (e.g., Mathematics Level 7, Matematika Fase D).
- **Responsibility:** Holds the stage name, optional code, and `sequence_order` within its parent Subject. Owns its Learning Units and optional Administrative Grade mappings.
- **Relationships:** Belongs to one Subject. Contains zero or more Learning Units. Maps to zero or more Administrative Grades (`School Level` + `Grade`). Targeted by Academic Participation, Assignments, and Report Card Final Grade Entries.

### Learning Unit

- **Purpose:** Represents a specific learning package or instructional unit within a Subject Stage (such as a Teaching Module, PACE, Lesson, Presentation, or Book).
- **Responsibility:** Holds the unit name, optional code, `unit_type` (`Module`, `PACE`, `Lesson`, `Presentation`, `Book`, `Other`), and `sequence_order` within the stage.
- **Relationships:** Belongs to one Subject Stage. Optionally referenced by Assignments.

### Stage Grade Mapping

- **Purpose:** Maintains the separate many-to-many link between a Subject Stage and Administrative Grades (`School Level` + `Grade`) without embedding administrative grade assumptions into the curriculum hierarchy.
- **Responsibility:** Records which Administrative Grades commonly correspond to a Subject Stage, supporting cohort setup while allowing students in the same Administrative Grade to follow different Subject Stages.
- **Relationships:** Owned by a Subject Stage; specifies a `(School Level, Grade)` pair.

### Academic Participation

- **Purpose:** Represents the fact that a specific student undertakes a specific Subject Stage in a given Academic Year and Operational Period (Semester).
- **Responsibility:** Tracks participation status (`Active` or `Completed`). Serves as the basis for which students should have attendance recorded (via the Subject) and scores entered (via the Subject Stage).
- **Relationships:** Links one Student to one Subject Stage (and its parent Subject) for one Operational Period.

### Attendance Record

- **Purpose:** Captures a student's presence status for a Subject session on a given date.
- **Responsibility:** Holds the attendance status (`Present`, `Absent`, `Excused`, `Sick`, `Late`) and the date. Keyed at the Subject level so timetable sessions align with attendance even when students work on different Subject Stages.
- **Relationships:** Belongs to one Student and one Subject within an Operational Period. One record per student per Subject per date.

### Assignment

- **Purpose:** Represents a piece of assessed work or an exam within a Subject Stage.
- **Responsibility:** Holds the assignment/exam title, assessment type, date, optional Learning Unit reference, and optional weight. Serves as the container to which student scores are attached.
- **Relationships:** Belongs to one Subject Stage within an Operational Period, and optionally references one Learning Unit inside that Subject Stage. Has many Scores (one per student).

### Score

- **Purpose:** Records a student's numeric achievement on a single assignment or exam.
- **Responsibility:** Holds the numeric value (0–100) for one student on one assignment.
- **Relationships:** Belongs to one Assignment and one Student.

### Report Card

- **Purpose:** The formal periodic learning report for one student for one semester (`Operational Period`).
- **Responsibility:** Aggregates final grades across all Subject Stages the student participates in during the semester. May include general remarks from the Homeroom Teacher and per-stage teacher notes.
- **Relationships:** Belongs to one Student and one Operational Period. Contains one Final Grade Entry per Subject Stage the student participates in.

### Final Grade Entry

- **Purpose:** A single Subject Stage result line within a Report Card.
- **Responsibility:** Holds the computed final grade (numeric), optional letter grade, and optional teacher note for one Subject Stage (carrying both Subject and Subject Stage identifiers).
- **Relationships:** Belongs to one Report Card. Corresponds to one Subject Stage (and its parent Subject).

---

## 6. Aggregates

### Subject Aggregate

- **Aggregate Root:** Subject
- **Owned Entities:** None (`Subject Stage` is managed as its own aggregate referencing `Subject`).
- **Consistency Boundary:** A Subject's identity (`name` and optional `code`).
- **Business Responsibility:** Ensures each Subject has a unique identity within the school.

### Subject Stage Aggregate

- **Aggregate Root:** Subject Stage
- **Owned Entities:** Learning Unit, Stage Grade Mapping.
- **Consistency Boundary:** A Subject Stage's identity, progression order within its Subject, its Learning Units, and its Administrative Grade mappings.
- **Business Responsibility:** Ensures stage progression order and unit progression order remain consistent and unique within their containers, while keeping Administrative Grade mappings optional and separate from the curriculum hierarchy.

### Academic Participation Aggregate

- **Aggregate Root:** Academic Participation
- **Owned Entities:** None.
- **Consistency Boundary:** A single student–subject-stage–operational-period link and its status.
- **Business Responsibility:** Ensures a student is linked to a given Subject Stage at most once per Operational Period while allowing multiple Subject Stages of the same Subject when progressing within a semester.

### Attendance Record Aggregate

- **Aggregate Root:** Attendance Record
- **Owned Entities:** None.
- **Consistency Boundary:** A single attendance entry for one student, one Subject, one date.
- **Business Responsibility:** Ensures only one attendance status exists per student per Subject per date.

### Assignment Aggregate

- **Aggregate Root:** Assignment
- **Owned Entities:** Score.
- **Consistency Boundary:** An assignment (within a Subject Stage, optionally linked to a Learning Unit) and all its student scores.
- **Business Responsibility:** Ensures scores are only recorded for a valid assignment, scores fall within the allowed range, and each student has at most one score per assignment.

### Report Card Aggregate

- **Aggregate Root:** Report Card
- **Owned Entities:** Final Grade Entry.
- **Consistency Boundary:** A report card and all its Subject Stage final grade entries form a single consistent unit.
- **Business Responsibility:** Ensures a report card accurately reflects the computed grades for all Subject Stages a student participates in during the semester.

---

## 7. Business Rules

### Learning Structure

| ID | Rule |
|---|---|
| BR-LSTR-001 | Every Subject must have a unique name within the school. |
| BR-LSTR-002 | A Subject code, if provided, must be unique within the school. |
| BR-LSTR-003 | Every Subject Stage must belong to exactly one Subject, and have a unique name and unique sequence order within that Subject. |
| BR-LSTR-004 | Every Learning Unit must belong to exactly one Subject Stage, specify a valid Unit Type (Module, PACE, Lesson, Presentation, Book, or Other), and have a unique name and unique sequence order within that Subject Stage. |
| BR-LSTR-005 | A Subject Stage may map to zero or more Administrative Grades (School Level + Grade), and an Administrative Grade may map to zero or more Subject Stages, with no duplicate `(Subject Stage, School Level, Grade)` mapping. |

### Academic Participation

| ID | Rule |
|---|---|
| BR-APRT-001 | A Student may participate in a given Subject Stage at most once per Academic Year/Semester (Operational Period). |
| BR-APRT-002 | Academic Participation can only be created for a Subject Stage that exists in the Learning Structure. |
| BR-APRT-003 | Academic Participation can only be created for a Student recognized by the school (student identity managed outside this domain). |

### Student Attendance

| ID | Rule |
|---|---|
| BR-ATTN-001 | Only one Attendance Record may exist per Student per Subject per date. |
| BR-ATTN-002 | Attendance Status must be one of: Present, Absent, Excused, Sick, or Late. |
| BR-ATTN-003 | Attendance may only be recorded for a Student with an Active Academic Participation in at least one Subject Stage belonging to that Subject for the Operational Period. |

### Assignments & Assessment

| ID | Rule |
|---|---|
| BR-ASGN-001 | An Assignment must belong to exactly one Subject Stage, and any optional Learning Unit referenced must belong to that same Subject Stage. |
| BR-ASGN-002 | Assessment Type must be one of: Assignment, Quiz, Midterm, or Final. |
| BR-ASGN-003 | A Score must be a numeric value between 0 and 100 inclusive. |
| BR-ASGN-004 | A Student may have at most one Score per Assignment. |
| BR-ASGN-005 | Scores may only be recorded for Students with Active Academic Participation in the Assignment's Subject Stage. |
| BR-ASGN-006 | If weights are used for assignments within a Subject Stage, the sum of all weights for that Subject Stage should equal 100%. |

### Report Card

| ID | Rule |
|---|---|
| BR-RPCD-001 | A Report Card is generated per Student per Semester (Operational Period). |
| BR-RPCD-002 | The Final Grade for a Subject Stage is calculated as the weighted average of all scores in that Subject Stage for the semester. If no weights are defined, a simple arithmetic average is used. |
| BR-RPCD-003 | A Report Card must include a Final Grade Entry for every Subject Stage in which the Student has an Active or Completed participation during the semester. |
| BR-RPCD-004 | Letter Grade mapping, if used, must follow the school's defined conversion table. |

---

## 8. State Machines & Lifecycles

### Academic Participation Lifecycle

```
[Active] → [Completed]
```

| State | Meaning |
|---|---|
| **Active** | The student is currently undertaking this Subject Stage for the Operational Period. Attendance and scores can be recorded. |
| **Completed** | The semester has ended. No further attendance or scores are recorded. Final grade has been determined. |

**Transitions:**

| From | To | Trigger |
|---|---|---|
| Active | Completed | Semester ends and report card is finalized. |

### Report Card Lifecycle

```
[Draft] → [Finalized]
```

| State | Meaning |
|---|---|
| **Draft** | The report card has been generated with computed grades. It can still be reviewed and corrected. |
| **Finalized** | The report card is confirmed and ready for distribution to parents. No further changes. |

**Transitions:**

| From | To | Trigger |
|---|---|---|
| Draft | Finalized | Homeroom Teacher or School Administrator confirms the report card. |

---

## 9. Domain Events

| Event | Meaning | Triggered When |
|---|---|---|
| **Subject Defined** | A new Subject has been added to the school's learning structure. | School Administrator adds a Subject. |
| **Subject Stage Defined** | A new progression stage has been added under a Subject. | School Administrator adds a Subject Stage (and optional Learning Units or Grade Mappings). |
| **Learning Unit Defined** | A specific instructional unit has been added under a Subject Stage. | School Administrator adds a Learning Unit. |
| **Participation Established** | A student has been linked to a Subject Stage for an Operational Period. | Student is assigned to a Subject Stage (via Grade mapping or individual proficiency). |
| **Attendance Recorded** | A student's presence status for a Subject on a date has been captured. | Teacher records attendance. |
| **Score Recorded** | A student's score for an assignment or exam has been captured. | Teacher enters a score. |
| **Report Card Generated** | A draft report card with computed final grades has been produced for a student for a semester. | System computes grades per Subject Stage and assembles the report card. |
| **Report Card Finalized** | A report card has been confirmed and is ready for distribution. | Homeroom Teacher or Administrator confirms the report card. |
| **Participation Completed** | A student's participation in a Subject Stage has transitioned from Active to Completed. | Semester concludes and grades are finalized. |

---

## 10. Business Workflows

### Define Learning Structure

1. School Administrator defines Subjects taught at the school (e.g., Mathematics, Science, Islamic Studies).
2. Under each Subject, the administrator creates ordered Subject Stages representing curriculum progressions (e.g., Level 7, Level 8, or Fase D).
3. Inside each Subject Stage, the administrator defines ordered Learning Units with their Unit Type (e.g., PACE 1073, Teaching Module 1, Lesson 1).
4. Optionally, the administrator maintains Stage Grade Mappings linking Subject Stages to Administrative Grades (`School Level` + `Grade`) for default cohort assignment without binding curriculum progression to administrative Grade.
5. **Outcome:** The school has a flexible, curriculum-agnostic hierarchy independent of administrative structure.

### Establish Academic Participation

1. At the beginning of an Operational Period (or as a student advances during the semester), students are linked to the Subject Stages they will undertake.
2. For standard cohort programs, students may be assigned to Subject Stages suggested by their Administrative Grade mapping via study group membership; for individualized programs (e.g., ACE), students in the same study group may be assigned individually to different Subject Stages based on proficiency.
3. Each participation record is created with status Active.
4. **Outcome:** Every student has a defined set of Subject Stages for the semester.

### Record Daily Attendance

1. For each school day, the Teacher opens the Subject session (matching the study group's timetable schedule) and records attendance for participating students.
2. Each student receives an attendance status: Present, Absent, Excused, Sick, or Late.
3. If a previously recorded attendance is incorrect, the Teacher corrects it by updating the status.
4. **Outcome:** Daily attendance is captured per Subject for all participating students.

### Score Assignments & Exams

1. The Teacher defines an assignment or exam for a Subject Stage (optionally selecting a specific Learning Unit for unit-level tasks/PACE tests, or leaving Learning Unit unset for stage-wide Midterm/Final exams), specifying title, type, date, and optional weight.
2. After students complete the work, the Teacher records a numeric score (0–100) for each student with Active participation in that Subject Stage.
3. Scores can be updated if corrections are needed.
4. **Outcome:** All student scores for the Subject Stage are recorded and available for grade computation.

### Generate & Finalize Report Cards

1. At the end of the semester, the School Administrator triggers report card generation for students.
2. For each student, the system computes the Final Grade per Subject Stage using the weighted average (or simple average if no weights) of all recorded scores in that Subject Stage.
3. Optional letter grades are derived from the numeric final grade per the school's conversion table.
4. Teachers may add optional notes per Subject Stage, and the Homeroom Teacher may add general remarks and review the draft report card.
5. Upon confirmation, the report card is finalized and made available for distribution to parents.
6. Academic Participation for the semester transitions to Completed.
7. **Outcome:** Each student receives a finalized report card detailing their Subject Stage results for the semester.
