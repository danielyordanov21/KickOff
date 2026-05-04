import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { Router } from '@angular/router';

import { ProjectCatalogueDto } from '../../../../models/project-models/project-catalogue.model';

@Component({
  selector: 'project-card',
  standalone: true,
  imports: [],
  templateUrl: './project-card.html',
  styleUrl: './project-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectCard {
  private readonly router = inject(Router);
  private readonly previewDescriptionLength = 108;
  private readonly currencyFormatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0
  });
  private readonly dateFormatter = new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric'
  });

  public project = input.required<ProjectCatalogueDto>();

  protected get cardImageUrl(): string | null {
    return this.project().imageUrl ?? null;
  }

  protected get stateClass(): string {
    return this.project().state.toLowerCase().replace(/[^a-z0-9]+/g, '-');
  }

  protected get projectInitial(): string {
    return this.project().name.trim().charAt(0).toUpperCase() || '?';
  }

  protected get ownerInitial(): string {
    return this.project().owner.trim().charAt(0).toUpperCase() || '?';
  }

  protected get projectDescription(): string {
    const description = this.project().description?.trim();
    return description && description.length > 0
      ? description
      : 'No description has been added yet.';
  }

  protected get fundingMetricLabel(): string {
    return this.hasFundingTarget ? '% funded' : 'Funding';
  }

  protected get fundingMetricValue(): string {
    return this.hasFundingTarget ? `${this.fundingProgressPercent}%` : 'TBD';
  }

  protected get timelineMetricValue(): string {
    const endDate = this.parsedEndDate;

    if (!endDate) {
      return 'No deadline';
    }

    const remainingMs = endDate.getTime() - Date.now();

    if (remainingMs <= 0) {
      return 'Ended';
    }

    const dayMs = 24 * 60 * 60 * 1000;
    const daysLeft = Math.ceil(remainingMs / dayMs);
    return `${daysLeft} ${daysLeft === 1 ? 'day' : 'days'} left`;
  }

  protected get fundingProgressWidth(): number {
    return Math.min(100, this.fundingProgressPercent);
  }

  protected get campaignSupportingCopy(): string {
    if (this.hasFundingTarget) {
      return `${this.pledgedAmountLabel} pledged of ${this.fundingGoalLabel} goal`;
    }

    const endDate = this.parsedEndDate;

    if (!endDate) {
      return 'Funding target and deadline have not been published yet.';
    }

    const endDateLabel = this.dateFormatter.format(endDate);

    return this.timelineMetricValue === 'Ended'
      ? `Campaign closed on ${endDateLabel}.`
      : `Campaign closes on ${endDateLabel}.`;
  }

  protected get progressAriaLabel(): string {
    return `${this.fundingMetricValue} funded, ${this.campaignSupportingCopy}`;
  }

  protected get projectPreviewDescription(): string {
    const description = this.projectDescription;

    if (description.length <= this.previewDescriptionLength) {
      return description;
    }

    const trimmedPreview = description.slice(0, this.previewDescriptionLength + 1);
    const lastSpaceIndex = trimmedPreview.lastIndexOf(' ');
    const previewEnd = lastSpaceIndex > 64 ? lastSpaceIndex : this.previewDescriptionLength;

    return `${trimmedPreview.slice(0, previewEnd).trimEnd()}...`;
  }

  protected get hasExpandedDescription(): boolean {
    return this.projectPreviewDescription !== this.projectDescription;
  }

  protected get projectRevealCopy(): string {
    const campaignCopy = this.campaignSupportingCopy.endsWith('.')
      ? this.campaignSupportingCopy
      : `${this.campaignSupportingCopy}.`;

    return this.hasExpandedDescription
      ? this.projectDescription
      : `Built by ${this.project().owner}. ${campaignCopy} Currently marked as ${this.project().state.toLowerCase()}.`;
  }

  protected get footerCopy(): string {
    return 'Funding snapshot';
  }

  protected get showFundingProgressBar(): boolean {
    return this.hasFundingTarget;
  }

  protected openProject(): void {
    this.router.navigate(['/project', this.project().id]);
  }

  protected onCardKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter' && event.key !== ' ') {
      return;
    }

    event.preventDefault();
    this.openProject();
  }

  private get hasFundingTarget(): boolean {
    return this.normalizedFundingGoal > 0;
  }

  private get normalizedFundingGoal(): number {
    const goal = this.project().financialGoal;

    if (typeof goal !== 'number' || Number.isNaN(goal)) {
      return 0;
    }

    return Math.max(0, goal);
  }

  private get normalizedFundingRaised(): number {
    const raised = this.project().financialRaised;

    if (typeof raised !== 'number' || Number.isNaN(raised)) {
      return 0;
    }

    return Math.max(0, raised);
  }

  private get fundingProgressPercent(): number {
    if (!this.hasFundingTarget) {
      return 0;
    }

    return Math.max(0, Math.round((this.normalizedFundingRaised / this.normalizedFundingGoal) * 100));
  }

  private get pledgedAmountLabel(): string {
    return this.currencyFormatter.format(this.normalizedFundingRaised);
  }

  private get fundingGoalLabel(): string {
    return this.currencyFormatter.format(this.normalizedFundingGoal);
  }

  private get parsedEndDate(): Date | null {
    const value = this.project().endDate;

    if (!value) {
      return null;
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }
}
