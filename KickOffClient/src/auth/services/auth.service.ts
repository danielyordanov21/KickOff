import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { switchMap, tap } from 'rxjs/operators';

import { User } from '../user.model';
import { AuthStateService } from './auth-state.service';

export interface RegisterResponse {
  requiresEmailConfirmation: boolean;
  emailDeliveryEnabled: boolean;
  message: string;
  verificationUrl?: string | null;
}

export interface ConfirmEmailResponse {
  success: boolean;
  alreadyConfirmed: boolean;
  message: string;
}

export interface ResendConfirmationResponse {
  alreadyConfirmed: boolean;
  emailDeliveryEnabled: boolean;
  message: string;
  verificationUrl?: string | null;
}

export interface ForgotPasswordResponse {
  message: string;
}

export interface ResetPasswordResponse {
  message: string;
}

export interface ChangePasswordResponse {
  message: string;
  accessToken?: string | null;
}

export interface ChangeEmailResponse {
  message: string;
  emailDeliveryEnabled: boolean;
  verificationUrl?: string | null;
  accessToken?: string | null;
  user: User;
}

export interface AccountActionResponse {
  message: string;
}

export interface PendingVerificationState {
  email: string;
  emailDeliveryEnabled: boolean;
  message: string;
  verificationUrl?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = '/api/auth';
  private readonly pendingVerificationStateKey = 'kickoff.auth.verification.pending';
  private readonly sessionMarkerKey = 'kickoff.auth.session';
  private initializePromise: Promise<void> | null = null;

  private http = inject(HttpClient);
  private authState = inject(AuthStateService);

  async initialize(): Promise<void> {
    if (this.authState.isInitialized()) {
      return;
    }

    if (this.initializePromise) {
      return this.initializePromise;
    }

    this.initializePromise = (async () => {
      this.authState.setLoading(true);
      const existingUser = this.authState.currentUser();
      const hasAccessToken = this.hasStoredAccessToken();
      const hasSessionMarker = this.hasSessionMarker();

      try {
        if (!existingUser && !hasAccessToken && !hasSessionMarker) {
          this.authState.setUser(null);
          return;
        }

        const user = await firstValueFrom(
          hasAccessToken
            ? this.getCurrentUser()
            : this.refreshToken().pipe(
                tap(({ accessToken }) => this.storeAccessToken(accessToken)),
                switchMap(() => this.getCurrentUser())
              )
        );

        this.authState.setUser(user);
        this.setSessionMarker(true);
      } catch (error) {
        const status = error instanceof HttpErrorResponse ? error.status : null;
        const authRejected = status === 401 || status === 403;

        // Preserve the in-memory user on transient failures so one failed request
        // does not make the UI look logged out.
        if (authRejected) {
          this.clearStoredAuthArtifacts();
        }

        if (!existingUser && (authRejected || (!hasAccessToken && !hasSessionMarker))) {
          this.authState.setUser(null);
        }
      } finally {
        this.authState.setLoading(false);
        this.authState.setInitialized(true);
        this.initializePromise = null;
      }
    })();

    return this.initializePromise;
  }

