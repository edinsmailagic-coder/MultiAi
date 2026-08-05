import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { TopBar } from '../../shared/components/top-bar/top-bar';

@Component({
  selector: 'app-docs',
  imports: [RouterLink, TranslatePipe, TopBar],
  templateUrl: './docs.html',
  styleUrl: './docs.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Docs {}
