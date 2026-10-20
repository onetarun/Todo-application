// Mirrors TodoApi.Dtos.Notes. Dates are ISO 8601 UTC strings.

export interface Note {
  id: number;
  taskId: number;
  content: string;
  createdAt: string;
}

export interface CreateNoteRequest {
  content: string;
}

export interface UpdateNoteRequest {
  content: string;
}
