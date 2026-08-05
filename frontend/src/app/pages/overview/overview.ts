import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { TopBar } from '../../shared/components/top-bar/top-bar';

@Component({
  selector: 'app-overview',
  imports: [RouterLink, TranslatePipe, TopBar],
  templateUrl: './overview.html',
  styleUrl: './overview.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Overview {}
