import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-brand',
  imports: [RouterLink],
  templateUrl: './brand.html',
  styleUrl: './brand.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Brand {
  /** Router link target for the brand anchor. */
  readonly link = input<string>('/');
  /** Visual size variant. */
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  /** Show wordmark next to the mark. */
  readonly showName = input(true);
}
