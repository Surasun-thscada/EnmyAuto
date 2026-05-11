import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { RenderProgressEvent, RenderProgressService } from '../../services/render-progress.service';

@Component({
  selector: 'app-render-progress',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="w-full space-y-3" *ngIf="event">
      <!-- Progress bar -->
      <div class="h-3 w-full rounded-full bg-gray-200 overflow-hidden">
        <div
          class="h-full rounded-full transition-all duration-500"
          [style.width.%]="event.progressPercent"
          [class]="barClass">
        </div>
      </div>

      <!-- Status label -->
      <p class="text-sm font-medium" [class]="labelClass">
        {{ statusLabel }}
        <span *ngIf="event.eventType === 'Progress'">
          — {{ event.progressPercent }}%
        </span>
      </p>

      <!-- Download link on completion -->
      <a
        *ngIf="event.eventType === 'Completed' && event.outputPath"
        [href]="downloadUrl"
        download
        class="inline-block mt-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm text-white hover:bg-indigo-700">
        Download MP4
      </a>

      <!-- Error message -->
      <p *ngIf="event.eventType === 'Failed'" class="text-sm text-red-600">
        {{ event.errorMessage }}
      </p>
    </div>
  `
})
export class RenderProgressComponent implements OnInit, OnDestroy {
  @Input({ required: true }) storyboardId!: string;

  event: RenderProgressEvent | null = null;
  private sub!: Subscription;

  constructor(private renderProgress: RenderProgressService) {}

  async ngOnInit(): Promise<void> {
    this.sub = this.renderProgress.progress$
      .pipe(filter(Boolean))
      .subscribe(e => (this.event = e));

    await this.renderProgress.subscribeAsync(this.storyboardId);
  }

  async ngOnDestroy(): Promise<void> {
    this.sub?.unsubscribe();
    await this.renderProgress.unsubscribeAsync(this.storyboardId);
  }

  get barClass(): string {
    const map: Record<string, string> = {
      Started:   'bg-blue-400',
      Progress:  'bg-indigo-500',
      Completed: 'bg-green-500',
      Failed:    'bg-red-500',
    };
    return map[this.event?.eventType ?? 'Progress'];
  }

  get labelClass(): string {
    const map: Record<string, string> = {
      Started:   'text-blue-600',
      Progress:  'text-indigo-600',
      Completed: 'text-green-600',
      Failed:    'text-red-600',
    };
    return map[this.event?.eventType ?? 'Progress'];
  }

  get statusLabel(): string {
    const map: Record<string, string> = {
      Started:   'Render started…',
      Progress:  'Rendering',
      Completed: 'Render complete!',
      Failed:    'Render failed',
    };
    return map[this.event?.eventType ?? 'Progress'];
  }

  get downloadUrl(): string {
    return `/api/media/download?path=${encodeURIComponent(this.event?.outputPath ?? '')}`;
  }
}
