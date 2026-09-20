import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TaskApiService } from './task-api.service';

describe('TaskApiService', () => {
  let service: TaskApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(TaskApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates, filters and updates tasks through the REST contract', () => {
    const task = { id: 7, title: 'Build API', status: 'Pending', userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' };

    service.createTask({ title: 'Build API', userId: 2, additionalInfo: '{"priority":"High"}' }).subscribe((value) => {
      expect(value).toEqual(task);
    });

    const createRequest = http.expectOne('/api/tasks');
    expect(createRequest.request.method).toBe('POST');
    createRequest.flush(task);

    service.listTasks({ status: 'Pending', priority: 'High' }).subscribe((tasks) => {
      expect(tasks).toEqual([task]);
    });

    const listRequest = http.expectOne((request) => request.url === '/api/tasks' && request.params.get('status') === 'Pending' && request.params.get('priority') === 'High');
    expect(listRequest.request.method).toBe('GET');
    listRequest.flush([task]);

    service.updateTaskStatus(7, 'InProgress').subscribe((updated) => {
      expect(updated.status).toBe('InProgress');
    });

    const updateRequest = http.expectOne('/api/tasks/7/status');
    expect(updateRequest.request.method).toBe('PUT');
    updateRequest.flush({ ...task, status: 'InProgress' });
  });
});
