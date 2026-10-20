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
import { filter, switchMap } from 'rxjs';
import { errorMessage } from '../../../core/error-message';
import { Tag } from '../../../models/tag';
import { TagService } from '../../../services/tag.service';
import { confirmAction } from '../../../shared/confirm-dialog/confirm-dialog';

// Mirrors the limits in TodoApi.Dtos.Tags.
const MAX_NAME_LENGTH = 50;
const COLOR_PATTERN = /^#[0-9A-Fa-f]{6}$/;
const DEFAULT_COLOR = '#3E63DD';

@Component({
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
  ],
  selector: 'app-tags-page',
  styleUrl: './tags-page.css',
  templateUrl: './tags-page.html',
})
export class TagsPage {
  private readonly tagService = inject(TagService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly tags = rxResource({ stream: () => this.tagService.getAll() });
  protected readonly saving = signal(false);
  protected readonly editingId = signal<number | null>(null);

  protected readonly createForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(MAX_NAME_LENGTH)]],
    color: [DEFAULT_COLOR, [Validators.required, Validators.pattern(COLOR_PATTERN)]],
  });

  protected readonly editForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(MAX_NAME_LENGTH)]],
    color: [DEFAULT_COLOR, [Validators.required, Validators.pattern(COLOR_PATTERN)]],
  });

  protected loadError(): string {
    return errorMessage(this.tags.error(), 'Could not load the tags.');
  }

  protected create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.tagService.create(this.createForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.createForm.reset();
        this.tags.reload();
      },
      error: (error: unknown) => this.fail(error, 'Could not create the tag.'),
    });
  }

  protected startEdit(tag: Tag): void {
    this.editForm.reset({ name: tag.name, color: tag.color });
    this.editingId.set(tag.id);
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
  }

  protected saveEdit(tag: Tag): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.tagService.update(tag.id, this.editForm.getRawValue()).subscribe({
      next: () => {
        this.saving.set(false);
        this.editingId.set(null);
        this.tags.reload();
      },
      error: (error: unknown) => this.fail(error, 'Could not save the tag.'),
    });
  }

  protected delete(tag: Tag): void {
    confirmAction(this.dialog, {
      title: 'Delete tag?',
      message: `"${tag.name}" is shared by everyone and will be removed from every task that uses it.`,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => this.tagService.delete(tag.id)),
      )
      .subscribe({
        next: () => this.tags.reload(),
        error: (error: unknown) => this.fail(error, 'Could not delete the tag.'),
      });
  }

  private fail(error: unknown, fallback: string): void {
    this.saving.set(false);
    this.snackBar.open(errorMessage(error, fallback), 'Dismiss', { duration: 6000 });
  }
}
