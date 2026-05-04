import { Injectable, inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, finalize, map, shareReplay, switchMap, tap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private authService = inject(AuthService);
  private refreshTokenRequest$: Observable<string> | null = null;

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {

    if (
      request.url.includes('/auth/login') ||
      request.url.includes('/auth/refresh') ||
      request.url.includes('/auth/register') ||
      request.url.includes('/auth/confirm-email') ||
      request.url.includes('/auth/resend-confirmation') ||
      request.url.includes('/auth/forgot-password') ||
      request.url.includes('/auth/reset-password')
    ) return next.handle(request);

    // Attach token if present
    const token = localStorage.getItem('accessToken');
    if (token) {
      request = this.addToken(request, token);
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status !== 401) {
          return throwError(() => error);
        }

        return this.refreshAccessToken().pipe(
          switchMap(newToken => next.handle(this.addToken(request, newToken))),
          catchError((refreshError: HttpErrorResponse) => {
            if (refreshError.status === 401 || refreshError.status === 403) {
              this.authService.clearLocalAuthState();
            }

            return throwError(() => error);
          })
        );
      })
    );
  }

  private refreshAccessToken(): Observable<string> {
    if (!this.refreshTokenRequest$) {
      this.refreshTokenRequest$ = this.authService.refreshToken().pipe(
        tap(response => localStorage.setItem('accessToken', response.accessToken)),
        map(response => response.accessToken),
        finalize(() => {
          this.refreshTokenRequest$ = null;
        }),
        shareReplay(1)
      );
    }

    return this.refreshTokenRequest$;
  }

  private addToken(request: HttpRequest<any>, token: string) {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }
}
