import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'storyboard', pathMatch: 'full' },

  {
    path: 'terms',
    loadComponent: () =>
      import('./components/terms/terms.component').then(m => m.TermsComponent),
  },
  {
    path: 'privacy',
    loadComponent: () =>
      import('./components/privacy/privacy.component').then(m => m.PrivacyComponent),
  },

  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./components/login/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./components/register/register.component').then(m => m.RegisterComponent),
  },

  // ── Authenticated shell ────────────────────────────────────────────────────
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./components/shell/shell.component').then(m => m.ShellComponent),
    children: [
      {
        path: 'storyboard',
        loadComponent: () =>
          import('./components/smart-storyboard/smart-storyboard.component')
            .then(m => m.SmartStoryboardComponent),
      },
      {
        path: 'videos',
        loadComponent: () =>
          import('./components/placeholder/placeholder.component')
            .then(m => m.PlaceholderComponent),
        data: { title: 'My Videos' },
      },
      {
        path: 'campaigns',
        loadComponent: () =>
          import('./components/placeholder/placeholder.component')
            .then(m => m.PlaceholderComponent),
        data: { title: 'Campaigns' },
      },
      {
        path: 'tiktok',
        loadComponent: () =>
          import('./components/placeholder/placeholder.component')
            .then(m => m.PlaceholderComponent),
        data: { title: 'TikTok Account' },
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./components/settings/settings.component')
            .then(m => m.SettingsComponent),
      },
    ],
  },

  { path: '**', redirectTo: 'login' },
];
