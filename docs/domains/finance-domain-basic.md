# Finance Domain — Basic

## 1. Business Overview

The Finance domain (Basic) manages foundational financial recording for student-related fees in an Indonesian K-12 school (SD/SMP/SMA/SMK). It is the authoritative source for what fee types apply, what charges each student owes across billing cycles, what student-specific waivers or discounts apply, what payments and advance guardian deposits have been received, how payments are allocated across obligations, and what balances or deposits remain.

The domain begins when the school defines its fee structure (including recurrence frequencies) and optional standing student discounts. It continues through individual or bulk charge generation per billing cycle, payment and guardian deposit recording, allocation of payments/deposits to student charges, and receivable and deposit balance reporting. Finance provides daily operational value by replacing manual spreadsheets and cash books with structured digital records that match real-world school billing workflows.

### In Scope

- Defining charge types with recurrence frequencies (`Monthly`, `Semester`, `Yearly`, `One-Time`) and standard unit amounts
- Defining standing student-specific discounts/waivers per charge type and applying inline charge discounts
- Generating and recording multiple student charges per billing cycle (fixed cohort fees and variable-quantity items such as PACE booklets)
- Recording payment receipts (including lump-sum and multi-month guardian deposits)
- Allocating payments and remaining deposit balances across one or more student charges (full or partial settlement)
- Viewing receivables, monthly student billing statements, outstanding balances, and remaining guardian deposit balances

### Out of Scope

- Academic results, student identity, teaching assignments, operational scheduling, student development
- General accounting, payroll, procurement
- Formal multi-step approval workflows, external payment gateway integrations
- Aging buckets, automated dunning/reminder engines, write-offs
- Complex benefit stacking engines, cross-student credit transfers, formal financial clearance workflows

---

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Charge Type | A category of fee the school charges to students, with a defined name, standard unit amount, recurrence frequency, and academic year. |
| Charge Frequency | The recurrence cadence of a charge type: `Monthly`, `Semester`, `Yearly`, or `One-Time`. |
| Academic Year | The school year to which a charge type or discount applies (e.g., 2026/2027). |
| Operational Period | A term or semester within an academic year to which a `Semester` charge applies (e.g., Semester 1, Semester 2). |
| Billing Month | The calendar month (`YYYY-MM`, e.g., `2026-07`) for which a charge is billed, enabling monthly statements across multiple charges. |
| School Level | The school tier a charge type applies to: SD (elementary), SMP (junior secondary), SMA (senior secondary), or SMK (vocational senior secondary). |
| Grade | A timeless year-level within a school level to which a charge type may apply (e.g., Grade 7, Grade 10). |
| Study Group | An academic-year-specific student group within a grade (e.g., 7A, 10B) to which a charge type may apply. |
| Student Discount | A standing waiver or discount rule assigned to an individual student for a specific charge type in an academic year (fixed amount or percentage), automatically applied during charge generation. |
| Student Charge | A specific fee obligation assigned to an individual student, derived from a charge type, carrying unit amount, quantity, gross amount, discount amount, net amount, paid amount, and outstanding amount. |
| Gross Amount | The pre-discount charge total computed as `Unit Amount × Quantity`. |
| Discount Amount | The monetary reduction applied to a student charge from a standing student discount or inline manual waiver. |
| Net Amount | The final payable obligation amount (`Gross Amount - Discount Amount`). |
| Paid Amount | The cumulative amount allocated to a student charge from one or more payment receipts. |
| Outstanding Amount | The remaining unpaid balance on a single student charge (`Net Amount - Paid Amount`). |
| Due Date | The date by which a student charge must be paid. |
| Charge Status | Whether a student charge is `Unpaid`, `Partially Paid`, or `Paid`. |
| Payment Receipt | A record of money received from a payer for a student, tracking total received amount, allocated amount, and remaining unallocated deposit amount. |
| Payment Allocation | A link applying a portion or all of a payment receipt's amount toward settling a specific student charge. |
| Deposit Balance | The sum of `Unallocated Amount` across a student's payment receipts that remains available to allocate toward current or future monthly charges. |
| Payer | The person who makes the payment, typically a parent or guardian. |
| Payment Method | The means by which payment is made: `Cash` or `Bank Transfer`. |
| Receipt Number | A unique identifier assigned to a payment receipt for reference. |
| Receivable | The financial position of a student across charges, allocations, outstanding balance, and remaining deposit balance. |
| Outstanding Balance | The total unpaid amount (`Outstanding Amount`) across all of a student's unpaid and partially paid charges. |
| Bulk Generation | The act of creating student charges for all students in a study group, grade, or school level at once from an active charge type, automatically applying standing student discounts. |

