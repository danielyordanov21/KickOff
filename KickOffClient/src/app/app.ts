import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Background } from './components/shared/background/background';
import { Header } from './components/shared/header/header';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
  imports: [
    RouterOutlet, 
    Background,
    Header
  ],
})
export class App {
  protected readonly title = signal('KickOffClient');
}
