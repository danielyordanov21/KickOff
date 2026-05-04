import { CommonModule } from '@angular/common';
import { Component, inject, isDevMode, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { ProjectService } from '../../../services/project.service';
import { CreateProjectRequest } from '../../../models/project-models/create-project-request.model';
import { Project } from '../../../models/project-models/project.model';
import { ImageUploader, PendingImageSelection } from '../../shared/image-uploader/image-uploader';

interface ProjectSettingsPolicy {
  title: string;
  status: string;
  description: string;
  detail: string;
  tone: 'supported' | 'partial' | 'planned';
}

@Component({
  selector: 'project-create',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    ImageUploader
  ],
  templateUrl: './project-create.html',
  styleUrl: './project-create.scss',
})
export class ProjectCreate implements OnInit {
  private static readonly postLaunchPolicies: readonly ProjectSettingsPolicy[] = [
    {
      title: 'Project description and public content',
      status: 'Supported now',
      description: 'Text, images, tags, contacts, and creator updates should stay editable after launch.',
      detail: 'KickOff already supports these fields directly in this owner view and on the live project page.',
      tone: 'supported'
    },
    {
      title: 'Reward details',
      status: 'Planned policy',
      description: 'Allow clarifications and light adjustments, but do not let owners materially rewrite what supporters paid for.',
      detail: 'This is a strong trust rule, but reward tiers are not modeled in this project yet, so the rule is guidance rather than enforcement today.',
      tone: 'planned'
    },
    {
      title: 'Funding goal',
      status: 'Partially supported',
      description: 'Treat the goal as increase-only before success, never decrease it, and lock it once the project is fully funded.',
      detail: 'The field already exists here. Full enforcement still needs backend checks against live funding progress.',
      tone: 'partial'
    },
    {
      title: 'Campaign duration',
      status: 'Partially supported',
      description: 'Owners should be able to shorten the campaign, but not extend it past the original public end date.',
      detail: 'This is a clean policy for trust. Full enforcement will need the API to preserve the original announced deadline.',
      tone: 'partial'
    },
    {
      title: 'Shipping estimates, FAQs, and risks',
      status: 'Planned policy',
      description: 'These can change, but every change should be transparent and paired with a clear owner update.',
      detail: 'KickOff already supports creator updates for transparent communication, even before these sections get dedicated structured settings.',
      tone: 'planned'
    }
  ];

  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly projectService = inject(ProjectService);

  protected readonly stateOptions = ['Proposed', 'Inactive', 'Active', 'OnHold', 'Completed', 'Cancelled'];
  protected isSubmitting = false;
  protected isLoading = this.route.snapshot.paramMap.get('id') !== null;
  protected submitError: string | null = null;
  protected mode: 'create' | 'edit' = 'create';
  protected projectId: string | null = null;
  protected selectedImages: PendingImageSelection[] = [];

