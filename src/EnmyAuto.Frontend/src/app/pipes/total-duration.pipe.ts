import { Pipe, PipeTransform } from '@angular/core';
import { SceneBreakdown } from '../models/storyboard.model';

@Pipe({ name: 'totalDuration', standalone: true })
export class TotalDurationPipe implements PipeTransform {
  transform(scenes: SceneBreakdown[]): number {
    return scenes.reduce((sum, s) => sum + (s.duration_seconds ?? 0), 0);
  }
}
