import { HttpErrorResponse } from '@angular/common/http';
import { dueDateToIso, isoToDueDate, localDateTimeToIso } from './dates';
import { errorMessage } from './error-message';

describe('dates', () => {
  it('stores a picked calendar day as UTC midnight and reads it back as the same day', () => {
    const picked = new Date(2026, 10, 1); // 1 Nov 2026, local midnight

    const iso = dueDateToIso(picked);
    const back = isoToDueDate(iso);

    expect(iso).toBe('2026-11-01T00:00:00.000Z');
    expect([back?.getFullYear(), back?.getMonth(), back?.getDate()]).toEqual([2026, 10, 1]);
    expect(dueDateToIso(null)).toBeNull();
    expect(isoToDueDate(null)).toBeNull();
  });

  it('converts a datetime-local value to a UTC instant', () => {
    expect(localDateTimeToIso('2026-10-31T09:00')).toBe(new Date(2026, 9, 31, 9, 0).toISOString());
  });
});

describe('errorMessage', () => {
  const httpError = (status: number, error: unknown) => new HttpErrorResponse({ status, error });

  it('prefers the first validation error, then detail, then title', () => {
    expect(
      errorMessage(httpError(400, { title: 'Bad', errors: { Title: ['Title is required.'] } })),
    ).toBe('Title is required.');
    expect(errorMessage(httpError(409, { title: 'Conflict', detail: 'Email taken.' }))).toBe(
      'Email taken.',
    );
    expect(errorMessage(httpError(404, { title: 'Not Found' }))).toBe('Not Found');
  });

  it('explains a network failure and falls back otherwise', () => {
    expect(errorMessage(httpError(0, null))).toContain('Cannot reach the server');
    expect(errorMessage(new Error('boom'), 'Fallback')).toBe('Fallback');
    expect(errorMessage(httpError(500, null), 'Fallback')).toBe('Fallback');
  });
});
