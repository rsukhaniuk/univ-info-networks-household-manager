import { Routes } from '@angular/router';

export const householdsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./household-list/household-list.component').then(m => m.HouseholdListComponent),
    title: 'My Households'
  },
  {
    path: 'create',
    loadComponent: () =>
      import('./household-form/household-form.component').then(m => m.HouseholdFormComponent),
    title: 'Create Household'
  },
  {
    path: 'join',
    loadComponent: () =>
      import('./join-household/join-household.component').then(m => m.JoinHouseholdComponent),
    title: 'Join Household'
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./household-details/household-details.component').then(m => m.HouseholdDetailsComponent),
    title: 'Household Details'
  },
  {
    path: ':id/edit',
    loadComponent: () =>
      import('./household-form/household-form.component').then(m => m.HouseholdFormComponent),
    title: 'Edit Household'
  }
];