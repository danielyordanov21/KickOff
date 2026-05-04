import { CommonModule, CurrencyPipe, DatePipe, TitleCasePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { catchError, map, of, switchMap } from 'rxjs';
import { ProjectCatalogueDto } from '../../../models/project-models/project-catalogue.model';
import { ProjectFollowState } from '../../../models/project-models/project-follow.model';
import { Project } from '../../../models/project-models/project.model';
import { ProjectUpdate } from '../../../models/project-models/project-update.model';
import { SaveProjectUpdateRequest } from '../../../models/project-models/save-project-update-request.model';
import { UpdateProjectFollowPreferencesRequest } from '../../../models/project-models/update-project-follow-preferences-request.model';
import { ProjectService } from '../../../services/project.service';
import { AuthService } from '../../../../auth/services/auth.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { UserService } from '../../../../auth/services/user.service';
import { Carousel } from '../../shared/carousel/carousel';


@Component({
  selector: 'project-view',
  standalone: true,
  templateUrl: './project-view.html',
  styleUrls: ['./project-view.scss'],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    CurrencyPipe,
    DatePipe,
    TitleCasePipe,
    Carousel
  ],
})
export class ProjectView {
  private readonly destroyRef = inject(DestroyRef);
  private readonly authService = inject(AuthService);
  private readonly authState = inject(AuthStateService);
  private readonly userService = inject(UserService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly projectService = inject(ProjectService);

  public project = signal<Project | null>(null);
  public isLoading = signal(true);
  public loadError = signal<string | null>(null);
  public recommendedProjects = signal<ProjectCatalogueDto[]>([]);
  public recommendationsLoading = signal(false);
  public updateMutationError = signal<string | null>(null);
  public followMutationError = signal<string | null>(null);
  public isCreatingUpdate = signal(false);
  public isFollowPending = signal(false);
  public isSavingFollowPreferences = signal(false);
  public editingUpdateId = signal<string | null>(null);
  public isSavingEditedUpdate = signal(false);
  public deletingUpdateId = signal<string | null>(null);
  public readonly recommendationResponsiveOptions = [
    {
      breakpoint: '1400px',
      numVisible: 3,
      numScroll: 1
    },
    {
      breakpoint: '980px',
      numVisible: 2,
      numScroll: 1
    },
    {
      breakpoint: '575px',
      numVisible: 1,
      numScroll: 1
    }
  ];

  private recommendationsRequestVersion = 0;
  protected newUpdateTitle = '';
  protected newUpdateContent = '';
  protected editUpdateTitle = '';
  protected editUpdateContent = '';

  constructor() {
    void this.authService.initialize();

    this.route.paramMap.pipe(
      takeUntilDestroyed(this.destroyRef),
      switchMap(params => {
        const id = params.get('id');
        this.isLoading.set(true);
        this.loadError.set(null);
        this.followMutationError.set(null);
        this.isFollowPending.set(false);
        this.isSavingFollowPreferences.set(false);

        if (!id) {
          this.project.set(null);
          this.loadError.set('We could not find that project.');
          this.isLoading.set(false);
          return of(null);
        }

        return this.projectService.getById(id).pipe(
          catchError(error => {
            console.error('Error fetching project:', error);
            this.project.set(null);
            this.loadError.set('We could not load this project right now.');
            this.isLoading.set(false);
            return of(null);
          })
        );
      })
    ).subscribe({
      next: (project: Project | null) => {
        this.project.set(project);
        this.isLoading.set(false);

        if (project) {
          this.loadRecommendations(project);
          return;
        }

        this.resetRecommendations();
      },
    });
  }

  public get isCurrentUserOwner(): boolean {
    return this.userService.isCurrentUserOwner(this.project()?.ownerId || '');
  }

  public get canEditProject(): boolean {
    const project = this.project();
    return project ? this.userService.canEditProject(project.ownerId) : false;
  }

  public get isAuthPending(): boolean {
    return !this.authState.isInitialized() || this.authState.isLoading();
  }

  public get canFollowProject(): boolean {
    return !this.isAuthPending && this.authState.isAuthenticated() && !!this.project() && !this.isCurrentUserOwner;
  }

  public get shouldPromptFollowSignIn(): boolean {
    return !this.isAuthPending && !this.authState.isAuthenticated() && !!this.project();
  }

  public get isFollowingProject(): boolean {
    return this.project()?.follow?.isFollowing ?? false;
  }

  public get followersCount(): number {
    return this.project()?.follow?.followersCount ?? 0;
  }

  public get followState(): ProjectFollowState {
    return this.project()?.follow ?? {
      followersCount: 0,
      isFollowing: false,
      receiveInAppNotifications: true,
      receiveEmailNotifications: true,
    };
  }

  public get heroImageUrl(): string | null {
    return this.galleryImages[0] ?? null;
  }

  public get galleryImages(): string[] {
    return this.normalizeValues(this.project()?.imageUrls);
  }

  public get secondaryGalleryImages(): string[] {
    return this.galleryImages.slice(1);
  }

  public get tags(): string[] {
    return this.normalizeValues(this.project()?.tags);
  }

  public get contacts(): string[] {
    return this.normalizeValues(this.project()?.contacts);
  }

  public get collaborators(): string[] {
    return this.normalizeValues(this.project()?.collaboratorsIdP);
  }

  public get backerIds(): string[] {
    return this.normalizeValues(this.project()?.backerIds);
  }

  public get updates(): ProjectUpdate[] {
    return this.sortProjectUpdates(this.project()?.updates ?? []);
  }

  public get publishedAssetCount(): number {
    return this.galleryImages.length;
  }

  public get primaryContact(): string | null {
    return this.contacts[0] ?? null;
  }

  public get ownerChatLink(): string[] | null {
    const ownerPublicId = this.project()?.ownerPublicId?.trim();
    return ownerPublicId && !this.isCurrentUserOwner ? ['/chat', ownerPublicId] : null;
  }

  public get ownerChatQueryParams(): Record<string, string> | null {
    const activeProject = this.project();

    if (!this.ownerChatLink || !activeProject?.name?.trim()) {
      return null;
    }

    return {
      starter: 'project-owner',
      projectName: activeProject.name.trim()
    };
  }

  public get showFunding(): boolean {
    const project = this.project();
    return !!project?.financialGoal && project.financialGoal > 0;
  }

  public get displayStartDate(): Date | null {
    return this.project()?.startDate ?? null;
  }

  public get displayEndDate(): Date | null {
    return this.project()?.endDate ?? null;
  }

  public get showSettingsId(): boolean {
    return this.canEditProject && !!this.project()?.settingsId?.trim();
  }

  public get showInternalReferences(): boolean {
    return this.canEditProject && (this.showSettingsId || this.collaborators.length > 0 || this.backerIds.length > 0);
  }

  public get hasUpdates(): boolean {
    return this.updates.length > 0;
  }

  public get projectTeamSummary(): string {
    const collaboratorCount = this.collaborators.length;
    const backerCount = this.backerIds.length;

    if (collaboratorCount > 0 && backerCount > 0) {
      return `Built with ${this.pluralize(collaboratorCount, 'collaborator')} and backed by ${this.pluralize(backerCount, 'supporter')}.`;
    }

    if (collaboratorCount > 0) {
      return `The owner is already building this with ${this.pluralize(collaboratorCount, 'collaborator')}.`;
    }

    if (backerCount > 0) {
      return `${this.pluralize(backerCount, 'supporter')} are already attached to this idea.`;
    }

    return 'This project is still early, with room to shape the first circle around it.';
  }

  public get projectMomentumCopy(): string {
    const collaboratorCount = this.collaborators.length;
    const backerCount = this.backerIds.length;

    if (collaboratorCount > 0 && backerCount > 0) {
      return `Execution and support are both visible here: ${this.pluralize(collaboratorCount, 'collaborator')} are helping build it, and ${this.pluralize(backerCount, 'backer')} are already showing belief in it.`;
    }

    if (collaboratorCount > 0) {
      return `The execution side is taking shape with ${this.pluralize(collaboratorCount, 'collaborator')} already involved.`;
    }

    if (backerCount > 0) {
      return `The idea already has ${this.pluralize(backerCount, 'backer')}, which is a good early signal of belief and interest.`;
    }

    return 'This is a clean early-stage project page, with the core story in place and room to grow the team, support, and public footprint.';
  }

  public get contactSummary(): string {
    if (this.contacts.length === 0) {
      return 'No public contact options have been published for this project yet.';
    }

    if (this.contacts.length === 1) {
      return 'One public contact option is available for follow-up.';
    }

    return `${this.contacts.length} public contact options are available for follow-up.`;
  }

  public goToEditProject(): void {
    const project = this.project();
    if (!project) {
      return;
    }

    this.router.navigate(['/project', project.id, 'edit'], {
      state: {
        projectSnapshot: project
      }
    });
  }

  public trackByValue(_: number, value: string): string {
    return value;
  }

  public trackProjectUpdate(_: number, update: ProjectUpdate): string {
    return update.id;
  }

  public isEditingUpdate(updateId: string): boolean {
    return this.editingUpdateId() === updateId;
  }

  public formatContactLink(contact: string): string {
    const trimmed = contact.trim();
    const compactPhone = trimmed.replace(/[^\d+]/g, '');

    if (trimmed.includes('@')) {
      return `mailto:${trimmed}`;
    }

    if (/^\+?[\d\s().-]{7,}$/.test(trimmed) && compactPhone.length >= 7) {
      return `tel:${compactPhone}`;
    }

    if (trimmed.startsWith('http://') || trimmed.startsWith('https://')) {
      return trimmed;
    }

    return `https://${trimmed}`;
  }

  public publishUpdate(): void {
    const project = this.project();

    if (!project || this.isCreatingUpdate() || this.isSavingEditedUpdate() || !!this.deletingUpdateId()) {
      return;
    }

    const payload = this.buildUpdatePayload(this.newUpdateTitle, this.newUpdateContent);
    if (!payload) {
      return;
    }

    this.isCreatingUpdate.set(true);
    this.updateMutationError.set(null);

    this.projectService.createUpdate(project.id, payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: update => {
          this.project.update(currentProject => this.withProjectUpdate(currentProject, update));
          this.newUpdateTitle = '';
          this.newUpdateContent = '';
          this.isCreatingUpdate.set(false);
        },
        error: error => {
          console.error('Error creating project update:', error);
          this.updateMutationError.set(this.getUpdateErrorMessage(error, 'We could not publish this update.'));
          this.isCreatingUpdate.set(false);
        }
      });
  }

