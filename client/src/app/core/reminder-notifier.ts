import { inject, Injector, Service } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import {
  catchError,
  concatMap,
  EMPTY,
  exhaustMap,
  from,
  map,
  Observable,
  Subscription,
  switchMap,
  take,
  timer,
} from 'rxjs';
import { DueReminder } from '../models/reminder';
import { ReminderService } from '../services/reminder.service';
import { AuthService } from './auth.service';

export const POLL_INTERVAL_MS = 60_000;
export const SHOWN_REMINDERS_KEY = 'todo.shownReminders';
// Enough to cover a day of reminders; older ids fall out of the server's 24-hour window anyway.
const MAX_REMEMBERED = 200;

/**
 * In-app reminder notifications (docs/plan.md, decision 7): while a user is signed in, checks for due reminders on
 * sign-in and every minute, and shows each one once as a snackbar with a link to its task. Only works while the app
 * is open; shown reminder ids are remembered per browser in localStorage.
 */
@Service()
export class ReminderNotifier {
  private readonly auth = inject(AuthService);
  private readonly reminderService = inject(ReminderService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  private subscription: Subscription | null = null;

  /** Starts watching the session. Safe to call more than once. */
  start(): void {
    this.subscription ??= toObservable(this.auth.isAuthenticated, { injector: this.injector })
      .pipe(switchMap((signedIn) => (signedIn ? this.dueReminders() : EMPTY)))
      .subscribe();
  }

  stop(): void {
    this.subscription?.unsubscribe();
    this.subscription = null;
  }

  private dueReminders(): Observable<unknown> {
    return timer(0, POLL_INTERVAL_MS).pipe(
      // Skip a tick rather than overlap requests when the server is slow.
      exhaustMap(() =>
        this.reminderService.getDue(new Date().toISOString()).pipe(catchError(() => EMPTY)),
      ),
      map((due) => this.takeUnshown(due)),
      // Show one at a time, oldest first; each waits for the previous snackbar to close.
      concatMap((fresh) => from(fresh)),
      concatMap((reminder) => this.show(reminder)),
    );
  }

  /** Returns the reminders not shown before, oldest first, and records them as shown. */
  private takeUnshown(due: DueReminder[]): DueReminder[] {
    const shown = readShownIds();
    const fresh = due.filter((reminder) => !shown.includes(reminder.id)).reverse();
    if (fresh.length > 0) {
      writeShownIds([...shown, ...fresh.map((reminder) => reminder.id)].slice(-MAX_REMEMBERED));
    }
    return fresh;
  }

  private show(reminder: DueReminder): Observable<unknown> {
    const snackBarRef = this.snackBar.open(`Reminder: ${reminder.taskTitle}`, 'Open', {
      duration: 10_000,
      politeness: 'assertive',
    });
    snackBarRef.onAction().subscribe(() => void this.router.navigate(['/tasks', reminder.taskId]));
    return snackBarRef.afterDismissed().pipe(take(1));
  }
}

// Storage can be unavailable; then reminders may be shown again after a reload, which is acceptable.
function readShownIds(): number[] {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(SHOWN_REMINDERS_KEY) ?? '[]');
    return Array.isArray(parsed) ? parsed.filter((id): id is number => typeof id === 'number') : [];
  } catch {
    return [];
  }
}

function writeShownIds(ids: number[]): void {
  try {
    localStorage.setItem(SHOWN_REMINDERS_KEY, JSON.stringify(ids));
  } catch {
    // Nothing to do: see readShownIds.
  }
}
