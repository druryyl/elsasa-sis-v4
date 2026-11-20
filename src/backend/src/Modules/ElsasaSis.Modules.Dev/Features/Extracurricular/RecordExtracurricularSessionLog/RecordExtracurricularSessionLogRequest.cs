namespace ElsasaSis.Modules.Dev.Features.Extracurricular.RecordExtracurricularSessionLog;

public sealed record RecordExtracurricularSessionLogRequest(
    DateOnly SessionDate,
    string Log);
