import { CommonModule } from '@angular/common';
import {
  CdkConnectedOverlay,
  CdkOverlayOrigin,
  ConnectedPosition,
} from '@angular/cdk/overlay';
import {
  Component,
  DestroyRef,
  HostListener,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterModule } from '@angular/router';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { UserService } from '../../../../auth/services/user.service';
import { User } from '../../../../auth/user.model';

export interface UserFloatingPanelPreview {
  id: string;
  userName: string;
  profilePictureUrl?: string | null;
  email?: string | null;
  role?: string | null;
  roles?: string[] | null;
  projectIds?: string[] | null;
  followerIdsP?: string[] | null;
  followingIdsP?: string[] | null;
  state?: string | null;
}

const HOVER_PANEL_CLOSE_DELAY_MS = 120;

const HOVER_PANEL_POSITIONS: ConnectedPosition[] = [
  {
    originX: 'end',
    originY: 'center',
    overlayX: 'start',
    overlayY: 'center',
    offsetX: 0,
  },
  {
    originX: 'start',
    originY: 'center',
    overlayX: 'end',
    overlayY: 'center',
    offsetX: 0,
  },
  {
    originX: 'end',
    originY: 'top',
    overlayX: 'start',
    overlayY: 'top',
    offsetX: 0,
  },
  {
    originX: 'start',
    originY: 'top',
    overlayX: 'end',
    overlayY: 'top',
    offsetX: 0,
  },
];

@Component({
  selector: 'app-user-floating-panel',
  imports: [
    CommonModule,
    RouterModule,
    CdkOverlayOrigin,
    CdkConnectedOverlay,
  ],
  templateUrl: './user-floating-panel.html',
  styleUrl: './user-floating-panel.scss',
})
export class UserFloatingPanel {
  private static activePanel: UserFloatingPanel | null = null;

  private readonly destroyRef = inject(DestroyRef);
  private readonly userService = inject(UserService);
  private readonly authState = inject(AuthStateService);
  private readonly userCache = new Map<string, User>();
  private closeTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private activeRequestId: string | null = null;

  readonly userId = input.required<string>();
  readonly previewUser = input<UserFloatingPanelPreview | null>(null);

