import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

import { AuthStateService } from '../../services/auth-state.service';
import { AuthService, PendingVerificationState } from '../../services/auth.service';
import { extractApiErrorMessage } from '../../utils/extract-api-error-message';

type VerificationUpdateSource = 'initial' | 'resend';
type VerificationNavigationState = {
  verificationUrl?: string | null;
  email?: string | null;
  emailDeliveryEnabled?: boolean | null;
  message?: string | null;
};

@Component({
  selector: 'app-verify-email',
  imports: [
    CommonModule,
    RouterModule
  ],
  templateUrl: './verify-email.html',
  styleUrl: './verify-email.scss'
})
export class VerifyEmail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly authStateService = inject(AuthStateService);

  email = '';
  verificationMessage =
    'Verify your email to keep your KickOff account secure and easy to recover.';
  verificationError: string | null = null;
  verificationPreviewUrl: string | null = null;
  verificationStatusHint: string | null =
    'Use the confirmation link whenever you are ready to finish verifying your account.';
  verificationStatusTitle: string | null = 'Check your inbox';
  emailDeliveryEnabled: boolean | null = null;
  isVerifying = false;
  isResending = false;
  emailConfirmed = false;

  get resendActionLabel(): string {
    if (this.isResending) {
      return this.verificationPreviewUrl
        ? 'Refreshing preview link...'
        : 'Sending link...';
    }

    return this.verificationPreviewUrl
      ? 'Refresh preview link'
      : 'Resend confirmation email';
  }

  get previewCardTitle(): string {
    if (this.emailConfirmed) {
      return 'Verification complete';
    }

    return this.verificationPreviewUrl && this.emailDeliveryEnabled === false
      ? 'Local verification preview ready'
      : 'Verification link preview';
  }

  ngOnInit(): void {
    const queryParams = this.route.snapshot.queryParamMap;
    const userId = queryParams.get('userId');
    const code = queryParams.get('code');
    const email = queryParams.get('email')?.trim() ?? '';
    const sent = queryParams.get('sent') === '1';
    const navigationState = window.history.state as {
      verificationUrl?: string | null;
      email?: string | null;
      emailDeliveryEnabled?: boolean | null;
      message?: string | null;
    };

    this.email = email;
    const verificationState =
      this.getVerificationNavigationState(email, navigationState)
      ?? this.authService.getVerificationState(email);

    if (verificationState) {
      this.applyVerificationState(verificationState, sent ? 'initial' : 'resend');
    } else if (sent) {
      this.verificationMessage = 'Check your inbox for a confirmation link. You can still sign in while you wait.';
      this.verificationStatusTitle = 'Check your inbox';
      this.verificationStatusHint = 'Use the confirmation email whenever you are ready to finish verifying your account.';
    }

    if (userId && code) {
      this.confirmEmail(userId, code);
    }
  }

  resendConfirmation(): void {
    if (!this.email || this.isResending) return;

    this.isResending = true;
    this.verificationError = null;

    this.authService.resendConfirmation(this.email).subscribe({
      next: response => {
        this.isResending = false;
        this.emailConfirmed = response.alreadyConfirmed;
        this.emailDeliveryEnabled = response.emailDeliveryEnabled;

        if (response.alreadyConfirmed) {
          this.authService.clearVerificationState(this.email);
          this.verificationPreviewUrl = null;
          this.verificationMessage = response.message;
          this.verificationStatusTitle = 'Email already confirmed';
          this.verificationStatusHint = 'You can continue to sign in now.';
          this.authStateService.updateCurrentUser(user => ({
            ...user,
            emailConfirmed: true
          }));
          return;
        }

        this.authService.rememberVerificationState(this.email, response);
        this.applyVerificationState({
          email: this.normalizeEmail(this.email),
          emailDeliveryEnabled: response.emailDeliveryEnabled,
          message: response.message,
          verificationUrl: response.verificationUrl ?? null
        }, 'resend');
      },
      error: (err: HttpErrorResponse) => {
        this.isResending = false;
        this.verificationError = extractApiErrorMessage(
          err,
          'We could not resend the confirmation email.'
        );
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/auth', 'login'], {
      queryParams: {
        email: this.email || undefined,
        verified: this.emailConfirmed ? '1' : undefined
      }
    });
  }

  private confirmEmail(userId: string, code: string): void {
    this.isVerifying = true;
    this.verificationError = null;

    this.authService.confirmEmail(userId, code).subscribe({
      next: response => {
        this.isVerifying = false;
        this.emailConfirmed = response.success;
        this.verificationMessage = response.message;
        this.verificationPreviewUrl = null;
        this.verificationStatusTitle = response.alreadyConfirmed
          ? 'Email already confirmed'
          : 'Email confirmed';
        this.verificationStatusHint = 'You can continue to sign in now.';
        this.authService.clearVerificationState(this.email);

        if (response.success) {
          this.authStateService.updateCurrentUser(user => ({
            ...user,
            emailConfirmed: true
          }));
        }
      },
      error: (err: HttpErrorResponse) => {
        this.isVerifying = false;
        this.verificationError = extractApiErrorMessage(
          err,
          'This verification link is no longer valid.'
        );
      }
    });
  }

  private applyVerificationState(
    state: PendingVerificationState,
    source: VerificationUpdateSource
  ): void {
    this.authService.rememberVerificationState(state.email, state);
    this.verificationPreviewUrl = state.verificationUrl ?? null;
    this.verificationMessage = state.message;
    this.emailDeliveryEnabled = state.emailDeliveryEnabled;

    if (this.verificationPreviewUrl && state.emailDeliveryEnabled === false) {
      this.verificationStatusTitle = source === 'resend'
        ? 'Fresh preview link ready'
        : 'Local verification preview ready';
      this.verificationStatusHint =
        'SMTP is disabled locally, so you can open the latest confirmation link directly from this page.';
      return;
    }

    if (source === 'resend') {
      this.verificationStatusTitle = 'Confirmation request updated';
      this.verificationStatusHint = this.verificationPreviewUrl
        ? 'A fresh confirmation request was created, and the latest link preview is ready below.'
        : 'A fresh confirmation email should be on its way now.';
      return;
    }

    this.verificationStatusTitle = this.verificationPreviewUrl
      ? 'Confirmation link ready'
      : 'Check your inbox';
    this.verificationStatusHint = this.verificationPreviewUrl
      ? 'You can use the link below or the email we just sent to verify this account.'
      : 'Use the confirmation email to finish activating your account.';
  }

  private getVerificationNavigationState(
    email: string,
    navigationState: VerificationNavigationState | null | undefined
  ): PendingVerificationState | null {
    const normalizedEmail = this.normalizeEmail(email);
    const stateCandidates = [
      this.router.getCurrentNavigation()?.extras.state as VerificationNavigationState | undefined,
      navigationState
    ];

    for (const candidate of stateCandidates) {
      if (!candidate) {
        continue;
      }

      const message = typeof candidate.message === 'string'
        ? candidate.message.trim()
        : '';

      if (!message && typeof candidate.verificationUrl !== 'string') {
        continue;
      }

      const candidateEmail = this.normalizeEmail(candidate.email ?? email);
      if (normalizedEmail && candidateEmail && candidateEmail !== normalizedEmail) {
        continue;
      }

      return {
        email: candidateEmail || normalizedEmail,
        emailDeliveryEnabled: typeof candidate.emailDeliveryEnabled === 'boolean'
          ? candidate.emailDeliveryEnabled
          : true,
        message: message || this.verificationMessage,
        verificationUrl: typeof candidate.verificationUrl === 'string'
          ? candidate.verificationUrl
          : null
      };
    }

    return null;
  }

  private normalizeEmail(email: string | null | undefined): string {
    return email?.trim().toLowerCase() ?? '';
  }
}
