import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { authResponse } from '../../testing/test-data';
import { AuthService, REFRESH_TOKEN_KEY } from './auth.service';
import { authInterceptor } from './auth-interceptor';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let auth: AuthService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    auth = TestBed.inject(AuthService);

    auth.login({ email: 'ada@test.local', password: 'Passw0rd!' }).subscribe();
    http.expectOne('/api/auth/login').flush(authResponse());
  });

  afterEach(() => http.verify());

  it('adds the bearer token to API calls', () => {
    client.get('/api/lists').subscribe();
    expect(http.expectOne('/api/lists').request.headers.get('Authorization')).toBe(
      'Bearer access-1',
    );
  });

  it('does not add the token to anonymous auth endpoints or other origins', () => {
    client.post('/api/auth/refresh', {}).subscribe();
    client.get('https://example.com/api/lists').subscribe();

    expect(http.expectOne('/api/auth/refresh').request.headers.has('Authorization')).toBe(false);
    expect(
      http.expectOne('https://example.com/api/lists').request.headers.has('Authorization'),
    ).toBe(false);
  });

  it('on 401 refreshes once and retries with the new token', () => {
    let body: unknown;
    client.get('/api/lists').subscribe((value) => (body = value));

    http.expectOne('/api/lists').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/auth/refresh').flush(authResponse('2'));
    const retry = http.expectOne('/api/lists');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer access-2');
    retry.flush([{ id: 1 }]);

    expect(body).toEqual([{ id: 1 }]);
  });

  it('when the refresh fails, signs out and redirects to the login page', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    let status = 0;
    client.get('/api/lists').subscribe({ error: (e: { status: number }) => (status = e.status) });

    http.expectOne('/api/lists').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(status).toBe(401);
    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(REFRESH_TOKEN_KEY)).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });

  it('passes other errors through without refreshing', () => {
    let status = 0;
    client.get('/api/lists/9').subscribe({ error: (e: { status: number }) => (status = e.status) });
    http.expectOne('/api/lists/9').flush(null, { status: 404, statusText: 'Not Found' });
    expect(status).toBe(404);
  });
});
