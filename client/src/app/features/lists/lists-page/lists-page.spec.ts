import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { TodoList } from '../../../models/todo-list';
import { ListsPage } from './lists-page';

describe('ListsPage', () => {
  let http: HttpTestingController;
  const work: TodoList = {
    id: 3,
    title: 'Work',
    description: 'Office tasks',
    createdAt: '2026-10-20T09:00:00Z',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListsPage],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        // Confirm every dialog.
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(true) }) } },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(lists: TodoList[]) {
    const fixture = TestBed.createComponent(ListsPage);
    TestBed.tick();
    http.expectOne({ method: 'GET', url: '/api/lists' }).flush(lists);
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  it('shows the lists with links to their tasks', async () => {
    const { element } = await render([work]);

    const link = element.querySelector<HTMLAnchorElement>('.list-title');
    expect(link?.textContent).toContain('Work');
    expect(link?.getAttribute('href')).toBe('/lists/3');
    expect(element.textContent).toContain('Office tasks');
  });

  it('shows an empty state', async () => {
    const { element } = await render([]);
    expect(element.textContent).toContain('You have no lists yet');
  });

  it('shows an error with a retry button when loading fails', async () => {
    const fixture = TestBed.createComponent(ListsPage);
    TestBed.tick();
    http
      .expectOne('/api/lists')
      .flush({ title: 'Server error' }, { status: 500, statusText: 'Error' });
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('[role=alert]')?.textContent).toContain('Server error');
    element.querySelector<HTMLButtonElement>('[role=alert] button')!.click();
    TestBed.tick();
    http.expectOne('/api/lists').flush([work]);
    await fixture.whenStable();
    expect(element.textContent).toContain('Work');
  });

  it('creates a list and reloads', async () => {
    const { fixture, element } = await render([]);
    const input = element.querySelector<HTMLInputElement>('input[formcontrolname=title]')!;
    input.value = 'Home';
    input.dispatchEvent(new Event('input'));

    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    const post = http.expectOne({ method: 'POST', url: '/api/lists' });
    expect(post.request.body).toEqual({ title: 'Home', description: null });
    post.flush({ ...work, id: 4, title: 'Home' });
    TestBed.tick();
    http.expectOne({ method: 'GET', url: '/api/lists' }).flush([{ ...work, id: 4, title: 'Home' }]);
    await fixture.whenStable();

    expect(element.textContent).toContain('Home');
  });

  it('deletes a list after confirmation', async () => {
    const { fixture, element } = await render([work]);

    element.querySelector<HTMLButtonElement>('button[aria-label="Delete Work"]')!.click();
    http.expectOne({ method: 'DELETE', url: '/api/lists/3' }).flush(null);
    TestBed.tick();
    http.expectOne({ method: 'GET', url: '/api/lists' }).flush([]);
    await fixture.whenStable();

    expect(element.textContent).toContain('You have no lists yet');
  });
});
