import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { TaskApiService } from '../../core/services/task-api.service';
import { TaskListComponent } from './task-list.component';

describe('TaskListComponent', () => {
  let fixture: ComponentFixture<TaskListComponent>;
  let component: TaskListComponent;
  let service: { listTasks: ReturnType<typeof vi.fn>; updateTaskStatus: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    service = { listTasks: vi.fn(), updateTaskStatus: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [TaskListComponent],
      providers: [{ provide: TaskApiService, useValue: service }],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskListComponent);
    component = fixture.componentInstance;
  });

  it('loads tasks and shows empty/error states', () => {
    service.listTasks.mockReturnValue(of([]));
    component.ngOnInit();
    expect(component.tasks()).toEqual([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="empty-state"]')?.textContent).toContain('No hay tareas');

    service.listTasks.mockReturnValue(throwError(() => new Error('No se pudieron cargar')));
    component.loadTasks();
    expect(component.errorMessage()).toContain('No se pudieron cargar');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('No se pudieron cargar');
    expect(fixture.nativeElement.querySelector('button[data-testid="retry-tasks"]')).toBeTruthy();
  });

  it('renders each task as a card with essential information', () => {
    const task = { id: 7, title: 'Build API', status: 'Pending' as const, userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' };
    service.listTasks.mockReturnValue(of([task]));

    fixture.detectChanges();

    const card = fixture.nativeElement.querySelector('[data-testid="task-card"]');
    expect(card).toBeTruthy();
    expect(card.textContent).toContain('Build API');
    expect(card.textContent).toContain('Pending');
    expect(card.textContent).toContain('Usuario #2');
    expect(card.textContent).toContain('19/09/2026');
    expect(card.textContent).toContain('High');
    expect(card.classList.contains('status-pending')).toBe(true);
  });

  it('renders compact state and priority filters', () => {
    service.listTasks.mockReturnValue(of([]));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="task-filters"]')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Estado');
    expect(fixture.nativeElement.textContent).toContain('Prioridad');
  });

  it('filters and updates task status', () => {
    const task = { id: 7, title: 'Build API', status: 'Pending' as const, userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' };
    service.listTasks.mockReturnValue(of([task]));
    fixture.detectChanges();

    const selects = fixture.nativeElement.querySelectorAll('select') as NodeListOf<HTMLSelectElement>;
    selects[0].value = 'Pending';
    selects[0].dispatchEvent(new Event('change'));
    selects[1].value = 'High';
    selects[1].dispatchEvent(new Event('change'));
    expect(service.listTasks).toHaveBeenCalledWith({ status: 'Pending', priority: 'High' });

    selects[0].value = '';
    selects[0].dispatchEvent(new Event('change'));
    selects[1].value = 'Low';
    selects[1].dispatchEvent(new Event('change'));
    expect(service.listTasks).toHaveBeenCalledWith({ status: undefined, priority: 'Low' });

    service.updateTaskStatus.mockReturnValue(of({ ...task, status: 'InProgress' as const }));
    component.advanceStatus(task);
    expect(service.updateTaskStatus).toHaveBeenCalledWith(7, 'InProgress');
  });

  it('shows the next status action and advances through the workflow', () => {
    const pendingTask = { id: 7, title: 'Build API', status: 'Pending' as const, userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' };
    service.listTasks.mockReturnValue(of([pendingTask]));
    service.updateTaskStatus.mockReturnValue(of({ ...pendingTask, status: 'InProgress' as const }));
    fixture.detectChanges();

    const advanceButton = fixture.nativeElement.querySelector('[data-testid="advance-task"]') as HTMLButtonElement;
    expect(advanceButton.textContent).toContain('InProgress');
    advanceButton.click();

    expect(service.updateTaskStatus).toHaveBeenCalledWith(7, 'InProgress');
    expect(component.tasks()[0].status).toBe('InProgress');
  });

  it('shows a transition error without changing the task', () => {
    const task = { id: 7, title: 'Build API', status: 'Pending' as const, userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' };
    service.listTasks.mockReturnValue(of([task]));
    service.updateTaskStatus.mockReturnValue(throwError(() => new Error('Transición no permitida')));
    fixture.detectChanges();

    component.advanceStatus(task);
    fixture.detectChanges();

    expect(component.tasks()[0].status).toBe('Pending');
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('Transición no permitida');
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('No se pudo cambiar el estado');
  });

  it('opens a task detail panel without losing list context', () => {
    const task = { id: 7, title: 'Build API', status: 'Pending' as const, userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' };
    service.listTasks.mockReturnValue(of([task]));
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('[data-testid="task-details"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    const panel = fixture.nativeElement.querySelector('[data-testid="task-detail-panel"]');
    expect(panel.textContent).toContain('Build API');
    expect(panel.textContent).toContain('Usuario #2');
    expect(panel.textContent).toContain('High');
    expect(panel.textContent).toContain('Pending');
    expect(fixture.nativeElement.querySelectorAll('[data-testid="task-card"]').length).toBe(1);
  });

  it('keeps task actions named and keyboard focusable', () => {
    const task = { id: 7, title: 'Build API', status: 'Pending' as const, userId: 2, createdAt: '2026-09-19T00:00:00Z', additionalInfo: '{"priority":"High"}' };
    service.listTasks.mockReturnValue(of([task]));
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    expect(buttons.every((button) => button.textContent?.trim() || button.getAttribute('aria-label'))).toBe(true);
    buttons[0].focus();
    expect(document.activeElement).toBe(buttons[0]);
  });
});