---

## 3. Business Capabilities

| Capability | Description |
|---|---|
| Fee Structure Definition | Define charge types with standard unit amounts and recurrence frequencies (`Monthly`, `Semester`, `Yearly`, `One-Time`), scoped by academic year and applicable school level, grade, or study group. |
| Student Discount & Waiver Management | Maintain standing student-specific discounts/waivers (`Fixed Amount` or `Percentage`) per charge type and academic year, plus inline discounts on individual charges. |
| Student Charge Generation | Create individual or bulk student charges for a billing month, operational period, or academic year, supporting multiple charge types per billing cycle and variable-quantity items (e.g., PACE booklets). |
| Payment & Deposit Recording | Record cash or bank transfer payments received for a student—whether paying current charges immediately or recording a lump-sum multi-month guardian deposit. |
| Payment & Deposit Allocation | Allocate funds from a new payment receipt or existing unallocated deposit balance across one or more student charges (supporting full or partial settlement). |
| Receipt Issuance | Produce a payment receipt with a unique receipt number as proof of payment for every cash/bank transfer received, showing current allocations and remaining deposit. |
| Receivables & Deposit Inquiry | Determine each student's monthly billing statement, total net charges, total paid, total outstanding balance, and remaining deposit balance, plus outstanding lists per study group and receipt lists per period. |

---

## 4. Actors & Roles

| Actor | Responsibilities |
|---|---|
| Finance Officer | Defines charge types and standing student discounts. Generates student charges (bulk or individual). Records payments and guardian deposits. Allocates payments and deposits across student charges. Issues receipts. Views receivables, monthly bills, and deposit reports. Has full operational authority over all finance activities. |
| School Administrator | Reviews receivables, study group outstanding reports, deposit summaries, and receipt lists for school-wide oversight. Does not record payments, define fees, or allocate funds. |
| Payer (Parent/Guardian) | Makes payments or advance multi-month deposits to the school. Receives payment receipts and monthly billing summaries. Not a direct operator of finance records. |

---

## 5. Domain Objects

### Charge Type

Represents a category of fee the school charges. Holds the fee name, standard unit amount, charge frequency (`Monthly`, `Semester`, `Yearly`, `One-Time`), academic year, applicability (`School Level`, `Grade`, or `Study Group`), and status (`Draft` or `Active`). Examples: monthly tuition (`SPP`) — Rp 500,000 (`Monthly`), building fee (`Uang Gedung`) — Rp 5,000,000 (`Yearly` or `One-Time`), workbook/module fee (`Materi PACE`) — Rp 150,000 (`One-Time`).

**Relationships:** A charge type produces zero or many student charges and may be referenced by zero or many student discounts.

### Student Discount

Represents a standing waiver or discount rule granted to an individual student for a specific charge type within an academic year. Holds the discount type (`Fixed Amount` or `Percentage`), discount value, reason, and active flag. When charges are generated for that student and charge type, the discount is applied automatically.

**Relationships:** Belongs to one student and one charge type in one academic year.

### Student Charge

Represents a specific fee obligation for an individual student. Holds the reference to the originating charge type, charge frequency, academic year, optional operational period, optional billing month (`YYYY-MM`), optional item description, `unit_amount`, `quantity` (default 1), `gross_amount`, `discount_amount`, `discount_reason`, `net_amount`, `paid_amount`, `outstanding_amount`, `due_date`, and status (`Unpaid`, `Partially Paid`, or `Paid`).

