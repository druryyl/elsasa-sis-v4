namespace ElsasaSis.Contracts.Teaching;

public sealed record TeacherSummaryDto(
    Guid Id,
    string FullName,
    string Status);

public interface ITeachingContract
{
    Task<TeacherSummaryDto?> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
