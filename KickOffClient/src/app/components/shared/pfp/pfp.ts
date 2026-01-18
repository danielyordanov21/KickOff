import { Component } from '@angular/core';

import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatDivider } from '@angular/material/divider';

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

}
