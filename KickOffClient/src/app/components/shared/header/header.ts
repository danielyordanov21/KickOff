import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RouterModule } from '@angular/router';
import { catchError, of, switchMap, timer } from 'rxjs';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { ProjectNotification } from '../../../models/project-models/project-notification.model';
import {
  PROJECT_FEED_PAGE_SIZE_OPTIONS,
  PROJECT_FEED_STATE_OPTIONS,
  ProjectFeedFiltersService,
} from '../../../services/project-feed-filters.service';
import { ProjectService } from '../../../services/project.service';
import { SendbirdService } from '../../../services/sendbird.service';
import { Pfp } from '../pfp/pfp';

@Component({
  selector: 'app-header',
  templateUrl: './header.html',
  styleUrl: './header.scss',
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    Pfp,
  ],
})
export class Header {
  private readonly router = inject(Router);
  private readonly authStateService = inject(AuthStateService);
  private readonly projectFeedFiltersService = inject(ProjectFeedFiltersService);
  private readonly projectService = inject(ProjectService);
  private readonly sendbirdService = inject(SendbirdService);

  protected readonly stateOptions = PROJECT_FEED_STATE_OPTIONS;
  protected readonly pageSizeOptions = PROJECT_FEED_PAGE_SIZE_OPTIONS;
  protected readonly projectNotifications = signal<ProjectNotification[]>([]);
  protected readonly notificationsLoading = signal(false);
  protected readonly notificationsError = signal<string | null>(null);
  protected readonly unreadProjectNotificationsCount = signal(0);

  protected keyword = '';
  protected selectedState = 'All';
  protected sortNewest = true;
  protected pageSize = 6;

  constructor() {
    effect(() => {
      const currentUserId = this.authStateService.currentUser()?.idP?.trim() ?? null;

      if (!currentUserId) {
        void this.sendbirdService.disconnect();
        this.projectNotifications.set([]);
        this.unreadProjectNotificationsCount.set(0);
        this.notificationsLoading.set(false);
        this.notificationsError.set(null);
        return;
      }

      void this.sendbirdService.connect(currentUserId);
    });

    effect(onCleanup => {
      const currentUserId = this.authStateService.currentUser()?.idP?.trim() ?? null;

      if (!currentUserId) {
        this.projectNotifications.set([]);
        this.unreadProjectNotificationsCount.set(0);
        this.notificationsLoading.set(false);
        this.notificationsError.set(null);
        return;
      }

      this.notificationsLoading.set(true);

      const subscription = timer(0, 60000).pipe(
        switchMap(() => this.projectService.getNotifications(8).pipe(
          catchError(error => {
            console.error('Error loading project notifications:', error);
            this.notificationsError.set('We could not load project alerts right now.');
            this.notificationsLoading.set(false);
            return of(null);
          })
        ))
      ).subscribe(feed => {
        if (!feed) {
          return;
        }

        this.projectNotifications.set(feed.notifications);
        this.unreadProjectNotificationsCount.set(feed.unreadCount);
        this.notificationsError.set(null);
        this.notificationsLoading.set(false);
      });

      onCleanup(() => subscription.unsubscribe());
    });

    effect(() => {
      const filters = this.projectFeedFiltersService.filters();

      this.keyword = filters.keyword;
      this.selectedState = filters.state;
      this.sortNewest = filters.sortNewest;
      this.pageSize = filters.pageSize;
    });
  }

  get isLoggedIn(): boolean {
    return this.authStateService.isAuthenticated();
  }

  get canCreateProjects(): boolean {
    return this.authStateService.canCreateProjects();
  }

  get unreadChatsCount(): number {
    return this.sendbirdService.totalUnreadMessageCount();
  }

  get unreadChatsLabel(): string {
    const unreadChatsCount = this.unreadChatsCount;
    return unreadChatsCount > 99 ? '99+' : unreadChatsCount.toString();
  }

  get unreadProjectNotificationsLabel(): string {
    const unreadCount = this.unreadProjectNotificationsCount();
    return unreadCount > 99 ? '99+' : unreadCount.toString();
  }

