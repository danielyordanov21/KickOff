import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, tap } from 'rxjs';
import { User } from '../user.model';
import { AuthStateService } from './auth-state.service';
import { DiscoverPerson } from '../../app/models/discover-person.model';

export interface UpdateUserChatPreferencesRequest {
  preferredChatLanguage?: string | null;
  showOriginalChatTextByDefault: boolean;
}

export interface UpdateUserProfileRequest {
  userName: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = '/api/users';

  private http = inject(HttpClient);
  private authState = inject(AuthStateService);

  private static normalizeDiscoverUser(person: Partial<DiscoverPerson> & {
    publicId?: string;
    username?: string;
  }): DiscoverPerson {
    return {
      id: person.id ?? person.publicId ?? '',
      userName: person.userName ?? person.username ?? '',
      profilePictureUrl: person.profilePictureUrl ?? ''
    };
  }

  public getUser(id: string): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/${id}`);
  }

  public followUser(publicId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${publicId}/follow`, {});
  }

  public unfollowUser(publicId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${publicId}/follow`);
  }

  public getCurrentUser(): User | null {
    return this.authState.currentUser();
  }

  public becomeProducer(): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/become-producer`, {}).pipe(
      tap(user => this.authState.setUser(user))
    );
  }

  public updateProfile(payload: UpdateUserProfileRequest): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/profile`, payload).pipe(
      tap(user => this.authState.setUser(user))
    );
  }

  public updateChatPreferences(payload: UpdateUserChatPreferencesRequest): Observable<User> {
    return this.http.put<User>(`${this.apiUrl}/chat-preferences`, payload).pipe(
      tap(user => this.authState.setUser(user))
    );
  }

  public getDiscoverProducers(): Observable<DiscoverPerson[]> {
    return this.http
      .get<Array<Partial<DiscoverPerson> & { publicId?: string; username?: string }>>(`${this.apiUrl}/get-discover`)
      .pipe(
        map(people => (people ?? [])
          .map(UserService.normalizeDiscoverUser)
          .filter(person => person.id.length > 0 && person.userName.length > 0))
      );
  }

  public isCurrentUserOwner(ownerId: string): boolean {
    const currentUser = this.authState.currentUser();
    return currentUser ? currentUser.id === ownerId : false;
  }

  public canEditProject(ownerId: string): boolean {
    const currentUser = this.authState.currentUser();
    if (!currentUser) {
      return false;
    }

    const isAdmin = (currentUser.roles ?? []).some(role => role.toLowerCase() === 'admin');
    return isAdmin || currentUser.id === ownerId;
  }
}
