import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  provideRouter,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { AuthService } from './auth.service';
import { authGuard } from './auth-guard';
import { guestGuard } from './guest-guard';

describe('route guards', () => {
  const signedIn = signal(false);
  const run = (guard: CanActivateFn, url = '/lists/3') =>
    TestBed.runInInjectionContext(() =>
      guard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isAuthenticated: signedIn.asReadonly() } },
      ],
    });
  });

  it('authGuard lets signed-in users through', () => {
    signedIn.set(true);
    expect(run(authGuard)).toBe(true);
  });

  it('authGuard sends anonymous users to login with a return URL', () => {
    signedIn.set(false);
    const result = run(authGuard) as UrlTree;
    expect(result.toString()).toBe('/login?returnUrl=%2Flists%2F3');
  });

  it('guestGuard lets anonymous users through', () => {
    signedIn.set(false);
    expect(run(guestGuard)).toBe(true);
  });

  it('guestGuard sends signed-in users to their lists', () => {
    signedIn.set(true);
    expect((run(guestGuard) as UrlTree).toString()).toBe('/lists');
  });
});
