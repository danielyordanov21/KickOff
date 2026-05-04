import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { ForgotPassword } from './forgot-password';

describe('ForgotPassword', () => {
  let component: ForgotPassword;
  let fixture: ComponentFixture<ForgotPassword>;
  let forgotPassword: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    forgotPassword = vi.fn().mockReturnValue(of({
      message: 'If an account exists for that email, we sent password reset instructions.'
    }));

    await TestBed.configureTestingModule({
      imports: [ForgotPassword],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ email: 'prefill@kickoff.app' })
            }
          }
        },
        {
          provide: AuthService,
          useValue: {
            forgotPassword
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ForgotPassword);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('prefills the email query parameter and submits the trimmed address', () => {
    expect(component.forgotPasswordForm.controls.email.value).toBe('prefill@kickoff.app');

    component.forgotPasswordForm.controls.email.setValue('  user@kickoff.app  ');
    component.onSubmit();

    expect(forgotPassword).toHaveBeenCalledWith('user@kickoff.app');
    expect(component.requestMessage).toContain('password reset instructions');
    expect(component.requestError).toBeNull();
  });
});
