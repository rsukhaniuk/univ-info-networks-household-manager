import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';

export const tasksRoutes: Routes = [
  {
    path: ':householdId',
    canActivate: [AuthGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./task-list/task-list.component').then(m => m.TaskListComponent),
        title: 'Tasks'
      },
      {
        path: 'create',
        loadComponent: () =>
          import('./task-form/task-form.component').then(m => m.TaskFormComponent),
        title: 'Create Task'
      },
      {
        path: ':taskId',
        loadComponent: () =>
          import('./task-details/task-details.component').then(m => m.TaskDetailsComponent),
        title: 'Task Details'
      },
      {
        path: ':taskId/edit',
        loadComponent: () =>
          import('./task-form/task-form.component').then(m => m.TaskFormComponent),
        title: 'Edit Task'
      }
    ]
  }
];