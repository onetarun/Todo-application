import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import { CreateTagRequest, Tag, UpdateTagRequest } from '../models/tag';

@Service()
export class TagService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getAll(): Observable<Tag[]> {
    return this.http.get<Tag[]>(`${this.baseUrl}/tags`);
  }

  create(request: CreateTagRequest): Observable<Tag> {
    return this.http.post<Tag>(`${this.baseUrl}/tags`, request);
  }

  update(id: number, request: UpdateTagRequest): Observable<Tag> {
    return this.http.put<Tag>(`${this.baseUrl}/tags/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/tags/${id}`);
  }

  /** Idempotent: assigning an already-assigned tag succeeds. */
  assign(taskId: number, tagId: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/tasks/${taskId}/tags/${tagId}`, null);
  }

  /** Idempotent: removing a tag that is not assigned succeeds. */
  unassign(taskId: number, tagId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/tasks/${taskId}/tags/${tagId}`);
  }
}
