import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TagService } from './tag.service';

describe('TagService', () => {
  let service: TagService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TagService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('calls the tag endpoints', () => {
    service.getAll().subscribe();
    service.create({ name: 'Work', color: '#3E63DD' }).subscribe();
    service.update(1, { name: 'Office', color: '#3E63DD' }).subscribe();
    service.delete(1).subscribe();
    service.assign(5, 1).subscribe();
    service.unassign(5, 1).subscribe();

    expect(http.expectOne({ method: 'GET', url: '/api/tags' })).toBeTruthy();
    expect(http.expectOne({ method: 'POST', url: '/api/tags' })).toBeTruthy();
    expect(http.expectOne({ method: 'PUT', url: '/api/tags/1' })).toBeTruthy();
    expect(http.expectOne({ method: 'DELETE', url: '/api/tags/1' })).toBeTruthy();
    expect(http.expectOne({ method: 'PUT', url: '/api/tasks/5/tags/1' })).toBeTruthy();
    expect(http.expectOne({ method: 'DELETE', url: '/api/tasks/5/tags/1' })).toBeTruthy();
  });
});
