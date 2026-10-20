import { HttpClient } from '@angular/common/http';
import { computed, inject, Service, signal } from '@angular/core';
import { catchError, finalize, Observable, of, shareReplay, tap, throwError } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models/auth';
import { API_BASE_URL } from './api-config';

export const REFRESH_TOKEN_KEY = 'todo.refreshToken';

// The access token lives only in memory. The refresh token is kept in localStorage so a session survives a page
// reload (decision recorded in docs/plan.md, Phase 5): script-readable, mitigated by short-lived access tokens and
// refresh-token rotation with reuse detection on the server.
@Service()
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  private readonly accessTokenValue = signal<string | null>(null);
  private readonly user = signal<User | null>(null);
  private refreshInFlight: Observable<AuthResponse> | null = null;

  readonly currentUser = this.user.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null);

  accessToken(): string | null {
    return this.accessTokenValue();
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/auth/register`, request)
      .pipe(tap((response) => this.startSession(response)));
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/auth/login`, request)
      .pipe(tap((response) => this.startSession(response)));
  }

  /**
   * Exchanges the stored refresh token for new tokens. Concurrent callers share one request, because the server
   * rotates refresh tokens and treats a second use of the same token as theft. Clears the session on failure.
   */
  refresh(): Observable<AuthResponse> {
    const refreshToken = readRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token.'));
    }

    this.refreshInFlight ??= this.http
      .post<AuthResponse>(`${this.baseUrl}/auth/refresh`, { refreshToken })
      .pipe(
        tap({
          next: (response) => this.startSession(response),
          error: () => this.clearSession(),
        }),
        finalize(() => (this.refreshInFlight = null)),
        shareReplay(1),
      );
    return this.refreshInFlight;
  }

  /** Reloads the signed-in user's profile from the server. */
  me(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/auth/me`).pipe(tap((user) => this.user.set(user)));
  }

  /** Revokes the refresh token on the server (best effort) and clears the local session. */
  logout(): Observable<void> {
    const refreshToken = readRefreshToken();
    this.clearSession();
    if (!refreshToken) {
      return of(undefined);
    }
    return this.http
      .post<void>(`${this.baseUrl}/auth/logout`, { refreshToken })
      .pipe(catchError(() => of(undefined)));
  }

  /** Called once at startup: signs the user back in from a stored refresh token. Never fails. */
  restoreSession(): Observable<unknown> {
    return readRefreshToken() ? this.refresh().pipe(catchError(() => of(null))) : of(null);
  }

  clearSession(): void {
    this.accessTokenValue.set(null);
    this.user.set(null);
    writeRefreshToken(null);
  }

  private startSession(response: AuthResponse): void {
    this.accessTokenValue.set(response.accessToken);
    this.user.set(response.user);
    writeRefreshToken(response.refreshToken);
  }
}

// Storage can be unavailable (private mode, blocked site data); the app then behaves as if nothing is stored.
function readRefreshToken(): string | null {
  try {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  } catch {
    return null;
  }
}

function writeRefreshToken(token: string | null): void {
  try {
    if (token) {
      localStorage.setItem(REFRESH_TOKEN_KEY, token);
    } else {
      localStorage.removeItem(REFRESH_TOKEN_KEY);
    }
  } catch {
    // Nothing to do: the session simply will not survive a reload.
  }
}
