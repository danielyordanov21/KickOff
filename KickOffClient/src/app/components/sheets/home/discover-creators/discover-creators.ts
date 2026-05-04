import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { catchError, map, of } from 'rxjs';

import { UserService } from '../../../../../auth/services/user.service';
import { DiscoverPerson } from '../../../../models/discover-person.model';
import { UserFloatingPanel } from '../../../shared/user-floating-panel/user-floating-panel';

@Component({
  selector: 'discover-creators',
  standalone: true,
  templateUrl: './discover-creators.html',
  styleUrls: ['./discover-creators.scss'],
  imports: [
    CommonModule,
    RouterModule,
    UserFloatingPanel,
  ],
})
export class DiscoverCreators {
  private readonly userService = inject(UserService);
  private readonly brokenImageUserIds = new Set<string>();

  protected readonly users$ = this.userService.getDiscoverProducers().pipe(
    map(users => users.slice(0, 8)),
    catchError(() => of([] as DiscoverPerson[]))
  );

  protected canShowProfileImage(user: DiscoverPerson): boolean {
    return this.hasUsableProfileImageUrl(user.profilePictureUrl) && !this.brokenImageUserIds.has(user.id);
  }

  protected markProfileImageBroken(userId: string): void {
    this.brokenImageUserIds.add(userId);
  }

  protected initialsFor(userName?: string | null): string {
    if (!userName) {
      return '?';
    }

    return userName
      .split(' ')
      .filter(part => part.length > 0)
      .slice(0, 2)
      .map(part => part[0]?.toUpperCase() ?? '')
      .join('') || '?';
  }

  private hasUsableProfileImageUrl(profilePictureUrl?: string | null): boolean {
    if (!profilePictureUrl) {
      return false;
    }

    try {
      return Boolean(new URL(profilePictureUrl));
    } catch {
      return false;
    }
  }
}
