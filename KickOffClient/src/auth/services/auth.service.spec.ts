import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { AuthInterceptor } from '../interceptors/auth.interceptor';
import { AuthService } from './auth.service';
import { AuthStateService } from './auth-state.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let authState: AuthStateService;

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        AuthStateService,
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    authState = TestBed.inject(AuthStateService);
  });

  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    httpMock.verify();
  });

  it('skips auth restore work on startup when there is no local auth state', async () => {
    await service.initialize();

    expect(authState.isInitialized()).toBe(true);
    expect(authState.isLoading()).toBe(false);
    expect(authState.currentUser()).toBeNull();
  });

  it('restores a marked session by refreshing before loading the current user', async () => {
    localStorage.setItem('kickoff.auth.session', '1');

    const initializePromise = service.initialize();

    const refreshRequest = httpMock.expectOne('/api/auth/refresh');
    refreshRequest.flush({ accessToken: 'fresh-token' });

    const meRequest = httpMock.expectOne('/api/auth/me');
    expect(meRequest.request.headers.get('Authorization')).toBe('Bearer fresh-token');
    meRequest.flush({
      id: 'user-1',
      email: 'daniel@example.com',
      username: 'Daniel',
      role: 'producer',
      roles: ['producer']
    });

    await initializePromise;

    expect(authState.currentUser()?.id).toBe('user-1');
    expect(localStorage.getItem('accessToken')).toBe('fresh-token');
  });

  it('registers a user and returns the email confirmation response payload', async () => {
    const registerPromise = service.register('jane@example.com', 'Sup3r!Pass', 'janedoe');

    const request = httpMock.expectOne('/api/auth/register');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'jane@example.com',
      password: 'Sup3r!Pass',
      username: 'janedoe'
    });

    request.flush({
      requiresEmailConfirmation: false,
      emailDeliveryEnabled: true,
      message: 'Check your inbox.'
    });

    await expect(registerPromise).resolves.toEqual({
      requiresEmailConfirmation: false,
      emailDeliveryEnabled: true,
      message: 'Check your inbox.'
    });
  });

  it('completes login even when the current user email is not confirmed yet', async () => {
    const loginPromise = firstValueFrom(service.login('jane@example.com', 'Sup3r!Pass'));

    const loginRequest = httpMock.expectOne('/api/auth/login');
    expect(loginRequest.request.method).toBe('POST');
    expect(loginRequest.request.body).toEqual({
      email: 'jane@example.com',
      userName: 'jane@example.com',
      password: 'Sup3r!Pass'
    });

    loginRequest.flush({
      accessToken: 'session-token'
    });

    const meRequest = httpMock.expectOne('/api/auth/me');
    expect(meRequest.request.headers.get('Authorization')).toBe('Bearer session-token');
    meRequest.flush({
      id: 'user-1',
      email: 'jane@example.com',
      userName: 'Jane',
      role: 'User',
      roles: ['User'],
      emailConfirmed: false
    });

    await expect(loginPromise).resolves.toMatchObject({
      id: 'user-1',
      emailConfirmed: false
    });
    expect(authState.currentUser()?.emailConfirmed).toBe(false);
    expect(localStorage.getItem('accessToken')).toBe('session-token');
    expect(localStorage.getItem('kickoff.auth.session')).toBe('1');
  });

  it('confirms email addresses through the public confirmation endpoint', async () => {
    const confirmationPromise = firstValueFrom(service.confirmEmail('user-123', 'encoded-token'));

    const request = httpMock.expectOne(
      req =>
        req.url === '/api/auth/confirm-email' &&
        req.params.get('userId') === 'user-123' &&
        req.params.get('code') === 'encoded-token'
    );

    expect(request.request.method).toBe('GET');
    request.flush({
      success: true,
      alreadyConfirmed: false,
      message: 'Email confirmed.'
    });

    await expect(confirmationPromise).resolves.toEqual({
      success: true,
      alreadyConfirmed: false,
      message: 'Email confirmed.'
    });
  });

  it('starts a forgot-password request through the public recovery endpoint', async () => {
    const forgotPasswordPromise = firstValueFrom(service.forgotPassword('jane@example.com'));

    const request = httpMock.expectOne('/api/auth/forgot-password');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'jane@example.com'
    });

    request.flush({
      message: 'If an account exists for that email, we sent password reset instructions.'
    });

    await expect(forgotPasswordPromise).resolves.toEqual({
      message: 'If an account exists for that email, we sent password reset instructions.'
    });
  });

  it('submits a password reset with the email, token, and new password', async () => {
    const resetPasswordPromise = firstValueFrom(
      service.resetPassword('jane@example.com', 'encoded-token', 'Sup3r!Pass')
    );

    const request = httpMock.expectOne('/api/auth/reset-password');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'jane@example.com',
      code: 'encoded-token',
      newPassword: 'Sup3r!Pass'
    });

    request.flush({
      message: 'Your password has been reset.'
    });

    await expect(resetPasswordPromise).resolves.toEqual({
      message: 'Your password has been reset.'
    });
  });

  it('submits an authenticated password change request', async () => {
    localStorage.setItem('accessToken', 'session-token');

    const changePasswordPromise = firstValueFrom(
      service.changePassword('OldPass!1', 'NewPass!2')
    );

    const request = httpMock.expectOne('/api/auth/change-password');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Authorization')).toBe('Bearer session-token');
    expect(request.request.body).toEqual({
      currentPassword: 'OldPass!1',
      newPassword: 'NewPass!2'
    });

    request.flush({
      message: 'Your password has been updated.',
      accessToken: 'rotated-token'
    });

    await expect(changePasswordPromise).resolves.toEqual({
      message: 'Your password has been updated.',
      accessToken: 'rotated-token'
    });
    expect(localStorage.getItem('accessToken')).toBe('rotated-token');
  });

  it('updates the current user and verification state after changing email', async () => {
    localStorage.setItem('accessToken', 'session-token');

    const changeEmailPromise = firstValueFrom(
      service.changeEmail('new@example.com', 'OldPass!1')
    );

    const request = httpMock.expectOne('/api/auth/change-email');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Authorization')).toBe('Bearer session-token');
    expect(request.request.body).toEqual({
      newEmail: 'new@example.com',
      currentPassword: 'OldPass!1'
    });

    request.flush({
      message: 'Your email address has been updated. We sent a confirmation link to your email.',
      emailDeliveryEnabled: false,
      verificationUrl: 'https://kickoff.local/auth/verify-email?email=new@example.com',
      accessToken: 'fresh-token',
      user: {
        id: 'user-1',
        email: 'new@example.com',
        userName: 'Jane',
        role: 'User',
        roles: ['User'],
        emailConfirmed: false,
        canDeleteAccount: true
      }
    });

    await expect(changeEmailPromise).resolves.toMatchObject({
      emailDeliveryEnabled: false,
      accessToken: 'fresh-token'
    });
    expect(localStorage.getItem('accessToken')).toBe('fresh-token');
    expect(authState.currentUser()?.email).toBe('new@example.com');
    expect(authState.currentUser()?.emailConfirmed).toBe(false);
    expect(service.getVerificationState('new@example.com')).toEqual({
      email: 'new@example.com',
      emailDeliveryEnabled: false,
      message: 'Your email address has been updated. We sent a confirmation link to your email.',
      verificationUrl: 'https://kickoff.local/auth/verify-email?email=new@example.com'
    });
  });

  it('clears local auth state after account deactivation succeeds', async () => {
    localStorage.setItem('accessToken', 'session-token');
    localStorage.setItem('kickoff.auth.session', '1');
    authState.setUser({
      id: 'user-1',
      email: 'jane@example.com',
      userName: 'Jane'
    });

    const deactivatePromise = firstValueFrom(
      service.deactivateAccount('OldPass!1', 'DEACTIVATE')
    );

    const request = httpMock.expectOne('/api/auth/deactivate-account');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      currentPassword: 'OldPass!1',
      confirmationText: 'DEACTIVATE'
    });

    request.flush({
      message: 'Your account has been deactivated.'
    });

    await expect(deactivatePromise).resolves.toEqual({
      message: 'Your account has been deactivated.'
    });
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('kickoff.auth.session')).toBeNull();
    expect(authState.currentUser()).toBeNull();
  });

  it('persists the latest verification response so the verify-email screen can reload it', () => {
    service.rememberVerificationState('Jane@Example.com', {
      emailDeliveryEnabled: false,
      message: 'Use the preview link below to verify it.',
      verificationUrl: 'https://kickoff.local/auth/confirm-email?userId=123&code=abc'
    });

    expect(service.getVerificationState('jane@example.com')).toEqual({
      email: 'jane@example.com',
      emailDeliveryEnabled: false,
      message: 'Use the preview link below to verify it.',
      verificationUrl: 'https://kickoff.local/auth/confirm-email?userId=123&code=abc'
    });

    service.clearVerificationState('jane@example.com');

    expect(service.getVerificationState('jane@example.com')).toBeNull();
  });
});
