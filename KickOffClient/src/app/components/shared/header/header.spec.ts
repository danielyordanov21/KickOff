import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { Header } from './header';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { ProjectFeedFiltersService } from '../../../services/project-feed-filters.service';
import { ProjectService } from '../../../services/project.service';
import { SendbirdService } from '../../../services/sendbird.service';
import { Router } from '@angular/router';

describe('Header', () => {
  let component: Header;
  let fixture: ComponentFixture<Header>;
  let projectService: {
    getNotifications: ReturnType<typeof vi.fn>;
    markNotificationRead: ReturnType<typeof vi.fn>;
    markAllNotificationsRead: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  beforeEach(async () => {
    projectService = {
      getNotifications: vi.fn().mockReturnValue(of({
        notifications: [],
        unreadCount: 0,
      })),
      markNotificationRead: vi.fn().mockReturnValue(of(undefined)),
      markAllNotificationsRead: vi.fn().mockReturnValue(of(undefined)),
    };

    await TestBed.configureTestingModule({
      imports: [Header],
      providers: [
        provideRouter([]),
        {
          provide: AuthStateService,
          useValue: {
            currentUser: () => null,
            isAuthenticated: () => false,
            canCreateProjects: () => false,
          },
        },
        {
          provide: SendbirdService,
          useValue: {
            totalUnreadMessageCount: () => 0,
            connect: async () => undefined,
            disconnect: async () => undefined,
          },
        },
        {
          provide: ProjectFeedFiltersService,
          useValue: {
            filters: () => ({
              keyword: '',
              state: 'All',
              sortNewest: true,
              pageSize: 6,
            }),
            updateFilters: () => undefined,
            resetFilters: () => undefined,
          },
        },
        {
          provide: ProjectService,
          useValue: projectService,
        },
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(Header);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('rolls back an unread alert when mark-as-read fails', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const notification = {
      id: 'notification-1',
      projectId: 'project-1',
      projectName: 'KickOff Live Sessions',
      title: 'New update from KickOff Live Sessions',
      message: 'Pilot launch date confirmed.',
      isRead: false,
      createdAt: new Date('2026-04-20T12:00:00Z'),
    };

    projectService.markNotificationRead.mockReturnValue(
      throwError(() => new Error('network'))
    );

    (component as any).projectNotifications.set([notification]);
    (component as any).unreadProjectNotificationsCount.set(1);

    component.openProjectNotification(notification);

    expect(projectService.markNotificationRead).toHaveBeenCalledWith('notification-1');
    expect(navigateSpy).toHaveBeenCalledWith(['/project', 'project-1']);
    expect((component as any).projectNotifications()[0].isRead).toBe(false);
    expect((component as any).unreadProjectNotificationsCount()).toBe(1);
    expect((component as any).notificationsError()).toBe('We could not update this alert right now.');
  });

  it('marks all alerts as read and clears any stale error state on success', () => {
    const notifications = [
      {
        id: 'notification-1',
        projectId: 'project-1',
        projectName: 'KickOff Live Sessions',
        title: 'New update from KickOff Live Sessions',
        message: 'Pilot launch date confirmed.',
        isRead: false,
        createdAt: new Date('2026-04-20T12:00:00Z'),
      },
      {
        id: 'notification-2',
        projectId: 'project-2',
        projectName: 'ForgeLearn',
        title: 'New update from ForgeLearn',
        message: 'Curriculum preview published.',
        isRead: false,
        createdAt: new Date('2026-04-19T12:00:00Z'),
      },
    ];

    (component as any).projectNotifications.set(notifications);
    (component as any).unreadProjectNotificationsCount.set(2);
    (component as any).notificationsError.set('We could not update this alert right now.');

    component.markAllProjectNotificationsAsRead();

    expect(projectService.markAllNotificationsRead).toHaveBeenCalled();
    expect((component as any).projectNotifications().every((notification: { isRead: boolean }) => notification.isRead)).toBe(true);
    expect((component as any).unreadProjectNotificationsCount()).toBe(0);
    expect((component as any).notificationsError()).toBeNull();
  });

  it('navigates to the projects sheet from the producer shortcut', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component.toProjects();

    expect(navigateSpy).toHaveBeenCalledWith(['/projects']);
  });
});
