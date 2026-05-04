import { Injectable, signal } from '@angular/core';

export const PROJECT_FEED_STATE_OPTIONS = [
  'All',
  'Active',
  'Proposed',
  'OnHold',
  'Completed',
  'Inactive',
  'Cancelled',
] as const;

export const PROJECT_FEED_PAGE_SIZE_OPTIONS = [6, 10, 14] as const;

export interface ProjectFeedFilters {
  keyword: string;
  state: string;
  sortNewest: boolean;
  pageNumber: number;
  pageSize: number;
}

const DEFAULT_PROJECT_FEED_FILTERS: ProjectFeedFilters = {
  keyword: '',
  state: 'All',
  sortNewest: true,
  pageNumber: 1,
  pageSize: 6,
};

@Injectable({
  providedIn: 'root',
})
export class ProjectFeedFiltersService {
  private readonly filtersState = signal<ProjectFeedFilters>({
    ...DEFAULT_PROJECT_FEED_FILTERS,
  });

  public readonly filters = this.filtersState.asReadonly();

  public updateFilters(patch: Partial<ProjectFeedFilters>): void {
    this.filtersState.update((currentFilters) => ({
      ...currentFilters,
      ...patch,
    }));
  }

  public resetFilters(): void {
    this.filtersState.set({
      ...DEFAULT_PROJECT_FEED_FILTERS,
    });
  }
}
