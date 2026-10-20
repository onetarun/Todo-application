import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TodoListService } from './todo-list.service';

describe('TodoListService', () => {
  let service: TodoListService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TodoListService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('calls the lists endpoints', () => {
    service.getAll().subscribe();
    service.get(3).subscribe();
    service.create({ title: 'Home' }).subscribe();
    service.update(3, { title: 'Work', description: null }).subscribe();
    service.delete(3).subscribe();

    expect(http.expectOne({ method: 'GET', url: '/api/lists' })).toBeTruthy();
    expect(http.expectOne({ method: 'GET', url: '/api/lists/3' })).toBeTruthy();
    expect(http.expectOne({ method: 'POST', url: '/api/lists' }).request.body).toEqual({
      title: 'Home',
    });
    expect(http.expectOne({ method: 'PUT', url: '/api/lists/3' }).request.body).toEqual({
      title: 'Work',
      description: null,
    });
    expect(http.expectOne({ method: 'DELETE', url: '/api/lists/3' })).toBeTruthy();
  });
});
