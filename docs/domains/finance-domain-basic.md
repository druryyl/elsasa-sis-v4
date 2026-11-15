# Finance Domain — Basic

## 1. Business Overview

The Finance domain (Basic) manages foundational financial recording for student-related fees in an Indonesian K-12 school (SD/SMP/SMA). It is the authoritative source for what fees apply, what each student owes, what payments have been received, and what balances remain outstanding.

The domain begins when the school defines its fee structure and generates charges for students. It continues through payment recording and balance tracking. Finance provides daily operational value by replacing manual spreadsheets and cash books with structured digital records.

### In Scope

- Defining charge types (fee categories and amounts)
- Generating and recording student charges
- Recording payment receipts
- Viewing receivables and outstanding balances

### Out of Scope

- Academic results, student identity, teaching assignments, operational scheduling, student development
- General accounting, payroll, procurement
- Installment schedules, payment confirmation workflows, external payment providers
- Aging analysis, overdue tracking, reminder systems, write-offs
- Financial policy versioning, charge rule engines, proration, currency/rounding rules

---

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Charge Type | A category of fee the school charges to students, with a defined name and standard amount for an academic year. |
| Academic Year | The school year to which a charge type applies (e.g., 2026/2027). |
| School Level | The school tier a charge type applies to: SD (elementary), SMP (junior secondary), SMA (senior secondary), or SMK (vocational senior secondary). |
| Grade | A timeless year-level within a school level to which a charge type may apply (e.g., Grade 7, Grade 10). |
| Study Group | An academic-year-specific student group within a grade (e.g., 7A, 10B) to which a charge type may apply. |
| Student Charge | A specific fee obligation assigned to an individual student, derived from a charge type. |
| Due Date | The date by which a student charge must be paid. |
| Charge Status | Whether a student charge is Unpaid or Paid. |
| Payment Receipt | A record that a payment has been received from a payer for a specific student charge. |
| Payer | The person who makes the payment, typically a parent or guardian. |
| Payment Method | The means by which payment is made: cash or bank transfer. |
| Receipt Number | A unique identifier assigned to a payment receipt for reference. |
| Receivable | The outstanding amount a student still owes to the school. |
| Outstanding Balance | The total unpaid amount across all of a student's charges. |
| Bulk Generation | The act of creating student charges for all students in a study group, grade, or school level at once from applicable charge types. |

---

## 3. Business Capabilities

| Capability | Description |
|---|---|
| Fee Structure Definition | Define the types and amounts of fees the school charges, scoped by academic year and applicable school level, grade, or study group. |
| Student Charge Generation | Create individual fee obligations for students, either in bulk for a study group/grade/school level or individually per student, based on defined charge types. |
| Payment Recording | Record payments received from payers against specific student charges, capturing payment date, amount, method, and payer identity. |
| Receipt Issuance | Produce a payment receipt as proof of payment for each recorded payment. |
| Receivables Inquiry | Determine each student's total charges, total payments, and outstanding balance. Produce balance summaries per student, outstanding lists per study group, and receipt lists per period. |

---

## 4. Actors & Roles

| Actor | Responsibilities |
|---|---|
| Finance Officer | Defines charge types. Generates student charges. Records payments. Issues receipts. Views receivables and produces reports. Has full operational authority over all finance activities. |
| School Administrator | Reviews receivables reports. May view outstanding balances and receipt lists for school-wide oversight. Does not record payments or define fees. |
| Payer (Parent/Guardian) | Makes payments to the school. Receives payment receipts. Not a direct operator of finance records. |

---

## 5. Domain Objects

### Charge Type

Represents a category of fee the school charges. Holds the fee name, standard amount, academic year, and the school level, grade, or study group to which it applies. Examples: monthly tuition (SPP) — Rp 500,000, enrollment fee — Rp 5,000,000, exam fee — Rp 200,000.

**Relationships:** A charge type produces one or many student charges.

### Student Charge

Represents a specific fee obligation for an individual student. Holds the reference to the originating charge type, the charged amount, due date, and charge status (Unpaid or Paid).

**Relationships:** Belongs to one student. Originates from one charge type. May be settled by one payment receipt.

### Payment Receipt

Represents a recorded payment for a student charge. Holds the receipt number, payment date, amount paid, payment method, payer name, and optional notes.

**Relationships:** Settles exactly one student charge. Belongs to one student.

### Receivable (View)

Represents the aggregated financial position of one student. Derived from the sum of all student charges and all payment receipts for that student, yielding the outstanding balance. This is a read-only view, not independently maintained.

