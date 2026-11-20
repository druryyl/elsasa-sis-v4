using ElsasaSis.Modules.Fin.Domain.Model;

namespace ElsasaSis.Modules.Fin.Features.Payments;

public sealed record PaymentReceiptDto(
    Guid Id,
    string ReceiptNumber,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? StudyGroupId,
    string? StudyGroupName,
    DateOnly PaymentDate,
    string PaymentMethod,
    string? BankReference,
    decimal TotalAmount,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    Guid ReceivedById,
    string ReceivedByName,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<PaymentAllocationDto> Allocations)
{
    public static PaymentReceiptDto FromDomain(PaymentReceipt receipt) =>
        new(
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.AcademicYearId,
            receipt.AcademicYearName,
            receipt.StudentId,
            receipt.StudentName,
            receipt.StudentNis,
            receipt.StudyGroupId,
            receipt.StudyGroupName,
            receipt.PaymentDate,
            receipt.PaymentMethod,
            receipt.BankReference,
            receipt.TotalAmount,
            receipt.AllocatedAmount,
            receipt.UnallocatedAmount,
            receipt.ReceivedById,
            receipt.ReceivedByName,
            receipt.Notes,
            receipt.CreatedAt,
            receipt.Allocations.Select(PaymentAllocationDto.FromDomain).ToList());
}
