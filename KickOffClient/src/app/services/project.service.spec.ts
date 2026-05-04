import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { ProjectService } from './project.service';

describe('ProjectService', () => {
  let service: ProjectService;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ProjectService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ProjectService);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  it('maps numeric project state values from the detail endpoint into display labels', async () => {
    const projectPromise = firstValueFrom(service.getById('project-1'));
    const request = httpController.expectOne('/api/project/project-1');

    expect(request.request.method).toBe('GET');

    request.flush({
      id: 'project-1',
      name: 'KickOff Live Sessions',
      headline: 'Creators meeting supporters in one place',
      goal: 'Launch recurring creator showcases with project discovery built in.',
      description: 'Detailed project view payload from the API.',
      state: 2,
      owner: 'Daniel',
      ownerId: 'owner-1',
      ownerPublicId: '00000000-0000-0000-0000-000000000123',
      imageUrls: ['https://example.com/hero.jpg', ''],
      imageBlobNames: ['hero.jpg'],
      contacts: ['team@kickoff.test', ''],
      tags: ['events', ''],
      collaboratorsIdP: ['collab-1', ''],
      backerIds: ['backer-1', ''],
      follow: {
        followersCount: 4,
        isFollowing: true,
        receiveInAppNotifications: true,
        receiveEmailNotifications: false
      },
      updates: [
        {
          id: 'update-1',
          projectId: 'project-1',
          title: ' First milestone ',
          content: ' We shipped the first milestone to internal testers. ',
          authorUserId: 'owner-1',
          authorName: ' Daniel ',
          createdAt: '2026-03-11T09:00:00Z',
          updatedAt: '2026-03-11T12:30:00Z',
          isEdited: true
        }
      ],
      startDate: '2026-03-01T09:00:00Z',
      endDate: '2026-05-01T09:00:00Z',
      settingsId: 'settings-1'
    });

    const project = await projectPromise;

    expect(project.state).toBe('Active');
    expect(project.ownerPublicId).toBe('00000000-0000-0000-0000-000000000123');
    expect(project.imageUrls).toEqual(['https://example.com/hero.jpg']);
    expect(project.contacts).toEqual(['team@kickoff.test']);
    expect(project.tags).toEqual(['events']);
    expect(project.collaboratorsIdP).toEqual(['collab-1']);
    expect(project.backerIds).toEqual(['backer-1']);
    expect(project.follow).toEqual({
      followersCount: 4,
      isFollowing: true,
      receiveInAppNotifications: true,
      receiveEmailNotifications: false
    });
    expect(project.updates).toEqual([
      {
        id: 'update-1',
        projectId: 'project-1',
        title: 'First milestone',
        content: 'We shipped the first milestone to internal testers.',
        authorUserId: 'owner-1',
        authorName: 'Daniel',
        createdAt: new Date('2026-03-11T09:00:00Z'),
        updatedAt: new Date('2026-03-11T12:30:00Z'),
        isEdited: true
      }
    ]);
  });

  it('maps project notification feeds into trimmed client models', async () => {
    const feedPromise = firstValueFrom(service.getNotifications(8));
    const request = httpController.expectOne(req =>
      req.url === '/api/project/notifications' &&
      req.params.get('take') === '8'
    );

    expect(request.request.method).toBe('GET');

    request.flush({
      notifications: [
        {
          id: 'notification-1',
          projectId: 'project-1',
          projectName: ' KickOff Live Sessions ',
          projectUpdateId: ' ',
          title: ' New update from KickOff Live Sessions ',
          message: ' Pilot launch date confirmed. ',
          isRead: false,
          createdAt: '2026-04-20T12:00:00Z'
        }
      ],
      unreadCount: 3
    });

    const feed = await feedPromise;

    expect(feed).toEqual({
      notifications: [
        {
          id: 'notification-1',
          projectId: 'project-1',
          projectName: 'KickOff Live Sessions',
          projectUpdateId: undefined,
          title: 'New update from KickOff Live Sessions',
          message: 'Pilot launch date confirmed.',
          isRead: false,
          createdAt: new Date('2026-04-20T12:00:00Z')
        }
      ],
      unreadCount: 3
    });
  });
});
