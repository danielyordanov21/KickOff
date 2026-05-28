import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { ProjectCatalogueDto } from '../../../models/project-models/project-catalogue.model';
import { Carousel } from '../../shared/carousel/carousel';
import { DiscoverCreators } from './discover-creators/discover-creators';
import { HomeStore } from './home.store';
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
  providers: [HomeStore],
})
export class Home {
  private readonly authState = inject(AuthStateService);
  private readonly homeStore = inject(HomeStore);
  private readonly router = inject(Router);

  protected get featuredProjects(): ProjectCatalogueDto[] {
    return this.homeStore.featuredProjects();
  }

  protected get projects(): ProjectCatalogueDto[] {
    return this.homeStore.projects();
  }

  protected get loading(): boolean {
    return this.homeStore.loading();
  }

  protected get featuredLoading(): boolean {
    return this.homeStore.featuredLoading();
  }

  protected get error(): string | null {
    return this.homeStore.error();
  }

  protected get totalCount(): number {
    return this.homeStore.totalCount();
  }

  protected applyFilters(): void {
    this.homeStore.applyFilters();
  }

  protected clearFilters(): void {
    this.homeStore.clearFilters();
  }

  protected goToNextPage(): void {
    this.homeStore.goToNextPage();
  }

  protected goToPreviousPage(): void {
    this.homeStore.goToPreviousPage();
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
    this.homeStore.retry();
  }

  protected trackProject(_index: number, project: ProjectCatalogueDto): string {
    return project.id;
  }

  protected hasActiveFilters(): boolean {
    return this.homeStore.hasActiveFilters();
  }

  protected get activeFilters(): string[] {
    return this.homeStore.activeFilters();
  }

  protected get totalPages(): number {
    return this.homeStore.totalPages();
  }

  protected get hasPreviousPage(): boolean {
    return this.homeStore.hasPreviousPage();
  }

  protected get hasNextPage(): boolean {
    return this.homeStore.hasNextPage();
  }

  protected get visibleRangeStart(): number {
    return this.homeStore.visibleRangeStart();
  }

  protected get visibleRangeEnd(): number {
    return this.homeStore.visibleRangeEnd();
  }

  protected get skeletonCards(): number[] {
    return this.homeStore.skeletonCards();
  }

  protected get isLoggedIn(): boolean {
    return this.authState.isAuthenticated();
  }

  protected get canCreateProjects(): boolean {
    return this.authState.canCreateProjects();
  }

  protected get keyword(): string {
    return this.homeStore.keyword();
  }

  protected get selectedState(): string {
    return this.homeStore.selectedState();
  }

  protected get sortNewest(): boolean {
    return this.homeStore.sortNewest();
  }

  protected get pageNumber(): number {
    return this.homeStore.pageNumber();
  }

  protected get pageSize(): number {
    return this.homeStore.pageSize();
  }
}
