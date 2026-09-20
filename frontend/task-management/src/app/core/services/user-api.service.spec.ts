import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { UserApiService } from './user-api.service';

describe('UserApiService', () => {
  let service: UserApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(UserApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates and lists users using the API contract', () => {
    const created = { id: 1, name: 'Ana', email: 'ana@example.com' };

    service.createUser({ name: 'Ana', email: 'ana@example.com' }).subscribe((user) => {
      expect(user).toEqual(created);
    });

    const createRequest = http.expectOne('/api/users');
    expect(createRequest.request.method).toBe('POST');
    createRequest.flush(created);

    service.listUsers().subscribe((users) => {
      expect(users).toEqual([created]);
    });

    const listRequest = http.expectOne('/api/users');
    expect(listRequest.request.method).toBe('GET');
    listRequest.flush([created]);
  });
});
