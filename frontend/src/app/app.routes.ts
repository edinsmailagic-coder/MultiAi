import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./pages/landing/landing').then((m) => m.Landing),
  },
  {
    path: 'overview',
    loadComponent: () => import('./pages/overview/overview').then((m) => m.Overview),
  },
  {
    path: 'docs',
    loadComponent: () => import('./pages/docs/docs').then((m) => m.Docs),
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/register/register').then((m) => m.Register),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/app-shell/app-shell').then((m) => m.AppShell),
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard) },
      { path: 'chat', loadComponent: () => import('./pages/chat/chat').then((m) => m.Chat) },
      {
        path: 'chat/:conversationId',
        loadComponent: () => import('./pages/chat/chat').then((m) => m.Chat),
      },
      {
        path: 'workflows',
        loadComponent: () => import('./pages/workflows/workflow-list/workflow-list').then((m) => m.WorkflowList),
      },
      {
        path: 'workflows/:id',
        loadComponent: () => import('./pages/workflows/workflow-editor/workflow-editor').then((m) => m.WorkflowEditor),
      },
      { path: 'profile', loadComponent: () => import('./pages/profile/profile').then((m) => m.Profile) },
      { path: 'providers', loadComponent: () => import('./pages/providers/providers').then((m) => m.Providers) },
      { path: 'settings', loadComponent: () => import('./pages/settings/settings').then((m) => m.Settings) },
    ],
  },
  { path: '**', redirectTo: '' },
];
