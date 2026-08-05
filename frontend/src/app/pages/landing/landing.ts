import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Hero } from '../../shared/components/hero/hero';
import { PartnerStrip } from '../../shared/components/partner-strip/partner-strip';
import { TopBar } from '../../shared/components/top-bar/top-bar';

@Component({
  selector: 'app-landing',
  imports: [TopBar, Hero, PartnerStrip],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Landing {}
