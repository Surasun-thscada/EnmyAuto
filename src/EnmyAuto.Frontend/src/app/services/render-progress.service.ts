import { Injectable, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ── Types ─────────────────────────────────────────────────────────────────────

export type RenderEventType = 'Started' | 'Progress' | 'Completed' | 'Failed';

export interface RenderProgressEvent {
  storyboardId: string;
  eventType: RenderEventType;
  progressPercent: number;
  outputPath: string | null;
  errorMessage: string | null;
  timestamp: string;
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class RenderProgressService implements OnDestroy {
  private connection: signalR.HubConnection;

  private readonly _progress$ = new BehaviorSubject<RenderProgressEvent | null>(null);

  /** Stream of all render progress events for the currently subscribed storyboard. */
  readonly progress$: Observable<RenderProgressEvent | null> = this._progress$.asObservable();

  constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/render`, {
        accessTokenFactory: () => localStorage.getItem('access_token') ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000]) // retry delays in ms
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('onRenderProgress', (event: RenderProgressEvent) => {
      this._progress$.next(event);
    });
  }

  /** Call once (e.g. in AppComponent) to establish the WebSocket connection. */
  async startAsync(): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      await this.connection.start();
    }
  }

  /**
   * Subscribe to render events for a specific storyboard.
   * Safe to call multiple times — unsubscribes the previous storyboard first.
   */
  async subscribeAsync(storyboardId: string): Promise<void> {
    await this.startAsync();
    this._progress$.next(null); // clear previous job state
    await this.connection.invoke('SubscribeToStoryboard', storyboardId);
  }

  async unsubscribeAsync(storyboardId: string): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('UnsubscribeFromStoryboard', storyboardId);
    }
  }

  async stopAsync(): Promise<void> {
    await this.connection.stop();
  }

  ngOnDestroy(): void {
    this.connection.stop();
  }
}
