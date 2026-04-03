import { Routes } from '@angular/router';
import { incidentsAuthGuard } from './auth/incidents-auth.guard';
import { AuthCallbackComponent } from './auth/auth-callback.component';
import { LoggedOutComponent } from './auth/logged-out.component';

export const routes: Routes = [
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./dashboard/dashboard').then(m => m.DashboardComponent)
  },
  {
    path: 'incidents',
    loadComponent: () =>
      import('./incidents/incidents').then(m => m.IncidentsComponent),
    canActivate: [incidentsAuthGuard]
  },
  {
    path: 'auth/callback',
    component: AuthCallbackComponent
  },
  {
    path: 'logged-out',
    component: LoggedOutComponent
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];