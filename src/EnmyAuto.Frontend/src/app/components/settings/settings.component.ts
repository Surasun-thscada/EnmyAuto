import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';
import { SettingsApiService, UserSettingsDto } from '../../services/settings-api.service';
import { environment } from '../../../environments/environment';
import { UserDto } from '../../models/auth.model';
import { STORYBOARD_CATEGORIES } from '../../models/storyboard.model';

function passwordMatchValidator(c: AbstractControl) {
  return c.get('newPassword')?.value === c.get('confirmPassword')?.value
    ? null : { mismatch: true };
}

export type Tab = 'profile' | 'ai' | 'tiktok' | 'security' | 'danger';

interface TikTokStatus {
  connected: boolean;
  accountId?: string;
  tokenExpiresAt?: string;
  connectedAt?: string;
}

const GEMINI_MODELS = [
  { value: 'gemini-2.0-flash',       label: 'Gemini 2.0 Flash'       },
  { value: 'gemini-2.0-flash-lite',   label: 'Gemini 2.0 Flash Lite'  },
  { value: 'gemini-1.5-pro',          label: 'Gemini 1.5 Pro'         },
  { value: 'gemini-1.5-flash',        label: 'Gemini 1.5 Flash'       },
];

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, DatePipe],
  templateUrl: './settings.component.html',
})
export class SettingsComponent implements OnInit {
  private fb          = inject(FormBuilder);
  private http        = inject(HttpClient);
  private auth        = inject(AuthService);
  private settingsApi = inject(SettingsApiService);
  private router      = inject(Router);
  private base        = `${environment.apiBaseUrl}/api`;

  readonly user        = this.auth.user;
  readonly activeTab   = signal<Tab>('profile');
  readonly geminiModels = GEMINI_MODELS;
  readonly categories   = STORYBOARD_CATEGORIES;
  readonly languages    = [
    { value: 'th', label: 'Thai (ภาษาไทย)' },
    { value: 'en', label: 'English'         },
  ];
  readonly tones = [
    { value: 'funny',         label: 'Funny & Entertaining'      },
    { value: 'serious',       label: 'Serious & Professional'    },
    { value: 'educational',   label: 'Educational & Informative' },
    { value: 'inspirational', label: 'Inspirational & Motivational' },
  ];
  readonly sceneCounts = [3, 5, 7];

