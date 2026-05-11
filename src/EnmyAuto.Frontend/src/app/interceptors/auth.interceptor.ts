import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  // Skip auth endpoints to avoid infinite loops
  if (isAuthEndpoint(req.url)) return next(req);

  const token = auth.getAccessToken();
  const authedReq = token ? addBearer(req, token) : req;

  return next(authedReq).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401) return throwError(() => err);

      // Try refreshing once — if that also 401s, send to login
      return auth.refresh().pipe(
        switchMap(res => next(addBearer(req, res.accessToken))),
        catchError(refreshErr => {
          router.navigate(['/login']);
          return throwError(() => refreshErr);
        })
      );
    })
  );
};

const addBearer = (req: HttpRequest<unknown>, token: string) =>
  req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

const isAuthEndpoint = (url: string) =>
  url.includes('/api/auth/login') ||
  url.includes('/api/auth/register') ||
  url.includes('/api/auth/refresh');
