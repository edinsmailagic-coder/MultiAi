import { Routes } from '@angular/router';
import { AppPermissions } from './core/auth/permissions';
import { authGuard, permissionGuard } from './core/guards/auth.guard';
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
    path: 'contact',
    loadComponent: () => import('./pages/contact/contact').then((m) => m.Contact),
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
      {
        path: 'dashboard',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.UsageRead },
        loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'chat',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.ChatRead },
        loadComponent: () => import('./pages/chat/chat').then((m) => m.Chat),
      },
      {
        path: 'chat/:conversationId',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.ChatRead },
        loadComponent: () => import('./pages/chat/chat').then((m) => m.Chat),
      },
      {
        path: 'workflows',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.WorkflowsRead },
        loadComponent: () => import('./pages/workflows/workflow-list/workflow-list').then((m) => m.WorkflowList),
      },
      {
        path: 'workflows/:id',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.WorkflowsRead },
        loadComponent: () => import('./pages/workflows/workflow-editor/workflow-editor').then((m) => m.WorkflowEditor),
      },
      {
        path: 'profile',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.AuthMe },
        loadComponent: () => import('./pages/profile/profile').then((m) => m.Profile),
      },
      {
        path: 'providers',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.ProvidersRead },
        loadComponent: () => import('./pages/providers/providers').then((m) => m.Providers),
      },
      {
        path: 'settings',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.AuthPasswordChange },
        loadComponent: () => import('./pages/settings/settings').then((m) => m.Settings),
      },
      {
        path: 'users',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.UsersRead },
        loadComponent: () => import('./pages/users-admin/users-admin').then((m) => m.UsersAdmin),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard],
        data: { permission: AppPermissions.RolesRead },
        loadComponent: () => import('./pages/roles-admin/roles-admin').then((m) => m.RolesAdmin),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
