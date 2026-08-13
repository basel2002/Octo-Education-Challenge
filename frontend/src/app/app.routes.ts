import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'builder', pathMatch: 'full' },
  {
    path: 'builder',
    loadComponent: () =>
      import('./builder/builder-page.component').then(m => m.BuilderPageComponent),
  },
  {
    path: 'programs/:id',
    loadComponent: () =>
      import('./viewer/viewer-page.component').then(m => m.ViewerPageComponent),
  },
  { path: '**', redirectTo: 'builder' },
];
