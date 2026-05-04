import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { throwError } from 'rxjs';

import { AuthService } from '../../services/auth.service';
import { Login } from './login';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let login: ReturnType<typeof vi.fn>;
  let routeStub: {
    snapshot: {
      queryParamMap: ReturnType<typeof convertToParamMap>;
    };
  };

  beforeEach(async () => {
    login = vi.fn();
    routeStub = {
      snapshot: {
        queryParamMap: convertToParamMap({})
      }
    };

    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: routeStub
        },
        {
          provide: AuthService,
          useValue: {
            login
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows a sign-in-ready message after a fresh registration', async () => {
    routeStub.snapshot.queryParamMap = convertToParamMap({
      email: 'verify@kickoff.app',
      registered: '1'
    });

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.registrationMessage).toBe(
      'Account created. You can sign in now, and verify your email whenever you are ready.'
    );
    expect(component.postRegistrationEmail).toBe('verify@kickoff.app');
    expect(component.loginForm.controls.identifier.value).toBe('verify@kickoff.app');
  });

  it('clears the submitting state and shows the 401 message after invalid credentials', async () => {
    login.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 401,
      error: 'Invalid credentials'
    })));

    component.loginForm.setValue({
      identifier: 'test account',
      password: 'Sup3r!Pass'
    });

    await component.onSubmit();
    fixture.detectChanges();

    expect(component.isSubmitting).toBe(false);
    expect(component.loginErrorTitle).toBe('That sign-in did not go through');
    expect(component.loginError).toBe('Invalid credentials');
    expect(component.loginErrorActionQueryParams).toEqual({ email: 'test account' });
    expect(fixture.nativeElement.querySelector('.alert-message')?.textContent).toContain('Invalid credentials');
  });

  it('turns account-lock bad requests into a recovery-focused alert', async () => {
    login.mockReturnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: {
        code: 'account_locked',
        message: 'Account locked.'
      }
    })));

    component.loginForm.setValue({
      identifier: 'verify@kickoff.app',
      password: 'Sup3r!Pass'
    });

    await component.onSubmit();
    fixture.detectChanges();

    expect(component.loginError).toBe('Account locked.');
    expect(component.loginErrorTitle).toBe('Your account is temporarily locked');
    expect(component.loginErrorHint).toContain('Resetting your password is usually the fastest way back in');
    expect(component.loginErrorActionQueryParams).toEqual({ email: 'verify@kickoff.app' });
    expect(component.verificationEmail).toBeNull();
    expect(fixture.nativeElement.querySelector('.alert-action')?.textContent).toContain('Reset password');
  });

  it('shows a deactivation message when the login screen is reached after deactivation', async () => {
    routeStub.snapshot.queryParamMap = convertToParamMap({
      email: 'verify@kickoff.app',
      deactivated: '1'
    });

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.registrationMessage).toBe(
      'Your account has been deactivated. Contact support if you want to restore access.'
    );
    expect(component.loginForm.controls.identifier.value).toBe('verify@kickoff.app');
  });
});
