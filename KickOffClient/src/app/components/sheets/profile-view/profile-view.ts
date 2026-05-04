import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { EMPTY, Observable, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';
import { ProjectCatalogueDto } from '../../../models/project-models/project-catalogue.model';
import { AuthService } from '../../../../auth/services/auth.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { UserService } from '../../../../auth/services/user.service';
import { ProfileConnection, User } from '../../../../auth/user.model';
import { ProjectCard } from '../home/project-card/project-card';
import { ProfileMetrics } from './profile-metrics/profile-metrics';
import { UserFloatingPanel } from '../../shared/user-floating-panel/user-floating-panel';

type ConnectionView = 'followers' | 'following';

interface RoleBadge {
  key: string;
  label: string;
  isBacker: boolean;
  backedProjects: ProjectCatalogueDto[];
  ariaLabel?: string;
}

@Component({
  selector: 'app-profile-view',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    RouterModule,
    ProjectCard,
    ProfileMetrics,
    UserFloatingPanel,
  ],
  templateUrl: './profile-view.html',
  styleUrl: './profile-view.scss',
})
export class ProfileView implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly userService = inject(UserService);
  private readonly authService = inject(AuthService);
  private readonly authState = inject(AuthStateService);

  user = signal<User | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  followError = signal<string | null>(null);
  isFollowPending = signal(false);
  activeConnectionView = signal<ConnectionView | null>(null);
  private profileId = signal<string | null>(null);

  get isOwnProfile(): boolean {
    const currentId = this.authState.currentUser()?.idP;
    const profileId = this.profileId();
    return profileId === 'self' || (!!currentId && profileId === currentId);
  }

  get displayRoles(): string[] {
    return this.roleBadges.map(role => role.label);
  }

  get roleBadges(): RoleBadge[] {
    const badges: RoleBadge[] = [];
    const seenRoles = new Set<string>();
    const activeUser = this.user();

    const appendRole = (role?: string | null) => {
      const normalizedRole = role?.trim().toLowerCase();
      if (!normalizedRole || seenRoles.has(normalizedRole)) {
        return;
      }

      seenRoles.add(normalizedRole);
      const isBacker = normalizedRole === 'backer';

      badges.push({
        key: normalizedRole,
        label: this.formatLabel(role),
        isBacker,
        backedProjects: isBacker ? this.backedProjects : [],
        ariaLabel: isBacker ? this.backerRoleAriaLabel : undefined,
      });
    };

    for (const role of activeUser?.roles ?? []) {
      appendRole(role);
    }

    appendRole(activeUser?.role);
    return badges;
  }

  get projectCount(): number {
    return this.user()?.projects?.length ?? this.user()?.projectIds?.length ?? 0;
  }

  get followerCount(): number {
    return this.user()?.followerIdsP?.length ?? 0;
  }

  get followingCount(): number {
    return this.user()?.followingIdsP?.length ?? 0;
  }

  get followers(): ProfileConnection[] {
    return this.user()?.followers ?? [];
  }

  get following(): ProfileConnection[] {
    return this.user()?.following ?? [];
  }

  get activeConnections(): ProfileConnection[] {
    return this.activeConnectionView() === 'followers' ? this.followers : this.following;
  }

  get activeConnectionTitle(): string {
    return this.activeConnectionView() === 'following' ? 'Following' : 'Followers';
  }

  get activeConnectionDescription(): string {
    return this.activeConnectionView() === 'following'
      ? 'Profiles this user is currently following.'
      : 'Profiles currently following this user.';
  }

  get activeConnectionEmptyMessage(): string {
    if (this.activeConnectionView() === 'following') {
      return this.isOwnProfile
        ? 'You are not following anyone yet.'
        : 'This profile is not following anyone yet.';
    }

    return this.isOwnProfile
      ? 'You do not have followers yet.'
      : 'This profile does not have followers yet.';
  }

  get profileInitials(): string {
    const name = this.user()?.userName?.trim();
    if (!name) {
      return 'KO';
    }

    const parts = name.split(/\s+/).filter(Boolean).slice(0, 2);
    return parts.map(part => part[0]?.toUpperCase() ?? '').join('') || name.slice(0, 2).toUpperCase();
  }

  get stateLabel(): string | null {
    const user = this.user();
    return user?.state ? this.formatLabel(user.state) : null;
  }

  get backedProjects(): ProjectCatalogueDto[] {
    return this.user()?.backedProjects ?? [];
  }

  get backerRoleAriaLabel(): string {
    return this.backerRoleTooltip(this.backedProjects, 'Backer role.');
  }

  get canCreateProjects(): boolean {
    return this.authState.canCreateProjects();
  }

  get canFollowProfile(): boolean {
    return this.authState.isAuthenticated() && !this.isOwnProfile && !!this.user()?.idP;
  }

  get canMessageProfile(): boolean {
    return this.authState.isAuthenticated() && !this.isOwnProfile && !!this.user()?.idP;
  }

  get shouldPromptFollowSignIn(): boolean {
    return !this.authState.isAuthenticated() && !this.isOwnProfile;
  }

  get isFollowingProfile(): boolean {
    const activeUserPublicId = this.user()?.idP;
    if (!activeUserPublicId) {
      return false;
    }

    return (this.authState.currentUser()?.followingIdsP ?? []).includes(activeUserPublicId);
  }

  ngOnInit(): void {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .pipe(
        map(paramMap => paramMap.get('id')),
        switchMap(profileId => {
          this.profileId.set(profileId);
          this.error.set(null);
          this.followError.set(null);
          this.isFollowPending.set(false);
          this.activeConnectionView.set(null);

          if (!profileId) {
            this.user.set(null);
            this.error.set('We could not find that profile.');
            this.isLoading.set(false);
            return EMPTY;
          }

          const activeUser = this.user();
          const isDifferentProfile = activeUser?.idP !== profileId && !(profileId === 'self' && this.isOwnProfile);

          if (isDifferentProfile) {
            this.user.set(null);
          }

          this.isLoading.set(true);

          return profileId === 'self'
            ? this.resolveCurrentUser()
            : this.userService.getUser(profileId);
        })
      )
      .subscribe({
        next: user => {
          this.user.set(this.mergeUser(user, this.user()));
          this.followError.set(null);
          this.isLoading.set(false);
        },
        error: err => {
          this.error.set(typeof err === 'string' ? err : 'An error occurred while fetching this profile.');
          this.isLoading.set(false);
          console.error(err);
        }
      });
  }

  public formatLabel(value?: string | null): string {
    if (!value?.trim()) {
      return 'KickOff member';
    }

    return value
      .replace(/[_-]+/g, ' ')
      .split(' ')
      .filter(Boolean)
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');
  }

  protected trackProject(_index: number, project: ProjectCatalogueDto): string {
    return project.id;
  }

  protected trackRoleBadge(_index: number, badge: RoleBadge): string {
    return badge.key;
  }

  protected backerRoleTooltip(projects: ProjectCatalogueDto[], prefix = 'Backed projects:'): string {
    if (projects.length === 0) {
      return `${prefix} none yet.`;
    }

    const projectLabels = projects
      .map(project => [project.name?.trim(), this.formatLabel(project.state)].filter(Boolean).join(' - '))
      .filter(label => label.length > 0);

    return projectLabels.length > 0
      ? `${prefix} ${projectLabels.join(', ')}.`
      : `${prefix} attached to this account.`;
  }

  protected trackConnection(_index: number, connection: ProfileConnection): string {
    return connection.idP;
  }

  protected profileConnectionInitials(userName?: string | null): string {
    if (!userName) {
      return 'KO';
    }

    const parts = userName.split(/\s+/).filter(Boolean).slice(0, 2);
    return parts.map(part => part[0]?.toUpperCase() ?? '').join('') || userName.slice(0, 2).toUpperCase();
  }

  protected toggleConnectionView(view: ConnectionView): void {
    this.activeConnectionView.update(currentView => currentView === view ? null : view);
  }

  protected isConnectionViewActive(view: ConnectionView): boolean {
    return this.activeConnectionView() === view;
  }

  protected toggleFollow(): void {
    const activeUser = this.user();
    const activeUserPublicId = activeUser?.idP;
    if (!activeUser || !activeUserPublicId || this.isFollowPending()) {
      return;
    }

    const shouldFollow = !this.isFollowingProfile;
    const request$ = shouldFollow
      ? this.userService.followUser(activeUserPublicId)
      : this.userService.unfollowUser(activeUserPublicId);

    this.isFollowPending.set(true);
    this.followError.set(null);

    request$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.syncFollowState(activeUserPublicId, shouldFollow);
          this.isFollowPending.set(false);
        },
        error: () => {
          this.followError.set(shouldFollow
            ? 'We could not follow this profile right now.'
            : 'We could not unfollow this profile right now.');
          this.isFollowPending.set(false);
        }
      });
  }

  private resolveCurrentUser(): Observable<User> {
    const currentUser = this.userService.getCurrentUser();
    if (currentUser) {
      return of(currentUser);
    }

    return this.authService.getCurrentUser();
  }

  private syncFollowState(targetPublicId: string, shouldFollow: boolean): void {
    const currentUserPublicId = this.authState.currentUser()?.idP;
    const activeUser = this.user();
    const targetConnection = this.toProfileConnection(activeUser);

    this.authState.updateCurrentUser(currentUser => ({
      ...currentUser,
      following: shouldFollow
        ? this.includeConnection(currentUser.following, targetConnection)
        : this.removeConnection(currentUser.following, targetPublicId),
      followingIdsP: this.updatePublicIdList(currentUser.followingIdsP, targetPublicId, shouldFollow)
    }));

    if (!currentUserPublicId) {
      return;
    }

    this.user.update(profileUser => {
      if (!profileUser) {
        return profileUser;
      }

      const updatedFollowers = shouldFollow
        ? this.includeConnection(profileUser.followers, this.toProfileConnection(this.authState.currentUser()))
        : this.removeConnection(profileUser.followers, currentUserPublicId);

      return {
        ...profileUser,
        followers: updatedFollowers,
        followerIdsP: this.updatePublicIdList(profileUser.followerIdsP, currentUserPublicId, shouldFollow)
      };
    });
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

  private mergeUser(incomingUser: User, fallbackUser: User | null): User {
    if (!fallbackUser) {
      return incomingUser;
    }

    const sameProfile = !!incomingUser.idP && incomingUser.idP === fallbackUser.idP;
    if (!sameProfile) {
      return incomingUser;
    }

    return {
      ...fallbackUser,
      ...incomingUser,
      profilePictureUrl: incomingUser.profilePictureUrl ?? fallbackUser.profilePictureUrl
    };
  }

  private includeConnection(
    connections: ProfileConnection[] | null | undefined,
    connection: ProfileConnection | null
  ): ProfileConnection[] {
    if (!connection?.idP) {
      return connections ?? [];
    }

    const nextConnections = new Map((connections ?? []).map(existingConnection => [existingConnection.idP, existingConnection]));
    nextConnections.set(connection.idP, connection);
    return Array.from(nextConnections.values());
  }

  private removeConnection(
    connections: ProfileConnection[] | null | undefined,
    publicId: string
  ): ProfileConnection[] {
    return (connections ?? []).filter(connection => connection.idP !== publicId);
  }

  private toProfileConnection(user: User | null): ProfileConnection | null {
    if (!user?.idP) {
      return null;
    }

    return {
      id: user.id,
      idP: user.idP,
      userName: user.userName,
      profilePictureUrl: user.profilePictureUrl,
      state: user.state
    };
  }
}
