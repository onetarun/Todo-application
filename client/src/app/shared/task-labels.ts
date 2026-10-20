import { TaskStatus } from '../models/task';

export const STATUS_LABELS: Record<TaskStatus, string> = {
  Todo: 'To do',
  InProgress: 'In progress',
  Done: 'Done',
};

export const PRIORITY_OPTIONS: readonly { value: number; label: string }[] = [
  { value: 1, label: 'Low' },
  { value: 2, label: 'Medium' },
  { value: 3, label: 'High' },
];

export function priorityLabel(priority: number): string {
  return PRIORITY_OPTIONS.find((option) => option.value === priority)?.label ?? String(priority);
}
