import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import {
  CreateTaskRequest,
  Task,
  TaskDetail,
  TaskQuery,
  TaskStatus,
  UpdateTaskRequest,
} from '../models/task';

@Service()
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getForList(listId: number, query: TaskQuery = {}): Observable<Task[]> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== null && value !== undefined) {
        params = params.set(key, value);
      }
    }
    return this.http.get<Task[]>(`${this.baseUrl}/lists/${listId}/tasks`, { params });
  }

  create(listId: number, request: CreateTaskRequest): Observable<Task> {
    return this.http.post<Task>(`${this.baseUrl}/lists/${listId}/tasks`, request);
  }

  get(id: number): Observable<TaskDetail> {
    return this.http.get<TaskDetail>(`${this.baseUrl}/tasks/${id}`);
  }

  update(id: number, request: UpdateTaskRequest): Observable<Task> {
    return this.http.put<Task>(`${this.baseUrl}/tasks/${id}`, request);
  }

  updateStatus(id: number, status: TaskStatus): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/tasks/${id}/status`, { status });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/tasks/${id}`);
  }
}
