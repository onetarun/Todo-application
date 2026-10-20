// Sample API payloads shared by the unit tests.
import { AuthResponse } from '../app/models/auth';
import { TaskDetail } from '../app/models/task';

export function authResponse(suffix = '1'): AuthResponse {
  return {
    accessToken: `access-${suffix}`,
    accessTokenExpiresAt: '2026-10-20T10:15:00Z',
    refreshToken: `refresh-${suffix}`,
    refreshTokenExpiresAt: '2026-10-27T10:00:00Z',
    user: { id: 7, name: 'Ada', email: 'ada@test.local' },
  };
}

export function taskDetail(overrides: Partial<TaskDetail> = {}): TaskDetail {
  return {
    id: 5,
    todoListId: 3,
    title: 'Write report',
    description: null,
    status: 'Todo',
    priority: 2,
    dueDate: '2026-11-01T00:00:00Z',
    createdAt: '2026-10-20T09:00:00Z',
    updatedAt: '2026-10-20T09:00:00Z',
    tags: [{ id: 1, name: 'Work', color: '#3E63DD' }],
    subtasks: [{ id: 11, taskId: 5, title: 'Gather data', isCompleted: false }],
    notes: [{ id: 21, taskId: 5, content: 'Ask finance', createdAt: '2026-10-20T09:30:00Z' }],
    reminders: [{ id: 31, taskId: 5, remindAt: '2026-10-31T09:00:00Z' }],
    ...overrides,
  };
}
