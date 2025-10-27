import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';

export const routes: Routes = [
  // Public routes
  {
    path: '',
    loadComponent: () => 
      import('./features/home/home.component').then(m => m.HomeComponent),
    title: 'Home - Household Manager'
  },
  
  // Auth0 callback
  {
    path: 'callback',
    loadComponent: () => 
      import('./features/auth/callback/callback.component').then(m => m.CallbackComponent),
    title: 'Signing in...'
  },

  // About page
  {
    path: 'about',
    loadComponent: () => 
      import('./features/about/about.component').then(m => m.AboutComponent),
    title: 'About - Household Manager'
  },

  // Privacy policy
  {
    path: 'privacy',
    loadComponent: () => 
      import('./features/privacy/privacy.component').then(m => m.PrivacyComponent),
    title: 'Privacy Policy - Household Manager'
  },

//   // Protected routes (require authentication)
//   {
//     path: 'dashboard',
//     loadComponent: () => 
//       import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
//     canActivate: [AuthGuard],
//     title: 'Dashboard - Household Manager'
//   },

  {
    path: 'households',
    loadChildren: () => 
      import('./features/households/households.routes').then(m => m.householdsRoutes),
    canActivate: [AuthGuard]
  },

  {
    path: 'rooms',
    loadChildren: () => 
      import('./features/rooms/rooms.routes').then(m => m.roomsRoutes),
    canActivate: [AuthGuard]
  },

//   {
//     path: 'tasks',
//     loadChildren: () => 
//       import('./features/tasks/tasks.routes').then(m => m.tasksRoutes),
//     canActivate: [AuthGuard]
//   },

//   {
//     path: 'executions',
//     loadChildren: () => 
//       import('./features/executions/executions.routes').then(m => m.executionsRoutes),
//     canActivate: [AuthGuard]
//   },

//   {
//     path: 'profile',
//     loadComponent: () => 
//       import('./features/profile/profile.component').then(m => m.ProfileComponent),
//     canActivate: [AuthGuard],
//     title: 'Profile - Household Manager'
//   },

  // Wildcard - redirect to home
  {
    path: '**',
    redirectTo: '',
    pathMatch: 'full'
  }
];