import { Routes } from '@angular/router';
import { authGuard } from './core/auth-guard';
import { guestGuard } from './core/guest-guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'lists' },
  {
    path: 'login',
    title: 'Sign in · Todo',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'register',
    title: 'Create account · Todo',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register/register').then((m) => m.Register),
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'lists',
        title: 'Lists · Todo',
        loadComponent: () =>
          import('./features/lists/lists-page/lists-page').then((m) => m.ListsPage),
      },
      {
        path: 'lists/:listId',
        title: 'Tasks · Todo',
        loadComponent: () =>
          import('./features/tasks/tasks-page/tasks-page').then((m) => m.TasksPage),
      },
      {
        path: 'tasks/:taskId',
        title: 'Task · Todo',
        loadComponent: () =>
          import('./features/tasks/task-detail-page/task-detail-page').then(
            (m) => m.TaskDetailPage,
          ),
      },
      {
        path: 'tags',
        title: 'Tags · Todo',
        loadComponent: () => import('./features/tags/tags-page/tags-page').then((m) => m.TagsPage),
      },
    ],
  },
  { path: '**', redirectTo: 'lists' },
];
