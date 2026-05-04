import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Background } from './components/shared/background/background';
import { EmailConfirmation } from './components/shared/email-confirmation/email-confirmation';
import { Header } from './components/shared/header/header';
import { AuthService } from '../auth/services/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
  imports: [
    RouterOutlet,
    Background,
    EmailConfirmation,
    Header
  ],
})
export class App {
  protected readonly title = signal('KickOffClient');

  private readonly authService = inject(AuthService);

  constructor() {
    this.authService.initialize();
  }
}
