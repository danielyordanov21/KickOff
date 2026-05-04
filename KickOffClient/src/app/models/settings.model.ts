import { SettingGroup } from '../enums/settings/settings-group.enum';
import { SettingValue } from '../enums/settings/settings-value.enum';

export interface SettingModel {
    Group: SettingGroup;
    Name: string;
    Description?: string;
    HoverText?: string;
    Value: SettingValue;
}