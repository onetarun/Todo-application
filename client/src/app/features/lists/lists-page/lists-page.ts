import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RouterLink } from '@angular/router';
import { filter, switchMap } from 'rxjs';
import { errorMessage } from '../../../core/error-message';
import { TodoList } from '../../../models/todo-list';
import { TodoListService } from '../../../services/todo-list.service';
import { confirmAction } from '../../../shared/confirm-dialog/confirm-dialog';

// Mirrors the limits in TodoApi.Dtos.Lists.
const MAX_TITLE_LENGTH = 200;
const MAX_DESCRIPTION_LENGTH = 2000;

@Component({
  imports: [
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  selector: 'app-lists-page',
  styleUrl: './lists-page.css',
  templateUrl: './lists-page.html',
})
export class ListsPage {
  private readonly listService = inject(TodoListService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly lists = rxResource({ stream: () => this.listService.getAll() });
  protected readonly saving = signal(false);
  /** Id of the list being renamed, if any. */
  protected readonly editingId = signal<number | null>(null);

  protected readonly createForm = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(MAX_TITLE_LENGTH)]],
    description: ['', Validators.maxLength(MAX_DESCRIPTION_LENGTH)],
  });

  protected readonly editForm = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(MAX_TITLE_LENGTH)]],
    description: ['', Validators.maxLength(MAX_DESCRIPTION_LENGTH)],
  });

  protected loadError(): string {
    return errorMessage(this.lists.error(), 'Could not load your lists.');
  }

  protected create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const { title, description } = this.createForm.getRawValue();
    this.saving.set(true);
    this.listService.create({ title, description: description || null }).subscribe({
      next: () => {
        this.saving.set(false);
        this.createForm.reset();
        this.lists.reload();
      },
      error: (error: unknown) => this.fail(error, 'Could not create the list.'),
    });
  }

  protected startEdit(list: TodoList): void {
    this.editForm.reset({ title: list.title, description: list.description ?? '' });
    this.editingId.set(list.id);
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
  }

  protected saveEdit(list: TodoList): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const { title, description } = this.editForm.getRawValue();
    this.saving.set(true);
    this.listService.update(list.id, { title, description: description || null }).subscribe({
      next: () => {
        this.saving.set(false);
        this.editingId.set(null);
        this.lists.reload();
      },
      error: (error: unknown) => this.fail(error, 'Could not rename the list.'),
    });
  }

  protected delete(list: TodoList): void {
    confirmAction(this.dialog, {
      title: 'Delete list?',
      message: `"${list.title}" and all of its tasks will be deleted. This cannot be undone.`,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.listService.delete(list.id)),
      )
      .subscribe({
        next: () => this.lists.reload(),
        error: (error: unknown) => this.fail(error, 'Could not delete the list.'),
      });
  }

  private fail(error: unknown, fallback: string): void {
    this.saving.set(false);
    this.snackBar.open(errorMessage(error, fallback), 'Dismiss', { duration: 6000 });
  }
}
