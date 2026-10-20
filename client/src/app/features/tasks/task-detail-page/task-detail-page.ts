import { DatePipe } from '@angular/common';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import {
  Component,
  computed,
  effect,
  inject,
  input,
  numberAttribute,
  signal,
  untracked,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { filter, Observable, switchMap } from 'rxjs';
import { dueDateToIso, isoToDueDate, localDateTimeToIso } from '../../../core/dates';
import { errorMessage } from '../../../core/error-message';
import { Note } from '../../../models/note';
import { Reminder } from '../../../models/reminder';
import { Subtask } from '../../../models/subtask';
import { Tag } from '../../../models/tag';
import { TASK_STATUSES, TaskStatus } from '../../../models/task';
import { NoteService } from '../../../services/note.service';
import { ReminderService } from '../../../services/reminder.service';
import { SubtaskService } from '../../../services/subtask.service';
import { TagService } from '../../../services/tag.service';
import { TaskService } from '../../../services/task.service';
import { confirmAction } from '../../../shared/confirm-dialog/confirm-dialog';
import { PRIORITY_OPTIONS, STATUS_LABELS } from '../../../shared/task-labels';

// Mirror the limits in the backend request DTOs.
const MAX_TITLE_LENGTH = 200;
const MAX_DESCRIPTION_LENGTH = 2000;
const MAX_NOTE_LENGTH = 4000;

@Component({
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  selector: 'app-task-detail-page',
  styleUrl: './task-detail-page.css',
  templateUrl: './task-detail-page.html',
})
export class TaskDetailPage {
  private readonly taskService = inject(TaskService);
  private readonly subtaskService = inject(SubtaskService);
  private readonly noteService = inject(NoteService);
  private readonly reminderService = inject(ReminderService);
  private readonly tagService = inject(TagService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  /** Route parameter (bound by the router). */
  readonly taskId = input.required({ transform: numberAttribute });

  protected readonly statuses = TASK_STATUSES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly priorityOptions = PRIORITY_OPTIONS;
  protected readonly saving = signal(false);
  protected readonly editingNoteId = signal<number | null>(null);

  protected readonly task = rxResource({
    params: () => this.taskId(),
    stream: ({ params: taskId }) => this.taskService.get(taskId),
  });

  protected readonly allTags = rxResource({ stream: () => this.tagService.getAll() });

  protected readonly availableTags = computed<Tag[]>(() => {
    if (!this.task.hasValue() || !this.allTags.hasValue()) {
      return [];
    }
    const assigned = new Set(this.task.value().tags.map((tag) => tag.id));
    return this.allTags.value().filter((tag) => !assigned.has(tag.id));
  });

  protected readonly taskForm = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(MAX_TITLE_LENGTH)]],
    description: ['', Validators.maxLength(MAX_DESCRIPTION_LENGTH)],
    status: ['Todo' as TaskStatus, Validators.required],
    priority: [2, Validators.required],
    dueDate: [null as Date | null],
  });

  protected readonly subtaskForm = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(MAX_TITLE_LENGTH)]],
  });

  protected readonly noteForm = this.fb.group({
    content: ['', [Validators.required, Validators.maxLength(MAX_NOTE_LENGTH)]],
  });

  protected readonly noteEditForm = this.fb.group({
    content: ['', [Validators.required, Validators.maxLength(MAX_NOTE_LENGTH)]],
  });

  protected readonly reminderForm = this.fb.group({
    remindAt: ['', Validators.required],
  });

  constructor() {
    // Fill the edit form from the loaded task, but never overwrite changes the user has not saved yet
    // (the task reloads after every subtask, note, reminder or tag change).
    effect(() => {
      if (!this.task.hasValue()) {
        return;
      }
      const task = this.task.value();
      untracked(() => {
        if (!this.taskForm.dirty) {
          this.taskForm.reset({
            title: task.title,
            description: task.description ?? '',
            status: task.status,
            priority: task.priority,
            dueDate: isoToDueDate(task.dueDate),
          });
        }
      });
    });
  }

  protected loadError(): string {
    const error = this.task.error();
    return error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotFound
      ? 'This task does not exist.'
      : errorMessage(error, 'Could not load the task.');
  }

  protected saveTask(): void {
    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      return;
    }

    const { title, description, status, priority, dueDate } = this.taskForm.getRawValue();
    this.mutate(
      this.taskService.update(this.taskId(), {
        title,
        description: description || null,
        status,
        priority,
        dueDate: dueDateToIso(dueDate),
      }),
      'Could not save the task.',
      () => {
        this.taskForm.markAsPristine();
        this.snackBar.open('Task saved.', undefined, { duration: 3000 });
      },
    );
  }

  protected resetTaskForm(): void {
    this.taskForm.markAsPristine();
    this.task.reload();
  }

  protected deleteTask(listId: number, title: string): void {
    confirmAction(this.dialog, {
      title: 'Delete task?',
      message: `"${title}" and its subtasks, notes and reminders will be deleted.`,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.taskService.delete(this.taskId())),
      )
      .subscribe({
        next: () => void this.router.navigate(['/lists', listId]),
        error: (error: unknown) => this.fail(error, 'Could not delete the task.'),
      });
  }

  // Subtasks

  protected addSubtask(): void {
    if (this.subtaskForm.invalid) {
      this.subtaskForm.markAllAsTouched();
      return;
    }
    this.mutate(
      this.subtaskService.create(this.taskId(), this.subtaskForm.getRawValue()),
      'Could not add the subtask.',
      () => this.subtaskForm.reset(),
    );
  }

  protected toggleSubtask(subtask: Subtask, completed: boolean): void {
    // PATCH .../complete only marks a subtask done; un-completing goes through the full update.
    const request = completed
      ? this.subtaskService.complete(subtask.id)
      : this.subtaskService.update(subtask.id, { title: subtask.title, isCompleted: false });
    this.mutate(request, 'Could not update the subtask.');
  }

  protected deleteSubtask(subtask: Subtask): void {
    this.mutate(this.subtaskService.delete(subtask.id), 'Could not delete the subtask.');
  }

  // Notes

  protected addNote(): void {
    if (this.noteForm.invalid) {
      this.noteForm.markAllAsTouched();
      return;
    }
    this.mutate(
      this.noteService.create(this.taskId(), this.noteForm.getRawValue()),
      'Could not add the note.',
      () => this.noteForm.reset(),
    );
  }

  protected startNoteEdit(note: Note): void {
    this.noteEditForm.reset({ content: note.content });
    this.editingNoteId.set(note.id);
  }

  protected cancelNoteEdit(): void {
    this.editingNoteId.set(null);
  }

  protected saveNote(note: Note): void {
    if (this.noteEditForm.invalid) {
      this.noteEditForm.markAllAsTouched();
      return;
    }
    this.mutate(
      this.noteService.update(note.id, this.noteEditForm.getRawValue()),
      'Could not save the note.',
      () => this.editingNoteId.set(null),
    );
  }

  protected deleteNote(note: Note): void {
    this.mutate(this.noteService.delete(note.id), 'Could not delete the note.');
  }

  // Reminders

  protected addReminder(): void {
    if (this.reminderForm.invalid) {
      this.reminderForm.markAllAsTouched();
      return;
    }
    const remindAt = localDateTimeToIso(this.reminderForm.getRawValue().remindAt);
    this.mutate(
      this.reminderService.create(this.taskId(), { remindAt }),
      'Could not add the reminder.',
      () => this.reminderForm.reset(),
    );
  }

  protected deleteReminder(reminder: Reminder): void {
    this.mutate(this.reminderService.delete(reminder.id), 'Could not delete the reminder.');
  }

  // Tags

  protected assignTag(change: MatSelectChange<number | null>): void {
    const tagId = change.value;
    // The select is an "add" action, not a stored value: clear it for the next pick.
    change.source.value = null;
    if (tagId !== null) {
      this.mutate(this.tagService.assign(this.taskId(), tagId), 'Could not add the tag.');
    }
  }

  protected unassignTag(tag: Tag): void {
    this.mutate(this.tagService.unassign(this.taskId(), tag.id), 'Could not remove the tag.');
  }

  /** Runs a change, then reloads the task so every section shows the server's state. */
  private mutate(request: Observable<unknown>, fallback: string, onSuccess?: () => void): void {
    this.saving.set(true);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        onSuccess?.();
        this.task.reload();
      },
      error: (error: unknown) => this.fail(error, fallback),
    });
  }

  private fail(error: unknown, fallback: string): void {
    this.saving.set(false);
    this.snackBar.open(errorMessage(error, fallback), 'Dismiss', { duration: 6000 });
  }
}
