import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import { CreateSubtaskRequest, Subtask, UpdateSubtaskRequest } from '../models/subtask';

@Service()
export class SubtaskService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getForTask(taskId: number): Observable<Subtask[]> {
    return this.http.get<Subtask[]>(`${this.baseUrl}/tasks/${taskId}/subtasks`);
  }

  create(taskId: number, request: CreateSubtaskRequest): Observable<Subtask> {
    return this.http.post<Subtask>(`${this.baseUrl}/tasks/${taskId}/subtasks`, request);
  }

  update(id: number, request: UpdateSubtaskRequest): Observable<Subtask> {
    return this.http.put<Subtask>(`${this.baseUrl}/subtasks/${id}`, request);
  }

  complete(id: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/subtasks/${id}/complete`, null);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/subtasks/${id}`);
  }
}
