import { format, isValid, parseISO } from 'date-fns';

export type DateInput = Date | string | null | undefined;

export function parseDateInput(value: DateInput): Date | null {
  if (!value) {
    return null;
  }
  if (value instanceof Date) {
    return isValid(value) ? value : null;
  }
  const parsed = parseISO(value);
  return isValid(parsed) ? parsed : null;
}

export function formatDisplayDate(value: DateInput, fallback = ''): string {
  const date = parseDateInput(value);
  return date ? format(date, 'dd MMM yyyy') : fallback;
}

export function formatDisplayDateTime(value: DateInput, fallback = ''): string {
  const date = parseDateInput(value);
  return date ? format(date, 'dd MMM yyyy, HH:mm') : fallback;
}

export function formatBackendDateOnly(value: DateInput, fallback = ''): string {
  const date = parseDateInput(value);
  return date ? format(date, 'yyyy-MM-dd') : fallback;
}

export function formatBackendTimeOnly(value: DateInput, fallback = ''): string {
  const date = parseDateInput(value);
  return date ? format(date, 'HH:mm:ss') : fallback;
}
