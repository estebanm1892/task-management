import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterOutlet, RouterLink],
  selector: 'app-root',
  template: `
    <div class="app-shell responsive-shell">
      <header class="topbar">
        <div class="brand-block">
          <span class="brand-badge">TM</span>
          <div>
            <p class="eyebrow">Gestión operativa</p>
            <h1>Task Management</h1>
          </div>
        </div>

        <nav class="main-nav" aria-label="Navegación principal">
          <a routerLink="/users">Usuarios</a>
          <a routerLink="/tasks">Crear tarea</a>
          <a routerLink="/task-list">Listado</a>
        </nav>
      </header>

      <main class="dashboard">
        <section class="content-panel">
          <div class="panel-header">
            <h2>Panel de trabajo</h2>
          </div>
          <router-outlet />
        </section>
      </main>
    </div>
  `,
})
export class App {}
