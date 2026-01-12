import { Component } from '@angular/core';
import { Pfp } from '../pfp/pfp';

@Component({
  selector: 'app-header',
  templateUrl: './header.html',
  styleUrl: './header.scss',
  imports: [
    Pfp
  ],
})
export class Header {

}
