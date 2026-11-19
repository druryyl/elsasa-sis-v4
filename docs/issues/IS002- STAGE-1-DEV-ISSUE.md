# ISSUE

## Metadata

ID: CR-STAGE-1
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage 1: Operational Setup & Master Data Foundation (School Operations, Student Lifecycle, and Teacher Master Data)

## Source

Reported By: User
Reported Date: 2026-11-19

## Description

Implement Stage 1 ("Operational Setup & Master Data Foundation") of the ELSASA-SIS v4 development roadmap.

This change request builds upon the foundational infrastructure and reference slice established in Stage 0 by operationalizing the core master data domains for the institution. Specifically, it encompasses the end-to-end realization of:
1. **School Operational Setup (`opr`):** School calendar event and holiday planning, facility/room master data inventory, operational bulletin announcements, and public/role-based calendar and announcement browsing.
2. **Student Lifecycle & Master Data (`std`):** Prospective student applicant intake, admission review by administrative staff, formal candidate approval/rejection by the principal, atomic enrollment with NIS generation, student profile and guardian maintenance, annual study group (rombel) configuration, student cohort placement, and role-scoped student roster directories.
3. **Teaching Master Data (`tch`):** Teacher profile records, subject capability mappings, homeroom and subject teaching allocations, and teacher workload/assignment directory views.

The scope realizes 23 actor-driven use cases (`UC-ADM-02..16`, `UC-ADM-27`, `UC-PRC-01`, `UC-PRC-05`, `UC-PRC-07`, `UC-TCH-11`, `UC-HRM-05`) across 5 comprehensive UI screens (`SCR-OPR-CALENDAR` Full, `SCR-OPR-ANNOUNCEMENTS`, `SCR-STD-ADMISSIONS`, `SCR-STD-DIRECTORY`, and `SCR-TCH-ASSIGNMENTS`).

## Desired Outcome

1. **School Operational Configuration:**
   - Administrative personnel can configure and maintain school calendar entries, national holidays, and institutional closures across the active academic year.
   - Administrative personnel can record physical room inventory (classrooms, laboratories, halls) with room capacities and operational statuses.
   - Administrative personnel can author, review, and publish announcements with audience targeting, which are accessible to teachers and staff through dedicated views.

2. **Admissions & Student Management:**
   - Administrative personnel can capture prospective student candidate registrations including applicant demographics and primary guardian contacts.
   - School administrative staff can record admissions recommendations, and the school principal can review, approve, or reject candidate applications.
   - Accepted admission candidates can be formally enrolled into official student records with automatic, sequential NIS allocation.
   - Administrative personnel can maintain ongoing student profiles, demographic records, and guardian relationships.
   - School administrators can define annual study groups (rombel) per grade level and assign enrolled students into their respective classes.
   - Homeroom teachers can securely access the roster and guardian emergency contact details strictly scoped to their assigned study group.

3. **Teacher Profiles & Workload Allocation:**
   - Administrative personnel can manage teacher profiles (NIP/NIK, employment status, contact details).
   - Qualified subject capabilities can be registered against teacher records.
   - Teachers can be assigned to study groups as homeroom teachers or subject teachers, and workload distribution can be inspected by administration and leadership.

## Current Situation

Stage 0 ("Walking Skeleton & Reference Slice") has been successfully implemented and verified (`IS001- STAGE-0-DEV-IMPLEMENTATION-PLAN.md` is `COMPLETED`). The application provides working authentication (JWT and session management), the ASP.NET Core API host pipeline with error handling, the database schema migration runner, core shared kernel primitives, and basic academic year / period configuration (`UC-ADM-01`, `SCR-AUTH-LOGIN`, `SCR-OPR-CALENDAR` Part 1).

However, the application currently lacks:
- Calendar events, school holidays, and operational announcements.
- Physical room inventory.
- Prospective student admissions processing and formal principal approval workflows.
- Official student enrollment, NIS numbering sequence, and guardian management.
- Study group (rombel) structures and student placement assignments.
- Teacher profile management, subject capability tagging, and teaching workload assignments.
- The corresponding backend domain models/modules (`ElsasaSis.Modules.Std`, `ElsasaSis.Modules.Tch`, expanded `ElsasaSis.Modules.Opr`) and frontend screens (`SCR-STD-ADMISSIONS`, `SCR-STD-DIRECTORY`, `SCR-TCH-ASSIGNMENTS`, `SCR-OPR-ANNOUNCEMENTS`, and full `SCR-OPR-CALENDAR`).

## Evidence

- Development Roadmap: [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md) (Section 3: Stage 1 Operational Setup & Master Data Foundation)
- Stage 0 Implementation Plan (Completed): [IS001- STAGE-0-DEV-IMPLEMENTATION-PLAN.md](file:///d:/Project.Private/elsasa-sis-v4/docs/issues/IS001-%20STAGE-0-DEV-IMPLEMENTATION-PLAN.md)
- System Architecture: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
- Backend Architecture: [backend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/backend-architecture.md)
- Frontend Architecture: [frontend-architecture.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/frontend-architecture.md)
- Screen Inventory: [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) (`SCR-OPR-CALENDAR`, `SCR-OPR-ANNOUNCEMENTS`, `SCR-STD-ADMISSIONS`, `SCR-STD-DIRECTORY`, `SCR-TCH-ASSIGNMENTS`)
- Use Case Discovery: [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (Use Cases `UC-ADM-02..16`, `UC-ADM-27`, `UC-PRC-01`, `UC-PRC-05`, `UC-PRC-07`, `UC-TCH-11`, `UC-HRM-05`)
- Operational Table Design: [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md)
- Student Table Design: [student-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/student-tables-design.md)
- Teaching Table Design: [teaching-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/teaching-tables-design.md)

## Notes

- Intake captured in accordance with the ELSASA-SIS v4 development roadmap for Stage 1.
- Solution-neutral intake: captures the requested system capabilities and user-facing operational outcomes without prescribing technical implementation specifics, architectural boundaries, or slice sequencing, which will be formally evaluated during Discovery and Feasibility Assessment by the Analyst.
