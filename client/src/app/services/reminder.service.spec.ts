import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ReminderService } from './reminder.service';

describe('ReminderService', () => {
  let service: ReminderService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ReminderService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('calls the reminder endpoints', () => {
    service.getForTask(5).subscribe();
    service.create(5, { remindAt: '2026-10-31T09:00:00.000Z' }).subscribe();
    service.delete(31).subscribe();
    service.getDue('2026-10-31T10:00:00.000Z').subscribe();

    expect(http.expectOne({ method: 'GET', url: '/api/tasks/5/reminders' })).toBeTruthy();
    expect(http.expectOne({ method: 'POST', url: '/api/tasks/5/reminders' }).request.body).toEqual({
      remindAt: '2026-10-31T09:00:00.000Z',
    });
    expect(http.expectOne({ method: 'DELETE', url: '/api/reminders/31' })).toBeTruthy();
    const due = http.expectOne((r) => r.method === 'GET' && r.url === '/api/reminders');
    expect(due.request.params.get('before')).toBe('2026-10-31T10:00:00.000Z');
  });
});
