import { DatePipe } from '@angular/common';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { dueDateToIso } from '../../../core/dates';
import { errorMessage } from '../../../core/error-message';
import { Task, TASK_STATUSES, TaskStatus } from '../../../models/task';
import { TagService } from '../../../services/tag.service';
import { TaskService } from '../../../services/task.service';
import { TodoListService } from '../../../services/todo-list.service';
import { confirmAction } from '../../../shared/confirm-dialog/confirm-dialog';
import { PRIORITY_OPTIONS, priorityLabel, STATUS_LABELS } from '../../../shared/task-labels';

// Mirrors the limit in TodoApi.Dtos.Tasks.
const MAX_TITLE_LENGTH = 200;
const DEFAULT_PRIORITY = 2;

@Component({
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  selector: 'app-tasks-page',
  styleUrl: './tasks-page.css',
  templateUrl: './tasks-page.html',
})
export class TasksPage {
  private readonly listService = inject(TodoListService);
  private readonly taskService = inject(TaskService);
  private readonly tagService = inject(TagService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  /** Route parameter (bound by the router). */
  readonly listId = input.required({ transform: numberAttribute });

  protected readonly statuses = TASK_STATUSES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityOptions = PRIORITY_OPTIONS;
  protected readonly priorityLabel = priorityLabel;

  protected readonly statusFilter = signal<TaskStatus | null>(null);
  protected readonly priorityFilter = signal<number | null>(null);
  protected readonly tagFilter = signal<number | null>(null);
  protected readonly hasFilters = computed(
    () =>
      this.statusFilter() !== null || this.priorityFilter() !== null || this.tagFilter() !== null,
  );
  protected readonly saving = signal(false);

  protected readonly list = rxResource({
    params: () => this.listId(),
    stream: ({ params: listId }) => this.listService.get(listId),
  });

  protected readonly tags = rxResource({ stream: () => this.tagService.getAll() });

  protected readonly tasks = rxResource({
    params: () => ({
      listId: this.listId(),
      status: this.statusFilter(),
      priority: this.priorityFilter(),
      tagId: this.tagFilter(),
    }),
    stream: ({ params: { listId, ...query } }) => this.taskService.getForList(listId, query),
  });

  protected readonly createForm = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(MAX_TITLE_LENGTH)]],
    priority: [DEFAULT_PRIORITY],
    dueDate: [null as Date | null],
  });

  protected loadError(): string {
    const error = this.list.error() ?? this.tasks.error();
    return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound
      ? 'This list does not exist.'
      : errorMessage(error, 'Could not load the tasks.');
  }

  protected clearFilters(): void {
    this.statusFilter.set(null);
    this.priorityFilter.set(null);
    this.tagFilter.set(null);
  }

  protected create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const { title, priority, dueDate } = this.createForm.getRawValue();
    this.saving.set(true);
    this.taskService
      .create(this.listId(), { title, priority, dueDate: dueDateToIso(dueDate) })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.createForm.reset();
          this.tasks.reload();
        },
        error: (error: unknown) => this.fail(error, 'Could not create the task.'),
      });
  }

  protected changeStatus(task: Task, status: TaskStatus): void {
    this.taskService.updateStatus(task.id, status).subscribe({
      next: () => this.tasks.reload(),
      error: (error: unknown) => {
        this.fail(error, 'Could not change the status.');
        this.tasks.reload();
      },
    });
  }

  protected delete(task: Task): void {
    confirmAction(this.dialog, {
      title: 'Delete task?',
      message: `"${task.title}" and its subtasks, notes and reminders will be deleted.`,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.taskService.delete(task.id)),
      )
      .subscribe({
        next: () => this.tasks.reload(),
        error: (error: unknown) => this.fail(error, 'Could not delete the task.'),
      });
  }

  private fail(error: unknown, fallback: string): void {
    this.saving.set(false);
    this.snackBar.open(errorMessage(error, fallback), 'Dismiss', { duration: 6000 });
  }
}
