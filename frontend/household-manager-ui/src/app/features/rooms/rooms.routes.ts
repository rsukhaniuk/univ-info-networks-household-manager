import { Routes } from '@angular/router';

export const roomsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./room-list/room-list.component').then(m => m.RoomListComponent),
    title: 'Rooms'
  },
  {
    path: 'create',
    loadComponent: () =>
      import('./room-form/room-form.component').then(m => m.RoomFormComponent),
    title: 'Create Room'
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./room-details/room-details.component').then(m => m.RoomDetailsComponent),
    title: 'Room Details'
  },
  {
    path: ':id/edit',
    loadComponent: () =>
      import('./room-form/room-form.component').then(m => m.RoomFormComponent),
    title: 'Edit Room'
  }
];