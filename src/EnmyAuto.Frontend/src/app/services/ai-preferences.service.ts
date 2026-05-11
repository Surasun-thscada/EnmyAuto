import { Injectable, signal } from '@angular/core';

export interface AiPreferences {
  defaultCategory: string;
  defaultSceneCount: number;
  contentLanguage: 'en' | 'th';
  contentTone: 'funny' | 'serious' | 'educational' | 'inspirational';
}

const STORAGE_KEY = 'enmy_ai_prefs';

const DEFAULTS: AiPreferences = {
  defaultCategory:   '',
  defaultSceneCount: 5,
  contentLanguage:   'th',
  contentTone:       'funny',
};

@Injectable({ providedIn: 'root' })
export class AiPreferencesService {
  private _prefs = signal<AiPreferences>(this.load());
  readonly prefs = this._prefs.asReadonly();

  save(prefs: AiPreferences): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
    this._prefs.set(prefs);
  }

  private load(): AiPreferences {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? { ...DEFAULTS, ...JSON.parse(raw) } : { ...DEFAULTS };
    } catch {
      return { ...DEFAULTS };
    }
  }
}
