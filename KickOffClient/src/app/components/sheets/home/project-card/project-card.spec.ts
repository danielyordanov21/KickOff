import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ProjectCard } from './project-card';

describe('ProjectCard', () => {
  let component: ProjectCard;
  let fixture: ComponentFixture<ProjectCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectCard],
      providers: [provideRouter([])]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProjectCard);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('project', {
      id: 'project-1',
      name: 'Launch Week',
      description: 'Kickoff planning and execution hub.',
      owner: 'Daniel',
      state: 'Active',
      imageUrl: null
    });
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders owner and status details for projects without an image', async () => {
    fixture.componentRef.setInput('project', {
      id: 'project-2',
      name: 'Creator Studio',
      description: 'A planning space for launch calendars, collaborator check-ins, and campaign milestones.',
      owner: 'Daniel',
      state: 'Active',
      imageUrl: null
    });
    fixture.detectChanges();
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    const ownerLabel = element.querySelector('.owner-label')?.textContent?.trim();
    const ownerBadge = element.querySelector('.owner-badge')?.textContent?.trim();
    const footerCopy = element.querySelector('.footer-copy')?.textContent?.trim();
    const description = element.querySelector('.project-description')?.textContent?.trim();

    expect(ownerLabel).toBe('Daniel');
    expect(ownerBadge).toBe('D');
    expect(footerCopy).toBe('Funding snapshot');
    expect(description).toContain('campaign milestones');
  });

  it('renders funding progress and remaining time when campaign data is available', async () => {
    fixture.componentRef.setInput('project', {
      id: 'project-4',
      name: 'Atlas AI Ops',
      description: 'Campaign copy for a funded product launch.',
      owner: 'Daniel',
      state: 'Active',
      imageUrl: null,
      financialGoal: 185000,
      financialRaised: 92000,
      endDate: new Date(Date.now() + (42 * 24 * 60 * 60 * 1000)).toISOString()
    });
    fixture.detectChanges();
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    const fundingValue = element.querySelector('.funding-stat .campaign-value')?.textContent?.trim();
    const timelineValue = element.querySelector('.timeline-stat .campaign-value')?.textContent?.trim();
    const supportingCopy = element.querySelector('.campaign-supporting')?.textContent?.trim();

    expect(fundingValue).toBe('50%');
    expect(timelineValue).toBe('42 days left');
    expect(supportingCopy).toContain('$92,000 pledged of $185,000 goal');
  });

  it('shows fallback copy when the project description is empty', async () => {
    fixture.componentRef.setInput('project', {
      id: 'project-3',
      name: 'Silent Launch',
      description: '   ',
      owner: 'Team',
      state: 'Proposed',
      imageUrl: null
    });
    fixture.detectChanges();
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    const description = element.querySelector('.project-description')?.textContent?.trim();
    const fundingValue = element.querySelector('.funding-stat .campaign-value')?.textContent?.trim();
    const timelineValue = element.querySelector('.timeline-stat .campaign-value')?.textContent?.trim();

    expect(description).toBe('No description has been added yet.');
    expect(fundingValue).toBe('TBD');
    expect(timelineValue).toBe('No deadline');
  });
});