**Relationships:** Belongs to one student. Originates from one charge type. May be settled by zero or many payment allocations.

### Payment Receipt

Represents money received from a payer for a student. Holds the receipt number, payment date, `total_amount`, `allocated_amount`, `unallocated_amount`, `payment_method` (`Cash` or `Bank Transfer`), `payer_name`, and optional `notes`. Unallocated money represents the student's available guardian deposit balance.

**Relationships:** Belongs to one student. May fund zero or many payment allocations across one or more student charges.

### Payment Allocation

Represents the application of a specific amount from a payment receipt toward settling a specific student charge (either immediately upon receipt or later from an unallocated guardian deposit). Holds the allocated amount, allocation date, and optional notes.

**Relationships:** Links one payment receipt to one student charge of the same student.

### Receivable & Deposit Position (View)

Represents the aggregated financial position of one student. Derived from the sum of `net_amount`, `paid_amount`, and `outstanding_amount` across all student charges and the sum of `unallocated_amount` across all payment receipts for that student. This is a read-only view, not independently maintained.

**Relationships:** Derived from a student's charges, payment receipts, and payment allocations.

---

## 6. Aggregates

### Charge Type (Aggregate Root)

- **Consistency Boundary:** A single charge type with its fee name, standard unit amount, frequency, academic year, applicability (school level/grade/study group), and status.
- **Owned Entities:** None. Charge type is a self-contained definition.
- **Business Responsibility:** Ensures a fee definition is complete, valid, and has a defined recurrence frequency before it can be activated and used to generate student charges.

### Student Discount (Aggregate Root)

- **Consistency Boundary:** A single standing discount rule linking one student, one charge type, and one academic year with its discount type, value, reason, and active flag.
- **Owned Entities:** None.
- **Business Responsibility:** Guarantees valid discount definitions (positive fixed amount or `0–100%` percentage, mandatory reason) and prevents duplicate standing rules for the same student, charge type, and academic year.

### Student Charge (Aggregate Root)

- **Consistency Boundary:** A single student obligation with its period context (`academic_year`, optional `operational_period`, optional `billing_month`), `unit_amount`, `quantity`, `gross_amount`, `discount_amount`, `net_amount`, `paid_amount`, `outstanding_amount`, `due_date`, and `status`.
- **Owned Entities:** None.
- **Business Responsibility:** Maintains arithmetic invariants (`gross_amount = unit_amount × quantity`, `net_amount = gross_amount - discount_amount`, `outstanding_amount = net_amount - paid_amount`), frequency-based uniqueness per period, and `Unpaid` / `Partially Paid` / `Paid` state accuracy.

### Payment Receipt (Aggregate Root)

- **Consistency Boundary:** A single payment receipt (`total_amount`, `allocated_amount`, `unallocated_amount`, `payment_method`, `payer_name`, `receipt_number`) together with its **Payment Allocation** entries (`student_charge`, `allocated_amount`, `allocated_date`, `notes`).
- **Owned Entities:** **Payment Allocation**.
- **Business Responsibility:** Ensures every incoming payment or deposit is assigned a unique receipt number, prevents over-allocation beyond `total_amount`, keeps `allocated_amount + unallocated_amount == total_amount`, and coordinates with target `Student Charge` records when allocating funds.

---

## 7. Business Rules

