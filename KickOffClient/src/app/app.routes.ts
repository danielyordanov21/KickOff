import { Routes } from '@angular/router';
import { authGuard } from '../auth/auth.guard';

import { projectCreatorGuard } from '../auth/project-creator.guard';
import { projectEditorGuard } from '../auth/project-editor.guard';

export const routes: Routes = [
    { path: '', loadComponent: () => import('./components/sheets/home/home').then(m => m.Home) },
    { path: 'landing', loadComponent: () => import('./components/sheets/landing-page/landing-page').then(m => m.LandingPage) },
    { path: 'auth/register', loadComponent: () => import('../auth/components/register/register').then(m => m.Register) },
    { path: 'auth/login', loadComponent: () => import('../auth/components/login/login').then(m => m.Login) },
    { path: 'auth/forgot-password', loadComponent: () => import('../auth/components/forgot-password/forgot-password').then(m => m.ForgotPassword) },
    { path: 'auth/reset-password', loadComponent: () => import('../auth/components/reset-password/reset-password').then(m => m.ResetPassword) },
    { path: 'auth/verify-email', loadComponent: () => import('../auth/components/verify-email/verify-email').then(m => m.VerifyEmail) },
    { path: 'chat/:userId', loadComponent: () => import('./components/sheets/chat-component/chat-component').then(m => m.ChatComponent), canActivate: [authGuard] },
    { path: 'chat', loadComponent: () => import('./components/sheets/chat-component/chat-component').then(m => m.ChatComponent), canActivate: [authGuard] },
    { path: 'profile/:id', loadComponent: () => import('./components/sheets/profile-view/profile-view').then(m => m.ProfileView) },
    { path: 'projects', loadComponent: () => import('./components/sheets/projects/projects').then(m => m.Projects), canActivate: [projectCreatorGuard] },
    { path: 'project/create', loadComponent: () => import('./components/sheets/project-create/project-create').then(m => m.ProjectCreate), canActivate: [projectCreatorGuard] },
    { path: 'project/:id/edit', loadComponent: () => import('./components/sheets/project-create/project-create').then(m => m.ProjectCreate), canActivate: [projectEditorGuard] },
    { path: 'project/:id', loadComponent: () => import('./components/sheets/project-view/project-view').then(m => m.ProjectView) },
    { path: 'sponsors/:id', loadComponent: () => import('./components/sheets/sponsors-view/sponsors-view').then(m => m.SponsorsView), canActivate: [authGuard] },
    { path: 'account-settings', loadComponent: () => import('./components/sheets/account-settings/account-settings').then(m => m.AccountSettings), canActivate: [authGuard] },
    { path: '**', loadComponent: () => import('./components/sheets/not-found/not-found').then(m => m.NotFound) },
];
