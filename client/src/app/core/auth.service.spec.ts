import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { authResponse } from '../../testing/test-data';
import { AuthService, REFRESH_TOKEN_KEY } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('login starts a session: user signal, in-memory access token, stored refresh token', () => {
    service.login({ email: 'ada@test.local', password: 'Passw0rd!' }).subscribe();

    const req = http.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    req.flush(authResponse());

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.name).toBe('Ada');
    expect(service.accessToken()).toBe('access-1');
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBe('refresh-1');
  });

  it('refresh shares one request between concurrent callers and rotates the stored token', () => {
    localStorage.setItem(REFRESH_TOKEN_KEY, 'refresh-old');
    const results: string[] = [];

    service.refresh().subscribe((r) => results.push(r.accessToken));
    service.refresh().subscribe((r) => results.push(r.accessToken));

    const req = http.expectOne('/api/auth/refresh');
    expect(req.request.body).toEqual({ refreshToken: 'refresh-old' });
    req.flush(authResponse('2'));

    expect(results).toEqual(['access-2', 'access-2']);
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBe('refresh-2');
  });

  it('a failed refresh clears the session', () => {
    localStorage.setItem(REFRESH_TOKEN_KEY, 'refresh-old');
    let failed = false;

    service.refresh().subscribe({ error: () => (failed = true) });
    http.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBe(true);
    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
  });

  it('refresh without a stored token fails without calling the API', () => {
    let failed = false;
    service.refresh().subscribe({ error: () => (failed = true) });
    expect(failed).toBe(true);
  });

  it('logout clears the session and revokes the refresh token on the server', () => {
    service.login({ email: 'ada@test.local', password: 'Passw0rd!' }).subscribe();
    http.expectOne('/api/auth/login').flush(authResponse());

    let done = false;
    service.logout().subscribe(() => (done = true));
    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();

    const req = http.expectOne('/api/auth/logout');
    expect(req.request.body).toEqual({ refreshToken: 'refresh-1' });
    req.flush(null, { status: 500, statusText: 'Server Error' });
    expect(done).toBe(true);
  });

  it('me reloads the signed-in user', () => {
    service.me().subscribe();
    http
      .expectOne({ method: 'GET', url: '/api/auth/me' })
      .flush({ id: 7, name: 'Ada L.', email: 'ada@test.local' });
    expect(service.currentUser()?.name).toBe('Ada L.');
  });

  it('restoreSession does nothing without a stored token and never fails', () => {
    let completed = false;
    service.restoreSession().subscribe({ complete: () => (completed = true) });
    expect(completed).toBe(true);

    localStorage.setItem(REFRESH_TOKEN_KEY, 'stale');
    let failed = false;
    service.restoreSession().subscribe({ error: () => (failed = true) });
    http.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(failed).toBe(false);
    expect(service.isAuthenticated()).toBe(false);
  });
});
