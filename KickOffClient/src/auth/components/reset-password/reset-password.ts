import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { extractApiErrorMessage } from '../../utils/extract-api-error-message';

@Component({
  selector: 'app-reset-password',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.scss',
})
export class ResetPassword implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  resetPasswordForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*[^A-Za-z0-9]).{6,}$/)]],
    confirmPassword: ['', [Validators.required]],
  }, { validators: this.passwordMatchValidator });

  resetCode: string | null = null;
  isSubmitting = false;
  resetMessage: string | null = null;
  resetError: string | null = null;
  resetComplete = false;
  showPassword = false;
  showConfirmPassword = false;

  ngOnInit(): void {
    const queryParams = this.route.snapshot.queryParamMap;
    const email = queryParams.get('email')?.trim() ?? '';

    this.resetCode = queryParams.get('code');

    if (email) {
      this.resetPasswordForm.controls.email.setValue(email);
    }

    if (!this.resetCode) {
      this.resetError = 'This password reset link is incomplete. Request a new one to continue.';
    }
  }

  get passwordChecks(): Array<{ label: string; met: boolean }> {
    const password = this.resetPasswordForm.controls.password.value;

    return [
      { label: 'At least 6 characters', met: password.length >= 6 },
      { label: 'Uppercase and lowercase letter', met: /[A-Z]/.test(password) && /[a-z]/.test(password) },
      { label: 'One symbol or special character', met: /[^A-Za-z0-9]/.test(password) },
    ];
  }

  goToLogin(): void {
    void this.router.navigate(['/auth', 'login'], {
      queryParams: {
        email: this.resetPasswordForm.controls.email.value.trim() || undefined
      }
    });
  }

  onSubmit(): void {
    if (!this.resetCode) {
      this.resetError = 'This password reset link is incomplete. Request a new one to continue.';
      return;
    }

    const trimmedEmail = this.resetPasswordForm.controls.email.value.trim();
    this.resetPasswordForm.controls.email.setValue(trimmedEmail);

    if (this.resetPasswordForm.invalid || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.resetError = null;

    const { password } = this.resetPasswordForm.getRawValue();

    this.authService.resetPassword(trimmedEmail, this.resetCode, password).subscribe({
      next: response => {
        this.resetComplete = true;
        this.resetMessage = response.message;
        this.isSubmitting = false;
      },
      error: (error: HttpErrorResponse) => {
        this.resetError = extractApiErrorMessage(
          error,
          'We could not reset your password.'
        );
        this.isSubmitting = false;
      }
    });
  }

  passwordMatchValidator(group: any) {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }
}
