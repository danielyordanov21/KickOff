import { Component } from '@angular/core';
import { Settings } from '../../shared/settings/settings';

import { SettingModel } from '../../../models/settings.model';

@Component({
  selector: 'app-account-settings',
  templateUrl: './account-settings.html',
  styleUrl: './account-settings.scss',
  imports: [
    Settings,
  ],
})
export class AccountSettings {
  settings: SettingModel[] = [
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