  // ── Profile ─────────────────────────────────────────────────────────────────
  profileForm = this.fb.group({
    name:  [this.user()?.name ?? '', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
    email: [{ value: this.user()?.email ?? '', disabled: true }],
  });
  profileLoading = false;
  profileSuccess = '';
  profileError   = '';

  // ── AI Settings ─────────────────────────────────────────────────────────────
  aiForm = this.fb.group({
    geminiApiKey:      [''],
    geminiModel:       ['gemini-2.0-flash', Validators.required],
    temperature:       [0.7, [Validators.required, Validators.min(0), Validators.max(2)]],
    maxOutputTokens:   [1500, [Validators.required, Validators.min(100), Validators.max(8192)]],
    contentLanguage:   ['th', Validators.required],
    contentTone:       ['funny', Validators.required],
    defaultCategory:   [''],
    defaultSceneCount: [5, Validators.required],
  });
  aiLoading    = false;
  aiSuccess    = '';
  aiError      = '';
  aiKeyMasked  = true;
  currentApiKeyDisplay = '';

  // ── TikTok ──────────────────────────────────────────────────────────────────
  tiktokStatus: TikTokStatus | null = null;
  tiktokLoading       = false;
  tiktokDisconnecting = false;
  tiktokError         = '';

  // ── Security ─────────────────────────────────────────────────────────────────
  passwordForm = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword:     ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  }, { validators: passwordMatchValidator });
  showCurrentPw   = false;
  showNewPw       = false;
  passwordLoading = false;
  passwordSuccess = '';
  passwordError   = '';

  // ── Danger Zone ──────────────────────────────────────────────────────────────
  deleteConfirmText = '';
  deleteLoading     = false;
  deleteError       = '';

  // ── Getters ──────────────────────────────────────────────────────────────────
  get nameCtrl()         { return this.profileForm.get('name')!; }
  get currentPwCtrl()    { return this.passwordForm.get('currentPassword')!; }
  get newPwCtrl()        { return this.passwordForm.get('newPassword')!; }
  get confirmPwCtrl()    { return this.passwordForm.get('confirmPassword')!; }
  get passwordMismatch() { return this.passwordForm.errors?.['mismatch'] && this.confirmPwCtrl.touched; }
  get temperatureCtrl()  { return this.aiForm.get('temperature')!; }

  ngOnInit(): void {
    this.loadAiSettings();
    this.loadTikTokStatus();
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
    this.profileSuccess = this.profileError = '';
    this.passwordSuccess = this.passwordError = '';
    this.aiSuccess = this.aiError = '';
    this.tiktokError = '';
  }

  // ── Profile ──────────────────────────────────────────────────────────────────
  saveProfile(): void {
    if (this.profileForm.invalid) { this.profileForm.markAllAsTouched(); return; }
    this.profileLoading = true;
    this.profileSuccess = this.profileError = '';
    this.http.put<UserDto>(`${this.base}/auth/profile`, { name: this.nameCtrl.value })
      .pipe(finalize(() => (this.profileLoading = false)))
      .subscribe({
        next:  u   => { this.auth.updateUser(u); this.profileSuccess = 'Profile updated successfully.'; },
        error: err => (this.profileError = err.error?.error ?? 'Failed to update profile.'),
      });
  }

  // ── AI Settings ───────────────────────────────────────────────────────────────
  loadAiSettings(): void {
    this.aiLoading = true;
    this.settingsApi.get()
      .pipe(finalize(() => (this.aiLoading = false)))
      .subscribe({
        next: s => {
          this.currentApiKeyDisplay = s.geminiApiKey ?? '';
          this.aiForm.patchValue({
            geminiApiKey:      '',
            geminiModel:       s.geminiModel,
            temperature:       s.temperature,
            maxOutputTokens:   s.maxOutputTokens,
            contentLanguage:   s.contentLanguage,
            contentTone:       s.contentTone,
            defaultCategory:   s.defaultCategory,
            defaultSceneCount: s.defaultSceneCount,
          });
        },
        error: () => {},
      });
  }

  saveAiSettings(): void {
    if (this.aiForm.invalid) { this.aiForm.markAllAsTouched(); return; }
    this.aiLoading = true;
    this.aiSuccess = this.aiError = '';

    const v = this.aiForm.value;
    this.settingsApi.update({
      geminiApiKey:      v.geminiApiKey?.trim() || null,
      geminiModel:       v.geminiModel!,
      temperature:       v.temperature!,
      maxOutputTokens:   v.maxOutputTokens!,
      contentLanguage:   v.contentLanguage!,
      contentTone:       v.contentTone!,
      defaultCategory:   v.defaultCategory ?? '',
      defaultSceneCount: v.defaultSceneCount!,
    })
      .pipe(finalize(() => (this.aiLoading = false)))
      .subscribe({
        next: s => {
          this.currentApiKeyDisplay = s.geminiApiKey ?? '';
          this.aiForm.patchValue({ geminiApiKey: '' });
          this.aiSuccess = 'AI settings saved successfully.';
        },
        error: err => (this.aiError = err.error?.error ?? 'Failed to save settings.'),
      });
  }

  getTemperatureLabel(): string {
    const t = this.temperatureCtrl.value ?? 0.7;
    if (t <= 0.3) return 'Precise';
    if (t <= 0.7) return 'Balanced';
    if (t <= 1.2) return 'Creative';
    return 'Wild';
  }

  getTemperatureColor(): string {
    const t = this.temperatureCtrl.value ?? 0.7;
    if (t <= 0.3) return 'text-sky-400';
    if (t <= 0.7) return 'text-green-400';
    if (t <= 1.2) return 'text-yellow-400';
    return 'text-red-400';
  }

  // ── TikTok ────────────────────────────────────────────────────────────────────
  loadTikTokStatus(): void {
    this.tiktokLoading = true;
    this.http.get<TikTokStatus>(`${this.base}/tiktok/auth/status`)
      .pipe(finalize(() => (this.tiktokLoading = false)))
      .subscribe({
        next:  s  => (this.tiktokStatus = s),
        error: () => (this.tiktokError  = 'Could not load TikTok status.'),
      });
  }

  connectTikTok(): void {
    const userId = this.auth.user()?.id;
    window.location.href = `${this.base}/tiktok/auth/connect?userId=${userId}`;
  }

  disconnectTikTok(): void {
    this.tiktokDisconnecting = true;
    this.http.delete(`${this.base}/tiktok/auth/disconnect`)
      .pipe(finalize(() => (this.tiktokDisconnecting = false)))
      .subscribe({
        next:  () => (this.tiktokStatus = { connected: false }),
        error: err => (this.tiktokError = err.error?.error ?? 'Failed to disconnect.'),
      });
  }

  // ── Security ──────────────────────────────────────────────────────────────────
  changePassword(): void {
    if (this.passwordForm.invalid) { this.passwordForm.markAllAsTouched(); return; }
    this.passwordLoading = true;
    this.passwordSuccess = this.passwordError = '';
    this.http.put(`${this.base}/auth/password`, {
      currentPassword: this.currentPwCtrl.value,
      newPassword:     this.newPwCtrl.value,
    })
      .pipe(finalize(() => (this.passwordLoading = false)))
      .subscribe({
        next:  () => { this.passwordSuccess = 'Password changed successfully.'; this.passwordForm.reset(); },
        error: err => (this.passwordError = err.error?.error ?? 'Failed to change password.'),
      });
  }

  getPasswordStrength(): number {
    const pw = this.newPwCtrl.value ?? '';
    let s = 0;
    if (pw.length >= 8)          s++;
    if (/[A-Z]/.test(pw))        s++;
    if (/[0-9]/.test(pw))        s++;
    if (/[^A-Za-z0-9]/.test(pw)) s++;
    return s;
  }
  getStrengthColor(bar: number): string {
    const s = this.getPasswordStrength();
    if (bar > s) return 'bg-slate-700';
    return ['', 'bg-red-500', 'bg-orange-500', 'bg-yellow-500', 'bg-green-500'][s];
  }
  getStrengthLabel(): string {
    return ['', 'Weak', 'Fair', 'Good', 'Strong'][this.getPasswordStrength()];
  }

  // ── Danger ────────────────────────────────────────────────────────────────────
  deleteAccount(): void {
    if (this.deleteConfirmText !== 'DELETE') return;
    this.deleteLoading = true;
    this.http.delete(`${this.base}/auth/account`)
      .pipe(finalize(() => (this.deleteLoading = false)))
      .subscribe({
        next:  () => { this.auth.clearSession(); this.router.navigate(['/login']); },
        error: err => (this.deleteError = err.error?.error ?? 'Failed to delete account.'),
      });
  }
}
