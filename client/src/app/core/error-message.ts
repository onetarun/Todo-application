import { HttpErrorResponse } from '@angular/common/http';

// RFC 7807 body returned by the API for errors; validation failures add `errors`.
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

const DEFAULT_MESSAGE = 'Something went wrong. Please try again.';

/** A user-facing message for a failed API call, taken from the ProblemDetails body when present. */
export function errorMessage(error: unknown, fallback = DEFAULT_MESSAGE): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'Cannot reach the server. Check that the API is running.';
  }

  const body = error.error as ProblemDetails | null;
  if (body && typeof body === 'object') {
    const firstValidationError = body.errors ? Object.values(body.errors).flat()[0] : undefined;
    return firstValidationError ?? body.detail ?? body.title ?? fallback;
  }

  return fallback;
}
