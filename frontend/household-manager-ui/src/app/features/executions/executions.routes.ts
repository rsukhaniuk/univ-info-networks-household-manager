import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';

export const executionsRoutes: Routes = [
  {
    path: 'task/:taskId/history',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./execution-history/execution-history.component').then(m => m.ExecutionHistoryComponent),
    title: 'Task Execution History'
  },
  {
    path: 'household/:householdId/history',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./execution-history/execution-history.component').then(m => m.ExecutionHistoryComponent),
    title: 'Household Execution History'
  }
];