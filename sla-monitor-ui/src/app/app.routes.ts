import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard';
import { IncidentsComponent } from './incidents/incidents';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'incidents', component: IncidentsComponent },
  { path: '**', redirectTo: 'dashboard' },
];