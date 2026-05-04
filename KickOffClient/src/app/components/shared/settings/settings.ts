import { Component, input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

import { SettingModel } from '../../../models/settings.model';
import { SettingGroup } from '../../../enums/settings/settings-group.enum';

@Component({
  selector: 'settings',
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
  imports: [
    CommonModule,
  ],
})
export class Settings {
  settings = input<SettingModel[]>([]);

  selectedGroup = signal<string | null>(null);

  groupedSettings = computed(() => {
    const groups: Partial<Record<SettingGroup, SettingModel[]>> = {};
    for (const setting of this.settings()) {
      if (!groups[setting.Group]) {
        groups[setting.Group] = [];
      }
      groups[setting.Group]!.push(setting);
    }
    return groups;
  });

  filteredSettings = computed(() => {
    if (!this.selectedGroup()) return this.settings();
    return this.settings().filter(s => s.Group === this.selectedGroup());
  });
}
