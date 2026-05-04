import { Component, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { AuthService } from '../../../../auth/services/auth.service';

@Component({
  selector: 'app-landing-page',
  imports: [CommonModule, RouterModule],
  templateUrl: './landing-page.html',
  styleUrls: ['./landing-page.scss'],
})
export class LandingPage {
  protected authState = inject(AuthStateService);
  protected authService = inject(AuthService);

  public logout() {
    this.authService.logout();
  }
}
