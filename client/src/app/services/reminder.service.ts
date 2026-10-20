import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import { CreateReminderRequest, DueReminder, Reminder } from '../models/reminder';

@Service()
export class ReminderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getForTask(taskId: number): Observable<Reminder[]> {
    return this.http.get<Reminder[]>(`${this.baseUrl}/tasks/${taskId}/reminders`);
  }

  create(taskId: number, request: CreateReminderRequest): Observable<Reminder> {
    return this.http.post<Reminder>(`${this.baseUrl}/tasks/${taskId}/reminders`, request);
  }

  /** The signed-in user's reminders due in the 24 hours up to `before` (UTC ISO string), newest first. */
  getDue(before: string): Observable<DueReminder[]> {
    return this.http.get<DueReminder[]>(`${this.baseUrl}/reminders`, { params: { before } });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/reminders/${id}`);
  }
}
