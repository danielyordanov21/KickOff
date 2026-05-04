import { Injectable, signal, computed } from '@angular/core';
import { User } from '../user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthStateService {
  private currentUserSignal = signal<User | null>(null);
  private isLoadingSignal = signal(false);
  private isInitializedSignal = signal(false);

  currentUser = this.currentUserSignal.asReadonly();
  isLoading = this.isLoadingSignal.asReadonly();
  isInitialized = this.isInitializedSignal.asReadonly();

  isAuthenticated = computed(() => this.currentUserSignal() !== null);
  currentUserRoles = computed(() => this.normalizeRoles(this.currentUserSignal()));
  canCreateProjects = computed(() =>
    this.currentUserRoles().some(role =>
      ['producer', 'admin'].includes(role.toLowerCase())
    )
  );

  setUser(user: User | null) {
    if (!user) {
      this.currentUserSignal.set(null);
      return;
    }

    this.currentUserSignal.set({
      ...user,
      roles: this.normalizeRoles(user),
      role: user.role ?? this.normalizeRoles(user)[0]
    });
  }

  setLoading(value: boolean) {
    this.isLoadingSignal.set(value);
  }

  setInitialized(value: boolean) {
    this.isInitializedSignal.set(value);
  }

  updateCurrentUser(updater: (user: User) => User) {
    const currentUser = this.currentUserSignal();
    if (!currentUser) {
      return;
    }

    this.setUser(updater(currentUser));
  }

  clear() {
    this.currentUserSignal.set(null);
    this.isLoadingSignal.set(false);
    this.isInitializedSignal.set(true);
  }

  private normalizeRoles(user: User | null): string[] {
    if (!user) {
      return [];
    }

    const normalizedRoles = new Set<string>();

    for (const role of user.roles ?? []) {
      if (role?.trim()) {
        normalizedRoles.add(role.trim());
      }
    }

    if (user.role?.trim()) {
      normalizedRoles.add(user.role.trim());
    }

    return Array.from(normalizedRoles);
  }
}
