import { ApiError } from '@/shared/types/common';

export function mapApiValidationErrors(
  fieldErrors?: Record<string, string[]> | null
): Record<string, string> {
  if (!fieldErrors) {
    return {};
  }

  const mapped: Record<string, string> = {};
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const first = messages?.[0];
    if (first) {
      const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
      mapped[camelKey] = first;
    }
  }
  return mapped;
}

export function resolveFormSubmissionError(
  error: unknown,
  setErrors: (fields: Record<string, string>) => void,
  fallbackMessage: string
): string | null {
  if (error instanceof ApiError) {
    if (error.status === 422 && error.errors) {
      const mapped = mapApiValidationErrors(error.errors);
      if (Object.keys(mapped).length > 0) {
        setErrors(mapped);
        return null;
      }
    }
    return error.detail || error.title || fallbackMessage;
  }

  return error instanceof Error ? error.message : fallbackMessage;
}
