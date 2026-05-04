import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { ResetPassword } from './reset-password';

describe('ResetPassword', () => {
  let component: ResetPassword;
  let fixture: ComponentFixture<ResetPassword>;
  let resetPassword: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    resetPassword = vi.fn().mockReturnValue(of({
      message: 'Your password has been reset.'
    }));

    await TestBed.configureTestingModule({
      imports: [ResetPassword],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({
                email: 'reset@kickoff.app',
                code: 'encoded-token'
              })
            }
          }
        },
        {
          provide: AuthService,
          useValue: {
            resetPassword
          }
        },
        {
          provide: Router,
          useValue: {
            navigate: vi.fn().mockResolvedValue(true)
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ResetPassword);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('submits the reset request using the email and code from the route', () => {
    component.resetPasswordForm.patchValue({
      password: 'Sup3r!Pass',
      confirmPassword: 'Sup3r!Pass'
    });

    component.onSubmit();

    expect(resetPassword).toHaveBeenCalledWith(
      'reset@kickoff.app',
      'encoded-token',
      'Sup3r!Pass'
    );
    expect(component.resetComplete).toBe(true);
    expect(component.resetMessage).toContain('password has been reset');
  });

  it('shows validation details returned from a bad reset-password response', () => {
    resetPassword.mockReturnValueOnce(throwError(() => new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: {
          NewPassword: ['Passwords must have at least one non alphanumeric character.']
        }
      }
    })));

    component.resetPasswordForm.patchValue({
      password: 'Sup3r!Pass',
      confirmPassword: 'Sup3r!Pass'
    });

    component.onSubmit();

    expect(component.resetError).toBe('Passwords must have at least one non alphanumeric character.');
    expect(component.resetComplete).toBe(false);
  });
});
