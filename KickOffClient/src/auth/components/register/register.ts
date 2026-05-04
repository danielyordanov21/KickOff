import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { extractApiErrorDetails } from '../../utils/extract-api-error-message';

type RegisterFieldKey = 'email' | 'password' | 'username';

@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  registerForm = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*[^A-Za-z0-9]).{6,}$/)]],
    confirmPassword: ['', [Validators.required]],
  }, { validators: this.passwordMatchValidator });

  isSubmitting = false;
  registerError: string | null = null;
  registerErrorDetails: string[] = [];
  registerErrorTitle: string | null = null;
  serverFieldErrors: Partial<Record<RegisterFieldKey, string>> = {};
  showPassword = false;
  showConfirmPassword = false;

  passwordMatchValidator(group: any) {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  get passwordChecks(): Array<{ label: string; met: boolean }> {
    const password = this.registerForm.controls.password.value;

    return [
      { label: 'At least 6 characters', met: password.length >= 6 },
      { label: 'Uppercase and lowercase letter', met: /[A-Z]/.test(password) && /[a-z]/.test(password) },
      { label: 'One symbol or special character', met: /[^A-Za-z0-9]/.test(password) },
    ];
  }

  clearServerFieldError(field: RegisterFieldKey): void {
    if (this.serverFieldErrors[field]) {
      delete this.serverFieldErrors[field];
    }
  }

  hasServerFieldError(field: RegisterFieldKey): boolean {
    return Boolean(this.serverFieldErrors[field]);
  }

  async onSubmit() {
    if (this.registerForm.invalid || this.isSubmitting) return;

    this.isSubmitting = true;
    this.clearRegisterFeedback();

    const { username, email, password } = this.registerForm.getRawValue();

    try {
      const result = await this.authService.register(email, password, username);
      this.authService.rememberVerificationState(email, result);

      await this.router.navigate(['/auth', 'login'], {
        queryParams: {
          email,
          registered: '1'
        }
      });
    } catch (err: any) {
      this.applyRegisterError(err);
    } finally {
      this.isSubmitting = false;
    }
  }

  private applyRegisterError(error: unknown): void {
    const errorDetails = extractApiErrorDetails(error, 'Registration failed. Please try again.');
    const issueMessages = errorDetails.messages.length > 0
      ? errorDetails.messages
      : [errorDetails.message];

    this.registerErrorTitle = errorDetails.code === 'validation_failed'
      ? 'A few details still need attention'
      : 'We could not create your account yet';
    this.registerError = errorDetails.code === 'validation_failed'
      ? 'Update the highlighted fields below and try again.'
      : errorDetails.message;
    this.registerErrorDetails = issueMessages;
    this.serverFieldErrors = this.mapServerFieldErrors(issueMessages);
  }

  private clearRegisterFeedback(): void {
    this.registerError = null;
    this.registerErrorDetails = [];
    this.registerErrorTitle = null;
    this.serverFieldErrors = {};
  }

  private mapServerFieldErrors(messages: string[]): Partial<Record<RegisterFieldKey, string>> {
    const fieldPatterns: Record<RegisterFieldKey, RegExp> = {
      username: /\buser\s*name\b|\busername\b/i,
      email: /\bemail\b/i,
      password: /\bpasswords?\b/i
    };

    return messages.reduce<Partial<Record<RegisterFieldKey, string>>>((errors, message) => {
      const matchingField = (Object.keys(fieldPatterns) as RegisterFieldKey[])
        .find(field => fieldPatterns[field].test(message));

      if (matchingField && !errors[matchingField]) {
        errors[matchingField] = message;
      }

      return errors;
    }, {});
  }
}
