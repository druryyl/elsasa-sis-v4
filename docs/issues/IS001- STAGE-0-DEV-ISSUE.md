# ISSUE

## Metadata

ID: CR-STAGE-0
Type: CHANGE-REQUEST
Status: OPEN
Title: Implement Stage 0: Walking Skeleton & Reference Slice (Authentication & Academic Year Management)

## Source

Reported By: User
Reported Date: 2026-11-18

## Description

Implement Stage 0 ("Walking Skeleton & Reference Slice") of the ELSASA-SIS v4 development roadmap.

This request establishes the foundational application scaffold and infrastructure—including the monorepo solution structure, core authentication pipeline, database schema initialization and migrations, architectural boundary enforcement, and frontend application shell—and delivers the first operational vertical slice enabling school administrators to manage academic years and operational periods (UC-ADM-01) across two foundational user interfaces (SCR-AUTH-LOGIN and SCR-OPR-CALENDAR Part 1).

## Desired Outcome

1. School administrative personnel can authenticate via a dedicated login screen (`SCR-AUTH-LOGIN`) using administrative credentials and establish an authenticated session with appropriate role claims.
2. School administrators can access the operational calendar management screen (`SCR-OPR-CALENDAR` Part 1) to define, inspect, update, and activate academic years and their operational semesters.
3. The foundational technical baseline (database migrations, backend modular architecture, API pipeline with standard error handling, and frontend infrastructure) is proven end-to-end to serve as the reference implementation for subsequent roadmap stages.

## Current Situation

The project is currently in a pre-implementation greenfield state. Foundational specifications, architectural blueprints, screen inventories, use case definitions, and the development roadmap (`docs/elsasa-sis-v4-development-roadmap.md`) have been approved, but no runnable backend or frontend applications, database migrations, or active features are currently present in the codebase (`src/backend` and `src/frontend` are unpopulated).

## Evidence

- Development Roadmap: [elsasa-sis-v4-development-roadmap.md](file:///d:/Project.Private/elsasa-sis-v4/docs/elsasa-sis-v4-development-roadmap.md) (Section 3: Stage 0 Walking Skeleton & Reference Slice)
- System Architecture: [ELSASA-SIS-ARCHITECTURE.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/ELSASA-SIS-ARCHITECTURE.md)
- Use Case Discovery: [use-case-discovery-basic.md](file:///d:/Project.Private/elsasa-sis-v4/docs/use-case-discovery-basic.md) (`UC-ADM-01`)
- Screen Inventory: [screen-inventory.md](file:///d:/Project.Private/elsasa-sis-v4/docs/screen-inventory.md) (`SCR-AUTH-LOGIN`, `SCR-OPR-CALENDAR`)
- Operational Table Design: [operational-tables-design.md](file:///d:/Project.Private/elsasa-sis-v4/docs/architecture/operational-tables-design.md) (`opr.academic_years`, `opr.academic_semesters`)

## Notes

- Intake captured according to Stage 0 specifications of the ELSASA-SIS v4 roadmap.
- Solution-neutral intake: captures the requested system change and operational outcomes without prescribing detailed implementation or execution mechanics, which will be addressed in downstream SDLC phases.
