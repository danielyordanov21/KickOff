import { DestroyRef, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withProps,
  withState,
} from '@ngrx/signals';

import { ProjectCatalogueDto } from '../../../models/project-models/project-catalogue.model';
import { ProjectFeedFilters, ProjectFeedFiltersService } from '../../../services/project-feed-filters.service';
import { ProjectService } from '../../../services/project.service';

interface HomeState {
  featuredProjects: ProjectCatalogueDto[];
  projects: ProjectCatalogueDto[];
  loading: boolean;
  featuredLoading: boolean;
  error: string | null;
  totalCount: number;
}

const initialState: HomeState = {
  featuredProjects: [],
  projects: [],
  loading: true,
  featuredLoading: true,
  error: null,
  totalCount: 0,
};

const skeletonCardSlots = [0, 1, 2, 3, 4, 5];

export const HomeStore = signalStore(
  withState(initialState),
  withProps(() => {
    const projectFeedFiltersService = inject(ProjectFeedFiltersService);

    return {
      filters: projectFeedFiltersService.filters,
      _destroyRef: inject(DestroyRef),
      _projectFeedFiltersService: projectFeedFiltersService,
      _projectService: inject(ProjectService),
    };
  }),
  withComputed(({ filters, totalCount }) => ({
    keyword: () => filters().keyword,
    selectedState: () => filters().state,
    sortNewest: () => filters().sortNewest,
    pageNumber: () => filters().pageNumber,
    pageSize: () => filters().pageSize,
    hasActiveFilters: () =>
      filters().keyword.length > 0 || filters().state !== 'All' || !filters().sortNewest,
    activeFilters: () => {
      const activeFilters: string[] = [];
      const currentFilters = filters();

      if (currentFilters.keyword.length > 0) {
        activeFilters.push(`Keyword: ${currentFilters.keyword}`);
      }

      if (currentFilters.state !== 'All') {
        activeFilters.push(`State: ${currentFilters.state}`);
      }

      if (!currentFilters.sortNewest) {
        activeFilters.push('Sort: Oldest first');
      }

      return activeFilters;
    },
    totalPages: () => Math.max(1, Math.ceil(totalCount() / filters().pageSize)),
    hasPreviousPage: () => filters().pageNumber > 1,
    hasNextPage: () => filters().pageNumber < Math.max(1, Math.ceil(totalCount() / filters().pageSize)),
    visibleRangeStart: () =>
      totalCount() === 0 ? 0 : (filters().pageNumber - 1) * filters().pageSize + 1,
    visibleRangeEnd: () => Math.min(filters().pageNumber * filters().pageSize, totalCount()),
    skeletonCards: () => {
      const skeletonCount = Math.max(2, Math.min(filters().pageSize, skeletonCardSlots.length));
      return skeletonCardSlots.slice(0, skeletonCount);
    },
  })),
  withMethods((store) => {
    let featuredRequestVersion = 0;
    let projectRequestVersion = 0;

    const loadFeaturedProjects = (): void => {
      const requestVersion = ++featuredRequestVersion;
      patchState(store, { featuredLoading: true });

      store._projectService.getPaginated(null, 1, 6)
        .pipe(takeUntilDestroyed(store._destroyRef))
        .subscribe({
          next: result => {
            if (requestVersion !== featuredRequestVersion) {
              return;
            }

            patchState(store, {
              featuredProjects: result.data ?? [],
              featuredLoading: false,
            });
          },
          error: () => {
            if (requestVersion !== featuredRequestVersion) {
              return;
            }

            patchState(store, {
              featuredProjects: [],
              featuredLoading: false,
            });
          }
        });
    };

    const loadProjects = (filters: ProjectFeedFilters): void => {
      const requestVersion = ++projectRequestVersion;
      patchState(store, {
        loading: true,
        error: null,
      });

      store._projectService.search({
        pageNumber: filters.pageNumber,
        pageSize: filters.pageSize,
        state: filters.state === 'All' ? null : filters.state,
        keyword: filters.keyword || null,
        sortNewest: filters.sortNewest
      })
        .pipe(takeUntilDestroyed(store._destroyRef))
        .subscribe({
          next: result => {
            if (requestVersion !== projectRequestVersion) {
              return;
            }

            const projects = result.data ?? [];

            patchState(store, {
              projects,
              totalCount: result.totalCount ?? projects.length,
              loading: false,
            });
          },
          error: () => {
            if (requestVersion !== projectRequestVersion) {
              return;
            }

            patchState(store, {
              projects: [],
              totalCount: 0,
              loading: false,
              error: 'Could not load projects right now.',
            });
          }
        });
    };

    return {
      applyFilters(): void {
        loadProjects(store.filters());
      },

      clearFilters(): void {
        store._projectFeedFiltersService.resetFilters();
      },

      goToNextPage(): void {
        if (!store.hasNextPage()) {
          return;
        }

        store._projectFeedFiltersService.updateFilters({
          pageNumber: store.pageNumber() + 1,
        });
      },

      goToPreviousPage(): void {
        if (!store.hasPreviousPage()) {
          return;
        }

        store._projectFeedFiltersService.updateFilters({
          pageNumber: store.pageNumber() - 1,
        });
      },

      retry(): void {
        loadFeaturedProjects();
        loadProjects(store.filters());
      },

      _loadFeaturedProjects(): void {
        loadFeaturedProjects();
      },

      _loadProjects(filters: ProjectFeedFilters): void {
        loadProjects(filters);
      },
    };
  }),
  withHooks({
    onInit(store) {
      effect(() => {
        store._loadProjects(store.filters());
      });

      store._loadFeaturedProjects();
    }
  })
);
