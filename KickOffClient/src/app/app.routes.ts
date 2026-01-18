import { Routes } from '@angular/router';
import { Home } from './components/home/home';
import { AccountSettings } from './components/shared/account-settings/account-settings';

export const routes: Routes = [
    { path: '', component: Home },
    { path: 'account-settings/:id', component: AccountSettings },
    { path: '**', redirectTo: '' }
];
