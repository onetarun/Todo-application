import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../core/api-config';
import { CreateTodoListRequest, TodoList, UpdateTodoListRequest } from '../models/todo-list';

@Service()
export class TodoListService {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/lists`;

  getAll(): Observable<TodoList[]> {
    return this.http.get<TodoList[]>(this.url);
  }

  get(id: number): Observable<TodoList> {
    return this.http.get<TodoList>(`${this.url}/${id}`);
  }

  create(request: CreateTodoListRequest): Observable<TodoList> {
    return this.http.post<TodoList>(this.url, request);
  }

  update(id: number, request: UpdateTodoListRequest): Observable<TodoList> {
    return this.http.put<TodoList>(`${this.url}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
