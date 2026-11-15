# Operational Domain — Basic

## 1. Business Overview

The Operations domain (Basic) manages the foundational shared time, place, and communication context required for a school to operate digitally. It provides the minimum operating structure: knowing what academic year it is, when school is open, what the study group schedule is, what rooms exist, and how to communicate announcements.

It is the authoritative source for:

- The active Academic Year and its Operational Periods
- The School Calendar
- The Study Group Timetable and Schedules
- Room master data
- School Announcements

Operations begins when the school establishes its basic operating context and continues by maintaining these shared references that all other domains depend on.

**Boundary:** This domain does not determine student status, learning requirements, student-development outcomes, financial outcomes, or teaching assignments.

---

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Academic Year | A named span of time (typically one calendar year) that defines the school's primary operating cycle. Exactly one Academic Year may be active at any time. |
| Operational Period | A subdivision of an Academic Year representing a grading and reporting cycle. At the Basic level this is a semester (Semester 1, Semester 2). |
| Active Academic Year | The single Academic Year currently governing school operations. All time-dependent references resolve against this year. |
| School Calendar | The complete set of Calendar Entries for one Academic Year, defining which dates are school days, holidays, or closures. |
| Calendar Entry | A single date within a School Calendar classified by Day Type with an optional description. |
| Day Type | The classification of a calendar date: School Day, Holiday, or Closure. |
| School Day | A date on which regular learning activities are expected to occur. |
| Holiday | A date on which the school is closed for a recognised holiday. |
| Closure | A date on which the school is closed for a reason other than a holiday (e.g., natural disaster, emergency). |
| Timetable | The recurring weekly schedule of learning activities for a Study Group within an Operational Period, expressed as Study Group Schedules (Timetable Entries) and optional date-range block mappings (Study Group Timetables). |
| Timetable Entry | A single scheduled learning activity (Study Group Schedule) specifying subject, study group, day of week, start time, end time, teacher, and optionally a room. |
| Study Group | An academic-year-specific student group within a grade (e.g., 7A, 7B), referenced from the Student domain. |
| Time Slot | The combination of day of week, start time, and end time that positions a Timetable Entry within the week. |
| Room | A named physical space in the school available for learning activities or other school use. |
| Room Type | The classification of a Room: Classroom, Laboratory, Hall, or Other. |
| Capacity | The maximum number of occupants a Room can accommodate. |
| Announcement | A published communication from the school to a defined segment of the school community. |
| Target Audience | The intended recipient group of an Announcement: All, Teachers, Students, or Parents. |
| Author | The person who creates an Announcement. |

---

## 3. Business Capabilities

| # | Capability | Description |
|---|---|---|
| 1 | Establish Academic Year & Periods | Define and activate the Academic Year and its Operational Periods so the school has a shared temporal frame of reference. |
| 2 | Maintain School Calendar | Record which dates within the Academic Year are school days, holidays, or closures so the community knows when school is open. |
| 3 | Schedule Timetable | Place learning activities into weekly time slots per Operational Period, assigning subject, study group, teacher, and optionally a room, so the school has a known study group schedule. |
| 4 | Manage Room Data | Maintain the list of available rooms with their type and capacity so scheduling and space awareness are possible. |
| 5 | Publish Announcements | Communicate information to the school community (all, teachers, students, or parents) so stakeholders remain informed. |

---

## 4. Actors & Roles

| Actor | Responsibilities |
|---|---|
| **School Administrator** | Establishes and activates the Academic Year and its Operational Periods. Maintains the School Calendar. Manages Room data. Publishes Announcements to any audience. Has full authority over all operational data. |
| **Curriculum Coordinator** | Creates and maintains the Timetable, assigning subjects, study groups, teachers, and rooms to time slots. |
| **Teacher** | Consults the Timetable and School Calendar. May author Announcements directed at students or parents. |
| **Student** | Consults the Timetable, School Calendar, and Announcements relevant to them. No authority to modify operational data. |
| **Parent / Guardian** | Consults the School Calendar and Announcements relevant to them. No authority to modify operational data. |

---

## 5. Domain Objects

### Academic Year

The primary temporal boundary for all school operations. Carries a name (e.g., "2025/2026"), start date, end date, and status. Every other operational object exists within the context of an Academic Year.

### Operational Period

A named subdivision of an Academic Year (Semester 1, Semester 2). Carries a name, start date, and end date. Periods enable grading, reporting, and scheduling cycles within the year.

**Relationship:** Belongs to exactly one Academic Year.

### Calendar Entry

A single date record within a School Calendar. Carries a date, Day Type, and optional description.

**Relationship:** Belongs to exactly one Academic Year.

### Timetable Entry

A single scheduled learning activity (Study Group Schedule) within the weekly Timetable for an Operational Period. Carries subject, study group, day of week, start time, end time, teacher, and an optional room reference.

**Relationship:** Belongs to exactly one Operational Period. May reference one Room.

### Room

A physical space available for school use. Carries a name, Room Type, and Capacity. Exists independently of any Academic Year.

### Announcement

A communication published to the school community. Carries a title, content, author, publication date, and Target Audience.

**Relationship:** Belongs to exactly one Academic Year.

---

## 6. Aggregates

### Academic Year (Aggregate Root)

- **Consistency boundary:** The Academic Year and all its Operational Periods.
- **Owned entities:** Operational Period.
- **Business responsibility:** Ensures exactly one Academic Year is active. Ensures its Operational Periods are temporally valid and non-overlapping within the year's boundaries.

