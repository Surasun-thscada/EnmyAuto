import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface UserSettingsDto {
  geminiApiKey:      string | null;
  geminiModel:       string;
  temperature:       number;
  maxOutputTokens:   number;
  contentLanguage:   string;
  contentTone:       string;
  defaultCategory:   string;
  defaultSceneCount: number;
}

export interface UpdateSettingsRequest extends UserSettingsDto {
  geminiApiKey: string | null;
}

@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private http = inject(HttpClient);
  private base = `${environment.apiBaseUrl}/api/settings`;

  get() {
    return this.http.get<UserSettingsDto>(this.base);
  }

  update(request: UpdateSettingsRequest) {
    return this.http.put<UserSettingsDto>(this.base, request);
  }
}
