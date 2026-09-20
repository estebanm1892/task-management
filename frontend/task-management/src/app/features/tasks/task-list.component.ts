import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { TaskApiService } from '../../core/services/task-api.service';
import { TaskModel, TaskStatus } from '../../shared/models/task.model';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="task-list" aria-labelledby="task-list-title">
      <div class="task-list-header">
        <div>
          <p class="eyebrow">Seguimiento operativo</p>
          <h2 id="task-list-title">Tareas del equipo</h2>
        </div>
        <span class="task-count">{{ tasks().length }} tareas</span>
      </div>

      <div class="task-filters" data-testid="task-filters">
        <label for="status-filter">
          Estado
          <select id="status-filter" [value]="filter()" (change)="onFilterChange($any($event.target).value)">
          <option value="">Todos</option>
          <option value="Pending">Pending</option>
          <option value="InProgress">InProgress</option>
          <option value="Done">Done</option>
          </select>
        </label>
        <label for="priority-filter">
          Prioridad
          <select id="priority-filter" [value]="priorityFilter()" (change)="onPriorityFilterChange($any($event.target).value)">
          <option value="">Todas</option>
          <option value="Low">Low</option>
          <option value="Medium">Medium</option>
          <option value="High">High</option>
          </select>
        </label>
      </div>

      @if (errorMessage()) {
        <div class="list-error" role="alert">
          <strong>No se pudo actualizar el listado</strong>
          <p>{{ errorMessage() }}</p>
          <button type="button" data-testid="retry-tasks" (click)="loadTasks()">Reintentar</button>
        </div>
      } @else if (transitionErrorMessage()) {
        <div class="list-error transition-error" role="alert">
          <strong>No se pudo cambiar el estado</strong>
          <p>{{ transitionErrorMessage() }}</p>
          <button type="button" (click)="transitionErrorMessage.set(null)">Cerrar</button>
        </div>
      } @else if (tasks().length === 0) {
        <div class="empty-state" data-testid="empty-state">
          <strong>No hay tareas</strong>
          <p>Cuando se creen tareas aparecerán aquí para su seguimiento.</p>
        </div>
      } @else {
        <div class="task-card-grid">
          @for (task of tasks(); track task.id) {
            <article class="task-card" [class]="'status-' + task.status.toLowerCase()" data-testid="task-card">
              <div class="task-card-topline">
                <span class="status-pill">{{ task.status }}</span>
                <span class="priority-pill">{{ priorityFor(task) }}</span>
              </div>
              <h3>{{ task.title }}</h3>
              <div class="task-card-meta">
                <span>Responsable: Usuario #{{ task.userId }}</span>
                <time [attr.datetime]="task.createdAt">{{ formatDate(task.createdAt) }}</time>
              </div>
              <div class="task-card-actions">
                <button type="button" data-testid="task-details" (click)="selectTask(task)">Ver detalle</button>
                @if (task.status !== 'Done') {
                  <button type="button" data-testid="advance-task" (click)="advanceStatus(task)">Avanzar a {{ nextStatus(task) }}</button>
                }
              </div>
            </article>
          }
        </div>
      }

      @if (selectedTask(); as task) {
        <aside class="task-detail-panel" data-testid="task-detail-panel" aria-labelledby="task-detail-title">
          <div class="detail-header">
            <div>
              <p class="eyebrow">Detalle de tarea</p>
              <h3 id="task-detail-title">{{ task.title }}</h3>
            </div>
            <button type="button" class="detail-close" (click)="selectedTask.set(null)" aria-label="Cerrar detalle">Cerrar</button>
          </div>
          <dl>
            <div><dt>Estado</dt><dd>{{ task.status }}</dd></div>
            <div><dt>Responsable</dt><dd>Usuario #{{ task.userId }}</dd></div>
            <div><dt>Prioridad</dt><dd>{{ priorityFor(task) }}</dd></div>
            <div><dt>Creada</dt><dd>{{ formatDate(task.createdAt) }}</dd></div>
          </dl>
        </aside>
      }
    </section>
  `,
})
export class TaskListComponent {
  private readonly tasksApi = inject(TaskApiService);

  readonly tasks = signal<TaskModel[]>([]);
  readonly filter = signal('');
  readonly priorityFilter = signal('');
  readonly errorMessage = signal<string | null>(null);
  readonly transitionErrorMessage = signal<string | null>(null);
  readonly selectedTask = signal<TaskModel | null>(null);

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.tasksApi.listTasks({
      status: this.filter() || undefined,
      priority: this.priorityFilter() || undefined,
    }).subscribe({
      next: (items) => {
        this.tasks.set(items);
        this.errorMessage.set(null);
      },
      error: (error: Error) => this.errorMessage.set(error.message || 'No se pudieron cargar las tareas.'),
    });
  }

  applyFilters(): void {
    this.loadTasks();
  }

  onFilterChange(value: string): void {
    this.filter.set(value);
    this.applyFilters();
  }

  onPriorityFilterChange(value: string): void {
    this.priorityFilter.set(value);
    this.applyFilters();
  }

  priorityFor(task: TaskModel): string {
    if (!task.additionalInfo) {
      return 'Sin prioridad';
    }

    try {
      const additionalInfo = JSON.parse(task.additionalInfo) as Record<string, unknown>;
      return typeof additionalInfo['priority'] === 'string' ? additionalInfo['priority'] : 'Sin prioridad';
    } catch {
      return 'Sin prioridad';
    }
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat('es-ES', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      timeZone: 'UTC',
    }).format(new Date(value));
  }

  nextStatus(task: TaskModel): TaskStatus {
    return task.status === 'Pending' ? 'InProgress' : 'Done';
  }

  selectTask(task: TaskModel): void {
    this.selectedTask.set(task);
  }

  advanceStatus(task: TaskModel): void {
    this.transitionErrorMessage.set(null);
    this.tasksApi.updateTaskStatus(task.id, this.nextStatus(task)).subscribe({
      next: (updated) => {
        this.tasks.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
      },
      error: (error: Error) => this.transitionErrorMessage.set(error.message || 'La transición solicitada no está permitida.'),
    });
  }
}
