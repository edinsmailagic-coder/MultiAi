import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-partner-strip',
  imports: [TranslatePipe],
  templateUrl: './partner-strip.html',
  styleUrl: './partner-strip.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PartnerStrip {
  readonly partners = [
    { nameKey: 'landing.partners.p1.name', quoteKey: 'landing.partners.p1.quote' },
    { nameKey: 'landing.partners.p2.name', quoteKey: 'landing.partners.p2.quote' },
    { nameKey: 'landing.partners.p3.name', quoteKey: 'landing.partners.p3.quote' },
    { nameKey: 'landing.partners.p4.name', quoteKey: 'landing.partners.p4.quote' },
  ] as const;
}
