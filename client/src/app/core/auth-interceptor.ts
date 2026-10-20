import {
  HttpErrorResponse,
  HttpInterceptorFn,
  HttpRequest,
  HttpStatusCode,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { AuthService } from './auth.service';

// These endpoints authenticate with the request body, never with the access token.
const ANONYMOUS_AUTH_PATHS = ['/auth/login', '/auth/register', '/auth/refresh', '/auth/logout'];

/**
 * Adds the access token to API calls. On a 401 it refreshes the tokens once and retries the request;
 * if the refresh fails the session is over, so it signs out and sends the user to the login page.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const baseUrl = inject(API_BASE_URL);

  const isApiCall = req.url.startsWith(`${baseUrl}/`);
  if (!isApiCall || ANONYMOUS_AUTH_PATHS.some((path) => req.url === `${baseUrl}${path}`)) {
    return next(req);
  }

  return next(withAccessToken(req, auth.accessToken())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Unauthorized) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        catchError(() => {
          auth.clearSession();
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
          return throwError(() => error);
        }),
        switchMap(() => next(withAccessToken(req, auth.accessToken()))),
      );
    }),
  );
};

function withAccessToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}
