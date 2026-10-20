import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TaskService } from './task.service';

describe('TaskService', () => {
  let service: TaskService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TaskService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends only the filters that are set', () => {
    service.getForList(3, { status: 'Done', priority: null, tagId: 4 }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/lists/3/tasks');
    expect(req.request.params.get('status')).toBe('Done');
    expect(req.request.params.get('tagId')).toBe('4');
    expect(req.request.params.has('priority')).toBe(false);
  });

  it('calls the task endpoints', () => {
    service.create(3, { title: 'New' }).subscribe();
    service.get(5).subscribe();
    service
      .update(5, { title: 'T', description: null, status: 'Todo', priority: 1, dueDate: null })
      .subscribe();
    service.updateStatus(5, 'InProgress').subscribe();
    service.delete(5).subscribe();

    expect(http.expectOne({ method: 'POST', url: '/api/lists/3/tasks' })).toBeTruthy();
    expect(http.expectOne({ method: 'GET', url: '/api/tasks/5' })).toBeTruthy();
    expect(http.expectOne({ method: 'PUT', url: '/api/tasks/5' })).toBeTruthy();
    expect(http.expectOne({ method: 'PATCH', url: '/api/tasks/5/status' }).request.body).toEqual({
      status: 'InProgress',
    });
    expect(http.expectOne({ method: 'DELETE', url: '/api/tasks/5' })).toBeTruthy();
  });
});
