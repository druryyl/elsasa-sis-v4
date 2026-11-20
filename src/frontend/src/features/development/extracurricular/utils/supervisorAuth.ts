import type { ExtracurricularActivityDto } from '../api/extracurricular.api';

export function isActivitySupervisor(
  activity: Pick<ExtracurricularActivityDto, 'supervisorId'> | null | undefined,
  userId: string | null | undefined,
  teacherId: string | null | undefined
): boolean {
  if (!activity?.supervisorId) {
    return false;
  }

  return (
    (Boolean(teacherId) && activity.supervisorId === teacherId) ||
    (Boolean(userId) && activity.supervisorId === userId)
  );
}
