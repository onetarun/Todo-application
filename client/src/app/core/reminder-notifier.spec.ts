import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { provideRouter, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { DueReminder } from '../models/reminder';
import { AuthService } from './auth.service';
import { POLL_INTERVAL_MS, ReminderNotifier, SHOWN_REMINDERS_KEY } from './reminder-notifier';

describe('ReminderNotifier', () => {
  const signedIn = signal(false);
  let http: HttpTestingController;
  let notifier: ReminderNotifier;
  let dismissed: Subject<void>;
  let action: Subject<void>;
  const open = vi.fn();

  const reminder = (id: number, title: string): DueReminder => ({
    id,
    taskId: id * 10,
    taskTitle: title,
    remindAt: '2026-10-20T09:00:00Z',
  });

  /** Lets the session effect run and the poll timer fire. */
  async function advance(ms: number) {
    TestBed.tick();
    await vi.advanceTimersByTimeAsync(ms);
  }

  const duePolls = () => http.match((r) => r.url === '/api/reminders');

  beforeEach(() => {
    vi.useFakeTimers();
    localStorage.clear();
    signedIn.set(false);
    open.mockReset();
    open.mockImplementation(() => {
      dismissed = new Subject<void>();
      action = new Subject<void>();
      return { afterDismissed: () => dismissed, onAction: () => action };
    });

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { isAuthenticated: signedIn.asReadonly() } },
        { provide: MatSnackBar, useValue: { open } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    notifier = TestBed.inject(ReminderNotifier);
    notifier.start();
  });

  afterEach(() => {
    notifier.stop();
    vi.useRealTimers();
  });

  it('does not poll while signed out', async () => {
    await advance(POLL_INTERVAL_MS * 2);
    expect(duePolls()).toHaveLength(0);
  });

  it('shows each due reminder once, oldest first, one at a time', async () => {
    signedIn.set(true);
    await advance(0);
    // The API returns newest first.
    duePolls()[0].flush([reminder(2, 'Call Sam'), reminder(1, 'Write report')]);

    expect(open).toHaveBeenCalledTimes(1);
    expect(open).toHaveBeenLastCalledWith('Reminder: Write report', 'Open', expect.anything());
    dismissed.next();
    expect(open).toHaveBeenLastCalledWith('Reminder: Call Sam', 'Open', expect.anything());
    dismissed.next();

    await advance(POLL_INTERVAL_MS);
    duePolls()[0].flush([reminder(2, 'Call Sam'), reminder(1, 'Write report')]);
    expect(open).toHaveBeenCalledTimes(2);
    expect(JSON.parse(localStorage.getItem(SHOWN_REMINDERS_KEY)!)).toEqual([1, 2]);
  });

  it('skips reminders already shown in this browser', async () => {
    localStorage.setItem(SHOWN_REMINDERS_KEY, JSON.stringify([1]));
    signedIn.set(true);
    await advance(0);

    duePolls()[0].flush([reminder(1, 'Write report')]);

    expect(open).not.toHaveBeenCalled();
  });

  it('opens the task from the snackbar action', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    signedIn.set(true);
    await advance(0);
    duePolls()[0].flush([reminder(1, 'Write report')]);

    action.next();

    expect(navigate).toHaveBeenCalledWith(['/tasks', 10]);
  });

  it('stops polling on sign-out and ignores failed checks', async () => {
    signedIn.set(true);
    await advance(0);
    duePolls()[0].flush(null, { status: 500, statusText: 'Server Error' });

    await advance(POLL_INTERVAL_MS);
    duePolls()[0].flush([]);

    signedIn.set(false);
    await advance(POLL_INTERVAL_MS * 3);
    expect(duePolls()).toHaveLength(0);
    expect(open).not.toHaveBeenCalled();
  });
});
