import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { StoryboardApiService } from '../../services/storyboard-api.service';
import {
  StoryboardScript,
  STORYBOARD_CATEGORIES,
} from '../../models/storyboard.model';
import { TotalDurationPipe } from '../../pipes/total-duration.pipe';

@Component({
  selector: 'app-smart-storyboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TotalDurationPipe],
  templateUrl: './smart-storyboard.component.html',
})
export class SmartStoryboardComponent {
  private fb       = inject(FormBuilder);
  private api      = inject(StoryboardApiService);

  readonly categories = STORYBOARD_CATEGORIES;

  form: FormGroup = this.fb.group({
    productName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(120)]],
    category:    ['', Validators.required],
  });

  isLoading  = false;
  storyboard: StoryboardScript | null = null;
  errorMsg:   string | null = null;

  // ── Derived helpers ──────────────────────────────────────────────────────

  get productNameCtrl() { return this.form.get('productName')!; }
  get categoryCtrl()    { return this.form.get('category')!;    }

  get selectedCategoryLabel(): string {
    return this.categories.find(c => c.value === this.categoryCtrl.value)?.label ?? '';
  }

  trackByScene(_: number, scene: { scene_number: number }) {
    return scene.scene_number;
  }

  // ── Actions ──────────────────────────────────────────────────────────────

  generate(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading  = true;
    this.storyboard = null;
    this.errorMsg   = null;

    this.api
      .generate({
        productName: this.productNameCtrl.value.trim(),
        category:    this.categoryCtrl.value,
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next:  res  => (this.storyboard = res.script),
        error: err  => (this.errorMsg   = err.message),
      });
  }

  reset(): void {
    this.form.reset();
    this.storyboard = null;
    this.errorMsg   = null;
  }
}
