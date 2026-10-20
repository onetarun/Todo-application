// Mirrors TodoApi.Dtos.Reminders. Dates are ISO 8601 UTC strings.

export interface Reminder {
  id: number;
  taskId: number;
  remindAt: string;
}

export interface CreateReminderRequest {
  remindAt: string;
}
