import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TaskApiService } from '../../core/services/task-api.service';
import { UserApiService } from '../../core/services/user-api.service';
import { TaskFormComponent } from './task-form.component';

describe('TaskFormComponent', () => {
  let fixture: ComponentFixture<TaskFormComponent>;
  let component: TaskFormComponent;
  let service: { createTask: ReturnType<typeof vi.fn> };
  let usersService: { listUsers: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    service = { createTask: vi.fn() };
    usersService = { listUsers: vi.fn().mockReturnValue(of([
      { id: 2, name: 'Ana', email: 'ana@example.com' },
      { id: 3, name: 'Luis', email: 'luis@example.com' },
    ])) };

    await TestBed.configureTestingModule({
      imports: [TaskFormComponent, ReactiveFormsModule],
      providers: [
        { provide: TaskApiService, useValue: service },
        { provide: UserApiService, useValue: usersService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads collaborators into a reactive assignment select', () => {
    const select = fixture.nativeElement.querySelector('select[formControlName="userId"]') as HTMLSelectElement;

    expect(usersService.listUsers).toHaveBeenCalledOnce();
    expect(Array.from(select.options).map((option) => option.text.trim())).toEqual([
      'Selecciona un responsable',
      'Ana',
      'Luis',
    ]);

    select.value = select.options[2].value;
    select.dispatchEvent(new Event('change'));
    expect(component.form.controls.userId.value).toBe(3);
  });

  it('validates title, user assignment and additional info', () => {
    service.createTask.mockReturnValue(of({ id: 1, title: 'Build API', status: 'Pending', userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' }));

    component.form.setValue({ title: 'Build API', userId: 2, priority: 'Medium', additionalInfo: '{"priority":"High"}' });
    component.submit();

    expect(service.createTask).toHaveBeenCalledWith({ title: 'Build API', userId: 2, additionalInfo: '{"priority":"High"}' });
    expect(component.errorMessage()).toBeFalsy();

    component.form.setValue({ title: '', userId: 0, priority: 'Medium', additionalInfo: 'bad-json' });
    component.submit();

    expect(component.form.invalid).toBe(true);
    expect(component.errorMessage()).toContain('Completa');
  });

  it('renders task fields, priority options and loading feedback', () => {
    service.createTask.mockReturnValue(of({ id: 1, title: 'Build API', status: 'Pending', userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' }));

    expect(fixture.nativeElement.querySelector('label[for="task-title"]')?.textContent).toContain('Título');
    expect(fixture.nativeElement.querySelector('label[for="task-user"]')?.textContent).toContain('Responsable');

    const priority = fixture.nativeElement.querySelector('select[formControlName="priority"]') as HTMLSelectElement;
    expect(Array.from(priority.options).map((option) => option.text.trim())).toEqual(['Baja', 'Media', 'Alta']);

    component.submitting.set(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Guardando tarea');
  });

  it('includes the selected priority in additional info when creating a task', () => {
    service.createTask.mockReturnValue(of({ id: 1, title: 'Build API', status: 'Pending', userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' }));

    component.form.setValue({ title: 'Build API', userId: 2, priority: 'High', additionalInfo: '' });
    component.submit();

    expect(service.createTask).toHaveBeenCalledWith({
      title: 'Build API',
      userId: 2,
      additionalInfo: '{"priority":"High"}',
    });
  });

  it('shows inline messages for invalid task controls', () => {
    component.form.controls.title.markAsTouched();
    component.form.controls.userId.markAsTouched();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('El título es obligatorio');
    expect(fixture.nativeElement.textContent).toContain('Selecciona un responsable');
  });

  it('shows task api errors', () => {
    service.createTask.mockReturnValue(throwError(() => new Error('Usuario no encontrado')));
    component.form.setValue({ title: 'Build API', userId: 99, priority: 'Medium', additionalInfo: '{"priority":"Medium"}' });
    component.submit();

    expect(component.errorMessage()).toContain('Usuario no encontrado');
  });

  it('shows the backend ProblemDetails detail instead of a generic HTTP error', () => {
    service.createTask.mockReturnValue(throwError(() => ({
      message: 'Http failure response for http://localhost:4200/api/tasks: 400 Bad Request',
      error: { detail: 'La información adicional debe contener JSON válido.' },
    })));

    component.form.setValue({ title: 'Build API', userId: 2, priority: 'Medium', additionalInfo: '{invalid' });
    component.submit();

    expect(component.errorMessage()).toBe('La información adicional debe contener JSON válido.');
  });

  it('rejects malformed additional JSON before calling the API', () => {
    component.form.setValue({ title: 'Build API', userId: 2, priority: 'Medium', additionalInfo: '{invalid' });
    component.submit();

    expect(service.createTask).not.toHaveBeenCalled();
    expect(component.errorMessage()).toContain('JSON válido');
  });
});
