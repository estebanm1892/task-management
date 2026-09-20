import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateUserRequest, UserModel } from '../../shared/models/user.model';

@Injectable({ providedIn: 'root' })
export class UserApiService {
  constructor(private readonly http: HttpClient) {}

  createUser(request: CreateUserRequest): Observable<UserModel> {
    return this.http.post<UserModel>('/api/users', request);
  }

  listUsers(): Observable<UserModel[]> {
    return this.http.get<UserModel[]>('/api/users');
  }
}
