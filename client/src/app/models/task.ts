// Mirrors TodoApi.Dtos.Tasks. Dates are ISO 8601 UTC strings.
import { Note } from './note';
import { Reminder } from './reminder';
import { Subtask } from './subtask';
import { Tag } from './tag';

export type TaskStatus = 'Todo' | 'InProgress' | 'Done';

export const TASK_STATUSES: readonly TaskStatus[] = ['Todo', 'InProgress', 'Done'];

export const MIN_PRIORITY = 1;
export const MAX_PRIORITY = 3;

export interface Task {
  id: number;
  todoListId: number;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: number;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
  tags: Tag[];
}

export interface TaskDetail extends Task {
  subtasks: Subtask[];
  notes: Note[];
  reminders: Reminder[];
}

export interface CreateTaskRequest {
  title: string;
  description?: string | null;
  status?: TaskStatus;
  priority?: number;
  dueDate?: string | null;
}

// Full replacement (PUT): status and priority are required.
export interface UpdateTaskRequest {
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: number;
  dueDate: string | null;
}

export interface UpdateTaskStatusRequest {
  status: TaskStatus;
}

export interface TaskQuery {
  status?: TaskStatus | null;
  priority?: number | null;
  tagId?: number | null;
}
