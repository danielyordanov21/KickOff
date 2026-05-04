import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-profile-metrics',
  standalone: true,
  imports: [CommonModule],
  host: {
    'class': 'profile-metrics-host',
    '[class.expanded]': 'expanded()',
  },
  templateUrl: './profile-metrics.html',
  styleUrl: './profile-metrics.scss',
})
export class ProfileMetrics {
  label = input.required<string>();
  value = input.required<number>();
  description = input.required<string>();
  interactive = input(false);
  expanded = input(false);
  toggleLabel = input<string>('');
  pressed = output<void>();

  protected handlePress(): void {
    if (this.interactive()) {
      this.pressed.emit();
    }
  }
}
