import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TagsPage } from './tags-page';

describe('TagsPage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TagsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists tags and creates a new one', async () => {
    const fixture = TestBed.createComponent(TagsPage);
    TestBed.tick();
    http.expectOne('/api/tags').flush([{ id: 1, name: 'Work', color: '#3E63DD' }]);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.tag-name')?.textContent).toContain('Work');

    fixture.componentInstance['createForm'].setValue({ name: 'Home', color: '#30a46c' });
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    expect(http.expectOne({ method: 'POST', url: '/api/tags' }).request.body).toEqual({
      name: 'Home',
      color: '#30a46c',
    });
  });

  it('rejects an invalid color without calling the API', async () => {
    const fixture = TestBed.createComponent(TagsPage);
    TestBed.tick();
    http.expectOne('/api/tags').flush([]);
    await fixture.whenStable();

    fixture.componentInstance['createForm'].setValue({ name: 'Bad', color: 'blue' });
    (fixture.nativeElement as HTMLElement)
      .querySelector('form')!
      .dispatchEvent(new Event('submit'));

    http.expectNone({ method: 'POST', url: '/api/tags' });
  });
});
