import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { extractApiErrorDetails } from '../../utils/extract-api-error-message';

type LoginAlertTone = 'danger' | 'warning';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
})
export class Login implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  loginForm = this.fb.nonNullable.group({
    identifier: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  isSubmitting = false;
  loginError: string | null = null;
  loginErrorActionLabel: string | null = null;
  loginErrorActionQueryParams: Record<string, string | undefined> | null = null;
  loginErrorActionRoute: string[] | null = null;
  loginErrorHint: string | null = null;
  loginErrorTitle: string | null = null;
  loginErrorTone: LoginAlertTone = 'danger';
  postRegistrationEmail: string | null = null;
  registrationMessage: string | null = null;
  verificationEmail: string | null = null;
  showPassword = false;

  ngOnInit(): void {
    const identifier = this.route.snapshot.queryParamMap.get('email')?.trim() ?? '';
    const registered = this.route.snapshot.queryParamMap.get('registered') === '1';
    const deactivated = this.route.snapshot.queryParamMap.get('deactivated') === '1';
    const deleted = this.route.snapshot.queryParamMap.get('deleted') === '1';
    const verified = this.route.snapshot.queryParamMap.get('verified') === '1';

    if (identifier) {
      this.loginForm.controls.identifier.setValue(identifier);
    }

    if (deleted) {
      this.registrationMessage = 'Your account has been permanently deleted.';
    } else if (deactivated) {
      this.registrationMessage = 'Your account has been deactivated. Contact support if you want to restore access.';
    } else if (verified) {
      this.registrationMessage = 'Email confirmed. Sign in to jump back into KickOff.';
    } else if (registered) {
      this.registrationMessage = 'Account created. You can sign in now, and verify your email whenever you are ready.';
      this.postRegistrationEmail = identifier.includes('@')
        ? identifier
        : null;
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  async onSubmit(): Promise<void> {
    if (this.loginForm.invalid || this.isSubmitting) return;

    this.isSubmitting = true;
    const { identifier, password } = this.loginForm.getRawValue();
    const trimmedIdentifier = identifier.trim();

    this.clearLoginFeedback();

    try {
      await firstValueFrom(this.authService.login(trimmedIdentifier, password));
      await this.router.navigate(['/']);
    } catch (error: unknown) {
      this.applyLoginError(error, trimmedIdentifier);
    } finally {
      this.isSubmitting = false;
      this.cdr.markForCheck();
    }
  }

  private clearLoginFeedback(): void {
    this.loginError = null;
    this.loginErrorActionLabel = null;
    this.loginErrorActionQueryParams = null;
    this.loginErrorActionRoute = null;
    this.loginErrorHint = null;
    this.loginErrorTitle = null;
    this.loginErrorTone = 'danger';
    this.verificationEmail = null;
  }

  private applyLoginError(error: unknown, trimmedIdentifier: string): void {
    try {
      const errorDetails = extractApiErrorDetails(error, 'Unexpected error occurred.');
      const httpError = error instanceof HttpErrorResponse ? error : null;

      if (httpError?.status === 401) {
        this.loginErrorTitle = 'That sign-in did not go through';
        this.loginError = errorDetails.message || 'That email, username, or password did not match.';
        this.loginErrorHint = 'Double-check your password, or reset it if you are locked out.';
        this.loginErrorActionLabel = 'Reset password';
        this.loginErrorActionRoute = ['/auth', 'forgot-password'];
        this.loginErrorActionQueryParams = { email: trimmedIdentifier || undefined };
      } else if (httpError?.status === 403 && httpError.error?.emailConfirmationRequired) {
        this.loginErrorTitle = 'Confirm your email to continue';
        this.loginError = errorDetails.message || 'Confirm your email using the inbox link we sent.';
        this.loginErrorHint = 'Once you confirm the inbox link, you can come right back and finish signing in.';
        this.verificationEmail = httpError.error?.email ?? trimmedIdentifier;
        this.loginErrorActionLabel = 'Resend confirmation email';
        this.loginErrorActionRoute = ['/auth', 'verify-email'];
        this.loginErrorActionQueryParams = { email: this.verificationEmail ?? undefined };
      } else if (httpError?.status === 400) {
        this.loginErrorTitle = 'We could not sign you in yet';
        this.loginError = errorDetails.message || 'Check the sign-in details and try again.';
        this.loginErrorHint = 'Review the message below and choose the quickest recovery path.';

        if (errorDetails.code === 'account_locked') {
          this.loginErrorTone = 'warning';
          this.loginErrorTitle = 'Your account is temporarily locked';
          this.loginErrorHint = 'Resetting your password is usually the fastest way back in, or you can wait and try again later.';
          this.loginErrorActionLabel = 'Reset password';
          this.loginErrorActionRoute = ['/auth', 'forgot-password'];
          this.loginErrorActionQueryParams = { email: trimmedIdentifier || undefined };
        } else if (errorDetails.code === 'account_deactivated') {
          this.loginErrorTone = 'warning';
          this.loginErrorTitle = 'This account has been deactivated';
          this.loginErrorHint = 'Contact support if you want to restore access to this account.';
          this.loginErrorActionLabel = null;
          this.loginErrorActionRoute = null;
          this.loginErrorActionQueryParams = null;
        } else if (errorDetails.code === 'sign_in_not_allowed') {
          this.loginErrorTitle = 'This account cannot sign in yet';
          this.loginErrorHint = 'If this account should already be active, contact support so we can help finish setup.';
        }
      } else if (httpError?.status === 0) {
        this.loginErrorTitle = 'Connection problem';
        this.loginError = 'Network error. Check your connection and try again.';
        this.loginErrorHint = 'Nothing was submitted yet, so you can retry as soon as the connection is back.';
      } else {
        this.loginErrorTitle = 'Something unexpected happened';
        this.loginError = errorDetails.message || 'Unexpected error occurred.';
        this.loginErrorHint = 'Try again in a moment. If this keeps happening, contact support.';
      }
    } catch {
      this.loginErrorTitle = 'That sign-in did not go through';
      this.loginError = 'We received a response, but could not display it cleanly. Try again once more.';
      this.loginErrorHint = 'If this keeps happening, refresh the page and try again.';
      this.loginErrorActionLabel = 'Reset password';
      this.loginErrorActionRoute = ['/auth', 'forgot-password'];
      this.loginErrorActionQueryParams = { email: trimmedIdentifier || undefined };
    }
  }
}
