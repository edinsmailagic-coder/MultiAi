import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ThemeService } from './core/services/theme.service';
import { DialogHost } from './shared/components/dialog-host/dialog-host';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, DialogHost],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  constructor(translate: TranslateService) {
    inject(ThemeService);
    const language = localStorage.getItem('lang') ?? 'bs';
    translate.setFallbackLang('bs');
    translate.use(language);
  }
}