  readonly isOpen = signal(false);
  readonly isLoading = signal(false);
  readonly isFollowPending = signal(false);
  readonly error = signal<string | null>(null);
  readonly followError = signal<string | null>(null);
  readonly loadedUser = signal<User | null>(null);
  readonly overlayPositions = HOVER_PANEL_POSITIONS;

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.cancelClose();
      this.releaseActivePanel();
    });
  }

  readonly displayUser = computed<UserFloatingPanelPreview | User | null>(() => {
    const previewUser = this.previewUser();
    const loadedUser = this.loadedUser();

    if (!previewUser) {
      return loadedUser;
    }

    if (!loadedUser) {
      return previewUser;
    }

    return {
      ...previewUser,
      ...loadedUser,
      profilePictureUrl: loadedUser.profilePictureUrl ?? previewUser.profilePictureUrl
    };
  });

  readonly displayRoles = computed(() => {
    const user = this.displayUser();
    if (!user) {
      return [];
    }

    const roles = new Set<string>();

    for (const role of user.roles ?? []) {
      if (role?.trim()) {
        roles.add(this.formatLabel(role));
      }
    }

    if (user.role?.trim()) {
      roles.add(this.formatLabel(user.role));
    }

    return Array.from(roles);
  });

  readonly profileInitials = computed(() => {
    const name = this.displayUser()?.userName?.trim();
    if (!name) {
      return 'KO';
    }

    const parts = name.split(/\s+/).filter(Boolean).slice(0, 2);
    return parts.map(part => part[0]?.toUpperCase() ?? '').join('') || name.slice(0, 2).toUpperCase();
  });

  readonly projectCount = computed(() => this.displayUser()?.projectIds?.length ?? 0);
  readonly followerCount = computed(() => this.displayUser()?.followerIdsP?.length ?? 0);
  readonly followingCount = computed(() => this.displayUser()?.followingIdsP?.length ?? 0);
  readonly profileLink = computed(() => ['/profile', this.userId()]);
  readonly canFollow = computed(() => {
    const currentUser = this.authState.currentUser();
    const targetPublicId = this.userId();

    return !!currentUser && !!targetPublicId && currentUser.idP !== targetPublicId;
  });
  readonly shouldPromptFollowSignIn = computed(() => {
    const targetPublicId = this.userId();
    return !this.authState.isAuthenticated() && !!targetPublicId;
  });
  readonly isFollowing = computed(() => {
    const currentUser = this.authState.currentUser();
    const targetPublicId = this.userId();

    if (!currentUser || !targetPublicId) {
      return false;
    }

    return (currentUser.followingIdsP ?? []).includes(targetPublicId);
  });

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.closeImmediately();
  }

  openPanel(): void {
    this.cancelClose();
    this.claimActivePanel();
    this.isOpen.set(true);
    this.error.set(null);
    this.followError.set(null);
    this.loadUser();
  }

  scheduleClose(): void {
    this.cancelClose();

    if (UserFloatingPanel.activePanel !== this) {
      this.isOpen.set(false);
      return;
    }

    this.closeTimeoutId = setTimeout(() => {
      this.closeTimeoutId = null;
      this.releaseActivePanel();
      this.isOpen.set(false);
    }, HOVER_PANEL_CLOSE_DELAY_MS);
  }

  cancelClose(): void {
    if (this.closeTimeoutId) {
      clearTimeout(this.closeTimeoutId);
      this.closeTimeoutId = null;
    }
  }

  closeImmediately(): void {
    this.cancelClose();
    this.releaseActivePanel();
    this.isOpen.set(false);
  }

  toggleFollow(): void {
    const targetPublicId = this.userId();
    if (!targetPublicId || this.isFollowPending()) {
      return;
    }

    const shouldFollow = !this.isFollowing();
    const request$ = shouldFollow
      ? this.userService.followUser(targetPublicId)
      : this.userService.unfollowUser(targetPublicId);

    this.isFollowPending.set(true);
    this.followError.set(null);

    request$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.syncFollowState(targetPublicId, shouldFollow);
          this.isFollowPending.set(false);
        },
        error: () => {
          this.followError.set(shouldFollow
            ? 'We could not follow this creator right now.'
            : 'We could not unfollow this creator right now.');
          this.isFollowPending.set(false);
        },
      });
  }

  private loadUser(): void {
    const id = this.userId();
    if (!id) {
      return;
    }

    if (this.loadedUser()?.idP === id) {
      return;
    }

    const cachedUser = this.userCache.get(id);
    if (cachedUser) {
      this.loadedUser.set(cachedUser);
      return;
    }

    if (this.activeRequestId === id) {
      return;
    }

    this.isLoading.set(true);
    this.activeRequestId = id;

    this.userService.getUser(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: user => {
          const mergedUser = this.mergeLoadedUser(user, this.previewUser());
          this.userCache.set(id, mergedUser);
          this.loadedUser.set(mergedUser);
          this.isLoading.set(false);
          this.activeRequestId = null;
        },
        error: () => {
          this.error.set('We could not load more details right now.');
          this.isLoading.set(false);
          this.activeRequestId = null;
        },
      });
  }

  private syncFollowState(targetPublicId: string, shouldFollow: boolean): void {
    const currentUserPublicId = this.authState.currentUser()?.idP;

    this.authState.updateCurrentUser(currentUser => ({
      ...currentUser,
      followingIdsP: this.updatePublicIdList(currentUser.followingIdsP, targetPublicId, shouldFollow)
    }));

    if (!currentUserPublicId) {
      return;
    }

    this.loadedUser.update(user => {
      if (!user) {
        return user;
      }

      const updatedUser = {
        ...user,
        followerIdsP: this.updatePublicIdList(user.followerIdsP, currentUserPublicId, shouldFollow)
      };

      this.userCache.set(targetPublicId, updatedUser);
      return updatedUser;
    });
  }

  private formatLabel(value: string): string {
    return value
      .replace(/[_-]+/g, ' ')
      .split(' ')
      .filter(Boolean)
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');
  }

  private updatePublicIdList(
    ids: string[] | null | undefined,
    targetPublicId: string,
    shouldInclude: boolean
  ): string[] {
    const nextIds = new Set(ids ?? []);

    if (shouldInclude) {
      nextIds.add(targetPublicId);
    } else {
      nextIds.delete(targetPublicId);
    }

    return Array.from(nextIds);
  }

  private mergeLoadedUser(user: User, previewUser: UserFloatingPanelPreview | null): User {
    if (!previewUser) {
      return user;
    }

    return {
      ...user,
      profilePictureUrl: user.profilePictureUrl ?? previewUser.profilePictureUrl ?? undefined
    };
  }

  private claimActivePanel(): void {
    const activePanel = UserFloatingPanel.activePanel;

    if (activePanel && activePanel !== this) {
      activePanel.closeImmediately();
    }

    UserFloatingPanel.activePanel = this;
  }

  private releaseActivePanel(): void {
    if (UserFloatingPanel.activePanel === this) {
      UserFloatingPanel.activePanel = null;
    }
  }
}
