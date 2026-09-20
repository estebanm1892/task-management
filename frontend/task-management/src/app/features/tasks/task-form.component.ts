import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TaskApiService } from '../../core/services/task-api.service';
import { UserApiService } from '../../core/services/user-api.service';
import { UserModel } from '../../shared/models/user.model';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <form class="task-form" [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div class="form-field">
        <label for="task-title">Título</label>
        <input id="task-title" formControlName="title" placeholder="Ej. Preparar informe semanal" [attr.aria-invalid]="form.controls.title.invalid && form.controls.title.touched" />
        @if (form.controls.title.touched && form.controls.title.hasError('required')) {
          <small class="field-error">El título es obligatorio</small>
        }
      </div>
      <div class="form-field">
        <label for="task-user">Responsable</label>
        <select id="task-user" formControlName="userId" [attr.aria-invalid]="form.controls.userId.invalid && form.controls.userId.touched">
          <option [ngValue]="0">Selecciona un responsable</option>
        @for (user of users(); track user.id) {
          <option [ngValue]="user.id">{{ user.name }}</option>
        }
        </select>
        @if (form.controls.userId.touched && form.controls.userId.invalid) {
          <small class="field-error">Selecciona un responsable</small>
        }
      </div>
      <div class="form-field">
        <label for="task-priority">Prioridad</label>
        <select id="task-priority" formControlName="priority">
          <option value="Low">Baja</option>
          <option value="Medium">Media</option>
          <option value="High">Alta</option>
        </select>
      </div>
      <div class="form-field">
        <label for="task-additional-info">Información adicional (Formato JSON)</label>
        <textarea id="task-additional-info" formControlName="additionalInfo" placeholder="Añade contexto para el equipo"></textarea>
      </div>
      <button type="submit" [disabled]="form.invalid || submitting()">
        {{ submitting() ? 'Guardando tarea' : 'Guardar tarea' }}
      </button>
    </form>
    @if (errorMessage()) {
      <p class="form-error" role="alert">{{ errorMessage() }}</p>
    }
  `,
})
export class TaskFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly tasksApi = inject(TaskApiService);
  private readonly usersApi = inject(UserApiService);

  readonly form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    userId: [0, [Validators.required, Validators.min(1)]],
    priority: ['Medium'],
    additionalInfo: [''],
  });

  readonly submitting = signal(false);
  readonly users = signal<UserModel[]>([]);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.usersApi.listUsers().subscribe({
      next: (users) => this.users.set(users),
      error: (error: Error) => this.errorMessage.set(error.message || 'No se pudieron cargar los colaboradores.'),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Completa título y usuario asignado.');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { title, userId, priority, additionalInfo } = this.form.getRawValue();
    let serializedInfo = additionalInfo.trim();

    if (!serializedInfo) {
      serializedInfo = JSON.stringify({ priority });
    } else {
      try {
        const parsedInfo = JSON.parse(serializedInfo) as Record<string, unknown>;
        if (typeof parsedInfo !== 'object' || parsedInfo === null || Array.isArray(parsedInfo)) {
          this.submitting.set(false);
          this.errorMessage.set('La información adicional debe ser un objeto JSON válido.');
          return;
        }
        if (parsedInfo['priority'] !== undefined &&
            (typeof parsedInfo['priority'] !== 'string' || !['Low', 'Medium', 'High'].includes(parsedInfo['priority']))) {
          this.submitting.set(false);
          this.errorMessage.set('La prioridad debe ser Low, Medium o High.');
          return;
        }
        if (parsedInfo['priority'] === undefined) {
          parsedInfo['priority'] = priority;
          serializedInfo = JSON.stringify(parsedInfo);
        }
      } catch {
        this.submitting.set(false);
        this.errorMessage.set('La información adicional debe contener JSON válido.');
        return;
      }
    }

    this.tasksApi.createTask({ title, userId, additionalInfo: serializedInfo || null }).subscribe({
      next: () => this.submitting.set(false),
      error: (error: Error) => {
        this.submitting.set(false);
        this.errorMessage.set(this.apiErrorMessage(error, 'No se pudo crear la tarea.'));
      },
    });
  }

  private apiErrorMessage(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null) {
      const responseError = (error as { error?: unknown }).error;
      if (typeof responseError === 'object' && responseError !== null &&
          typeof (responseError as { detail?: unknown }).detail === 'string') {
        return (responseError as { detail: string }).detail;
      }
      if (typeof responseError === 'string') {
        return responseError;
      }
    }

    return error instanceof Error && error.message ? error.message : fallback;
  }
}
