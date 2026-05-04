import { ComponentFixture, TestBed } from '@angular/core/testing';
import { convertToParamMap, ActivatedRoute, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { ProfileView } from './profile-view';
import { AuthService } from '../../../../auth/services/auth.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { UserService } from '../../../../auth/services/user.service';

describe('ProfileView', () => {
  let component: ProfileView;
  let fixture: ComponentFixture<ProfileView>;

  const currentUser = {
    id: 'user-1',
    idP: 'public-1',
    userName: 'Casey Backer',
    email: 'casey@example.com',
    role: 'Producer',
    roles: ['Producer', 'Backer'],
    state: 'Online',
    projectIds: [],
    followerIdsP: [],
    followingIdsP: [],
    backedProjects: [
      {
        id: 'project-1',
        name: 'Atlas AI Ops',
        description: 'An AI workspace for support teams.',
        owner: 'Maya Ross',
        state: 'Active',
      },
      {
        id: 'project-2',
        name: 'PocketCare',
        description: 'A proactive care companion.',
        owner: 'Samir Khan',
        state: 'Proposed',
      },
    ],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfileView],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ id: 'self' })),
          },
        },
        {
          provide: UserService,
          useValue: {
            getCurrentUser: () => currentUser,
            getUser: vi.fn().mockReturnValue(of(currentUser)),
            followUser: vi.fn(),
            unfollowUser: vi.fn(),
          },
        },
        {
          provide: AuthService,
          useValue: {
            getCurrentUser: vi.fn().mockReturnValue(of(currentUser)),
          },
        },
        {
          provide: AuthStateService,
          useValue: {
            currentUser: () => currentUser,
            isAuthenticated: () => true,
            canCreateProjects: () => true,
          },
        },
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProfileView);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders a tooltip for the backer role with the backed projects', () => {
    const pills = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('.pill')
    ) as HTMLElement[];
    const backerPill = pills.find((pill: HTMLElement) => pill.textContent?.includes('Backer')) ?? null;
    expect(backerPill?.textContent).toContain('Backer');
    expect(backerPill?.getAttribute('title')).toContain('Atlas AI Ops - Active');
    expect(backerPill?.getAttribute('title')).toContain('PocketCare - Proposed');
  });
});
