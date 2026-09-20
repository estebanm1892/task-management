import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateTaskRequest, TaskFilters, TaskModel } from '../../shared/models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskApiService {
  constructor(private readonly http: HttpClient) {}

  createTask(request: CreateTaskRequest): Observable<TaskModel> {
    return this.http.post<TaskModel>('/api/tasks', request);
  }

  listTasks(filters: TaskFilters = {}): Observable<TaskModel[]> {
    let params = new HttpParams();

    if (filters.userId !== undefined) {
      params = params.set('userId', String(filters.userId));
    }
    if (filters.status) {
      params = params.set('status', filters.status);
    }
    if (filters.priority) {
      params = params.set('priority', filters.priority);
    }

    return this.http.get<TaskModel[]>('/api/tasks', { params });
  }

  updateTaskStatus(taskId: number, status: string): Observable<TaskModel> {
    return this.http.put<TaskModel>(`/api/tasks/${taskId}/status`, { status });
  }
}