| ID | Rule |
|---|---|
| BR-FIN-001 | A charge type must have a fee name, a positive standard amount, a valid charge frequency (`Monthly`, `Semester`, `Yearly`, or `One-Time`), and an assigned academic year before it can be used to generate student charges. |
| BR-FIN-002 | A charge type must specify at least one applicable school level, grade, or study group. |
| BR-FIN-003 | A student charge's unit amount must equal the standard amount of its originating charge type at the time of generation, its quantity must be at least `1`, and its `gross_amount` must equal `unit_amount × quantity`. |
| BR-FIN-004 | A student charge must have a due date that falls within the academic year of its charge type. |
| BR-FIN-005 | Charge uniqueness per student follows the charge type's frequency: at most one `Yearly` charge per academic year, at most one `Semester` charge per operational period, and at most one `Monthly` charge per billing month (`YYYY-MM`). `One-Time` charges (such as PACE booklet orders) may be generated multiple times as needed. |
| BR-FIN-006 | A student may have at most one standing student discount per charge type per academic year, with a valid discount type (`Fixed Amount` or `Percentage`, where percentage is between `0` and `100`) and a non-empty reason. |
| BR-FIN-007 | A student charge's `discount_amount` must be between `0` and its `gross_amount`, its `net_amount` must equal `gross_amount - discount_amount`, and a discount cannot reduce `net_amount` below the already `paid_amount`. |
| BR-FIN-008 | A payment receipt must record a positive `total_amount`, a valid payment method (`Cash` or `Bank Transfer`), a non-empty payer name, and a unique receipt number. |
| BR-FIN-009 | On every payment receipt, `allocated_amount` must equal the sum of its payment allocations, `unallocated_amount` must equal `total_amount - allocated_amount`, and `allocated_amount` must never exceed `total_amount`. |
| BR-FIN-010 | Each payment allocation must link a payment receipt to a student charge belonging to the same student for a positive `allocated_amount` that does not exceed the receipt's available `unallocated_amount` or the charge's remaining `outstanding_amount`. |
| BR-FIN-011 | On every student charge, `paid_amount` must equal the sum of its payment allocations and `outstanding_amount` must equal `net_amount - paid_amount`. Charge status is `Unpaid` when `paid_amount = 0` and `net_amount > 0`, `Partially Paid` when `0 < paid_amount < net_amount`, and `Paid` when `outstanding_amount = 0` (including 100% waived charges where `net_amount = 0`). |
| BR-FIN-012 | A student's total outstanding balance equals the sum of `outstanding_amount` across all unpaid and partially paid charges, and a student's remaining deposit balance equals the sum of `unallocated_amount` across all of the student's payment receipts. |

---

## 8. State Machines & Lifecycles

### Charge Type Lifecycle

```
[Draft] ---(all required fields & frequency provided)---> [Active]
```

| State | Meaning |
|---|---|
| Draft | Charge type is being defined. Cannot be used for charge generation. |
| Active | Charge type is complete and available for generating student charges. |

### Student Charge Lifecycle

```
[Unpaid] ---(partial allocation: 0 < paid_amount < net_amount)---> [Partially Paid]
[Unpaid] ---(full allocation or 100% waiver: outstanding_amount = 0)---> [Paid]
[Partially Paid] ---(remaining outstanding_amount = 0)---> [Paid]
```

| State | Meaning |
|---|---|
| Unpaid | The student owes the full `net_amount`. No payment allocation has been recorded (`paid_amount = 0`, `net_amount > 0`). |
| Partially Paid | One or more payment allocations cover part of `net_amount`, leaving `outstanding_amount > 0`. |
| Paid | Payment allocations (and/or a 100% waiver) cover `net_amount` in full (`outstanding_amount = 0`). |

### Payment Receipt Allocation Lifecycle (Derived)

```
[Unallocated Deposit] ---(0 < allocated_amount < total_amount)---> [Partially Allocated]
[Unallocated Deposit] ---(allocated_amount = total_amount)-------> [Fully Allocated]
[Partially Allocated] ---(remaining unallocated_amount = 0)------> [Fully Allocated]
```

| Derived State | Meaning |
|---|---|
| Unallocated Deposit | Full `total_amount` remains in `unallocated_amount` (`allocated_amount = 0`), held as an advance guardian deposit. |
| Partially Allocated | Part of the receipt settled one or more charges; the remaining `unallocated_amount > 0` stays as guardian deposit balance. |
| Fully Allocated | The entire receipt amount has been allocated to student charges (`unallocated_amount = 0`). |

---

## 9. Domain Events

