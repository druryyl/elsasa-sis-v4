using ElsasaSis.Modules.Dev.Features.Achievements;
using ElsasaSis.Modules.Dev.Features.Behaviour;
using ElsasaSis.Modules.Dev.Features.Discipline;
using ElsasaSis.Modules.Dev.Features.Extracurricular;

namespace ElsasaSis.Modules.Dev.Features.Dossier;

public sealed record StudentDossierDto(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    IReadOnlyList<BehaviourNoteDto> BehaviourNotes,
    IReadOnlyList<DisciplineRecordDto> DisciplineRecords,
    IReadOnlyList<ExtracurricularParticipationDto> Extracurriculars,
    IReadOnlyList<AchievementDto> Achievements,
    IReadOnlyList<DossierTimelineItemDto> Timeline);
