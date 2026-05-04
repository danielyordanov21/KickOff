import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { Subject } from 'rxjs';

import { AuthInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';

describe('AuthInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let refreshSubject: Subject<{ accessToken: string }>;
  let clearLocalAuthState: ReturnType<typeof vi.fn>;
  let refreshToken: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    refreshSubject = new Subject<{ accessToken: string }>();
    clearLocalAuthState = vi.fn();
    refreshToken = vi.fn().mockReturnValue(refreshSubject.asObservable());
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
        {
          provide: AuthService,
          useValue: {
            refreshToken,
            clearLocalAuthState
          }
        }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    localStorage.clear();
    httpMock.verify();
  });

  it('retries concurrent unauthorized requests after a single refresh', () => {
    localStorage.setItem('accessToken', 'stale-token');

    const responses: string[] = [];

    http.get('/api/project/search', { responseType: 'text' }).subscribe(value => responses.push(value));
    http.get('/api/project/paginated', { responseType: 'text' }).subscribe(value => responses.push(value));

    const firstRequest = httpMock.expectOne('/api/project/search');
    const secondRequest = httpMock.expectOne('/api/project/paginated');

    expect(firstRequest.request.headers.get('Authorization')).toBe('Bearer stale-token');
    expect(secondRequest.request.headers.get('Authorization')).toBe('Bearer stale-token');

    firstRequest.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    secondRequest.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(refreshToken).toHaveBeenCalledTimes(1);

    refreshSubject.next({ accessToken: 'fresh-token' });
    refreshSubject.complete();

    const retriedSearch = httpMock.expectOne('/api/project/search');
    const retriedPaginated = httpMock.expectOne('/api/project/paginated');

    expect(retriedSearch.request.headers.get('Authorization')).toBe('Bearer fresh-token');
    expect(retriedPaginated.request.headers.get('Authorization')).toBe('Bearer fresh-token');

    retriedSearch.flush('search-result');
    retriedPaginated.flush('paginated-result');

    expect(responses).toEqual(['search-result', 'paginated-result']);
  });

  it('clears local auth state when refresh is rejected', () => {
    localStorage.setItem('accessToken', 'stale-token');

    http.get('/api/project/search').subscribe({
      next: () => {
        throw new Error('expected the request to fail');
      },
      error: () => undefined
    });

    const request = httpMock.expectOne('/api/project/search');
    request.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    refreshSubject.error({ status: 403 });

    expect(clearLocalAuthState).toHaveBeenCalled();
  });

  it('skips attaching auth headers to public password recovery endpoints', () => {
    localStorage.setItem('accessToken', 'stale-token');

    http.post('/api/auth/forgot-password', { email: 'user@kickoff.app' }).subscribe();
    http.post('/api/auth/reset-password', {
      email: 'user@kickoff.app',
      code: 'encoded-token',
      newPassword: 'Sup3r!Pass'
    }).subscribe();

    const forgotPasswordRequest = httpMock.expectOne('/api/auth/forgot-password');
    const resetPasswordRequest = httpMock.expectOne('/api/auth/reset-password');

    expect(forgotPasswordRequest.request.headers.has('Authorization')).toBe(false);
    expect(resetPasswordRequest.request.headers.has('Authorization')).toBe(false);
    expect(refreshToken).not.toHaveBeenCalled();

    forgotPasswordRequest.flush({ message: 'ok' });
    resetPasswordRequest.flush({ message: 'ok' });
  });
});
