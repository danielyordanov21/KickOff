import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../../auth/services/auth.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { User } from '../../../../auth/user.model';
import { ProjectCatalogueDto } from '../../../models/project-models/project-catalogue.model';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    RouterModule,
  ],
  templateUrl: './projects.html',
  styleUrl: './projects.scss',
})
export class Projects implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly authState = inject(AuthStateService);
  private readonly currencyFormatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  });
  private readonly dateFormatter = new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
  private readonly statePriority = new Map<string, number>([
    ['active', 0],
    ['onhold', 1],
    ['proposed', 2],
    ['completed', 3],
    ['inactive', 4],
    ['cancelled', 5],
  ]);
  private refreshVersion = 0;

  protected readonly user = signal<User | null>(this.authState.currentUser());
  protected readonly isLoading = signal(this.authState.currentUser() === null);
  protected readonly error = signal<string | null>(null);
  protected readonly projects = computed(() => {
    const projects = this.user()?.projects ?? [];

    return [...projects].sort((left, right) => {
      const leftPriority = this.statePriority.get(this.normalizeState(left.state)) ?? Number.MAX_SAFE_INTEGER;
      const rightPriority = this.statePriority.get(this.normalizeState(right.state)) ?? Number.MAX_SAFE_INTEGER;

      if (leftPriority !== rightPriority) {
        return leftPriority - rightPriority;
      }

      return left.name.localeCompare(right.name);
    });
  });
  protected readonly projectCount = computed(() => this.projects().length);
  protected readonly activeProjectCount = computed(() => this.countProjectsByState('active', 'onhold'));
  protected readonly proposedProjectCount = computed(() => this.countProjectsByState('proposed'));
  protected readonly completedProjectCount = computed(() => this.countProjectsByState('completed'));
  protected readonly projectOwnerLabel = computed(() => this.user()?.userName?.trim() || 'your account');

  ngOnInit(): void {
    this.refreshProjects();
  }

  protected trackProject(_index: number, project: ProjectCatalogueDto): string {
    return project.id;
  }

  protected formatState(state?: string | null): string {
    if (!state?.trim()) {
      return 'Unknown';
    }

    return state
      .replace(/[_-]+/g, ' ')
      .split(' ')
      .filter(Boolean)
      .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
      .join(' ');
  }

  protected projectStateClass(project: ProjectCatalogueDto): string {
    return this.normalizeState(project.state) || 'unknown';
  }

  protected projectDescription(project: ProjectCatalogueDto): string {
    const description = project.description?.trim();
    return description && description.length > 0
      ? description
      : 'No summary has been added for this project yet.';
  }

  protected fundingSummary(project: ProjectCatalogueDto): string {
    const goal = this.normalizeAmount(project.financialGoal);
    const raised = this.normalizeAmount(project.financialRaised);

    if (goal > 0) {
      return `${this.currencyFormatter.format(raised)} of ${this.currencyFormatter.format(goal)}`;
    }

    if (raised > 0) {
      return `${this.currencyFormatter.format(raised)} pledged`;
    }

    return 'No funding target yet';
  }

  protected deadlineSummary(project: ProjectCatalogueDto): string {
    const endDate = this.parseDate(project.endDate);

    if (!endDate) {
      return 'No deadline set';
    }

    const prefix = endDate.getTime() < Date.now() ? 'Ended' : 'Due';
    return `${prefix} ${this.dateFormatter.format(endDate)}`;
  }

  protected refreshProjects(): void {
    const requestVersion = ++this.refreshVersion;
    const fallbackUser = this.authState.currentUser();

    if (fallbackUser) {
      this.user.set(fallbackUser);
    } else {
      this.isLoading.set(true);
    }

    this.error.set(null);

    this.authService.getCurrentUser().subscribe({
      next: user => {
        if (requestVersion !== this.refreshVersion) {
          return;
        }

        this.authState.setUser(user);
        this.user.set(user);
        this.error.set(null);
        this.isLoading.set(false);
      },
      error: error => {
        if (requestVersion !== this.refreshVersion) {
          return;
        }

        console.error('Error loading projects sheet:', error);
        this.error.set('We could not refresh your projects right now.');
        this.isLoading.set(false);
      },
    });
  }

  private countProjectsByState(...states: string[]): number {
    const normalizedStates = new Set(states.map(state => this.normalizeState(state)));

    return this.projects()
      .filter(project => normalizedStates.has(this.normalizeState(project.state)))
      .length;
  }

  private normalizeState(state?: string | null): string {
    return state?.replace(/[^a-z0-9]+/gi, '').toLowerCase() ?? '';
  }

  private normalizeAmount(amount?: number | null): number {
    if (typeof amount !== 'number' || Number.isNaN(amount)) {
      return 0;
    }

    return Math.max(0, amount);
  }

  private parseDate(value?: string | null): Date | null {
    if (!value) {
      return null;
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }
}