  public startEditingUpdate(update: ProjectUpdate): void {
    this.editingUpdateId.set(update.id);
    this.editUpdateTitle = update.title;
    this.editUpdateContent = update.content;
    this.updateMutationError.set(null);
  }

  public cancelEditingUpdate(): void {
    this.editingUpdateId.set(null);
    this.editUpdateTitle = '';
    this.editUpdateContent = '';
    this.updateMutationError.set(null);
  }

  public saveEditedUpdate(updateId: string): void {
    const project = this.project();

    if (!project || this.isSavingEditedUpdate() || this.isCreatingUpdate() || !!this.deletingUpdateId()) {
      return;
    }

    const payload = this.buildUpdatePayload(this.editUpdateTitle, this.editUpdateContent);
    if (!payload) {
      return;
    }

    this.isSavingEditedUpdate.set(true);
    this.updateMutationError.set(null);

    this.projectService.updateUpdate(project.id, updateId, payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: update => {
          this.project.update(currentProject => this.withProjectUpdate(currentProject, update));
          this.cancelEditingUpdate();
          this.isSavingEditedUpdate.set(false);
        },
        error: error => {
          console.error('Error updating project update:', error);
          this.updateMutationError.set(this.getUpdateErrorMessage(error, 'We could not save this edit.'));
          this.isSavingEditedUpdate.set(false);
        }
      });
  }

  public deleteUpdate(updateId: string): void {
    const project = this.project();

    if (!project || this.isCreatingUpdate() || this.isSavingEditedUpdate() || !!this.deletingUpdateId()) {
      return;
    }

    if (!window.confirm('Delete this project update?')) {
      return;
    }

    this.deletingUpdateId.set(updateId);
    this.updateMutationError.set(null);

    this.projectService.deleteUpdate(project.id, updateId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.project.update(currentProject => this.withoutProjectUpdate(currentProject, updateId));

          if (this.editingUpdateId() === updateId) {
            this.cancelEditingUpdate();
          }

          this.deletingUpdateId.set(null);
        },
        error: error => {
          console.error('Error deleting project update:', error);
          this.updateMutationError.set(this.getUpdateErrorMessage(error, 'We could not delete this update.'));
          this.deletingUpdateId.set(null);
        }
      });
  }

  public toggleProjectFollow(): void {
    const project = this.project();

    if (!project || this.isFollowPending() || this.isSavingFollowPreferences()) {
      return;
    }

    const shouldFollow = !this.isFollowingProject;
    const request$ = shouldFollow
      ? this.projectService.followProject(project.id)
      : this.projectService.unfollowProject(project.id);

    this.isFollowPending.set(true);
    this.followMutationError.set(null);

    request$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: follow => {
          this.project.update(currentProject => this.withProjectFollow(currentProject, follow));
          this.isFollowPending.set(false);
        },
        error: error => {
          console.error('Error updating project follow state:', error);
          this.followMutationError.set(shouldFollow
            ? 'We could not follow this project right now.'
            : 'We could not unfollow this project right now.');
          this.isFollowPending.set(false);
        }
      });
  }

  public updateInAppFollowPreference(receiveInAppNotifications: boolean): void {
    this.saveFollowPreferences({
      receiveInAppNotifications,
      receiveEmailNotifications: this.followState.receiveEmailNotifications
    });
  }

  public updateEmailFollowPreference(receiveEmailNotifications: boolean): void {
    this.saveFollowPreferences({
      receiveInAppNotifications: this.followState.receiveInAppNotifications,
      receiveEmailNotifications
    });
  }

  private normalizeValues(values?: string[] | null): string[] {
    const uniqueValues = new Set(
      (values ?? [])
        .map(value => value.trim())
        .filter(value => value.length > 0)
    );

    return Array.from(uniqueValues);
  }

  private pluralize(count: number, singular: string, plural = `${singular}s`): string {
    return `${count} ${count === 1 ? singular : plural}`;
  }

  private loadRecommendations(project: Project): void {
    const requestVersion = ++this.recommendationsRequestVersion;
    this.recommendationsLoading.set(true);

    this.projectService.search({
      pageNumber: 1,
      pageSize: 6,
      state: project.state,
      sortNewest: true
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      switchMap(result => {
        const primaryRecommendations = this.buildRecommendations(project.id, result.data ?? []);

        if (primaryRecommendations.length >= 4) {
          return of(primaryRecommendations);
        }

        return this.projectService.getPaginated(null, 1, 10).pipe(
          map(fallbackResult =>
            this.buildRecommendations(project.id, [
              ...primaryRecommendations,
              ...(fallbackResult.data ?? [])
            ])
          )
        );
      }),
      catchError(error => {
        console.error('Error loading project recommendations:', error);
        return of([] as ProjectCatalogueDto[]);
      })
    ).subscribe(recommendations => {
      if (requestVersion !== this.recommendationsRequestVersion) {
        return;
      }

      this.recommendedProjects.set(recommendations);
      this.recommendationsLoading.set(false);
    });
  }

  private buildRecommendations(currentProjectId: string, projects: ProjectCatalogueDto[]): ProjectCatalogueDto[] {
    const uniqueProjects = new Map<string, ProjectCatalogueDto>();

    projects.forEach(project => {
      if (!project?.id || project.id === currentProjectId || uniqueProjects.has(project.id)) {
        return;
      }

      uniqueProjects.set(project.id, project);
    });

    return Array.from(uniqueProjects.values()).slice(0, 5);
  }

  private resetRecommendations(): void {
    this.recommendationsRequestVersion += 1;
    this.recommendedProjects.set([]);
    this.recommendationsLoading.set(false);
  }

  private buildUpdatePayload(title: string, content: string): SaveProjectUpdateRequest | null {
    const trimmedTitle = title.trim();
    const trimmedContent = content.trim();

    if (trimmedTitle.length < 3) {
      this.updateMutationError.set('Update titles must be at least 3 characters long.');
      return null;
    }

    if (trimmedTitle.length > 120) {
      this.updateMutationError.set('Update titles must stay under 120 characters.');
      return null;
    }

    if (trimmedContent.length < 10) {
      this.updateMutationError.set('Updates should include at least 10 characters of context.');
      return null;
    }

    if (trimmedContent.length > 4000) {
      this.updateMutationError.set('Updates must stay under 4000 characters.');
      return null;
    }

    return {
      title: trimmedTitle,
      content: trimmedContent
    };
  }

  private withProjectUpdate(project: Project | null, nextUpdate: ProjectUpdate): Project | null {
    if (!project) {
      return project;
    }

    return {
      ...project,
      updates: this.sortProjectUpdates([
        ...(project.updates ?? []).filter(update => update.id !== nextUpdate.id),
        nextUpdate
      ])
    };
  }

  private withoutProjectUpdate(project: Project | null, updateId: string): Project | null {
    if (!project) {
      return project;
    }

    return {
      ...project,
      updates: (project.updates ?? []).filter(update => update.id !== updateId)
    };
  }

  private sortProjectUpdates(updates: ProjectUpdate[]): ProjectUpdate[] {
    return [...updates].sort((left, right) => {
      const leftTime = left.createdAt?.getTime() ?? 0;
      const rightTime = right.createdAt?.getTime() ?? 0;

      if (rightTime !== leftTime) {
        return rightTime - leftTime;
      }

      return right.id.localeCompare(left.id);
    });
  }

  private getUpdateErrorMessage(error: unknown, fallbackMessage: string): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const errorPayload = (error as { error?: unknown }).error;

      if (typeof errorPayload === 'string' && errorPayload.trim().length > 0) {
        return errorPayload;
      }

      if (typeof errorPayload === 'object' && errorPayload !== null && 'message' in errorPayload) {
        const message = (errorPayload as { message?: unknown }).message;

        if (typeof message === 'string' && message.trim().length > 0) {
          return message;
        }
      }
    }

    return fallbackMessage;
  }

  private saveFollowPreferences(payload: UpdateProjectFollowPreferencesRequest): void {
    const project = this.project();

    if (!project || !this.isFollowingProject || this.isSavingFollowPreferences() || this.isFollowPending()) {
      return;
    }

    this.isSavingFollowPreferences.set(true);
    this.followMutationError.set(null);

    this.projectService.updateFollowPreferences(project.id, payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: follow => {
          this.project.update(currentProject => this.withProjectFollow(currentProject, follow));
          this.isSavingFollowPreferences.set(false);
        },
        error: error => {
          console.error('Error updating project follow preferences:', error);
          this.followMutationError.set('We could not update your alert preferences right now.');
          this.isSavingFollowPreferences.set(false);
        }
      });
  }

  private withProjectFollow(project: Project | null, follow: ProjectFollowState): Project | null {
    if (!project) {
      return project;
    }

    return {
      ...project,
      follow
    };
  }
}
