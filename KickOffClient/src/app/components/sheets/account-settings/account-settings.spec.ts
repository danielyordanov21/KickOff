import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { AuthService } from '../../../../auth/services/auth.service';
import { UserService } from '../../../../auth/services/user.service';
import { SendbirdService } from '../../../services/sendbird.service';
import { AccountSettings } from './account-settings';

describe('AccountSettings', () => {
  let component: AccountSettings;
  let fixture: ComponentFixture<AccountSettings>;
  let authState: AuthStateService;
  let changePassword: ReturnType<typeof vi.fn>;
  let changeEmail: ReturnType<typeof vi.fn>;
  let deactivateAccount: ReturnType<typeof vi.fn>;
  let deleteAccount: ReturnType<typeof vi.fn>;
  let becomeProducer: ReturnType<typeof vi.fn>;
  let updateProfile: ReturnType<typeof vi.fn>;
  let updateChatPreferences: ReturnType<typeof vi.fn>;
  let syncPreferredLanguages: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    changePassword = vi.fn().mockReturnValue(of({
      message: 'Your password has been updated.',
      accessToken: 'fresh-token'
    }));
    changeEmail = vi.fn().mockReturnValue(of({
      message: 'Your email address has been updated. We sent a confirmation link to your email.',
      emailDeliveryEnabled: true,
      verificationUrl: null,
      user: {
        id: 'user-1',
        idP: 'public-1',
        userName: 'Daniel',
        email: 'new@example.com',
        role: 'user',
        roles: ['user'],
        emailConfirmed: false,
        canDeleteAccount: true,
      }
    }));
    deactivateAccount = vi.fn().mockReturnValue(of({
      message: 'Your account has been deactivated and this session has been signed out.'
    }));
    deleteAccount = vi.fn().mockReturnValue(of({
      message: 'Your account has been permanently deleted.'
    }));
    becomeProducer = vi.fn().mockReturnValue(of({
      id: 'user-1',
      userName: 'Daniel',
      email: 'daniel@example.com',
      role: 'producer',
      roles: ['producer'],
      emailConfirmed: true
    }));
    updateChatPreferences = vi.fn().mockReturnValue(of({
      id: 'user-1',
      idP: 'public-1',
      userName: 'Daniel',
      email: 'daniel@example.com',
      role: 'user',
      roles: ['user'],
      emailConfirmed: true,
      preferredChatLanguage: 'es',
      showOriginalChatTextByDefault: true
    }));
    updateProfile = vi.fn().mockReturnValue(of({
      id: 'user-1',
      idP: 'public-1',
      userName: 'Daniel Updated',
      email: 'daniel@example.com',
      role: 'user',
      roles: ['user'],
      emailConfirmed: true,
      canDeleteAccount: true,
    }));
    syncPreferredLanguages = vi.fn().mockResolvedValue(undefined);

    await TestBed.configureTestingModule({
      imports: [AccountSettings],
      providers: [
        provideRouter([]),
        AuthStateService,
        {
          provide: AuthService,
          useValue: {
            changePassword,
            changeEmail,
            deactivateAccount,
            deleteAccount
          }
        },
        {
          provide: UserService,
          useValue: {
            becomeProducer,
            updateProfile,
            updateChatPreferences
          }
        },
        {
          provide: SendbirdService,
          useValue: {
            syncPreferredLanguages
          }
        }
      ]
    }).compileComponents();

    authState = TestBed.inject(AuthStateService);
    authState.setUser({
      id: 'user-1',
      idP: 'public-1',
      userName: 'Daniel',
      email: 'daniel@example.com',
      role: 'user',
      roles: ['user'],
      emailConfirmed: true,
      preferredChatLanguage: null,
      showOriginalChatTextByDefault: false,
      projectIds: ['project-1'],
      followerIdsP: ['follower-1'],
      followingIdsP: ['following-1'],
      state: 'online',
      canDeleteAccount: true,
      deleteAccountRestriction: null
    });

    fixture = TestBed.createComponent(AccountSettings);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('submits the change-password form and clears it after success', () => {
    component.passwordForm.setValue({
      currentPassword: 'OldPass!1',
      newPassword: 'NewPass!2',
      confirmPassword: 'NewPass!2'
    });

    component.changePassword();

    expect(changePassword).toHaveBeenCalledWith('OldPass!1', 'NewPass!2');
    expect(component.passwordChangeSuccess).toBe('Your password has been updated.');
    expect(component.passwordChangeError).toBeNull();
    expect(component.passwordForm.controls.currentPassword.value).toBe('');
    expect(component.passwordForm.controls.newPassword.value).toBe('');
    expect(component.passwordForm.controls.confirmPassword.value).toBe('');
  });

  it('updates profile details and refreshes the form state after success', () => {
    component.profileForm.setValue({
      userName: 'Daniel Updated'
    });

    component.saveProfile();

    expect(updateProfile).toHaveBeenCalledWith({
      userName: 'Daniel Updated'
    });
    expect(component.profileSaveSuccess).toBe('Your profile details have been updated.');
    expect(component.profileSaveError).toBeNull();
    expect(component.profileForm.controls.userName.value).toBe('Daniel Updated');
  });

  it('submits the email-change form and clears the password field after success', () => {
    component.emailForm.setValue({
      email: 'new@example.com',
      currentPassword: 'OldPass!1'
    });

    component.changeEmail();

    expect(changeEmail).toHaveBeenCalledWith('new@example.com', 'OldPass!1');
    expect(component.emailChangeSuccess).toContain('Your email address has been updated.');
    expect(component.emailChangeError).toBeNull();
    expect(component.emailForm.controls.email.value).toBe('new@example.com');
    expect(component.emailForm.controls.currentPassword.value).toBe('');
  });

  it('shows validation details when change-password returns a bad request', () => {
    changePassword.mockReturnValueOnce(throwError(() => new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: {
          NewPassword: ['Passwords must have at least one non alphanumeric character.']
        }
      }
    })));

    component.passwordForm.setValue({
      currentPassword: 'OldPass!1',
      newPassword: 'NewPass!2',
      confirmPassword: 'NewPass!2'
    });

    component.changePassword();

    expect(component.passwordChangeError).toBe('Passwords must have at least one non alphanumeric character.');
    expect(component.passwordChangeSuccess).toBeNull();
    expect(component.isChangingPassword).toBe(false);
  });

  it('saves chat translation preferences and syncs Sendbird languages', () => {
    component.chatPreferencesForm.setValue({
      preferredChatLanguage: 'es',
      showOriginalChatTextByDefault: true
    });

    component.saveChatPreferences();

    expect(updateChatPreferences).toHaveBeenCalledWith({
      preferredChatLanguage: 'es',
      showOriginalChatTextByDefault: true
    });
    expect(component.chatPreferencesSuccess).toBe('Chat translation preferences saved.');
    expect(component.chatPreferencesError).toBeNull();
    expect(syncPreferredLanguages).toHaveBeenCalledWith(['es']);
  });

  it('submits the deactivate flow once the confirmation phrase matches', () => {
    const navigateSpy = vi.spyOn((component as any).router, 'navigate').mockResolvedValue(true);

    component.deactivateForm.setValue({
      currentPassword: 'OldPass!1',
      confirmationText: 'DEACTIVATE'
    });

    component.deactivateAccount();

    expect(deactivateAccount).toHaveBeenCalledWith('OldPass!1', 'DEACTIVATE');
    expect(navigateSpy).toHaveBeenCalledWith(['/auth', 'login'], {
      queryParams: {
        email: 'daniel@example.com',
        deactivated: '1',
      }
    });
  });
});