  protected readonly projectForm = this.fb.nonNullable.group({
    headline: [''],
    goal: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(5000)]],
    category: [''],
    financialGoal: [null as number | null],
    problem: [''],
    extraInfo: [''],
    state: ['Inactive', [Validators.required]],
    endsAt: [''],
    settingsId: this.fb.nonNullable.control<string>(crypto.randomUUID(), [Validators.required]),
    tags: this.fb.array<FormControl<string>>([this.createListControl()]),
    contacts: this.fb.array<FormControl<string>>([this.createListControl()]),
    collaboratorsIdP: this.fb.array<FormControl<string>>([])
  });

  ngOnInit(): void {
    void this.initializeForm();
  }

  protected get isEditMode(): boolean {
    return this.mode === 'edit';
  }

  protected get pageEyebrow(): string {
    return this.isEditMode ? 'Project Settings' : 'Launch A Project';
  }

  protected get pageTitle(): string {
    return this.isEditMode
      ? 'Manage the public story and campaign controls'
      : 'Shape the pitch before you share it';
  }

  protected get pageIntro(): string {
    return this.isEditMode
      ? 'This is where the owner manages a live project: public content, campaign rules, timing, and internal references.'
      : 'Give collaborators and backers a clear snapshot of the goal, the problem, and what this project needs next.';
  }

  protected get strategyVerdict(): string {
    return 'Yes, this is a valid strategy for KickOff.';
  }

  protected get strategyVerdictCopy(): string {
    return 'These post-launch change rules protect backer trust without freezing the project. KickOff can already manage content, images, updates, timing, and core campaign controls here. Rewards, FAQs, risks, and shipping estimates still need dedicated structured models before those policies can be fully enforced.';
  }

  protected get postLaunchPolicies(): readonly ProjectSettingsPolicy[] {
    return ProjectCreate.postLaunchPolicies;
  }

  protected get tags(): FormArray<FormControl<string>> {
    return this.projectForm.get('tags') as FormArray<FormControl<string>>;
  }

  protected get contacts(): FormArray<FormControl<string>> {
    return this.projectForm.get('contacts') as FormArray<FormControl<string>>;
  }

  protected get collaboratorsIdP(): FormArray<FormControl<string>> {
    return this.projectForm.get('collaboratorsIdP') as FormArray<FormControl<string>>;
  }

  protected get tagCount(): number {
    return this.cleanArray(this.tags.getRawValue()).length;
  }

  protected get contactCount(): number {
    return this.cleanArray(this.contacts.getRawValue()).length;
  }

  protected get collaboratorCount(): number {
    return this.cleanArray(this.collaboratorsIdP.getRawValue()).length;
  }

  protected get formattedFinancialGoal(): string {
    const goal = this.projectForm.controls.financialGoal.value;

    if (typeof goal !== 'number' || Number.isNaN(goal) || goal <= 0) {
      return 'No goal set';
    }

    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 0
    }).format(goal);
  }

  protected get formattedCampaignEnd(): string {
    const rawValue = this.projectForm.controls.endsAt.value;

    if (!rawValue) {
      return 'No end date';
    }

    const parsed = new Date(rawValue);
    if (Number.isNaN(parsed.getTime())) {
      return 'End date scheduled';
    }

    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    }).format(parsed);
  }

  protected get imageUploaderHelperText(): string {
    return this.isEditMode
      ? 'Refresh up to 6 public project images. Swapping visuals after launch is okay when the story stays honest.'
      : 'Upload up to 6 images. Each image must be 8MB or smaller.';
  }

  protected addTag(): void {
    this.tags.push(this.createListControl());
  }

  protected addContact(): void {
    this.contacts.push(this.createListControl());
  }

  protected addCollaborator(): void {
    this.collaboratorsIdP.push(this.createListControl());
  }

  protected removeTag(index: number): void {
    this.removeArrayItem(this.tags, index);
  }

  protected removeContact(index: number): void {
    this.removeArrayItem(this.contacts, index);
  }

  protected removeCollaborator(index: number): void {
    this.removeArrayItem(this.collaboratorsIdP, index);
  }

  protected onSubmit(): void {
    if (this.projectForm.invalid || this.isSubmitting) {
      this.projectForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.submitError = null;

    const rawValue = this.projectForm.getRawValue();
    const payload: CreateProjectRequest = {
      headline: this.nullIfEmpty(rawValue.headline),
      goal: rawValue.goal.trim(),
      description: rawValue.description.trim(),
      category: this.nullIfEmpty(rawValue.category),
      financialGoal: rawValue.financialGoal,
      problem: this.nullIfEmpty(rawValue.problem),
      extraInfo: this.nullIfEmpty(rawValue.extraInfo),
      state: rawValue.state,
      endsAt: this.nullIfEmpty(rawValue.endsAt),
      settingsId: rawValue.settingsId,
      tags: this.cleanArray(rawValue.tags),
      imageUrls: this.selectedImages
        .map(image => image.blobName)
        .filter((value): value is string => !!value),
      contacts: this.cleanArray(rawValue.contacts),
      collaboratorsIdP: this.cleanArray(rawValue.collaboratorsIdP)
    };

    const imageFiles = this.selectedImages
      .map(image => image.file)
      .filter((file): file is File => !!file);
    const request$ = this.mode === 'edit' && this.projectId
      ? this.projectService.updateProject(this.projectId, payload, imageFiles)
      : this.projectService.createProject(payload, imageFiles);

    this.logSubmitAttempt(payload, imageFiles);

    request$.subscribe({
      next: project => {
        if (isDevMode()) {
          console.info('[ProjectCreate] Submit succeeded', {
            mode: this.mode,
            projectId: project.id
          });
        }

        this.isSubmitting = false;
        this.router.navigate(['/project', project.id]);
      },
      error: (error: HttpErrorResponse) => {
        if (isDevMode()) {
          console.error('[ProjectCreate] Submit failed', {
            mode: this.mode,
            status: error.status,
            statusText: error.statusText,
            url: error.url,
            error: error.error
          });
        }

        this.isSubmitting = false;
        this.submitError = this.getErrorMessage(error);
      }
    });
  }

  private createListControl(): FormControl<string> {
    return this.fb.nonNullable.control('');
  }

  private async initializeForm(): Promise<void> {
    this.projectId = this.route.snapshot.paramMap.get('id');
    this.mode = this.projectId ? 'edit' : 'create';

    if (!this.projectId) {
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    this.submitError = null;
    const navigationProject = this.getNavigationProject(this.projectId);

    if (navigationProject) {
      this.hydrateForm(navigationProject);
      this.isLoading = false;
    }

    try {
      const project = await firstValueFrom(this.projectService.getById(this.projectId));
      this.hydrateForm(project);
    } catch {
      if (!navigationProject) {
        this.submitError = 'Could not load this project for editing.';
      }
    } finally {
      this.isLoading = false;
    }
  }

  private removeArrayItem(array: FormArray<FormControl<string>>, index: number): void {
    if (array.length > 1) {
      array.removeAt(index);
      return;
    }

    array.at(0).setValue('');
  }

  private replaceArrayValues(array: FormArray<FormControl<string>>, values: string[]): void {
    while (array.length > 0) {
      array.removeAt(0);
    }

    const nextValues = values.length > 0 ? values : [''];
    nextValues.forEach(value => array.push(this.fb.nonNullable.control(value)));
  }

  private hydrateForm(project: Project): void {
    this.projectForm.patchValue({
      headline: project.headline ?? '',
      goal: project.goal,
      description: project.description,
      category: project.category ?? '',
      financialGoal: project.financialGoal ?? null,
      problem: project.problem ?? '',
      extraInfo: project.extraInfo ?? '',
      state: project.state,
      endsAt: this.toDateTimeLocal(project.endDate ?? null),
      settingsId: project.settingsId ?? crypto.randomUUID()
    });

    this.replaceArrayValues(this.tags, project.tags ?? []);
    this.replaceArrayValues(this.contacts, project.contacts ?? []);
    this.replaceArrayValues(this.collaboratorsIdP, project.collaboratorsIdP ?? []);
    this.selectedImages = (project.imageBlobNames ?? []).map((blobName, index) => ({
      blobName,
      previewUrl: project.imageUrls?.[index] ?? ''
    })).filter(image => image.previewUrl.length > 0);
  }

  private getNavigationProject(projectId: string): Project | null {
    const currentNavigation = this.router.getCurrentNavigation();
    const navigationState = currentNavigation?.extras.state ?? window.history.state;
    const candidate = navigationState?.['projectSnapshot'] as Project | undefined;

    if (!candidate || candidate.id !== projectId) {
      return null;
    }

    return candidate;
  }

  protected onImagesChanged(images: PendingImageSelection[]): void {
    this.selectedImages = images;
  }

  private logSubmitAttempt(payload: CreateProjectRequest, imageFiles: File[]): void {
    if (!isDevMode()) {
      return;
    }

    console.info('[ProjectCreate] Submit attempt', {
      mode: this.mode,
      projectId: this.projectId,
      headline: payload.headline,
      state: payload.state,
      tagCount: payload.tags.length,
      contactCount: payload.contacts.length,
      collaboratorCount: payload.collaboratorsIdP.length,
      existingImageCount: payload.imageUrls.length,
      newImageCount: imageFiles.length,
      hasEndsAt: !!payload.endsAt,
      settingsId: payload.settingsId
    });
  }

  private cleanArray(values: string[]): string[] {
    return values
      .map(value => value.trim())
      .filter(value => value.length > 0);
  }

  private nullIfEmpty(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }

  private toDateTimeLocal(value: Date | null): string {
    if (!value) {
      return '';
    }

    const local = new Date(value.getTime() - value.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 16);
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (typeof error.error === 'string' && error.error.length > 0) {
      return error.error;
    }

    if (error.status === 403) {
      return this.isEditMode
        ? 'Only the project owner or an admin can manage these settings.'
        : 'Only producers and admins can create projects.';
    }

    if (error.status === 400) {
      return this.isEditMode
        ? 'Please review these project settings and try again.'
        : 'Please review the form fields and try again.';
    }

    return this.isEditMode
      ? 'Project settings could not be saved. Please try again.'
      : 'Project creation failed. Please try again.';
  }
}
