import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {TranslateService} from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  constructor(private translate: TranslateService) {
    // Set the default language to Bosnian
    this.translate.setFallbackLang('bs');
    this.translate.use('bs'); // Use Bosnian as the current language
  }
  protected readonly title = signal('multiAi');
}
