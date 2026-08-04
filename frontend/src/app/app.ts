import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
@Component({ selector: 'app-root', imports: [RouterOutlet], templateUrl: './app.html', styleUrl: './app.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class App { constructor(translate: TranslateService) { const language = localStorage.getItem('lang') ?? 'bs'; translate.setFallbackLang('bs'); translate.use(language); } }
