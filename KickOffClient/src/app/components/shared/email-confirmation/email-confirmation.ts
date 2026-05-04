import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../../../auth/services/auth.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { extractApiErrorMessage } from '../../../../auth/utils/extract-api-error-message';

@Component({
  selector: 'app-email-confirmation',
  imports: [],
  templateUrl: './email-confirmation.html',
  styleUrl: './email-confirmation.scss',
})
export class EmailConfirmation {
  protected readonly emailConfirmationMessage = signal<string | null>(null);
  protected readonly emailConfirmationError = signal<string | null>(null);
  protected readonly emailConfirmationResending = signal(false);
  protected readonly emailConfirmationDismissed = signal(false);
  protected readonly currentUser;
  protected readonly needsEmailConfirmation;
  protected readonly shouldShowEmailConfirmation;
  private lastEmailAlertUserKey: string | null = null;

  private readonly authService = inject(AuthService);
  private readonly authStateService = inject(AuthStateService);
  private readonly router = inject(Router);

  constructor() {
    this.currentUser = this.authStateService.currentUser;
    this.needsEmailConfirmation = computed(() => this.currentUser()?.emailConfirmed === false);
    this.shouldShowEmailConfirmation = computed(
      () => this.needsEmailConfirmation() && !this.emailConfirmationDismissed()
    );

    effect(() => {
      const user = this.currentUser();
      const nextKey = user ? `${user.id}:${user.emailConfirmed}` : null;

      if (nextKey === this.lastEmailAlertUserKey) {
        return;
      }

      this.lastEmailAlertUserKey = nextKey;
      this.emailConfirmationMessage.set(null);
      this.emailConfirmationError.set(null);
      this.emailConfirmationResending.set(false);
      this.emailConfirmationDismissed.set(false);
    });
  }

  protected dismissEmailConfirmation(): void {
    this.emailConfirmationDismissed.set(true);
  }

  protected resendEmailConfirmation(): void {
    const email = this.currentUser()?.email?.trim();
    if (!email || this.emailConfirmationResending()) {
      return;
    }

    this.emailConfirmationResending.set(true);
    this.emailConfirmationError.set(null);

    this.authService.resendConfirmation(email).subscribe({
      next: response => {
        this.emailConfirmationResending.set(false);
        this.emailConfirmationMessage.set(response.message);

        if (response.alreadyConfirmed) {
          this.authStateService.updateCurrentUser(user => ({
            ...user,
            emailConfirmed: true
          }));
        }
      },
      error: error => {
        this.emailConfirmationResending.set(false);
        this.emailConfirmationError.set(extractApiErrorMessage(
          error,
          'We could not resend the verification email right now.'
        ));
      }
    });
  }

  protected openVerificationPage(): void {
    void this.router.navigate(['/auth', 'verify-email'], {
      queryParams: {
        email: this.currentUser()?.email || undefined
      }
    });
  }
}
