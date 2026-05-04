import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDivider } from '@angular/material/divider';
import { MatMenuModule } from '@angular/material/menu';
import { Router } from '@angular/router';
import { AuthService } from '../../../../auth/services/auth.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { User } from '../../../../auth/user.model';

@Component({
  selector: 'pfp',
  templateUrl: './pfp.html',
  styleUrl: './pfp.scss',
  imports: [
    CommonModule,
    MatIconModule,
    MatMenuModule,
    MatButtonModule,
    MatDivider
  ],
})
export class Pfp {
  private readonly router = inject(Router);
  private readonly authState = inject(AuthStateService);
  private readonly authService = inject(AuthService);

  public get user(): User | null {
    return this.authState.currentUser();
  }

  public get isLoggedIn(): boolean {
    return this.authState.isAuthenticated();
  }

  public get canCreateProjects(): boolean {
    return this.authState.canCreateProjects();
  }

  public get userInitials(): string {
    const name = this.user?.userName?.trim();
    if (!name) {
      return 'KO';
    }

    const segments = name.split(/\s+/).filter(Boolean).slice(0, 2);
    return segments.map(segment => segment[0]?.toUpperCase() ?? '').join('') || name.slice(0, 2).toUpperCase();
  }

  public redirectToProfile(): void {
    this.router.navigate(['/profile', 'self']);
  }

  public redirectToSponsors(): void {
    this.router.navigate(['/sponsors', 'self']);
  }

  public redirectToSettings(): void {
    this.router.navigate(['/account-settings']);
  }

  public redirectToProjectCreate(): void {
    this.router.navigate(['/project', 'create']);
  }

  public login(): void {
    this.router.navigate(['/auth', 'login']);
  }

  public register(): void {
    this.router.navigate(['/auth', 'register']);
  }

  public async logout(): Promise<void> {
    await this.authService.logout();
    await this.router.navigate(['/landing']);
  }
}
