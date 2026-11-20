export const pastoralKeys = {
  all: ['development', 'pastoral'] as const,
  dossiers: () => [...pastoralKeys.all, 'dossier'] as const,
  dossier: (studentId: string) => [...pastoralKeys.dossiers(), studentId] as const,
  overview: () => [...pastoralKeys.all, 'overview'] as const
};
