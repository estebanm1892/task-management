import { HttpClient } from '@angular/common/http';
import { createEnvironmentInjector, EnvironmentInjector } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { appConfig } from './app.config';
import { routes } from './app.routes';
import { TaskApiService } from './core/services/task-api.service';
import { UserApiService } from './core/services/user-api.service';
import { describe, expect, it} from 'vitest';

describe('App', () => {
  it('renders the task management dashboard shell', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes)],
    }).compileComponents();

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('h1')?.textContent).toContain('Task Management');
    expect(element.querySelectorAll('nav a').length).toBeGreaterThanOrEqual(3);
    expect(element.textContent).toContain('Tareas pendientes');
    expect(element.textContent).toContain('En progreso');
    expect(element.textContent).toContain('Completadas');
  });

  it('exposes a responsive shell and accessible interactive names', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes)],
    }).compileComponents();

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const interactiveElements = Array.from(element.querySelectorAll('a, button'));

    expect(element.querySelector('.responsive-shell')).toBeTruthy();
    expect(element.querySelector('nav[aria-label="Navegación principal"]')).toBeTruthy();
    expect(interactiveElements.length).toBeGreaterThan(0);
    expect(interactiveElements.every((item) => (item.getAttribute('aria-label') || item.textContent?.trim()))).toBe(true);
  });

  it('resolves API services through the production application providers', () => {
    TestBed.configureTestingModule({});
    const injector = createEnvironmentInjector(
      appConfig.providers,
      TestBed.inject(EnvironmentInjector),
    );

    expect(injector.get(HttpClient, null, { self: true })).toBeInstanceOf(HttpClient);
    expect(injector.get(UserApiService)).toBeInstanceOf(UserApiService);
    expect(injector.get(TaskApiService)).toBeInstanceOf(TaskApiService);
    injector.destroy();
  });
});
