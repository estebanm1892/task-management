import { Routes } from '@angular/router';
import { TaskFormComponent } from './features/tasks/task-form.component';
import { TaskListComponent } from './features/tasks/task-list.component';
import { UserFormComponent } from './features/users/user-form.component';

export const routes: Routes = [
  { path: 'users', component: UserFormComponent },
  { path: 'tasks', component: TaskFormComponent },
  { path: 'task-list', component: TaskListComponent },
  { path: '', redirectTo: '/users', pathMatch: 'full' },
  { path: '**', redirectTo: '/users' },
];
