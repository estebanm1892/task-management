import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserApiService } from '../../core/services/user-api.service';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <form class="task-form" [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <div class="form-field">
        <label for="user-name">Nombre</label>
        <input id="user-name" formControlName="name" placeholder="Ej. Ana García" [attr.aria-invalid]="form.controls.name.invalid && form.controls.name.touched" />
        @if (form.controls.name.touched && form.controls.name.hasError('required')) {
          <small class="field-error">El nombre es obligatorio</small>
        }
      </div>
      <div class="form-field">
        <label for="user-email">Correo electrónico</label>
        <input id="user-email" type="email" formControlName="email" placeholder="ana@empresa.com" [attr.aria-invalid]="form.controls.email.invalid && form.controls.email.touched" />
        @if (form.controls.email.touched && form.controls.email.invalid) {
          <small class="field-error">Introduce un correo válido</small>
        }
      </div>
      <button type="submit" [disabled]="form.invalid || submitting()">
        {{ submitting() ? 'Guardando usuario' : 'Guardar usuario' }}
      </button>
    </form>
    @if (errorMessage()) {
      <p class="form-error" role="alert">{{ errorMessage() }}</p>
    }
  `,
})
export class UserFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly usersApi = inject(UserApiService);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
  });

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Completa nombre y correo válidos.');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.usersApi.createUser(this.form.getRawValue()).subscribe({
      next: () => this.submitting.set(false),
      error: (error: Error) => {
        this.submitting.set(false);
        this.errorMessage.set(error.message || 'No se pudo crear el usuario.');
      },
    });
  }
}
