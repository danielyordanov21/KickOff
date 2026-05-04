import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, effect, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { Router, RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { SendbirdService } from '../../../services/sendbird.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { AuthService } from '../../../../auth/services/auth.service';
import { UserService } from '../../../../auth/services/user.service';
import { extractApiErrorMessage } from '../../../../auth/utils/extract-api-error-message';
import { User } from '../../../../auth/user.model';

interface AccountDetailCard {
  eyebrow: string;
  title: string;
  description: string;
  entries: Array<{
    label: string;
    value: string;
    mono?: boolean;
  }>;
  note?: string;
  actionLabel?: string;
  actionLink?: string;
}

@Component({
  selector: 'app-account-settings',
  templateUrl: './account-settings.html',
  styleUrl: './account-settings.scss',
  imports: [
    CommonModule,
    MatIconModule,
    ReactiveFormsModule,
    RouterModule,
  ],
})
export class AccountSettings {
  private readonly passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*[^A-Za-z0-9]).{6,}$/;
  private readonly authState = inject(AuthStateService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly sendbirdService = inject(SendbirdService);
  private readonly userService = inject(UserService);
  private lastSyncedEmail: string | null = null;
  private lastSyncedUserName: string | null = null;
  private readonly availableChatLanguages = [
    { value: '', label: 'Keep messages in their original language' },
    { value: 'de', label: 'German' },
    { value: 'en', label: 'English' },
    { value: 'es', label: 'Spanish' },
    { value: 'fr', label: 'French' },
    { value: 'it', label: 'Italian' },
    { value: 'ja', label: 'Japanese' },
    { value: 'ko', label: 'Korean' },
    { value: 'pt', label: 'Portuguese' },
    { value: 'ru', label: 'Russian' },
    { value: 'zh', label: 'Chinese' },
  ] as const;

  public readonly chatPreferencesForm = this.fb.nonNullable.group({
    preferredChatLanguage: [''],
    showOriginalChatTextByDefault: [false],
  });

  public readonly profileForm = this.fb.nonNullable.group({
    userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(64)]],
  });

  public readonly emailForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    currentPassword: ['', [Validators.required]],
  });

  public readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(6), Validators.pattern(this.passwordPattern)]],
    confirmPassword: ['', [Validators.required]],
  }, { validators: this.passwordMatchValidator });

  public readonly deactivateForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    confirmationText: ['', [Validators.required]],
  });

  public readonly deleteForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    confirmationText: ['', [Validators.required]],
  });

  public isSavingProfile = false;
  public profileSaveError: string | null = null;
  public profileSaveSuccess: string | null = null;
  public isChangingEmail = false;
  public emailChangeError: string | null = null;
  public emailChangeSuccess: string | null = null;
  public isUpgradingProducer = false;
  public producerUpgradeError: string | null = null;
  public producerUpgradeSuccess: string | null = null;
  public isSavingChatPreferences = false;
  public chatPreferencesError: string | null = null;
  public chatPreferencesSuccess: string | null = null;
  public isChangingPassword = false;
  public passwordChangeError: string | null = null;
  public passwordChangeSuccess: string | null = null;
  public showCurrentPassword = false;
  public showNewPassword = false;
  public showConfirmPassword = false;
  public showEmailPassword = false;
  public isProcessingDeactivation = false;
  public deactivationError: string | null = null;
  public showDeactivatePassword = false;
  public isDeletingAccount = false;
  public deleteAccountError: string | null = null;
  public showDeletePassword = false;
  public readonly deactivateConfirmationTarget = 'DEACTIVATE';
  public readonly deleteConfirmationTarget = 'DELETE';

  constructor() {
    effect(() => {
      const user = this.user;
      if (!user) {
        return;
      }

      if (this.profileForm.pristine || this.lastSyncedUserName !== user.userName) {
        this.profileForm.setValue({
          userName: user.userName,
        }, { emitEvent: false });
        this.profileForm.markAsPristine();
        this.profileForm.markAsUntouched();
        this.lastSyncedUserName = user.userName;
      }

      if (this.emailForm.pristine || this.lastSyncedEmail !== user.email) {
        this.emailForm.setValue({
          email: user.email,
          currentPassword: '',
        }, { emitEvent: false });
        this.emailForm.markAsPristine();
        this.emailForm.markAsUntouched();
        this.lastSyncedEmail = user.email;
      }

      this.chatPreferencesForm.setValue({
        preferredChatLanguage: user.preferredChatLanguage ?? '',
        showOriginalChatTextByDefault: user.showOriginalChatTextByDefault ?? false,
      }, { emitEvent: false });
      this.chatPreferencesForm.markAsPristine();
      this.chatPreferencesForm.markAsUntouched();
    });
  }

  public get user(): User | null {
    return this.authState.currentUser();
  }

  public get canCreateProjects(): boolean {
    return this.authState.canCreateProjects();
  }

  public get displayRoles(): string[] {
    const roles = new Set<string>();

    for (const role of this.user?.roles ?? []) {
      if (role?.trim()) {
        roles.add(this.formatLabel(role));
      }
    }

    if (this.user?.role?.trim()) {
      roles.add(this.formatLabel(this.user.role));
    }

    return Array.from(roles);
  }

  public get profileInitials(): string {
    const name = this.user?.userName?.trim();
    if (!name) {
      return 'KO';
    }

    const parts = name.split(/\s+/).filter(Boolean).slice(0, 2);
    return parts.map(part => part[0]?.toUpperCase() ?? '').join('') || name.slice(0, 2).toUpperCase();
  }

  public get projectsCount(): number {
    return this.user?.projectIds?.length ?? 0;
  }

  public get canDeleteAccount(): boolean {
    if (typeof this.user?.canDeleteAccount === 'boolean') {
      return this.user.canDeleteAccount;
    }

    return this.projectsCount === 0;
  }

  public get deleteAccountRestriction(): string | null {
    if (this.user?.deleteAccountRestriction) {
      return this.user.deleteAccountRestriction;
    }

    return this.projectsCount > 0
      ? 'Permanent deletion is disabled while this account still owns projects.'
      : null;
  }

  public get verificationEmailQueryParams(): Record<string, string> | null {
    if (!this.user?.email) {
      return null;
    }

    return {
      email: this.user.email,
      sent: '1',
    };
  }

  public get chatLanguageOptions(): ReadonlyArray<{ value: string; label: string }> {
    return this.availableChatLanguages;
  }

  public get selectedChatLanguageLabel(): string {
    return this.resolveChatLanguageLabel(this.chatPreferencesForm.controls.preferredChatLanguage.value);
  }

  public get followersCount(): number {
    return this.user?.followerIdsP?.length ?? 0;
  }

  public get followingCount(): number {
    return this.user?.followingIdsP?.length ?? 0;
  }

  public get passwordChecks(): Array<{ label: string; met: boolean }> {
    const password = this.passwordForm.controls.newPassword.value;

    return [
      { label: 'At least 6 characters', met: password.length >= 6 },
      { label: 'Uppercase and lowercase letter', met: /[A-Z]/.test(password) && /[a-z]/.test(password) },
      { label: 'One symbol or special character', met: /[^A-Za-z0-9]/.test(password) },
    ];
  }

  public get detailCards(): AccountDetailCard[] {
    if (!this.user) {
      return [];
    }

    return [
      {
        eyebrow: 'Profile',
        title: 'Public account basics',
        description: 'The essentials people see when they land on your profile.',
        entries: [
          { label: 'Username', value: this.user.userName },
          { label: 'Email', value: this.user.email },
          { label: 'Email status', value: this.user.emailConfirmed === false ? 'Pending confirmation' : 'Confirmed' },
          { label: 'Avatar', value: this.user.profilePictureUrl ? 'Custom image added' : 'Using initials fallback' },
        ],
        actionLabel: 'Preview profile',
        actionLink: '/profile/self',
      },
      {
        eyebrow: 'Access',
        title: 'Roles and permissions',
        description: 'A quick look at what your current account can do inside KickOff.',
        entries: [
          { label: 'Primary role', value: this.displayRoles[0] ?? 'Member' },
          { label: 'All roles', value: this.displayRoles.join(', ') || 'Member' },
          { label: 'Project creation', value: this.canCreateProjects ? 'Enabled' : 'Not available' },
        ],
        note: this.canCreateProjects
          ? 'Profile, email, password, and account management now live on this page.'
          : 'Producer access is self-serve now. Use the button above to unlock project creation on this account.',
      },
      {
        eyebrow: 'Activity',
        title: 'Signals from the platform',
        description: 'Counts that help you gauge how active or discoverable this account already is.',
        entries: [
          { label: 'Projects', value: String(this.projectsCount) },
          { label: 'Followers', value: String(this.followersCount) },
          { label: 'Following', value: String(this.followingCount) },
        ],
        actionLabel: this.canCreateProjects ? 'Create a project' : 'Explore projects',
        actionLink: this.canCreateProjects ? '/project/create' : '/',
      },
      {
        eyebrow: 'Identifiers',
        title: 'Reference IDs',
        description: 'Useful for support or debugging. Keep these private when you can.',
        entries: [
          { label: 'User ID', value: this.user.id, mono: true },
          { label: 'Identity provider ID', value: this.user.idP ?? 'Not linked yet', mono: true },
          { label: 'Profile state', value: this.user.state ? this.formatLabel(this.user.state) : 'Active' },
        ],
        note: 'Use these when support needs to track a specific account quickly.',
      },
    ];
  }

  public formatLabel(value: string): string {
    return value
      .replace(/[_-]+/g, ' ')
      .split(' ')
      .filter(Boolean)
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');
  }

  public saveChatPreferences(): void {
    if (!this.user || this.isSavingChatPreferences) {
      return;
    }

    this.isSavingChatPreferences = true;
    this.chatPreferencesError = null;
    this.chatPreferencesSuccess = null;

    const { preferredChatLanguage, showOriginalChatTextByDefault } = this.chatPreferencesForm.getRawValue();

    this.userService.updateChatPreferences({
      preferredChatLanguage: preferredChatLanguage || null,
      showOriginalChatTextByDefault,
    }).subscribe({
      next: updatedUser => {
        this.chatPreferencesSuccess = 'Chat translation preferences saved.';
        this.chatPreferencesForm.setValue({
          preferredChatLanguage: updatedUser.preferredChatLanguage ?? '',
          showOriginalChatTextByDefault: updatedUser.showOriginalChatTextByDefault ?? false,
        }, { emitEvent: false });
        this.chatPreferencesForm.markAsPristine();
        void this.sendbirdService.syncPreferredLanguages(this.getPreferredChatLanguages(updatedUser));
        this.isSavingChatPreferences = false;
      },
      error: (error: HttpErrorResponse) => {
        this.chatPreferencesError = extractApiErrorMessage(
          error,
          'We could not update your chat translation preferences right now.'
        );
        this.isSavingChatPreferences = false;
      }
    });
  }

  public saveProfile(): void {
    if (!this.user || this.isSavingProfile) {
      return;
    }

    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    const userName = this.profileForm.controls.userName.value.trim();
    if (userName === this.user.userName) {
      this.profileForm.markAsPristine();
      this.profileSaveError = null;
      this.profileSaveSuccess = null;
      return;
    }

    this.isSavingProfile = true;
    this.profileSaveError = null;
    this.profileSaveSuccess = null;

    this.userService.updateProfile({ userName }).subscribe({
      next: updatedUser => {
        this.profileSaveSuccess = 'Your profile details have been updated.';
        this.profileForm.setValue({
          userName: updatedUser.userName,
        }, { emitEvent: false });
        this.profileForm.markAsPristine();
        this.profileForm.markAsUntouched();
        this.lastSyncedUserName = updatedUser.userName;
        this.isSavingProfile = false;
      },
      error: (error: HttpErrorResponse) => {
        this.profileSaveError = extractApiErrorMessage(
          error,
          'We could not update your profile details right now.'
        );
        this.isSavingProfile = false;
      }
    });
  }

  public changeEmail(): void {
    if (!this.user || this.isChangingEmail) {
      return;
    }

    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    const email = this.emailForm.controls.email.value.trim().toLowerCase();
    if (email === this.user.email.trim().toLowerCase()) {
      this.emailChangeError = 'Enter a different email address to continue.';
      this.emailChangeSuccess = null;
      return;
    }

    this.isChangingEmail = true;
    this.emailChangeError = null;
    this.emailChangeSuccess = null;

    this.authService.changeEmail(email, this.emailForm.controls.currentPassword.value).subscribe({
      next: response => {
        this.emailChangeSuccess = response.message;
        this.emailForm.setValue({
          email: response.user.email,
          currentPassword: '',
        }, { emitEvent: false });
        this.emailForm.markAsPristine();
        this.emailForm.markAsUntouched();
        this.showEmailPassword = false;
        this.lastSyncedEmail = response.user.email;
        this.isChangingEmail = false;
      },
      error: (error: HttpErrorResponse) => {
        this.emailChangeError = extractApiErrorMessage(
          error,
          'We could not update your email address right now.'
        );
        this.isChangingEmail = false;
      }
    });
  }

  public togglePasswordVisibility(field: 'current' | 'new' | 'confirm' | 'email' | 'deactivate' | 'delete'): void {
    if (field === 'current') {
      this.showCurrentPassword = !this.showCurrentPassword;
      return;
    }

    if (field === 'new') {
      this.showNewPassword = !this.showNewPassword;
      return;
    }

    if (field === 'email') {
      this.showEmailPassword = !this.showEmailPassword;
      return;
    }

    if (field === 'deactivate') {
      this.showDeactivatePassword = !this.showDeactivatePassword;
      return;
    }

    if (field === 'delete') {
      this.showDeletePassword = !this.showDeletePassword;
      return;
    }

    this.showConfirmPassword = !this.showConfirmPassword;
  }

  public changePassword(): void {
    if (!this.user || this.isChangingPassword) {
      return;
    }

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.isChangingPassword = true;
    this.passwordChangeError = null;
    this.passwordChangeSuccess = null;

    const { currentPassword, newPassword } = this.passwordForm.getRawValue();

    this.authService.changePassword(currentPassword, newPassword).subscribe({
      next: response => {
        this.passwordChangeSuccess = response.message;
        this.passwordForm.reset({
          currentPassword: '',
          newPassword: '',
          confirmPassword: ''
        });
        this.passwordForm.markAsPristine();
        this.passwordForm.markAsUntouched();
        this.showCurrentPassword = false;
        this.showNewPassword = false;
        this.showConfirmPassword = false;
        this.isChangingPassword = false;
      },
      error: (error: HttpErrorResponse) => {
        this.passwordChangeError = extractApiErrorMessage(
          error,
          'We could not update your password right now.'
        );
        this.isChangingPassword = false;
      }
    });
  }

  public passwordMatchValidator(group: any) {
    const password = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  public async becomeProducer(): Promise<void> {
    if (!this.user || this.isUpgradingProducer) {
      return;
    }

    if (this.canCreateProjects) {
      await this.router.navigate(['/project', 'create']);
      return;
    }

    this.isUpgradingProducer = true;
    this.producerUpgradeError = null;
    this.producerUpgradeSuccess = null;

    try {
      await firstValueFrom(this.userService.becomeProducer());
      this.producerUpgradeSuccess = 'Producer access unlocked. You can publish your first project now.';
      await this.router.navigate(['/project', 'create']);
    } catch (error: any) {
      this.producerUpgradeError = extractApiErrorMessage(
        error,
        'We could not unlock producer access right now. Please try again in a moment.'
      );
    } finally {
      this.isUpgradingProducer = false;
    }
  }

  private getPreferredChatLanguages(user: User | null): string[] {
    const language = user?.preferredChatLanguage?.trim().toLowerCase();
    return language ? [language] : [];
  }

  private resolveChatLanguageLabel(language: string | null | undefined): string {
    const normalizedLanguage = language?.trim().toLowerCase() ?? '';
    return this.availableChatLanguages.find(option => option.value === normalizedLanguage)?.label
      ?? 'Keep messages in their original language';
  }

  public get deactivateConfirmationMatches(): boolean {
    return this.matchesConfirmationText(
      this.deactivateForm.controls.confirmationText.value,
      this.deactivateConfirmationTarget
    );
  }

  public get deleteConfirmationMatches(): boolean {
    return this.matchesConfirmationText(
      this.deleteForm.controls.confirmationText.value,
      this.deleteConfirmationTarget
    );
  }

  public deactivateAccount(): void {
    if (!this.user || this.isProcessingDeactivation) {
      return;
    }

    if (this.deactivateForm.invalid || !this.deactivateConfirmationMatches) {
      this.deactivateForm.markAllAsTouched();
      return;
    }

    const currentEmail = this.user.email;
    const { currentPassword, confirmationText } = this.deactivateForm.getRawValue();

    this.isProcessingDeactivation = true;
    this.deactivationError = null;

    this.authService.deactivateAccount(currentPassword, confirmationText).subscribe({
      next: () => {
        this.isProcessingDeactivation = false;
        this.showDeactivatePassword = false;
        void this.router.navigate(['/auth', 'login'], {
          queryParams: {
            email: currentEmail,
            deactivated: '1',
          }
        });
      },
      error: (error: HttpErrorResponse) => {
        this.deactivationError = extractApiErrorMessage(
          error,
          'We could not deactivate your account right now.'
        );
        this.isProcessingDeactivation = false;
      }
    });
  }

  public deleteAccount(): void {
    if (!this.user || this.isDeletingAccount) {
      return;
    }

    if (this.deleteForm.invalid || !this.deleteConfirmationMatches || !this.canDeleteAccount) {
      this.deleteForm.markAllAsTouched();
      return;
    }

    const { currentPassword, confirmationText } = this.deleteForm.getRawValue();

    this.isDeletingAccount = true;
    this.deleteAccountError = null;

    this.authService.deleteAccount(currentPassword, confirmationText).subscribe({
      next: () => {
        this.isDeletingAccount = false;
        this.showDeletePassword = false;
        void this.router.navigate(['/auth', 'login'], {
          queryParams: {
            deleted: '1',
          }
        });
      },
      error: (error: HttpErrorResponse) => {
        this.deleteAccountError = extractApiErrorMessage(
          error,
          'We could not permanently delete your account right now.'
        );
        this.isDeletingAccount = false;
      }
    });
  }

  private matchesConfirmationText(value: string | null | undefined, expectedValue: string): boolean {
    return value?.trim().toUpperCase() === expectedValue;
  }
}
