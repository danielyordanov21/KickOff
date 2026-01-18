import { Component } from '@angular/core';

import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatDivider } from '@angular/material/divider';
import { Router } from '@angular/router';

@Component({
  selector: 'pfp',
  templateUrl: './pfp.html',
  styleUrl: './pfp.scss',
  imports: [
    MatIconModule,
    MatMenuModule,
    MatButtonModule,
    MatDivider
  ],
})
export class Pfp {
  private readonly router: Router;

  constructor(router: Router) {
    this.router = router;
  }

  public redirectToSettings(): void {
    this.router.navigate(['/account-settings', 123]);
  }
}
