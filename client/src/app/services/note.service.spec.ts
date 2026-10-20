import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NoteService } from './note.service';

describe('NoteService', () => {
  let service: NoteService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(NoteService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('calls the note endpoints', () => {
    service.getForTask(5).subscribe();
    service.create(5, { content: 'Hello' }).subscribe();
    service.update(21, { content: 'Edited' }).subscribe();
    service.delete(21).subscribe();

    expect(http.expectOne({ method: 'GET', url: '/api/tasks/5/notes' })).toBeTruthy();
    expect(http.expectOne({ method: 'POST', url: '/api/tasks/5/notes' }).request.body).toEqual({
      content: 'Hello',
    });
    expect(http.expectOne({ method: 'PUT', url: '/api/notes/21' })).toBeTruthy();
    expect(http.expectOne({ method: 'DELETE', url: '/api/notes/21' })).toBeTruthy();
  });
});
