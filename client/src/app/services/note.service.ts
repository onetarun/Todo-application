import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import { CreateNoteRequest, Note, UpdateNoteRequest } from '../models/note';

@Service()
export class NoteService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getForTask(taskId: number): Observable<Note[]> {
    return this.http.get<Note[]>(`${this.baseUrl}/tasks/${taskId}/notes`);
  }

  create(taskId: number, request: CreateNoteRequest): Observable<Note> {
    return this.http.post<Note>(`${this.baseUrl}/tasks/${taskId}/notes`, request);
  }

  update(id: number, request: UpdateNoteRequest): Observable<Note> {
    return this.http.put<Note>(`${this.baseUrl}/notes/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/notes/${id}`);
  }
}
