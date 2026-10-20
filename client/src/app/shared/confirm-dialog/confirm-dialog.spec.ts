import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ConfirmDialog } from './confirm-dialog';

describe('ConfirmDialog', () => {
  it('shows the title, message and confirm label', async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmDialog],
      providers: [
        {
          provide: MAT_DIALOG_DATA,
          useValue: { title: 'Delete list?', message: 'Gone for good.', confirmLabel: 'Remove' },
        },
        { provide: MatDialogRef, useValue: { close: vi.fn() } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ConfirmDialog);
    await fixture.whenStable();
    const text = (fixture.nativeElement as HTMLElement).textContent;

    expect(text).toContain('Delete list?');
    expect(text).toContain('Gone for good.');
    expect(text).toContain('Remove');
  });
});
