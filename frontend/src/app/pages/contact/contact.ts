import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { TopBar } from '../../shared/components/top-bar/top-bar';

@Component({
  selector: 'app-contact',
  imports: [RouterLink, TranslatePipe, TopBar],
  templateUrl: './contact.html',
  styleUrl: './contact.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Contact {
  readonly details = [
    { labelKey: 'landing.contact.email', value: 'test@nodexa.test', href: 'mailto:test@nodexa.test' },
    { labelKey: 'landing.contact.phone', value: '+000 00 000 000', href: 'tel:+00000000000' },
    { labelKey: 'landing.contact.location', value: 'Test City, Test Street 1' },
    { labelKey: 'landing.contact.hours', value: 'Test: Mon–Fri 09:00–17:00' },
    { labelKey: 'landing.contact.company', value: 'Nodexa Test d.o.o.' },
  ] as const;
}