| Event | Description | Triggered When |
|---|---|---|
| Charge Type Activated | A new charge type with its frequency and applicability is available for use. | A charge type transitions from Draft to Active. |
| Student Discount Configured | A standing waiver or discount has been created or updated for a student. | A student discount rule is created, updated, or toggled. |
| Student Charge Created | A fee obligation (with calculated gross, discount, and net amounts) has been assigned to a student. | A student charge is generated individually or in bulk for a period. |
| Student Charge Discount Applied | An inline waiver or discount adjusted an unpaid or partially paid charge. | A charge's discount amount/reason is updated. |
| Payment Receipt Recorded | Money (current payment or advance deposit) has been received from a payer. | A payment receipt is recorded and assigned a unique receipt number. |
| Payment Allocated | Funds from a payment receipt or deposit have been applied to a student charge. | A payment allocation is recorded, updating the receipt's unallocated amount and the charge's paid/outstanding amounts and status. |
| Student Charge Settled | A student charge's outstanding amount has reached zero. | Payment allocation(s) or a 100% waiver brings `outstanding_amount` to `0`. |

---

## 10. Business Workflows

### Define Fee Structure

The Finance Officer creates a charge type by specifying the fee name, standard unit amount, recurrence frequency (`Monthly`, `Semester`, `Yearly`, or `One-Time`), academic year, and applicable school level, grade, or study group. Once all required fields are provided, the charge type becomes active and available for generating student charges.

### Define Standing Student Discount

When a student qualifies for a recurring waiver or discount (such as a sibling discount on `SPP` or a scholarship reduction), the Finance Officer creates a `Student Discount` linking the student, charge type, and academic year with either a fixed amount or percentage reduction and a reason.

### Generate Student Charges (Bulk & Individual / Variable Quantity)

The Finance Officer selects an active charge type and its target period (`billing_month` for `Monthly`, `operational_period` for `Semester`, or `academic_year` for `Yearly`/`One-Time`), a target study group, grade, or school level (or an individual student), and a due date. For variable items like `Materi PACE`, the Finance Officer specifies the quantity (default `1`) and optional item description (e.g., `"5x PACE Math & English"`). During generation, the system automatically checks for an active `Student Discount` for each student and charge type, computes `gross_amount`, `discount_amount`, and `net_amount`, and sets status to `Unpaid` (or `Paid` immediately if 100% waived). Students who already have a charge for that same non-`One-Time` period are skipped to prevent duplicates.

### Record Payment or Guardian Deposit

When a payer transfers money or pays cash—either to pay this month's charges (`SPP` + `Materi PACE` + `Uang Gedung`), make a partial payment toward a large fee, or send an advance deposit for the next `n` months—the Finance Officer records a `Payment Receipt` with the payment date, total amount received, payment method, and payer name. The Finance Officer may allocate all or part of the receipt immediately to one or more unpaid/partially paid charges, leaving any remainder in `unallocated_amount` as the student's guardian deposit balance. A unique receipt number is issued.

### Allocate Existing Deposit to Monthly Charges

When new monthly charges (such as next month's `SPP`) are generated for a student who has a remaining `Deposit Balance` (`unallocated_amount > 0`), the Finance Officer allocates available deposit funds (manually or in FIFO receipt order) to settle or partially settle the student's charges. Each allocation updates `payment_allocations`, reduces the source receipt's `unallocated_amount`, increases the target charge's `paid_amount`, and transitions the charge to `Partially Paid` or `Paid`.

### View Receivables & Deposit Balances

The Finance Officer or School Administrator views the financial position of students. The school provides:

- **Monthly student billing statement** — all charges grouped by `billing_month` (e.g., `SPP` + `Materi PACE` + `Uang Gedung`) with gross, discount, net, paid, and outstanding amounts.
- **Balance & deposit summary per student** — total net charges, total paid, total outstanding balance, and remaining guardian deposit balance.
- **Outstanding & deposit list per study group** — all students in a study group who have unpaid/partially paid charges (and their available deposit balances).
- **Receipts per period** — all payment receipts recorded within a specified date range, showing total received, allocated, and unallocated deposit amounts.
