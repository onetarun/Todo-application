import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SubtaskService } from './subtask.service';

describe('SubtaskService', () => {
  let service: SubtaskService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SubtaskService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('calls the subtask endpoints', () => {
    service.getForTask(5).subscribe();
    service.create(5, { title: 'Step' }).subscribe();
    service.update(11, { title: 'Step', isCompleted: false }).subscribe();
    service.complete(11).subscribe();
    service.delete(11).subscribe();

    expect(http.expectOne({ method: 'GET', url: '/api/tasks/5/subtasks' })).toBeTruthy();
    expect(http.expectOne({ method: 'POST', url: '/api/tasks/5/subtasks' })).toBeTruthy();
    expect(http.expectOne({ method: 'PUT', url: '/api/subtasks/11' }).request.body).toEqual({
      title: 'Step',
      isCompleted: false,
    });
    expect(http.expectOne({ method: 'PATCH', url: '/api/subtasks/11/complete' })).toBeTruthy();
    expect(http.expectOne({ method: 'DELETE', url: '/api/subtasks/11' })).toBeTruthy();
  });
});
