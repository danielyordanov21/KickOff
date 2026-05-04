import { of } from 'rxjs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';

import { ProjectView } from './project-view';
import { ProjectCatalogueDto } from '../../../models/project-models/project-catalogue.model';
import { Project } from '../../../models/project-models/project.model';
import { PaginatedResult } from '../../../models/project-models/project-paginated.model';
import { ProjectUpdate } from '../../../models/project-models/project-update.model';
import { ProjectService } from '../../../services/project.service';
import { AuthService } from '../../../../auth/services/auth.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { UserService } from '../../../../auth/services/user.service';

describe('ProjectView', () => {
  const baseUpdates: ProjectUpdate[] = [
    {
      id: 'update-2',
      projectId: 'project-1',
      title: 'Venue lineup locked',
      content: 'We locked the first three partner venues and finalized the pilot schedule.',
      authorUserId: 'owner-1',
      authorName: 'Daniel',
      createdAt: new Date('2026-03-15T09:00:00.000Z'),
      updatedAt: new Date('2026-03-15T09:00:00.000Z'),
      isEdited: false
    },
    {
      id: 'update-1',
      projectId: 'project-1',
      title: 'Applications opened',
      content: 'Creator applications are open and the first batch of interest has started coming in.',
      authorUserId: 'owner-1',
      authorName: 'Daniel',
      createdAt: new Date('2026-03-10T09:00:00.000Z'),
      updatedAt: new Date('2026-03-10T12:00:00.000Z'),
      isEdited: true
    }
  ];

  const baseProject: Project = {
    id: 'project-1',
    name: 'KickOff Live Sessions',
    headline: 'Bring creators and backers together in one space',
    goal: 'Launch a recurring live session series for emerging creators.',
    description: 'A formatted home for the story, media, support signals, and contact paths behind the project.',
    imageUrls: ['https://example.com/hero.jpg', 'https://example.com/gallery.jpg'],
    state: 'Active',
    extraInfo: 'Pilot season starts with three cities.',
    owner: 'Daniel',
    ownerId: 'owner-1',
    ownerPublicId: '00000000-0000-0000-0000-000000000123',
    category: 'Community',
    financialGoal: 2500,
    problem: 'Creators need a clearer way to show traction and needs.',
    imageBlobNames: ['hero.jpg', 'gallery.jpg'],
    collaboratorsIdP: ['collab-1'],
    contacts: ['team@kickoff.test', '+1 (555) 123-4567'],
    tags: ['events', 'creators'],
    backerIds: ['backer-1'],
    follow: {
      followersCount: 3,
      isFollowing: false,
      receiveInAppNotifications: true,
      receiveEmailNotifications: true
    },
    updates: baseUpdates,
    startDate: new Date('2026-03-01T09:00:00.000Z'),
    endDate: new Date('2026-05-01T09:00:00.000Z'),
    settingsId: 'settings-1'
  };

  const recommendedProjects: ProjectCatalogueDto[] = [
    {
      id: 'project-2',
      name: 'ForgeLearn',
      description: 'Guided learning cohorts for practical upskilling.',
      owner: 'Samir',
      state: 'Active',
      imageUrl: 'https://example.com/recommendation-1.jpg'
    },
    {
      id: 'project-3',
      name: 'HarvestLink',
      description: 'Local producer matchmaking for restaurants and buyers.',
      owner: 'Darius',
      state: 'Active',
      imageUrl: 'https://example.com/recommendation-2.jpg'
    },
    {
      id: 'project-4',
      name: 'Signal Sessions',
      description: 'Short creator events with sponsor-friendly reporting.',
      owner: 'Ava',
      state: 'Active',
      imageUrl: 'https://example.com/recommendation-3.jpg'
    },
    {
      id: 'project-5',
      name: 'Creator Commons',
      description: 'Shared resources for early-stage creative teams.',
      owner: 'Mina',
      state: 'Active',
      imageUrl: 'https://example.com/recommendation-4.jpg'
    }
  ];

  async function setup(options?: {
    routeId?: string | null;
    project?: Project;
    canEditProject?: boolean;
    isCurrentUserOwner?: boolean;
    searchResults?: ProjectCatalogueDto[];
    paginatedResults?: ProjectCatalogueDto[];
    isAuthenticated?: boolean;
    isAuthInitialized?: boolean;
    isAuthLoading?: boolean;
  }): Promise<{
    fixture: ComponentFixture<ProjectView>;
    component: ProjectView;
    projectService: {
      getById: ReturnType<typeof vi.fn>;
      search: ReturnType<typeof vi.fn>;
      getPaginated: ReturnType<typeof vi.fn>;
      followProject: ReturnType<typeof vi.fn>;
      unfollowProject: ReturnType<typeof vi.fn>;
      updateFollowPreferences: ReturnType<typeof vi.fn>;
    };
    authService: {
      initialize: ReturnType<typeof vi.fn>;
    };
    userService: {
      canEditProject: ReturnType<typeof vi.fn>;
      isCurrentUserOwner: ReturnType<typeof vi.fn>;
    };
    authState: {
      isAuthenticated: ReturnType<typeof vi.fn>;
      currentUser: ReturnType<typeof vi.fn>;
      isInitialized: ReturnType<typeof vi.fn>;
      isLoading: ReturnType<typeof vi.fn>;
    };
    router: Router;
  }> {
    const projectService = {
      getById: vi.fn(),
      search: vi.fn(),
      getPaginated: vi.fn(),
      followProject: vi.fn(),
      unfollowProject: vi.fn(),
      updateFollowPreferences: vi.fn()
    };
    const userService = {
      canEditProject: vi.fn(),
      isCurrentUserOwner: vi.fn()
    };
    const authService = {
      initialize: vi.fn().mockResolvedValue(undefined)
    };
    const authState = {
      isAuthenticated: vi.fn(),
      currentUser: vi.fn(),
      isInitialized: vi.fn(),
      isLoading: vi.fn()
    };
    const routeId = options?.routeId === undefined ? baseProject.id : options.routeId;
    const searchResponse: PaginatedResult<ProjectCatalogueDto> = {
      data: options?.searchResults ?? recommendedProjects,
      pageNumber: 1,
      pageSize: 6,
      totalCount: (options?.searchResults ?? recommendedProjects).length
    };
    const paginatedResponse: PaginatedResult<ProjectCatalogueDto> = {
      data: options?.paginatedResults ?? recommendedProjects,
      pageNumber: 1,
      pageSize: 10,
      totalCount: (options?.paginatedResults ?? recommendedProjects).length
    };

    if (routeId) {
      projectService.getById.mockReturnValue(of(options?.project ?? baseProject));
    }

    projectService.search.mockReturnValue(of(searchResponse));
    projectService.getPaginated.mockReturnValue(of(paginatedResponse));
    projectService.followProject.mockReturnValue(of({
      followersCount: 4,
      isFollowing: true,
      receiveInAppNotifications: true,
      receiveEmailNotifications: true
    }));
    projectService.unfollowProject.mockReturnValue(of({
      followersCount: 2,
      isFollowing: false,
      receiveInAppNotifications: true,
      receiveEmailNotifications: true
    }));
    projectService.updateFollowPreferences.mockReturnValue(of({
      followersCount: 3,
      isFollowing: true,
      receiveInAppNotifications: false,
      receiveEmailNotifications: true
    }));

    userService.canEditProject.mockReturnValue(options?.canEditProject ?? false);
    userService.isCurrentUserOwner.mockReturnValue(options?.isCurrentUserOwner ?? false);
    authState.isAuthenticated.mockReturnValue(options?.isAuthenticated ?? true);
    authState.currentUser.mockReturnValue({ idP: 'viewer-1' });
    authState.isInitialized.mockReturnValue(options?.isAuthInitialized ?? true);
    authState.isLoading.mockReturnValue(options?.isAuthLoading ?? false);

    await TestBed.configureTestingModule({
      imports: [ProjectView],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap(routeId ? { id: routeId } : {}))
          }
        },
        { provide: ProjectService, useValue: projectService },
        { provide: AuthService, useValue: authService },
        { provide: UserService, useValue: userService },
        { provide: AuthStateService, useValue: authState }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(ProjectView);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    return {
      fixture,
      component: fixture.componentInstance,
      projectService,
      authService,
      userService,
      authState,
      router: TestBed.inject(Router)
    };
  }

  it('loads a project and normalizes view-friendly collections', async () => {
    const project: Project = {
      ...baseProject,
      imageUrls: ['https://example.com/hero.jpg', 'https://example.com/gallery.jpg', 'https://example.com/gallery.jpg', ''],
      contacts: [' team@kickoff.test ', 'team@kickoff.test', ''],
      tags: ['events', 'events', ' creators '],
      collaboratorsIdP: ['collab-1', 'collab-1'],
      backerIds: ['backer-1', 'backer-1']
    };

    const { component, projectService } = await setup({ project });

    expect(projectService.getById).toHaveBeenCalledWith('project-1');
    expect(component.heroImageUrl).toBe('https://example.com/hero.jpg');
    expect(component.secondaryGalleryImages).toEqual(['https://example.com/gallery.jpg']);
    expect(component.contacts).toEqual(['team@kickoff.test']);
    expect(component.tags).toEqual(['events', 'creators']);
    expect(component.collaborators).toEqual(['collab-1']);
    expect(component.backerIds).toEqual(['backer-1']);
    expect(component.followersCount).toBe(3);
    expect(component.isFollowingProject).toBe(false);
    expect(component.updates.map(update => update.id)).toEqual(['update-2', 'update-1']);
    expect(component.recommendedProjects()).toEqual(recommendedProjects);
    expect(component.project()).toEqual(project);
  }, 10000);

  it('restores auth state when the public project page loads', async () => {
    const { authService } = await setup();

    expect(authService.initialize).toHaveBeenCalled();
  });

  it('navigates to the edit page when requested', async () => {
    const { component, router } = await setup({
      canEditProject: true,
      isCurrentUserOwner: true
    });
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component.goToEditProject();

    expect(component.showInternalReferences).toBe(true);
    expect(navigateSpy).toHaveBeenCalledWith(['/project', 'project-1', 'edit'], {
      state: {
        projectSnapshot: baseProject
      }
    });
  });

  it('uses the owner public id for the contact owner chat link', async () => {
    const { component } = await setup();

    expect(component.ownerChatLink).toEqual(['/chat', '00000000-0000-0000-0000-000000000123']);
  });

  it('does not show the contact project action to the owner', async () => {
    const { fixture } = await setup({
      canEditProject: true,
      isCurrentUserOwner: true
    });

    expect(fixture.nativeElement.textContent).not.toContain('Contact Project');
  });

  it('does not prompt sign-in while auth is still restoring', async () => {
    const { fixture, component } = await setup({
      isAuthenticated: false,
      isAuthInitialized: false,
      isAuthLoading: true
    });

    expect(component.shouldPromptFollowSignIn).toBe(false);
    expect(fixture.nativeElement.textContent).not.toContain('Sign in to Follow');
  });

  it('surfaces a friendly error when the route has no project id', async () => {
    const { component, projectService } = await setup({ routeId: null });

    expect(projectService.getById).not.toHaveBeenCalled();
    expect(component.project()).toBeNull();
    expect(component.loadError()).toBe('We could not find that project.');
    expect(component.isLoading()).toBe(false);
  });

  it('formats email, phone, and bare links into usable href values', async () => {
    const { component } = await setup();

    expect(component.formatContactLink('team@kickoff.test')).toBe('mailto:team@kickoff.test');
    expect(component.formatContactLink('+1 (555) 123-4567')).toBe('tel:+15551234567');
    expect(component.formatContactLink('kickoff.example')).toBe('https://kickoff.example');
  });

  it('follows a project and updates the local follow state', async () => {
    const { component, projectService } = await setup();

    component.toggleProjectFollow();

    expect(projectService.followProject).toHaveBeenCalledWith('project-1');
    expect(component.isFollowingProject).toBe(true);
    expect(component.followersCount).toBe(4);
  });

  it('excludes the current project and fills recommendations from the fallback list when needed', async () => {
    const sparseSameStateResults: ProjectCatalogueDto[] = [
      {
        id: 'project-1',
        name: 'KickOff Live Sessions',
        description: 'Current project should be excluded.',
        owner: 'Daniel',
        state: 'Active',
        imageUrl: 'https://example.com/current.jpg'
      },
      recommendedProjects[0]
    ];

    const { component, projectService } = await setup({
      searchResults: sparseSameStateResults,
      paginatedResults: recommendedProjects
    });

    expect(projectService.getPaginated).toHaveBeenCalledWith(null, 1, 10);
    expect(component.recommendedProjects().map(project => project.id)).toEqual([
      'project-2',
      'project-3',
      'project-4',
      'project-5'
    ]);
  });
});
