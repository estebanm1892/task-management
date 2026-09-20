import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';

describe('App routes', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes)],
    });
  });

  it('registers the user and tasks routes', () => {
    expect(routes.some((route) => route.path === 'users')).toBe(true);
    expect(routes.some((route) => route.path === 'tasks')).toBe(true);
  });
});
