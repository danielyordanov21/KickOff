import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { UserService } from './user.service';
import { AuthStateService } from './auth-state.service';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;
  let capturedUsers: unknown[];

  beforeEach(() => {
    capturedUsers = [];

    TestBed.configureTestingModule({
      providers: [
        UserService,
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AuthStateService,
          useValue: {
            setUser: (user: unknown) => capturedUsers.push(user),
            currentUser: () => null,
          },
        },
      ],
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('upgrades the current user to producer and refreshes auth state', async () => {
    const responseBody = {
      id: 'user-1',
      idP: 'public-1',
      userName: 'Casey Producer',
      email: 'casey@example.com',
      roles: ['Producer', 'User'],
      role: 'Producer',
    };

    const requestPromise = firstValueFrom(service.becomeProducer());

    const request = httpMock.expectOne('/api/users/become-producer');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({});

    request.flush(responseBody);

    const result = await requestPromise;

    expect(result).toEqual(responseBody);
    expect(capturedUsers).toEqual([responseBody]);
  });

  it('updates profile details and refreshes auth state', async () => {
    const responseBody = {
      id: 'user-1',
      idP: 'public-1',
      userName: 'Casey Updated',
      email: 'casey@example.com',
      canDeleteAccount: true,
    };

    const requestPromise = firstValueFrom(service.updateProfile({
      userName: 'Casey Updated',
    }));

    const request = httpMock.expectOne('/api/users/profile');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      userName: 'Casey Updated',
    });

    request.flush(responseBody);

    const result = await requestPromise;

    expect(result).toEqual(responseBody);
    expect(capturedUsers).toEqual([responseBody]);
  });

  it('updates chat preferences and refreshes auth state', async () => {
    const responseBody = {
      id: 'user-1',
      idP: 'public-1',
      userName: 'Casey Producer',
      email: 'casey@example.com',
      preferredChatLanguage: 'fr',
      showOriginalChatTextByDefault: true,
    };

    const requestPromise = firstValueFrom(service.updateChatPreferences({
      preferredChatLanguage: 'fr',
      showOriginalChatTextByDefault: true,
    }));

    const request = httpMock.expectOne('/api/users/chat-preferences');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      preferredChatLanguage: 'fr',
      showOriginalChatTextByDefault: true,
    });

    request.flush(responseBody);

    const result = await requestPromise;

    expect(result).toEqual(responseBody);
    expect(capturedUsers).toEqual([responseBody]);
  });

  it('loads discover creators from the producer-only endpoint and normalizes the response', async () => {
    const requestPromise = firstValueFrom(service.getDiscoverProducers());

    const request = httpMock.expectOne('/api/users/get-discover');
    expect(request.request.method).toBe('GET');

    request.flush([
      {
        publicId: 'producer-1',
        username: 'Casey Producer',
        profilePictureUrl: 'https://cdn.example.com/casey.png',
      },
      {
        id: 'producer-2',
        userName: 'Jordan Maker',
      },
      {
        publicId: '',
        username: 'No Id',
      },
      {
        publicId: 'producer-4',
        username: '',
      },
    ]);

    const result = await requestPromise;

    expect(result).toEqual([
      {
        id: 'producer-1',
        userName: 'Casey Producer',
        profilePictureUrl: 'https://cdn.example.com/casey.png',
      },
      {
        id: 'producer-2',
        userName: 'Jordan Maker',
        profilePictureUrl: '',
      },
    ]);
  });
});
