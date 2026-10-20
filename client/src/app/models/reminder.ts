// Mirrors TodoApi.Dtos.Reminders. Dates are ISO 8601 UTC strings.

export interface Reminder {
  id: number;
  taskId: number;
  remindAt: string;
}

/** A reminder that has come due (GET /api/reminders?before=...). */
export interface DueReminder {
  id: number;
  taskId: number;
  taskTitle: string;
  remindAt: string;
}

export interface CreateReminderRequest {
  remindAt: string;
}
