import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
})
export class Login {
  private fb = inject(FormBuilder);
  private router = inject(Router);

  // Strongly typed, non-nullable form
  loginForm = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  isSubmitting = false;
  loginError: string | null = null;

  async onSubmit() {
    if (this.loginForm.invalid || this.isSubmitting) return;

    this.isSubmitting = true;
    this.loginError = null;

    const { username, password } = this.loginForm.getRawValue();

    try {
      // Replace with real auth API call
      await fakeAuthRequest(username, password);

      this.router.navigate(['/dashboard']);
    } catch (err) {
      this.loginError = 'Invalid credentials';
    } finally {
      this.isSubmitting = false;
    }
  }
}

// Simulated async auth
function fakeAuthRequest(username: string, password: string): Promise<void> {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      username === 'admin' && password === 'password'
        ? resolve()
        : reject();
    }, 800);
  });

}
