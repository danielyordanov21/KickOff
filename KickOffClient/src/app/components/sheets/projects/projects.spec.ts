import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { AuthService } from '../../../../auth/services/auth.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { User } from '../../../../auth/user.model';
import { Projects } from './projects';

describe('Projects', () => {
  async function setup(user: User): Promise<{
    fixture: ComponentFixture<Projects>;
    authService: {
      getCurrentUser: ReturnType<typeof vi.fn>;
    };
    authState: {
      currentUser: ReturnType<typeof vi.fn>;
      setUser: ReturnType<typeof vi.fn>;
    };
  }> {
    const authService = {
      getCurrentUser: vi.fn().mockReturnValue(of(user)),
    };
    const authState = {
      currentUser: vi.fn(() => user),
      setUser: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [Projects],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        { provide: AuthStateService, useValue: authState },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(Projects);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    return { fixture, authService, authState };
  }

  it('renders a repository-style list for the current user projects', async () => {
    const user: User = {
      id: 'user-1',
      idP: 'public-1',
      userName: 'Daniel',
      email: 'daniel@example.com',
      role: 'producer',
      roles: ['producer'],
      projectIds: ['project-1', 'project-2'],
      projects: [
        {
          id: 'project-1',
          name: 'KickOff Live Sessions',
          description: 'A recurring launchpad for creator-led live sessions.',
          owner: 'Daniel',
          state: 'Active',
          financialGoal: 2500,
          financialRaised: 1600,
          endDate: '2026-06-01T09:00:00.000Z',
        },
        {
          id: 'project-2',
          name: 'ForgeLearn',
          description: 'Short-form training tracks for technical collaborators.',
          owner: 'Daniel',
          state: 'Completed',
          financialGoal: 1500,
          financialRaised: 1500,
          endDate: '2026-03-01T09:00:00.000Z',
        },
      ],
    };

    const { fixture, authService, authState } = await setup(user);
    const text = fixture.nativeElement.textContent;

    expect(authService.getCurrentUser).toHaveBeenCalled();
    expect(authState.setUser).toHaveBeenCalledWith(user);
    expect(text).toContain('Your project workspace');
    expect(text).toContain('KickOff Live Sessions');
    expect(text).toContain('ForgeLearn');
    expect(text).toContain('Repository View');
  });

  it('shows an empty state when the current account has no projects', async () => {
    const user: User = {
      id: 'user-2',
      idP: 'public-2',
      userName: 'Taylor',
      email: 'taylor@example.com',
      role: 'producer',
      roles: ['producer'],
      projectIds: [],
      projects: [],
    };

    const { fixture } = await setup(user);

    expect(fixture.nativeElement.textContent).toContain('No projects yet');
    expect(fixture.nativeElement.textContent).toContain('Create Project');
  });
});
