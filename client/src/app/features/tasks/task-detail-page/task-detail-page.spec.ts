import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideRouter } from '@angular/router';
import { taskDetail } from '../../../../testing/test-data';
import { TaskDetail } from '../../../models/task';
import { TaskDetailPage } from './task-detail-page';

describe('TaskDetailPage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TaskDetailPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNativeDateAdapter(),
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(task: TaskDetail = taskDetail()) {
    const fixture = TestBed.createComponent(TaskDetailPage);
    fixture.componentRef.setInput('taskId', task.id);
    TestBed.tick();
    http.expectOne('/api/tasks/5').flush(task);
    http.expectOne('/api/tags').flush([
      { id: 1, name: 'Work', color: '#3E63DD' },
      { id: 2, name: 'Urgent', color: '#E5484D' },
    ]);
    await fixture.whenStable();
    return {
      fixture,
      element: fixture.nativeElement as HTMLElement,
      page: fixture.componentInstance,
    };
  }

  /** The page reloads the task after every change. */
  async function expectReload(fixture: { whenStable(): Promise<unknown> }, task = taskDetail()) {
    TestBed.tick();
    http.expectOne('/api/tasks/5').flush(task);
    await fixture.whenStable();
  }

  it('shows every section of the task', async () => {
    const { element, page } = await render();

    expect(element.querySelector('h1')?.textContent).toContain('Write report');
    expect(element.textContent).toContain('Gather data');
    expect(element.textContent).toContain('Ask finance');
    expect(element.querySelector('mat-chip-set')?.textContent).toContain('Work');
    expect(page['taskForm'].getRawValue()).toEqual({
      title: 'Write report',
      description: '',
      status: 'Todo',
      priority: 2,
      dueDate: new Date(2026, 10, 1),
    });
    expect(page['availableTags']().map((t) => t.name)).toEqual(['Urgent']);
  });

  it('saves the task as a full update with a UTC due date', async () => {
    const { fixture, page } = await render();
    page['taskForm'].patchValue({ status: 'Done', priority: 3 });
    page['taskForm'].markAsDirty();

    page['saveTask']();
    const put = http.expectOne({ method: 'PUT', url: '/api/tasks/5' });
    expect(put.request.body).toEqual({
      title: 'Write report',
      description: null,
      status: 'Done',
      priority: 3,
      dueDate: '2026-11-01T00:00:00.000Z',
    });
    put.flush(taskDetail({ status: 'Done', priority: 3 }));
    await expectReload(fixture, taskDetail({ status: 'Done', priority: 3 }));

    expect(page['taskForm'].pristine).toBe(true);
  });

  it('completes a subtask with PATCH and un-completes it with PUT', async () => {
    const { fixture, page } = await render();
    const subtask = taskDetail().subtasks[0];

    page['toggleSubtask'](subtask, true);
    http.expectOne({ method: 'PATCH', url: '/api/subtasks/11/complete' }).flush(null);
    await expectReload(fixture);

    page['toggleSubtask'](subtask, false);
    expect(http.expectOne({ method: 'PUT', url: '/api/subtasks/11' }).request.body).toEqual({
      title: 'Gather data',
      isCompleted: false,
    });
  });

  it('keeps unsaved task edits when another section reloads the task', async () => {
    const { fixture, page } = await render();
    page['taskForm'].controls.title.setValue('Unsaved title');
    page['taskForm'].markAsDirty();

    page['deleteNote'](taskDetail().notes[0]);
    http.expectOne({ method: 'DELETE', url: '/api/notes/21' }).flush(null);
    await expectReload(fixture, taskDetail({ notes: [] }));

    expect(page['taskForm'].controls.title.value).toBe('Unsaved title');
  });

  it('adds a reminder as a UTC instant', async () => {
    const { page } = await render();
    page['reminderForm'].setValue({ remindAt: '2026-10-31T09:00' });

    page['addReminder']();
    expect(http.expectOne({ method: 'POST', url: '/api/tasks/5/reminders' }).request.body).toEqual({
      remindAt: new Date(2026, 9, 31, 9, 0).toISOString(),
    });
  });
});
