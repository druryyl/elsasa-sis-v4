using ElsasaSis.Modules.Dev.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Dev.Features.Achievements;

public sealed class RecordAchievementHandler(
    IAchievementRepository repository,
    ICurrentUser currentUser,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<AchievementDto>> HandleAsync(
        RecordAchievementRequest request,
        CancellationToken cancellationToken = default)
    {
        var recorderId = currentUser.UserId != Guid.Empty
            ? currentUser.UserId
            : (request.RecorderId ?? Guid.Empty);

        var recorderName = !string.IsNullOrWhiteSpace(currentUser.FullName)
            ? currentUser.FullName
            : (!string.IsNullOrWhiteSpace(currentUser.Username)
                ? currentUser.Username
                : (!string.IsNullOrWhiteSpace(request.RecorderName) ? request.RecorderName : string.Empty));

        var achievementResult = Achievement.Create(
            request.StudentId,
            request.StudentName,
            request.StudentNis,
            request.AcademicYearId,
            request.AcademicYearName,
            request.AchievementType,
            request.EventName,
            request.RecognitionLevel,
            request.AchievementDate,
            request.Description,
            recorderId,
            recorderName);

        if (achievementResult.IsFailure)
        {
            return Result<AchievementDto>.Failure(achievementResult.Error);
        }

        var achievement = achievementResult.Value!;
        await repository.InsertAsync(achievement, cancellationToken);

        if (achievement.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(achievement.DomainEvents, cancellationToken);
            achievement.ClearDomainEvents();
        }

        return Result<AchievementDto>.Success(new AchievementDto(
            achievement.Id,
            achievement.StudentId,
            achievement.StudentName,
            achievement.StudentNis,
            achievement.AcademicYearId,
            achievement.AcademicYearName,
            achievement.AchievementType,
            achievement.EventName,
            achievement.RecognitionLevel,
            achievement.AchievementDate,
            achievement.Description,
            achievement.RecorderId,
            achievement.RecorderName,
            achievement.CreatedAt));
    }
}
