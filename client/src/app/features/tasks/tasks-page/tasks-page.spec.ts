import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideRouter } from '@angular/router';
import { taskDetail } from '../../../../testing/test-data';
import { TasksPage } from './tasks-page';

describe('TasksPage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TasksPage],
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

  async function render() {
    const fixture = TestBed.createComponent(TasksPage);
    fixture.componentRef.setInput('listId', 3);
    TestBed.tick();
    http
      .expectOne('/api/lists/3')
      .flush({ id: 3, title: 'Work', description: null, createdAt: '2026-10-20T09:00:00Z' });
    http.expectOne('/api/tags').flush([{ id: 1, name: 'Work', color: '#3E63DD' }]);
    http.expectOne((r) => r.url === '/api/lists/3/tasks').flush([taskDetail()]);
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  it('shows the list title and its tasks with due date and tags', async () => {
    const { element } = await render();

    expect(element.querySelector('h1')?.textContent).toContain('Work');
    expect(element.querySelector('.task-title')?.getAttribute('href')).toBe('/tasks/5');
    expect(element.textContent).toContain('Due Nov 1, 2026');
    expect(element.textContent).toContain('Medium priority');
  });

  it('reloads with the filter when one is chosen', async () => {
    const { fixture } = await render();

    fixture.componentInstance['statusFilter'].set('Done');
    TestBed.tick();
    const req = http.expectOne((r) => r.url === '/api/lists/3/tasks');
    expect(req.request.params.get('status')).toBe('Done');
    req.flush([]);
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'No tasks match these filters.',
    );
  });

  it('creates a task with a UTC midnight due date', async () => {
    const { fixture, element } = await render();
    const form = fixture.componentInstance['createForm'];
    form.setValue({ title: 'Plan trip', priority: 3, dueDate: new Date(2026, 11, 24) });

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    const post = http.expectOne({ method: 'POST', url: '/api/lists/3/tasks' });
    expect(post.request.body).toEqual({
      title: 'Plan trip',
      priority: 3,
      dueDate: '2026-12-24T00:00:00.000Z',
    });
    post.flush(taskDetail({ id: 6, title: 'Plan trip' }));
    TestBed.tick();
    http.expectOne((r) => r.url === '/api/lists/3/tasks').flush([]);
    await fixture.whenStable();
  });

  it('says when the list does not exist', async () => {
    const fixture = TestBed.createComponent(TasksPage);
    fixture.componentRef.setInput('listId', 99);
    TestBed.tick();
    http.expectOne('/api/lists/99').flush(null, { status: 404, statusText: 'Not Found' });
    http.expectOne('/api/tags').flush([]);
    http
      .expectOne((r) => r.url === '/api/lists/99/tasks')
      .flush(null, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[role=alert]')?.textContent,
    ).toContain('This list does not exist.');
  });
});
