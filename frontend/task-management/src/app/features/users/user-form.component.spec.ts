import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { UserApiService } from '../../core/services/user-api.service';
import { UserFormComponent } from './user-form.component';
import { beforeEach, describe, expect, it, vi } from 'vitest';

describe('UserFormComponent', () => {
  let fixture: ComponentFixture<UserFormComponent>;
  let component: UserFormComponent;
  let service: { createUser: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    service = { createUser: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [UserFormComponent, ReactiveFormsModule],
      providers: [{ provide: UserApiService, useValue: service }],
    }).compileComponents();

    fixture = TestBed.createComponent(UserFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('validates the form and creates a user', () => {
    service.createUser.mockReturnValue(of({ id: 1, name: 'Ana', email: 'ana@example.com' }));

    component.form.setValue({ name: 'Ana', email: 'ana@example.com' });
    component.submit();

    expect(service.createUser).toHaveBeenCalledWith({ name: 'Ana', email: 'ana@example.com' });
    expect(component.errorMessage()).toBeFalsy();
  });

  it('renders labeled fields and loading feedback while saving', () => {
    service.createUser.mockReturnValue(of({ id: 1, name: 'Ana', email: 'ana@example.com' }));

    expect(fixture.nativeElement.querySelector('label[for="user-name"]')?.textContent).toContain('Nombre');
    expect(fixture.nativeElement.querySelector('label[for="user-email"]')?.textContent).toContain('Correo electrónico');

    component.form.setValue({ name: 'Ana', email: 'ana@example.com' });
    component.submitting.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Guardando usuario');
    expect(fixture.nativeElement.querySelector('button[type="submit"]')?.disabled).toBe(true);
  });

  it('shows validation and api errors', () => {
    component.form.setValue({ name: '', email: 'bad-email' });
    component.submit();

    expect(component.form.invalid).toBe(true);
    expect(component.errorMessage()).toContain('Completa');

    service.createUser.mockReturnValue(throwError(() => new Error('Email duplicado')));
    component.form.setValue({ name: 'Ana', email: 'ana@example.com' });
    component.submit();

    expect(component.errorMessage()).toContain('Email duplicado');
  });

  it('shows inline messages for invalid controls', () => {
    component.form.controls.name.markAsTouched();
    component.form.controls.email.markAsTouched();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('El nombre es obligatorio');
    expect(fixture.nativeElement.textContent).toContain('Introduce un correo válido');
  });
});
