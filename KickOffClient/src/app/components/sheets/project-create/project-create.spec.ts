import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { from, of } from 'rxjs';
import { vi } from 'vitest';

import { ProjectCreate } from './project-create';
import { Project } from '../../../models/project-models/project.model';
import { ProjectService } from '../../../services/project.service';

describe('ProjectCreate', () => {
  const baseProject: Project = {
    id: 'project-1',
    name: 'KickOff Live Sessions',
    headline: 'Bring creators and backers together in one space',
    goal: 'Launch a recurring live session series for emerging creators.',
    description: 'A formatted home for the story, media, support signals, and contact paths behind the project.',
    imageUrls: ['https://example.com/hero.jpg'],
    state: 'Active',
    extraInfo: 'Pilot season starts with three cities.',
    owner: 'Daniel',
    ownerId: 'owner-1',
    ownerPublicId: '00000000-0000-0000-0000-000000000123',
    category: 'Community',
    financialGoal: 2500,
    problem: 'Creators need a clearer way to show traction and needs.',
    imageBlobNames: ['hero.jpg'],
    collaboratorsIdP: ['collab-1'],
    contacts: ['team@kickoff.test'],
    tags: ['events', 'creators'],
    backerIds: ['backer-1'],
    follow: {
      followersCount: 3,
      isFollowing: false,
      receiveInAppNotifications: true,
      receiveEmailNotifications: true
    },
    updates: [],
    startDate: new Date('2026-03-01T09:00:00.000Z'),
    endDate: new Date('2026-05-01T09:00:00.000Z'),
    settingsId: 'settings-1'
  };

  async function setup(
    routeId: string | null = null,
    navigationState: Record<string, unknown> = {}
  ): Promise<{
    fixture: ComponentFixture<ProjectCreate>;
    component: ProjectCreate;
    projectService: {
      getById: ReturnType<typeof vi.fn>;
      createProject: ReturnType<typeof vi.fn>;
      updateProject: ReturnType<typeof vi.fn>;
    };
  }> {
    const projectService = {
      getById: vi.fn(),
      createProject: vi.fn(),
      updateProject: vi.fn()
    };

    if (routeId) {
      projectService.getById.mockReturnValue(from(Promise.resolve(baseProject)));
    }

    projectService.createProject.mockReturnValue(of(baseProject));
    projectService.updateProject.mockReturnValue(of(baseProject));

    await TestBed.configureTestingModule({
      imports: [ProjectCreate],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(routeId ? { id: routeId } : {})
            }
          }
        },
        { provide: ProjectService, useValue: projectService }
      ]
    }).compileComponents();

    window.history.replaceState(navigationState, '');
    const fixture = TestBed.createComponent(ProjectCreate);
    const component = fixture.componentInstance;

    fixture.detectChanges();
    await fixture.whenStable();
    await new Promise(resolve => setTimeout(resolve, 0));
    fixture.detectChanges();

    return { fixture, component, projectService };
  }

  it('renders the launch flow by default', async () => {
    const { fixture, projectService } = await setup();

    expect(projectService.getById).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Launch A Project');
    expect(fixture.nativeElement.textContent).toContain('Create Project');
  });

  it('exposes project settings guidance in edit mode', async () => {
    const { component } = await setup();

    (component as ProjectCreate & { mode: 'create' | 'edit' }).mode = 'edit';

    expect((component as ProjectCreate & { pageEyebrow: string }).pageEyebrow).toBe('Project Settings');
    expect((component as ProjectCreate & { strategyVerdict: string }).strategyVerdict)
      .toBe('Yes, this is a valid strategy for KickOff.');
    expect((component as ProjectCreate & {
      postLaunchPolicies: Array<{ title: string }>;
    }).postLaunchPolicies.map(policy => policy.title)).toEqual([
      'Project description and public content',
      'Reward details',
      'Funding goal',
      'Campaign duration',
      'Shipping estimates, FAQs, and risks'
    ]);
  });

  it('hydrates edit mode from navigation state without waiting on the API call to finish', async () => {
    const { component } = await setup(baseProject.id, { projectSnapshot: baseProject });

    expect(component['isLoading']).toBe(false);
    expect(component['projectForm'].controls.goal.value).toBe(baseProject.goal);
    expect(component['contactCount']).toBe(1);
  });
});
