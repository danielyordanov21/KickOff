import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { from, of } from 'rxjs';

import { AuthStateService } from '../../services/auth-state.service';
import { AuthService } from '../../services/auth.service';
import { VerifyEmail } from './verify-email';

describe('VerifyEmail', () => {
  let component: VerifyEmail;
  let fixture: ComponentFixture<VerifyEmail>;
  let router: Router;
  let routeStub: {
    snapshot: {
      queryParamMap: ReturnType<typeof convertToParamMap>;
    };
  };
  let confirmEmail: ReturnType<typeof vi.fn>;
  let resendConfirmation: ReturnType<typeof vi.fn>;
  let getVerificationState: ReturnType<typeof vi.fn>;
  let rememberVerificationState: ReturnType<typeof vi.fn>;
  let clearVerificationState: ReturnType<typeof vi.fn>;
  let updateCurrentUser: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    routeStub = {
      snapshot: {
        queryParamMap: convertToParamMap({})
      }
    };
    confirmEmail = vi.fn();
    resendConfirmation = vi.fn();
    getVerificationState = vi.fn().mockReturnValue(null);
    rememberVerificationState = vi.fn();
    clearVerificationState = vi.fn();
    updateCurrentUser = vi.fn();

    await TestBed.configureTestingModule({
      imports: [VerifyEmail],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: routeStub
        },
        {
          provide: AuthService,
          useValue: {
            confirmEmail,
            resendConfirmation,
            getVerificationState,
            rememberVerificationState,
            clearVerificationState
          }
        },
        {
          provide: AuthStateService,
          useValue: {
            updateCurrentUser
          }
        }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
  });

  async function createComponent(
    queryParams: Record<string, string> = {},
    navigationState: Record<string, unknown> | null = null
  ): Promise<void> {
    routeStub.snapshot.queryParamMap = convertToParamMap(queryParams);
    vi.spyOn(router, 'getCurrentNavigation').mockReturnValue(
      navigationState
        ? { extras: { state: navigationState } } as never
        : null
    );

    fixture = TestBed.createComponent(VerifyEmail);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads a stored local preview so the verification screen reflects the latest response', async () => {
    getVerificationState.mockReturnValue({
      email: 'jane@example.com',
      emailDeliveryEnabled: false,
      message: 'Your account is ready. Email delivery is not configured locally, so use the preview link below to verify it.',
      verificationUrl: 'https://kickoff.local/auth/confirm-email?userId=123&code=abc'
    });

    await createComponent({
      email: 'jane@example.com',
      sent: '1'
    });

    expect(component.verificationStatusTitle).toBe('Local verification preview ready');
    expect(component.verificationPreviewUrl).toBe(
      'https://kickoff.local/auth/confirm-email?userId=123&code=abc'
    );
    expect(fixture.nativeElement.querySelector('.preview-link')?.textContent).toContain(
      'Open latest verification link'
    );
    expect(fixture.nativeElement.querySelector('.secondary-btn')?.textContent).toContain(
      'Refresh preview link'
    );
  });

  it('updates the verification state after a resend response arrives', async () => {
    resendConfirmation.mockReturnValue(from(Promise.resolve({
      alreadyConfirmed: false,
      emailDeliveryEnabled: false,
      message: 'Your account is ready. Email delivery is not configured locally, so use the preview link below to verify it.',
      verificationUrl: 'https://kickoff.local/auth/confirm-email?userId=999&code=fresh'
    })));

    await createComponent({
      email: 'jane@example.com'
    });

    component.resendConfirmation();
    await fixture.whenStable();

    expect(component.isResending).toBe(false);
    expect(component.verificationStatusTitle).toBe('Fresh preview link ready');
    expect(component.verificationPreviewUrl).toBe(
      'https://kickoff.local/auth/confirm-email?userId=999&code=fresh'
    );
    expect(component.previewCardTitle).toBe('Local verification preview ready');
    expect(rememberVerificationState).toHaveBeenCalledWith('jane@example.com', {
      alreadyConfirmed: false,
      emailDeliveryEnabled: false,
      message: 'Your account is ready. Email delivery is not configured locally, so use the preview link below to verify it.',
      verificationUrl: 'https://kickoff.local/auth/confirm-email?userId=999&code=fresh'
    });
    expect(component.resendActionLabel).toBe('Refresh preview link');
  });
});
