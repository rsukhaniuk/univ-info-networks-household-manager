import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';

export const executionsRoutes: Routes = [
  {
    path: 'executions',
    canActivate: [AuthGuard],
    children: [
      {
        path: 'task/:taskId/history',
        loadComponent: () =>
          import('./execution-history/execution-history.component').then(m => m.ExecutionHistoryComponent),
        title: 'Task Execution History'
      },
      {
        path: 'household/:householdId/history',
        loadComponent: () =>
          import('./execution-history/execution-history.component').then(m => m.ExecutionHistoryComponent),
        title: 'Household Execution History'
      }
    ]
  }
];