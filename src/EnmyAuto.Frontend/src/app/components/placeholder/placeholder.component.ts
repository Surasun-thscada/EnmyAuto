import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

@Component({
  selector: 'app-placeholder',
  standalone: true,
  imports: [],
  template: `
    <div class="flex min-h-full items-center justify-center p-12">
      <div class="text-center space-y-4">
        <div class="inline-flex h-16 w-16 items-center justify-center rounded-2xl
                    bg-indigo-600/20 border border-indigo-500/30">
          <svg class="h-8 w-8 text-indigo-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                  d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"/>
          </svg>
        </div>
        <h2 class="text-xl font-bold text-white">{{ title() }}</h2>
        <p class="text-sm text-slate-400">This page is coming soon.</p>
      </div>
    </div>
  `,
})
export class PlaceholderComponent {
  private route = inject(ActivatedRoute);
  readonly title = toSignal(
    this.route.data.pipe(map(d => d['title'] ?? 'Coming Soon')),
    { initialValue: 'Coming Soon' }
  );
}
