import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ProjectCatalogueDto } from '../models/project-models/project-catalogue.model';
import { map, Observable } from 'rxjs';
import { Project } from '../models/project-models/project.model';
import { PaginatedResult } from '../models/project-models/project-paginated.model';
import { ProjectDtoRaw } from '../models/project-models/project-dto-raw';
import { CreateProjectRequest } from '../models/project-models/create-project-request.model';
import { ProjectFollowState } from '../models/project-models/project-follow.model';
import {
  ProjectNotification,
  ProjectNotificationFeed,
  ProjectNotificationFeedRaw,
  ProjectNotificationRaw
} from '../models/project-models/project-notification.model';
import { ProjectUpdate, ProjectUpdateRaw } from '../models/project-models/project-update.model';
import { SaveProjectUpdateRequest } from '../models/project-models/save-project-update-request.model';
import { UpdateProjectFollowPreferencesRequest } from '../models/project-models/update-project-follow-preferences-request.model';

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private readonly projectStateLabels = ['Proposed', 'Cancelled', 'Active', 'Inactive', 'OnHold', 'Completed'] as const;
  private apiUrl = '/api/project';

  constructor(private http: HttpClient) { }

  public getById(id: string): Observable<Project> {
    return this.http
      .get<ProjectDtoRaw>(`${this.apiUrl}/${id}`)
      .pipe(map(dto => this.mapProject(dto)));
  }

  public getCatalogue(): Observable<ProjectCatalogueDto[]> {
    return this.http.get<ProjectCatalogueDto[]>(`${this.apiUrl}/projects`);
  }

  public getCatalogueByState(state: string): Observable<ProjectCatalogueDto[]> {
    return this.http.get<ProjectCatalogueDto[]>(
      `${this.apiUrl}/projects/state/${encodeURIComponent(state)}`
    );
  }

  public createProject(payload: CreateProjectRequest, imageFiles: File[] = []): Observable<Project> {
    return this.http
      .post<ProjectDtoRaw>(this.apiUrl, this.buildProjectFormData(payload, imageFiles))
      .pipe(map(dto => this.mapProject(dto)));
  }

  public updateProject(id: string, payload: CreateProjectRequest, imageFiles: File[] = []): Observable<Project> {
    return this.http
      .put<ProjectDtoRaw>(`${this.apiUrl}/${id}`, this.buildProjectFormData(payload, imageFiles))
      .pipe(map(dto => this.mapProject(dto)));
  }

  public createUpdate(projectId: string, payload: SaveProjectUpdateRequest): Observable<ProjectUpdate> {
    return this.http
      .post<ProjectUpdateRaw>(`${this.apiUrl}/${projectId}/updates`, payload)
      .pipe(map(dto => this.mapProjectUpdate(dto)));
  }

  public followProject(projectId: string): Observable<ProjectFollowState> {
    return this.http
      .post<ProjectFollowState>(`${this.apiUrl}/${projectId}/follow`, {})
      .pipe(map(dto => this.mapProjectFollow(dto)));
  }

  public unfollowProject(projectId: string): Observable<ProjectFollowState> {
    return this.http
      .delete<ProjectFollowState>(`${this.apiUrl}/${projectId}/follow`)
      .pipe(map(dto => this.mapProjectFollow(dto)));
  }

  public updateFollowPreferences(
    projectId: string,
    payload: UpdateProjectFollowPreferencesRequest
  ): Observable<ProjectFollowState> {
    return this.http
      .put<ProjectFollowState>(`${this.apiUrl}/${projectId}/follow/preferences`, payload)
      .pipe(map(dto => this.mapProjectFollow(dto)));
  }

  public getNotifications(take = 12): Observable<ProjectNotificationFeed> {
    const params = new HttpParams().set('take', String(take));

    return this.http
      .get<ProjectNotificationFeedRaw>(`${this.apiUrl}/notifications`, { params })
      .pipe(map(feed => this.mapNotificationFeed(feed)));
  }

  public markNotificationRead(notificationId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/notifications/${notificationId}/read`, {});
  }

  public markAllNotificationsRead(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/notifications/read-all`, {});
  }

  public updateUpdate(projectId: string, updateId: string, payload: SaveProjectUpdateRequest): Observable<ProjectUpdate> {
    return this.http
      .put<ProjectUpdateRaw>(`${this.apiUrl}/${projectId}/updates/${updateId}`, payload)
      .pipe(map(dto => this.mapProjectUpdate(dto)));
  }

  public deleteUpdate(projectId: string, updateId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${projectId}/updates/${updateId}`);
  }

  private mapProject(dto: ProjectDtoRaw): Project {
    const {
      id,
      name,
      headline,
      goal = '',
      description,
      imageUrls,
      state,
      extraInfo,
      owner,
      ownerId,
      ownerPublicId,
      category,
      financialGoal,
      problem,
      imageBlobNames = [],
      collaboratorsIdP = [],
      contacts = [],
      tags = [],
      backerIds = [],
      updates = [],
      follow,
      startDate,
      endDate,
      settingsId
    } = dto;

    return {
      id,
      name,
      headline,
      goal,
      description,
      imageUrls: this.cleanValues(imageUrls),
      state: this.normalizeState(state),
      extraInfo,
      owner,
      ownerId,
      ownerPublicId: ownerPublicId ?? undefined,
      category,
      financialGoal,
      problem,
      imageBlobNames: this.cleanValues(imageBlobNames),
      collaboratorsIdP: this.cleanValues(collaboratorsIdP),
      contacts: this.cleanValues(contacts),
      tags: this.cleanValues(tags),
      backerIds: this.cleanValues(backerIds),
      updates: updates.map(update => this.mapProjectUpdate(update)),
      follow: this.mapProjectFollow(follow),
      startDate: this.parseDate(startDate),
      endDate: this.parseDate(endDate),
      settingsId: settingsId ?? undefined,
    };
  }

  private mapProjectUpdate(dto: ProjectUpdateRaw): ProjectUpdate {
    return {
      id: dto.id,
      projectId: dto.projectId,
      title: dto.title.trim(),
      content: dto.content.trim(),
      authorUserId: dto.authorUserId,
      authorName: dto.authorName.trim(),
      createdAt: this.parseDate(dto.createdAt),
      updatedAt: this.parseDate(dto.updatedAt),
      isEdited: dto.isEdited ?? false,
    };
  }

  private mapProjectFollow(dto?: Partial<ProjectFollowState> | null): ProjectFollowState {
    return {
      followersCount: this.normalizeNonNegativeNumber(dto?.followersCount),
      isFollowing: dto?.isFollowing ?? false,
      receiveInAppNotifications: dto?.receiveInAppNotifications ?? true,
      receiveEmailNotifications: dto?.receiveEmailNotifications ?? true,
    };
  }

  private mapNotificationFeed(dto?: ProjectNotificationFeedRaw | null): ProjectNotificationFeed {
    return {
      notifications: (dto?.notifications ?? []).map(notification => this.mapNotification(notification)),
      unreadCount: this.normalizeNonNegativeNumber(dto?.unreadCount),
    };
  }

  private mapNotification(dto: ProjectNotificationRaw): ProjectNotification {
    return {
      id: dto.id,
      projectId: dto.projectId,
      projectName: dto.projectName.trim(),
      projectUpdateId: dto.projectUpdateId?.trim() || undefined,
      title: dto.title.trim(),
      message: dto.message.trim(),
      isRead: dto.isRead ?? false,
      createdAt: this.parseDate(dto.createdAt),
    };
  }

  private cleanValues(values?: string[] | null): string[] {
    return (values ?? [])
      .map(value => value.trim())
      .filter(value => value.length > 0);
  }

  private parseDate(value?: string | null): Date | undefined {
    if (!value) {
      return undefined;
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? undefined : parsed;
  }

  private normalizeState(value: string | number): string {
    if (typeof value === 'string') {
      const trimmed = value.trim();
      return trimmed.length > 0 ? trimmed : 'Inactive';
    }

    return this.projectStateLabels[value] ?? 'Inactive';
  }

  private normalizeNonNegativeNumber(value: number | null | undefined): number {
    if (typeof value !== 'number' || Number.isNaN(value)) {
      return 0;
    }

    return Math.max(0, Math.floor(value));
  }

  private buildProjectFormData(payload: CreateProjectRequest, imageFiles: File[]): FormData {
    const formData = new FormData();
    formData.append('project', JSON.stringify(payload));

    imageFiles.forEach(file => formData.append('files', file));

    return formData;
  }

  public search(options: {
    pageNumber?: number;
    pageSize?: number;
    state?: string | null;
    keyword?: string | null;
    owner?: string | null;
    sortNewest?: boolean;
  }): Observable<PaginatedResult<ProjectCatalogueDto>> {
    let params = new HttpParams();

    if (options.pageNumber) params = params.set('pageNumber', String(options.pageNumber));
    if (options.pageSize) params = params.set('pageSize', String(options.pageSize));
    if (options.state) params = params.set('state', options.state);
    if (options.keyword) params = params.set('keyword', options.keyword);
    if (options.owner) params = params.set('owner', options.owner);
    if (options.sortNewest !== undefined)
      params = params.set('sortNewest', String(options.sortNewest));

    return this.http.get<PaginatedResult<ProjectCatalogueDto>>(`${this.apiUrl}/search`, { params });
  }

  public searchByGoal(keyword: string, state?: string): Observable<ProjectCatalogueDto[]> {
    let params = new HttpParams().set('keyword', keyword);
    if (state) params = params.set('state', state);

    return this.http.get<ProjectCatalogueDto[]>(`${this.apiUrl}/search-by-goal`, { params });
  }

  public getPaginated(
    state: string | null = null,
    pageNumber = 1,
    pageSize = 20
  ): Observable<PaginatedResult<ProjectCatalogueDto>> {

    let params = new HttpParams()
      .set('pageNumber', String(pageNumber))
      .set('pageSize', String(pageSize));

    if (state) params = params.set('state', state);

    return this.http.get<PaginatedResult<ProjectCatalogueDto>>(`${this.apiUrl}/paginated`, { params });
  }

  public clearCache(): Observable<any> {
    return this.http.post(`${this.apiUrl}/cache/clear`, {});
  }
}
