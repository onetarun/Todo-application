import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { AuthService } from './core/auth.service';
import { User } from './models/auth';

describe('App', () => {
  const user = signal<User | null>(null);

  beforeEach(async () => {
    user.set(null);
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser: user.asReadonly() } },
      ],
    }).compileComponents();
  });

  it('hides navigation when signed out', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('nav')).toBeNull();
    expect(element.textContent).not.toContain('Sign out');
  });

  it('shows navigation and the user name when signed in', async () => {
    user.set({ id: 1, name: 'Ada', email: 'ada@test.local' });
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('nav')?.textContent).toContain('Lists');
    expect(element.textContent).toContain('Ada');
    expect(element.textContent).toContain('Sign out');
  });
});
