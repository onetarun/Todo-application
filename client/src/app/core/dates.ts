// The API exchanges UTC ISO strings. A due date is a calendar day, stored as UTC midnight so it reads the
// same in every time zone (display it with the 'UTC' time zone). Reminders are instants in time.

/** Calendar day picked in the UI (local midnight) → UTC midnight ISO string. */
export function dueDateToIso(date: Date | null): string | null {
  return date
    ? new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate())).toISOString()
    : null;
}

/** UTC midnight ISO string → the same calendar day as a local Date for the date picker. */
export function isoToDueDate(iso: string | null): Date | null {
  if (!iso) {
    return null;
  }
  const date = new Date(iso);
  return new Date(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
}

/** Value of an `<input type="datetime-local">` (local time) → UTC ISO string. */
export function localDateTimeToIso(value: string): string {
  return new Date(value).toISOString();
}
