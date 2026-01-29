import { Routes } from '@angular/router';
import { Home } from './components/sheets/home/home';
import { ProfileView } from './components/sheets/profile-view/profile-view';
import { SponsorsView } from './components/sheets/sponsors-view/sponsors-view';
import { AccountSettings } from './components/sheets/account-settings/account-settings';

import { Register } from '../auth/components/register/register';
import { Login } from '../auth/components/login/login';

import { NotFound } from './components/sheets/not-found/not-found';

export const routes: Routes = [
    { path: '', component: Home },
    { path: 'auth/register', component: Register },
    { path: 'auth/login', component: Login },
    { path: 'profile/:id', component: ProfileView },
    { path: 'sponsors/:id', component: SponsorsView },
    { path: 'account-settings', component: AccountSettings, /*canActivate: [] */ },
    { path: '**', component: NotFound },
];
