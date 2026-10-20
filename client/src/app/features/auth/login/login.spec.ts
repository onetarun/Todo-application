import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { authResponse } from '../../../../testing/test-data';
import { Login, safeReturnUrl } from './login';

describe('Login', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render() {
    const fixture = TestBed.createComponent(Login);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const type = (selector: string, value: string) => {
      const input = element.querySelector<HTMLInputElement>(selector)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    const submit = async () => {
      element.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };
    return { fixture, element, type, submit };
  }

  it('does not call the API and shows messages when the form is invalid', async () => {
    const { element, submit } = await render();

    await submit();

    http.expectNone('/api/auth/login');
    expect(element.textContent).toContain('Email is required.');
    expect(element.textContent).toContain('Password is required.');
  });

  it('signs in and goes to the return URL', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const { fixture, type, submit } = await render();
    fixture.componentRef.setInput('returnUrl', '/lists/3');

    type('input[type=email]', 'ada@test.local');
    type('input[type=password]', 'Passw0rd!');
    await submit();
    const req = http.expectOne('/api/auth/login');
    expect(req.request.body).toEqual({ email: 'ada@test.local', password: 'Passw0rd!' });
    req.flush(authResponse());

    expect(navigate).toHaveBeenCalledWith('/lists/3');
  });

  it('shows the server error when the credentials are wrong', async () => {
    const { fixture, element, type, submit } = await render();

    type('input[type=email]', 'ada@test.local');
    type('input[type=password]', 'wrong');
    await submit();
    http
      .expectOne('/api/auth/login')
      .flush(
        { status: 401, detail: 'Invalid email or password.' },
        { status: 401, statusText: 'Unauthorized' },
      );
    await fixture.whenStable();

    expect(element.querySelector('[role=alert]')?.textContent).toContain(
      'Invalid email or password.',
    );
  });

  it('only follows in-app return URLs', () => {
    expect(safeReturnUrl('/tasks/5')).toBe('/tasks/5');
    expect(safeReturnUrl('https://evil.example')).toBe('/lists');
    expect(safeReturnUrl('//evil.example')).toBe('/lists');
    expect(safeReturnUrl(undefined)).toBe('/lists');
  });
});