  getUser(id: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/${id}`);
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/me`);
  }

  async register(email: string, password: string, username?: string): Promise<RegisterResponse> {
    return await firstValueFrom(
      this.http.post<RegisterResponse>(`${this.apiUrl}/register`, {
        email,
        password,
        username: username || email
      })
    );
  }

  login(identifier: string, password: string): Observable<User> {
    return this.http.post<{ accessToken: string }>(
      `${this.apiUrl}/login`,
      {
        email: identifier,
        userName: identifier,
        password
      }
    ).pipe(
      tap(res => this.storeAccessToken(res.accessToken)),
      switchMap(() => this.http.get<User>(`${this.apiUrl}/me`)),
      tap(user => {
        this.authState.setUser(user);
        this.setSessionMarker(true);
      })
    );
  }

  refreshToken() {
    return this.http.post<{ accessToken: string }>(`${this.apiUrl}/refresh`, {});
  }

  confirmEmail(userId: string, code: string): Observable<ConfirmEmailResponse> {
    return this.http.get<ConfirmEmailResponse>(`${this.apiUrl}/confirm-email`, {
      params: {
        userId,
        code
      }
    });
  }

  resendConfirmation(email: string): Observable<ResendConfirmationResponse> {
    return this.http.post<ResendConfirmationResponse>(`${this.apiUrl}/resend-confirmation`, {
      email
    });
  }

  forgotPassword(email: string): Observable<ForgotPasswordResponse> {
    return this.http.post<ForgotPasswordResponse>(`${this.apiUrl}/forgot-password`, {
      email
    });
  }

  resetPassword(email: string, code: string, newPassword: string): Observable<ResetPasswordResponse> {
    return this.http.post<ResetPasswordResponse>(`${this.apiUrl}/reset-password`, {
      email,
      code,
      newPassword
    });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<ChangePasswordResponse> {
    return this.http.post<ChangePasswordResponse>(`${this.apiUrl}/change-password`, {
      currentPassword,
      newPassword
    }).pipe(
      tap(response => this.storeAccessTokenIfPresent(response.accessToken))
    );
  }

  changeEmail(newEmail: string, currentPassword: string): Observable<ChangeEmailResponse> {
    return this.http.post<ChangeEmailResponse>(`${this.apiUrl}/change-email`, {
      newEmail,
      currentPassword
    }).pipe(
      tap(response => {
        this.storeAccessTokenIfPresent(response.accessToken);
        this.authState.setUser(response.user);
        this.rememberVerificationState(response.user.email, {
          emailDeliveryEnabled: response.emailDeliveryEnabled,
          message: response.message,
          verificationUrl: response.verificationUrl ?? null
        });
      })
    );
  }

  deactivateAccount(currentPassword: string, confirmationText: string): Observable<AccountActionResponse> {
    return this.http.post<AccountActionResponse>(`${this.apiUrl}/deactivate-account`, {
      currentPassword,
      confirmationText
    }).pipe(
      tap(() => this.clearLocalAuthState())
    );
  }

  deleteAccount(currentPassword: string, confirmationText: string): Observable<AccountActionResponse> {
    return this.http.post<AccountActionResponse>(`${this.apiUrl}/delete-account`, {
      currentPassword,
      confirmationText
    }).pipe(
      tap(() => this.clearLocalAuthState())
    );
  }

  rememberVerificationState(
    email: string,
    response: Pick<PendingVerificationState, 'emailDeliveryEnabled' | 'message' | 'verificationUrl'>
  ): void {
    const normalizedEmail = email.trim().toLowerCase();
    if (!normalizedEmail) {
      return;
    }

    this.writePendingVerificationState({
      email: normalizedEmail,
      emailDeliveryEnabled: response.emailDeliveryEnabled,
      message: response.message,
      verificationUrl: response.verificationUrl ?? null
    });
  }

  getVerificationState(email?: string): PendingVerificationState | null {
    const state = this.readPendingVerificationState();
    if (!state) {
      return null;
    }

    if (!email) {
      return state;
    }

    return state.email === email.trim().toLowerCase()
      ? state
      : null;
  }

  clearVerificationState(email?: string): void {
    if (!email) {
      sessionStorage.removeItem(this.pendingVerificationStateKey);
      return;
    }

    const state = this.readPendingVerificationState();
    if (state?.email === email.trim().toLowerCase()) {
      sessionStorage.removeItem(this.pendingVerificationStateKey);
    }
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post(`${this.apiUrl}/logout`, {})
      );
    } finally {
      this.clearLocalAuthState();
    }
  }

  clearLocalAuthState(): void {
    this.clearStoredAuthArtifacts();
    this.initializePromise = null;
    this.authState.clear();
  }

  private hasStoredAccessToken(): boolean {
    return this.getStoredAccessToken() !== null;
  }

  private getStoredAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  private storeAccessToken(accessToken: string): void {
    localStorage.setItem('accessToken', accessToken);
  }

  private storeAccessTokenIfPresent(accessToken: string | null | undefined): void {
    const normalizedAccessToken = accessToken?.trim();
    if (!normalizedAccessToken) {
      return;
    }

    this.storeAccessToken(normalizedAccessToken);
  }

  private hasSessionMarker(): boolean {
    return localStorage.getItem(this.sessionMarkerKey) === '1';
  }

  private setSessionMarker(hasSession: boolean): void {
    if (hasSession) {
      localStorage.setItem(this.sessionMarkerKey, '1');
      return;
    }

    localStorage.removeItem(this.sessionMarkerKey);
  }

  private clearStoredAuthArtifacts(): void {
    localStorage.removeItem('accessToken');
    this.setSessionMarker(false);
  }

  private readPendingVerificationState(): PendingVerificationState | null {
    const rawState = sessionStorage.getItem(this.pendingVerificationStateKey);
    if (!rawState) {
      return null;
    }

    try {
      const parsedState = JSON.parse(rawState) as Partial<PendingVerificationState>;
      const normalizedEmail = typeof parsedState.email === 'string'
        ? parsedState.email.trim().toLowerCase()
        : '';

      if (
        !normalizedEmail ||
        typeof parsedState.emailDeliveryEnabled !== 'boolean' ||
        typeof parsedState.message !== 'string'
      ) {
        sessionStorage.removeItem(this.pendingVerificationStateKey);
        return null;
      }

      return {
        email: normalizedEmail,
        emailDeliveryEnabled: parsedState.emailDeliveryEnabled,
        message: parsedState.message,
        verificationUrl: typeof parsedState.verificationUrl === 'string'
          ? parsedState.verificationUrl
          : null
      };
    } catch {
      sessionStorage.removeItem(this.pendingVerificationStateKey);
      return null;
    }
  }

  private writePendingVerificationState(state: PendingVerificationState): void {
    sessionStorage.setItem(this.pendingVerificationStateKey, JSON.stringify(state));
  }
}
