import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { extractApiErrorMessage } from '../../utils/extract-api-error-message';

@Component({
  selector: 'app-forgot-password',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.scss',
})
export class ForgotPassword implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  forgotPasswordForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  isSubmitting = false;
  requestMessage: string | null = null;
  requestError: string | null = null;

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email')?.trim() ?? '';
    if (email.includes('@')) {
      this.forgotPasswordForm.controls.email.setValue(email);
    }
  }

  onSubmit(): void {
    const email = this.forgotPasswordForm.controls.email.value.trim();
    this.forgotPasswordForm.controls.email.setValue(email);

    if (this.forgotPasswordForm.invalid || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.requestMessage = null;
    this.requestError = null;

    this.authService.forgotPassword(email).subscribe({
      next: response => {
        this.requestMessage = response.message;
        this.isSubmitting = false;
      },
      error: (error: HttpErrorResponse) => {
        this.requestError = extractApiErrorMessage(
          error,
          'We could not start password recovery right now.'
        );
        this.isSubmitting = false;
      }
    });
  }
}
