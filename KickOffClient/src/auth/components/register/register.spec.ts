import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { throwError } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { Register } from './register';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;
  let register: ReturnType<typeof vi.fn>;
  let rememberVerificationState: ReturnType<typeof vi.fn>;
  let router: Router;

  beforeEach(async () => {
    register = vi.fn();
    rememberVerificationState = vi.fn();

    await TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            register,
            rememberVerificationState
          }
        }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('stores the verification response before routing to the login screen', async () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    register.mockResolvedValue({
      requiresEmailConfirmation: false,
      emailDeliveryEnabled: false,
      message: 'Use the preview link below to verify it.',
      verificationUrl: 'https://kickoff.local/auth/confirm-email?userId=123&code=abc'
    });

    component.registerForm.setValue({
      username: 'janedoe',
      email: 'jane@example.com',
      password: 'Sup3r!Pass',
      confirmPassword: 'Sup3r!Pass'
    });

    await component.onSubmit();

    expect(rememberVerificationState).toHaveBeenCalledWith('jane@example.com', {
      requiresEmailConfirmation: false,
      emailDeliveryEnabled: false,
      message: 'Use the preview link below to verify it.',
      verificationUrl: 'https://kickoff.local/auth/confirm-email?userId=123&code=abc'
    });
    expect(navigate).toHaveBeenCalledWith(['/auth', 'login'], {
      queryParams: {
        email: 'jane@example.com',
        registered: '1'
      }
    });
  });

  it('shows a structured error summary and highlights the affected fields when registration fails validation', async () => {
    register.mockRejectedValue(new HttpErrorResponse({
      status: 400,
      error: {
        code: 'validation_failed',
        message: 'We could not create your account.',
        errors: [
          'Email is already taken.',
          'Passwords must have at least one non alphanumeric character.'
        ]
      }
    }));

    component.registerForm.setValue({
      username: 'janedoe',
      email: 'jane@example.com',
      password: 'Sup3r!Pass',
      confirmPassword: 'Sup3r!Pass'
    });

    await component.onSubmit();
    fixture.detectChanges();

    expect(component.registerErrorTitle).toBe('A few details still need attention');
    expect(component.registerError).toBe('Update the highlighted fields below and try again.');
    expect(component.registerErrorDetails).toEqual([
      'Email is already taken.',
      'Passwords must have at least one non alphanumeric character.'
    ]);
    expect(component.serverFieldErrors.email).toBe('Email is already taken.');
    expect(component.serverFieldErrors.password).toBe(
      'Passwords must have at least one non alphanumeric character.'
    );

    const highlightedInputs = fixture.nativeElement.querySelectorAll('.form-input--server-error');
    const summaryItems = Array.from(
      fixture.nativeElement.querySelectorAll('.alert-list li'),
      (item: Element) => item.textContent?.trim()
    );

    expect(highlightedInputs.length).toBe(2);
    expect(summaryItems).toEqual([
      'Email is already taken.',
      'Passwords must have at least one non alphanumeric character.'
    ]);
  });
});
