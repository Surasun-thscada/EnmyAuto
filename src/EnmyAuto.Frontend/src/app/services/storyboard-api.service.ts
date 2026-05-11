import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  GenerateStoryboardRequest,
  GenerateStoryboardResponse,
} from '../models/storyboard.model';

@Injectable({ providedIn: 'root' })
export class StoryboardApiService {
  private readonly base = `${environment.apiBaseUrl}/api/storyboards`;

  constructor(private http: HttpClient) {}

  generate(request: GenerateStoryboardRequest): Observable<GenerateStoryboardResponse> {
    return this.http
      .post<GenerateStoryboardResponse>(`${this.base}/generate`, request)
      .pipe(catchError(this.handleError));
  }

  private handleError(err: HttpErrorResponse): Observable<never> {
    const message =
      err.error?.error ??
      err.error?.message ??
      (err.status === 0
        ? 'Cannot reach the server. Check your connection.'
        : `Server error ${err.status}: ${err.statusText}`);

    return throwError(() => new Error(message));
  }
}
