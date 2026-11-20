using ElsasaSis.Modules.Dev.Features.Achievements;
using ElsasaSis.Modules.Dev.Features.Behaviour;
using ElsasaSis.Modules.Dev.Features.Discipline;

namespace ElsasaSis.Modules.Dev.Features.Overview;

public sealed record DevelopmentOverviewDto(
    int TotalBehaviourNotes,
    int PositiveBehaviourNotes,
    int NegativeBehaviourNotes,
    int TotalDisciplineIncidents,
    int VerbalWarnings,
    int WrittenWarnings,
    int ParentMeetings,
    int Suspensions,
    int TotalAchievements,
    int ActiveExtracurricularActivities,
    int TotalExtracurricularParticipants,
    IReadOnlyList<BehaviourNoteDto> RecentBehaviourNotes,
    IReadOnlyList<DisciplineRecordDto> RecentDisciplineRecords,
    IReadOnlyList<AchievementDto> RecentAchievements);
