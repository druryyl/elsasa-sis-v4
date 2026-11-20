# ISSUE

## Metadata

ID: CR-STAGE-2
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage 2: Timetables & Daily Academic Operations (Timetables, Curriculum Structure, Daily Attendance, and Teacher Scorebook)

## Source

Reported By: User
Reported Date: 2026-11-19

## Description

Implement Stage 2 ("Timetables & Daily Academic Operations") of the ELSASA-SIS v4 development roadmap.

This change request builds directly upon the operational setup and master data foundation delivered in Stage 1 (academic years, rooms, calendar entries, announcements, students with NIS, study groups, student placements, teacher profiles, and teaching assignments). It aims to activate the core daily instructional and academic operations of the institution:

1. **Timetables & Scheduling (`opr`):** Master weekly scheduling for study groups, period configuration, classroom/facility allocation, teacher assignment linking, SMK vocational block rotation templates / alternate-week cycle support, and personalized teacher schedule views.
2. **Curriculum Structure & Subject Enrollment (`acd`):** Formal subject curriculum hierarchy (subjects, sequential stages, learning units, and grade mapping scales) and batch academic enrollment of placed students into subject stages (`academic_participations`).
3. **Daily Classroom Attendance (`acd`):** Daily period-based or subject-based attendance recording (`PRESENT`, `ABSENT`, `LATE`, `EXCUSED`), mass attendance marking, and real-time attendance status tracking.
4. **Classroom Assessments & Teacher Scorebook (`acd`):** Formative, summative, and examination assessment creation, student score and feedback recording, visual passing threshold indicators, running subject score computation, and consolidated gradebook summaries.

The scope realizes 8 actor-driven use cases (`UC-ADM-04`, `UC-ADM-17..18`, `UC-TCH-01..03`, `UC-TCH-05`, `UC-TCH-10`) across 4 interactive UI screens (`SCR-OPR-TIMETABLE`, `SCR-ACD-CURRICULUM`, `SCR-ACD-ATTENDANCE`, and `SCR-ACD-SCOREBOOK`).

## Desired Outcome

1. **Master Timetables & Teacher Schedule:**
   - Administrative personnel can configure study group timetables with day, period slot, subject, assigned teacher, and allocated room.
   - The scheduling interface accommodates standard weekly patterns as well as SMK block rotation templates (e.g., alternating theory and workshop weeks).
   - Teachers can log in and view their personal weekly schedule and assigned classes without seeing unrelated operational noise.

2. **Curriculum Structure & Student Academic Participation:**
   - Administrative staff can structure academic curriculum under subjects, defining sequential learning stages, modular learning units, and grading scales.
   - Students assigned to a study group can be batch-enrolled into their corresponding subject stages (`academic_participations`) for the active academic term.

3. **Daily Attendance Management:**
   - Teachers can record and update daily subject or period attendance for their enrolled students using standard classifications (`PRESENT`, `ABSENT`, `LATE`, `EXCUSED`).
   - Teachers can utilize bulk marking shortcuts (e.g., "Mark All Present") to streamline daily classroom routines.

4. **Assessment & Scorebook Operations:**
   - Teachers can create graded assessments categorized by type (`FORMATIVE`, `SUMMATIVE`, `EXAM`) with defined maximum points, weightings, and assessment dates.
   - Teachers can enter and adjust individual student scores and qualitative feedback in a responsive spreadsheet-style grid.
   - The system computes student running totals, highlights passing/failing thresholds visually, and provides attendance summaries within the gradebook view.

## Current Situation

Stage 0 ("Walking Skeleton & Reference Slice") and Stage 1 ("Operational Setup & Master Data Foundation") have both been completed and verified (`IS001- STAGE-0-DEV-IMPLEMENTATION-PLAN.md` and `IS002- STAGE-1-DEV-IMPLEMENTATION-PLAN.md` are marked `COMPLETED`). The application provides working authentication, navigation, operational calendars, rooms, announcements, student admission and enrollment with NIS, annual study groups (rombel), student class placements, teacher directories, subject capabilities, and teaching allocations.

However, the application currently lacks:
- Master timetable scheduling grids and room/teacher conflict safeguards.
- SMK alternate-week / block rotation schedule management.
- Teacher-scoped personal timetable and schedule views.
- Curriculum hierarchy management (subject stages, learning units, grading scale definitions).
- Student batch enrollment into subject stages (`academic_participations`).
- Daily attendance tracking and roster logging mechanisms.
- Assessment definition, score entry, running subject grade calculation, and teacher gradebook views.
- The corresponding backend domain models/modules (`ElsasaSis.Modules.Acd` expansion, `ElsasaSis.Modules.Opr` timetable expansion) and frontend screens (`SCR-OPR-TIMETABLE`, `SCR-ACD-CURRICULUM`, `SCR-ACD-ATTENDANCE`, and `SCR-ACD-SCOREBOOK`).

## Evidence

- Development Roadmap: [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md) (Section 3: Stage 2 Timetables & Daily Academic Operations)
- Stage 0 Implementation Plan (Completed): [IS001- STAGE-0-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-IMPLEMENTATION-PLAN.md)
- Stage 1 Implementation Plan (Completed): [IS002- STAGE-1-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS002-%20STAGE-1-DEV-IMPLEMENTATION-PLAN.md)
- System Architecture: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
- Backend Architecture: [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
- Frontend Architecture: [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
- Screen Inventory: [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) (`SCR-OPR-TIMETABLE`, `SCR-ACD-CURRICULUM`, `SCR-ACD-ATTENDANCE`, `SCR-ACD-SCOREBOOK`)
- Use Case Discovery: [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (Use Cases `UC-ADM-04`, `UC-ADM-17`, `UC-ADM-18`, `UC-TCH-01`, `UC-TCH-02`, `UC-TCH-03`, `UC-TCH-05`, `UC-TCH-10`)
- Operational Table Design: [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md) (`opr.study_group_schedules`, `opr.study_group_timetables`)
- Academic Table Design: [academic-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/academic-tables-design.md) (`acd.subjects`, `acd.subject_stages`, `acd.learning_units`, `acd.academic_participations`, `acd.daily_attendances`, `acd.assessments`, `acd.assessment_scores`)
- Student Table Design: [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md)
- Teaching Table Design: [teaching-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/teaching-tables-design.md)
- Roles & Actors Specification: [actors-roles-specification-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/actors-roles-specification-basic.md)

## Notes

- Intake captured in accordance with the ELSASA-SIS v4 development roadmap for Stage 2.
- Solution-neutral intake: captures the requested system capabilities and user-facing operational outcomes without prescribing technical implementation specifics, architectural boundaries, or slice sequencing, which will be formally evaluated during Discovery and Feasibility Assessment by the Analyst (`ica-analyst`).
