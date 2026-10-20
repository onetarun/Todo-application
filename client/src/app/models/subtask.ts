// Mirrors TodoApi.Dtos.Subtasks.

export interface Subtask {
  id: number;
  taskId: number;
  title: string;
  isCompleted: boolean;
}

export interface CreateSubtaskRequest {
  title: string;
}

// Full replacement (PUT): both fields are required.
export interface UpdateSubtaskRequest {
  title: string;
  isCompleted: boolean;
}
