import { Injectable } from '@angular/core';
import { SettingModel } from '../models/settings.model';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {

  getSettings(): SettingModel[] {
    return [
      {
        Group: 'Account',
        Name: 'Email',
        Description: 'Your email address',
        HoverText: 'This is your primary email address',
        Value: 'string'
      },
      {
        Group: 'Account',
        Name: 'Password',
        Description: 'Change your password',
        HoverText: 'Update your account password',
        Value: ''
      },
      {
        Group: 'Notifications',
        Name: 'Username',
        Description: 'Your username',
        HoverText: 'This is your unique username',
        Value: 'string'
      }
    ];
  }
}