**Relationships:** Derived from a student's charges and payment receipts.

---

## 6. Aggregates

### Charge Type (Aggregate Root)

- **Consistency Boundary:** A single charge type with its fee name, amount, academic year, and applicability (school level/grade/study group).
- **Owned Entities:** None. Charge type is a self-contained definition.
- **Business Responsibility:** Ensures a fee definition is complete and valid before it can be used to generate student charges.

### Student Charge (Aggregate Root)

- **Consistency Boundary:** A single student charge with its amount, due date, and status.
- **Owned Entities:** None.
- **Business Responsibility:** Maintains the correctness of one student's obligation for one fee. Ensures a charge cannot be marked paid without a corresponding payment receipt.

### Payment Receipt (Aggregate Root)

- **Consistency Boundary:** A single payment receipt with its amount, method, date, payer, and link to the student charge it settles.
- **Owned Entities:** None.
- **Business Responsibility:** Ensures a payment is fully recorded and that a receipt number is assigned. Ensures the payment amount matches the charge amount.

---

## 7. Business Rules

| ID | Rule |
|---|---|
| BR-FIN-001 | A charge type must have a fee name, a positive amount, and an assigned academic year before it can be used to generate student charges. |
| BR-FIN-002 | A charge type must specify at least one applicable school level, grade, or study group. |
| BR-FIN-003 | A student charge amount must equal the standard amount defined in its originating charge type at the time of generation. |
| BR-FIN-004 | A student charge must have a due date that falls within the academic year of its charge type. |
| BR-FIN-005 | A student may have only one charge of the same charge type for the same academic year. |
| BR-FIN-006 | A payment receipt must record exactly one student charge. One payment settles one charge. |
| BR-FIN-007 | The payment amount must equal the full student charge amount. Partial payments are not permitted. |
| BR-FIN-008 | A student charge status changes to Paid only when a corresponding payment receipt exists. |
| BR-FIN-009 | A payment receipt must record a valid payment method: cash or bank transfer. |
| BR-FIN-010 | A payment receipt must identify the payer by name. |
| BR-FIN-011 | Each payment receipt must be assigned a unique receipt number. |
| BR-FIN-012 | A student's outstanding balance equals the sum of all unpaid student charge amounts. |

---

## 8. State Machines & Lifecycles

### Charge Type Lifecycle

```
[Draft] ---(all required fields provided)---> [Active]
```

| State | Meaning |
|---|---|
| Draft | Charge type is being defined. Cannot be used for charge generation. |
| Active | Charge type is complete and available for generating student charges. |

### Student Charge Lifecycle

```
[Unpaid] ---(payment received)---> [Paid]
```

| State | Meaning |
|---|---|
| Unpaid | The student owes this charge. No payment has been recorded. |
| Paid | A payment receipt has been recorded for this charge. The obligation is settled. |

---

## 9. Domain Events

| Event | Description | Triggered When |
|---|---|---|
| Charge Type Activated | A new charge type has been defined and is available for use. | A charge type transitions from Draft to Active. |
| Student Charge Created | A fee obligation has been assigned to a student. | A student charge is generated from a charge type, individually or in bulk. |
| Payment Received | A payment has been recorded and a student charge has been settled. | A payment receipt is recorded against an unpaid student charge. |
| Receipt Issued | A payment receipt has been produced as proof of payment. | A payment receipt is finalized with a receipt number. |

---

## 10. Business Workflows

### Define Fee Structure

The Finance Officer creates a charge type by specifying the fee name, amount, academic year, and applicable school level, grade, or study group. Once all required information is provided, the charge type becomes active and available for generating student charges.

### Generate Student Charges

The Finance Officer selects an active charge type and a target study group, grade, or school level. The school generates a student charge for each applicable student, each with the standard amount and an assigned due date. Charges may also be generated individually for a single student.

### Record Payment

When a payer makes a payment, the Finance Officer identifies the student and the unpaid charge. The Finance Officer records the payment date, amount, payment method, and payer name. The payment amount must equal the charge amount in full. Upon recording, the student charge is marked as paid and a payment receipt with a unique receipt number is produced.

### View Receivables

The Finance Officer or School Administrator views the financial position of students. The school provides:

- **Balance per student** — total charges, total payments, and outstanding balance for one student.
- **Outstanding list per study group** — all students in a study group who have unpaid charges.
- **Receipts per period** — all payment receipts recorded within a specified date range.
