export type TaskStatus = 'Pending' | 'InProgress' | 'Done';

export interface TaskModel {
  id: number;
  title: string;
  status: TaskStatus;
  userId: number;
  createdAt: string;
  additionalInfo?: string | null;
}

export interface CreateTaskRequest {
  title: string;
  userId: number;
  additionalInfo?: string | null;
}

export interface TaskFilters {
  userId?: number;
  status?: string;
  priority?: string;
}