### School Calendar (Aggregate Root)

- **Consistency boundary:** All Calendar Entries for a single Academic Year.
- **Owned entities:** Calendar Entry.
- **Business responsibility:** Ensures each date within the Academic Year has at most one Calendar Entry and that all entries fall within the Academic Year's date range.

### Timetable (Aggregate Root)

- **Consistency boundary:** All Timetable Entries (Study Group Schedules) and block mappings (Study Group Timetables) for a single Operational Period.
- **Owned entities:** Timetable Entry (Study Group Schedule), Study Group Timetable.
- **Business responsibility:** Holds the complete weekly schedule of learning activities for an Operational Period.

### Room (Aggregate Root)

- **Consistency boundary:** The Room itself.
- **Owned entities:** None.
- **Business responsibility:** Maintains the identity, type, and capacity of a physical space. Room names must be unique across the school.

### Announcement (Aggregate Root)

- **Consistency boundary:** The Announcement itself.
- **Owned entities:** None.
- **Business responsibility:** Holds a published communication with its audience targeting.

---

## 7. Business Rules

| ID | Rule |
|---|---|
| BR-OPS-001 | Exactly one Academic Year may be active at any time. Activating a new Academic Year deactivates the previously active one. |
| BR-OPS-002 | An Academic Year must contain at least one Operational Period. |
| BR-OPS-003 | Each Operational Period's start and end dates must fall within the start and end dates of its Academic Year. |
| BR-OPS-004 | Operational Periods within the same Academic Year must not have overlapping date ranges. |
| BR-OPS-005 | An Academic Year's end date must be after its start date. |
| BR-OPS-006 | An Operational Period's end date must be after its start date. |
| BR-OPS-007 | A Calendar Entry's date must fall within the date range of its Academic Year. |
| BR-OPS-008 | Each date within an Academic Year may have at most one Calendar Entry. |
| BR-OPS-009 | A Timetable Entry's (Study Group Schedule's) end time must be after its start time. |
| BR-OPS-010 | Room names must be unique across the school. |
| BR-OPS-011 | Room Capacity must be a positive number. |
| BR-OPS-012 | An Announcement must specify exactly one Target Audience. |
| BR-OPS-013 | An Announcement must have a non-empty title and content. |

---

## 8. State Machines & Lifecycles

### Academic Year Lifecycle

```
Inactive ──► Active ──► Inactive
```

| State | Meaning |
|---|---|
| **Inactive** | The Academic Year is defined but is not the current operating year. It may be a future year not yet started, or a past year that has concluded. |
| **Active** | The Academic Year is the current operating year. All operational references resolve against this year. |

**Transitions:**

| From | To | Trigger |
|---|---|---|
| Inactive | Active | School Administrator activates this Academic Year (BR-OPS-001 deactivates any previously active year). |
| Active | Inactive | Another Academic Year is activated, or the School Administrator explicitly deactivates this year. |

### Announcement Lifecycle

```
Draft ──► Published
```

| State | Meaning |
|---|---|
| **Draft** | The Announcement is being composed and is not yet visible to the Target Audience. |
| **Published** | The Announcement is visible to its Target Audience. |

**Transitions:**

| From | To | Trigger |
|---|---|---|
| Draft | Published | Author publishes the Announcement. |

---

## 9. Domain Events

| Event | Meaning | Triggered When |
|---|---|---|
| Academic Year Activated | A new Academic Year has become the active operating year. | School Administrator activates an Academic Year. |
| Academic Year Deactivated | An Academic Year is no longer the active operating year. | Another Academic Year is activated, or the year is explicitly deactivated. |
| Calendar Entry Recorded | A date has been classified on the School Calendar. | School Administrator adds or updates a Calendar Entry. |
| Timetable Entry Scheduled | A learning activity has been placed in the weekly Timetable for a study group. | Curriculum Coordinator adds a Timetable Entry (Study Group Schedule). |
| Room Registered | A new Room has been added to the school's room list. | School Administrator registers a Room. |
| Announcement Published | An Announcement has been made visible to its Target Audience. | Author publishes an Announcement. |

---

## 10. Business Workflows

### Establish Academic Year

The School Administrator defines a new Academic Year with its name, start date, and end date, then creates its Operational Periods (Semester 1, Semester 2). When the school is ready to begin the year, the Administrator activates it. Activation makes this Academic Year the shared reference for all school operations and deactivates any previously active year.

### Maintain School Calendar

Once an Academic Year is active, the School Administrator records Calendar Entries to classify dates as school days, holidays, or closures. Entries may be added or modified throughout the year as circumstances change (e.g., an unplanned closure). The resulting calendar provides the authoritative answer to "Is school open on this date?"

### Schedule Timetable

The Curriculum Coordinator builds the weekly Timetable per Operational Period within the active Academic Year by creating Study Group Schedules (and optional Study Group Timetables for rotating block weeks). Each entry assigns a subject and teacher to a study group at a specific day and time, with an optional room. The Timetable provides the authoritative answer to "What is happening in which study group, when, and where?"

### Manage Room Data

The School Administrator maintains the list of available Rooms, recording each room's name, type, and capacity. Rooms may be added or updated at any time. This list serves as shared reference data for Timetable scheduling and general space awareness.

### Publish Announcements

An Author (School Administrator or Teacher) composes an Announcement with a title, content, and Target Audience. The Announcement begins as a Draft. When ready, the Author publishes it, making it visible to the specified audience. Published Announcements inform the school community of relevant information.
