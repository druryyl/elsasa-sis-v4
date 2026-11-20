using ElsasaSis.Contracts.Operational;
using ElsasaSis.Contracts.Student;
using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Fin.Features.Payments.RecordPaymentReceipt;

public sealed class RecordPaymentReceiptHandler(
    IPaymentReceiptRepository paymentReceiptRepository,
    IReceiptSequenceGenerator sequenceGenerator,
    IStudentChargeRepository studentChargeRepository,
    IOperationalContract operationalContract,
    IStudentContract studentContract,
    ICurrentUser currentUser,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<PaymentReceiptDto>> HandleAsync(
        RecordPaymentReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        var receivedById = currentUser.UserId != Guid.Empty
            ? currentUser.UserId
            : (request.ReceivedById ?? Guid.Empty);

        var receivedByName = !string.IsNullOrWhiteSpace(currentUser.FullName)
            ? currentUser.FullName
            : (!string.IsNullOrWhiteSpace(currentUser.Username)
                ? currentUser.Username
                : (!string.IsNullOrWhiteSpace(request.ReceivedByName) ? request.ReceivedByName : string.Empty));

        if (receivedById == Guid.Empty || string.IsNullOrWhiteSpace(receivedByName))
        {
            return Result<PaymentReceiptDto>.Failure(
                DomainError.Validation("Fin.PaymentReceipt.ReceivedByRequired", "Received-by cashier details are required.", "receivedById"));
        }

        var academicYearName = request.AcademicYearName;
        if (string.IsNullOrWhiteSpace(academicYearName))
        {
            var academicYear = await operationalContract.GetAcademicYearByIdAsync(request.AcademicYearId, cancellationToken);
            if (academicYear is not null)
            {
                academicYearName = academicYear.Name;
            }
            else
            {
                var activeYear = await operationalContract.GetActiveAcademicYearAsync(cancellationToken);
                if (activeYear is not null && activeYear.Id == request.AcademicYearId)
                {
                    academicYearName = activeYear.Name;
                }
                else
                {
                    return Result<PaymentReceiptDto>.Failure(
                        DomainError.NotFound("Fin.AcademicYear.NotFound", $"Academic year with ID '{request.AcademicYearId}' was not found."));
                }
            }
        }

        var allocationRequests = request.Allocations ?? [];
        var chargesToUpdate = new List<StudentCharge>();
        var studentName = request.StudentName;
        var studentNis = request.StudentNis;
        var studyGroupId = request.StudyGroupId;
        var studyGroupName = request.StudyGroupName;

        if (allocationRequests.Count > 0)
        {
            var chargeIds = allocationRequests.Select(a => a.ChargeId).Distinct().ToList();
            var charges = await studentChargeRepository.GetByIdsAsync(chargeIds, cancellationToken);
            var chargesById = charges.ToDictionary(c => c.Id);

            foreach (var allocReq in allocationRequests)
            {
                if (!chargesById.TryGetValue(allocReq.ChargeId, out var charge))
                {
                    return Result<PaymentReceiptDto>.Failure(
                        FinErrors.StudentChargeNotFound(allocReq.ChargeId));
                }

                if (charge.StudentId != request.StudentId)
                {
                    return Result<PaymentReceiptDto>.Failure(
                        DomainError.Conflict(
                            "Fin.StudentCharge.StudentMismatch",
                            $"Charge '{charge.Id}' belongs to student '{charge.StudentId}', not '{request.StudentId}'."));
                }

                if (charge.Status == ChargeStatuses.Cancelled)
                {
                    return Result<PaymentReceiptDto>.Failure(
                        FinErrors.StudentChargeAlreadyCancelled(charge.Id));
                }

                if (allocReq.Amount > charge.OutstandingAmount)
                {
                    return Result<PaymentReceiptDto>.Failure(
                        FinErrors.PaymentAmountExceedsOutstanding(allocReq.Amount, charge.OutstandingAmount));
                }

                studentName ??= charge.StudentName;
                studentNis ??= charge.StudentNis;
                studyGroupId ??= charge.StudyGroupId;
                studyGroupName ??= charge.StudyGroupName;

                var applyPaymentResult = charge.ApplyPayment(allocReq.Amount);
                if (applyPaymentResult.IsFailure)
                {
                    return Result<PaymentReceiptDto>.Failure(applyPaymentResult.Error);
                }

                if (!chargesToUpdate.Contains(charge))
                {
                    chargesToUpdate.Add(charge);
                }
            }
        }

        if (string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(studentNis))
        {
            var studentCharges = await studentChargeRepository.GetByStudentIdAsync(request.StudentId, cancellationToken: cancellationToken);
            var firstCharge = studentCharges.FirstOrDefault();
            if (firstCharge is not null)
            {
                studentName = firstCharge.StudentName;
                studentNis = firstCharge.StudentNis;
                studyGroupId ??= firstCharge.StudyGroupId;
                studyGroupName ??= firstCharge.StudyGroupName;
            }
            else
            {
                var scopeStudents = await studentContract.GetActiveStudentsByScopeAsync(null, null, null, cancellationToken);
                var student = scopeStudents.FirstOrDefault(s => s.StudentId == request.StudentId);
                if (student is not null)
                {
                    studentName = student.StudentName;
                    studentNis = student.StudentNis;
                    studyGroupId ??= student.StudyGroupId;
                    studyGroupName ??= student.StudyGroupName;
                }
                else
                {
                    return Result<PaymentReceiptDto>.Failure(FinErrors.StudentNotFound(request.StudentId));
                }
            }
        }

        var month = request.PaymentDate.ToString("yyyy-MM");
        var receiptNumber = await sequenceGenerator.GenerateNextReceiptNumberAsync(month, cancellationToken);

        var receiptResult = PaymentReceipt.Create(
            receiptNumber,
            request.AcademicYearId,
            academicYearName,
            request.StudentId,
            studentName,
            studentNis,
            studyGroupId,
            studyGroupName,
            request.PaymentDate,
            request.PaymentMethod,
            request.TotalAmount,
            receivedById,
            receivedByName,
            request.BankReference,
            request.Notes);

        if (receiptResult.IsFailure)
        {
            return Result<PaymentReceiptDto>.Failure(receiptResult.Error);
        }

        var receipt = receiptResult.Value!;

        foreach (var allocReq in allocationRequests)
        {
            var charge = chargesToUpdate.First(c => c.Id == allocReq.ChargeId);
            var allocResult = receipt.AllocateCharge(
                charge.Id,
                charge.ChargeTypeName,
                charge.BillingMonth,
                charge.StudentId,
                charge.StudentName,
                charge.StudentNis,
                allocReq.Amount,
                request.PaymentDate);

            if (allocResult.IsFailure)
            {
                return Result<PaymentReceiptDto>.Failure(allocResult.Error);
            }
        }

        await paymentReceiptRepository.SavePaymentReceiptTransactionAsync(receipt, chargesToUpdate, cancellationToken);

        if (receipt.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(receipt.DomainEvents, cancellationToken);
            receipt.ClearDomainEvents();
        }

        return Result<PaymentReceiptDto>.Success(PaymentReceiptDto.FromDomain(receipt));
    }
}
