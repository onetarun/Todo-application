import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import { CreateReminderRequest, Reminder } from '../models/reminder';

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

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/reminders/${id}`);
  }
}
