import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, effect, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { ProjectCatalogueDto } from '../../../models/project-models/project-catalogue.model';
import { ProjectFeedFilters, ProjectFeedFiltersService } from '../../../services/project-feed-filters.service';
import { ProjectService } from '../../../services/project.service';
import { Carousel } from '../../shared/carousel/carousel';
import { DiscoverCreators } from './discover-creators/discover-creators';
import { ProjectCard } from './project-card/project-card';

@Component({
  selector: 'app-home',
  standalone: true,
  templateUrl: './home.html',
  styleUrls: ['./home.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    Carousel,
    ProjectCard,
    DiscoverCreators
  ],
})
export class Home implements OnInit, OnDestroy {
  protected readonly skeletonCardSlots = [0, 1, 2, 3, 4, 5];

  private readonly authState = inject(AuthStateService);
  private readonly projectFeedFiltersService = inject(ProjectFeedFiltersService);

  protected featuredProjects: ProjectCatalogueDto[] = [];
  protected projects: ProjectCatalogueDto[] = [];
  protected loading = true;
  protected featuredLoading = true;
  protected error: string | null = null;
  protected totalCount = 0;

  private readonly destroy$ = new Subject<void>();
  private featuredRequestVersion = 0;
  private projectRequestVersion = 0;

  constructor(
    private readonly projectService: ProjectService,
    private readonly router: Router,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
    effect(() => {
      const activeFilters = this.projectFeedFiltersService.filters();
      this.loadProjects(activeFilters);
    });
  }

  ngOnInit(): void {
    this.loadFeaturedProjects();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  protected applyFilters(): void {
    this.loadProjects(this.filters);
  }

  protected clearFilters(): void {
    this.projectFeedFiltersService.resetFilters();
  }

  protected goToNextPage(): void {
    if (!this.hasNextPage) {
      return;
    }

    this.projectFeedFiltersService.updateFilters({
      pageNumber: this.pageNumber + 1,
    });
  }

  protected goToPreviousPage(): void {
    if (!this.hasPreviousPage) {
      return;
    }

    this.projectFeedFiltersService.updateFilters({
      pageNumber: this.pageNumber - 1,
    });
  }

  protected goToCreateProject(): void {
    this.router.navigate(['/project/create']);
  }

  protected goToBecomeProducer(): void {
    this.router.navigate(['/account-settings']);
  }

  protected goToRegister(): void {
    this.router.navigate(['/auth/register']);
  }

  protected retry(): void {
    this.loadFeaturedProjects();
    this.loadProjects(this.filters);
  }

  protected trackProject(_index: number, project: ProjectCatalogueDto): string {
    return project.id;
  }

  protected hasActiveFilters(): boolean {
    return this.keyword.length > 0 || this.selectedState !== 'All' || !this.sortNewest;
  }

  protected get activeFilters(): string[] {
    const filters: string[] = [];
    if (this.keyword.length > 0) {
      filters.push(`Keyword: ${this.keyword}`);
    }

    if (this.selectedState !== 'All') {
      filters.push(`State: ${this.selectedState}`);
    }

    if (!this.sortNewest) {
      filters.push('Sort: Oldest first');
    }

    return filters;
  }

  protected get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  protected get hasPreviousPage(): boolean {
    return this.pageNumber > 1;
  }

  protected get hasNextPage(): boolean {
    return this.pageNumber < this.totalPages;
  }

  protected get visibleRangeStart(): number {
    return this.totalCount === 0 ? 0 : ((this.pageNumber - 1) * this.pageSize) + 1;
  }

  protected get visibleRangeEnd(): number {
    return Math.min(this.pageNumber * this.pageSize, this.totalCount);
  }

  protected get skeletonCards(): number[] {
    const skeletonCount = Math.max(2, Math.min(this.pageSize, this.skeletonCardSlots.length));
    return this.skeletonCardSlots.slice(0, skeletonCount);
  }

  protected get isLoggedIn(): boolean {
    return this.authState.isAuthenticated();
  }

  protected get canCreateProjects(): boolean {
    return this.authState.canCreateProjects();
  }

  protected get keyword(): string {
    return this.filters.keyword;
  }

  protected get selectedState(): string {
    return this.filters.state;
  }

  protected get sortNewest(): boolean {
    return this.filters.sortNewest;
  }

  protected get pageNumber(): number {
    return this.filters.pageNumber;
  }

  protected get pageSize(): number {
    return this.filters.pageSize;
  }

  private loadFeaturedProjects(): void {
    const requestVersion = ++this.featuredRequestVersion;
    this.featuredLoading = true;

    this.projectService.getPaginated(null, 1, 6)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          if (requestVersion !== this.featuredRequestVersion) {
            return;
          }

          this.featuredProjects = result.data ?? [];
          this.featuredLoading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: () => {
          if (requestVersion !== this.featuredRequestVersion) {
            return;
          }

          this.featuredProjects = [];
          this.featuredLoading = false;
          this.changeDetectorRef.markForCheck();
        }
      });
  }

  private get filters(): ProjectFeedFilters {
    return this.projectFeedFiltersService.filters();
  }

  private loadProjects(filters: ProjectFeedFilters): void {
    const requestVersion = ++this.projectRequestVersion;
    this.loading = true;
    this.error = null;

    this.projectService.search({
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
      state: filters.state === 'All' ? null : filters.state,
      keyword: filters.keyword || null,
      sortNewest: filters.sortNewest
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          if (requestVersion !== this.projectRequestVersion) {
            return;
          }

          const projects = result.data ?? [];

          this.projects = projects;
          this.totalCount = result.totalCount ?? projects.length;
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: () => {
          if (requestVersion !== this.projectRequestVersion) {
            return;
          }

          this.projects = [];
          this.totalCount = 0;
          this.loading = false;
          this.error = 'Could not load projects right now.';
          this.changeDetectorRef.markForCheck();
        }
      });
  }
}
