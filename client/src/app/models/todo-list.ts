// Mirrors TodoApi.Dtos.Lists. Dates are ISO 8601 UTC strings.

export interface TodoList {
  id: number;
  title: string;
  description: string | null;
  createdAt: string;
}

export interface CreateTodoListRequest {
  title: string;
  description?: string | null;
}

export interface UpdateTodoListRequest {
  title: string;
  description?: string | null;
}
