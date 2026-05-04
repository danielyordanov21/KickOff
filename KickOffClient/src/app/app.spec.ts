import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { App } from './app';
import { AuthService } from '../auth/services/auth.service';
import { AuthStateService } from '../auth/services/auth-state.service';
import { ProjectFeedFiltersService } from './services/project-feed-filters.service';
import { ProjectService } from './services/project.service';
import { SendbirdService } from './services/sendbird.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            initialize: vi.fn().mockResolvedValue(undefined),
            resendConfirmation: vi.fn().mockReturnValue(
              of({
                alreadyConfirmed: false,
                emailDeliveryEnabled: true,
                message: 'Confirmation email sent.'
              })
            )
          }
        },
        {
          provide: AuthStateService,
          useValue: {
            currentUser: () => null,
            isAuthenticated: () => false,
            canCreateProjects: () => false,
            updateCurrentUser: vi.fn()
          }
        },
        {
          provide: ProjectFeedFiltersService,
          useValue: {
            filters: () => ({
              keyword: '',
              state: 'All',
              sortNewest: true,
              pageNumber: 1,
              pageSize: 6
            }),
            updateFilters: vi.fn(),
            resetFilters: vi.fn()
          }
        },
        {
          provide: ProjectService,
          useValue: {
            getNotifications: vi.fn().mockReturnValue(
              of({
                notifications: [],
                unreadCount: 0
              })
            ),
            markNotificationRead: vi.fn().mockReturnValue(of(undefined)),
            markAllNotificationsRead: vi.fn().mockReturnValue(of(undefined))
          }
        },
        {
          provide: SendbirdService,
          useValue: {
            totalUnreadMessageCount: () => 0,
            connect: vi.fn().mockResolvedValue(undefined),
            disconnect: vi.fn().mockResolvedValue(undefined)
          }
        }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-header')).not.toBeNull();
  });
});