  get activeFilters(): string[] {
    const appliedFilters = this.projectFeedFiltersService.filters();
    const activeFilters: string[] = [];

    if (appliedFilters.keyword.length > 0) {
      activeFilters.push(`Keyword: ${appliedFilters.keyword}`);
    }

    if (appliedFilters.state !== 'All') {
      activeFilters.push(`State: ${appliedFilters.state}`);
    }

    if (!appliedFilters.sortNewest) {
      activeFilters.push('Sort: Oldest first');
    }

    if (appliedFilters.pageSize !== 6) {
      activeFilters.push(`${appliedFilters.pageSize} per page`);
    }

    return activeFilters;
  }

  public onKeywordBlur(): void {
    this.keyword = this.keyword.trim();
  }

  public async applyBrowseFilters(): Promise<void> {
    this.keyword = this.keyword.trim();

    this.projectFeedFiltersService.updateFilters({
      keyword: this.keyword,
      state: this.selectedState,
      sortNewest: this.sortNewest,
      pageNumber: 1,
      pageSize: this.pageSize,
    });

    await this.navigateHomeIfNeeded();
  }

  public async setState(state: string): Promise<void> {
    this.selectedState = state;
    await this.applyBrowseFilters();
  }

  public async setSortNewest(sortNewest: boolean): Promise<void> {
    this.sortNewest = sortNewest;
    await this.applyBrowseFilters();
  }

  public async onPageSizeChange(pageSize: number): Promise<void> {
    this.pageSize = pageSize;
    await this.applyBrowseFilters();
  }

  public async resetBrowseFilters(): Promise<void> {
    this.projectFeedFiltersService.resetFilters();
    await this.navigateHomeIfNeeded();
  }

  public toProfile(): void {
    this.router.navigate(['/profile', 'self']);
  }

  public toChat(): void {
    this.router.navigate(['/chat']);
  }

  public openProjectNotification(notification: ProjectNotification): void {
    if (!notification.isRead) {
      this.markProjectNotificationAsRead(notification.id);
    }

    this.router.navigate(['/project', notification.projectId]);
  }

  public markAllProjectNotificationsAsRead(): void {
    if (this.unreadProjectNotificationsCount() === 0) {
      return;
    }

    this.notificationsError.set(null);

    this.projectService.markAllNotificationsRead().subscribe({
      next: () => {
        this.projectNotifications.update(notifications =>
          notifications.map(notification => ({ ...notification, isRead: true }))
        );
        this.unreadProjectNotificationsCount.set(0);
        this.notificationsError.set(null);
      },
      error: error => {
        console.error('Error marking all project notifications as read:', error);
        this.notificationsError.set('We could not clear project alerts right now.');
      }
    });
  }

  public trackProjectNotification(_index: number, notification: ProjectNotification): string {
    return notification.id;
  }

  public toProjects(): void {
    this.router.navigate(['/projects']);
  }

  public toBecomeProducer(): void {
    this.router.navigate(['/account-settings']);
  }

  public toLogin(): void {
    this.router.navigate(['/auth', 'login']);
  }

  public toRegister(): void {
    this.router.navigate(['/auth', 'register']);
  }

  private markProjectNotificationAsRead(notificationId: string): void {
    const unreadNotification = this.projectNotifications()
      .find(notification => notification.id === notificationId && !notification.isRead);

    if (!unreadNotification) {
      return;
    }

    this.projectNotifications.update(notifications =>
      notifications.map(notification =>
        notification.id === notificationId
          ? { ...notification, isRead: true }
          : notification
      )
    );

    this.unreadProjectNotificationsCount.update(count => Math.max(0, count - 1));
    this.notificationsError.set(null);

    this.projectService.markNotificationRead(notificationId).subscribe({
      next: () => {
        this.notificationsError.set(null);
      },
      error: error => {
        console.error('Error marking project notification as read:', error);
        this.projectNotifications.update(notifications =>
          notifications.map(notification =>
            notification.id === notificationId
              ? { ...notification, isRead: false }
              : notification
          )
        );
        this.unreadProjectNotificationsCount.update(count => count + 1);
        this.notificationsError.set('We could not update this alert right now.');
      }
    });
  }

  private async navigateHomeIfNeeded(): Promise<void> {
    if (this.router.url.split('?')[0] === '/') {
      return;
    }

    await this.router.navigate(['/']);
  }
}
